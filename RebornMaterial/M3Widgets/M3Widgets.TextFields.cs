using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool SearchField(string id, string hint, ref string text, float width, int maxLength = 128, bool busy = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = M3.FitText(40f, 8f);
		width = width > 0f ? width : MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var effectivelyEnabled = enabled && (ImGuiP.GetItemFlags() & ImGuiItemFlags.Disabled) == 0;
		var hoveringField = effectivelyEnabled && M3Draw.MouseOver(min, max);

		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHigh, !enabled ? 0.5f : hoveringField ? 1f : 0.92f), height * 0.5f);

		var iconPadding = 14f * scale;
		var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.Search);
		if (busy)
		{
			var stroke = 2f * scale;
			Spinner(drawList, new Vector2(min.X + iconPadding + (iconSize.X * 0.5f), min.Y + (height * 0.5f)),
				MathF.Max(1f, (iconSize.X - stroke) * 0.5f), s.Primary, stroke);
		}
		else
		{
			M3Draw.Icon(drawList, FontAwesomeIcon.Search,
				new Vector2(min.X + iconPadding, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, enabled ? 0.9f : M3.DisabledContent));
		}

		var fieldStart = min.X + iconPadding + iconSize.X + (10f * scale);
		var hasText = !string.IsNullOrEmpty(text);
		var clearWidth = hasText ? 32f * scale : 0f;
		var fieldWidth = MathF.Max(24f * scale, max.X - fieldStart - (14f * scale) - clearWidth);

		ImGui.SetCursorScreenPos(new Vector2(fieldStart, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(fieldWidth);

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputTextWithHint(id, hint, ref text, maxLength, ImGuiInputTextFlags.AutoSelectAll);
		}

		var focused = ImGui.IsItemActive();
		if (focused)
		{
			M3.CaptureEscape();
			drawList.AddRect(min, max, M3.U32(s.Primary, 0.85f), height * 0.5f, ImDrawFlags.RoundCornersAll, 1.5f * scale);
		}

		if (hasText)
		{
			using var idScope = ImRaii.PushId(id);
			ImGui.SetCursorScreenPos(new Vector2(max.X - clearWidth - (6f * scale), min.Y + ((height - (26f * scale)) * 0.5f)));
			if (IconButton("##clear", FontAwesomeIcon.Times, "Clear search", diameter: 26f * scale, enabled: enabled))
			{
				text = string.Empty;
				changed = true;
			}
		}

		ImGui.SetCursorScreenPos(min);
		ImGui.Dummy(new Vector2(width, height));
		return changed;
	}

	public static float TextFieldHeight => M3.FitText(48f, 12f);

	public static bool TextField(string id, string label, ref string text, float width, int maxLength = 256, string? supporting = null, bool error = false, FontAwesomeIcon leadingIcon = FontAwesomeIcon.None, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = TextFieldHeight;
		width = width > 0f ? width : MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var paddingX = 16f * scale;
		var framePadding = ImGui.GetStyle().FramePadding.X;
		var bodyFontSize = ImGui.GetFontSize();

		float smallFontSize;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			smallFontSize = ImGui.GetFontSize();
		}

		var origin = ImGui.GetCursorScreenPos();
		var min = origin + new Vector2(0f, smallFontSize * 0.5f);
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var effectivelyEnabled = enabled && (ImGuiP.GetItemFlags() & ImGuiItemFlags.Disabled) == 0;
		var hovered = effectivelyEnabled && M3Draw.MouseOver(min, max);
		var dim = enabled ? 1f : M3.DisabledContent;

		var textLeft = min.X + paddingX;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(textLeft, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(error ? s.Error : s.OnSurfaceVariant, dim));
			textLeft += iconSize.X + (12f * scale);
		}

		var trailingWidth = error ? M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle).X + (12f * scale) : 0f;
		var textRight = max.X - paddingX - trailingWidth;

		var inputLeft = textLeft - framePadding;
		ImGui.SetCursorScreenPos(new Vector2(inputLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, textRight + framePadding - inputLeft));

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputText(id, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		if (focused)
		{
			M3.CaptureEscape();
		}

		var floating = M3Motion.Approach(ImGuiP.GetItemID(), focused || !string.IsNullOrEmpty(text) ? 1f : 0f, M3Motion.FastDuration);

		var accent = M3.Alpha(error ? s.Error : focused ? s.Primary : hovered ? s.OnSurface : s.Outline, dim);
		var labelColor = M3.Alpha(error ? s.Error : focused ? s.Primary : s.OnSurfaceVariant, dim);

		var rounding = M3.ShapeExtraSmall;
		var hasLabel = !string.IsNullOrEmpty(label);
		var floatingX = min.X + paddingX;
		var maxNotchEnd = max.X - rounding;
		var notchStart = MathF.Max(min.X + rounding, floatingX - (4f * scale));
		var labelScale = bodyFontSize > 0f ? smallFontSize / bodyFontSize : 1f;
		var maxLabelWidth = MathF.Max(8f * scale, maxNotchEnd - floatingX - (4f * scale));
		var floatingLabel = hasLabel ? M3Navigation.Truncate(label, maxLabelWidth / MathF.Max(0.1f, labelScale)) : string.Empty;
		var notchEnd = hasLabel
			? MathF.Min(maxNotchEnd, notchStart + (((ImGui.CalcTextSize(floatingLabel).X * labelScale) + (8f * scale)) * floating))
			: notchStart;
		OutlineWithNotch(drawList, min, max, rounding, notchStart, notchEnd, M3.U32(accent), (focused || error ? 2f : 1f) * scale);

		if (hasLabel)
		{
			var restingPosition = new Vector2(textLeft, min.Y + ((height - bodyFontSize) * 0.5f));
			var floatingPosition = new Vector2(floatingX, min.Y - (smallFontSize * 0.5f));
			var labelPos = Vector2.Lerp(restingPosition, floatingPosition, floating);
			var currentFontSize = float.Lerp(bodyFontSize, smallFontSize, floating);
			var clipRight = float.Lerp(textRight, maxNotchEnd - (2f * scale), floating);
			drawList.PushClipRect(new Vector2(min.X, origin.Y - (2f * scale)), new Vector2(MathF.Max(min.X + 1f, clipRight), max.Y), true);
			drawList.AddText(ImGui.GetFont(), currentFontSize, labelPos, M3.U32(labelColor), floatingLabel);
			drawList.PopClipRect();
		}

		if (error)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExclamationCircle,
				new Vector2(max.X - paddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)), M3.Alpha(s.Error, dim));
		}

		var bottom = max.Y;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			bottom = M3Draw.WrappedText(supporting, new Vector2(min.X + paddingX, max.Y + (4f * scale)),
				MathF.Max(32f * scale, width - (paddingX * 2f)), error ? M3.Alpha(s.Error, dim) : M3.Alpha(s.OnSurfaceVariant, 0.9f * dim));
		}

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(width, bottom - origin.Y));
		return changed;
	}

	private static void OutlineWithNotch(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float notchStart, float notchEnd, uint color, float thickness)
	{
		if (notchEnd - notchStart < 1f || notchEnd > max.X - rounding)
		{
			drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
			return;
		}

		drawList.PathLineTo(new Vector2(notchEnd, min.Y));
		drawList.PathArcToFast(new Vector2(max.X - rounding, min.Y + rounding), rounding, 9, 12);
		drawList.PathArcToFast(new Vector2(max.X - rounding, max.Y - rounding), rounding, 0, 3);
		drawList.PathArcToFast(new Vector2(min.X + rounding, max.Y - rounding), rounding, 3, 6);
		drawList.PathArcToFast(new Vector2(min.X + rounding, min.Y + rounding), rounding, 6, 9);
		drawList.PathLineTo(new Vector2(notchStart, min.Y));
		drawList.PathStroke(color, ImDrawFlags.None, thickness);
	}
}
