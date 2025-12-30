using System.IO;
using System.Reflection;
using BepInEx;
using Common;
using PluginConfig.API;
using UnityEngine;

namespace MessingAround {
    [BepInPlugin("com.einfachirgendwa1.messingAround", "MessingAround", "1.0.0.0")]
    public class MessingAround : BaseUnityPlugin {
        private PluginConfigurator? config;
        private GameObject? cur;
        private GameObject? fugg;

        private void Awake() {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            config = PluginConfigurator.Create("Messing Around", "com.einfachirgendwa1.messingAround");
            config.SetIconWithURL($"file://{Path.Combine(directory, "icon.png")}");

            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(directory, "fugg"));
            Statics.Assert(assetBundle != null, () => "Failed to read asset bundle!");

            fugg = assetBundle.LoadAsset<GameObject>("fugg");
            Logger.LogInfo(fugg.name);
            assetBundle.Unload(false);
        }

        private void Update() {
            GunControl gunControl = GunControl.Instance;
            if (gunControl == null) return;

            GameObject currentWeapon = gunControl.currentWeapon;

            bool isNew = cur != currentWeapon;
            cur = currentWeapon;

            if (currentWeapon == null || !isNew || currentWeapon.name != "Alternative Revolver Pierce(Clone)") {
                return;
            }

            Transform? absRerigged = null;
            foreach (Transform rerigged in currentWeapon.transform) {
                absRerigged = rerigged;
                foreach (Transform thing in rerigged.transform) {
                    if (thing.name == "MinosRevolver_Cyllinder") thing.gameObject.SetActive(false);
                }
            }

            GameObject fuggInstance = Instantiate(fugg!, absRerigged!, true);
            fuggInstance.SetActive(true);
            // fuggInstance.transform.position = new Vector3(0.73f, 1.04f, -0.337f);
            // fuggInstance.transform.rotation = Quaternion.Euler(73.886f, -90.349f, -271.257f);
            fuggInstance.transform.localScale = new Vector3(10, 10, 10);
        }
    }
}