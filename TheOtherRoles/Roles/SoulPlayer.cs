using HarmonyLib;
using TheOtherRoles.Objects;
using UnityEngine;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class SoulPlayer : RoleBase<Template>
    {
        public static Color color = Palette.CrewmateBlue;
        private static CustomButton senriganButton;
        private static bool enableSenrigan { get { return CustomOptionHolder.enableSenrigan.getBool(); } }
        public static bool toggle = false;
        public static Sprite senriganIcon;
        public static void senrigan()
        {
            var hm = DestroyableSingleton<HudManager>.Instance;
            if (toggle)
            {
                Camera.main.orthographicSize /= 6f;
                hm.UICamera.orthographicSize /= 6f;
                hm.transform.localScale /= 6f;
            }
            else
            {
                Camera.main.orthographicSize *= 6f;
                hm.UICamera.orthographicSize *= 6f;
                hm.transform.localScale *= 6f;
            }
            toggle = !toggle;
        }

        public SoulPlayer()
        {
            RoleType = roleId = RoleType.NoRole;
        }

        public override void OnMeetingStart()
        {
        }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate() { }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm)
        {
            senriganButton = new CustomButton(
                () =>
                {/*ボタンが押されたとき*/
                    senrigan();
                },
                () => {/*ボタンが有効になる条件*/ return enableSenrigan && PlayerControl.LocalPlayer.isDead() && !PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer); },
                () => {/*ボタンが使える条件*/ return PlayerControl.LocalPlayer.isDead(); },
                () => {/*ミーティング終了時*/ },
                getSenriganIcon(),
                new Vector3(-1.8f, -0.06f, 0),
                hm,
                hm.AbilityButton,
                KeyCode.F
            )
            {
                MaxTimer = 0f,
                Timer = 0f,
                buttonText = ModTranslation.getString("")
            };
        }
        public static Sprite getSenriganIcon()
        {
            if (senriganIcon) return senriganIcon;
            senriganIcon = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Senrigan.png", 115f);
            return senriganIcon;
        }
        public static void SetButtonCooldowns()
        {
            senriganButton.Timer = senriganButton.MaxTimer = 0f;

        }



        public static void Clear()
        {
            toggle = false;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoStartMeeting))]
        class StartMeetingPatch
        {
            public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo meetingTarget)
            {
                if (PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (toggle)
                    {
                        senrigan();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
        class MinigameBeginPatch
        {
            static void Prefix(Minigame __instance)
            {
                if (PlayerControl.LocalPlayer.isDead())
                {
                    if (toggle)
                    {
                        senrigan();
                    }
                }
            }
        }
    }
}
