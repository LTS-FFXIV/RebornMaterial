using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static uint _numberEditId;
	private static float _numberEditFloat;
	private static int _numberEditInt;
	private static bool _numberEditDirty;
	private static int _numberEditFrame = -2;

	private static void ResetNumberFields()
	{
		_numberEditId = 0;
		_numberEditFloat = 0f;
		_numberEditInt = 0;
		_numberEditDirty = false;
		_numberEditFrame = -2;
	}

	public static float NumberFieldHeight => M3.FitText(40f, 8f);

	private static float StepperDiameter => NumberFieldHeight - (8f * M3.Scale);

	public static float NumberFieldWidth(bool steppers = false, string? unit = null)
	{
		return NumberFieldWidth(string.Empty, steppers, unit);
	}

	public static float NumberFieldWidth(string sampleText, bool steppers = false, string? unit = null)
	{
		var scale = M3.Scale;
		var minTextWidth = MathF.Max(64f * scale, ImGui.CalcTextSize("00000").X);
		var sampleWidth = string.IsNullOrEmpty(sampleText) ? 0f : ImGui.CalcTextSize(sampleText).X + ImGui.CalcTextSize("0").X;
		var width = MathF.Max(minTextWidth, sampleWidth) + (22f * scale);
		if (steppers)
		{
			width += (StepperDiameter * 2f) + (6f * scale);
		}

		if (!string.IsNullOrEmpty(unit))
		{
			width += ImGui.CalcTextSize(unit).X + (6f * scale);
		}

		return width;
	}

	private static string? ActiveInputText(uint fieldKey)
	{
		if (ImGuiP.GetActiveID() != fieldKey)
		{
			return null;
		}

		var state = ImGuiP.GetInputTextState(fieldKey);
		if (state.IsNull)
		{
			return null;
		}

		if (state.CurLenW > 0)
		{
			var len = Math.Min(state.CurLenW, state.TextW.Size);
			if (len > 0)
			{
				return new string(MemoryMarshal.Cast<ushort, char>(state.TextW.AsSpan()[..len]));
			}
		}

		if (state.TextAIsValid && state.CurLenA > 0)
		{
			var len = Math.Min(state.CurLenA, state.TextA.Size);
			if (len > 0)
			{
				return Encoding.UTF8.GetString(state.TextA.AsSpan()[..len]);
			}
		}

		return null;
	}

	private static string LiveFloatText(string id, float value, string format)
	{
		var frame = ImGui.GetFrameCount();
		using var idScope = ImRaii.PushId(id);
		var fieldKey = ImGui.GetID("##value");
		var working = _numberEditId == fieldKey && _numberEditDirty && _numberEditFrame >= frame - 1 ? _numberEditFloat : value;
		return ActiveInputText(fieldKey) ?? FormatValue(format, working);
	}

	private static string LiveIntText(string id, int value)
	{
		var frame = ImGui.GetFrameCount();
		using var idScope = ImRaii.PushId(id);
		var fieldKey = ImGui.GetID("##value");
		var working = _numberEditId == fieldKey && _numberEditDirty && _numberEditFrame >= frame - 1 ? _numberEditInt : value;
		return ActiveInputText(fieldKey) ?? working.ToString(CultureInfo.InvariantCulture);
	}

	public static bool NumberField(string id, ref float value, float width, float step = 0f, float min = float.MinValue, float max = float.MaxValue, string format = "%.2f", string? unit = null, bool enabled = true)
	{
		return NumberFieldCore(id, ref value, width, step, min, max, format, unit, enabled, true);
	}

	internal static bool NumberFieldCore(string id, ref float value, float width, float step, float min, float max, string format, string? unit, bool enabled, bool autoWiden)
	{
		var frame = ImGui.GetFrameCount();
		if (_numberEditDirty && _numberEditFrame < frame - 1)
		{
			_numberEditDirty = false;
		}

		using var idScope = ImRaii.PushId(id);
		var before = value;
		var fieldKey = ImGui.GetID("##value");
		var working = _numberEditId == fieldKey && _numberEditDirty ? _numberEditFloat : value;
		var effectiveWidth = autoWiden
			? MathF.Min(MathF.Max(width, NumberFieldWidth(ActiveInputText(fieldKey) ?? FormatValue(format, working), step > 0f, unit)), MathF.Max(width, ImGui.GetContentRegionAvail().X))
			: width;
		var layout = BeginNumberField(effectiveWidth, step > 0f, unit);

		bool typed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding with { X = 0f }))
		{
			typed = ImGui.InputFloat("##value", ref working, 0f, 0f, IsValidFloatFormat(format) ? format : "%.2f");
		}

		var active = ImGui.IsItemActive();
		var committed = ImGui.IsItemDeactivatedAfterEdit();
		var deactivated = ImGui.IsItemDeactivated();

		if (typed)
		{
			_numberEditId = fieldKey;
			_numberEditFloat = working;
			_numberEditDirty = true;
			_numberEditFrame = frame;
		}
		else if (active && _numberEditId == fieldKey)
		{
			_numberEditFrame = frame;
		}

		var changed = false;
		if (committed && _numberEditId == fieldKey && _numberEditDirty)
		{
			if (float.IsFinite(_numberEditFloat))
			{
				value = Math.Clamp(_numberEditFloat, MathF.Min(min, max), MathF.Max(min, max));
				changed = true;
			}

			_numberEditDirty = false;
		}
		else if (deactivated && _numberEditId == fieldKey)
		{
			_numberEditDirty = false;
		}

		var direction = EndNumberField(layout, effectiveWidth, step > 0f, unit, enabled, active);
		if (direction != 0)
		{
			var baseValue = _numberEditId == fieldKey && _numberEditDirty && float.IsFinite(_numberEditFloat) ? _numberEditFloat : value;
			if (_numberEditId == fieldKey)
			{
				_numberEditDirty = false;
			}

			var next = baseValue + (direction * step);
			if (float.IsFinite(next))
			{
				value = Math.Clamp(next, MathF.Min(min, max), MathF.Max(min, max));
				changed = true;
			}
		}

		return changed && !value.Equals(before);
	}

	public static bool NumberField(string id, ref int value, float width, int step = 0, int min = int.MinValue, int max = int.MaxValue, string? unit = null, bool enabled = true)
	{
		return NumberFieldCore(id, ref value, width, step, min, max, unit, enabled, true);
	}

	internal static bool NumberFieldCore(string id, ref int value, float width, int step, int min, int max, string? unit, bool enabled, bool autoWiden)
	{
		var frame = ImGui.GetFrameCount();
		if (_numberEditDirty && _numberEditFrame < frame - 1)
		{
			_numberEditDirty = false;
		}

		using var idScope = ImRaii.PushId(id);
		var before = value;
		var fieldKey = ImGui.GetID("##value");
		var working = _numberEditId == fieldKey && _numberEditDirty ? _numberEditInt : value;
		var effectiveWidth = autoWiden
			? MathF.Min(MathF.Max(width, NumberFieldWidth(ActiveInputText(fieldKey) ?? working.ToString(CultureInfo.InvariantCulture), step > 0, unit)), MathF.Max(width, ImGui.GetContentRegionAvail().X))
			: width;
		var layout = BeginNumberField(effectiveWidth, step > 0, unit);

		bool typed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding with { X = 0f }))
		{
			typed = ImGui.InputInt("##value", ref working, 0, 0);
		}

		var active = ImGui.IsItemActive();
		var committed = ImGui.IsItemDeactivatedAfterEdit();
		var deactivated = ImGui.IsItemDeactivated();

		if (typed)
		{
			_numberEditId = fieldKey;
			_numberEditInt = working;
			_numberEditDirty = true;
			_numberEditFrame = frame;
		}
		else if (active && _numberEditId == fieldKey)
		{
			_numberEditFrame = frame;
		}

		var changed = false;
		if (committed && _numberEditId == fieldKey && _numberEditDirty)
		{
			value = Math.Clamp(_numberEditInt, Math.Min(min, max), Math.Max(min, max));
			_numberEditDirty = false;
			changed = true;
		}
		else if (deactivated && _numberEditId == fieldKey)
		{
			_numberEditDirty = false;
		}

		var direction = EndNumberField(layout, effectiveWidth, step > 0, unit, enabled, active);
		if (direction != 0)
		{
			var baseValue = _numberEditId == fieldKey && _numberEditDirty ? _numberEditInt : value;
			if (_numberEditId == fieldKey)
			{
				_numberEditDirty = false;
			}

			var stepped = (int)Math.Clamp((long)baseValue + ((long)direction * step), int.MinValue, int.MaxValue);
			value = Math.Clamp(stepped, Math.Min(min, max), Math.Max(min, max));
			changed = true;
		}

		return changed && value != before;
	}

	public static bool RowNumber(string label, ref float value, float step = 0f, float min = float.MinValue, float max = float.MaxValue, string format = "%.2f", string? unit = null, string? supporting = null, bool enabled = true)
	{
		var (display, id) = RowParts(label, "_number");
		var width = MathF.Min(NumberFieldWidth(LiveFloatText(id, value, format), step > 0f, unit), M3SettingRow.MaxControlWidth());
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, NumberFieldHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = NumberFieldCore(id, ref value, width, step, min, max, format, unit, enabled, false);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowNumber(string label, ref int value, int step = 1, int min = int.MinValue, int max = int.MaxValue, string? unit = null, string? supporting = null, bool enabled = true)
	{
		var (display, id) = RowParts(label, "_number");
		var width = MathF.Min(NumberFieldWidth(LiveIntText(id, value), step > 0, unit), M3SettingRow.MaxControlWidth());
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, NumberFieldHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = NumberFieldCore(id, ref value, width, step, min, max, unit, enabled, false);
		M3SettingRow.End(row);
		return changed;
	}

	private readonly record struct NumberLayout(Vector2 Min, Vector2 Max, float UnitX);

	private static NumberLayout BeginNumberField(float width, bool steppers, string? unit)
	{
		var scale = M3.Scale;
		var height = NumberFieldHeight;
		var min = ImGui.GetCursorScreenPos();
		var max = min + new Vector2(width, height);
		var diameter = StepperDiameter;

		var rightEdge = steppers ? max.X - (10f * scale) - (diameter * 2f) : max.X - (10f * scale);
		var unitWidth = string.IsNullOrEmpty(unit) ? 0f : ImGui.CalcTextSize(unit).X;
		var unitX = unitWidth > 0f ? rightEdge - unitWidth : 0f;
		var textMaxX = unitWidth > 0f ? unitX - (6f * scale) : rightEdge;
		var textMinX = min.X + (12f * scale);
		var inputWidth = MathF.Max(1f, textMaxX - textMinX);

		ImGui.SetCursorScreenPos(new Vector2(textMinX, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(inputWidth);
		return new NumberLayout(min, max, unitX);
	}

	private static int EndNumberField(NumberLayout layout, float width, bool steppers, string? unit, bool enabled, bool focused)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var (min, max, unitX) = layout;
		var height = max.Y - min.Y;
		var hovered = enabled && (ImGuiP.GetItemFlags() & ImGuiItemFlags.Disabled) == 0 && M3Draw.MouseOver(min, max);
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		if (focused)
		{
			M3.CaptureEscape();
		}

		var outline = focused ? s.Primary : hovered ? s.OnSurface : s.Outline;
		drawList.AddRect(min, max, M3.U32(outline, dim), M3.ShapeExtraSmall, ImDrawFlags.None, (focused ? 2f : 1f) * scale);

		if (!string.IsNullOrEmpty(unit))
		{
			var unitSize = ImGui.CalcTextSize(unit);
			if (unitX >= min.X + (12f * scale))
			{
				drawList.AddText(new Vector2(unitX, min.Y + ((height - unitSize.Y) * 0.5f)), M3.U32(s.OnSurfaceVariant, 0.9f * dim), unit);
			}
		}

		var direction = 0;
		if (steppers)
		{
			var diameter = StepperDiameter;
			var top = min.Y + ((height - diameter) * 0.5f);
			var right = max.X - (4f * scale);

			ImGui.PushButtonRepeat(true);
			ImGui.SetCursorScreenPos(new Vector2(right - (diameter * 2f) - (2f * scale), top));
			if (IconButton("##less", FontAwesomeIcon.Minus, null, diameter: diameter, enabled: enabled))
			{
				direction = -1;
			}

			ImGui.SetCursorScreenPos(new Vector2(right - diameter, top));
			if (IconButton("##more", FontAwesomeIcon.Plus, null, diameter: diameter, enabled: enabled))
			{
				direction = 1;
			}

			ImGui.PopButtonRepeat();
		}

		ImGui.SetCursorScreenPos(min);
		ImGui.Dummy(new Vector2(width, height));
		return direction;
	}
}
