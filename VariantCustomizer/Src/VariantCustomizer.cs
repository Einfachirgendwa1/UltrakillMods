using System;
using BepInEx;
using Common;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;
using UnityEngine.UI;
using VariantCustomizer.Bridge;
using static Common.Statics;

namespace VariantCustomizer;

[BepInPlugin("com.einfachirgendwa1.variantCustomizer", "VariantCustomizer", "1.1.0")]
public class VariantCustomizer : BaseUnityPlugin {
    private static readonly int CustomColor1 = Shader.PropertyToID("_CustomColor1");
    private static readonly int CustomColor2 = Shader.PropertyToID("_CustomColor2");
    private static readonly int CustomColor3 = Shader.PropertyToID("_CustomColor3");

    private static readonly Observed<int> ActiveGun = new();
    private static readonly Observed<bool> ActiveAlt = new();
    private static readonly Observed<int> ActiveVariant = new();

    private static readonly string[] WeaponWindows = {
        "Revolver Window",
        "Shotgun Window",
        "Nailgun Window",
        "Railcannon Window",
        "Rocket Launcher Window"
    };

    internal static bool Dirty;

    private static GameObject? lastShop;

    private readonly PluginConfigurator config = InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private BoolField? allowNotUnlocked;
    private GameObject? currentWeaponCache;

    private Gun[] guns = { };
    private BoolField? modEnabled;

    private static VariantColorId ModdedColorId => new(ActiveGun.Value, ActiveAlt.Value, ActiveVariant.Value);
    private static VanillaColorId VanillaColorId => new(ActiveGun.Value, ActiveAlt.Value);

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
        if (Find(Logger, null, "Shop", "Canvas", "Background", "Main Panel", "Weapons") is { } shop) {
            if (!ReferenceEquals(shop, lastShop)) {
                lastShop = shop;

                for (int g = 0; g < guns.Length; g++) {
                    GameObject window = shop.transform.Find(WeaponWindows[g]).gameObject
                                        ?? throw new Exception("Could not find window");

                    PatchShopTerminal(window, g);
                }
            }
        }

        GunControl gunControl = GunControl.Instance;
        if (gunControl is null) return;

        GameObject currentWeapon = gunControl.currentWeapon;
        if (currentWeapon is null || currentWeapon == currentWeaponCache && !Dirty) return;
        currentWeaponCache = currentWeapon;
        Dirty = false;

        GunColorGetter colorGetter = currentWeapon.GetComponentInChildren<GunColorGetter>();
        bool alternate = colorGetter is not null && colorGetter.altVersion;
        SkinnedMeshRenderer[] renderers = currentWeapon.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers) {
            VariantColorId cid = new(gunControl.currentSlotIndex - 1, alternate,
                gunControl.currentVariationIndex);

            GunColors colors = cid.GetColors();
            Logger.LogInfo($"got colors for {cid.ColorId("<num>")}: " + colors);

            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block.SetColor(CustomColor1, colors.Color1);
            block.SetColor(CustomColor2, colors.Color2);
            block.SetColor(CustomColor3, colors.Color3);
            renderer.SetPropertyBlock(block);
        }
    }

    /// <summary>
    ///     Deactivates the GameObject at "Variation Screen/Variations/Info and Color
    ///     Panel/ColorButton".
    ///     Deactivates everything at "Variation Screen/Variations/Variation Panel
    ///     ([Blue/Green/Red])/Equipment/Buttons/*".
    ///     Loads a ColorButton at every "Variation Screen/Variations/Variation Panel
    ///     ([Blue/Green/Red])/Equipment"
    /// </summary>
    /// <param name="window">
    ///     Should be a GameObject at "Shop/Canvas/Background/Main
    ///     Panel/Weapons"
    /// </param>
    /// <param name="gun">The index of the gun</param>
    /// <exception cref="NullReferenceException">
    ///     Any of the GameObjects that should be
    ///     deactivated don't exist
    /// </exception>
    private void PatchShopTerminal(GameObject window, int gun) {
        GameObject variations = window.FindAssertExists(Logger, "Variation Screen", "Variations");
        GameObject colorButton = variations.FindAssertExists(Logger, "Info and Color Panel", "ColorButton");

        GameObject mainWindow = window.FindAssertExists(Logger, "Color Screen", "Main Window");
        GunColorTypeGetter gctg = mainWindow.GetComponent<GunColorTypeGetter>();

        colorButton.SetActive(false);

        for (int i = 0; i <= 2; i++) {
            string panelName = i switch {
                0 => "Blue",
                1 => "Green",
                2 => "Red",
                _ => throw new ArgumentOutOfRangeException()
            };

            GameObject equipment = variations.FindAssertExists(
                Logger,
                $"Variation Panel ({panelName})",
                "Equipment"
            );

            GameObject buttons = equipment.FindAssertExists(Logger, "Buttons");
            foreach (Transform button in buttons.transform) {
                button.gameObject.SetActive(false);
            }

            GameObject newColorButton = Instantiate(colorButton, buttons.transform, false);
            newColorButton.SetActive(true);
            newColorButton.transform.SetAsLastSibling();

            int variant = i;
            newColorButton
                .GetComponent<Button>()
                .Also(button => Assert(button is not null, () => "Color Button not found!"))
                .onClick
                .AddListener(() => {
                        ActiveGun.Value = gun;
                        ActiveVariant.Value = variant + 1;
                        ActiveAlt.Value = gctg.altVersion;

                        Logger.LogFatal("Setting gun to "
                                        + ActiveGun.Value
                                        + ", variant to "
                                        + ActiveVariant.Value
                                        + ", alt to "
                                        + ActiveAlt.Value);
                    }
                );


            Canvas.ForceUpdateCanvases();
        }
    }

    internal void Redraw() {
        if (GunControl.Instance is not { currentWeapon: { } currentWeapon } gunControl) return;

        ActiveGun.Value = gunControl.currentSlotIndex;
        ActiveVariant.Changed.Then(() => Logger.LogInfo($"Active gun set to {ActiveGun.Value}"));

        ActiveVariant.Value = gunControl.currentVariationIndex + 1;
        ActiveVariant.Changed.Then(() => Logger.LogInfo($"Active variation changed to {ActiveVariant.Value}"));

        GunColorTypeGetter gctg = currentWeapon.GetComponentInChildren<GunColorTypeGetter>();

        ActiveAlt.Value = gctg is { altVersion: true };
        ActiveAlt.Changed.Then(() => Logger.LogInfo($"Active alt set to {ActiveAlt.Value}"));
    }
}