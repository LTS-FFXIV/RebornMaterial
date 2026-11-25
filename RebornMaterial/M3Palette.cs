using System.Globalization;

namespace RebornMaterial;

public sealed class TonalPalette(float hue, float chroma)
{
	private readonly Dictionary<int, Vector4> _cache = [];

	public float Hue { get; } = hue;
	public float Chroma { get; } = chroma;

	public Vector4 this[float tone]
	{
		get
		{
			var key = (int)MathF.Round(tone * 10f);
			if (_cache.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var color = M3ColorMath.FromLch(Math.Clamp(tone, 0f, 100f), Chroma, Hue);
			_cache[key] = color;
			return color;
		}
	}
}

public sealed class CorePalette
{
	private const float BaseErrorHue = 25f;
	private const float MinimumErrorSeparation = 35f;

	private CorePalette(TonalPalette primary, TonalPalette secondary, TonalPalette tertiary, TonalPalette neutral, TonalPalette neutralVariant, TonalPalette error)
	{
		Primary = primary;
		Secondary = secondary;
		Tertiary = tertiary;
		Neutral = neutral;
		NeutralVariant = neutralVariant;
		Error = error;
	}

	public TonalPalette Primary { get; }
	public TonalPalette Secondary { get; }
	public TonalPalette Tertiary { get; }
	public TonalPalette Neutral { get; }
	public TonalPalette NeutralVariant { get; }
	public TonalPalette Error { get; }

	public static CorePalette FromSeed(Vector4 seed)
	{
		M3ColorMath.ToLch(seed, out _, out var chroma, out var hue);

		var primaryChroma = MathF.Max(chroma, 48f);
		return new CorePalette(
			primary: new TonalPalette(hue, primaryChroma),
			secondary: new TonalPalette(hue, MathF.Max(primaryChroma / 3f, 16f)),
			tertiary: new TonalPalette(M3ColorMath.WrapHue(hue + 60f), MathF.Max(primaryChroma / 2f, 24f)),
			neutral: new TonalPalette(hue, MathF.Min(chroma / 12f, 4f)),
			neutralVariant: new TonalPalette(hue, MathF.Min(chroma / 6f, 8f)),
			error: new TonalPalette(ResolveErrorHue(hue), 84f));
	}

	private static float ResolveErrorHue(float seedHue)
	{
		var separation = MathF.Abs(M3ColorMath.WrapHue(BaseErrorHue - seedHue + 180f) - 180f);
		return separation >= MinimumErrorSeparation
			? BaseErrorHue
			: M3ColorMath.WrapHue(seedHue - MinimumErrorSeparation);
	}
}

public sealed class M3Scheme
{
	private static readonly TonalPalette _warningPalette = new(75f, 80f);
	private static readonly TonalPalette _successPalette = new(145f, 50f);
	private static readonly TonalPalette _infoPalette = new(250f, 50f);

