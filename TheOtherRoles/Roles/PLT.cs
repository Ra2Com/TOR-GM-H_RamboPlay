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
    public class PLT : RoleBase<PLT>
    {
        public static Color color = Palette.ImpostorRed;
        public static float maxDistance {get {return CustomOptionHolder.pltMaxDistance.getFloat();}}
        public static float minDistance {get {return CustomOptionHolder.pltMinDistance.getFloat();}}

        public static float cooldown = 0f;

        public PlayerControl currentTarget;

        public static GameObject audioObject;
        public static AudioClip kii;
        public static AudioClip kuroji;
        public static AudioClip narumi;
        public static AudioClip nmk;
        public static AudioClip nmk2;
        public static AudioClip plt;
        public static AudioClip joari;
        public static AudioClip kmtm;
        public static AudioClip tasuketejoari;
        public static AudioClip tasuketekii;
        public static AudioClip nazenarumi;
        public static AudioClip nazenmk;
        public static AudioClip nazekuroji;

        public static float counter = 0f;

        public PLT()
        {
            RoleType = roleId = RoleType.NMK;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd()
        {
        }
        public override void FixedUpdate()
        {
            // currentTarget = setTarget();
            // setPlayerOutline(currentTarget, PLT.color);
            if(CachedPlayer.LocalPlayer.PlayerControl.isRole(RoleType.PLT))
            {
                counter += Time.deltaTime;
                if (counter > 10)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.PlayPLTVoice, Hazel.SendOption.Reliable, -1);
                    writer.Write(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.playPLTVoice(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
                    counter = 0;
                }
            }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }
        public override void OnFinishShipStatusBegin() { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm)
        {
        }

        public static void SetButtonCooldowns()
        {
        }

        public static void Clear()
        {
            players = new List<PLT>();
            counter = 0;
        }

        public static Sprite buttonSprite;
        public static Sprite getButtonSprite()
        {
            return buttonSprite;
        }

        public static void playVoice(byte pltId)
        {
            HudManager.Instance.StartCoroutine(CoPlayVoice(pltId).WrapToIl2Cpp());
        }

        public static IEnumerator CoPlayVoice(byte pltId)
        {
            var pltClip = getClipById(rnd.Next(0, 13));
            var plt = Helpers.playerById(pltId);
            if(audioObject == null)audioObject = new GameObject("audioObject");
            AudioSource pltAudioSource = audioObject.GetComponent<AudioSource>();
            if (pltAudioSource == null)
            {
                pltAudioSource = audioObject.AddComponent<AudioSource>();
                pltAudioSource.priority = 0;
                pltAudioSource.spatialBlend = 1;
                pltAudioSource.clip = pltClip;
                pltAudioSource.loop = false;
                pltAudioSource.playOnAwake = false;
                pltAudioSource.maxDistance = maxDistance;
                pltAudioSource.minDistance = minDistance;
                pltAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            while (pltAudioSource.isPlaying)
            {
                yield return new WaitForSeconds(1);
            }
            audioObject.transform.position = plt.transform.position;
            pltAudioSource.PlayOneShot(pltClip);
            while (pltAudioSource.isPlaying)
            {
                audioObject.transform.position = plt.transform.position;
                yield return new WaitForSeconds(0.04f);
            }
            yield break;
        }

        public static AudioClip getClipById(int audioNum)
        {
            switch (audioNum)
            {
                case 0:
                    return kii;
                case 1:
                    return narumi;
                case 2:
                    return kuroji;
                case 3:
                    return joari;
                case 4:
                    return nmk;
                case 5:
                    return nmk2;
                case 6:
                    return plt;
                case 7:
                    return kmtm;
                case 8:
                    return tasuketejoari;
                case 9:
                    return tasuketekii;
                case 10:
                    return nazenarumi;
                case 11:
                    return nazenmk;
                case 12:
                    return nazekuroji;
                default:
                    return nmk;
            }
        }
    }
}
#endif
