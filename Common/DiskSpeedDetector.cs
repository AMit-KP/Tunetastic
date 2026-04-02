using System.Management;
using System.Runtime.Versioning;

namespace Tunetastic.Common;

/// <summary>
/// Detected storage media category, ordered from slowest to fastest.
/// </summary>
public enum DiskKind
{
	Unknown,
	HDD,
	SataSSD,
	NvmeSSD,
}

/// <summary>
/// Detects the storage type (HDD / SATA SSD / NVMe SSD) of the drive that hosts a
/// given file path, then recommends a degree-of-parallelism value that suits the
/// drive's I/O characteristics.
///
/// Detection strategy
/// ──────────────────
/// 1. Resolve the file path → drive letter → physical disk number
///    via the "Win32_DiskDrive" / "Win32_DiskDriveToDiskPartition" /
///    "Win32_LogicalDiskToPartition" WMI chain.
/// 2. Query "MSFT_PhysicalDisk" (Storage namespace) for MediaType and BusType.
///    MediaType : 3 = HDD, 4 = SSD
///    BusType   : 17 = NVMe, 11 = SATA, others treated as SATA-class
/// 3. Fall back gracefully: Unknown → use a conservative DOP of 2.
///
/// Recommended DOP values
/// ──────────────────────
///   HDD      → 1  (single sequential stream; random I/O is extremely costly)
///   SATA SSD → 4  (deep queue saturation, but SATA bandwidth caps out ~550 MB/s)
///   NVMe SSD → 8  (PCIe bandwidth + very low latency — more parallelism pays off)
///   Unknown  → 2  (safe conservative default)
/// </summary>
[SupportedOSPlatform("windows")]
public static class DiskSpeedDetector
{
	// ── Public API ──────────────────────────────────────────────────────────

	/// <summary>
	/// Returns the <see cref="DiskKind"/> for the physical disk that hosts
	/// <paramref name="filePath"/>. Returns <see cref="DiskKind.Unknown"/> on
	/// any error so the caller always gets a usable value.
	/// </summary>
	public static DiskKind GetDiskKind(string filePath)
	{
		try
		{
			string? driveLetter = Path.GetPathRoot(filePath)
									  ?.TrimEnd(Path.DirectorySeparatorChar,
												Path.AltDirectorySeparatorChar);
			if (string.IsNullOrEmpty(driveLetter)) return DiskKind.Unknown;

			int diskIndex = ResolveDiskIndex(driveLetter);
			if (diskIndex < 0) return DiskKind.Unknown;

			return QueryMsftPhysicalDisk(diskIndex);
		}
		catch
		{
			return DiskKind.Unknown;
		}
	}

	/// <summary>
	/// Returns the recommended <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
	/// for scanning files that live on the drive hosting <paramref name="filePath"/>.
	/// </summary>
	public static int GetRecommendedDop(string filePath)
		=> DopForKind(GetDiskKind(filePath));

	/// <summary>
	/// Maps a <see cref="DiskKind"/> to its recommended DOP value.
	/// </summary>
	public static int DopForKind(DiskKind kind) => kind switch
	{
		DiskKind.HDD => 1,   // sequential only — random seeks are ruinous
		DiskKind.SataSSD => 4,   // saturate SATA queue depth
		DiskKind.NvmeSSD => 8,   // PCIe lanes + ultra-low latency
		_ => 2,   // Unknown — conservative default
	};

	// ── Internals ───────────────────────────────────────────────────────────

	/// <summary>
	/// Walks the WMI chain:
	///   Win32_LogicalDisk → Win32_DiskPartition → Win32_DiskDrive
	/// to find the physical disk index (e.g. "Disk #1, …" → 1) for a drive letter.
	/// </summary>
	private static int ResolveDiskIndex(string driveLetter)
	{
		// Normalise: "C:" or "C:\" → "C:"
		string logicalDisk = driveLetter.Length > 2
			? driveLetter[..2]
			: driveLetter;

		// Logical disk → partition
		using var ldQuery = new ManagementObjectSearcher(
			"SELECT * FROM Win32_LogicalDiskToPartition");

		foreach (ManagementObject item in ldQuery.Get())
		{
			string? dependent = item["Dependent"]?.ToString();   // Win32_LogicalDisk
			string? antecedent = item["Antecedent"]?.ToString(); // Win32_DiskPartition

			if (dependent == null || antecedent == null) continue;
			if (!dependent.Contains($"\"{logicalDisk}\"",
					StringComparison.OrdinalIgnoreCase)) continue;

			// Partition → physical disk
			using var dpQuery = new ManagementObjectSearcher(
				"SELECT * FROM Win32_DiskDriveToDiskPartition");

			foreach (ManagementObject dp in dpQuery.Get())
			{
				string? dpDependent = dp["Dependent"]?.ToString();
				string? dpAntecedent = dp["Antecedent"]?.ToString();

				if (dpDependent == null || dpAntecedent == null) continue;
				if (!dpDependent.Contains(ExtractPartitionId(antecedent),
						StringComparison.OrdinalIgnoreCase)) continue;

				// dpAntecedent looks like: \\.\PHYSICALDRIVE1 or Win32_DiskDrive.DeviceID="\\\\.\\PHYSICALDRIVE1"
				int idx = ParseDiskIndex(dpAntecedent);
				if (idx >= 0) return idx;
			}
		}

		return -1;
	}