	private M3Scheme(CorePalette core, bool light)
	{
		var p = core.Primary;
		var s = core.Secondary;
		var t = core.Tertiary;
		var n = core.Neutral;
		var nv = core.NeutralVariant;
		var e = core.Error;

		IsLight = light;

		if (light)
		{
			Primary = p[40];
			OnPrimary = p[100];
			PrimaryContainer = p[90];
			OnPrimaryContainer = p[10];
			PrimaryFixedDim = p[80];
			InversePrimary = p[80];

			Secondary = s[40];
			OnSecondary = s[100];
			SecondaryContainer = s[90];
			OnSecondaryContainer = s[10];

			Tertiary = t[40];
			OnTertiary = t[100];
			TertiaryContainer = t[90];
			OnTertiaryContainer = t[10];

			Error = e[40];
			OnError = e[100];
			ErrorContainer = e[90];
			OnErrorContainer = e[10];

			Surface = n[98];
			SurfaceDim = n[87];
			SurfaceBright = n[98];
			OnSurface = n[10];
			SurfaceContainerLowest = n[100];
			SurfaceContainerLow = n[96];
			SurfaceContainer = n[94];
			SurfaceContainerHigh = n[92];
			SurfaceContainerHighest = n[90];
			OnSurfaceVariant = nv[30];
			InverseSurface = n[20];
			InverseOnSurface = n[95];

			Outline = nv[50];
			OutlineVariant = nv[80];
			Scrim = n[0];
			Shadow = n[0];

			Warning = _warningPalette[40];
			OnWarning = _warningPalette[100];
			WarningContainer = _warningPalette[90];
			OnWarningContainer = _warningPalette[10];
			Success = _successPalette[40];
			OnSuccess = _successPalette[100];
			SuccessContainer = _successPalette[90];
			OnSuccessContainer = _successPalette[10];
			Info = _infoPalette[40];
			OnInfo = _infoPalette[100];
			InfoContainer = _infoPalette[90];
			OnInfoContainer = _infoPalette[10];
		}
		else
		{
			Primary = p[80];
			OnPrimary = p[20];
			PrimaryContainer = p[30];
			OnPrimaryContainer = p[90];
			PrimaryFixedDim = p[70];
			InversePrimary = p[40];

			Secondary = s[80];
			OnSecondary = s[20];
			SecondaryContainer = s[30];
			OnSecondaryContainer = s[90];

			Tertiary = t[80];
			OnTertiary = t[20];
			TertiaryContainer = t[30];
			OnTertiaryContainer = t[90];

			Error = e[80];
			OnError = e[20];
			ErrorContainer = e[30];
			OnErrorContainer = e[90];

			Surface = n[6];
			SurfaceDim = n[6];
			SurfaceBright = n[24];
			OnSurface = n[90];
			SurfaceContainerLowest = n[4];
			SurfaceContainerLow = n[10];
			SurfaceContainer = n[12];
			SurfaceContainerHigh = n[17];
			SurfaceContainerHighest = n[22];
			OnSurfaceVariant = nv[80];
			InverseSurface = n[90];
			InverseOnSurface = n[20];

			Outline = nv[60];
			OutlineVariant = nv[30];
			Scrim = n[0];
			Shadow = n[0];

			Warning = _warningPalette[80];
			OnWarning = _warningPalette[20];
			WarningContainer = _warningPalette[30];
			OnWarningContainer = _warningPalette[90];
			Success = _successPalette[80];
			OnSuccess = _successPalette[20];
			SuccessContainer = _successPalette[30];
			OnSuccessContainer = _successPalette[90];
			Info = _infoPalette[80];
			OnInfo = _infoPalette[20];
			InfoContainer = _infoPalette[30];
			OnInfoContainer = _infoPalette[90];
		}
	}

	public bool IsLight { get; }

	public Vector4 Primary { get; }
	public Vector4 OnPrimary { get; }
	public Vector4 PrimaryContainer { get; }
	public Vector4 OnPrimaryContainer { get; }
	public Vector4 PrimaryFixedDim { get; }

	public Vector4 InversePrimary { get; }

	public Vector4 Secondary { get; }
	public Vector4 OnSecondary { get; }
	public Vector4 SecondaryContainer { get; }
	public Vector4 OnSecondaryContainer { get; }

	public Vector4 Tertiary { get; }
	public Vector4 OnTertiary { get; }
	public Vector4 TertiaryContainer { get; }
	public Vector4 OnTertiaryContainer { get; }

	public Vector4 Error { get; }
	public Vector4 OnError { get; }
	public Vector4 ErrorContainer { get; }
	public Vector4 OnErrorContainer { get; }

	public Vector4 Surface { get; }
	public Vector4 SurfaceDim { get; }
	public Vector4 SurfaceBright { get; }
	public Vector4 OnSurface { get; }
	public Vector4 SurfaceContainerLowest { get; }
	public Vector4 SurfaceContainerLow { get; }
	public Vector4 SurfaceContainer { get; }
	public Vector4 SurfaceContainerHigh { get; }
	public Vector4 SurfaceContainerHighest { get; }
	public Vector4 OnSurfaceVariant { get; }
	public Vector4 InverseSurface { get; }
	public Vector4 InverseOnSurface { get; }

	public Vector4 Outline { get; }
	public Vector4 OutlineVariant { get; }
	public Vector4 Scrim { get; }
	public Vector4 Shadow { get; }

