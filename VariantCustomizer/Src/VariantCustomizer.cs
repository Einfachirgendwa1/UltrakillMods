using BepInEx;
using Common;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;

namespace VariantCustomizer;

[BepInPlugin("com.einfachirgendwa1.variantCustomizer", "VariantCustomizer", "1.1.0")]
public class VariantCustomizer : BaseUnityPlugin {
    private static readonly int CustomColor1 = Shader.PropertyToID("_CustomColor1");
    private static readonly int CustomColor2 = Shader.PropertyToID("_CustomColor2");
    private static readonly int CustomColor3 = Shader.PropertyToID("_CustomColor3");

    internal static bool Dirty;

    private readonly PluginConfigurator config = Statics.InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private BoolField? allowNotUnlocked;
    private GameObject? currentWeaponCache;

    private Gun[] guns = { };
    private BoolField? modEnabled;

    private void Awake() {
        modEnabled = new BoolField(config.rootPanel, "Enabled", "enabled", true);

        allowNotUnlocked = new BoolField(
            config.rootPanel,
            "Circumvent having to buy custom colors",
            "allow_when_no_custom_colors",
            true
        );

        guns = new[] {
            new Gun("Revolver", true, 1),
            new Gun("Shotgun", true, 2),
            new Gun("Nailgun", true, 3),
            new Gun("Railgun", false, 4),
            new Gun("Rocket Launcher", false, 5)
        };

        foreach (Gun gun in guns) {
            gun.Subpanel(config.rootPanel);
        }

        modEnabled.onValueChange += _ => Dirty = true;
        allowNotUnlocked.onValueChange += data => {
            Dirty = true;
            foreach (Gun gun in guns) {
                gun.UpdateVisibility(data.value);
            }
        };
    }

    private void Update() {
        GunControl gunControl = GunControl.Instance;
        if (gunControl == null) return;

        GameObject currentWeapon = gunControl.currentWeapon;
        if (currentWeapon == null || (currentWeapon == currentWeaponCache && !Dirty)) return;
        currentWeaponCache = currentWeapon;
        Dirty = false;

        GunColorGetter colorGetter = currentWeapon.GetComponentInChildren<GunColorGetter>();
        bool alternate = colorGetter != null && colorGetter.altVersion;

        Gun gun = guns[gunControl.currentSlotIndex - 1];
        bool useCustomColors = gun.UseCustomColors(allowNotUnlocked!.value) && modEnabled!.value;

        VariantConfiguration variantConfig = gun.GetVariantConfig(gunControl.currentVariationIndex, alternate);
        SkinnedMeshRenderer[] renderers = currentWeapon.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers) {
            GunColorPreset colors = useCustomColors ? variantConfig.GetColorPreset() : gun.GetNormalColor(alternate);

            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block.SetColor(CustomColor1, colors.color1);
            block.SetColor(CustomColor2, colors.color2);
            block.SetColor(CustomColor3, colors.color3);
            renderer.SetPropertyBlock(block);
        }
    }
}