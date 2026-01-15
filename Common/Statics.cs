using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using PluginConfig.API;
using UnityEngine;

namespace Common {
    public static class Statics {
        public static PluginConfigurator InitPluginConfig(string displayName, string guid) {
            string directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

            PluginConfigurator config = PluginConfigurator.Create(displayName, guid);
            config.SetIconWithURL($"file://{Path.Combine(directory, "icon.png")}");

            return config;
        }

        public static string InExeDir(string path) {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, path);
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
                throw new Exception(fullMessage);
            }
        }

        public static TReturn PrivateField<T, TReturn>(string fieldName, T instance) {
            FieldInfo? fieldInfo = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert(fieldInfo != null, () => $"Could not find field: {fieldName}");

            return (TReturn)fieldInfo.GetValue(instance);
        }

        public static GameObject? GetChild(GameObject parent, string childName) {
            foreach (Transform child in parent.transform) {
                if (child.name == childName) return child.gameObject;
            }

            return null;
        }

        public static void PrintSceneTree(this GameObject root, ManualLogSource logSource, string[]? prefix = null) {
            prefix ??= Array.Empty<string>();

            foreach (Transform child in root.transform) {
                List<string> thisPrefix = prefix.ToList();
                thisPrefix.Add(child.name);

                logSource.LogMessage($"{string.Join('/', thisPrefix)}:");
                logSource.LogInfo(" - Active: " + child.gameObject.activeSelf);

                Component[] components = child.GetComponents<Component>();
                string[] componentNames = Array.ConvertAll(components, component => component.GetType().Name);
                logSource.LogInfo(" - Components: " + string.Join(", ", componentNames));

                RectTransform? rectTransform = child.GetComponent<RectTransform>();

                string position;
                if (rectTransform != null) {
                    position = $"RectTransform (UI Element) Position = {rectTransform.position}; ";
                    position += $"Width = {rectTransform.sizeDelta.x}; ";
                    position += $"Height = {rectTransform.sizeDelta.y}";
                } else {
                    position = $"Position = {child.position}; Scale = {child.transform.localScale}";
                }

                logSource.LogInfo($" - Transform: {position}\n");

                child.gameObject.PrintSceneTree(logSource, thisPrefix.ToArray());
            }
        }

        public static GameObject FindAssertExists(
            ManualLogSource? logSource = null,
            GameObject? parent = null,
            params string[] path
        ) {
            if (parent == null) {
                parent = GameObject.Find(path[0]);
                path = path[1..];
            }

            return Find(logSource, parent, path)
                   ?? throw new NullReferenceException($"Couldn't find '{string.Join('/', path)}' in '{parent.name}'");
        }

        public static GameObject? Find(
            ManualLogSource? logSource = null,
            GameObject? parent = null,
            params string[] path
        ) {
            if (path.Length == 0 && parent == null) {
                logSource?.LogWarning("Find called without any path or parent");
                return null;
            }

            GameObject? go = parent ?? GameObject.Find(path[0]);
            foreach (string sub in parent == null ? path[1..] : path) {
                if (go == null) return null;
                go = GetChild(go, sub);
            }

            return go;
        }
    }
}