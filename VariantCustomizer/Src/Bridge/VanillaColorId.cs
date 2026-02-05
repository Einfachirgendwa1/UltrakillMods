namespace VariantCustomizer.Bridge;

public class VanillaColorId : IColorId {
    internal readonly bool Alt;
    internal readonly int Gun;
    private readonly string postfix;
    private readonly string prefix;

    public VanillaColorId(int gun, bool alt) {
        Alt = alt;
        Gun = gun;
        prefix = $"gunColor.{gun}.";
        postfix = alt ? ".a" : ".";
    }

    public string ColorId(string colorNumber) {
        return $"{prefix}{colorNumber}{postfix}";
    }
}