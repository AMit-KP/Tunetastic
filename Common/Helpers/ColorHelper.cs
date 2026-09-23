using Windows.UI;

namespace Tunetastic.Common.Helpers;


/// <summary>
/// Provides color manipulation and analysis utilities.
/// </summary>
public static class ColorHelper
{
	// ---------- Public API ----------

	/// <summary>
	/// Determines whether two colors are visually similar based on the CIEDE2000 color difference formula.
	/// </summary>
	/// <param name="c1">The first color to compare.</param>
	/// <param name="c2">The second color to compare.</param>
	/// <param name="deltaEThreshold">The threshold for considering colors similar. Default is 10.0.</param>
	/// <returns>True if the colors are visually similar; otherwise, false.</returns>
	public static bool AreColorsTooSimilar(Color c1, Color c2, double deltaEThreshold = 10.0)
	{
		return DeltaE2000(c1, c2) < deltaEThreshold;
	}

	/// <summary>
	/// Determines whether the background color is dark or light for proper text contrast.
	/// </summary>
	/// <param name="background">The background color to analyze.</param>
	/// <returns>OverlayTheme.Dark if the background is dark, OverlayTheme.Light if it's light.</returns>
	public static OverlayTheme IsItDarkOrLight(Color background)
	{
		double bgLuminance = RelativeLuminance(background);

		double contrastWithWhite = ContrastRatio(bgLuminance, 1.0);
		double contrastWithBlack = ContrastRatio(bgLuminance, 0.0);

		return contrastWithWhite >= contrastWithBlack ? OverlayTheme.Dark : OverlayTheme.Light;
	}

	// WCAG contrast ratio formula: (L1 + 0.05) / (L2 + 0.05), L1 = lighter luminance
	/// <summary>
	/// Calculates the WCAG contrast ratio between two relative luminance values.
	/// </summary>
	/// <param name="l1">The first luminance value (0-1).</param>
	/// <param name="l2">The second luminance value (0-1).</param>
	/// <returns>The contrast ratio between the two luminance values.</returns>
	private static double ContrastRatio(double l1, double l2)
	{
		double lighter = Math.Max(l1, l2);
		double darker = Math.Min(l1, l2);
		return (lighter + 0.05) / (darker + 0.05);
	}

	// WCAG relative luminance (0 = black, 1 = white), gamma-corrected
	/// <summary>
	/// Calculates the relative luminance of a color according to WCAG 2.0 standards.
	/// </summary>
	/// <param name="c">The color to calculate luminance for.</param>
	/// <returns>The relative luminance value (0-1).</returns>
	private static double RelativeLuminance(Color c)
	{
		double r = InverseGamma(c.R / 255.0);
		double g = InverseGamma(c.G / 255.0);
		double b = InverseGamma(c.B / 255.0);
		return 0.2126 * r + 0.7152 * g + 0.0722 * b;
	}

	// ---------- RGB -> XYZ -> Lab ----------

	private struct Lab { public double L, A, B; }

	/// <summary>
	/// Converts an RGB color to CIELAB color space.
	/// </summary>
	/// <param name="c">The RGB color to convert.</param>
	/// <returns>The corresponding CIELAB color values.</returns>
	private static Lab RgbToLab(Color c)
	{
		double r = InverseGamma(c.R / 255.0);
		double g = InverseGamma(c.G / 255.0);
		double b = InverseGamma(c.B / 255.0);

		double x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
		double y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
		double z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

		const double Xn = 0.95047, Yn = 1.00000, Zn = 1.08883;
		double fx = LabF(x / Xn);
		double fy = LabF(y / Yn);
		double fz = LabF(z / Zn);

		return new Lab
		{
			L = 116 * fy - 16,
			A = 500 * (fx - fy),
			B = 200 * (fy - fz)
		};
	}

	/// <summary>
	/// Applies the inverse gamma correction to a color component.
	/// </summary>
	/// <param name="c">The color component value (0-1).</param>
	/// <returns>The gamma-corrected value.</returns>
	private static double InverseGamma(double c)
	{
		return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
	}

	/// <summary>
	/// Calculates the Lab F function used in CIELAB color space conversions.
	/// </summary>
	/// <param name="t">The input value.</param>
	/// <returns>The calculated F value.</returns>
	private static double LabF(double t)
	{
		const double delta = 6.0 / 29.0;
		return t > delta * delta * delta
			? Math.Cbrt(t)
			: t / (3 * delta * delta) + 4.0 / 29.0;
	}

