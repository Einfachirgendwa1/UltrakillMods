using System;
using System.Collections.Generic;
using BepInEx;
using Common;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;
using UnityEngine.UI;

namespace VariantCustomizer;

[BepInPlugin("com.einfachirgendwa1.variantCustomizer", "VariantCustomizer", "1.1.0")]
public class VariantCustomizer : BaseUnityPlugin {
    public static VariantCustomizer? Instance;

    private static readonly int CustomColor1 = Shader.PropertyToID("_CustomColor1");
    private static readonly int CustomColor2 = Shader.PropertyToID("_CustomColor2");
    private static readonly int CustomColor3 = Shader.PropertyToID("_CustomColor3");

    private static readonly string[] WeaponWindows = {
        "Revolver Window", "Shotgun Window", "Nailgun Window", "Railcannon Window", "Rocket Launcher Window"
    };

    private readonly PluginConfigurator config = Statics.InitPluginConfig(
        "Variant Customizer",
        "com.einfachirgendwa1.variantCustomizer"
    );

    private readonly Dictionary<string, GameObject> previewWeaponCache = new();

    private BoolField? allowNotUnlocked;
    private GameObject? currentShopCache;
    private GameObject? currentWeaponCache;

    private Gun[] guns = { };
    private BoolField? modEnabled;

    private AssetBundle? weaponsBundle;

    private void Awake() {
        Instance = this;
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
        modEnabled.onValueChange += _ => Redraw();
        allowNotUnlocked.onValueChange += data => {
            Redraw();
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

        for (int gun = 1; gun <= 5; gun++) {
            PatchShopTerminal(windows[gun - 1], gun);
        }
    }


    /// <summary>
    ///     Deactivates the GameObject at "Variation Screen/Variations/Info and Color Panel/ColorButton".
    ///     Deactivates everything at "Variation Screen/Variations/Variation Panel ([Blue/Green/Red])/Equipment/Buttons/*".
    ///     Loads a ColorButton at every "Variation Screen/Variations/Variation Panel ([Blue/Green/Red])/Equipment"
    /// </summary>
    /// <param name="window">Should be a GameObject at "Shop/Canvas/Background/Main Panel/Weapons"</param>
    /// <param name="gun">The index of the gun</param>
    /// <exception cref="NullReferenceException">Any of the GameObjects that should be deactivated don't exist</exception>
    private void PatchShopTerminal(GameObject window, int gun) {
        GameObject variations = Statics.FindAssertExists(Logger, window, "Variation Screen", "Variations");
        GameObject colorButton = Statics.FindAssertExists(Logger, variations, "Info and Color Panel", "ColorButton");

        colorButton.SetActive(false);

        for (int i = 1; i <= 3; i++) {
            string panelName = i switch {
                1 => "Blue",
                2 => "Green",
                3 => "Red",
                _ => throw new ArgumentOutOfRangeException()
            };

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

            int variation = i;
            GameObject weaponModel = Statics.FindAssertExists(
                Logger,
                window,
                "Color Screen",
                "Main Window",
                "Preview Window",
                "Weapon Model"
            );


            foreach (Component component in newButton.GetComponents<Component>()) {
                Logger.LogInfo(component.GetType().Name);
            }

            newButton
                .GetComponent<Button>()
                .Also(button => Statics.Assert(button != null, () => "Button not found!"))
                .onClick
                .AddListener(() => UpdateWeaponPreview(weaponModel, gun, variation, false));

            Canvas.ForceUpdateCanvases();
        }
    }

    private void UpdateWeaponPreview(GameObject weaponModel, int gun, int variation, bool alternate) {
        GameObject preview = LoadWeaponPreview(gun, variation, alternate);
        foreach (Transform t in weaponModel.transform) {
            Destroy(t.gameObject);
        }

        Instantiate(preview, weaponModel.transform, false);
    }

    private GameObject LoadWeaponPreview(int gun, int variation, bool alternate) {
        string weaponPath = $"{gun}{variation}{(alternate ? "1" : "0")}";
        if (previewWeaponCache.TryGetValue(weaponPath, out GameObject cached)) return cached;

        weaponsBundle ??= LoadWeaponsBundle();
        return weaponsBundle.LoadAsset<GameObject>(weaponPath).Also(go => previewWeaponCache.Add(weaponPath, go));
    }

    private static AssetBundle LoadWeaponsBundle() {
        string assetBundlePath = Statics.InExeDir("weapons.assetbundle");
        return AssetBundle.LoadFromFile(assetBundlePath);
    }

    internal void Redraw() {
        GunControl gunControl = GunControl.Instance;
        if (gunControl == null) return;

        GameObject currentWeapon = gunControl.currentWeapon;
        if (currentWeapon == null || currentWeapon == currentWeaponCache) return;
        currentWeaponCache = currentWeapon;

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

public static class Ext {
    public static T Also<T>(this T thing, Action<T> action) {
        action(thing);
        return thing;
    }
}