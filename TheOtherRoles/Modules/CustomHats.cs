using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TheOtherRoles.Modules
{

    [HarmonyPatch]
    public class CustomHats
    {
        public static Material hatShader;

        public static Dictionary<string, HatExtension> CustomHatRegistry = new();
        public static HatExtension TestExt = null;

        public class HatExtension
        {
            public string author { get; set; }
            public string package { get; set; }
            public string condition { get; set; }
            public Sprite FlipImage { get; set; }
            public Sprite BackFlipImage { get; set; }
        }

        public class CustomHat
        {
            public string author { get; set; }
            public string package { get; set; }
            public string condition { get; set; }
            public string name { get; set; }
            public string resource { get; set; }
            public string flipresource { get; set; }
            public string backflipresource { get; set; }
            public string backresource { get; set; }
            public string climbresource { get; set; }
            public bool bounce { get; set; }
            public bool adaptive { get; set; }
            public bool behind { get; set; }
        }

        private static List<CustomHat> createCustomHatDetails(string[] hats, bool fromDisk = false)
        {
            Dictionary<string, CustomHat> fronts = new();
            Dictionary<string, string> backs = new();
            Dictionary<string, string> flips = new();
            Dictionary<string, string> backflips = new();
            Dictionary<string, string> climbs = new();

            for (int i = 0; i < hats.Length; i++)
            {
                string s = fromDisk ? hats[i][(hats[i].LastIndexOf("\\") + 1)..].Split('.')[0] : hats[i].Split('.')[3];
                string[] p = s.Split('_');

                HashSet<string> options = new();
                for (int j = 1; j < p.Length; j++)
                    options.Add(p[j]);

                if (options.Contains("back") && options.Contains("flip"))
                    backflips.Add(p[0], hats[i]);
                else if (options.Contains("climb"))
                    climbs.Add(p[0], hats[i]);
                else if (options.Contains("back"))
                    backs.Add(p[0], hats[i]);
                else if (options.Contains("flip"))
                    flips.Add(p[0], hats[i]);
                else
                {
                    CustomHat custom = new()
                    {
                        resource = hats[i],
                        name = p[0].Replace('-', ' '),
                        bounce = options.Contains("bounce"),
                        adaptive = options.Contains("adaptive"),
                        behind = options.Contains("behind")
                    };

                    fronts.Add(p[0], custom);
                }
            }

            List<CustomHat> customhats = new();

            foreach (string k in fronts.Keys)
            {
                CustomHat hat = fronts[k];
                backs.TryGetValue(k, out string br);
                climbs.TryGetValue(k, out string cr);
                flips.TryGetValue(k, out string fr);
                backflips.TryGetValue(k, out string bfr);
                if (br != null)
                    hat.backresource = br;
                if (cr != null)
                    hat.climbresource = cr;
                if (fr != null)
                    hat.flipresource = fr;
                if (bfr != null)
                    hat.backflipresource = bfr;
                if (hat.backresource != null)
                    hat.behind = true;

                customhats.Add(hat);
            }

            return customhats;
        }

        private static Sprite CreateHatSprite(string path, bool fromDisk = false)
        {
            Texture2D texture = fromDisk ? Helpers.loadTextureFromDisk(path) : Helpers.loadTextureFromResources(path);
            if (texture == null)
                return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.53f, 0.575f), texture.width * 0.375f);
            if (sprite == null)
                return null;
            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        private static HatData CreateHatData(CustomHat ch, bool fromDisk = false, bool testOnly = false)
        {
            if (hatShader == null && DestroyableSingleton<HatManager>.InstanceExists)
                hatShader = new Material(Shader.Find("Unlit/PlayerShader"));

            HatData hat = new();
            hat.hatViewData.viewData = new HatViewData
            {
                MainImage = CreateHatSprite(ch.resource, fromDisk)
            };
            if (ch.backresource != null)
            {
                hat.hatViewData.viewData.BackImage = CreateHatSprite(ch.backresource, fromDisk);
                ch.behind = true; // Required to view backresource
            }
            if (ch.climbresource != null)
                hat.hatViewData.viewData.ClimbImage = CreateHatSprite(ch.climbresource, fromDisk);
            hat.name = ch.name;
            hat.displayOrder = 99;
            hat.ProductId = "hat_" + ch.name.Replace(' ', '_') + "_" + ch.author;
            hat.InFront = !ch.behind;
            hat.NoBounce = !ch.bounce;
            hat.ChipOffset = new Vector2(0f, 0.2f);
            hat.Free = true;
            hat.NotInStore = true;


            if (ch.adaptive && hatShader != null)
                hat.hatViewData.viewData.AltShader = hatShader;

            HatExtension extend = new()
            {
                author = ch.author ?? "Unknown",
                package = ch.package ?? "Misc.",
                condition = ch.condition ?? "none"
            };

            if (ch.flipresource != null)
                extend.FlipImage = CreateHatSprite(ch.flipresource, fromDisk);
            if (ch.backflipresource != null)
                extend.BackFlipImage = CreateHatSprite(ch.backflipresource, fromDisk);

            if (testOnly)
            {
                TestExt = extend;
                TestExt.condition = hat.name;
            }
            else
            {
                CustomHatRegistry.Add(hat.name, extend);
            }

            return hat;
        }

        private static HatData CreateHatData(CustomHatLoader.CustomHatOnline chd)
        {
            string filePath = Path.GetDirectoryName(Application.dataPath) + @"\TheOtherHats\";
            chd.resource = filePath + chd.resource;
            if (chd.backresource != null)
                chd.backresource = filePath + chd.backresource;
            if (chd.climbresource != null)
                chd.climbresource = filePath + chd.climbresource;
            if (chd.flipresource != null)
                chd.flipresource = filePath + chd.flipresource;
            if (chd.backflipresource != null)
                chd.backflipresource = filePath + chd.backflipresource;
            return CreateHatData(chd, true);
        }

        [HarmonyPatch(typeof(HatManager), nameof(HatManager.GetHatById))]
        private static class HatManagerPatch
        {
            private static bool LOADED;
            private static bool RUNNING;

            static void Prefix(HatManager __instance)
            {
                if (RUNNING) return;
                RUNNING = true; // prevent simultaneous execution

                try
                {
                    if (!LOADED)
                    {
                        Assembly assembly = Assembly.GetExecutingAssembly();
                        string hatres = $"{assembly.GetName().Name}.Resources.CustomHats";
                        string[] hats = (from r in assembly.GetManifestResourceNames()
                                         where r.StartsWith(hatres) && r.EndsWith(".png")
                                         select r).ToArray<string>();

                        List<CustomHat> customhats = createCustomHatDetails(hats);
                        foreach (CustomHat ch in customhats)
                            __instance.allHats.Add(CreateHatData(ch));
                    }
                    while (CustomHatLoader.hatDetails.Count > 0)
                    {
                        __instance.allHats.Add(CreateHatData(CustomHatLoader.hatDetails[0]));
                        Logger.info(String.Format("Add CustomHat Author:{0} Name:{1}", CustomHatLoader.hatDetails[0].author.PadRightV2(20), CustomHatLoader.hatDetails[0].name), "CustomHats");
                        CustomHatLoader.hatDetails.RemoveAt(0);
                    }
                }
                catch (System.Exception e)
                {
                    if (!LOADED)
                        Logger.error("Unable to add Custom Hats\n" + e, "CustomHats");
                }
                LOADED = true;
            }
            static void Postfix(HatManager __instance)
            {
                RUNNING = false;
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
        private static class PlayerPhysicsHandleAnimationPatch
        {
            private static void Postfix(PlayerPhysics __instance)
            {
                AnimationClip currentAnimation = __instance.Animator.GetCurrentAnimation();
                if (currentAnimation == __instance.CurrentAnimationGroup.ClimbAnim || currentAnimation == __instance.CurrentAnimationGroup.ClimbDownAnim) return;
                HatParent hp = __instance.myPlayer.cosmetics.hat;
                if (hp.Hat == null) return;
                HatExtension extend = hp.Hat.getHatExtension();
                if (extend == null) return;
                if (extend.FlipImage != null)
                {
                    if (__instance.FlipX)
                    {
                        hp.FrontLayer.sprite = extend.FlipImage;
                    }
                    else
                    {
                        hp.FrontLayer.sprite = hp.Hat.hatViewData.viewData.MainImage;
                    }
                }
                if (extend.BackFlipImage != null)
                {
                    if (__instance.FlipX)
                    {
                        hp.BackLayer.sprite = extend.BackFlipImage;
                    }
                    else
                    {
                        hp.BackLayer.sprite = hp.Hat.hatViewData.viewData.BackImage;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        private static class ShipStatusSetHat
        {
            static void Postfix(ShipStatus __instance)
            {
                if (DestroyableSingleton<TutorialManager>.InstanceExists)
                {
                    string filePath = Path.GetDirectoryName(Application.dataPath) + @"\TheOtherHats\Test";
                    DirectoryInfo d = new(filePath);
                    string[] filePaths = d.GetFiles("*.png").Select(x => x.FullName).ToArray(); // Getting Text files
                    List<CustomHat> hats = createCustomHatDetails(filePaths, true);
                    if (hats.Count > 0)
                    {
                        foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                        {
                            var color = pc.CurrentOutfit.ColorId;
                            pc.SetHat("hat_dusk", color);
                            pc.cosmetics.hat.Hat = CreateHatData(hats[0], true, true);
                            pc.cosmetics.hat.SetHat(color);
                        }
                    }
                }
            }
        }

        private static List<TMPro.TMP_Text> hatsTabCustomTexts = new();
        public static string innerslothPackageName = "innerslothHats";
        private static float headerSize = 0.8f;
        private static float headerX = 0.8f;
        private static float inventoryTop = 1.5f;
        private static float inventoryBot = -2.5f;
        private static float inventoryZ = -2f;

        public static void calcItemBounds(HatsTab __instance)
        {
            inventoryTop = __instance.scroller.Inner.position.y - 0.5f;
            inventoryBot = __instance.scroller.Inner.position.y - 4.5f;
        }

        [HarmonyPatch(typeof(HatsTab), nameof(HatsTab.OnEnable))]
        public class HatsTabOnEnablePatch
        {
            public static TMPro.TMP_Text textTemplate;

            public static float createHatPackage(List<System.Tuple<HatData, HatExtension>> hats, string packageName, float YStart, HatsTab __instance)
            {
                float offset = YStart;

                if (textTemplate != null)
                {
                    TMPro.TMP_Text title = UnityEngine.Object.Instantiate<TMPro.TMP_Text>(textTemplate, __instance.scroller.Inner);
                    title.transform.parent = __instance.scroller.Inner;
                    title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                    title.alignment = TMPro.TextAlignmentOptions.Center;
                    title.fontSize *= 1.25f;
                    title.fontWeight = TMPro.FontWeight.Thin;
                    title.enableAutoSizing = false;
                    title.autoSizeTextContainer = true;
                    title.text = ModTranslation.getString(packageName);
                    offset -= headerSize * __instance.YOffset;
                    hatsTabCustomTexts.Add(title);
                }

                var numHats = hats.Count;

                for (int i = 0; i < hats.Count; i++)
                {
                    HatData hat = hats[i].Item1;
                    HatExtension ext = hats[i].Item2;

                    float xpos = __instance.XRange.Lerp((i % __instance.NumPerRow) / (__instance.NumPerRow - 1f));
                    float ypos = offset - (i / __instance.NumPerRow) * __instance.YOffset;
                    ColorChip colorChip = UnityEngine.Object.Instantiate<ColorChip>(__instance.ColorTabPrefab, __instance.scroller.Inner);

                    int color = __instance.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : SaveManager.BodyColor;

                    colorChip.transform.localPosition = new Vector3(xpos, ypos, inventoryZ);
                    if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                    {
                        colorChip.Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(hat)));
                        colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(DestroyableSingleton<HatManager>.Instance.GetHatById(SaveManager.LastHat))));
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                    }
                    else
                    {
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(hat)));
                    }

                    colorChip.Inner.SetHat(hat, color);
                    colorChip.Inner.transform.localPosition = hat.ChipOffset;
                    colorChip.Tag = hat;
                    colorChip.Button.ClickMask = __instance.scroller.Hitbox;
                    __instance.ColorChips.Add(colorChip);
                }

                return offset - ((numHats - 1) / __instance.NumPerRow) * __instance.YOffset - headerSize;
            }

            public static bool Prefix(HatsTab __instance)
            {
                calcItemBounds(__instance);

                HatData[] unlockedHats = DestroyableSingleton<HatManager>.Instance.GetUnlockedHats();
                Dictionary<string, List<System.Tuple<HatData, HatExtension>>> packages = new();

                Helpers.destroyList(hatsTabCustomTexts);
                Helpers.destroyList(__instance.ColorChips);

                hatsTabCustomTexts.Clear();
                __instance.ColorChips.Clear();

                textTemplate = PlayerCustomizationMenu.Instance.itemName;

                foreach (HatData hatData in unlockedHats)
                {
                    HatExtension ext = hatData.getHatExtension();

                    if (ext != null)
                    {
                        if (!packages.ContainsKey(ext.package))
                            packages[ext.package] = new List<System.Tuple<HatData, HatExtension>>();
                        packages[ext.package].Add(new System.Tuple<HatData, HatExtension>(hatData, ext));
                    }
                    else
                    {
                        if (!packages.ContainsKey(innerslothPackageName))
                            packages[innerslothPackageName] = new List<System.Tuple<HatData, HatExtension>>();
                        packages[innerslothPackageName].Add(new System.Tuple<HatData, HatExtension>(hatData, null));
                    }
                }

                float YOffset = __instance.YStart;

                var orderedKeys = packages.Keys.OrderBy((string x) =>
                {
                    if (x == innerslothPackageName) return 10004;
                    if (x == "developerHats") return 10000;
                    if (x.Contains("gmEdition")) return 10003;
                    if (x.Contains("shiune")) return 10002;
                    if (x.Contains("01haomingHat")) return 0;
                    if (x.Contains("02haomingHat")) return 1;
                    if (x.Contains("nationalFlagHats")) return 2;
                    if (x.Contains("CameraCrew")) return 3;
                    if (x.Contains("NonOHat")) return 10001;
                    return 500;
                });

                foreach (string key in orderedKeys)
                {
                    List<System.Tuple<HatData, HatExtension>> value = packages[key];
                    YOffset = createHatPackage(value, key, YOffset, __instance);
                }

                __instance.scroller.ContentYBounds.max = -(YOffset + 3.0f + headerSize);
                return false;
            }
        }

        [HarmonyPatch(typeof(HatsTab), nameof(HatsTab.Update))]
        public class HatsTabUpdatePatch
        {
            public static bool Prefix()
            {
                //return false;
                return true;
            }

            public static void Postfix(HatsTab __instance)
            {
                // Manually hide all custom TMPro.TMP_Text objects that are outside the ScrollRect
                foreach (TMPro.TMP_Text customText in hatsTabCustomTexts)
                {
                    if (customText != null && customText.transform != null && customText.gameObject != null)
                    {
                        bool active = customText.transform.position.y <= inventoryTop && customText.transform.position.y >= inventoryBot;
                        float epsilon = Mathf.Min(Mathf.Abs(customText.transform.position.y - inventoryTop), Mathf.Abs(customText.transform.position.y - inventoryBot));
                        if (active != customText.gameObject.active && epsilon > 0.1f) customText.gameObject.SetActive(active);
                    }
                }
            }
        }
    }

    public class CustomHatLoader
    {
        public static bool running = false;
        public static List<CustomHatOnline> hatDetails = new List<CustomHatOnline>();
        public static void LaunchHatFetcher() 
        {
            if (running)
                return;
            running = true;
            LaunchHatFetcherAsync();
        }

        private static void LaunchHatFetcherAsync() 
        {
            try {
                FetchHats();
            } catch (System.Exception e) {
                System.Console.WriteLine("Unable to fetch hats\n" + e.Message);
            }
            running = false;
        }


        private static string sanitizeResourcePath(string res) 
        {
            if (res == null || !res.EndsWith(".png")) 
                return null;

            res = res.Replace("\\", "")
                     .Replace("/", "")
                     .Replace("*", "")
                     .Replace("..", "");
            return res;
        }

        public static void FetchHats() 
        {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue{ NoCache = true };
            try {
                string jsonPath = Path.GetDirectoryName(Application.dataPath) + @"\CustomHats.json";
                string json = File.ReadAllText(jsonPath);
                JToken jobj = JObject.Parse(json)["hats"];
                if (!jobj.HasValues) return;

                List<CustomHatOnline> hatdatas = new();

                for (JToken current = jobj.First; current != null; current = current.Next)
                {
                    if (current.HasValues)
                    {
                        CustomHatOnline info = new()
                        {
                            name = current["name"]?.ToString(),
                            resource = sanitizeResourcePath(current["resource"]?.ToString())
                        };
                        if (info.resource == null || info.name == null) // required
                            continue;
                        info.backresource = sanitizeResourcePath(current["backresource"]?.ToString());
                        info.climbresource = sanitizeResourcePath(current["climbresource"]?.ToString());
                        info.flipresource = sanitizeResourcePath(current["flipresource"]?.ToString());
                        info.backflipresource = sanitizeResourcePath(current["backflipresource"]?.ToString());

                        info.author = current["author"]?.ToString();
                        info.package = current["package"]?.ToString();
                        info.condition = current["condition"]?.ToString();
                        info.bounce = current["bounce"] != null;
                        info.adaptive = current["adaptive"] != null;
                        info.behind = current["behind"] != null;

                        if (info.package == "Developer Hats")
                            info.package = "developerHats";

                        if (info.package == "Community Hats")
                            info.package = "communityHats";

                        hatdatas.Add(info);
                    }
                }

                List<string> markedNotExist = new List<string>();

                string filePath = Path.GetDirectoryName(Application.dataPath) + @"\TheOtherHats\";
                for (int i = 0; i < hatdatas.Count; i++)
                {
                    CustomHatOnline data = hatdatas[i];
                    markedNotExist.Clear();
                    
                    if (!doesResourceExist(filePath + data.resource))
                        markedNotExist.Add(data.resource);
                    if (data.backresource != null && !doesResourceExist(filePath + data.backresource))
                        markedNotExist.Add(data.backresource);
                    if (data.climbresource != null && !doesResourceExist(filePath + data.climbresource))
                        markedNotExist.Add(data.climbresource);
                    if (data.flipresource != null && !doesResourceExist(filePath + data.flipresource))
                        markedNotExist.Add(data.flipresource);
                    if (data.backflipresource != null && !doesResourceExist(filePath + data.backflipresource))
                        markedNotExist.Add(data.backflipresource);

                    if (markedNotExist.Count != 0)
                    {
                        TheOtherRolesPlugin.Logger.LogMessage(data.name + " Removed!");
                        hatdatas.RemoveAt(i);
                    }
                }
                hatDetails = hatdatas;
            } catch (System.Exception ex) {
                TheOtherRolesPlugin.Instance.Log.LogError(ex.ToString());
                System.Console.WriteLine(ex);
            }
        }

        private static bool doesResourceExist(string respath) 
        {
            if (!File.Exists(respath)) 
                return false;
            return true;
        }

        public class CustomHatOnline : CustomHats.CustomHat
        {
            public string reshasha { get; set; }
            public string reshashb { get; set; }
            public string reshashc { get; set; }
            public string reshashf { get; set; }
            public string reshashbf { get; set; }
        }
    }
    public static class CustomHatExtensions
    {
        public static CustomHats.HatExtension getHatExtension(this HatData hat)
        {
            if (CustomHats.TestExt != null && CustomHats.TestExt.condition.Equals(hat.name))
            {
                return CustomHats.TestExt;
            }
            CustomHats.CustomHatRegistry.TryGetValue(hat.name, out CustomHats.HatExtension ret);
            return ret;
        }
    }

    // TODO 暫定コメントアウト
    /*
    [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.UpdateFromPlayerOutfit))]
    public static class PoolablePlayerPatch
    {
        public static void Postfix(PoolablePlayer __instance)
        {
            if (__instance.VisorSlot?.transform == null || __instance.HatSlot?.transform == null) return;

            // fixes a bug in the original where the visor will show up beneath the hat,
            // instead of on top where it's supposed to be
            __instance.VisorSlot.transform.localPosition = new Vector3(
                __instance.VisorSlot.transform.localPosition.x,
                __instance.VisorSlot.transform.localPosition.y,
                __instance.HatSlot.transform.localPosition.z - 1
                );
        }
    }
    */
}
