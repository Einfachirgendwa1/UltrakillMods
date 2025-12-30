using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;

namespace VariantCustomizer;

public class VariantConfiguration {
    private ColorField? color1;
    private ColorField? color2;
    private ColorField? color3;

    internal GunColorPreset GetColorPreset() {
        return new GunColorPreset(color1!.value, color2!.value, color3!.value);
    }

    private static void MarkDirty(ColorField.ColorValueChangeEvent _) {
        VariantCustomizer.Dirty = true;
    }

    internal void Subpanel(ConfigPanel parentPanel) {
        color1 = new ColorField(parentPanel, "Color 1", parentPanel.guid + ".color1", Color.white);
        color2 = new ColorField(parentPanel, "Color 2", parentPanel.guid + ".color2", Color.white);
        color3 = new ColorField(parentPanel, "Color 3", parentPanel.guid + ".color3", Color.white);

        color1.onValueChange += MarkDirty;
        color2.onValueChange += MarkDirty;
        color3.onValueChange += MarkDirty;
    }
}