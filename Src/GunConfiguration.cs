using System;
using PluginConfig.API;

namespace VariantCustomizer;

public class GunConfiguration {
    private readonly VariantConfiguration blueVariant = new();
    private readonly VariantConfiguration greenVariant = new();
    private readonly VariantConfiguration redVariant = new();

    internal VariantConfiguration GetVariantConfig(int variantIndex) {
        return variantIndex switch {
            0 => blueVariant,
            1 => greenVariant,
            2 => redVariant,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void Subpanel(ConfigPanel parentPanel) {
        blueVariant.Subpanel(new ConfigPanel(parentPanel, "Blue Variant", parentPanel.guid + ".blue"));
        greenVariant.Subpanel(new ConfigPanel(parentPanel, "Green Variant", parentPanel.guid + ".green"));
        redVariant.Subpanel(new ConfigPanel(parentPanel, "Red Variant", parentPanel.guid + ".red"));
    }
}