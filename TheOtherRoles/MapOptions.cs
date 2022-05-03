using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles{
    static class MapOptions {
        // Set values
        public static int maxNumberOfMeetings = 10;
        public static bool blockSkippingInEmergencyMeetings = false;
        public static bool noVoteIsSelfVote = false;
        public static bool hidePlayerNames = false;
        public static bool ghostsSeeRoles = true;
        public static bool ghostsSeeModifier = true;
        public static bool ghostsSeeTasks = true;
        public static bool ghostsSeeVotes = true;
        public static bool showRoleSummary = true;
        public static bool allowParallelMedBayScans = false;
        public static bool showLighterDarker = true;
        public static bool enableHorseMode = false;
        public static bool shieldFirstKill = false;

        // Updating values
        public static int meetingsCount = 0;
        public static List<SurvCamera> camerasToAdd = new List<SurvCamera>();
        public static List<Vent> ventsToSeal = new List<Vent>();
        public static Dictionary<byte, PoolablePlayer> playerIcons = new Dictionary<byte, PoolablePlayer>();
        public static float AdminTimer = 0f;
        public static TMPro.TextMeshPro AdminTimerText = null;
        public static string firstKillName;
        public static PlayerControl firstKillPlayer;

        public static void clearAndReloadMapOptions() {
            meetingsCount = 0;
            camerasToAdd = new List<SurvCamera>();
            ventsToSeal = new List<Vent>();
            playerIcons = new Dictionary<byte, PoolablePlayer>(); ;

            AdminTimer = CustomOptionHolder.adminTimer.getFloat();
            ClearAdminTimerText();
            UpdateAdminTimerText();

            maxNumberOfMeetings = Mathf.RoundToInt(CustomOptionHolder.maxNumberOfMeetings.getSelection());
            blockSkippingInEmergencyMeetings = CustomOptionHolder.blockSkippingInEmergencyMeetings.getBool();
            noVoteIsSelfVote = CustomOptionHolder.noVoteIsSelfVote.getBool();
            hidePlayerNames = CustomOptionHolder.hidePlayerNames.getBool();
            allowParallelMedBayScans = CustomOptionHolder.allowParallelMedBayScans.getBool();
            shieldFirstKill = CustomOptionHolder.shieldFirstKill.getBool();
            firstKillPlayer = null;
        }

        public static void reloadPluginOptions() {
            ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value;
            ghostsSeeModifier = TheOtherRolesPlugin.GhostsSeeModifier.Value;
            ghostsSeeTasks = TheOtherRolesPlugin.GhostsSeeTasks.Value;
            ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value;
            showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value;
            showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value;
            enableHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value;
            Patches.ShouldAlwaysHorseAround.isHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value;
        }

        public static void MeetingEndedUpdate()
        {
            ClearAdminTimerText();
            UpdateAdminTimerText();
        }

        public static void UpdateAdminTimerText()
        {
            if (!CustomOptionHolder.enabledAdminTimer.getBool())
                return;
            if (HudManager.Instance == null)
                return;
            AdminTimerText = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, HudManager.Instance.transform);
            AdminTimerText.transform.localPosition = new Vector3(-3.5f, -4.0f, 0);
            if (AdminTimer > 0)
                AdminTimerText.text = $"Admin: {Mathf.RoundToInt(AdminTimer)} sec remaining";
            else
                AdminTimerText.text = "Admin: ran out of time";
            AdminTimerText.gameObject.SetActive(true);
        }

        private static void ClearAdminTimerText()
        {
            if (AdminTimerText == null)
                return;
            UnityEngine.Object.Destroy(AdminTimerText);
            AdminTimerText = null;
        }
    }
}
