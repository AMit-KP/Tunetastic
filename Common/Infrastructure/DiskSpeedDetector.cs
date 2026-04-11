using System.Management;
using System.Runtime.Versioning;

namespace Tunetastic.Common.Infrastructure;

/// <summary>
/// Detects the storage type (HDD / SATA SSD / NVMe SSD) of the drive that hosts a
/// given file path, then recommends a degree-of-parallelism value that suits the
/// drive's I/O characteristics.
/// <br/>
/// <br/>
/// Detection strategy
/// <br/>
/// ──────────────────
/// <br/>
/// 1. Resolve the file path → drive letter → physical disk number
///    via the "Win32_DiskDrive" / "Win32_DiskDriveToDiskPartition" /
///    "Win32_LogicalDiskToPartition" WMI chain.
///    <br/>
/// 2. Query "MSFT_PhysicalDisk" (Storage namespace) for MediaType and BusType.
/// <br/>
///    MediaType : 3 = HDD, 4 = SSD
///    <br/>
///    BusType   : 17 = NVMe, 11 = SATA, others treated as SATA-class
/// 3. Every WMI call is wrapped in Task.Run + Wait(timeout) so a hung/broken
///    WMI provider can NEVER stall the scan. Falls back to DiskKind.Unknown
///    (DOP=2) on any timeout or error.
///
/// Recommended DOP values
/// <br/>
/// ──────────────────────
/// <br/>
///   HDD      → 1  (single sequential stream; random I/O is extremely costly)
///   <br/>
///   SATA SSD → 4  (deep queue saturation, but SATA bandwidth caps out ~550 MB/s)
///   <br/>
///   NVMe SSD → 8  (PCIe bandwidth + very low latency — more parallelism pays off)
///   <br/>
///   Unknown  → 2  (safe conservative default)
/// </summary>
[SupportedOSPlatform("windows")]
public static class DiskSpeedDetector
{
	/// <summary>
	/// Hard ceiling on how long the entire detection is allowed to take.
	/// If WMI is broken/hung on a user's machine, we bail and fall back to
	/// DiskKind.Unknown so the scan starts immediately rather than freezing.
	/// </summary>
	private static readonly TimeSpan TotalDetectionTimeout = TimeSpan.FromSeconds(3);

	// ── Public API ──────────────────────────────────────────────────────────

	/// <summary>
	/// Returns the <see cref="DiskKind"/> for the physical disk that hosts
	/// <paramref name="filePath"/>. Guaranteed to return within
	/// <see cref="TotalDetectionTimeout"/> regardless of WMI provider health.
	/// Returns <see cref="DiskKind.Unknown"/> on timeout or any error.
	/// </summary>
	public static DiskKind GetDiskKind(string filePath)
	{
		try
		{
			var task = Task.Run(() => DetectInternal(filePath));

			return task.Wait(TotalDetectionTimeout) ? task.Result : DiskKind.Unknown;
		}
		catch
		{
			return DiskKind.Unknown;
		}
	}

