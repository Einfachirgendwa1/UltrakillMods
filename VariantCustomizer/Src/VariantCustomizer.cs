using System;
using BepInEx;
using Common;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;
using UnityEngine.UI;
using static Common.Statics;

// ReSharper disable Unity.InefficientMultidimensionalArrayUsage

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

    private static bool dirty;

    private static GameObject? lastShop;

    private static readonly string[] Guns = {
        "Revolver",
        "Shotgun",
        "Nailgun",
        "Railgun",
        "Rocket Launcher"
    };

    private readonly PluginConfigurator config = InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private readonly GunColorPreset[,,] variants = new GunColorPreset[5, 2, 3];

    private BoolField? allowNotUnlocked;

    private GunColorSetter[]? colorSetters;
    private GameObject? currentWeaponCache;

    private BoolField? modEnabled;

    private void Awake() {
        modEnabled = new BoolField(config.rootPanel, "Enabled", "enabled", true);

        allowNotUnlocked = new BoolField(
            config.rootPanel,
            "Circumvent having to buy custom colors",
            "allow_when_no_custom_colors",
            true
        );

        for (int gun = 0; gun < 5; gun++) {
            string gunName = Guns[gun];
            ConfigPanel slotPanel = new(config.rootPanel, gunName, gunName);

            if (!HasAlt(gun)) MakeConfig(gun, 0, slotPanel);
            else {
                ConfigPanel noAlt = new(slotPanel, $"Base {gunName}", $"{gunName}.base");
                ConfigPanel yesAlt = new(slotPanel, $"Alternate {gunName}", $"{gunName}.alt");

                MakeConfig(gun, 0, noAlt);
                MakeConfig(gun, 1, yesAlt);
            }

            continue;

            void MakeConfig(int thisGun, int alt, ConfigPanel parentPanel) {
                for (int variant = 0; variant < 3; variant++) {
                    int localVariant = variant;

                    string variantName = variant switch {
                        0 => "Blue Variation",
                        1 => "Green Variation",
                        2 => "Red Variation",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    ConfigPanel variantPanel = new(parentPanel, variantName, $"{parentPanel.guid}.variant{variant}");

                    ColorField color1 = new(variantPanel, "Color 1", variantPanel.guid + ".color1", Color.white);
                    ColorField color2 = new(variantPanel, "Color 2", variantPanel.guid + ".color2", Color.white);
                    ColorField color3 = new(variantPanel, "Color 3", variantPanel.guid + ".color3", Color.white);

                    variants[gun, alt, variant] = new GunColorPreset(color1.value, color2.value, color3.value);

                    color1.onValueChange += data => variants[thisGun, alt, localVariant].color1 = data.value;
                    color2.onValueChange += data => variants[thisGun, alt, localVariant].color2 = data.value;
                    color3.onValueChange += data => variants[thisGun, alt, localVariant].color3 = data.value;

                    color1.onValueChange += _ => ApplyModColors();
                    color2.onValueChange += _ => ApplyModColors();
                    color3.onValueChange += _ => ApplyModColors();
                }

                modEnabled.onValueChange += _ => dirty = true;
                allowNotUnlocked.onValueChange += _ => dirty = true;
            }
        }
    }

    private void Update() {
        if (Find(Logger, null, "Shop", "Canvas", "Background", "Main Panel", "Weapons") is { } shop) {
            if (!ReferenceEquals(shop, lastShop)) {
                lastShop = shop;

                colorSetters = shop.GetComponentsInChildren<GunColorSetter>();
                for (int g = 0; g < 5; g++) {
                    GameObject window = shop.transform.Find(WeaponWindows[g]).gameObject
                                        ?? throw new Exception("Could not find window");

                    PatchShopTerminal(window, g);
                }
            }
        }

        GunControl gunControl = GunControl.Instance;
        if (gunControl is null) return;

        GameObject currentWeapon = gunControl.currentWeapon;
        if (currentWeapon is null || currentWeapon == currentWeaponCache && !dirty) return;
        currentWeaponCache = currentWeapon;
        dirty = false;

        GunColorGetter colorGetter = currentWeapon.GetComponentInChildren<GunColorGetter>();
        bool alternate = colorGetter is not null && colorGetter.altVersion;
        SkinnedMeshRenderer[] renderers = currentWeapon.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers) {
            int slot = gunControl.currentSlotIndex - 1;
            if (slot >= variants.GetLength(0) || slot < 0) {
                Logger.LogWarning($"Gun index out of bounds ({slot} is >= {variants.GetLength(0)} or < 0)!");
                continue;
            }

            int variation = gunControl.currentVariationIndex;
            if (variation >= variants.GetLength(2) || variation < 0) {
                Logger.LogWarning(
                    $"Variation index out of bounds ({variation} is >= {variants.GetLength(2)} or < 0)!");
                continue;
            }

            GunColorPreset colors = variants[slot, alternate ? 1 : 0, variation];
            Logger.LogInfo(
                $"Colors of slot={slot}, alt={alternate}, variation={variation} is {colors.color1}, {colors.color2}, {colors.color3}");

            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block.SetColor(CustomColor1, colors.color1);
            block.SetColor(CustomColor2, colors.color2);
            block.SetColor(CustomColor3, colors.color3);
            renderer.SetPropertyBlock(block);
        }
    }

    private void ApplyModColors() {
        dirty = true;
        if (colorSetters is null) return;

        for (int gun = 0; gun < variants.GetLength(0); gun++) {
            for (int alt = 0; alt < variants.GetLength(1); alt++) {
                for (int variant = 0; variant < variants.GetLength(2); variant++) {
                    if (alt == 1 && !HasAlt(gun)) continue;

                    GunColorPreset preset = variants[gun, alt, variant];
                    Color[] colors = {
                        preset.color1,
                        preset.color2,
                        preset.color3
                    };

                    for (int color = 0; color < colors.Length; color++) {
                        PrefsManager.Instance.SetFloat($"{gun}.{color}.{(alt == 0 ? "" : "a")}r", colors[color].r);
                        PrefsManager.Instance.SetFloat($"{gun}.{color}.{(alt == 0 ? "" : "a")}g", colors[color].g);
                        PrefsManager.Instance.SetFloat($"{gun}.{color}.{(alt == 0 ? "" : "a")}b", colors[color].b);
                        PrefsManager.Instance.SetFloat($"{gun}.{color}.{(alt == 0 ? "" : "a")}a", colors[color].a);
                    }
                }
            }
        }

        Logger.LogInfo($"updating {colorSetters.Length} sliders");
        colorSetters.ForEach(setter => setter.UpdateSliders());
    }

    private static bool HasAlt(int gun) => gun < 3;

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
}