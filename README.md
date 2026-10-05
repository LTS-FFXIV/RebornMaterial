# RebornMaterial

Material Design 3 components for Dalamud ImGui plugins, based heavily on Google's Material Design 3 UI/UX design.

The whole colour scheme is generated from one accent colour, and every metric follows the user's text and element size settings.

## Components

| Type | What it draws |
| --- | --- |
| `M3` | Scheme, shapes, spacing, fonts, scale, and the colour helpers (`Alpha`, `StateLayer`, `ContentOn`, `Severity`) |
| `M3Style` | Pushes the scheme onto ImGui's own style, for a whole window |
| `M3Widgets` | Buttons, icon buttons and toggles, switches, checkboxes, radios, segmented buttons, chips, pills, badges, sliders, progress bars and spinners, text and search fields, combos, menus, banners, empty states, colour swatches, dividers, and the window title-bar actions |
| `M3Card`, `M3ExpandableCard` | Filled, outlined and elevated cards, which can also collapse |
| `M3SettingRow`, `M3SubGroup` | Label and description on the left, control on the right. Wraps to two lines when narrow |
| `M3Navigation` | Navigation drawer and rail, and primary and secondary tabs |
| `M3Dialog` | Modal dialogs with actions |
| `M3Snackbar` | Queued toast messages with an optional action |
| `M3Badge` | A small tag on a line of text |
| `M3TextField` | Filled single-line text field |
| `M3ActionIcon` | Action and game icons with a cooldown sweep and charge pips |
| `M3HeroHeader` | Window header with an image, or a gradient with animated stars |
| `M3WindowFold` | Minimises a window down to its title-bar actions, with animation |
| `M3Draw`, `M3Motion` | Drawing primitives (arcs, shadows, gradients, icons) and per-widget easing |
| `M3Gallery` | A showcase of everything above, for a debug tab |

## Setup

Add the package:

```xml
<PackageReference Include="RebornMaterial" Version="1.0.0" />
```

Then wire it into the plugin's lifetime:

```csharp
using RebornMaterial;

public MyPlugin(IDalamudPluginInterface pluginInterface)
{
    Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

    // Optional. Below 1 makes every component more compact than the Material spec (RSR uses 0.75).
    M3.ElementBaseline = 1f;
    M3.Initialize(pluginInterface, Config);

    pluginInterface.UiBuilder.Draw += OnDraw;
}

private void OnDraw()
{
    M3.BeginFrame(); // Once per frame, before any window draws.
    windowSystem.Draw();
}

public void Dispose()
{
    M3.Dispose(); // Releases the fonts and clears cached animation state.
}
```

### Settings

`M3.Initialize` takes any `IM3Settings`. Its property names match the existing Reborn configs, so a configuration class can usually implement it directly:

```csharp
public class Configuration : IPluginConfiguration, IM3Settings
{
    public Vector4 UiAccentColor { get; set; } = M3.DefaultSeed;
    public float UiTextScale { get; set; } = 1f;
    public float UiElementScale { get; set; } = 1f;
}
```

If the settings live somewhere the library can't reference, write a small adapter that forwards to them. Pass nothing to use a plain `M3Settings`, which you can change through `M3.Settings`.

### Tooltips

Components show tooltips through `M3Tooltip`. To route them through your own tooltip code instead, for example one that honours a "show tooltips" option:

```csharp
M3Tooltip.Handler = text => MyTooltips.Show(text);
```

## Using it in a window

```csharp
private M3Style.Scope _theme;

public override void PreDraw()
{
    _theme = M3Style.Push();
}

public override void PostDraw()
{
    _theme.Dispose();
    _theme = default;
}

public override void Draw()
{
    using (var card = M3Card.Begin("general", "General", FontAwesomeIcon.Cog))
    {
        var row = M3SettingRow.Begin("Enabled", "Turns the feature on.", M3Widgets.SwitchSize());
        var enabled = Config.Enabled;
        if (M3Widgets.Switch("##enabled", ref enabled))
        {
            Config.Enabled = enabled;
        }


        M3SettingRow.End(row);

        if (M3Widgets.Button("##save", "Save", M3ButtonStyle.Filled, FontAwesomeIcon.Save))
        {
            Config.Save();
            M3Snackbar.Show("Saved");
        }
    }

    // Last, so it draws on top.
    M3Snackbar.Draw(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());
}
```

To scale everything in one window, push `M3.PushWindowScale(scale)` before `M3Style.Push` and dispose it after.

`M3Gallery.Draw()` shows every component, with controls for the preview scale and accent colour. Put it in a debug tab to try things out.

### Action icons

`M3ActionIcon` takes a texture or a game icon ID, plus an optional `M3Cooldown`:

```csharp
var cooldown = new M3Cooldown(isCoolingDown, recastTime, elapsed, currentCharges, maxCharges);
if (M3ActionIcon.Draw("##fire", action.Icon, 48f * M3.Scale, cooldown, enabled: level >= action.Level, tooltip: action.Name))
{
    // Pressed.
}
```

## Building locally

You need the .NET 10 SDK and a Dalamud dev install. Like any Dalamud plugin, the build finds Dalamud through XIVLauncher's `addon/Hooks/dev`, or through `DALAMUD_HOME`.

```bash
dotnet build -c Release
```

```bash
dotnet pack RebornMaterial/RebornMaterial.csproj -c Release -o artifacts
```
