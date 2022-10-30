#if DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using HarmonyLib;
using TheOtherRoles.Objects;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using UnityEngine;
using BepInEx.IL2CPP.Utils.Collections;
namespace TheOtherRoles
{
    [HarmonyPatch]
    public class NMK : RoleBase<NMK>
    {
        public static GameObject targetAudioObject;
        public static GameObject nmkAudioObject;
        public static TMPro.TMP_Text numNMKText;
        private static CustomButton nmkButton;
        public static float maxDistance {get {return CustomOptionHolder.nmkMaxDistance.getFloat();}}
        public static float minDistance {get {return CustomOptionHolder.nmkMinDistance.getFloat();}}

        public static Color color = new Color32(172, 213, 239, byte.MaxValue);

        public static float numNMK { get { return CustomOptionHolder.nmkNum.getFloat(); } }
        public static float cooldown = 0f;

        public PlayerControl currentTarget;

        public static List<PlayerControl> nmks = new List<PlayerControl>();

        public bool lightActive = false;

        public static AudioClip nattyae;
        public static AudioClip siteyaru;
        public static AudioClip syouti;
        public static AudioClip yeah;

        public NMK()
        {
            RoleType = roleId = RoleType.NMK;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd()
        {
            foreach (var p in nmks)
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
                    writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.PlayNMKVoice, Hazel.SendOption.Reliable, -1);
                    writer.Write(local.currentTarget.PlayerId);
                    writer.Write(local.player.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.playNMKVoice(local.currentTarget.PlayerId, local.player.PlayerId);
                },
                () => { return CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.NMK) && !CachedPlayer.LocalPlayer.PlayerControl.Data.IsDead && numNMK > nmks.Count(); },
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
            )
            { buttonText = "NMK!" };
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

        public static Sprite buttonSprite;
        public static Sprite getButtonSprite()
        {
            return buttonSprite;
        }

        public static void playVoice(byte targetPlayerId, byte nmkId)
        {
            HudManager.Instance.StartCoroutine(CoPlayVoice(targetPlayerId, nmkId).WrapToIl2Cpp());
        }

        public static IEnumerator CoPlayVoice(byte targetPlayerId, byte nmkId)
        {
            var targetClip = getClipById(rnd.Next(2, 4));
            var nmkClip = getClipById(rnd.Next(0, 2));
            var target = Helpers.playerById(targetPlayerId);
            var nmk = Helpers.playerById(nmkId);
            if (!nmkAudioObject) nmkAudioObject = new GameObject("nmkAudioSource");
            AudioSource nmkAudioSource = nmkAudioObject.GetComponent<AudioSource>();
            if (nmkAudioSource == null)
            {
                nmkAudioSource = nmkAudioObject.AddComponent<AudioSource>();
                nmkAudioSource.priority = 0;
                nmkAudioSource.spatialBlend = 1;
                nmkAudioSource.clip = nmkClip;
                nmkAudioSource.loop = false;
                nmkAudioSource.playOnAwake = false;
                nmkAudioSource.maxDistance = 3;
                nmkAudioSource.minDistance = 1;
                nmkAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (!targetAudioObject) targetAudioObject= new GameObject("targetAudioSource");
            AudioSource targetAudioSource = target.gameObject.GetComponent<AudioSource>();
            if (targetAudioSource == null)
            {
                targetAudioSource = target.gameObject.AddComponent<AudioSource>();
                targetAudioSource.priority = 0;
                targetAudioSource.spatialBlend = 1;
                targetAudioSource.clip = targetClip;
                targetAudioSource.loop = false;
                targetAudioSource.playOnAwake = false;
                targetAudioSource.maxDistance = maxDistance;
                targetAudioSource.minDistance = minDistance;
                targetAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }
            nmkAudioObject.transform.position = nmk.transform.position;
            nmkAudioSource.PlayOneShot(nmkClip);
            while (nmkAudioSource.isPlaying)
            {
                nmkAudioObject.transform.position = nmk.transform.position;
                yield return new WaitForSeconds(0.04f);
            }
            targetAudioObject.transform.position = target.transform.position;
            targetAudioSource.PlayOneShot(targetClip);
            while (targetAudioSource.isPlaying)
            {
                targetAudioObject.transform.position = target.transform.position;
                yield return new WaitForSeconds(0.04f);
            }
            yield break;
        }

        public static AudioClip getClipById(int audioNum)
        {
            switch (audioNum)
            {
                case 0:
                    return nattyae;
                case 1:
                    return siteyaru;
                case 2:
                    return syouti;
                case 3:
                    return yeah;
                default:
                    return nattyae;
            }
        }
    }
}
#endif
