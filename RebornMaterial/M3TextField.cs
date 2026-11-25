using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public enum M3TextFieldStyle
{
	Filled,

	Outlined,
}

public static class M3TextField
{
	public static float Height => M3.FitText(40f, 8f);

	public static bool Draw(string id, string hint, ref string text, float width, int maxLength = 256, bool password = false)
	{
		return Draw(id, hint, ref text, width, maxLength, password, M3TextFieldStyle.Filled, true);
	}

	public static bool Draw(string id, string hint, ref string text, float width, int maxLength, bool password, M3TextFieldStyle style, bool enabled = true,
		string? supporting = null, bool error = false, FontAwesomeIcon leadingIcon = FontAwesomeIcon.None, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		if (password)
		{
			flags |= ImGuiInputTextFlags.Password;
		}

		width = width > 0f ? width : MathF.Max(32f * M3.Scale, ImGui.GetContentRegionAvail().X);
		if (style == M3TextFieldStyle.Outlined)
		{
			return M3Widgets.TextField(id, hint, ref text, width, maxLength, supporting, error, leadingIcon, flags, enabled);
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = Height;
		var min = ImGui.GetCursorScreenPos();
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var effectivelyEnabled = enabled && (ImGuiP.GetItemFlags() & ImGuiItemFlags.Disabled) == 0;
		var hovered = effectivelyEnabled && M3Draw.MouseOver(min, max);
		var dim = enabled ? 1f : M3.DisabledContent;

		var fill = hovered ? M3.StateLayer(s.SurfaceContainerHighest, s.OnSurface, true, false) : s.SurfaceContainerHighest;
		drawList.AddRectFilled(min, max, M3.U32(fill, enabled ? fill.W : M3.DisabledContainer), M3.ShapeExtraSmall, ImDrawFlags.RoundCornersTop);

		var paddingX = 12f * scale;
		var textLeft = min.X + paddingX;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(textLeft, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(error ? s.Error : s.OnSurfaceVariant, dim));
			textLeft += iconSize.X + (10f * scale);
		}

		var trailingWidth = error ? M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle).X + (10f * scale) : 0f;
		var textRight = max.X - paddingX - trailingWidth;
		var transparent = new Vector4(0f, 0f, 0f, 0f);

		ImGui.SetCursorScreenPos(new Vector2(textLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, textRight - textLeft));

		bool changed;
		using (ImRaii.PushId(id))
		using (ImRaii.Disabled(!enabled))
		using (ImRaii.PushColor(ImGuiCol.FrameBg, transparent)
			.Push(ImGuiCol.FrameBgHovered, transparent)
			.Push(ImGuiCol.FrameBgActive, transparent))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding with { X = 0f }))
		{
			changed = ImGui.InputTextWithHint("##field", hint, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		if (focused)
		{
			M3.CaptureEscape();
		}

		if (error)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExclamationCircle,
				new Vector2(max.X - paddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)), M3.Alpha(s.Error, dim));
		}

		var thickness = (focused || error ? 2f : 1f) * scale;
		var indicator = error ? s.Error : focused ? s.Primary : M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.7f);
		drawList.AddRectFilled(new Vector2(min.X, max.Y - thickness), max, M3.U32(indicator, dim));

		var bottom = max.Y;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			bottom = M3Draw.WrappedText(supporting, new Vector2(min.X + paddingX, max.Y + (4f * scale)),
				MathF.Max(32f * scale, width - (paddingX * 2f)), error ? M3.Alpha(s.Error, dim) : M3.Alpha(s.OnSurfaceVariant, 0.9f * dim));
		}

		ImGui.SetCursorScreenPos(min);
		ImGui.Dummy(new Vector2(width, bottom - min.Y));

		return changed;
	}
}
