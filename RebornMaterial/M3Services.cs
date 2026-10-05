using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace RebornMaterial;

// Filled in by M3.Initialize, from the host plugin's own service scope.
internal sealed class M3Services
{
	[PluginService]
	internal static ITextureProvider TextureProvider { get; private set; } = null!;
}
