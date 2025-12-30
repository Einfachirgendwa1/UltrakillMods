using BepInEx;
using Common;
using PluginConfig.API;
using UnityEngine;

namespace VariantCustomizer;

[BepInPlugin("com.einfachirgendwa1.variantCustomizer", "VariantCustomizer", "v1.0.0")]
public class VariantCustomizer : BaseUnityPlugin {
    internal static bool Dirty;

    private readonly PluginConfigurator config = Statics.InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private readonly Gun[] guns = {
        new("Revolver", true),
        new("Shotgun", true),
        new("Nailgun", true),
        new("Railgun", false),
        new("Rocket Launcher", false)
    };

    private GameObject? currentWeaponCache;

    private void Awake() {
        foreach (Gun gun in guns) {
            gun.Subpanel(config.rootPanel);
        }
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
        VariantConfiguration variantConfig = gun.GetVariantConfig(gunControl.currentVariationIndex, alternate);

        SkinnedMeshRenderer[] renderers = currentWeapon.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers) {
            variantConfig.ApplyToRenderer(renderer);
        }
    }
}