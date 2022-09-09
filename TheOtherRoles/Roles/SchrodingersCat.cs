using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class SchrodingersCat : RoleBase<SchrodingersCat>
    {
        public enum exileType
        {
            None = 0,
            Crew = 1,
            Random = 2,
        }

        public enum Team
        {
            None = 0,
            Crew = 1,
            Impostor = 2,
            Jackal = 3,
            JekyllAndHyde = 4,
            Moriarty = 5,
        }

        public static Color color = Color.grey;
        public static Team team = Team.None;
        public static float killCooldown { get { return CustomOptionHolder.schrodingersCatKillCooldown.getFloat(); } }
        public static bool becomesImpostor { get { return CustomOptionHolder.schrodingersCatBecomesImpostor.getBool(); } }
        public static exileType becomesWhichTeamsOnExiled { get { return (exileType)CustomOptionHolder.schrodingersCatBecomesWhichTeamsOnExiled.getSelection(); } }
        public static bool cantKillUntilLastOne { get { return CustomOptionHolder.schrodingersCatCantKillUntilLastOne.getBool(); } }
        public static bool killsKiller { get { return CustomOptionHolder.schrodingersCatKillsKiller.getBool(); } }
        public static bool justDieOnKilledByCrew { get { return CustomOptionHolder.schrodingersCatJustDieOnKilledByCrew.getBool(); } }
        public static PlayerControl killer = null;

        public SchrodingersCat()
        {
            RoleType = roleId = RoleType.SchrodingersCat;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd()
        {
            if (CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.SchrodingersCat))
                CachedPlayer.LocalPlayer.PlayerControl.SetKillTimerUnchecked(killCooldown);
        }
        public override void FixedUpdate()
        {
            if (player == CachedPlayer.LocalPlayer.PlayerControl && team == Team.Jackal)
            {
                if (!isTeamJackalAlive() || !cantKillUntilLastOne)
                {
                    currentTarget = setTarget();
                    setPlayerOutline(currentTarget, Sheriff.color);
                }
            }
            if (player == CachedPlayer.LocalPlayer.PlayerControl && team == Team.JekyllAndHyde)
            {
                if (JekyllAndHyde.livingPlayers.Count == 0 || !cantKillUntilLastOne)
                {
                    currentTarget = setTarget();
                    setPlayerOutline(currentTarget, Sheriff.color);
                }
            }
            if (player == CachedPlayer.LocalPlayer.PlayerControl && team == Team.Moriarty)
            {
                if (Moriarty.livingPlayers.Count == 0 || !cantKillUntilLastOne)
                {
                    currentTarget = setTarget();
                    setPlayerOutline(currentTarget, Sheriff.color);
                }
            }
            if (player == CachedPlayer.LocalPlayer.PlayerControl && team == Team.Impostor && !isLastImpostor() && cantKillUntilLastOne)
            {
                FastDestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(null);
            }
        }

        public override void OnKill(PlayerControl target)
        {
            if (CachedPlayer.LocalPlayer.PlayerControl == player && team == Team.Impostor)
                player.SetKillTimerUnchecked(killCooldown);
        }
        public override void OnDeath(PlayerControl killer = null)
        {
            // 占い師の画面では呪殺したことを分からなくするために自殺処理させているので注意すること
            if (team != Team.None) return;
            if (((killer != null && killer.isCrew()) || killer.isRole(RoleType.SchrodingersCat)) && justDieOnKilledByCrew) return;
            if (killer == null)
            {
                if (becomesWhichTeamsOnExiled == exileType.Random && player == CachedPlayer.LocalPlayer.PlayerControl)
                {
                    List<Team> candidates = new();
                    candidates.Add(Team.Crew);
                    candidates.Add(Team.Impostor);
                    if (JekyllAndHyde.exists) candidates.Add(Team.JekyllAndHyde);
                    if (Moriarty.exists) candidates.Add(Team.Moriarty);
                    if (Jackal.jackal != null) candidates.Add(Team.Jackal);
                    int rndVal = rnd.Next(0, candidates.Count);
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SchrodingersCatSetTeam, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte)candidates[rndVal]);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.schrodingersCatSetTeam((byte)candidates[rndVal]);
                }
                else if (becomesWhichTeamsOnExiled == exileType.Crew)
                {
                    setCrewFlag();
                }
                return;
            }
            else
            {
                bool isCrewOrSchrodingersCat = (!killer.isRole(RoleType.JekyllAndHyde) && killer.isCrew()) || killer.isRole(RoleType.SchrodingersCat);
                if (killer.isImpostor())
                {
                    setImpostorFlag();
                    if (becomesImpostor)
                        FastDestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
                }
                else if (killer.isRole(RoleType.Jackal))
                {
                    setJackalFlag();
                }
                else if (killer.isRole(RoleType.JekyllAndHyde))
                {
                    setJekyllAndHydeFlag();
                }
                else if (killer.isRole(RoleType.Moriarty))
                {
                    setMoriartyFlag();
                }
                else if (isCrewOrSchrodingersCat)
                {
                    setCrewFlag();
                }

                // EndGamePatchでゲームを終了させないために先にkillerに値を代入する
                if (SchrodingersCat.killsKiller && !isCrewOrSchrodingersCat)
                    SchrodingersCat.killer = killer;


                // 蘇生する
                player.Revive();
                // 死体を消す
                DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == player.PlayerId)
                    {
                        array[i].gameObject.active = false;
                    }
                }
                if (SchrodingersCat.killsKiller && !isCrewOrSchrodingersCat)
                {
                    if (CachedPlayer.LocalPlayer.PlayerControl == killer)
                    {
                        // 死亡までのカウントダウン
                        TMPro.TMP_Text text;
                        RoomTracker roomTracker = FastDestroyableSingleton<HudManager>.Instance?.roomTracker;
                        GameObject gameObject = UnityEngine.Object.Instantiate(roomTracker.gameObject);
                        UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                        gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                        gameObject.transform.localPosition = new Vector3(0, -1.8f, gameObject.transform.localPosition.z);
                        gameObject.transform.localScale = Vector3.one * 3f;
                        text = gameObject.GetComponent<TMPro.TMP_Text>();
                        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(15f, new Action<float>((p) =>
                        {
                            string message = (15 - (p * 15f)).ToString("0");
                            bool even = ((int)(p * 15f / 0.25f)) % 2 == 0; // Bool flips every 0.25 seconds
                            string prefix = even ? "<color=#FCBA03FF>" : "<color=#FF0000FF>";
                            text.text = prefix + message + "</color>";
                            if (text != null) text.color = even ? Color.yellow : Color.red;
                            if (p == 1f && text != null && text.gameObject != null)
                            {
                                if (SchrodingersCat.killer != null && SchrodingersCat.killer.isAlive())
                                {
                                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SchrodingersCatSuicide, Hazel.SendOption.Reliable, -1);
                                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                                    RPCProcedure.schrodingersCatSuicide();
                                    SchrodingersCat.killer = null;
                                }
                                UnityEngine.Object.Destroy(text.gameObject);
                            }
                        })));
                    }
                }
            }
        }
        public override void OnFinishShipStatusBegin() { }

        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        private static CustomButton killButton;
        public static PlayerControl currentTarget;
        public static void MakeButtons(HudManager hm)
        {
            killButton = new CustomButton(
                () =>
                {
                    if (Helpers.checkMuderAttemptAndKill(CachedPlayer.LocalPlayer.PlayerControl, SchrodingersCat.currentTarget) == MurderAttemptResult.SuppressKill) return;

                    killButton.Timer = killButton.MaxTimer;
                    Jackal.currentTarget = null;
                },
                () => { return isJackalButtonEnable() || isJekyllAndHydeButtonEnable() || isMoriartyButtonEnable(); },
                () => { return SchrodingersCat.currentTarget && CachedPlayer.LocalPlayer.PlayerControl.CanMove; },
                () => { killButton.Timer = killButton.MaxTimer; },
                hm.KillButton.graphic.sprite,
                new Vector3(0, 1f, 0),
                hm,
                hm.KillButton,
                KeyCode.Q
            );
            killButton.Timer = killButton.MaxTimer = killCooldown;
        }
        public static void SetButtonCooldowns()
        {
            killButton.MaxTimer = killCooldown;
        }

        public static void Clear()
        {
            players = new List<SchrodingersCat>();
            team = Team.None;
            RoleInfo.schrodingersCat.color = color;
            killer = null;
        }

        public static void setImpostorFlag()
        {
            team = Team.Impostor;
            RoleInfo.schrodingersCat.color = Palette.ImpostorRed;
        }

        public static void setCrewFlag()
        {
            team = Team.Crew;
            RoleInfo.schrodingersCat.color = Color.white;
        }

        public static void setJackalFlag()
        {
            team = Team.Jackal;
            RoleInfo.schrodingersCat.color = Jackal.color;
        }

        public static void setJekyllAndHydeFlag()
        {
            team = Team.JekyllAndHyde;
            RoleInfo.jekyllAndHyde.color = JekyllAndHyde.color;
        }
        public static void setMoriartyFlag()
        {
            team = Team.Moriarty;
            RoleInfo.moriarty.color = Moriarty.color;
        }

        public static bool isJackalButtonEnable()
        {
            if (team == Team.Jackal && CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.SchrodingersCat) && CachedPlayer.LocalPlayer.PlayerControl.isAlive())
            {
                if (!isTeamJackalAlive() || !cantKillUntilLastOne)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isJekyllAndHydeButtonEnable()
        {
            if (team == Team.JekyllAndHyde && CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.SchrodingersCat) && CachedPlayer.LocalPlayer.PlayerControl.isAlive())
            {
                if (JekyllAndHyde.livingPlayers.Count == 0 || !cantKillUntilLastOne)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isMoriartyButtonEnable()
        {
            if (team == Team.Moriarty && PlayerControl.LocalPlayer.isRole(RoleType.SchrodingersCat) && PlayerControl.LocalPlayer.isAlive())
            {
                if (Moriarty.livingPlayers.Count == 0 || !cantKillUntilLastOne)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isTeamJackalAlive()
        {
            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.isRole(RoleType.Jackal) && p.isAlive())
                {
                    return true;
                }
                else if (p.isRole(RoleType.Sidekick) && p.isAlive())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isLastImpostor()
        {
            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (CachedPlayer.LocalPlayer.PlayerControl != p && p.isImpostor() && p.isAlive()) return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        class PlayerControlCmdReportDeadBodyPatch
        {
            public static void Prefix(PlayerControl __instance)
            {
                // 時限爆弾よりも前にミーティングが来たら直後に死亡する
                if (killer != null && killsKiller)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SchrodingersCatSuicide, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.schrodingersCatSuicide();
                    killer = null;
                }
            }
        }
    }
}
