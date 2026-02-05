using PluginConfig.API;

namespace VariantCustomizer;

public class Gun {
    private readonly Variant blueVariant;
    private readonly Variant greenVariant;
    private readonly Variant redVariant;

    public Gun(int gun, bool alt) {
        blueVariant = new Variant(gun, alt, 0);
        greenVariant = new Variant(gun, alt, 1);
        redVariant = new Variant(gun, alt, 2);
    }

    public void Subpanel(ConfigPanel parentPanel) {
        blueVariant.Subpanel(new ConfigPanel(parentPanel, "Blue Variant", parentPanel.guid + ".blue"));
        greenVariant.Subpanel(new ConfigPanel(parentPanel, "Green Variant", parentPanel.guid + ".green"));
        redVariant.Subpanel(new ConfigPanel(parentPanel, "Red Variant", parentPanel.guid + ".red"));
    }
}