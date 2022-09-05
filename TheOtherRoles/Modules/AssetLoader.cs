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
        private static readonly Assembly allaudio = Assembly.GetExecutingAssembly();
        private static bool flag = false;
        public static void LoadAssets()
        {
            if (flag) return;
            flag = true;
            var resourceAudioAssetBundleStream = allaudio.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.audiobundle");
            var assetBundleBundle = AssetBundle.LoadFromMemory(resourceAudioAssetBundleStream.ReadFully());
            Trap.activate = assetBundleBundle.LoadAsset<AudioClip>("TrapperActivate.mp3").DontUnload();
            Trap.countdown = assetBundleBundle.LoadAsset<AudioClip>("TrapperCountdown.mp3").DontUnload();
            Trap.disable = assetBundleBundle.LoadAsset<AudioClip>("TrapperDisable.mp3").DontUnload();
            Trap.kill = assetBundleBundle.LoadAsset<AudioClip>("TrapperKill.mp3").DontUnload();
            Trap.place = assetBundleBundle.LoadAsset<AudioClip>("TrapperPlace.mp3").DontUnload();
            Puppeteer.laugh = assetBundleBundle.LoadAsset<AudioClip>("PuppeteerLaugh.mp3").DontUnload();
        }
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
