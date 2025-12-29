using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using PluginConfig.API;

namespace Common {
    public static class Statics {
        public static readonly ManualLogSource logSource = new ManualLogSource("Mods");

        public static PluginConfigurator InitPluginConfig(string displayName, string guid) {
            string directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

            PluginConfigurator config = PluginConfigurator.Create(displayName, guid);
            config.SetIconWithURL($"file://{Path.Combine(directory, "icon.png")}");

            return config;
        }

        public static Stream GetEmbeddedResource(string path) {
            string fullPath = $"UltrakillMods.EmbeddedResources.{path}";

            Stream? stream = Assembly.GetCallingAssembly().GetManifestResourceStream(fullPath);

            Assert(stream != null, () => $"Could not find embedded resource: {path} ('{fullPath}')");
            return stream;
        }

        public static void Assert([DoesNotReturnIf(false)] bool cond, Func<string>? msg = null) {
            if (!cond) {
                string fullMessage = "Assertion failed" + (msg != null ? $": {msg.Invoke()}" : "!");
                logSource.LogError(fullMessage);
                throw new Exception(fullMessage);
            }
        }

        public static R PrivateField<T, R>(string fieldName, T instance) {
            FieldInfo? fieldInfo = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert(fieldInfo != null, () => $"Could not find field: {fieldName}");

            return (R)fieldInfo.GetValue(instance);
        }
    }
}