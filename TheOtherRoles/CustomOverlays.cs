using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TheOtherRolesGM;

namespace TheOtherRoles {
    [Harmony]
    public class CustomOverlays {

        public static Sprite helpButton;
        private static Sprite colorBG;
        private static SpriteRenderer meetingUnderlay;
        private static SpriteRenderer infoUnderlay;
        private static TMPro.TextMeshPro infoOverlayRules;
        private static TMPro.TextMeshPro infoOverlayRoles;
        public static bool overlayShown = false;
        private static SpriteRenderer roleUnderlay;
        private static TMPro.TextMeshPro[] roleOverlayList;
        public static int rolePage = 0;
        public static int maxRolePage = 0;
        private static List<string> roleDatas;

        public static void resetOverlays()
        {
            hideBlackBG();
            hideInfoOverlay();
            hideRoleOverlay();
            UnityEngine.Object.Destroy(meetingUnderlay);
            UnityEngine.Object.Destroy(infoUnderlay);
            UnityEngine.Object.Destroy(infoOverlayRules);
            UnityEngine.Object.Destroy(infoOverlayRoles);
            UnityEngine.Object.Destroy(roleUnderlay);
            if (roleOverlayList != null)
            {
                foreach( var roleOverlay in roleOverlayList )
                {
                    UnityEngine.Object.Destroy(roleOverlay);
                }
            }
            meetingUnderlay = infoUnderlay = null;
            infoOverlayRules = infoOverlayRoles = null;
            overlayShown = false;
            roleUnderlay = null;
            roleOverlayList = null;
            rolePage = 0;
            maxRolePage = 0;
            roleDatas = null;
        }

        public static bool initializeOverlays()
        {
            HudManager hudManager = DestroyableSingleton<HudManager>.Instance;
            if (hudManager == null) return false;

            if (helpButton == null)
            {
                helpButton = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HelpButton.png", 115f);
            }

            if (colorBG == null)
            {
                colorBG = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.White.png", 100f);
            }

            if (meetingUnderlay == null)
            {
                meetingUnderlay = UnityEngine.Object.Instantiate(hudManager.FullScreen, hudManager.transform);
                meetingUnderlay.transform.localPosition = new Vector3(0f, 0f, 20f);
                meetingUnderlay.gameObject.SetActive(true);
                meetingUnderlay.enabled = false;
            }

            if (infoUnderlay == null)
            {
                infoUnderlay = UnityEngine.Object.Instantiate(meetingUnderlay, hudManager.transform);
                infoUnderlay.transform.localPosition = new Vector3(0f, 0f, -900f);
                infoUnderlay.gameObject.SetActive(true);
                infoUnderlay.enabled = false;
            }

            if (infoOverlayRules == null)
            {
                infoOverlayRules = UnityEngine.Object.Instantiate(hudManager.TaskText, hudManager.transform);
                infoOverlayRules.fontSize = infoOverlayRules.fontSizeMin = infoOverlayRules.fontSizeMax = 1.15f;
                infoOverlayRules.autoSizeTextContainer = false;
                infoOverlayRules.enableWordWrapping = false;
                infoOverlayRules.alignment = TMPro.TextAlignmentOptions.TopLeft;
                infoOverlayRules.transform.position = Vector3.zero;
                infoOverlayRules.transform.localPosition = new Vector3(-2.5f, 1.15f, -910f);
                infoOverlayRules.transform.localScale = Vector3.one;
                infoOverlayRules.color = Palette.White;
                infoOverlayRules.enabled = false;
            }

            if (infoOverlayRoles == null) { 
                infoOverlayRoles = UnityEngine.Object.Instantiate(infoOverlayRules, hudManager.transform);
                infoOverlayRoles.maxVisibleLines = 28;
                infoOverlayRoles.fontSize = infoOverlayRoles.fontSizeMin = infoOverlayRoles.fontSizeMax = 1.15f;
                infoOverlayRoles.outlineWidth += 0.02f;
                infoOverlayRoles.autoSizeTextContainer = false;
                infoOverlayRoles.enableWordWrapping = false;
                infoOverlayRoles.alignment = TMPro.TextAlignmentOptions.TopLeft;
                infoOverlayRoles.transform.position = Vector3.zero;
                infoOverlayRoles.transform.localPosition = infoOverlayRules.transform.localPosition + new Vector3(2.5f, 0.0f, 0.0f);
                infoOverlayRoles.transform.localScale = Vector3.one;
                infoOverlayRoles.color = Palette.White;
                infoOverlayRoles.enabled = false;
            }

            if (roleUnderlay == null)
            {
                roleUnderlay = UnityEngine.Object.Instantiate(meetingUnderlay, hudManager.transform);
                roleUnderlay.transform.localPosition = new Vector3(0f, 0f, -900f);
                roleUnderlay.gameObject.SetActive(true);
                roleUnderlay.enabled = false;
            }

            if (roleOverlayList == null)
            {
                roleOverlayList = new TMPro.TextMeshPro[3];
            }

            for( var i = 0; i < roleOverlayList.Length; i++ )
            {
                if (roleOverlayList[i] == null)
                {
                    if (i == 0)
                    {
                        roleOverlayList[i] = UnityEngine.Object.Instantiate(hudManager.TaskText, hudManager.transform);

                        initializeRoleOverlay(roleOverlayList[i]);

                        roleOverlayList[i].transform.localPosition = new Vector3(-3.5f, 1.15f, -910f);
                    }
                    else
                    {
                        roleOverlayList[i] = UnityEngine.Object.Instantiate(roleOverlayList[i-1], hudManager.transform);

                        initializeRoleOverlay(roleOverlayList[i]);

                        roleOverlayList[i].transform.localPosition = roleOverlayList[i-1].transform.localPosition + new Vector3(3.1f, 0.0f, 0.0f);
                    }
                }
            }

            if ( roleDatas == null )
            {
                roleDatas = new List<string>();

                StringBuilder entry = new StringBuilder();
                List<string> entries = new List<string>();

                // First add the presets and the role counts
                entries.Add(GameOptionsDataPatch.optionToString(CustomOptionHolder.presetSelection));

                var optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), ModTranslation.getString("crewmateRoles"));
                var min = CustomOptionHolder.crewmateRolesCountMin.getSelection();
                var max = CustomOptionHolder.crewmateRolesCountMax.getSelection();
                if (min > max) min = max;
                var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                entry.AppendLine($"{optionName}: {optionValue}");

                optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), ModTranslation.getString("neutralRoles"));
                min = CustomOptionHolder.neutralRolesCountMin.getSelection();
                max = CustomOptionHolder.neutralRolesCountMax.getSelection();
                if (min > max) min = max;
                optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                entry.AppendLine($"{optionName}: {optionValue}");

                optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), ModTranslation.getString("impostorRoles"));
                min = CustomOptionHolder.impostorRolesCountMin.getSelection();
                max = CustomOptionHolder.impostorRolesCountMax.getSelection();
                if (min > max) min = max;
                optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                entry.AppendLine($"{optionName}: {optionValue}");

                entries.Add(entry.ToString().Trim('\r', '\n'));

                foreach (CustomOption option in CustomOption.options)
                {
                    if ((option == CustomOptionHolder.presetSelection) ||
                        (option == CustomOptionHolder.crewmateRolesCountMin) ||
                        (option == CustomOptionHolder.crewmateRolesCountMax) ||
                        (option == CustomOptionHolder.neutralRolesCountMin) ||
                        (option == CustomOptionHolder.neutralRolesCountMax) ||
                        (option == CustomOptionHolder.impostorRolesCountMin) ||
                        (option == CustomOptionHolder.impostorRolesCountMax))
                    {
                        continue;
                    }

                    if (option.parent == null)
                    {
                        if (!option.enabled)
                        {
                            continue;
                        }

                        entry = new StringBuilder();
                        if (!option.isHidden)
                            entry.AppendLine(GameOptionsDataPatch.optionToString(option));

                        addChildren(option, ref entry, !option.isHidden);
                        entries.Add(entry.ToString().Trim('\r', '\n'));
                    }
                }

                int maxLines = 28;
                int lineCount = 0;
                string page = "";
                foreach (var e in entries)
                {
                    int lines = e.Count(c => c == '\n') + 1;

                    if (lineCount + lines > maxLines)
                    {
                        roleDatas.Add(page);
                        page = "";
                        lineCount = 0;
                    }

                    page = page + e + "\n\n";
                    lineCount += lines + 1;
                }

                page = page.Trim('\r', '\n');
                if (page != "")
                {
                    roleDatas.Add(page);
                }

                maxRolePage = ((roleDatas.Count - 1) / 3) + 1;
            }

            return true;
        }

        private static void initializeRoleOverlay(TMPro.TextMeshPro roleOverlay)
        {
            roleOverlay.maxVisibleLines = 29;
            roleOverlay.fontSize = roleOverlay.fontSizeMin = roleOverlay.fontSizeMax = 1.15f;
            roleOverlay.autoSizeTextContainer = false;
            roleOverlay.enableWordWrapping = false;
            roleOverlay.alignment = TMPro.TextAlignmentOptions.TopLeft;
            roleOverlay.transform.position = Vector3.zero;
            roleOverlay.transform.localScale = Vector3.one;
            roleOverlay.color = Palette.White;
            roleOverlay.enabled = false;
        }

        public static void addChildren(CustomOption option, ref StringBuilder entry, bool indent = true)
        {
            if (!option.enabled) return;

            foreach (var child in option.children)
            {
                if (!child.isHidden)
                    entry.AppendLine((indent ? "    " : "") + GameOptionsDataPatch.optionToString(child));
                addChildren(child, ref entry, indent);
            }
        }

        public static void showBlackBG()
        {
            if (HudManager.Instance == null) return;
            if (!initializeOverlays()) return;

            meetingUnderlay.sprite = colorBG;
            meetingUnderlay.enabled = true;
            meetingUnderlay.transform.localScale = new Vector3(20f, 20f, 1f);
            var clearBlack = new Color32(0, 0, 0, 0);

            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                meetingUnderlay.color = Color.Lerp(clearBlack, Palette.Black, t);
            })));
        }

        public static void hideBlackBG()
        {
            if (meetingUnderlay == null) return;
            meetingUnderlay.enabled = false;
        }

        public static void showInfoOverlay()
        {
            if (overlayShown || MapOptions.hideSettings) return;

            HudManager hudManager = DestroyableSingleton<HudManager>.Instance;
            if (ShipStatus.Instance == null || PlayerControl.LocalPlayer == null || hudManager == null || HudManager.Instance.IsIntroDisplayed || (!PlayerControl.LocalPlayer.CanMove && MeetingHud.Instance == null))
                return;

            if (!initializeOverlays()) return;

            hideRoleOverlay();

            if (MapBehaviour.Instance != null)
                MapBehaviour.Instance.Close();

            hudManager.SetHudActive(false);
            
            overlayShown = true;

            Transform parent;
            if (MeetingHud.Instance != null)
                parent = MeetingHud.Instance.transform;
            else
                parent = hudManager.transform;

            infoUnderlay.transform.parent = parent;
            infoOverlayRules.transform.parent = parent;
            infoOverlayRoles.transform.parent = parent;

            infoUnderlay.sprite = colorBG;
            infoUnderlay.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            infoUnderlay.transform.localScale = new Vector3(7.5f, 5f, 1f);
            infoUnderlay.enabled = true;

            TheOtherRolesPlugin.optionsPage = 0;
            GameOptionsData o = PlayerControl.GameOptions;
            List<string> gameOptions = o.ToString().Split("\n", StringSplitOptions.RemoveEmptyEntries).ToList().GetRange(2, 17);
            infoOverlayRules.text = string.Join("\n", gameOptions) + "\n\n" + GameOptionsDataPatch.optionsToString(CustomOptionHolder.specialOptions);
            infoOverlayRules.enabled = true;

            string rolesText = "";
            foreach (RoleInfo r in RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer))
            {
                string roleOptions = r.roleOptions;
                string roleDesc = r.fullDescription;
                rolesText += $"<size=150%>{r.nameColored}</size>" +
                    (roleDesc != "" ? $"\n{r.fullDescription}" : "") + "\n\n" +
                    (roleOptions != "" ? $"{roleOptions}\n\n" : "");
            }

            infoOverlayRoles.text = rolesText;
            infoOverlayRoles.enabled = true;

            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                infoUnderlay.color = Color.Lerp(underlayTransparent, underlayOpaque, t);
                infoOverlayRules.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
                infoOverlayRoles.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
            })));
        }

        public static void hideInfoOverlay()
        {
            if (!overlayShown) return;

            if (MeetingHud.Instance == null) DestroyableSingleton<HudManager>.Instance.SetHudActive(true);

            overlayShown = false;
            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);

            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                if (infoUnderlay != null)
                {
                    infoUnderlay.color = Color.Lerp(underlayOpaque, underlayTransparent, t);
                    if (t >= 1.0f) infoUnderlay.enabled = false;
                }

                if (infoOverlayRules != null)
                {
                    infoOverlayRules.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                    if (t >= 1.0f) infoOverlayRules.enabled = false;
                }

                if (infoOverlayRoles != null)
                {
                    infoOverlayRoles.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                    if (t >= 1.0f) infoOverlayRoles.enabled = false;
                }
            })));
        }

        public static void toggleInfoOverlay()
        {
            if (overlayShown)
                hideInfoOverlay();
            else
                showInfoOverlay();
        }

        public static void showRoleOverlay()
        {
            if ((rolePage != 0) || MapOptions.hideSettings) return;

            HudManager hudManager = DestroyableSingleton<HudManager>.Instance;
            if (ShipStatus.Instance == null || PlayerControl.LocalPlayer == null || hudManager == null || HudManager.Instance.IsIntroDisplayed || (!PlayerControl.LocalPlayer.CanMove && MeetingHud.Instance == null))
                return;

            if (!initializeOverlays()) return;

            hideInfoOverlay();

            if (MapBehaviour.Instance != null)
                MapBehaviour.Instance.Close();

            hudManager.SetHudActive(false);
            
            rolePage = 1;

            Transform parent;
            if (MeetingHud.Instance != null)
                parent = MeetingHud.Instance.transform;
            else
                parent = hudManager.transform;

            roleUnderlay.transform.parent = parent;
            foreach( var roleOverlay in roleOverlayList )
            {
                roleOverlay.transform.parent = parent;
            }

            roleUnderlay.sprite = colorBG;
            roleUnderlay.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            roleUnderlay.transform.localScale = new Vector3(9.3f, 5f, 1f);
            roleUnderlay.enabled = true;

            setRoleOverlayText();

            foreach( var roleOverlay in roleOverlayList )
            {
                roleOverlay.enabled = true;
            }

            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                roleUnderlay.color = Color.Lerp(underlayTransparent, underlayOpaque, t);
                foreach( var roleOverlay in roleOverlayList )
                {
                    roleOverlay.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
                }
            })));
        }

        public static void setRoleOverlayText()
        {
            var i = (rolePage - 1) * 3;
            var pageText = $" ({rolePage}/{maxRolePage})" + "\n";
            foreach( var roleOverlay in roleOverlayList )
            {
                if ( i < roleDatas.Count )
                {
                    roleOverlay.text = pageText + roleDatas[i].Trim('\r', '\n');
                }
                else
                {
                    roleOverlay.text = string.Empty;
                }
                i++;
                pageText = "\n";
            }
        }

        public static void hideRoleOverlay()
        {
            if (rolePage == 0) return;

            if (MeetingHud.Instance == null) DestroyableSingleton<HudManager>.Instance.SetHudActive(true);

            rolePage = 0;
            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);

            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                if (roleUnderlay != null)
                {
                    roleUnderlay.color = Color.Lerp(underlayOpaque, underlayTransparent, t);
                    if (t >= 1.0f) roleUnderlay.enabled = false;
                }

                if (roleOverlayList != null)
                {
                    foreach(var roleOverlay in roleOverlayList)
                    {
                        if (roleOverlay != null)
                        {
                            roleOverlay.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                            if (t >= 1.0f) roleOverlay.enabled = false;
                        }
                    }
                }
            })));
        }

        public static void toggleRoleOverlay()
        {
            if (rolePage == 0)
                showRoleOverlay();
            else if (maxRolePage <= rolePage)
                hideRoleOverlay();
            else
            {
                rolePage++;
                setRoleOverlayText();
            }
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class CustomOverlayKeybinds
        {
            public static void Postfix(KeyboardJoystick __instance)
            {
                if (Input.GetKeyDown(KeyCode.H) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                {
                    toggleInfoOverlay();
                }
                else if (Input.GetKeyDown(KeyCode.I) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                {
                    toggleRoleOverlay();
                }
            }
        }
    }
}