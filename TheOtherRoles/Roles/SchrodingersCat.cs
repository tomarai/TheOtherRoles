using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class SchrodingersCat : RoleBase<SchrodingersCat>
    {
        public static Color color = Color.grey;
        public static bool impostorFlag = false;
        public static bool jackalFlag = false;
        public static bool crewFlag = false;
        public static float killCooldown {get {return CustomOptionHolder.schrodingersCatKillCooldown.getFloat();}}
        public static bool becomesImpostor {get {return CustomOptionHolder.schrodingersCatBecomesImpostor.getBool();}}
        public static bool becomesRandomTeamOnExiled {get {return CustomOptionHolder.schrodingersCatBecomesRandomTeamOnExiled.getBool();}}
        public static bool cantKillUntilLastOne {get {return CustomOptionHolder.schrodingersCatCantKillUntilLastOne.getBool();}}
        public static bool killsKiller {get {return CustomOptionHolder.schrodingersCatKillsKiller.getBool();}}

        public SchrodingersCat()
        {
            RoleType = roleId = RoleId.SchrodingersCat;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() 
        {
            if (PlayerControl.LocalPlayer.isRole(RoleId.SchrodingersCat))
                PlayerControl.LocalPlayer.SetKillTimerUnchecked(killCooldown);
        }
        public override void FixedUpdate()
        {
            if (player == PlayerControl.LocalPlayer && jackalFlag)
            {
                if(!isTeamJackalAlive() || !cantKillUntilLastOne)
                {
                    currentTarget = setTarget();
                    setPlayerOutline(currentTarget, Sheriff.color);
                }
            }
            if (player == PlayerControl.LocalPlayer && impostorFlag && !isLastImpostor() && cantKillUntilLastOne)
            {
                HudManager.Instance.KillButton.SetTarget(null);
            }
        }

        public override void OnKill(PlayerControl target) 
        {
            if (PlayerControl.LocalPlayer == player && impostorFlag)
                player.SetKillTimerUnchecked(killCooldown);
        }
        public override void OnDeath(PlayerControl killer = null)
        {
            if(impostorFlag|| jackalFlag|| crewFlag) return;
            if(killer == null)
            {
                if(becomesRandomTeamOnExiled)
                {
                    int rndVal = Jackal.jackal != null ? rnd.Next(0, 2): rnd.Next(0, 1);
                    switch(rndVal)
                    {
                        case 0:
                            crewFlag = true;
                            break;
                        case 1:
                            impostorFlag = true;
                            break;
                        case 2:
                            jackalFlag = true;
                            break;
                        default:
                            crewFlag = true;
                            break;
                    }
                }
                else
                {
                    crewFlag = true;
                }
                return;
            }
            else
            {
                if(killer.isImpostor())
                {
                    impostorFlag = true;
                    if(becomesImpostor)
                        DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
                }
                else if(killer.isRole(RoleId.Jackal))
                {
                    jackalFlag = true;
                }
                else if(killer.isRole(RoleId.Sheriff))
                {
                    crewFlag = true;
                }
                player.Revive();
                DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
                for (int i = 0; i < array.Length; i++) {
                    if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == player.PlayerId) {
                        array[i].gameObject.active = false;
                    }     
                }
                if(killsKiller)
                {
                    player.MurderPlayer(killer);
                }
            }
        }

        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        private static CustomButton jackalKillButton;
        public static PlayerControl currentTarget;
        public static void MakeButtons(HudManager hm)
        {
                jackalKillButton = new CustomButton(
                () =>
                {
                    if (Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, SchrodingersCat.currentTarget) == MurderAttemptResult.SuppressKill) return;

                    jackalKillButton.Timer = jackalKillButton.MaxTimer;
                    Jackal.currentTarget = null;
                },
                () => { return isJackalButtonEnable(); },
                () => { return SchrodingersCat.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { jackalKillButton.Timer = jackalKillButton.MaxTimer; },
                hm.KillButton.graphic.sprite,
                new Vector3(0, 1f, 0),
                hm,
                hm.KillButton,
                KeyCode.Q
            );
        }
        public static void SetButtonCooldowns()
        {
            jackalKillButton.MaxTimer = killCooldown;
        }

        public static void Clear()
        {
            players = new List<SchrodingersCat>();
            impostorFlag = false;
            crewFlag = false;
            jackalFlag = false;
        }

        public static bool isJackalButtonEnable()
        {
            if(jackalFlag && PlayerControl.LocalPlayer.isRole(RoleId.SchrodingersCat) && PlayerControl.LocalPlayer.isAlive())
            {
                if(!isTeamJackalAlive() || !cantKillUntilLastOne )
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isTeamJackalAlive()
        {
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if(p.isRole(RoleId.Jackal) && p.isAlive()){
                    return true;
                }
                else if(p.isRole(RoleId.Sidekick) && p.isAlive()){
                    return true;
                }
            }
            return false;
        }

        public static bool isLastImpostor()
        {
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if(PlayerControl.LocalPlayer != p && p.isImpostor() && p.isAlive()) return false;
            }
            return true;
        }
    }
}