using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using PluginConfig.API;

namespace ModTemplate {
    [BepInPlugin("templateGuid", "ModTemplate", "1.0.0.0")]
    public class ModTemplate : BaseUnityPlugin {
        private readonly Harmony harmony = new Harmony("ModTemplate");

        private PluginConfigurator? config;

        private void Awake() {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            config = PluginConfigurator.Create("Mod Template", "templateGuid");
            config.SetIconWithURL($"file://{Path.Combine(directory, "icon.png")}");
        }
    }
}