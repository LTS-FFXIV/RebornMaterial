using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private const int MaxSliderTexts = 512;

	private static readonly Dictionary<uint, (string Format, float Low, float High, bool Range, string Text)> _sliderReadouts = [];
	private static readonly Dictionary<uint, (int Low, int High, bool Range, string Text)> _sliderIntReadouts = [];
	private static readonly Dictionary<(string Format, float Min, float Max, bool Range), (string Low, string High)> _sliderGutterTexts = [];
	private static readonly Dictionary<(int Min, int Max, bool Range), (string Low, string High)> _sliderIntGutterTexts = [];

	private static void ResetSliders()
	{
		_sliderReadouts.Clear();
		_sliderIntReadouts.Clear();
		_sliderGutterTexts.Clear();
		_sliderIntGutterTexts.Clear();
	}

	private static float SliderHandleRadius => 10f * M3.Scale;

	private static float SliderGrabSize => MathF.Max(0f, (SliderHandleRadius * 2f) - 4f);

	private static string SliderReadout(uint id, string format, float low, float high, bool range)
	{
		if (_sliderReadouts.TryGetValue(id, out var shown) && shown.Range == range && shown.Low.Equals(low) && shown.High.Equals(high) && shown.Format == format)
		{
			return shown.Text;
		}

		if (_sliderReadouts.Count >= MaxSliderTexts)
		{
			_sliderReadouts.Clear();
		}

		var text = range ? RangeReadout(format, low, high) : FormatValue(format, low);
		_sliderReadouts[id] = (format, low, high, range, text);
		return text;
	}

	private static string SliderReadout(uint id, int value)
	{
		return SliderIntReadout(id, value, value, false);
	}

	private static string SliderIntReadout(uint id, int low, int high, bool range)
	{
		if (_sliderIntReadouts.TryGetValue(id, out var shown) && shown.Range == range && shown.Low == low && shown.High == high)
		{
			return shown.Text;
		}

		if (_sliderIntReadouts.Count >= MaxSliderTexts)
		{
			_sliderIntReadouts.Clear();
		}

		var text = range ? $"{low} - {high}" : low.ToString();
		_sliderIntReadouts[id] = (low, high, range, text);
		return text;
	}

	private static float SliderGutter(string format, float min, float max, bool range)
	{
		var key = (format, min, max, range);
		if (!_sliderGutterTexts.TryGetValue(key, out var ends))
		{
			if (_sliderGutterTexts.Count >= MaxSliderTexts)
			{
				_sliderGutterTexts.Clear();
			}

			ends = range
				? (RangeReadout(format, min, min), RangeReadout(format, max, max))
				: (FormatValue(format, min), FormatValue(format, max));
			_sliderGutterTexts[key] = ends;
		}

		return MathF.Max(SliderValueGutter(ends.Low), SliderValueGutter(ends.High));
	}

	private static float SliderIntGutter(int min, int max, bool range)
	{
		var key = (min, max, range);
		if (!_sliderIntGutterTexts.TryGetValue(key, out var ends))
		{
			if (_sliderIntGutterTexts.Count >= MaxSliderTexts)
			{
				_sliderIntGutterTexts.Clear();
			}

			ends = range
				? ($"{min} - {min}", $"{max} - {max}")
				: (min.ToString(), max.ToString());
			_sliderIntGutterTexts[key] = ends;
		}

		return MathF.Max(SliderValueGutter(ends.Low), SliderValueGutter(ends.High));
	}

	private static ImRaii.ColorDisposable PushInvisibleSliderChrome(bool condition)
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrab, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrabActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f), condition);
	}

	public static bool Slider(string id, ref float value, float min, float max, string displayValue, float width, bool logarithmic = false, bool enabled = true, float step = 0f)
	{
		return SliderCore(id, ref value, min, max, displayValue, null, width, logarithmic, enabled, step, true);
	}

	public static bool SliderInt(string id, ref int value, int min, int max, string displayValue, float width, bool logarithmic = false, bool enabled = true, int step = 0)
	{
		return SliderIntCore(id, ref value, min, max, displayValue, width, logarithmic, enabled, step, true);
	}

	internal static bool IsValidFloatFormat(string? format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return false;
		}

		var percent = -1;
		for (var i = 0; i < format.Length - 1; i++)
		{
			if (format[i] != '%')
			{
				continue;
			}

			if (format[i + 1] == '%')
			{
				i++;
				continue;
			}

			percent = i;
			break;
		}

		if (percent < 0)
		{
			return false;
		}

		var cursor = percent + 1;
		while (cursor < format.Length && format[cursor] is '-' or '+' or ' ' or '#' or '0')
		{
			cursor++;
		}

		while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
		{
			cursor++;
		}

		if (cursor < format.Length && format[cursor] == '.')
		{
			cursor++;
			while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
			{
				cursor++;
			}
		}

		while (cursor < format.Length && format[cursor] is 'l' or 'h')
		{
			cursor++;
		}

		return cursor < format.Length && format[cursor] is 'f' or 'F' or 'g' or 'G' or 'e' or 'E';
	}

	private static bool SliderCore(string id, ref float value, float min, float max, string? displayValue, string? format, float width, bool logarithmic, bool enabled, float step, bool reserveReadout)
	{
		var scale = M3.Scale;
		var key = ImGui.GetID(id);
		var editing = ImGuiP.TempInputIsActive(key);
		var flags = ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat | (logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None);
		var before = value;
		var sliderFormat = IsValidFloatFormat(format) ? format! : "%.2f";
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale)
			.Push(ImGuiStyleVar.GrabMinSize, SliderGrabSize))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderFloat(id, ref value, min, max, sliderFormat, flags);
		}

		if (editing)
		{
			M3.CaptureEscape();
		}

		if (changed && step > 0f)
		{
			var raw = value;
			value = Snap(raw, min, max, step);

			if (value == before && raw != before && !editing && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				value = Snap(before + MathF.CopySign(step, raw - before), min, max, step);
			}

			changed = value != before;
		}

		var text = displayValue ?? SliderReadout(key, format ?? string.Empty, value, value, false);
		var gutter = reserveReadout && !string.IsNullOrEmpty(text)
			? (displayValue == null && !string.IsNullOrEmpty(format) ? SliderGutter(format, min, max, false) : SliderValueGutter(text))
			: 0f;
		DrawSliderVisual(value, min, max, text, M3.Scheme, scale, editing, logarithmic ? 0.01f : 0f, enabled, -1f, gutter);
		return changed;
	}

	private static bool SliderIntCore(string id, ref int value, int min, int max, string? displayValue, float width, bool logarithmic, bool enabled, int step, bool reserveReadout)
	{
		var scale = M3.Scale;
		var key = ImGui.GetID(id);
		var editing = ImGuiP.TempInputIsActive(key);
		var flags = ImGuiSliderFlags.AlwaysClamp | (logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None);
		var before = value;
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale)
			.Push(ImGuiStyleVar.GrabMinSize, SliderGrabSize))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderInt(id, ref value, min, max, "%d", flags);
		}

		if (editing)
		{
			M3.CaptureEscape();
		}

		if (changed && step > 1)
		{
			var raw = value;
			value = Snap(raw, min, max, step);
			if (value == before && raw != before && !editing && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				value = Snap(before + (raw > before ? (long)step : -step), min, max, step);
			}

			changed = value != before;
		}

		var text = displayValue ?? SliderReadout(key, value);
		var gutter = reserveReadout && !string.IsNullOrEmpty(text)
			? (displayValue == null ? SliderIntGutter(min, max, false) : SliderValueGutter(text))
			: 0f;
		DrawSliderVisual(value, min, max, text, M3.Scheme, scale, editing, logarithmic ? 0.1f : 0f, enabled, MathF.Abs((float)max - min), gutter);
		return changed;
	}

	private static float Snap(float value, float min, float max, float step)
	{
		return Math.Clamp(min + (MathF.Round((value - min) / step) * step), MathF.Min(min, max), MathF.Max(min, max));
	}

	private static int Snap(long value, int min, int max, int step)
	{
		var snapped = min + ((long)Math.Round((value - (double)min) / step) * step);
		return (int)Math.Clamp(snapped, Math.Min(min, max), Math.Max(min, max));
	}

	internal static float LogFraction(float value, float min, float max, float epsilon, float usableWidth)
	{
		if (min == max || !float.IsFinite(value))
		{
			return 0f;
		}

		var flipped = max < min;
		if (flipped)
		{
			(min, max) = (max, min);
		}

		var clamped = Math.Clamp(value, min, max);
		var minFudged = MathF.Abs(min) < epsilon ? (min < 0f ? -epsilon : epsilon) : min;
		var maxFudged = MathF.Abs(max) < epsilon ? (max < 0f ? -epsilon : epsilon) : max;
		if (min == 0f && max < 0f)
		{
			minFudged = -epsilon;
		}
		else if (max == 0f && min < 0f)
		{
			maxFudged = -epsilon;
		}

		float result;
		if (clamped <= minFudged)
		{
			result = 0f;
		}
		else if (clamped >= maxFudged)
		{
			result = 1f;
		}
		else if (min * max < 0f)
		{
			var zero = -min / (max - min);
			var deadzone = ImGui.GetStyle().LogSliderDeadzone * 0.5f / MathF.Max(usableWidth, 1f);
			var left = zero - deadzone;
			var right = zero + deadzone;
			if (clamped == 0f)
			{
				result = zero;
			}
			else if (clamped < 0f)
			{
				var denom = MathF.Log(-minFudged / epsilon);
				result = MathF.Abs(denom) > 1e-6f ? (1f - (MathF.Log(-clamped / epsilon) / denom)) * left : 0f;
			}
			else
			{
				var denom = MathF.Log(maxFudged / epsilon);
				result = MathF.Abs(denom) > 1e-6f ? right + (MathF.Log(clamped / epsilon) / denom * (1f - right)) : 1f;
			}
		}
		else if (min < 0f || max < 0f)
		{
			var denom = MathF.Log(-minFudged / -maxFudged);
			result = MathF.Abs(denom) > 1e-6f ? 1f - (MathF.Log(-clamped / -maxFudged) / denom) : 0f;
		}
		else
		{
			var denom = MathF.Log(maxFudged / minFudged);
			result = MathF.Abs(denom) > 1e-6f ? MathF.Log(clamped / minFudged) / denom : 0f;
		}

		result = float.IsFinite(result) ? Math.Clamp(result, 0f, 1f) : 0f;
		return flipped ? 1f - result : result;
	}

	private static void DrawSliderVisual(float value, float min, float max, string displayValue, M3Scheme s, float scale, bool editing, float logEpsilon, bool enabled, float intRange, float readoutGutter)
	{
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var hasReadout = !string.IsNullOrEmpty(displayValue);
		var textSize = hasReadout ? ImGui.CalcTextSize(displayValue) : Vector2.Zero;
		if (hasReadout)
		{
			drawList.AddText(new Vector2(itemMax.X + (10f * scale), centerY - (textSize.Y * 0.5f)),
				M3.U32(s.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent), displayValue);
		}

		if (!editing)
		{
			var hovered = IsHovered(enabled);
			var active = enabled && ImGui.IsItemActive();

			var trackHeight = 4f * scale;
			var handleRadius = SliderHandleRadius;

			var sliderSize = itemMax.X - itemMin.X - 4f;
			var grabSize = SliderGrabSize;
			if (intRange >= 0f)
			{
				grabSize = MathF.Max(sliderSize / (intRange + 1f), grabSize);
			}

			grabSize = MathF.Max(0f, MathF.Min(grabSize, sliderSize));
			var trackLeft = itemMin.X + 2f + (grabSize * 0.5f);
			var trackRight = itemMax.X - 2f - (grabSize * 0.5f);
			var fraction = !float.IsFinite(value) ? 0f
				: logEpsilon > 0f ? LogFraction(value, min, max, logEpsilon, trackRight - trackLeft)
				: max != min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
			var handleX = float.Lerp(trackLeft, trackRight, fraction);
			var accent = enabled ? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);

			drawList.AddRectFilled(
				new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
				new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
				enabled ? M3.U32(s.SurfaceContainerHighest) : M3.U32(s.OnSurface, M3.DisabledContainer), trackHeight);

			if (handleX > trackLeft)
			{
				drawList.AddRectFilled(
					new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
					new Vector2(handleX, centerY + (trackHeight * 0.5f)),
					M3.U32(accent), trackHeight);
			}

			if (hovered || active)
			{
				drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius + (8f * scale),
					M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
			}

			drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius, M3.U32(accent), 24);
			drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius * 0.45f, M3.U32(enabled ? s.OnPrimary : s.Surface, 0.9f), 16);

			if (hasReadout && active)
			{
				var padding = new Vector2(8f, 4f) * scale;
				var bubbleMin = new Vector2(handleX - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
				var bubbleMax = bubbleMin + textSize + (padding * 2f);
				var foreground = ImGui.GetForegroundDrawList();
				foreground.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
				foreground.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), displayValue);
			}
		}

		if (readoutGutter > 0f)
		{
			ReserveReadoutSpace(itemMax.X + readoutGutter);
		}
	}

	private static void ReserveReadoutSpace(float rightScreenX)
	{
		var window = ImGuiP.GetCurrentWindow();
		window.DC.CursorPosPrevLine.X = MathF.Max(window.DC.CursorPosPrevLine.X, rightScreenX);
		window.DC.CursorMaxPos.X = MathF.Max(window.DC.CursorMaxPos.X, rightScreenX);
	}

	public static float SliderValueGutter(string longestValue)
	{
		return ImGui.CalcTextSize(longestValue).X + (14f * M3.Scale);
	}
}