	public Vector4 Warning { get; }
	public Vector4 OnWarning { get; }
	public Vector4 WarningContainer { get; }
	public Vector4 OnWarningContainer { get; }
	public Vector4 Success { get; }
	public Vector4 OnSuccess { get; }
	public Vector4 SuccessContainer { get; }
	public Vector4 OnSuccessContainer { get; }
	public Vector4 Info { get; }
	public Vector4 OnInfo { get; }
	public Vector4 InfoContainer { get; }
	public Vector4 OnInfoContainer { get; }

	public static M3Scheme FromSeed(Vector4 seed)
	{
		return new M3Scheme(CorePalette.FromSeed(seed), false);
	}

	public static M3Scheme FromSeed(Vector4 seed, bool light)
	{
		return new M3Scheme(CorePalette.FromSeed(seed), light);
	}
}

public static class M3ColorMath
{
	private const float Epsilon = 216f / 24389f;
	private const float Kappa = 24389f / 27f;
	private const float ChromaScale = 300f;

	public static float WrapHue(float hue)
	{
		hue %= 360f;
		return hue < 0f ? hue + 360f : hue;
	}

	public static Vector4 FromRgb(uint rgb)
	{
		return new Vector4(
			((rgb >> 16) & 0xFF) / 255f,
			((rgb >> 8) & 0xFF) / 255f,
			(rgb & 0xFF) / 255f,
			1f);
	}

	public static string ToHex(Vector4 color, bool alpha = false)
	{
		static int Channel(float value)
		{
			return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
		}

		return alpha
			? $"#{Channel(color.X):X2}{Channel(color.Y):X2}{Channel(color.Z):X2}{Channel(color.W):X2}"
			: $"#{Channel(color.X):X2}{Channel(color.Y):X2}{Channel(color.Z):X2}";
	}

	public static bool TryParseHex(ReadOnlySpan<char> text, out Vector4 color)
	{
		return TryParseHex(text, out color, out _);
	}

