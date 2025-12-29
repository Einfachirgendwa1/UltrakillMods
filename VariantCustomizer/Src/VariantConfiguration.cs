using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;

namespace VariantCustomizer;

public class VariantConfiguration {
    private static readonly int CustomColor1 = Shader.PropertyToID("_CustomColor1");
    private static readonly int CustomColor2 = Shader.PropertyToID("_CustomColor2");
    private static readonly int CustomColor3 = Shader.PropertyToID("_CustomColor3");

    private ColorField? color1;
    private ColorField? color2;
    private ColorField? color3;

    internal void ApplyToRenderer(Renderer renderer) {
        MaterialPropertyBlock block = new();
        renderer.GetPropertyBlock(block);
        block.SetColor(CustomColor1, color1!.value);
        block.SetColor(CustomColor2, color2!.value);
        block.SetColor(CustomColor3, color3!.value);
        renderer.SetPropertyBlock(block);
    }

    private static void MarkDirty(ColorField.ColorValueChangeEvent _) {
        VariantCustomizer.Dirty = true;
    }

    internal void Subpanel(ConfigPanel parentPanel) {
        color1 = new ColorField(parentPanel, "Color 1", parentPanel.guid + ".color1", Color.white);
        color2 = new ColorField(parentPanel, "Color 2", parentPanel.guid + ".color2", Color.white);
        color3 = new ColorField(parentPanel, "Color 3", parentPanel.guid + ".color3", Color.white);

        color1.onValueChange += MarkDirty;
        color2.onValueChange += MarkDirty;
        color3.onValueChange += MarkDirty;
    }
}