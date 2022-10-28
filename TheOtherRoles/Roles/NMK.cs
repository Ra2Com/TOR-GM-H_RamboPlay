#if DEV
using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using HarmonyLib;
using TheOtherRoles.Objects;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using UnityEngine;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class NMK : RoleBase<NMK>
    {
        public static TMPro.TMP_Text numNMKText;
        private static CustomButton nmkButton;

        public static Color color = new Color32(172, 213, 239, byte.MaxValue);

        public static float numNMK { get { return CustomOptionHolder.nmkNum.getFloat(); } }
        public static float cooldown = 0f;

        public PlayerControl currentTarget;

        public static List<PlayerControl> nmks = new List<PlayerControl>();

        public bool lightActive = false;

        public NMK()
        {
            RoleType = roleId = RoleType.NMK;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd()
        {
            foreach(var p in nmks)
            {
                // MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetNMK, Hazel.SendOption.Reliable, -1);
                // writer.Write(p.PlayerId);
                // AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.setNMK(p.PlayerId);
            }
        }
        public override void FixedUpdate()
        {
            currentTarget = setTarget(untargetablePlayers: nmks);
            setPlayerOutline(currentTarget, NMK.color);
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }
        public override void OnFinishShipStatusBegin() { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm)
        {
            Logger.info("MakeButtons");
            nmkButton = new CustomButton(
                () =>
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetNMK, Hazel.SendOption.Reliable, -1);
                    writer.Write(local.currentTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.setNMK(local.currentTarget.PlayerId);
                },
                // () => { return CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.NMK) && !CachedPlayer.LocalPlayer.PlayerControl.Data.IsDead; },
                () => {return true;},
                () =>
                {
                    if (numNMKText != null)
                    {
                        if (numNMK > nmks.Count())
                            numNMKText.text = String.Format(ModTranslation.getString("sheriffShots"), numNMK - nmks.Count());
                        else
                            numNMKText.text = "";
                    }
                    return local.currentTarget && CachedPlayer.LocalPlayer.PlayerControl.CanMove;
                },
                () => { nmkButton.Timer = nmkButton.MaxTimer; },
                NMK.getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                hm,
                hm.UseButton,
                KeyCode.F
            ){buttonText = "NMK!"};
            numNMKText = GameObject.Instantiate(nmkButton.actionButton.cooldownTimerText, nmkButton.actionButton.cooldownTimerText.transform.parent);
            numNMKText.text = "";
            numNMKText.enableWordWrapping = false;
            numNMKText.transform.localScale = Vector3.one * 0.5f;
            numNMKText.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
        }

        public static void SetButtonCooldowns()
        {
            nmkButton.MaxTimer = cooldown;
        }

        public static void Clear()
        {
            players = new List<NMK>();
            nmks = new List<PlayerControl>();
        }

        private static Sprite buttonSprite;
        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = ModTranslation.getImage("LighterButton", 115f);
            return buttonSprite;
        }
    }
}
#endif
