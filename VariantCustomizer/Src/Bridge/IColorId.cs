using UnityEngine;

namespace VariantCustomizer.Bridge;

internal interface IColorId {
    internal string ColorId(string colorNumber);
}

internal static class ColorIdExtensions {
    internal static GunColors GetColors(this IColorId colorId) {
        return new GunColors(GetColor("1"), GetColor("2"), GetColor("3"));

        Color GetColor(string colorNumber) {
            string id = colorId.ColorId(colorNumber);

            return new Color(GetWithId($"{id}r"), GetWithId($"{id}g"), GetWithId($"{id}b"), GetWithId($"{id}a"));

            static float GetWithId(string id) => PrefsManager.Instance.GetFloat(id, 1);
        }
    }

    internal static void SetColors(this IColorId colorId, GunColors value) {
        VariantCustomizer.LogSource.LogWarning($"SET ALL {colorId.ColorId("<num>")} -> {{ {value} }}");

        SetColor("1", value.Color1);
        SetColor("2", value.Color2);
        SetColor("3", value.Color3);

        return;

        void SetColor(string colorNumber, Color color) {
            string id = colorId.ColorId(colorNumber);

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