using PluginConfig.API;

namespace VariantCustomizer;

public class Gun {
    private readonly string name;

    private readonly GunConfiguration mainGun = new();
    private readonly GunConfiguration? alternateGun;

    public Gun(string name, bool hasVariant) {
        this.name = name;

        alternateGun = hasVariant ? new GunConfiguration() : null;
    }

    public VariantConfiguration GetVariantConfig(int variantIndex, bool alternate) {
        return (alternate ? alternateGun! : mainGun).GetVariantConfig(variantIndex);
    }

    public void Subpanel(ConfigPanel parentPanel) {
        mainGun.Subpanel(new ConfigPanel(parentPanel, name, name));
        alternateGun?.Subpanel(new ConfigPanel(parentPanel, $"{name} (Alternate)", $"{name}.alt"));
    }
}