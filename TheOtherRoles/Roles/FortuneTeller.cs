using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using System.Collections.Generic;
using TheOtherRoles.Objects;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using System.Text.RegularExpressions;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class FortuneTeller : RoleBase<FortuneTeller>
    {
        public static Color color = new Color32(255, 255, 255, byte.MaxValue);
        public static int numUsed = 0;
        public static List<GameObject> targetBoxes;
        public static int numTasks {get { return (int)CustomOptionHolder.fortuneTellerNumTasks.getFloat();}}
        public static bool divineOnDiscussTime {get { return CustomOptionHolder.fortuneTellerDivineOnDiscussTime.getBool();}}
        public static bool resultIsCrewOrNot {get { return CustomOptionHolder.fortuneTellerResultIsCrewOrNot.getBool();}}

        private static Sprite targetSprite;


        public FortuneTeller()
        {
            RoleType = roleId = RoleId.FortuneTeller;
        }

        public override void OnMeetingStart() { }

        public override void OnMeetingEnd() { }
        public override void OnKill(PlayerControl target) { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }
        public override void OnDeath(PlayerControl killer = null) { }

        public override void FixedUpdate() { }
        public static void Clear()
        {
            players = new List<FortuneTeller>();
            numUsed = 0;
            targetBoxes = new List<GameObject>();
        }
         public static void divine(PlayerControl p)
         {
            PlayerControl fortuneTeller = PlayerControl.LocalPlayer;
            var (tasksCompleted, tasksTotal) = TasksHandler.taskInfo(fortuneTeller.Data);
            int divineNum = ((int)tasksCompleted - (numTasks*numUsed))/numTasks;
            if(divineNum <= 0) return;
            string msg = "";
            if(!resultIsCrewOrNot){
                string roleNames = String.Join(" ", RoleInfo.getRoleInfoForPlayer(p).Select(x => Helpers.cs(x.color, x.name)).ToArray());
                roleNames = Regex.Replace(roleNames, "<[^>]*>", "");
                msg = $"{p.name}は{roleNames}";
            }else{
                string ret = p.isCrew() ? "クルー" : "クルー以外";
                msg = $"{p.name}は{ret}";
            }
            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(MeetingHud.Instance.VoteSound, false, 0.8f);
            if (!string.IsNullOrWhiteSpace(msg))
            {   
                if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                {
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);
                }
                if (msg.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    DestroyableSingleton<Assets.CoreScripts.Telemetry>.Instance.SendWho();
                }
            }
            numUsed += 1;

            // 狐の場合はキルする
            if(p.isRole(RoleId.Fox))
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FortuneTellerShoot, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                writer.Write(p.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.fortuneTellerShoot(PlayerControl.LocalPlayer.PlayerId, p.PlayerId);
            }
        }
        public static Sprite getTargetSprite() {
            if (targetSprite) return targetSprite;
            targetSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Uranai.png", 150f);
            return targetSprite;
        }
        static void fortuneTellerOnClick(int buttonTarget, MeetingHud __instance) {
            PlayerControl p = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId); 
            FortuneTeller.divine(p);
        }

        public static void populateButtonsPostfix(MeetingHud __instance)
        {
            // Add FortuneTeller Buttons
            PlayerControl player = PlayerControl.LocalPlayer;
            if (player.isRole(RoleId.FortuneTeller) && !player.Data.IsDead) {
                FortuneTeller.targetBoxes = new List<GameObject>();
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == player.PlayerId) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "DivineButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = FortuneTeller.getTargetSprite();
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => fortuneTellerOnClick(copiedIndex, __instance)));
                    FortuneTeller.targetBoxes.Add(targetBox);
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
        class MeetingServerStartPatch {
            static void Postfix(MeetingHud __instance)
            {
                populateButtonsPostfix(__instance);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
        class MeetingDeserializePatch {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)]MessageReader reader, [HarmonyArgument(1)]bool initialState)
            {
                if (initialState) {
                    populateButtonsPostfix(__instance);
                }
            }
        }
         [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        class MeetingHudUpdatePatch {
            static void Postfix(MeetingHud __instance) {
                // Deactivate FortuneTeller Button
                PlayerControl player = PlayerControl.LocalPlayer;
                if (player.isRole(RoleId.FortuneTeller)){
                    var (tasksCompleted, tasksTotal) = TasksHandler.taskInfo(player.Data);
                    int divineNum = ((int)tasksCompleted - ((int)FortuneTeller.numTasks*FortuneTeller.numUsed))/(int)FortuneTeller.numTasks;
                    bool isActive = divineNum > 0;
                    if(FortuneTeller.divineOnDiscussTime)
                    {
                        if(isActive && __instance.state == MeetingHud.VoteStates.Discussion){
                            foreach(GameObject box in FortuneTeller.targetBoxes){
                                box.SetActive(true);
                            }
                        } else{
                            foreach(GameObject box in FortuneTeller.targetBoxes){
                                box.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                       if(isActive)
                       {
                            foreach(GameObject box in FortuneTeller.targetBoxes){
                                box.SetActive(true);
                            }
                       } 
                       else
                       {
                            foreach(GameObject box in FortuneTeller.targetBoxes){
                                box.SetActive(false);
                            }

                       }
                    }
                }
            }
        }
    }

}