using PluginConfig.API;

namespace VariantCustomizer;

public class Gun {
    private readonly string name;
    private readonly int weaponNumber;

    private readonly GunConfiguration mainGun = new();
    private readonly GunConfiguration? alternateGun;

    private ConfigPanel? mainGunPanel;
    private ConfigPanel? alternateGunPanel;

    internal bool UseCustomColors(bool visibleWithoutUnlock) {
        return visibleWithoutUnlock || GunColorController.Instance.hasUnlockedColors[weaponNumber - 1];
    }

    public Gun(string name, bool hasVariant, int weaponNumber) {
        this.name = name;
        this.weaponNumber = weaponNumber;

        alternateGun = hasVariant ? new GunConfiguration() : null;
    }

    public VariantConfiguration GetVariantConfig(int variantIndex, bool alternate) {
        return (alternate ? alternateGun! : mainGun).GetVariantConfig(variantIndex);
    }

    public GunColorPreset GetNormalColor(bool altVersion) {
        return altVersion switch {
            false => GunColorController.Instance!.currentColors[weaponNumber - 1],
            true  => GunColorController.Instance!.currentAltColors[weaponNumber - 1]
        };
    }

    public void Subpanel(ConfigPanel parentPanel) {
        mainGunPanel = new ConfigPanel(parentPanel, name, name);
        mainGun.Subpanel(mainGunPanel);

        if (alternateGun != null) {
            alternateGunPanel = new ConfigPanel(parentPanel, $"{name} (Alternate)", $"{name}.alt");
            alternateGun.Subpanel(alternateGunPanel);
        }
    }

    public void UpdateVisibility(bool visibleWithoutUnlock) {
        bool visible = UseCustomColors(visibleWithoutUnlock);

        mainGunPanel!.hidden = !visible;
        if (alternateGunPanel != null) {
            alternateGunPanel.hidden = !visible;
        }
    }
}