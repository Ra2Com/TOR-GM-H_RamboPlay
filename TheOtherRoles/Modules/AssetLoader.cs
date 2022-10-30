using System.IO;
using System.Reflection;
using TheOtherRoles.Objects;
using UnityEngine;
using UnityEngine.UI;
using Il2CppType = UnhollowerRuntimeLib.Il2CppType;

namespace TheOtherRoles.Modules
{
    public static class AssetLoader
    {
        private static readonly Assembly dll = Assembly.GetExecutingAssembly();
        private static bool flag = false;
        public static GameObject foxTask;
        public static void LoadAssets()
        {
            if (flag) return;
            flag = true;
            LoadAudioAssets();
            LoadHaomingAssets();
#if DEV
            LoadDevAssets();
#endif
        }
        private static void LoadAudioAssets()
        {
            var resourceAudioAssetBundleStream = dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.audiobundle");
            var assetBundleBundle = AssetBundle.LoadFromMemory(resourceAudioAssetBundleStream.ReadFully());
            Trap.activate = assetBundleBundle.LoadAsset<AudioClip>("TrapperActivate.mp3").DontUnload();
            Trap.countdown = assetBundleBundle.LoadAsset<AudioClip>("TrapperCountdown.mp3").DontUnload();
            Trap.disable = assetBundleBundle.LoadAsset<AudioClip>("TrapperDisable.mp3").DontUnload();
            Trap.kill = assetBundleBundle.LoadAsset<AudioClip>("TrapperKill.mp3").DontUnload();
            Trap.place = assetBundleBundle.LoadAsset<AudioClip>("TrapperPlace.mp3").DontUnload();
            Puppeteer.laugh = assetBundleBundle.LoadAsset<AudioClip>("PuppeteerLaugh.mp3").DontUnload();
        }
        private static void LoadHaomingAssets()
        {
            var resourceTestAssetBundleStream = dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.haomingassets");
            var assetBundleBundle = AssetBundle.LoadFromMemory(resourceTestAssetBundleStream.ReadFully());
            FoxTask.prefab = assetBundleBundle.LoadAsset<GameObject>("FoxTask.prefab").DontUnload();
            Shrine.sprite = assetBundleBundle.LoadAsset<Sprite>("shrine2.png").DontUnload();
            HaomingMenu.menuPrefab = assetBundleBundle.LoadAsset<GameObject>("HaomingMenu.prefab").DontUnload();
            HaomingMenu.loadSettingsPrefab = assetBundleBundle.LoadAsset<GameObject>("LoadSettingsMenu.prefab").DontUnload();
        }

#if DEV
        private static void LoadDevAssets()
        {
            var resourceTestAssetBundleStream = dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.devassets");
            var assetBundleBundle = AssetBundle.LoadFromMemory(resourceTestAssetBundleStream.ReadFully());
            NMK.buttonSprite = assetBundleBundle.LoadAsset<Sprite>("nmkbutton.png").DontUnload();
            NMK.nattyae= assetBundleBundle.LoadAsset<AudioClip>("NMKnattyae.mp3").DontUnload();
            NMK.siteyaru= assetBundleBundle.LoadAsset<AudioClip>("NMKsiteyaru.mp3").DontUnload();
            NMK.syouti = assetBundleBundle.LoadAsset<AudioClip>("NMKsyouti.mp3").DontUnload();
            NMK.yeah = assetBundleBundle.LoadAsset<AudioClip>("NMKyeah.mp3").DontUnload();
            Logger.currentMethod();
            PLT.kii = assetBundleBundle.LoadAsset<AudioClip>("pltkii.mp3").DontUnload();
            Logger.currentMethod();
            PLT.kuroji = assetBundleBundle.LoadAsset<AudioClip>("pltkuroji.mp3").DontUnload();
            Logger.currentMethod();
            PLT.narumi = assetBundleBundle.LoadAsset<AudioClip>("pltnarumi.mp3").DontUnload();
            Logger.currentMethod();
            PLT.nmk = assetBundleBundle.LoadAsset<AudioClip>("pltnmk.mp3").DontUnload();
            Logger.currentMethod();
            PLT.nmk2 = assetBundleBundle.LoadAsset<AudioClip>("pltnmk2.mp3").DontUnload();
            Logger.currentMethod();
            PLT.nazekuroji = assetBundleBundle.LoadAsset<AudioClip>("pltnazekuroji.mp3").DontUnload();
            Logger.currentMethod();
            PLT.nazenarumi = assetBundleBundle.LoadAsset<AudioClip>("pltnazenarumi.mp3").DontUnload();
            Logger.currentMethod();
            PLT.nazenmk = assetBundleBundle.LoadAsset<AudioClip>("pltnazenmk.mp3").DontUnload();
            Logger.currentMethod();
            PLT.tasuketejoari = assetBundleBundle.LoadAsset<AudioClip>("plttasuketejoari.mp3").DontUnload();
            Logger.currentMethod();
            PLT.tasuketekii = assetBundleBundle.LoadAsset<AudioClip>("plttasuketekii.mp3").DontUnload();
        }
#endif
        public static byte[] ReadFully(this Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

#nullable enable
        public static T? LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
        {
            return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
        }
#nullable disable
        public static T DontUnload<T>(this T obj) where T : Object
        {
            obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return obj;
        }
    }


}
