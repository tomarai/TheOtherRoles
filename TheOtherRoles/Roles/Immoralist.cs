using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class Immoralist : RoleBase<Immoralist>
    {
        public static Color color = Fox.color;
        private static CustomButton immoralistButton;

        public Immoralist()
        {
            RoleType = roleId = RoleId.NoRole;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate()
        {
            if(PlayerControl.LocalPlayer.isRole(RoleId.Immoralist))
            {
                arrowUpdate();
            }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null)
        {
            player.clearAllTasks();
        }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void SetButtonCooldowns() { }

        public static void Clear()
        {
            players = new List<Immoralist>();
        }

        private static Sprite buttonSprite;
         public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CurseButton.png", 115f);
            return buttonSprite;
        }
        public static void MakeButtons(HudManager hm)
        {
            // Fox stealth
            immoralistButton = new CustomButton(
                () => {
                    PlayerControl.LocalPlayer.MurderPlayer(PlayerControl.LocalPlayer);
                },
                () => { return PlayerControl.LocalPlayer.isRole(RoleId.Immoralist) && !PlayerControl.LocalPlayer.Data.IsDead; },
                () => {return true;},
                () => {
                    immoralistButton.Timer = immoralistButton.MaxTimer = 20;
                },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                hm,
                hm.KillButton,
                KeyCode.F,
                false,
                0,
                () => { }
            );
            immoralistButton.buttonText = ModTranslation.getString("自殺");
            immoralistButton.effectCancellable = true;
        }
        public static List<Arrow> arrows = new List<Arrow>();
        public static float updateTimer = 0f;
        public static float arrowUpdateInterval = 0.5f;
        static void arrowUpdate(){

            // 前フレームからの経過時間をマイナスする
            updateTimer -= Time.fixedDeltaTime;

            // 1秒経過したらArrowを更新
            if(updateTimer <= 0.0f){

                // 前回のArrowをすべて破棄する
                foreach(Arrow arrow in arrows){
                    arrow.arrow.SetActive(false);
                    UnityEngine.Object.Destroy(arrow.arrow);
                }

                // Arrorw一覧
                arrows = new List<Arrow>();

                // 狐の位置を示すArrorwを描画
                foreach(PlayerControl p in PlayerControl.AllPlayerControls){
                    if(p.Data.IsDead) continue;
                    Arrow arrow;
                    if(p.isRole(RoleId.Fox)){
                        arrow = new Arrow(Fox.color);
                        arrow.arrow.SetActive(true);
                        arrow.Update(p.transform.position);
                        arrows.Add(arrow);
                    }
                }

                // タイマーに時間をセット
                updateTimer = arrowUpdateInterval;
            }
        }
    }
}