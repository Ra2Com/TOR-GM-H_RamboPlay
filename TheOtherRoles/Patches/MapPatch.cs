using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System;
using Hazel;

namespace TheOtherRoles.Patches

{
    [HarmonyPatch]
    class MapBehaviorPatch
    {
        public static Dictionary<byte, SpriteRenderer> mapIcons = null;
        public static Dictionary<byte, SpriteRenderer> corpseIcons = null;

        public static Sprite corpseSprite;
        public static Sprite doorClosedSprite;
        public static Dictionary<String, SpriteRenderer> doorMarks;
        public static UnhollowerBaseLib.Il2CppArrayBase<PlainDoor> plainDoors;
        private static Vector3 useButtonPos;

        public static SpriteRenderer targetHerePoint;
        public static Dictionary<byte, SpriteRenderer> impostorHerePoint;

        public static Sprite getCorpseSprite()
        {
            if (corpseSprite) return corpseSprite;
            corpseSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CorpseIcon.png", 115f);
            return corpseSprite;
        }

        public static void reset()
        {
            if (mapIcons != null)
            {
                foreach (SpriteRenderer r in mapIcons.Values)
                    UnityEngine.Object.Destroy(r.gameObject);
                mapIcons.Clear();
                mapIcons = null;
            }

            if (corpseIcons != null)
            {
                foreach (SpriteRenderer r in corpseIcons.Values)
                    UnityEngine.Object.Destroy(r.gameObject);
                corpseIcons.Clear();
                corpseIcons = null;
            }

            if (targetHerePoint != null)
            {
                UnityEngine.Object.Destroy(targetHerePoint.gameObject);
            }

            if (impostorHerePoint != null)
            {
                foreach (SpriteRenderer r in impostorHerePoint.Values)
                {
                    UnityEngine.Object.Destroy(r.gameObject);
                }
                impostorHerePoint.Clear();
                impostorHerePoint = null;
            }
            if (doorMarks != null)
            {
                foreach (var mark in doorMarks.Values)
                {
                    UnityEngine.Object.Destroy(mark.gameObject);
                }
                doorMarks.Clear();
                doorMarks = null;
            }
            if (plainDoors != null)
            {
                plainDoors = null;
            }
        }

        static void initializeIcons(MapBehaviour __instance, PlayerControl pc = null)
        {
            List<PlayerControl> players = new();
            if (pc == null)
            {
                mapIcons = new Dictionary<byte, SpriteRenderer>();
                corpseIcons = new Dictionary<byte, SpriteRenderer>();
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    players.Add(p);
                }
            }
            else
            {
                players.Add(pc);
            }

            foreach (PlayerControl p in players)
            {
                if (p.isGM()) continue;

                byte id = p.PlayerId;
                mapIcons[id] = UnityEngine.Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);
                p.SetPlayerMaterialColors(mapIcons[id]);