	/// <summary>
	/// Returns the recommended <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
	/// for scanning files on the drive hosting <paramref name="filePath"/>.
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
		_ => 1,   // Unknown — conservative safe default
	};

	// ── Core detection (always runs inside Task.Run) ─────────────────────────

	private static DiskKind DetectInternal(string filePath)
	{
		string? driveLetter = Path.GetPathRoot(filePath)
								  ?.TrimEnd(Path.DirectorySeparatorChar,
											Path.AltDirectorySeparatorChar);
		if (string.IsNullOrEmpty(driveLetter)) return DiskKind.Unknown;

		int diskIndex = ResolveDiskIndex(driveLetter);
		if (diskIndex < 0) return DiskKind.Unknown;

		return QueryMsftPhysicalDisk(diskIndex);
	}

	// ── WMI helpers ──────────────────────────────────────────────────────────

	/// <summary>
	/// Walks the WMI chain:
	///   Win32_LogicalDisk → Win32_DiskPartition → Win32_DiskDrive
	/// to find the physical disk index for a drive letter.
	///
	/// The two association-table queries are each run with their own
	/// inner timeout so neither one can hang the whole call.
	/// </summary>
	private static int ResolveDiskIndex(string driveLetter)
	{
		string logicalDisk = driveLetter.Length > 2
			? driveLetter[..2]   // "C:\" → "C:"
			: driveLetter;

		// ── Step 1: logical disk → partitions ────────────────────────────
		var ldPairs = RunWithTimeout(
			() =>
			{
				var list = new List<(string Dep, string Ant)>();
				using var q = new ManagementObjectSearcher(
					"SELECT Dependent, Antecedent FROM Win32_LogicalDiskToPartition");
				foreach (ManagementObject item in q.Get())
				{
					string? dep = item["Dependent"]?.ToString();
					string? ant = item["Antecedent"]?.ToString();
					if (dep != null && ant != null) list.Add((dep, ant));
				}
				return list;
			},
			fallback: new List<(string, string)>(),
			timeout: TimeSpan.FromSeconds(2));

		// ── Step 2: partitions → physical disks ──────────────────────────
		var dpPairs = RunWithTimeout(
			() =>
			{
				var list = new List<(string Dep, string Ant)>();
				using var q = new ManagementObjectSearcher(
					"SELECT Dependent, Antecedent FROM Win32_DiskDriveToDiskPartition");
				foreach (ManagementObject item in q.Get())
				{
					string? dep = item["Dependent"]?.ToString();
					string? ant = item["Antecedent"]?.ToString();
					if (dep != null && ant != null) list.Add((dep, ant));
				}
				return list;
			},
			fallback: new List<(string, string)>(),
			timeout: TimeSpan.FromSeconds(2));

		// ── Step 3: match drive letter → partition → disk index ───────────
		foreach (var (dep, ant) in ldPairs)
		{
			if (!dep.Contains($"\"{logicalDisk}\"", StringComparison.OrdinalIgnoreCase))
				continue;

			string partitionId = ExtractPartitionId(ant);

			foreach (var (dpDep, dpAnt) in dpPairs)
			{
				if (!dpDep.Contains(partitionId, StringComparison.OrdinalIgnoreCase))
					continue;

				int idx = ParseDiskIndex(dpAnt);
				if (idx >= 0) return idx;
			}
		}

		return -1;
	}

	/// <summary>
	/// Primary detection: queries MSFT_PhysicalDisk in the Storage WMI namespace.
	///
	/// scope.Connect() is the single most common hang point — some OEM/driver
	/// combinations cause it to block indefinitely. It runs inside
	/// <see cref="RunWithTimeout{T}"/> so it is guaranteed to return.
	///
	/// Falls through to <see cref="FallbackWin32DiskDrive"/> when:
	///   • the namespace is unavailable (ManagementException)
	///   • MediaType comes back as 0 (Unspecified) — older Windows 10 builds
	///   • the query times out
	/// </summary>
	private static DiskKind QueryMsftPhysicalDisk(int diskIndex)
	{
		DiskKind result = RunWithTimeout(
			() =>
			{
				// scope.Connect() — most common WMI hang point
				var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
				scope.Connect();

				using var query = new ManagementObjectSearcher(
					scope,
					new ObjectQuery("SELECT MediaType, BusType FROM MSFT_PhysicalDisk"));

				foreach (ManagementObject disk in query.Get())
				{
					ushort mediaType = Convert.ToUInt16(disk["MediaType"]);
					ushort busType = Convert.ToUInt16(disk["BusType"]);

					// MediaType: 3 = HDD, 4 = SSD, 0 = Unspecified (fall through)
					if (mediaType == 3) return DiskKind.HDD;
					if (mediaType == 4)
						// BusType: 17 = NVMe, 11 = SATA, anything else → SATA-class
						return busType == 17 ? DiskKind.NvmeSSD : DiskKind.SataSSD;
				}

				return DiskKind.Unknown; // Unspecified or empty — try fallback
			},
			fallback: DiskKind.Unknown,
			timeout: TimeSpan.FromSeconds(2));

		// Unknown from MSFT_PhysicalDisk → try the older Win32_DiskDrive strings
		return result == DiskKind.Unknown
			? FallbackWin32DiskDrive(diskIndex)
			: result;
	}

	/// <summary>
	/// Legacy fallback using Win32_DiskDrive InterfaceType / MediaType strings.
	/// Covers older Windows versions and machines where the Storage namespace is absent.
	/// Also timeout-guarded to be safe.
	/// </summary>
	private static DiskKind FallbackWin32DiskDrive(int diskIndex)
	{
		return RunWithTimeout(
			() =>
			{
				using var query = new ManagementObjectSearcher(
					$"SELECT Index, MediaType, InterfaceType FROM Win32_DiskDrive WHERE Index = {diskIndex}");

				foreach (ManagementObject disk in query.Get())
				{
					string? mediaType = disk["MediaType"]?.ToString()?.ToLowerInvariant();
					string? interfaceType = disk["InterfaceType"]?.ToString()?.ToLowerInvariant();

					// "Fixed hard disk" → HDD; "External hard disk" is also fixed
					if (mediaType != null && mediaType.Contains("fixed"))
						return DiskKind.HDD;

					if (interfaceType != null)
					{
						if (interfaceType.Contains("nvme")) return DiskKind.NvmeSSD;
						if (interfaceType.Contains("scsi") ||
							interfaceType.Contains("sata") ||
							interfaceType.Contains("ide")) return DiskKind.SataSSD;
					}
				}

				return DiskKind.Unknown;
			},
			fallback: DiskKind.Unknown,
			timeout: TimeSpan.FromSeconds(2));
	}

	// ── Timeout helper ───────────────────────────────────────────────────────

	/// <summary>
	/// Runs <paramref name="work"/> on a thread-pool thread and blocks for up to
	/// <paramref name="timeout"/>. Returns <paramref name="fallback"/> if the work
	/// does not complete in time or throws any exception.
	///
	/// This is the single choke-point that prevents every WMI call in this class
	/// from hanging indefinitely on machines with broken WMI providers.
	/// </summary>
	private static T RunWithTimeout<T>(Func<T> work, T fallback, TimeSpan timeout)
	{
		try
		{
			var task = Task.Run(work);
			return task.Wait(timeout) ? task.Result : fallback;
		}
		catch
		{
			// Catches AggregateException from task.Result, timeout, or work() itself
			return fallback;
		}
	}

	// ── Parsing helpers ──────────────────────────────────────────────────────

	/// <summary>
	/// Extracts the bare partition id from a WMI object path.
	/// e.g. Win32_DiskPartition.DeviceID="Disk #0, Partition #1"
	///   →  "Disk #0, Partition #1"
	/// </summary>
	private static string ExtractPartitionId(string wmiPath)
	{
		int start = wmiPath.IndexOf('"');
		int end = wmiPath.LastIndexOf('"');
		return (start >= 0 && end > start)
			? wmiPath[(start + 1)..end]
			: wmiPath;
	}

	/// <summary>
	/// Parses the physical disk index from strings like:
	///   "PHYSICALDRIVE2"  |  "\\\\.\\PHYSICALDRIVE2"  |  WMI object paths
	/// </summary>
	private static int ParseDiskIndex(string raw)
	{
		const string token = "PHYSICALDRIVE";
		int pos = raw.IndexOf(token, StringComparison.OrdinalIgnoreCase);
		if (pos < 0) return -1;

		string suffix = raw[(pos + token.Length)..].Trim('"', '\\', ' ');
		int len = 0;
		while (len < suffix.Length && char.IsDigit(suffix[len])) len++;

		return len > 0 && int.TryParse(suffix[..len], out int idx) ? idx : -1;
	}
}
