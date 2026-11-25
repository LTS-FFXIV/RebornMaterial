using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Badge
{
	private static float PaddingX => 6f * M3.Scale;

	public static void Draw(string label, Vector4 accent, string? tooltip = null)
	{
		var frameHeight = ImGui.GetFrameHeight();
		var (size, textSize) = Measure(label);

		ImGui.Dummy(new Vector2(size.X, frameHeight));
		_ = PaintCore(ImGui.GetWindowDrawList(), ImGui.GetItemRectMin() + new Vector2(0f, (frameHeight - size.Y) * 0.5f), label, accent, size, textSize);

		if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			M3Tooltip.Show(tooltip);
		}
	}

	public static Vector2 Size(string label)
	{
		return Measure(label).Box;
	}

	public static Vector2 Paint(ImDrawListPtr drawList, Vector2 min, string label, Vector4 accent)
	{
		var (size, textSize) = Measure(label);
		return PaintCore(drawList, min, label, accent, size, textSize);
	}

	private static (Vector2 Box, Vector2 Text) Measure(string label)
	{
		var scale = M3.Scale;
		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(label);
		var height = MathF.Max(16f * scale, textSize.Y + (4f * scale));
		return (new Vector2(textSize.X + (PaddingX * 2f), height), textSize);
	}

	private static Vector2 PaintCore(ImDrawListPtr drawList, Vector2 min, string label, Vector4 accent, Vector2 size, Vector2 textSize)
	{
		var max = min + size;
		drawList.AddRectFilled(min, max, M3.U32(accent, 0.18f), M3.ShapeFull);
		drawList.AddRect(min, max, M3.U32(accent, 0.45f), M3.ShapeFull, ImDrawFlags.None, 1f * M3.Scale);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		drawList.AddText(new Vector2(min.X + PaddingX, min.Y + ((size.Y - textSize.Y) * 0.5f)), M3.U32(accent, 0.95f), label);
		return max;
	}
}
