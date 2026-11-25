using System.Globalization;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private const ImGuiHoveredFlags ActiveHoverFlags = ImGuiHoveredFlags.AllowWhenBlockedByActiveItem;
	private const ImGuiHoveredFlags MouseOverFlags = ActiveHoverFlags | ImGuiHoveredFlags.AllowWhenDisabled;

	private static bool IsHovered(bool enabled)
	{
		return enabled && ImGui.IsItemHovered(ActiveHoverFlags);
	}

	private static float TextureIconSize => MathF.Max(18f * M3.Scale, ImGui.GetTextLineHeight());

	private static bool DisabledButton(string id, Vector2 size, bool enabled, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
	{
		using (ImRaii.PushStyle(ImGuiStyleVar.DisabledAlpha, 1f, !enabled))
		using (ImRaii.Disabled(!enabled))
		{
			return ImGui.InvisibleButton(id, size, flags) && enabled;
		}
	}

	private static bool DisabledButton(ReadOnlySpan<char> id, Vector2 size, bool enabled, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
	{
		using (ImRaii.PushStyle(ImGuiStyleVar.DisabledAlpha, 1f, !enabled))
		using (ImRaii.Disabled(!enabled))
		{
			return ImGui.InvisibleButton(id, size, flags) && enabled;
		}
	}

	private static float IconSlotWidth(FontAwesomeIcon icon, IDalamudTextureWrap? texture)
	{
		return M3Draw.Usable(texture) ? TextureIconSize : icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X;
	}

	private static void DrawIconSlot(ImDrawListPtr drawList, FontAwesomeIcon icon, IDalamudTextureWrap? texture, float x, float top, float height, Vector4 color, float alpha = 1f)
	{
		if (texture != null)
		{
			var size = TextureIconSize;
			if (M3Draw.Texture(drawList, texture, new Vector2(x, top + ((height - size) * 0.5f)), size, alpha))
			{
				return;
			}
		}

		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(x, top + ((height - iconSize.Y) * 0.5f)), color);
		}
	}

	private static ImRaii.ColorDisposable PushClearFrame()
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f));
	}

	private static (string Label, string Id) SplitLabel(string label)
	{
		var explicitId = label.IndexOf("###", StringComparison.Ordinal);
		if (explicitId >= 0)
		{
			var trailing = label[(explicitId + 3)..];
			return (label[..explicitId], string.IsNullOrEmpty(trailing) ? label : trailing);
		}

		var hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? (label, label) : (label[..hidden], label);
	}

	private static ReadOnlySpan<char> SplitLabelSpan(string label, out ReadOnlySpan<char> id)
	{
		var explicitId = label.IndexOf("###", StringComparison.Ordinal);
		if (explicitId >= 0)
		{
			id = explicitId + 3 < label.Length ? label.AsSpan(explicitId + 3) : label;
			return label.AsSpan(0, explicitId);
		}

		id = label;
		var hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? label : label.AsSpan(0, hidden);
	}

	internal static string FormatValue(string format, float value)
	{
		if (!float.IsFinite(value))
		{
			return "-";
		}

		if (string.IsNullOrEmpty(format))
		{
			return value.ToString("0.##", CultureInfo.InvariantCulture);
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
			return Unescape(format);
		}

		var cursor = percent + 1;
		var digits = 0;
		var hasDigits = false;
		var width = 0;
		var zeroPad = false;
		var leftAlign = false;
		var plus = false;

		while (cursor < format.Length && format[cursor] is '-' or '+' or ' ' or '#' or '0')
		{
			var flag = format[cursor];
			plus |= flag == '+';
			leftAlign |= flag == '-';
			zeroPad |= flag == '0';
			cursor++;
		}

		while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
		{
			width = Math.Min(64, (width * 10) + (format[cursor] - '0'));
			cursor++;
		}

		if (cursor < format.Length && format[cursor] == '.')
		{
			cursor++;
			while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
			{
				digits = Math.Min(15, (digits * 10) + (format[cursor] - '0'));
				cursor++;
				hasDigits = true;
			}
		}

		while (cursor < format.Length && format[cursor] is 'l' or 'h')
		{
			cursor++;
		}

		if (cursor >= format.Length)
		{
			return Unescape(format);
		}

		var specifier = format[cursor];
		var rendered = specifier switch
		{
			'f' or 'F' => value.ToString($"F{(hasDigits ? digits : 3)}", CultureInfo.InvariantCulture),
			'g' or 'G' => value.ToString($"G{(hasDigits ? Math.Max(1, digits) : 6)}", CultureInfo.InvariantCulture),
			'd' or 'i' or 'u' => ((long)Math.Clamp(Math.Truncate(value), int.MinValue, int.MaxValue)).ToString(CultureInfo.InvariantCulture),
			_ => null,
		};

		if (rendered == null)
		{
			return Unescape(format);
		}

		if (plus && value >= 0f && !rendered.StartsWith('-'))
		{
			rendered = "+" + rendered;
		}

		if (width > rendered.Length)
		{
			if (leftAlign)
			{
				rendered = rendered.PadRight(width, ' ');
			}
			else if (zeroPad && (rendered[0] is '-' or '+'))
			{
				rendered = rendered[0] + rendered[1..].PadLeft(width - 1, '0');
			}
			else
			{
				rendered = rendered.PadLeft(width, zeroPad ? '0' : ' ');
			}
		}

		return Unescape(string.Concat(format.AsSpan(0, percent), rendered, format.AsSpan(cursor + 1)));
	}

	private static string Unescape(string text)
	{
		return text.Contains("%%", StringComparison.Ordinal)
			? text.Replace("%%", "%", StringComparison.Ordinal)
			: text;
	}

	internal static void Reset()
	{
		ResetButtons();
		ResetColorSwatch();
		ResetCombos();
		ResetRows();
		ResetSliders();
		ResetNumberFields();
		ResetStructure();
		ResetHotkeys();
		ResetReorder();
		ResetRange();
		ResetCopy();
	}
}
