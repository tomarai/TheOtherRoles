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
    public class Munou: RoleBase<Munou>
    {
        public static Color color = Color.grey;
        public static float camouflagerTimer = 0f;


        public Munou()
        {
            RoleType = roleId = RoleId.Munou;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate()
        {
            if(PlayerControl.LocalPlayer.isRole(RoleId.Munou) && PlayerControl.LocalPlayer.isAlive())
            {
                // ずっとカモフラージュ
                camouflagerTimer -= Time.fixedDeltaTime;
                if(camouflagerTimer <= 0)
                {
                    TheOtherRolesGM.Camouflager.camouflageTimer = 10f;
                    TheOtherRolesGM.Camouflager.startCamouflage();
                    camouflagerTimer = 5f;
                }
            }
        }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void MakeButtons(HudManager hm) { }
        public static void SetButtonCooldowns() { }

        public static void Clear()
        {
            players = new List<Munou>();
        }
    }
}