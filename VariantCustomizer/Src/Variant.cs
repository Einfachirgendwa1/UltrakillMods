using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;

namespace VariantCustomizer;

public class Variant {
    private readonly bool alt;

    private readonly int gun;
    private readonly int variation;
    private ColorField? color1;
    private ColorField? color2;
    private ColorField? color3;

    public Variant(int gun, bool alt, int variation) {
        this.gun = gun;
        this.alt = alt;
        this.variation = variation;
    }

    internal void Subpanel(ConfigPanel parentPanel) {
        color1 = new ColorField(parentPanel, "Color 1", parentPanel.guid + ".color1", Color.white);
        color2 = new ColorField(parentPanel, "Color 2", parentPanel.guid + ".color2", Color.white);
        color3 = new ColorField(parentPanel, "Color 3", parentPanel.guid + ".color3", Color.white);

        VariantColorId cid = new(gun, alt, variation);
        color1.onValueChange += data => cid.SetColors(new GunColorPreset(data.value, color2.value, color3.value));
        color2.onValueChange += data => cid.SetColors(new GunColorPreset(color1.value, data.value, color3.value));
        color3.onValueChange += data => cid.SetColors(new GunColorPreset(color1.value, color2.value, data.value));
    }
}