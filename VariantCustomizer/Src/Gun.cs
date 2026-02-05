using PluginConfig.API;

namespace VariantCustomizer;

public class Gun {
    private readonly GunConfiguration? alternateGun;

    private readonly GunConfiguration mainGun;
    private readonly string name;
    private readonly int weaponNumber;
    private ConfigPanel? alternateGunPanel;

    private ConfigPanel? mainGunPanel;

    public Gun(string name, bool hasVariant, int weaponNumber) {
        this.name = name;
        this.weaponNumber = weaponNumber;
        mainGun = new GunConfiguration(weaponNumber - 1, false);

        alternateGun = hasVariant ? new GunConfiguration(weaponNumber - 1, true) : null;
    }

    internal bool UseCustomColors(bool visibleWithoutUnlock) =>
        visibleWithoutUnlock || GunColorController.Instance.hasUnlockedColors[weaponNumber - 1];

    public VariantConfiguration GetVariantConfig(int variantIndex, bool alternate) =>
        (alternate ? alternateGun! : mainGun).GetVariantConfig(variantIndex);

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