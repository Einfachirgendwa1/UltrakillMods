using UnityEngine;

namespace VariantCustomizer;

internal class VariantColorId {
    private readonly string postfix;
    private readonly string prefix;

    public VariantColorId(int gun, bool alt, int variant) {
        prefix = $"gunColor.{gun}.";
        postfix = $".variantCustomizer{variant}{(alt ? ".a" : "")}";
    }

    public string ColorId(string colorNumber) => $"{prefix}{colorNumber}{postfix}";

    internal GunColorPreset GetColors() {
        return new GunColorPreset(GetColor("1"), GetColor("2"), GetColor("3"));

        Color GetColor(string colorNumber) {
            string id = ColorId(colorNumber);

            return new Color(GetWithId($"{id}r"), GetWithId($"{id}g"), GetWithId($"{id}b"), GetWithId($"{id}a"));

            static float GetWithId(string id) => PrefsManager.Instance.GetFloat(id, 1);
        }
    }

    internal void SetColors(GunColorPreset value) {
        SetColor("1", value.color1);
        SetColor("2", value.color2);
        SetColor("3", value.color3);

        return;

        void SetColor(string colorNumber, Color color) {
            string id = ColorId(colorNumber);

            SetWithId($"{id}r", color.r);
            SetWithId($"{id}g", color.g);
            SetWithId($"{id}b", color.b);
            SetWithId($"{id}a", color.a);
            return;

            static void SetWithId(string id, float value) {
                PrefsManager.Instance.SetFloat(id, value);
            }
        }
    }
}