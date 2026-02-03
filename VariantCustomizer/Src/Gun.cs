using BepInEx.Logging;
using JetBrains.Annotations;
using PluginConfig.API;
using VariantCustomizer.Bridge;

namespace VariantCustomizer;

public class Gun {
    private readonly GunConfiguration? alternateGun;
    private readonly int gun;

    private readonly GunConfiguration mainGun;
    private readonly string name;
    private ConfigPanel? alternateGunPanel;

    private ConfigPanel? mainGunPanel;

    internal Gun(string name, int gun, bool hasVariant) {
        this.name = name;
        this.gun = gun;

        mainGun = new GunConfiguration(new VanillaColorId(gun, false));
        alternateGun = hasVariant ? new GunConfiguration(new VanillaColorId(gun, true)) : null;
    }

    internal bool UseCustomColors(bool visibleWithoutUnlock) {
        return visibleWithoutUnlock || GunColorController.Instance.hasUnlockedColors[gun - 1];
    }

    internal VariantConfiguration GetVariantConfig(int variantIndex, bool alternate) {
        return (alternate ? alternateGun! : mainGun).Variant(variantIndex);
    }

    internal GunColorPreset GetNormalColor(bool altVersion) {
        return altVersion switch {
            false => GunColorController.Instance!.currentColors[gun - 1],
            true  => GunColorController.Instance!.currentAltColors[gun - 1]
        };
    }

    internal void Subpanel(ConfigPanel parentPanel, ManualLogSource logger) {
        mainGunPanel = new ConfigPanel(parentPanel, name, name);
        mainGun.Subpanel(mainGunPanel, logger);

        if (alternateGun != null) {
            alternateGunPanel = new ConfigPanel(parentPanel, $"{name} (Alternate)", $"{name}.alt");
            alternateGun.Subpanel(alternateGunPanel, logger);
        }
    }

    internal void UpdateVisibility(bool visibleWithoutUnlock) {
        bool visible = UseCustomColors(visibleWithoutUnlock);

        mainGunPanel!.hidden = !visible;
        if (alternateGunPanel != null) {
            alternateGunPanel.hidden = !visible;
        }
    }

    [MustUseReturnValue]
    internal GunConfiguration Alternate(bool alternate) {
        return alternate ? alternateGun! : mainGun;
    }
}