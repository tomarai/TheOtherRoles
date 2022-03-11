using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class Trapper : RoleBase<Trapper>
    {
        public enum Status{
            notPlaced,
            placed,
            active,
        }
        public static Color color = Palette.ImpostorRed;
        public static Sprite trapButtonSprite;
        public static DateTime placedTime;
        public static int numTrap {get {return (int)CustomOptionHolder.trapperNumTrap.getFloat();}}
        public static float extensionTime {get {return CustomOptionHolder.trapperExtensionTime.getFloat();}}
        public static float killTimer {get {return CustomOptionHolder.trapperKillTimer.getFloat();}}
        public static float cooldown {get {return CustomOptionHolder.trapperCooldown.getFloat();}}
        public static float minDsitance {get {return CustomOptionHolder.trapperMinDistance.getFloat();}}
        public static float maxDistance {get {return CustomOptionHolder.trapperMaxDistance.getFloat();}}
        public static float trapRange {get {return CustomOptionHolder.trapperTrapRange.getFloat();}}
        public static float penaltyTime {get {return CustomOptionHolder.trapperPenaltyTime.getFloat();}}
        public static float bonusTime {get {return CustomOptionHolder.trapperBonusTime.getFloat();}}
        public static bool isTrapKill = false;
        public static bool meetingFlag;
        

        public Trapper()
        {
            RoleType = roleId = RoleType.NoRole;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() 
        {
            Trap.clearAllTraps();
            meetingFlag = false;
        }

        public override void FixedUpdate() 
        {
            // 処理に自身がないので念の為tryで囲っておく
            try{
                if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper) && Trap.traps.Count != 0 && !Trap.hasTrappedPlayer() && !meetingFlag)
                {
                    // トラップを踏んだプレイヤーを動けなくする 
                    foreach(var p in PlayerControl.AllPlayerControls)
                    {
                        foreach(var trap in Trap.traps)
                        {
                            if(DateTime.UtcNow.Subtract(trap.Value.placedTime).TotalSeconds < extensionTime) continue;
                            if(trap.Value.isActive || p.isDead() || p.inVent || meetingFlag) continue;
                            var p1 = p.transform.localPosition;
                            Dictionary<GameObject, byte> listActivate = new Dictionary<GameObject, byte>();
                            var p2 = trap.Value.trap.transform.localPosition;
                            var distance = Vector3.Distance(p1, p2);
                            if(distance < trapRange)
                            {
                                TMPro.TMP_Text text;
                                RoomTracker roomTracker =  HudManager.Instance?.roomTracker;
                                GameObject gameObject = UnityEngine.Object.Instantiate(roomTracker.gameObject);
                                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                                gameObject.transform.SetParent(HudManager.Instance.transform);
                                gameObject.transform.localPosition = new Vector3(0, -1.8f, gameObject.transform.localPosition.z);
                                gameObject.transform.localScale = Vector3.one * 2f;
                                text = gameObject.GetComponent<TMPro.TMP_Text>();
                                text.text = p.name + "が罠にかかった";
                                HudManager.Instance.StartCoroutine(Effects.Lerp(3f, new Action<float>((p) => {
                                    if (p == 1f && text != null && text.gameObject != null) {
                                        UnityEngine.Object.Destroy(text.gameObject);
                                    }
                                })));
                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ActivateTrap, Hazel.SendOption.Reliable, -1);
                                writer.Write(trap.Key);
                                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                                writer.Write(p.PlayerId);
                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                RPCProcedure.activateTrap(trap.Key, PlayerControl.LocalPlayer.PlayerId, p.PlayerId);
                                break;
                            }
                        }
                    }
                }

                if(PlayerControl.LocalPlayer.isRole(RoleType.Trapper) && Trap.hasTrappedPlayer() && !meetingFlag)
                {
                    // トラップにかかっているプレイヤーを救出する
                    foreach(var trap in Trap.traps)
                    {
                        if(trap.Value.trap == null || !trap.Value.isActive) return;
                        Vector3 p1 = trap.Value.trap.transform.position;
                        foreach(var player in PlayerControl.AllPlayerControls)
                        {
                            if (player.PlayerId == trap.Value.target.PlayerId || player.isDead() || player.inVent|| player.isRole(RoleType.Trapper)) continue;
                            Vector3 p2 = player.transform.position;
                            float distance = Vector3.Distance(p1, p2);
                            if(distance < 0.5)
                            {
                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DisableTrap, Hazel.SendOption.Reliable, -1);
                                writer.Write(trap.Key);
                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                RPCProcedure.disableTrap(trap.Key);
                            }
                        }

                    }
                    
                }
            }
            catch (NullReferenceException e){
                Helpers.log(e.Message);
            }
        }
    public override void OnKill(PlayerControl target) 
    {
        //　キルクールダウン設定
        if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper))
        {
            if (Trap.isTrapped(target) && !isTrapKill)  // トラップにかかっている対象をキルした場合のボーナス
            {
                Helpers.log("トラップにかかっている対象をキルした場合のボーナス");
                player.killTimer = PlayerControl.GameOptions.KillCooldown - bonusTime;
                trapperSetTrapButton.Timer = cooldown - bonusTime;
            }
            else if (Trap.isTrapped(target) && isTrapKill)  // トラップキルした場合のペナルティ
            {
                Helpers.log("トラップキルした場合のクールダウン");
                player.killTimer = PlayerControl.GameOptions.KillCooldown;
                trapperSetTrapButton.Timer = cooldown;
            }
            else // トラップにかかっていない対象を通常キルした場合はペナルティーを受ける
            {
                Helpers.log("通常キル時のペナルティ");
                player.killTimer = PlayerControl.GameOptions.KillCooldown + penaltyTime;
                trapperSetTrapButton.Timer = cooldown + penaltyTime;
            }
            if(!isTrapKill)
            {
                MessageWriter writer;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ClearTrap, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.clearTrap();
            }
            isTrapKill = false;
        }
    }
    public override void OnDeath(PlayerControl killer = null) { }
    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

    public static CustomButton trapperSetTrapButton;
    public static void MakeButtons(HudManager hm)
    {
        trapperSetTrapButton = new CustomButton(
            () => { // ボタンが押された時に実行
                if (!PlayerControl.LocalPlayer.CanMove || Trap.hasTrappedPlayer()) return;
                Trapper.setTrap();
                trapperSetTrapButton.Timer = trapperSetTrapButton.MaxTimer;
            },
            () => { /*ボタン有効になる条件*/
                return PlayerControl.LocalPlayer.isRole(RoleType.Trapper) && !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () => { /*ボタンが使える条件*/
                return PlayerControl.LocalPlayer.CanMove && !Trap.hasTrappedPlayer();
            },
            () => { /*ミーティング終了時*/
                trapperSetTrapButton.Timer = trapperSetTrapButton.MaxTimer;
            },
            Trapper.getTrapButtonSprite(),
            // new Vector3(-2.6f, 0f, 0f),
            new Vector3(-1.8f, -0.06f, 0f),
            hm,
            hm.AbilityButton,
            KeyCode.F
        );
        trapperSetTrapButton.buttonText = "罠設置";
    }
    public static void SetButtonCooldowns()
    {
        trapperSetTrapButton.MaxTimer = cooldown;
    }

        public static void Clear()
        {
            players = new List<Trapper>();
            meetingFlag = false;
            Trap.clearAllTraps();
        }

        public static Sprite getTrapButtonSprite() {
            if (trapButtonSprite) return trapButtonSprite;
            trapButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrapperButton.png", 115f);
            return trapButtonSprite;
        }
        public static void setTrap(){
            var pos = PlayerControl.LocalPlayer.transform.position;
            byte[] buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaceTrap, Hazel.SendOption.Reliable);
            writer.WriteBytesAndSize(buff);
            writer.EndMessage();
            RPCProcedure.placeTrap(buff);
            placedTime = DateTime.UtcNow;
        }

        private static Sprite trapeffectSprite;
        public static Sprite getTrapEffectSprite() {
            if (trapeffectSprite) return trapeffectSprite;
            trapeffectSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrapEffect.png", 300f);
            return trapeffectSprite;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        class PlayerControlCmdReportDeadBodyPatch
        {
            public static void Prefix(PlayerControl __instance)
            {
                // トラップ中にミーティングが来たら直後に死亡する
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TrapperMeetingFlag, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.trapperMeetingFlag();
            }
        }
        
    }
}