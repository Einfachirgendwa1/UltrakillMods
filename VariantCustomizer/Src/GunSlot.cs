using PluginConfig.API;

namespace VariantCustomizer;

public class GunSlot {
    private readonly Gun? alternateGun;

    private readonly Gun mainGun;
    private readonly string name;
    private readonly int weaponNumber;
    private ConfigPanel? alternateGunPanel;

    private ConfigPanel? mainGunPanel;

    public GunSlot(string name, bool hasVariant, int weaponNumber) {
        this.name = name;
        this.weaponNumber = weaponNumber;
        mainGun = new Gun(weaponNumber - 1, false);

        alternateGun = hasVariant ? new Gun(weaponNumber - 1, true) : null;
    }

    private bool UseCustomColors(bool visibleWithoutUnlock) =>
        visibleWithoutUnlock || GunColorController.Instance.hasUnlockedColors[weaponNumber - 1];

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