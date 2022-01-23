using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public class Madmate : RoleBase<Madmate>
    {
        public static Color color = Palette.ImpostorRed;

        public static bool canEnterVents { get { return CustomOptionHolder.madmateCanEnterVents.getBool(); } }
        public static bool hasImpostorVision { get { return CustomOptionHolder.madmateHasImpostorVision.getBool(); } }
        public static bool canSabotage { get { return CustomOptionHolder.madmateCanSabotage.getBool(); } }
        public static bool canFixComm { get { return CustomOptionHolder.madmateCanFixComm.getBool(); } }
        public static bool noticeImpostors { get { return CustomOptionHolder.madmateNoticeImpostors.getBool(); } }
        public static int commonTasks { get { return Mathf.RoundToInt(CustomOptionHolder.madmateCommonTasks.getFloat()); } }
        public static int shortTasks { get { return Mathf.RoundToInt(CustomOptionHolder.madmateShortTasks.getFloat()); } }
        public static int longTasks { get { return Mathf.RoundToInt(CustomOptionHolder.madmateLongTasks.getFloat()); } }
        public static bool exileCrewmate { get { return CustomOptionHolder.madmateExileCrewmate.getBool(); } }

        public Madmate()
        {
            RoleType = roleId = RoleId.Madmate;
        }

        public override void OnMeetingStart() { }
        public override void OnMeetingEnd() { }
        public override void FixedUpdate() { }
        public override void OnKill(PlayerControl target) { }
        public override void OnDeath(PlayerControl killer = null) { }
        public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason) { }

        public static void Clear()
        {
            players = new List<Madmate>();
        }
    }
}