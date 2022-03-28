using HarmonyLib;
using Hazel;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TheOtherRolesGM;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerTools;
using TMPro;
using UnhollowerBaseLib;

namespace TheOtherRoles.Patches {

    [HarmonyPatch(typeof(ShipStatus))]
    public class ShipStatusPatch {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
        public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo player) {
            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
            if (systemType == null) return true;
            SwitchSystem switchSystem = systemType.TryCast<SwitchSystem>();
            if (switchSystem == null) return true;

            float num = (float)switchSystem.Value / 255f;
            
            if (player == null || player.IsDead || player.PlayerId == GM.gm?.PlayerId) // IsDead
                __result = __instance.MaxLightRadius;
            else if (player.Role.IsImpostor
                || (Jackal.jackal != null && Jackal.jackal.PlayerId == player.PlayerId && Jackal.hasImpostorVision)
                || (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == player.PlayerId && Sidekick.hasImpostorVision)
                || (Spy.spy != null && Spy.spy.PlayerId == player.PlayerId && Spy.hasImpostorVision)
                || (player.Object.hasModifier(ModifierType.Madmate) && Madmate.hasImpostorVision) // Impostor, Jackal/Sidekick, Spy, or Madmate with Impostor vision
                || (player.Object.hasModifier(ModifierType.CreatedMadmate) && CreatedMadmate.hasImpostorVision) // Impostor, Jackal/Sidekick, Spy, or Madmate with Impostor vision
                || (player.Object.isRole(RoleType.Puppeteer))
                || (Jester.jester != null && Jester.jester.PlayerId == player.PlayerId && Jester.hasImpostorVision) // Jester with Impostor vision
                || (player.Object.isRole(RoleType.Fox))
                )
                __result = __instance.MaxLightRadius * PlayerControl.GameOptions.ImpostorLightMod;
            else if (PlayerControl.LocalPlayer.isRole(RoleType.Lighter) && Lighter.isLightActive(PlayerControl.LocalPlayer)) // if player is Lighter and Lighter has his ability active
                __result = Mathf.Lerp(__instance.MaxLightRadius * Lighter.lighterModeLightsOffVision, __instance.MaxLightRadius * Lighter.lighterModeLightsOnVision, num);
            else if (Trickster.trickster != null && Trickster.lightsOutTimer > 0f) {
                float lerpValue = 1f;
                if (Trickster.lightsOutDuration - Trickster.lightsOutTimer < 0.5f) lerpValue = Mathf.Clamp01((Trickster.lightsOutDuration - Trickster.lightsOutTimer) * 2);
                else if (Trickster.lightsOutTimer < 0.5) lerpValue = Mathf.Clamp01(Trickster.lightsOutTimer * 2);
                __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, 1 - lerpValue) * PlayerControl.GameOptions.CrewLightMod; // Instant lights out? Maybe add a smooth transition?
            }
            else if (Lawyer.lawyer != null && Lawyer.lawyer.PlayerId == player.PlayerId) // if player is Lighter and Lighter has his ability active
                __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius * Lawyer.vision, num);
            else
                __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, num) * PlayerControl.GameOptions.CrewLightMod;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
        public static void Postfix2(ShipStatus __instance, ref bool __result)
        {
            __result = false;
        }

        private static int originalNumCommonTasksOption = 0;
        private static int originalNumShortTasksOption = 0;
        private static int originalNumLongTasksOption = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        public static bool Prefix(ShipStatus __instance)
        {
            if (CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.playerColorRandom.getBool() && AmongUsClient.Instance.AmHost)
            {
                List<int> colors = Enumerable.Range(0, Palette.PlayerColors.Count).ToList();
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    int i = TheOtherRoles.rnd.Next(0, colors.Count);
                    p.SetColor(colors[i]);
                    p.RpcSetColor((byte)colors[i]);
                    colors.RemoveAt(i);
                }
            }

            var commonTaskCount = __instance.CommonTasks.Count;
            var normalTaskCount = __instance.NormalTasks.Count;
            var longTaskCount = __instance.LongTasks.Count;
            originalNumCommonTasksOption = PlayerControl.GameOptions.NumCommonTasks;
            originalNumShortTasksOption = PlayerControl.GameOptions.NumShortTasks;
            originalNumLongTasksOption = PlayerControl.GameOptions.NumLongTasks;
            if(PlayerControl.GameOptions.NumCommonTasks > commonTaskCount) PlayerControl.GameOptions.NumCommonTasks = commonTaskCount;
            if(PlayerControl.GameOptions.NumShortTasks > normalTaskCount) PlayerControl.GameOptions.NumShortTasks = normalTaskCount;
            if(PlayerControl.GameOptions.NumLongTasks > longTaskCount) PlayerControl.GameOptions.NumLongTasks = longTaskCount;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        public static void Postfix3(ShipStatus __instance)
        {
            // Restore original settings after the tasks have been selected
            PlayerControl.GameOptions.NumCommonTasks = originalNumCommonTasksOption;
            PlayerControl.GameOptions.NumShortTasks = originalNumShortTasksOption;
            PlayerControl.GameOptions.NumLongTasks = originalNumLongTasksOption;
        }
            
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SpawnPlayer))]
        public static void Postfix(ShipStatus __instance, PlayerControl player, int numPlayers, bool initialSpawn){
            // Polusの湧き位置をランダムにする 無駄に人数分シャッフルが走るのをそのうち直す
            if(PlayerControl.GameOptions.MapId == 2 && CustomOptionHolder.polusRandomSpawn.getBool()){
                if(AmongUsClient.Instance.AmHost){
                    System.Random rand = new System.Random();
                    int randVal = rand.Next(0,6);
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RandomSpawn, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte)player.Data.PlayerId);
                    writer.Write((byte)randVal);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.randomSpawn((byte)player.Data.PlayerId, (byte)randVal);
                }
            }
        }

        private static List<SpawnCandidate> SpawnCandidates = new List<SpawnCandidate>();
        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Begin))]
        public static bool Prefix(SpawnInMinigame __instance, PlayerTask task)
        {
            // base.Begin(task);
            __instance.MyTask = task;
            __instance.MyNormTask = (task as NormalPlayerTask);
            if (PlayerControl.LocalPlayer)
            {
                if (MapBehaviour.Instance)
                {
                    MapBehaviour.Instance.Close();
                }
                PlayerControl.LocalPlayer.NetTransform.Halt();
            }
            __instance.StartCoroutine(__instance.CoAnimateOpen());

            // Additional Locations
            // vault -8.782744,8.569022
            // meeting 10.99753,14.73402
            // cockpit -22.03774,-1.175882
            // elec 16.37233,-8.558313
            // lounge 30.86165,7.473174,
            // medical 25.45923,-5.008366
            // security 10.3455,-16.15856
            // viewing deck -14.10035,-16.20251
            // armory -10.72389,-6.35868
            // comms -11.82773,3.18128
            // shower 20.77513,2.811245
            // brig 13.83497,6.367104
            SpawnCandidates.Add(new SpawnCandidate(StringNames.VaultRoom, new Vector2(-8.8f, 8.6f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.MeetingRoom, new Vector2(11.0f, 14.7f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Cockpit, new Vector2(-22.0f, -1.2f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Electrical, new Vector2(-16.4f, -8.5f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Lounge, new Vector2(30.9f, 7.5f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Medical, new Vector2(25.5f, -5.0f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Security, new Vector2(10.3f, -16.2f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.ViewingDeck, new Vector2(-14.1f, -16.2f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Armory, new Vector2(-10.7f, -6.3f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Comms, new Vector2(-11.8f, 3.2f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Showers, new Vector2(20.8f, 2.8f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));
            SpawnCandidates.Add(new SpawnCandidate(StringNames.Brig, new Vector2(13.8f, 6.4f), "TheOtherRoles.Resources.Locations.dummy.png", "rollover_brig"));

            List<SpawnInMinigame.SpawnLocation> list = __instance.Locations.ToList<SpawnInMinigame.SpawnLocation>();
            foreach(var spawnCandidate in SpawnCandidates)
            {
                spawnCandidate.ReloadTexture();
                SpawnInMinigame.SpawnLocation spawnlocation = new SpawnInMinigame.SpawnLocation();
                spawnlocation.Location = spawnCandidate.SpawnLocation;
                spawnlocation.Image = spawnCandidate.GetSprite();
                spawnlocation.Name = spawnCandidate.LocationKey;
                spawnlocation.Rollover = new AnimationClip();
                spawnlocation.RolloverSfx = __instance.DefaultRolloverSound;
                list.Add(spawnlocation);
            }

            SpawnInMinigame.SpawnLocation[] array = list.ToArray<SpawnInMinigame.SpawnLocation>();
            array.Shuffle(0);
            array = (from s in array.Take(__instance.LocationButtons.Length)
            orderby s.Location.x, s.Location.y descending
            select s).ToArray<SpawnInMinigame.SpawnLocation>();
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(-25f, 40f));

            for (int i = 0; i < __instance.LocationButtons.Length; i++)
            {
                PassiveButton passiveButton = __instance.LocationButtons[i];
                SpawnInMinigame.SpawnLocation pt = array[i];
                passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SpawnAt(pt.Location)));
                passiveButton.GetComponent<SpriteAnim>().Stop();
                passiveButton.GetComponent<SpriteRenderer>().sprite = pt.Image;
                // passiveButton.GetComponentInChildren<TextMeshPro>().text = DestroyableSingleton<TranslationController>.Instance.GetString(pt.Name, Array.Empty<object>());
                passiveButton.GetComponentInChildren<TextMeshPro>().text = DestroyableSingleton<TranslationController>.Instance.GetString(pt.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                ButtonAnimRolloverHandler component = passiveButton.GetComponent<ButtonAnimRolloverHandler>();
                component.StaticOutImage = pt.Image;
                component.RolloverAnim = pt.Rollover;
                component.HoverSound = (pt.RolloverSfx ? pt.RolloverSfx : __instance.DefaultRolloverSound);
            }




            PlayerControl.LocalPlayer.gameObject.SetActive(false);
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(-25f, 40f));
            if (CustomOptionHolder.airshipRandomSpawn.getBool())
            {
                // Helpers.log("ランダム");
                __instance.LocationButtons.Random<PassiveButton>().ReceiveClickUp();
            }
            else
            {
                // Helpers.log("Notランダム");
                __instance.StartCoroutine(__instance.RunTimer());
            }
            ControllerManager.Instance.OpenOverlayMenu(__instance.name, null, __instance.DefaultButtonSelected, __instance.ControllerSelectable, false);
            PlayerControl.HideCursorTemporarily();
            ConsoleJoystick.SetMode_Menu();
            return false;
        }
    }
}
