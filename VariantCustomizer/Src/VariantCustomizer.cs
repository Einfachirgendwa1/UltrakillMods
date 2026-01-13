using System;
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

    /// <summary>
    ///     If true the weapons colors will refresh next frame even if the weapon itself hasn't changed.
    ///     This is useful when e.g. the user has changed some setting, and we need to redraw.
    /// </summary>
    internal static bool Dirty;

    private static readonly string[] WeaponWindows = {
        "Revolver Window", "Shotgun Window", "Nailgun Window", "Railcannon Window", "Rocket Launcher Window"
    };

    private static readonly string[] VariationPanelNames = { "Blue", "Green", "Red" };

    private readonly PluginConfigurator config = Statics.InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    /// <summary>
    ///     Whether we should change colors of all weapons, even the ones that don't have custom colors unlocked yet.
    /// </summary>
    private BoolField? allowNotUnlocked;

    /// <summary>
    ///     The shop from last frame. If the shop didn't change, we also don't need to patch it.
    /// </summary>
    /// <seealso cref="Dirty" />
    private GameObject? currentShopCache;

    /// <summary>
    ///     The weapon that was held last frame. If we hold the same weapon multiple frames in a row, we only need to change
    ///     colors the first time.
    /// </summary>
    /// <seealso cref="Dirty" />
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

        // If the user changes any of these settings, we probably need to redraw weapon colors next frame.
        modEnabled.onValueChange += _ => Dirty = true;
        allowNotUnlocked.onValueChange += data => {
            Dirty = true;
            foreach (Gun gun in guns) {
                gun.UpdateVisibility(data.value);
            }
        };
    }

    private void Update() {
        GameObject? shop = Statics.Find(Logger, null, "Shop", "Canvas", "Background", "Main Panel", "Weapons");

        bool shopIsOld = shop == currentShopCache;
        currentShopCache = shop;

        if (shop == null || shopIsOld) return;

        GameObject[] windows = Array.ConvertAll(
            WeaponWindows,
            window => Statics.GetChild(shop, window) ?? throw new Exception($"Could not find window {window}")
        );

        foreach (GameObject window in windows) {
            PatchShopTerminal(window);
        }
    }


    /// <summary>
    ///     Deactivates the GameObject at "Variation Screen/Variations/Info and Color Panel/ColorButton".
    ///     Deactivates everything at "Variation Screen/Variations/Variation Panel ([Blue/Green/Red])/Equipment/Buttons/*".
    ///     Loads a ColorButton at every "Variation Screen/Variations/Variation Panel ([Blue/Green/Red])/Equipment"
    /// </summary>
    /// <param name="window">Should be a GameObject at "Shop/Canvas/Background/Main Panel/Weapons"</param>
    /// <exception cref="NullReferenceException">Any of the GameObjects that should be deactivated don't exist</exception>
    private void PatchShopTerminal(GameObject window) {
        GameObject variations = Statics.FindAssertExists(Logger, window, "Variation Screen", "Variations");
        GameObject colorButton = Statics.FindAssertExists(Logger, variations, "Info and Color Panel", "ColorButton");

        colorButton.SetActive(false);

        foreach (string panelName in VariationPanelNames) {
            GameObject equipment = Statics.FindAssertExists(
                Logger,
                variations,
                $"Variation Panel ({panelName})",
                "Equipment"
            );

            GameObject buttons = Statics.FindAssertExists(Logger, equipment, "Buttons");
            foreach (Transform button in buttons.transform) {
                button.gameObject.SetActive(false);
            }

            GameObject newButton = Instantiate(colorButton, buttons.transform, false);
            newButton.SetActive(true);
            newButton.transform.SetAsLastSibling();

            Canvas.ForceUpdateCanvases();
        }

        variations.PrintSceneTree(Logger);
    }


    private void Old() {
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