	public static bool TryParseHex(ReadOnlySpan<char> text, out Vector4 color, out int digits)
	{
		color = default;
		digits = 0;
		text = text.Trim();
		if (!text.IsEmpty && text[0] == '#')
		{
			text = text[1..];
		}
		else if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
		{
			text = text[2..];
		}

		if (text.Length is not (3 or 4 or 6 or 8) || !uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
		{
			return false;
		}

		var (r, g, b, a) = text.Length switch
		{
			3 => (((value >> 8) & 0xF) * 17, ((value >> 4) & 0xF) * 17, (value & 0xF) * 17, 255u),
			4 => (((value >> 12) & 0xF) * 17, ((value >> 8) & 0xF) * 17, ((value >> 4) & 0xF) * 17, (value & 0xF) * 17),
			6 => ((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF, 255u),
			_ => ((value >> 24) & 0xFF, (value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF),
		};

		digits = text.Length;
		color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
		return true;
	}

	public static void ToLch(Vector4 color, out float lightness, out float chroma, out float hue)
	{
		ToLab(color, out lightness, out var a, out var b);
		chroma = MathF.Sqrt((a * a) + (b * b));
		hue = chroma < 0.0001f ? 0f : WrapHue(MathF.Atan2(b, a) * 180f / MathF.PI);
	}

	public static void ToLab(Vector4 color, out float lightness, out float a, out float b)
	{
		var r = Linearize(color.X);
		var g = Linearize(color.Y);
		var bl = Linearize(color.Z);

		var l = (0.4122214708f * r) + (0.5363325363f * g) + (0.0514459929f * bl);
		var m = (0.2119034982f * r) + (0.6806995451f * g) + (0.1073969566f * bl);
		var s = (0.0883024619f * r) + (0.2817188376f * g) + (0.6299787005f * bl);

		var lRoot = MathF.Cbrt(l);
		var mRoot = MathF.Cbrt(m);
		var sRoot = MathF.Cbrt(s);

		var okL = (0.2104542553f * lRoot) + (0.7936177850f * mRoot) - (0.0040720468f * sRoot);
		var okA = (1.9779984951f * lRoot) - (2.4285922050f * mRoot) + (0.4505937099f * sRoot);
		var okB = (0.0259040371f * lRoot) + (0.7827717662f * mRoot) - (0.8086757660f * sRoot);

		lightness = OkLToTone(okL);
		a = okA * ChromaScale;
		b = okB * ChromaScale;
	}

	public static Vector4 FromLch(float lightness, float chroma, float hue)
	{
		if (chroma < 0.0001f)
		{
			return FromLab(lightness, 0f, 0f);
		}

		var radians = hue * MathF.PI / 180f;
		var cos = MathF.Cos(radians);
		var sin = MathF.Sin(radians);

		var full = FromLabRaw(lightness, chroma * cos, chroma * sin, out var inGamut);
		if (inGamut)
		{
			return full;
		}

		var low = 0f;
		var high = chroma;
		var best = FromLab(lightness, 0f, 0f);

		for (var i = 0; i < 12; i++)
		{
			var mid = (low + high) * 0.5f;
			var candidate = FromLabRaw(lightness, mid * cos, mid * sin, out inGamut);
			if (inGamut)
			{
				best = candidate;
				low = mid;
			}
			else
			{
				high = mid;
			}
		}

		return best;
	}

	public static Vector4 FromLab(float lightness, float a, float b)
	{
		return FromLabRaw(lightness, a, b, out _);
	}

	public static Vector4 Mix(Vector4 from, Vector4 to, float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0f)
		{
			return from;
		}

		if (amount >= 1f)
		{
			return to;
		}

		return new Vector4(
			Delinearize(float.Lerp(Linearize(from.X), Linearize(to.X), amount)),
			Delinearize(float.Lerp(Linearize(from.Y), Linearize(to.Y), amount)),
			Delinearize(float.Lerp(Linearize(from.Z), Linearize(to.Z), amount)),
			float.Lerp(from.W, to.W, amount));
	}

	private static Vector4 FromLabRaw(float lightness, float a, float b, out bool inGamut)
	{
		var okL = ToneToOkL(lightness);
		var okA = a / ChromaScale;
		var okB = b / ChromaScale;

		var lRoot = okL + (0.3963377774f * okA) + (0.2158037573f * okB);
		var mRoot = okL - (0.1055613458f * okA) - (0.0638541728f * okB);
		var sRoot = okL - (0.0894841775f * okA) - (1.2914855480f * okB);

		var l = lRoot * lRoot * lRoot;
		var m = mRoot * mRoot * mRoot;
		var s = sRoot * sRoot * sRoot;

		var r = (+4.0767416621f * l) - (3.3077115913f * m) + (0.2309699292f * s);
		var g = (-1.2684380046f * l) + (2.6097574011f * m) - (0.3413193965f * s);
		var bl = (-0.0041960863f * l) - (0.7034186147f * m) + (1.7076147010f * s);

		const float tolerance = 0.0005f;
		inGamut = r >= -tolerance && r <= 1f + tolerance
			&& g >= -tolerance && g <= 1f + tolerance
			&& bl >= -tolerance && bl <= 1f + tolerance;

		return new Vector4(
			Math.Clamp(Delinearize(r), 0f, 1f),
			Math.Clamp(Delinearize(g), 0f, 1f),
			Math.Clamp(Delinearize(bl), 0f, 1f),
			1f);
	}

	private static float ToneToOkL(float tone)
	{
		var fy = (Math.Clamp(tone, 0f, 100f) + 16f) / 116f;
		var y = LabFInverse(fy);
		return MathF.Cbrt(MathF.Max(0f, y));
	}

	private static float OkLToTone(float okL)
	{
		var clamped = MathF.Max(0f, okL);
		var y = clamped * clamped * clamped;
		return (116f * LabF(y)) - 16f;
	}

	private static float LabF(float t)
	{
		return t > Epsilon ? MathF.Cbrt(t) : ((Kappa * t) + 16f) / 116f;
	}

	private static float LabFInverse(float t)
	{
		var cubed = t * t * t;
		return cubed > Epsilon ? cubed : ((116f * t) - 16f) / Kappa;
	}

	private static float Linearize(float channel)
	{
		return channel <= 0.04045f ? channel / 12.92f : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
	}

	private static float Delinearize(float channel)
	{
		return channel <= 0.0031308f ? channel * 12.92f : (1.055f * MathF.Pow(channel, 1f / 2.4f)) - 0.055f;
	}
}
