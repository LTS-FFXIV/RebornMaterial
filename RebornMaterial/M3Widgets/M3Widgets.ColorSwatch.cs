using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static uint _hexEditId;
	private static string _hexEditText = string.Empty;

	private static void ResetColorSwatch()
	{
		_hexEditId = 0;
		_hexEditText = string.Empty;
	}

	public static float ColorSwatchSize => 28f * M3.Scale;

	public static bool ColorSwatch(string id, ref Vector4 color, Vector4? defaultColor = null, bool alpha = true, bool enabled = true, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var diameter = ColorSwatchSize;
		var radius = diameter * 0.5f;

		using var idScope = ImRaii.PushId(id);
		var clicked = DisabledButton("##swatch", Vector2.One * diameter, enabled);
		var mouseOver = ImGui.IsItemHovered(MouseOverFlags);
		var hovered = IsHovered(enabled);
		var min = ImGui.GetItemRectMin();
		var center = min + (Vector2.One * radius);
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		if (alpha && color.W < 0.999f)
		{
			drawList.AddCircleFilled(center, radius, M3.U32(s.SurfaceContainerHighest, dim), 32);
			drawList.PathArcTo(center, radius, MathF.PI * 0.5f, MathF.PI * 1.5f, 16);
			drawList.PathFillConvex(M3.U32(s.OnSurfaceVariant, 0.55f * dim));
		}

		var shown = alpha ? color : color with { W = 1f };
		drawList.AddCircleFilled(center, radius, M3.U32(shown with { W = shown.W * dim }), 32);
		drawList.AddCircle(center, radius, M3.U32(s.Outline, (hovered ? 1f : 0.6f) * dim), 32, 1.5f * scale);
		M3Draw.FocusRing(min, min + (Vector2.One * diameter), radius);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		if (clicked)
		{
			ImGui.OpenPopup("##picker");
		}

		var changed = false;
		using var popup = ImRaii.Popup("##picker");
		if (!popup)
		{
			return false;
		}

		M3.CaptureEscape();

		var flags = alpha ? ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf : ImGuiColorEditFlags.NoAlpha;
		changed = ImGui.ColorPicker4("##color", ref color, flags);

		var hexKey = ImGui.GetID("##hex");
		var hexActive = ImGuiP.GetActiveID() == hexKey;
		if (!hexActive || _hexEditId != hexKey)
		{
			_hexEditText = M3ColorMath.ToHex(color, alpha);
		}

		var hex = _hexEditId == hexKey ? _hexEditText : M3ColorMath.ToHex(color, alpha);
		ImGui.SetNextItemWidth(120f * scale);
		var typed = ImGui.InputText("##hex", ref hex, 16);
		if (typed || ImGui.IsItemActive())
		{
			_hexEditId = hexKey;
			_hexEditText = hex;
		}

		var committed = ImGui.IsItemDeactivatedAfterEdit();
		if ((typed || committed) && M3ColorMath.TryParseHex(hex, out var parsed, out var digits) && (digits >= 6 || committed))
		{
			var next = alpha && digits is 4 or 8 ? parsed : parsed with { W = color.W };
			if (next != color)
			{
				color = next;
				changed = true;
			}
		}

		if (ImGui.IsItemDeactivated() && _hexEditId == hexKey)
		{
			_hexEditId = 0;
			_hexEditText = string.Empty;
		}

		if (defaultColor is { } fallback)
		{
			ImGui.SameLine(0f, M3.Space2);
			if (Button("##reset", "Reset", M3ButtonStyle.Text, FontAwesomeIcon.Undo, enabled: !SameColor(fallback, color, alpha)))
			{
				color = alpha ? fallback : fallback with { W = 1f };
				changed = true;
			}
		}

		return changed;
	}
}
