namespace VariantCustomizer.Bridge;

internal class VariantColorId : IColorId {
    private readonly string postfix;
    private readonly string prefix;

    public VariantColorId(int gun, bool alt, int variant) {
        prefix = $"gunColor.{gun}.";
        postfix = $".variantCustomizer{variant}{(alt ? ".a" : "")}";

        VariantCustomizer.LogSource.LogInfo("Creating new VariantColorId instance with "
                                            + $"gun={gun}, alt={alt}, variant={variant} -> "
                                            + $"prefix='{prefix}', postfix='{postfix}'");
    }

    public VariantColorId(VanillaColorId vanilla, int variant) : this(vanilla.Gun, vanilla.Alt, variant) { }

    public string ColorId(string colorNumber) {
        return $"{prefix}{colorNumber}{postfix}";
    }
}