using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool PickerField(string id, string label, float width, bool open, bool placeholder = false, IDalamudTextureWrap? icon = null,
		bool enabled = true, string? tooltip = null)
	{
		return DrawDropdownField(id, label, width, open, placeholder, icon, enabled, tooltip);
	}
}
