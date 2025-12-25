using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UltrakillMods.UnityWav;
using UnityEngine;
using static Common.Statics;

namespace UltrakillMods {
    [BepInPlugin("com.einfachirgendwa1.coinMod", "CoinMod", "1.0.0.0")]
    public class CoindMod : BaseUnityPlugin {
        private static AudioClip? audioClip;
        private readonly Harmony harmony = new Harmony("CoinMod");

        private void Awake() {
            byte[] sound;
            using (Stream stream = GetEmbeddedResource("ThrowCoinSound.wav")) {
                sound = new byte[stream.Length];
                Assert(stream.Read(sound, 0, sound.Length) == sound.Length);
            }

            MethodInfo throwCoin = AccessTools.Method(typeof(Revolver), "ThrowCoin")!;
            HarmonyMethod prefix = new HarmonyMethod(typeof(CoindMod), nameof(PlaySound));
            harmony.Patch(throwCoin, prefix);
            audioClip = WavUtility.ToAudioClip(sound);
        }

        private static void PlaySound(Revolver __instance) {
            logSource.LogMessage("Playing sound!");
            AudioSource audio = PrivateField<Revolver, AudioSource>("gunAud", __instance);
            AudioSource newSource = audio.gameObject.AddComponent<AudioSource>();
            newSource.volume = audio.volume;
            newSource.clip = audioClip!;
            newSource.Play();
        }
    }
}