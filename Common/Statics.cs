using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using JetBrains.Annotations;
using PluginConfig.API;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace Common {
    public static class Statics {
        public static PluginConfigurator InitPluginConfig(string displayName, string guid) {
            string directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

            PluginConfigurator config = PluginConfigurator.Create(displayName, guid);
            config.SetIconWithURL($"file://{Path.Combine(directory, "icon.png")}");

            return config;
        }

        public static string InExeDir(string path) =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, path);

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


        public static void PrintSceneTree(this GameObject root, ManualLogSource logSource, string[]? prefix = null) {
            prefix ??= Array.Empty<string>();

            foreach (Transform child in root.transform) {
                List<string> thisPrefix = prefix.ToList();
                thisPrefix.Add(child.name);

                child.gameObject.Print(logSource, thisPrefix);
                child.gameObject.PrintSceneTree(logSource, thisPrefix.ToArray());
            }
        }

        public static void Print(this GameObject child, ManualLogSource logSource, List<string>? thisPrefix = null) {
            logSource.LogMessage($"{string.Join('/', thisPrefix ?? new List<string> { child.name })}:");
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
                position = $"Position = {child.transform.position}; Scale = {child.transform.localScale}";
            }

            logSource.LogInfo($" - Transform: {position}\n");
        }

        [MustUseReturnValue]
        public static GameObject FindAssertExists(
            this GameObject? parent,
            ManualLogSource? logSource = null,
            params string[] path
        ) {
            if (parent is null) {
                parent = GameObject.Find(path[0]);
                path = path[1..];
            }

            return Find(logSource, parent, path)
                   ?? throw new NullReferenceException($"Couldn't find '{string.Join('/', path)}' in '{parent.name}'");
        }

        [MustUseReturnValue]
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
            foreach (string sub in parent is null ? path[1..] : path) {
                if (go is null) return null;
                go = go.transform.Find(sub).gameObject;
            }

            return go;
        }

        [MustUseReturnValue]
        public static List<Transform> Children(this Transform transform) {
            List<Transform> children = new();

            foreach (Transform child in transform) {
                children.Add(child);
            }

            return children;
        }

        [MustUseReturnValue]
        public static int ToInt(this bool b) => b ? 1 : 0;

        public static T Also<T>(this T thing, Action<T> action) {
            action(thing);
            return thing;
        }

        public static bool Then(this bool thing, Action action) {
            return thing.Also(x => {
                    if (x) action();
                }
            );
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action) {
            foreach (T item in collection) action(item);
        }

        public static ManualLogSource LogSource(string name) {
            return new ManualLogSource(name).Also(logger => Logger.Sources.Add(logger));
        }

        public class PrivateField<T, TReturn> {
            private readonly FieldInfo fieldInfo;
            private readonly T instance;

            public PrivateField(string fieldName, T instance) {
                FieldInfo? f = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                Assert(f != null, () => $"Could not find field: {f}");

                fieldInfo = f;
                this.instance = instance;
            }

            public TReturn Value {
                get => (TReturn)fieldInfo.GetValue(instance);
                set => fieldInfo.SetValue(instance, value);
            }
        }
    }
}