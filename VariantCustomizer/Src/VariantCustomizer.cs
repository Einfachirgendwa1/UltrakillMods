using System;
using BepInEx;
using BepInEx.Logging;
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
    public static VariantCustomizer? Instance;

    public static ManualLogSource LogSource;

    private static readonly string[] WeaponWindows = {
        "Revolver Window",
        "Shotgun Window",
        "Nailgun Window",
        "Railcannon Window",
        "Rocket Launcher Window"
    };

    private static readonly Observed<int> ActiveGun = new();
    private static readonly Observed<bool> ActiveAlt = new();
    private static readonly Observed<int> ActiveVariant = new();

    private static readonly Observed<GunColors> VanillaColors = new();
    private static readonly Observed<GunColors> ModdedColors = new();

    private static readonly int CustomColor1 = Shader.PropertyToID("CustomColor1");
    private static readonly int CustomColor2 = Shader.PropertyToID("CustomColor2");
    private static readonly int CustomColor3 = Shader.PropertyToID("CustomColor3");

    private readonly PluginConfigurator config = InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private readonly Observed<GameObject?> shop = new();

    private BoolField? allowNotUnlocked;

    private Gun[] guns = { };
    private BoolField? modEnabled;

    private static VariantColorId ModdedColorId => new(ActiveGun.Value, ActiveAlt.Value, ActiveVariant.Value);
    private static VanillaColorId VanillaColorId => new(ActiveGun.Value, ActiveAlt.Value);

    private void Start() {
        LogSource = Logger;

        Instance = this;
        modEnabled = new BoolField(config.rootPanel, "Enabled", "enabled", true);

        allowNotUnlocked = new BoolField(
            config.rootPanel,
            "Circumvent having to buy custom colors",
            "allow_when_no_custom_colors",
            true
        );

        guns = new[] {
            new Gun("Revolver", 1, true),
            new Gun("Shotgun", 2, true),
            new Gun("Nailgun", 3, true),
            new Gun("Railgun", 4, false),
            new Gun("Rocket Launcher", 5, false)
        };

        foreach (Gun gun in guns) {
            gun.Subpanel(config.rootPanel, Logger);
        }

        // If the user changes any of these settings, we probably need to redraw weapon colors next frame.
        modEnabled.onValueChange += _ => Redraw();
        allowNotUnlocked.onValueChange += data => {
            Redraw();
            guns.ForEach(gun => gun.UpdateVisibility(data.value));
        };
    }

    private void Update() {
        Redraw();
        UpdateRenderers();

        shop.Value = Find(Logger, null, "Shop", "Canvas", "Background", "Main Panel", "Weapons");
        if (!shop.HasValue() || !shop.Changed) return;

        GameObject[] windows = Array.ConvertAll(
            WeaponWindows,
            window => shop.Value!.transform.Find(window).gameObject
                      ?? throw new Exception($"Could not find window {window}")
        );

        for (int gun = 0; gun < guns.Length; gun++) {
            PatchShopTerminal(windows[gun], gun);
        }
    }

    private void CheckVanillaColors() {
        VanillaColors.Value = VanillaColorId.GetColors();

        if (VanillaColors.Changed) {
            Logger.LogInfo($"Setting Modded Colors to Vanilla Colors: {VanillaColors.Value}");
            ModdedColorId.SetColors(VanillaColors.Value);
        }
    }

    internal void CheckModdedColors() {
        ModdedColors.Value = ModdedColorId.GetColors();

        Logger.LogInfo($"Setting Vanilla Colors to Modded Colors: {ModdedColors.Value}");
        VanillaColorId.SetColors(ModdedColors.Value);
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

        if (ActiveVariant.Changed || ActiveGun.Changed || ActiveAlt.Changed) CheckModdedColors();
        else CheckVanillaColors();

        // GunColorController.Instance.UpdateGunColors();
        // gctg?.UpdatePreview();

        UpdateRenderers();
    }

    private void UpdateRenderers() {
        if (GunControl.Instance is not { currentWeapon: { } currentWeapon }) return;

        VanillaColors.Value = VanillaColorId.GetColors();
        foreach (SkinnedMeshRenderer renderer in currentWeapon.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            Logger.LogInfo($"Rendering {VanillaColors.Value} onto {renderer.name}");

            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block.SetColor(CustomColor1, VanillaColors.Value.Color1);
            block.SetColor(CustomColor2, VanillaColors.Value.Color2);
            block.SetColor(CustomColor3, VanillaColors.Value.Color3);
            renderer.SetPropertyBlock(block);
        }
    }
}