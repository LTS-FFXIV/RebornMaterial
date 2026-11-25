namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool RowIntRange(string label, ref int low, ref int high, int min, int max, string? unit = null, string? supporting = null, bool enabled = true)
	{
		var (display, lowId) = RowParts(label, "_low");
		var highId = RowParts(label, "_high").Id;
		const string Separator = "to";
		var separatorWidth = ImGui.CalcTextSize(Separator).X;
		var gap = M3.Space2;
		var maxEach = MathF.Max(NumberFieldWidth(false, unit), (M3SettingRow.MaxControlWidth() - separatorWidth - (gap * 2f)) * 0.5f);
		var lowWidth = MathF.Min(NumberFieldWidth(LiveIntText(lowId, low), false, unit), maxEach);
		var highWidth = MathF.Min(NumberFieldWidth(LiveIntText(highId, high), false, unit), maxEach);
		var controlSize = new Vector2(lowWidth + highWidth + separatorWidth + (gap * 2f), NumberFieldHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		var origin = row.ControlPosition;
		var lo = low;
		var hi = high;

		ImGui.SetCursorScreenPos(origin);
		var lowChanged = NumberFieldCore(lowId, ref lo, lowWidth, 0, min, max, unit, enabled, false);
		ImGui.GetWindowDrawList().AddText(new Vector2(origin.X + lowWidth + gap, origin.Y + ((controlSize.Y - ImGui.GetTextLineHeight()) * 0.5f)),
			M3.U32(M3.Scheme.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent), Separator);
		ImGui.SetCursorScreenPos(new Vector2(origin.X + lowWidth + (gap * 2f) + separatorWidth, origin.Y));
		var highChanged = NumberFieldCore(highId, ref hi, highWidth, 0, min, max, unit, enabled, false);
		M3SettingRow.End(row);

		if (!lowChanged && !highChanged)
		{
			return false;
		}

		if (lowChanged && lo > hi)
		{
			hi = lo;
		}
		else if (highChanged && hi < lo)
		{
			lo = hi;
		}

		if (lo == low && hi == high)
		{
			return false;
		}

		low = lo;
		high = hi;
		return true;
	}
}
