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
        public static bool hideSettings = false;
        public static bool hideOutOfSightNametags = false;

        public static bool randomizeColors = false;
        public static bool allowDupeNames = false;

        public static int restrictDevices = 0;
        public static bool restrictAdmin = true;
        public static float restrictAdminTime = 600f;
        public static float restrictAdminTimeMax = 600f;
        public static bool restrictAdminText = true;
        public static bool restrictCameras = true;
        public static float restrictCamerasTime = 600f;
        public static float restrictCamerasTimeMax = 600f;
        public static bool restrictVitals = true;
        public static float restrictVitalsTime = 600f;
        public static float restrictVitalsTimeMax = 600f;
        public static bool disableVents = false;

        public static bool ghostsSeeRoles = true;
        public static bool ghostsSeeTasks = true;
        public static bool ghostsSeeVotes = true;
        public static bool showRoleSummary = true;
        public static bool hideNameplates = false;
        public static bool allowParallelMedBayScans = false;
        public static bool showLighterDarker = false;
        public static bool hideTaskArrows = false;

        // Updating values
        public static int meetingsCount = 0;
        public static List<SurvCamera> camerasToAdd = new List<SurvCamera>();
        public static List<Vent> ventsToSeal = new List<Vent>();
        public static Dictionary<byte, PoolablePlayer> playerIcons = new Dictionary<byte, PoolablePlayer>();
        public static TMPro.TextMeshPro AdminTimerText = null;

        public static void clearAndReloadMapOptions() {
            meetingsCount = 0;
            camerasToAdd = new List<SurvCamera>();
            ventsToSeal = new List<Vent>();
            playerIcons = new Dictionary<byte, PoolablePlayer>();

            maxNumberOfMeetings = Mathf.RoundToInt(CustomOptionHolder.maxNumberOfMeetings.getSelection());
            blockSkippingInEmergencyMeetings = CustomOptionHolder.blockSkippingInEmergencyMeetings.getBool();
            noVoteIsSelfVote = CustomOptionHolder.noVoteIsSelfVote.getBool();
            hidePlayerNames = CustomOptionHolder.hidePlayerNames.getBool();

            hideOutOfSightNametags = CustomOptionHolder.hideOutOfSightNametags.getBool();

            hideSettings = CustomOptionHolder.hideSettings.getBool();

            randomizeColors = CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.playerColorRandom.getBool();
            allowDupeNames = CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.playerNameDupes.getBool();

            restrictDevices = CustomOptionHolder.restrictDevices.getSelection();
            restrictAdmin = CustomOptionHolder.restrictAdmin.getBool();
            restrictAdminTime = restrictAdminTimeMax = CustomOptionHolder.restrictAdminTime.getFloat();
            restrictAdminText = CustomOptionHolder.restrictAdminText.getBool();
            restrictCameras = CustomOptionHolder.restrictCameras.getBool();
            restrictCamerasTime = restrictCamerasTimeMax = CustomOptionHolder.restrictCamerasTime.getFloat();
            restrictVitals = CustomOptionHolder.restrictVitals.getBool();
            restrictVitalsTime = restrictVitalsTimeMax = CustomOptionHolder.restrictVitalsTime.getFloat();
            disableVents = CustomOptionHolder.disableVents.getBool();
            ClearAdminTimerText();
            UpdateAdminTimerText();

            allowParallelMedBayScans = CustomOptionHolder.allowParallelMedBayScans.getBool();
            ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value;
            ghostsSeeTasks = TheOtherRolesPlugin.GhostsSeeTasks.Value;
            ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value;
            showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value;
            hideNameplates = TheOtherRolesPlugin.HideNameplates.Value;
            showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value;
            hideTaskArrows = TheOtherRolesPlugin.HideTaskArrows.Value;
        }

        public static void resetDeviceTimes()
        {
            restrictAdminTime = restrictAdminTimeMax;
            restrictCamerasTime = restrictCamerasTimeMax;
            restrictVitalsTime = restrictVitalsTimeMax;
        }

        public static bool canUseAdmin
        {
            get
            {
                return restrictDevices == 0 || restrictAdminTime > 0f;
            }
        }

        public static bool couldUseAdmin
        {
            get
            {
                return restrictDevices == 0 || !restrictAdmin || restrictAdminTimeMax > 0f;
            }
        }

        public static bool canUseCameras
        {
            get
            {
                return restrictDevices == 0 || !restrictCameras || restrictCamerasTime > 0f;
            }
        }

        public static bool couldUseCameras
        {
            get
            {
                return restrictDevices == 0 || !restrictCameras || restrictCamerasTimeMax > 0f;
            }
        }

        public static bool canUseVitals
        {
            get
            {
                return restrictDevices == 0 || !restrictCameras || restrictVitalsTime > 0f;
            }
        }

        public static bool couldUseVitals
        {
            get
            {
                return restrictDevices == 0 || !restrictCameras || restrictVitalsTimeMax > 0f;
            }
        }
        public static void MeetingEndedUpdate()
        {
            ClearAdminTimerText();
            UpdateAdminTimerText();
        }
        public static void UpdateAdminTimerText()
        {
            if (restrictDevices == 0 || !restrictAdmin || !restrictAdminText)
                return;
            if (HudManager.Instance == null)
                return;
            AdminTimerText = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, HudManager.Instance.transform);
            AdminTimerText.transform.localPosition = new Vector3(-3.5f, -4.0f, 0);
            if (restrictAdminTime> 0)
                // AdminTimerText.text = $"Admin: {Mathf.RoundToInt(restrictAdminTime)} sec remaining";
                AdminTimerText.text = String.Format(ModTranslation.getString("adminText"),restrictAdminTime.ToString("0.00"));
            else
                // AdminTimerText.text = "Admin: ran out of time";
                AdminTimerText.text = ModTranslation.getString("adminRanOut");
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