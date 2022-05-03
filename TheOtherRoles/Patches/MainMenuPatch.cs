using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using TheOtherRoles.Patches;

namespace TheOtherRoles.Modules {
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuPatch {
        private static bool horseButtonState = MapOptions.enableHorseMode;
        private static Sprite horseModeOffSprite = null;
        private static Sprite horseModeOnSprite = null;

        private static void Prefix(MainMenuManager __instance) {
            CustomHatLoader.LaunchHatFetcher();
            var template = GameObject.Find("ExitGameButton");
            if (template == null) return;

            // Horse mode stuff
            var horseModeSelectionBehavior = new ClientOptionsPatch.SelectionBehaviour("Enable Horse Mode", () => MapOptions.enableHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value = !TheOtherRolesPlugin.EnableHorseMode.Value, TheOtherRolesPlugin.EnableHorseMode.Value);

            var bottomTemplate = GameObject.Find("InventoryButton");
            if (bottomTemplate == null) return;
            var horseButton = Object.Instantiate(bottomTemplate, bottomTemplate.transform.parent);
            var passiveHorseButton = horseButton.GetComponent<PassiveButton>();
            var spriteHorseButton = horseButton.GetComponent<SpriteRenderer>();

            horseModeOffSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HorseModeButtonOff.png", 75f);
            horseModeOnSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HorseModeButtonOn.png", 75f);

            spriteHorseButton.sprite = horseButtonState ? horseModeOnSprite : horseModeOffSprite;

            passiveHorseButton.OnClick = new ButtonClickedEvent();

            passiveHorseButton.OnClick.AddListener((System.Action)delegate {
                horseButtonState = horseModeSelectionBehavior.OnClick();
                if (horseButtonState) {
                    if (horseModeOnSprite == null) horseModeOnSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HorseModeButtonOn.png", 75f);
                    spriteHorseButton.sprite = horseModeOnSprite;
                }
                else {
                    if (horseModeOffSprite == null) horseModeOffSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HorseModeButtonOff.png", 75f);
                    spriteHorseButton.sprite = horseModeOffSprite;
                }
                CredentialsPatch.LogoPatch.updateSprite();
                // Avoid wrong Player Particles floating around in the background
                var particles = GameObject.FindObjectOfType<PlayerParticles>(); 
                if (particles != null) {
                    particles.pool.ReclaimAll();
                    particles.Start();
                }
            });
        }
    }

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
    public static class Announcement
    {
        public static ModUpdateBehaviour.UpdateData updateData = null;
        public static bool Prefix(AnnouncementPopUp __instance)
        {
            if (ModUpdateBehaviour.showPopUp || updateData == null) return true;

            var text = __instance.AnnounceTextMeshPro;            
            text.text = $"<size=150%><color=#FC0303>THE OTHER ROLES </color> {(updateData.Tag)}\n{(updateData.Content)}";

            return false;
        }
    }
}
