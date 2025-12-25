using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UltrakillMod.UnityWav;
using UnityEngine;

namespace UltrakillMod {
    [BepInPlugin("UltrakillMod", "UltrakillMod", "1.0.0.0")]
    public class UltrakillMod : BaseUnityPlugin {
        private static AudioClip? audioClip;
        private static ManualLogSource? staticLogger;
        private readonly Harmony harmony = new Harmony("UltrakillMod");

        private void Awake() {
            staticLogger = Logger;
            foreach (string resource in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                Logger.LogMessage(resource);
            }

            using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("UltrakillMod.EmbeddedResources.ThrowCoinSound.wav")!;

            byte[] sound = new byte[stream.Length];
            stream.Read(sound, 0, sound.Length);

            MethodInfo throwCoin = AccessTools.Method(typeof(Revolver), "ThrowCoin")!;
            HarmonyMethod prefix = new HarmonyMethod(typeof(UltrakillMod), nameof(PlaySound));
            harmony.Patch(throwCoin, prefix);
            audioClip = WavUtility.ToAudioClip(sound);
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.B)) {
                Logger.LogError("B Key was pressed!");
                AudioSource.PlayClipAtPoint(audioClip!, Vector3.zero);
            }
        }

        private static void PlaySound(Revolver __instance) {
            staticLogger!.LogError("Playing Sound");
            FieldInfo gunAud = typeof(Revolver).GetField("gunAud", BindingFlags.NonPublic | BindingFlags.Instance)!;
            AudioSource audio = (AudioSource)gunAud.GetValue(__instance);
            AudioSource newSource = audio.gameObject.AddComponent<AudioSource>();
            newSource.volume = audio.volume;
            newSource.clip = audioClip!;
            newSource.Play();
        }
    }
}