	// ---------- CIEDE2000 (private — internal implementation detail) ----------

	/// <summary>
	/// Calculates the CIEDE2000 color difference between two colors.
	/// </summary>
	/// <param name="c1">The first color.</param>
	/// <param name="c2">The second color.</param>
	/// <returns>The color difference value (Delta E).</returns>
	private static double DeltaE2000(Color c1, Color c2)
	{
		var lab1 = RgbToLab(c1);
		var lab2 = RgbToLab(c2);
		return CIEDE2000(lab1, lab2);
	}

	/// <summary>
	/// Implements the CIEDE2000 color difference formula.
	/// </summary>
	/// <param name="lab1">The first CIELAB color.</param>
	/// <param name="lab2">The second CIELAB color.</param>
	/// <returns>The CIEDE2000 color difference value.</returns>
	private static double CIEDE2000(Lab lab1, Lab lab2)
	{
		double L1 = lab1.L, a1 = lab1.A, b1 = lab1.B;
		double L2 = lab2.L, a2 = lab2.A, b2 = lab2.B;

		double avgL = (L1 + L2) / 2.0;
		double C1 = Math.Sqrt(a1 * a1 + b1 * b1);
		double C2 = Math.Sqrt(a2 * a2 + b2 * b2);
		double avgC = (C1 + C2) / 2.0;

		double G = 0.5 * (1 - Math.Sqrt(Math.Pow(avgC, 7) / (Math.Pow(avgC, 7) + Math.Pow(25.0, 7))));
		double a1p = a1 * (1 + G);
		double a2p = a2 * (1 + G);

		double C1p = Math.Sqrt(a1p * a1p + b1 * b1);
		double C2p = Math.Sqrt(a2p * a2p + b2 * b2);
		double avgCp = (C1p + C2p) / 2.0;

		double h1p = Math.Atan2(b1, a1p) * 180.0 / Math.PI; if (h1p < 0) h1p += 360;
		double h2p = Math.Atan2(b2, a2p) * 180.0 / Math.PI; if (h2p < 0) h2p += 360;

		double deltahp;
		if (C1p * C2p == 0) deltahp = 0;
		else if (Math.Abs(h1p - h2p) <= 180) deltahp = h2p - h1p;
		else if (h2p <= h1p) deltahp = h2p - h1p + 360;
		else deltahp = h2p - h1p - 360;

		double deltaLp = L2 - L1;
		double deltaCp = C2p - C1p;
		double deltaHp = 2 * Math.Sqrt(C1p * C2p) * Math.Sin(deltahp * Math.PI / 360.0);

		double avgHp;
		if (C1p * C2p == 0) avgHp = h1p + h2p;
		else if (Math.Abs(h1p - h2p) <= 180) avgHp = (h1p + h2p) / 2.0;
		else if (h1p + h2p < 360) avgHp = (h1p + h2p + 360) / 2.0;
		else avgHp = (h1p + h2p - 360) / 2.0;

		double T = 1 - 0.17 * Math.Cos((avgHp - 30) * Math.PI / 180.0)
					 + 0.24 * Math.Cos((2 * avgHp) * Math.PI / 180.0)
					 + 0.32 * Math.Cos((3 * avgHp + 6) * Math.PI / 180.0)
					 - 0.20 * Math.Cos((4 * avgHp - 63) * Math.PI / 180.0);

		double deltaTheta = 30 * Math.Exp(-Math.Pow((avgHp - 275) / 25.0, 2));
		double Rc = 2 * Math.Sqrt(Math.Pow(avgCp, 7) / (Math.Pow(avgCp, 7) + Math.Pow(25.0, 7)));
		double Sl = 1 + (0.015 * Math.Pow(avgL - 50, 2)) / Math.Sqrt(20 + Math.Pow(avgL - 50, 2));
		double Sc = 1 + 0.045 * avgCp;
		double Sh = 1 + 0.015 * avgCp * T;
		double Rt = -Math.Sin(2 * deltaTheta * Math.PI / 180.0) * Rc;

		double kL = 1, kC = 1, kH = 1;

		double termL = deltaLp / (kL * Sl);
		double termC = deltaCp / (kC * Sc);
		double termH = deltaHp / (kH * Sh);

		return Math.Sqrt(termL * termL + termC * termC + termH * termH + Rt * termC * termH);
	}
}
