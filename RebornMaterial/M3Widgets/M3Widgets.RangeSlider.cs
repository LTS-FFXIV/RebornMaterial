using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static readonly M3FrameCache<int> _rangeThumb = new(64);
	private static readonly M3FrameCache<int> _rangeFocusThumb = new(64);
	private static uint _rangeNavActive;

	private static void ResetRange()
	{
		_rangeThumb.Clear();
		_rangeFocusThumb.Clear();
		_rangeNavActive = 0;
	}

	public static bool RangeSlider(string id, ref float low, ref float high, float min, float max, float width, string format = "%.2f", bool enabled = true, float step = 0f, float minSpan = 0f, string? displayValue = null)
	{
		return RangeCore(id, ref low, ref high, min, max, width, format, false, enabled, step, minSpan, displayValue, true);
	}

	public static bool RangeSliderInt(string id, ref int low, ref int high, int min, int max, float width, bool enabled = true, int step = 0, int minSpan = 0, string? displayValue = null)
	{
		return RangeSliderIntCore(id, ref low, ref high, min, max, width, enabled, step, minSpan, displayValue, true);
	}

	private static bool RangeSliderIntCore(string id, ref int low, ref int high, int min, int max, float width, bool enabled, int step, int minSpan, string? displayValue, bool reserveReadout)
	{
		float lowValue = low;
		float highValue = high;
		if (!RangeCore(id, ref lowValue, ref highValue, min, max, width, string.Empty, true, enabled, step, minSpan, displayValue, reserveReadout))
		{
			return false;
		}

		low = (int)MathF.Round(lowValue);
		high = (int)MathF.Round(highValue);
		return true;
	}

	public static bool RowRange(string label, ref float low, ref float high, float min, float max, string format, string? supporting = null, bool enabled = true, float step = 0f, float minSpan = 0f)
	{
		var (display, id) = RowParts(label, "_range");
		var trackWidth = 150f * M3.Scale;
		var controlSize = new Vector2(trackWidth + SliderGutter(format, min, max, true), ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = RangeCore(id, ref low, ref high, min, max, trackWidth, format, false, enabled, step, minSpan, null, false);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowRangeInt(string label, ref int low, ref int high, int min, int max, string? supporting = null, bool enabled = true, int step = 0, int minSpan = 0)
	{
		var (display, id) = RowParts(label, "_range");
		var trackWidth = 150f * M3.Scale;
		var gutter = SliderIntGutter(min, max, true);
		var controlSize = new Vector2(trackWidth + gutter, ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = RangeSliderIntCore(id, ref low, ref high, min, max, trackWidth, enabled, step, minSpan, null, false);
		M3SettingRow.End(row);
		return changed;
	}

	private static string RangeReadout(string format, float low, float high)
	{
		return string.Concat(FormatValue(format, low), " - ", FormatValue(format, high));
	}

	private static float RangeHandleX(float value, float min, float range, float trackLeft, float trackRight)
	{
		return float.Lerp(trackLeft, trackRight, range > 0f ? Math.Clamp((value - min) / range, 0f, 1f) : 0f);
	}

	private static int NearestThumb(float mouseX, float lowX, float highX, bool lowAtMin)
	{
		var toLow = MathF.Abs(mouseX - lowX);
		var toHigh = MathF.Abs(mouseX - highX);
		if (toLow != toHigh)
		{
			return toLow < toHigh ? 0 : 1;
		}

		return mouseX > highX || (mouseX == highX && lowAtMin) ? 1 : 0;
	}

	private static float SnapRangeBound(float value, float min, float max, float step, bool integer, bool floorToLimit)
	{
		if (integer)
		{
			if (step > 1f)
			{
				var steps = (value - min) / step;
				var index = floorToLimit ? MathF.Floor(steps) : MathF.Ceiling(steps);
				return Math.Clamp(min + (index * step), min, max);
			}

			return Math.Clamp(floorToLimit ? MathF.Floor(value) : MathF.Ceiling(value), min, max);
		}

		if (step > 0f)
		{
			var steps = (value - min) / step;
			var index = floorToLimit ? MathF.Floor(steps) : MathF.Ceiling(steps);
			return Math.Clamp(min + (index * step), min, max);
		}

		return Math.Clamp(value, min, max);
	}

	private static bool RangeCore(string id, ref float low, ref float high, float min, float max, float width, string format, bool integer, bool enabled, float step, float minSpan, string? displayValue, bool reserveReadout)
	{
		var scale = M3.Scale;
		var s = M3.Scheme;
		var height = ImGui.GetTextLineHeight() + (20f * scale);
		if (max < min)
		{
			(min, max) = (max, min);
		}

		minSpan = MathF.Max(minSpan, 0f);
		var safeLow = float.IsFinite(low) ? low : min;
		var safeHigh = float.IsFinite(high) ? high : max;
		var lo = Math.Clamp(MathF.Min(safeLow, safeHigh), min, max);
		var hi = Math.Clamp(MathF.Max(safeLow, safeHigh), min, max);

		bool clicked;
		using (ImRaii.PushStyle(ImGuiStyleVar.DisabledAlpha, 1f, !enabled))
		using (ImRaii.Disabled(!enabled))
		{
			clicked = ImGui.InvisibleButton(id, new Vector2(MathF.Max(width, 1f), height));
		}

		var key = ImGuiP.GetItemID();
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var trackHeight = 4f * scale;
		var handleRadius = 10f * scale;
		var trackLeft = itemMin.X + handleRadius;
		var trackRight = itemMax.X - handleRadius;
		var range = max - min;
		var mouseX = ImGui.GetIO().MousePos.X;

		var lowX = RangeHandleX(lo, min, range, trackLeft, trackRight);
		var highX = RangeHandleX(hi, min, range, trackLeft, trackRight);

		if (enabled && ImGui.IsItemActivated() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
		{
			if (_rangeNavActive == key)
			{
				_rangeNavActive = 0;
			}

			var picked = NearestThumb(mouseX, lowX, highX, lo <= min);
			_rangeThumb.Set(key, picked);
			_rangeFocusThumb.Set(key, picked);
		}

		var focused = enabled && ImGui.IsItemFocused();
		var enteredNavEdit = false;
		if (enabled && focused && clicked && !ImGui.IsMouseReleased(ImGuiMouseButton.Left))
		{
			if (_rangeNavActive == key)
			{
				_rangeNavActive = 0;
				if (ImGuiP.GetActiveID() == key)
				{
					ImGuiP.ClearActiveID();
				}
			}
			else
			{
				_rangeNavActive = key;
				enteredNavEdit = true;
				ImGuiP.SetActiveID(key, ImGuiP.GetCurrentWindow());
			}
		}

		var currentActiveId = ImGuiP.GetActiveID();
		if (_rangeNavActive == key && (!enabled || !focused || (currentActiveId != 0 && currentActiveId != key)))
		{
			_rangeNavActive = 0;
		}
		else if (_rangeNavActive == key && currentActiveId == 0)
		{
			ImGuiP.SetActiveID(key, ImGuiP.GetCurrentWindow());
		}

		var navEditing = enabled && _rangeNavActive == key;
		var active = enabled && (navEditing || ImGui.IsItemActive());
		var activeThumb = !active ? -1
			: navEditing ? (_rangeFocusThumb.TryGet(key, out var navThumb) ? navThumb : 0)
			: _rangeThumb.TryGet(key, out var stored) ? stored
			: NearestThumb(mouseX, lowX, highX, lo <= min);
		var changed = false;

		if (active && ImGui.IsMouseDown(ImGuiMouseButton.Left) && !navEditing)
		{
			var fraction = Math.Clamp((mouseX - trackLeft) / MathF.Max(trackRight - trackLeft, 1f), 0f, 1f);
			var v = min + (fraction * range);
			if (integer)
			{
				v = step > 1f ? Snap((long)MathF.Round(v), (int)min, (int)max, (int)step) : MathF.Round(v);
			}
			else if (step > 0f)
			{
				v = Snap(v, min, max, step);
			}

			if (activeThumb == 0)
			{
				var limit = SnapRangeBound(hi - minSpan, min, max, step, integer, true);
				v = MathF.Max(MathF.Min(v, limit), min);
				if (v != lo)
				{
					lo = v;
					changed = true;
				}
			}
			else
			{
				var limit = SnapRangeBound(lo + minSpan, min, max, step, integer, false);
				v = MathF.Min(MathF.Max(v, limit), max);
				if (v != hi)
				{
					hi = v;
					changed = true;
				}
			}
		}
		else if (navEditing)
		{
			ImGuiP.SetActiveIdUsingNavAndKeys();
			M3.CaptureEscape();

			if (!enteredNavEdit && (ImGui.IsKeyPressed(ImGuiKey.Escape)
				|| ImGui.IsKeyPressed(ImGuiKey.Enter)
				|| ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)
				|| ImGui.IsKeyPressed(ImGuiKey.Space)
				|| ImGui.IsKeyPressed(ImGuiKey.GamepadFaceDown)
				|| ImGui.IsKeyPressed(ImGuiKey.GamepadFaceRight)))
			{
				_rangeNavActive = 0;
				ImGuiP.ClearActiveID();
			}
			else
			{
				_ = _rangeFocusThumb.TryGet(key, out var focusThumb);
				if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)
					|| ImGui.IsKeyPressed(ImGuiKey.DownArrow)
					|| ImGui.IsKeyPressed(ImGuiKey.GamepadDpadUp)
					|| ImGui.IsKeyPressed(ImGuiKey.GamepadDpadDown))
				{
					focusThumb = focusThumb == 0 ? 1 : 0;
					_rangeFocusThumb.Set(key, focusThumb);
				}

				var deltaDir = ImGui.IsKeyPressed(ImGuiKey.LeftArrow) || ImGui.IsKeyPressed(ImGuiKey.GamepadDpadLeft) ? -1
					: ImGui.IsKeyPressed(ImGuiKey.RightArrow) || ImGui.IsKeyPressed(ImGuiKey.GamepadDpadRight) ? 1
					: 0;
				if (deltaDir != 0 && range > 0f)
				{
					var nudge = step > 0f ? step : integer ? 1f : range * 0.05f;
					if (focusThumb == 0)
					{
						var next = lo + (deltaDir * nudge);
						if (integer)
						{
							next = step > 1f ? Snap((long)MathF.Round(next), (int)min, (int)max, (int)step) : MathF.Round(next);
						}
						else if (step > 0f)
						{
							next = Snap(next, min, max, step);
						}

						var limit = SnapRangeBound(hi - minSpan, min, max, step, integer, true);
						next = MathF.Max(MathF.Min(next, limit), min);
						if (next != lo)
						{
							lo = next;
							changed = true;
						}
					}
					else
					{
						var next = hi + (deltaDir * nudge);
						if (integer)
						{
							next = step > 1f ? Snap((long)MathF.Round(next), (int)min, (int)max, (int)step) : MathF.Round(next);
						}
						else if (step > 0f)
						{
							next = Snap(next, min, max, step);
						}

						var limit = SnapRangeBound(lo + minSpan, min, max, step, integer, false);
						next = MathF.Min(MathF.Max(next, limit), max);
						if (next != hi)
						{
							hi = next;
							changed = true;
						}
					}
				}
			}
		}

		if (changed)
		{
			low = lo;
			high = hi;
			lowX = RangeHandleX(lo, min, range, trackLeft, trackRight);
			highX = RangeHandleX(hi, min, range, trackLeft, trackRight);
		}

		var hoveredThumb = active ? activeThumb
			: IsHovered(enabled) ? NearestThumb(mouseX, lowX, highX, lo <= min)
			: focused && ImGui.GetIO().NavVisible ? (_rangeFocusThumb.TryGet(key, out var focusedIndex) ? focusedIndex : 0)
			: -1;

		var drawList = ImGui.GetWindowDrawList();
		var accent = enabled ? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);
		var accentColor = M3.U32(accent);
		var dotColor = M3.U32(enabled ? s.OnPrimary : s.Surface, 0.9f);

		drawList.AddRectFilled(
			new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
			new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
			enabled ? M3.U32(s.SurfaceContainerHighest) : M3.U32(s.OnSurface, M3.DisabledContainer), trackHeight);

		if (highX > lowX)
		{
			drawList.AddRectFilled(
				new Vector2(lowX, centerY - (trackHeight * 0.5f)),
				new Vector2(highX, centerY + (trackHeight * 0.5f)),
				accentColor, trackHeight);
		}

		var topThumb = hoveredThumb >= 0 ? hoveredThumb : 1;
		var bottomCenter = new Vector2(topThumb == 0 ? highX : lowX, centerY);
		var topCenter = new Vector2(topThumb == 0 ? lowX : highX, centerY);

		drawList.AddCircleFilled(bottomCenter, handleRadius, accentColor, 24);
		drawList.AddCircleFilled(bottomCenter, handleRadius * 0.45f, dotColor, 16);

		if (hoveredThumb >= 0)
		{
			drawList.AddCircleFilled(topCenter, handleRadius + (8f * scale),
				M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
		}

		drawList.AddCircleFilled(topCenter, handleRadius, accentColor, 24);
		drawList.AddCircleFilled(topCenter, handleRadius * 0.45f, dotColor, 16);

		M3Draw.FocusRing(itemMin, itemMax, height * 0.5f);

		var readoutColor = M3.U32(s.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent);
		var readoutX = itemMax.X + (10f * scale);
		var readout = displayValue ?? (integer ? SliderIntReadout(key, (int)MathF.Round(lo), (int)MathF.Round(hi), true) : SliderReadout(key, format, lo, hi, true));
		var readoutSize = ImGui.CalcTextSize(readout);
		drawList.AddText(new Vector2(readoutX, centerY - (readoutSize.Y * 0.5f)), readoutColor, readout);

		if (active)
		{
			var value = activeThumb == 0 ? lo : hi;
			var bubble = integer ? SliderReadout(key ^ (activeThumb == 0 ? 1u : 2u), (int)MathF.Round(value)) : FormatValue(format, value);
			var textSize = ImGui.CalcTextSize(bubble);
			var padding = new Vector2(8f, 4f) * scale;
			var bubbleMin = new Vector2(topCenter.X - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
			var bubbleMax = bubbleMin + textSize + (padding * 2f);
			var foreground = ImGui.GetForegroundDrawList();
			foreground.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
			foreground.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), bubble);
		}

		if (reserveReadout && !string.IsNullOrEmpty(readout))
		{
			var gutter = displayValue != null ? SliderValueGutter(readout)
				: integer ? SliderIntGutter((int)min, (int)max, true)
				: SliderGutter(format, min, max, true);
			ReserveReadoutSpace(itemMax.X + gutter);
		}

		return changed;
	}
}
