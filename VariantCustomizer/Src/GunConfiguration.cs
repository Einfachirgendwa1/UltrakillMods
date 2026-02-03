using System;
using BepInEx.Logging;
using JetBrains.Annotations;
using PluginConfig.API;
using VariantCustomizer.Bridge;

namespace VariantCustomizer;

public class GunConfiguration {
    private readonly VariantConfiguration blueVariant;
    private readonly VariantConfiguration greenVariant;
    private readonly VariantConfiguration redVariant;


    internal GunConfiguration(VanillaColorId vanilla) {
        blueVariant = new VariantConfiguration(new VariantColorId(vanilla, 1), vanilla);
        greenVariant = new VariantConfiguration(new VariantColorId(vanilla, 2), vanilla);
        redVariant = new VariantConfiguration(new VariantColorId(vanilla, 3), vanilla);
    }

    [MustUseReturnValue]
    internal VariantConfiguration Variant(int variantIndex) {
        return variantIndex switch {
            0 => blueVariant,
            1 => greenVariant,
            2 => redVariant,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal void Subpanel(ConfigPanel parentPanel, ManualLogSource logger) {
        blueVariant.Subpanel(new ConfigPanel(parentPanel, "Blue Variant", parentPanel.guid + ".blue"), logger);
        greenVariant.Subpanel(new ConfigPanel(parentPanel, "Green Variant", parentPanel.guid + ".green"), logger);
        redVariant.Subpanel(new ConfigPanel(parentPanel, "Red Variant", parentPanel.guid + ".red"), logger);
    }
}