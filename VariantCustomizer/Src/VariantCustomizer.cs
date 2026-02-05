using System;
using BepInEx;
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

    private int lastClickedVariant = -1;

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

                colorSetters = shop.GetComponentsInChildren<GunColorSetter>(true);
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
        Logger.LogInfo("Applying mod colors");

        dirty = true;
        if (colorSetters is null) {
            Logger.LogInfo("Skipping applying colors to shop terminal because colorSetters is null");
            return;
        }

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
            newColorButton.GetComponent<Button>().onClick.AddListener(() => lastClickedVariant = variant);
        }

        GameObject colors = Find(Logger, mainWindow, "Window", "Custom", "Colors")
                            ?? throw new NullReferenceException("Could not find colors panel");


        GameObject sliders1 = colors.FindAssertExists(Logger, "Color 1", "Sliders");
        sliders1.FindAssertExists(Logger, "Red", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color1.r = e;
        });
        sliders1.FindAssertExists(Logger, "Green", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color1.g = e;
        });
        sliders1.FindAssertExists(Logger, "Blue", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color1.b = e;
        });
        sliders1.FindAssertExists(Logger, "Metal", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color1.a = e;
        });

        GameObject sliders2 = colors.FindAssertExists(Logger, "Color 2", "Sliders");
        sliders2.FindAssertExists(Logger, "Red", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color2.r = e;
        });
        sliders2.FindAssertExists(Logger, "Green", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color2.g = e;
        });
        sliders2.FindAssertExists(Logger, "Blue", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color2.b = e;
        });
        sliders2.FindAssertExists(Logger, "Metal", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color2.a = e;
        });

        GameObject sliders3 = colors.FindAssertExists(Logger, "Color 3", "Sliders");
        sliders3.FindAssertExists(Logger, "Red", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color3.r = e;
        });
        sliders3.FindAssertExists(Logger, "Green", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color3.g = e;
        });
        sliders3.FindAssertExists(Logger, "Blue", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color3.b = e;
        });
        sliders3.FindAssertExists(Logger, "Metal", "Slider").GetComponent<Slider>().onValueChanged.AddListener(e => {
            variants[gun, gctg.altVersion ? 1 : 0, lastClickedVariant].color3.a = e;
        });


        Canvas.ForceUpdateCanvases();
    }
}