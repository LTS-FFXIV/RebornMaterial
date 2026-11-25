using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Tooltip
{
	private const float WrapEms = 28f;

	private static int _shownFrame = -1;

	public static Action<string>? Handler { get; set; }

	internal static bool ShownThisFrame => _shownFrame == ImGui.GetFrameCount();

	public static void Hovered(string? text)
	{
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			Show(text);
		}
	}

	public static void Show(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		_shownFrame = ImGui.GetFrameCount();
		if (Handler != null)
		{
			Handler(text);
			return;
		}

		using var tooltip = ImRaii.Tooltip();
		using var font = ImRaii.PushFont(M3.IsInitialized ? M3.Body : default, M3.IsInitialized);
		using var wrap = ImRaii.TextWrapPos(ImGui.GetFontSize() * WrapEms);
		ImGui.TextUnformatted(text);
	}
}