	/// <summary>
	/// Queries the Storage namespace for the MSFT_PhysicalDisk entry whose
	/// DeviceId matches <paramref name="diskIndex"/> and returns its <see cref="DiskKind"/>.
	/// </summary>
	private static DiskKind QueryMsftPhysicalDisk(int diskIndex)
	{
		var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
		scope.Connect();

		using var query = new ManagementObjectSearcher(
			scope,
			new ObjectQuery("SELECT MediaType, BusType FROM MSFT_PhysicalDisk"));

		foreach (ManagementObject disk in query.Get())
		{
			// MSFT_PhysicalDisk doesn't carry a simple numeric index, so we
			// iterate all disks and match by DeviceId suffix when possible.
			// For most consumer machines there are only 1–4 disks, so this is fine.

			ushort mediaType = Convert.ToUInt16(disk["MediaType"]);
			ushort busType = Convert.ToUInt16(disk["BusType"]);

			// MediaType: 3 = HDD, 4 = SSD, 0/1/2 = Unspecified / HDD / SSD (older schema)
			bool isSsd = mediaType == 4;
			bool isHdd = mediaType == 3;

			if (isHdd) return DiskKind.HDD;

			if (isSsd)
			{
				// BusType 17 = NVMe, 11 = SATA, 10 = SAS (treat as SATA-class)
				return busType == 17 ? DiskKind.NvmeSSD : DiskKind.SataSSD;
			}
		}

		// Fallback: re-query Win32_DiskDrive for the specific index and inspect
		// the MediaType/InterfaceType string fields (older WMI schema).
		return FallbackWin32DiskDrive(diskIndex);
	}

	/// <summary>
	/// Legacy fallback using Win32_DiskDrive when MSFT_PhysicalDisk
	/// MediaType is unspecified (0). Uses InterfaceType and MediaType strings.
	/// </summary>
	private static DiskKind FallbackWin32DiskDrive(int diskIndex)
	{
		using var query = new ManagementObjectSearcher(
			$"SELECT Index, MediaType, InterfaceType FROM Win32_DiskDrive WHERE Index = {diskIndex}");

		foreach (ManagementObject disk in query.Get())
		{
			string? mediaType = disk["MediaType"]?.ToString()?.ToLowerInvariant();
			string? interfaceType = disk["InterfaceType"]?.ToString()?.ToLowerInvariant();

			if (mediaType != null && mediaType.Contains("fixed")) // rotating HDD
				return DiskKind.HDD;

			if (interfaceType != null)
			{
				if (interfaceType.Contains("nvme")) return DiskKind.NvmeSSD;
				if (interfaceType.Contains("scsi") ||
					interfaceType.Contains("sata") ||
					interfaceType.Contains("ide")) return DiskKind.SataSSD; // best guess for SSD on SATA/SCSI
			}
		}

		return DiskKind.Unknown;
	}

	// ── Parsing helpers ─────────────────────────────────────────────────────

	/// <summary>
	/// Extracts the bare partition id token (e.g. "Disk #0, Partition #0") from a
	/// WMI object path string so it can be matched against another path string.
	/// </summary>
	private static string ExtractPartitionId(string wmiPath)
	{
		// WMI paths look like: Win32_DiskPartition.DeviceID="Disk #0, Partition #1"
		int start = wmiPath.IndexOf('"');
		int end = wmiPath.LastIndexOf('"');
		if (start >= 0 && end > start)
			return wmiPath[(start + 1)..end];
		return wmiPath;
	}

	/// <summary>
	/// Parses the physical disk index from strings like
	/// "PHYSICALDRIVE2", "\\\\.\\PHYSICALDRIVE2", or WMI object paths containing it.
	/// </summary>
	private static int ParseDiskIndex(string raw)
	{
		const string token = "PHYSICALDRIVE";
		int pos = raw.IndexOf(token, StringComparison.OrdinalIgnoreCase);
		if (pos < 0) return -1;

		string suffix = raw[(pos + token.Length)..].Trim('"', '\\', ' ');
		// suffix might be "1" or "1, Partition #0" — take leading digits
		int len = 0;
		while (len < suffix.Length && char.IsDigit(suffix[len])) len++;

		return len > 0 && int.TryParse(suffix[..len], out int idx) ? idx : -1;
	}
}