                corpseIcons[id] = UnityEngine.Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);
                corpseIcons[id].sprite = getCorpseSprite();
                corpseIcons[id].transform.localScale = Vector3.one * 0.20f;
                p.SetPlayerMaterialColors(corpseIcons[id]);
            }
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
        class MapBehaviourFixedUpdatePatch
        {
            static bool Prefix(MapBehaviour __instance)
            {
                if (!MeetingHud.Instance) return true;  // Only run in meetings, and then set the Position of the HerePoint to the Position before the Meeting!
                // if (!ShipStatus.Instance) {
                //     return false;
                // }
                Vector3 vector = AntiTeleport.position != null ? AntiTeleport.position : PlayerControl.LocalPlayer.transform.position;
                vector /= ShipStatus.Instance.MapScale;
                vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                vector.z = -1f;
                __instance.HerePoint.transform.localPosition = vector;
                PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
                return false;
            }

            static void Postfix(MapBehaviour __instance)
            {
                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilTracker) && EvilTracker.canSeeTargetPosition)
                {
                    evilTrackerFixedUpdate(__instance);
                }

                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker))
                {
                    evilHackerFixedUpdate(__instance);
                }

                if (PlayerControl.LocalPlayer.isGM())
                {
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p == null || p.isGM()) continue;

                        byte id = p.PlayerId;
                        if (!mapIcons.ContainsKey(id))
                        {
                            continue;
                        }

                        bool enabled = !p.Data.IsDead;
                        if (enabled)
                        {
                            Vector3 vector = p.transform.position;
                            vector /= ShipStatus.Instance.MapScale;
                            vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                            vector.z = -1f;
                            mapIcons[id].transform.localPosition = vector;

                        }

                        mapIcons[id].enabled = enabled;
                    }

                    foreach (SpriteRenderer r in corpseIcons.Values) { r.enabled = false; }
                    foreach (DeadBody b in UnityEngine.Object.FindObjectsOfType<DeadBody>())
                    {
                        byte id = b.ParentId;
                        Vector3 vector = b.transform.position;
                        vector /= ShipStatus.Instance.MapScale;
                        vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                        vector.z = -1f;

                        if (!corpseIcons.ContainsKey(id))
                        {
                            continue;
                        }

                        corpseIcons[id].transform.localPosition = vector;
                        corpseIcons[id].enabled = true;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
        class MapBehaviourShowNormalMapPatch
        {
            static bool Prefix(MapBehaviour __instance)
            {
                if (!MeetingHud.Instance || __instance.IsOpen) return true;  // Only run in meetings and when the map is closed

                PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
                __instance.GenericShow();
                __instance.taskOverlay.Show();
                __instance.ColorControl.SetColor(new Color(0.05f, 0.2f, 1f, 1f));
                DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
                return false;

            }
        }



        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
        class MapBehaviourGenericShowPatch
        {
            static void Prefix(MapBehaviour __instance)
            {
                if (PlayerControl.LocalPlayer.isGM())
                {
                    useButtonPos = HudManager.Instance.UseButton.transform.localPosition;
                }
                CustomOverlays.hideInfoOverlay();
                CustomOverlays.hideRoleOverlay();

            }

            static void Postfix(MapBehaviour __instance)
            {
                if (PlayerControl.LocalPlayer.isGM())
                {
                    if (mapIcons == null || corpseIcons == null)
                        initializeIcons(__instance);

                    __instance.taskOverlay.Hide();
                    foreach (byte id in mapIcons.Keys)
                    {
                        PlayerControl p = Helpers.getPlayerById(id);
                        p.SetPlayerMaterialColors(mapIcons[id]);
                        mapIcons[id].enabled = !p.Data.IsDead;
                    }

                    foreach (DeadBody b in UnityEngine.Object.FindObjectsOfType<DeadBody>())
                    {
                        byte id = b.ParentId;
                        PlayerControl p = Helpers.getPlayerById(id);
                        p.SetPlayerMaterialColors(corpseIcons[id]);
                        corpseIcons[id].enabled = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]
        class MapBehaviorClosePatch
        {
            static void Postfix(MapBehaviour __instance)
            {
                if (PlayerControl.LocalPlayer.isGM())
                {
                    HudManager.Instance.UseButton.transform.localPosition = useButtonPos;
                }
                DestroyableSingleton<HudManager>.Instance.transform.FindChild("TaskDisplay").FindChild("TaskPanel").gameObject.SetActive(true);
            }
        }
        [HarmonyPatch(typeof(MapBehaviour), "get_IsOpenStopped")]
        class MapBehaviorGetIsOpenStoppedPatch
        {
            static bool Prefix(ref bool __result, MapBehaviour __instance)
            {
                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker) && CustomOptionHolder.evilHackerCanMoveEvenIfUsesAdmin.getBool())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        class MapBehaviourShowSabotageMap
        {
            static bool Prefix(MapBehaviour __instance)
            {
                Vector3 pos = __instance.HerePoint.transform.parent.transform.position;
                __instance.HerePoint.transform.parent.transform.position = new Vector3(pos.x, pos.y, -60f);
                changeSabotageLayout(__instance);
                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilHacker)) return evilHackerShowMap(__instance);
                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilTracker)) return evilTrackerShowMap(__instance);
                return true;
            }
            static void Postfix(MapBehaviour __instance)
            {
                if (TheOtherRolesPlugin.HideFakeTasks.Value)
                {
                    __instance.taskOverlay.Hide();
                }
            }
        }
        private static void showDoorStatus(MapBehaviour __instance)
        {
            if (doorClosedSprite == null)
            {
                doorClosedSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.cross.png", 500f);
            }
            // if (plainDoors == null) plainDoors = GameObject.FindObjectsOfType<PlainDoor>();
            if (doorMarks == null) doorMarks = new();

            foreach (var door in plainDoors)
            {
                Vector3 pos = door.gameObject.transform.position / ShipStatus.Instance.MapScale;
                pos.z = -10f;
                String key = $"{pos.x},{pos.y}";
                SpriteRenderer mark;
                if (doorMarks.ContainsKey(key))
                {
                    mark = doorMarks[key];
                }
                else
                {
                    mark = GameObject.Instantiate<SpriteRenderer>(__instance.HerePoint, __instance.HerePoint.transform.parent);
                    doorMarks.Add(key, mark);
                }
                if (!door.Open)
                {
                    mark.gameObject.SetActive(true);
                    mark.sprite = doorClosedSprite;
                    PlayerMaterial.SetColors(0, mark);
                    mark.transform.localPosition = pos;
                    mark.gameObject.SetActive(true);
                }
                else
                {
                    mark.gameObject.SetActive(false);
                }
            }

        }
        private static void changeSabotageLayout(MapBehaviour __instance)
        {
            if (PlayerControl.GameOptions.MapId == 4)
            {
                // サボタージュアイコンのレイアウトを変更
                Vector3 halfScale = new Vector3(0.75f, 0.75f, 0.75f);
                Vector3 originalScale = new Vector3(1f, 1f, 1f);
                Vector3 scale = TheOtherRolesPlugin.BetterSabotageMap.Value ? halfScale : originalScale;
                Transform comms = __instance.infectedOverlay.transform.FindChild("Comms");
                Transform electrical = __instance.infectedOverlay.transform.FindChild("Electrical");
                Transform mainHall = __instance.infectedOverlay.transform.FindChild("MainHall");
                Transform gapRoom = __instance.infectedOverlay.transform.FindChild("Gap Room");
                Transform records = __instance.infectedOverlay.transform.FindChild("Records");
                Transform brig = __instance.infectedOverlay.transform.FindChild("Brig");
                Transform kitchen = __instance.infectedOverlay.transform.FindChild("Kitchen");
                Transform medbay = __instance.infectedOverlay.transform.FindChild("Medbay");


                comms.localScale = scale;
                electrical.localScale = scale;
                mainHall.localScale = scale;
                gapRoom.localScale = scale;
                records.localScale = scale;
                brig.localScale = scale;
                kitchen.localScale = scale;
                medbay.localScale = scale;

                if (TheOtherRolesPlugin.BetterSabotageMap.Value)
                {
                    comms.FindChild("bomb").localPosition = new Vector3(-0.1f, 0.9f, -1f);
                    comms.FindChild("Doors").localPosition = new Vector3(0.5f, 0.45f, -1f);
                    electrical.FindChild("lightsOut").localPosition = new Vector3(0f, -0.6f, -1f);
                    mainHall.FindChild("Doors").localPosition = new Vector3(-0.18f, -0.35f, -1f);
                    gapRoom.FindChild("meltdown").localPosition = new Vector3(-0.34f, 0f, -1f);
                    records.FindChild("Doors").localPosition = new Vector3(0.01f, 1.2f, -1f);
                    brig.FindChild("Doors").localPosition = new Vector3(0f, 0.9f, -1f);
                    kitchen.FindChild("Doors").localPosition = new Vector3(0.1f, 0.9f, -1f);
                    medbay.FindChild("Doors").localPosition = new Vector3(0.2f, 0f, -1f);
                }
                else
                {
                    comms.FindChild("bomb").localPosition = new Vector3(-0.3f, 0f, -0.5f);
                    comms.FindChild("Doors").localPosition = new Vector3(0.3f, 0f, -0.5f);
                    electrical.FindChild("lightsOut").localPosition = new Vector3(0f, 0f, -0.5f);
                    mainHall.FindChild("Doors").localPosition = new Vector3(0f, 0f, -0.5f);
                    gapRoom.FindChild("meltdown").localPosition = new Vector3(0f, 0f, -0.5f);
                    records.FindChild("Doors").localPosition = new Vector3(0f, 0f, -0.5f);
                    brig.FindChild("Doors").localPosition = new Vector3(0f, 0f, -0.5f);
                    kitchen.FindChild("Doors").localPosition = new Vector3(0f, 0f, -0.5f);
                    medbay.FindChild("Doors").localPosition = new Vector3(0f, 0f, -0.5f);
                }
            }
        }

        private static void evilHackerFixedUpdate(MapBehaviour __instance)
        {
            showDoorStatus(__instance);
        }

        private static void evilTrackerFixedUpdate(MapBehaviour __instance)
        {
            // ターゲットの位置をマップに表示
            if (EvilTracker.target != null)
            {
                if (targetHerePoint == null)
                {
                    targetHerePoint = GameObject.Instantiate<SpriteRenderer>(__instance.HerePoint, __instance.HerePoint.transform.parent);
                }
                targetHerePoint.gameObject.SetActive(EvilTracker.target.isAlive());
                GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(EvilTracker.target.PlayerId);
                PlayerMaterial.SetColors((playerById != null) ? playerById.DefaultOutfit.ColorId : 0, targetHerePoint);
                Vector3 pos = new Vector3(EvilTracker.target.transform.position.x, EvilTracker.target.transform.position.y, EvilTracker.target.transform.position.z);
                pos /= ShipStatus.Instance.MapScale;
                pos.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                pos.z = -10;
                targetHerePoint.transform.localPosition = pos;
            }

            // インポスターの位置をマップに表示
            if (impostorHerePoint == null) impostorHerePoint = new();
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p.isImpostor() && p != PlayerControl.LocalPlayer)
                {
                    if (!impostorHerePoint.ContainsKey(p.PlayerId))
                    {
                        impostorHerePoint[p.PlayerId] = GameObject.Instantiate<SpriteRenderer>(__instance.HerePoint, __instance.HerePoint.transform.parent);
                    }
                    impostorHerePoint[p.PlayerId].gameObject.SetActive(p.isAlive());
                    GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(p.PlayerId);
                    PlayerMaterial.SetColors(0, impostorHerePoint[p.PlayerId]);
                    Vector3 pos = new Vector3(p.transform.position.x, p.transform.position.y, p.transform.position.z);
                    pos /= ShipStatus.Instance.MapScale;
                    pos.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                    pos.z = -10;
                    impostorHerePoint[p.PlayerId].transform.localPosition = pos;
                }
            }
        }
        private static bool evilTrackerShowMap(MapBehaviour __instance)
        {
            if (MeetingHud.Instance) return true;
            if (__instance.IsOpen)
            {
                __instance.Close();
                return false;
            }
            if (!PlayerControl.LocalPlayer.CanMove)
            {
                return false;
            }
            if (__instance.specialInputHandler != null)
            {
                __instance.specialInputHandler.disableVirtualCursor = true;
            }
            PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
            __instance.GenericShow();
            __instance.gameObject.SetActive(true);
            __instance.infectedOverlay.gameObject.SetActive(true);
            if (TheOtherRolesPlugin.HideFakeTasks.Value)
            {
                __instance.taskOverlay.Hide();
            }
            else
            {
                __instance.taskOverlay.Show();
            }
            __instance.ColorControl.SetColor(Palette.ImpostorRed);
            DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
            ConsoleJoystick.SetMode_Sabotage();

            return false;
        }
        private static bool evilHackerShowMap(MapBehaviour __instance)
        {
            if (MeetingHud.Instance) return true;
            if (__instance.IsOpen)
            {
                __instance.Close();
                return false;
            }
            if (!PlayerControl.LocalPlayer.CanMove)
            {
                return false;
            }
            if (__instance.specialInputHandler != null)
            {
                __instance.specialInputHandler.disableVirtualCursor = true;
            }
            plainDoors = GameObject.FindObjectsOfType<PlainDoor>();
            PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
            __instance.GenericShow();
            __instance.gameObject.SetActive(true);
            AdminPatch.isEvilHackerAdmin = true;
            __instance.countOverlay.gameObject.SetActive(true);
            __instance.infectedOverlay.gameObject.SetActive(true);
            if (TheOtherRolesPlugin.HideFakeTasks.Value)
            {
                __instance.taskOverlay.Hide();
            }
            else
            {
                __instance.taskOverlay.Show();
            }
            __instance.ColorControl.SetColor(Palette.ImpostorRed);
            DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
            ConsoleJoystick.SetMode_Sabotage();

            return false;
        }

        public static Dictionary<byte, Il2CppSystem.Collections.Generic.List<Vector2>> realTasks = new();
        public static void resetRealTasks()
        {
            realTasks.Clear();
        }
        public static void shareRealTasks()
        {
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
            {
                if (!task.IsComplete && task.HasLocation && !PlayerTask.TaskIsEmergency(task))
                {
                    foreach (var loc in task.Locations)
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShareRealTasks, Hazel.SendOption.Reliable, -1);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(loc.x);
                        writer.Write(loc.y);
                        writer.Write(task.TaskStep);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MapTaskOverlay), nameof(MapTaskOverlay.Show))]
        class MapTaskOverlayShow
        {
            static bool Prefix(MapTaskOverlay __instance)
            {
                if (PlayerControl.LocalPlayer.isRole(RoleType.EvilTracker))
                {
                    return evilTrackerShowTask(__instance);
                }
                return true;
            }
        }

        private static bool evilTrackerShowTask(MapTaskOverlay __instance)
        {
            if (!MeetingHud.Instance) return true;  // Only run in meetings, and then set the Position of the HerePoint to the Position before the Meeting!
            if (!PlayerControl.LocalPlayer.isRole(RoleType.EvilTracker) || !CustomOptionHolder.evilTrackerCanSeeTargetTask.getBool()) return true;
            if (EvilTracker.target == null) return true;
            if (realTasks[EvilTracker.target.PlayerId] == null) return false;
            __instance.gameObject.SetActive(true);
            __instance.data.Clear();
            for (int i = 0; i < realTasks[EvilTracker.target.PlayerId].Count; i++)
            {
                try
                {
                    Vector2 pos = realTasks[EvilTracker.target.PlayerId][i];

                    Vector3 localPosition = pos / ShipStatus.Instance.MapScale;
                    localPosition.z = -1f;
                    PooledMapIcon pooledMapIcon = __instance.icons.Get<PooledMapIcon>();
                    pooledMapIcon.transform.localScale = new Vector3(pooledMapIcon.NormalSize, pooledMapIcon.NormalSize, pooledMapIcon.NormalSize);
                    pooledMapIcon.rend.color = Color.yellow;
                    pooledMapIcon.name = $"{i}";
                    pooledMapIcon.lastMapTaskStep = 0;
                    pooledMapIcon.transform.localPosition = localPosition;
                    string text = $"{i}";
                    __instance.data.Add(text, pooledMapIcon);
                }
                catch (Exception ex)
                {
                    Logger.error(ex.Message);
                }
            }
            return false;
        }
    }
}
