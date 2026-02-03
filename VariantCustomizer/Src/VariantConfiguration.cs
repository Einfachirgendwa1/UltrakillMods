using BepInEx.Logging;
using PluginConfig.API;
using PluginConfig.API.Fields;
using VariantCustomizer.Bridge;

namespace VariantCustomizer;

public class VariantConfiguration {
    private readonly VariantColorId colorId;
    private readonly VanillaColorId vanillaColorId;
    private ColorField? color1;
    private ColorField? color2;
    private ColorField? color3;

    internal VariantConfiguration(VariantColorId colorId, VanillaColorId vanillaColorId) {
        this.colorId = colorId;
        this.vanillaColorId = vanillaColorId;
    }

    private void Redraw() {
        VariantCustomizer.LogSource.LogInfo("Redraw invoked");
        colorId.SetColors(new GunColors(color1!.value, color2!.value, color3!.value));

        VariantCustomizer.Instance?.CheckModdedColors();
    }

    internal void Subpanel(ConfigPanel parentPanel, ManualLogSource logger) {
        GunColors vanilla = vanillaColorId.GetColors();

        color1 = new ColorField(parentPanel, "Color 1", parentPanel.guid + ".color1", vanilla.Color1);
        color2 = new ColorField(parentPanel, "Color 2", parentPanel.guid + ".color2", vanilla.Color2);
        color3 = new ColorField(parentPanel, "Color 3", parentPanel.guid + ".color3", vanilla.Color3);

        color1.onValueChange += e => {
            color1.value = e.value;
            Redraw();
        };

        color2.onValueChange += e => {
            color2.value = e.value;
            Redraw();
        };

        color3.onValueChange += e => {
            color3.value = e.value;
            Redraw();
        };

        Redraw();
    }
}