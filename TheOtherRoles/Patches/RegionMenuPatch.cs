// Adapted from https://github.com/MoltenMods/Unify
/*
MIT License

Copyright (c) 2021 Daemon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace TheOtherRoles.Patches {
    [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
    public static class RegionMenuOpenPatch
    {
        private static GameObject ipField;
        private static GameObject portField;

        public static void Postfix(RegionMenu __instance)
        {
            var template = GameObject.Find("NormalMenu/JoinGameButton/JoinGameMenu/GameIdText");
            if (template == null ) return;

            if (ipField == null || ipField.gameObject == null)
            {
                ipField = UnityEngine.Object.Instantiate(template.gameObject, __instance.transform);
                ipField.gameObject.name = "IpTextBox";
                var arrow = ipField.transform.FindChild("arrowEnter");
                if (arrow == null || arrow.gameObject == null) return;
                UnityEngine.Object.DestroyImmediate(arrow.gameObject);

                ipField.transform.localPosition = new Vector3(0, -1f, -100f);

                var ipTextBox = ipField.GetComponent<TextBoxTMP>();
                ipTextBox.characterLimit = 30;
                ipTextBox.AllowSymbols = true;
                ipTextBox.ForceUppercase = false;
                ipTextBox.SetText(TheOtherRolesPlugin.Ip.Value);
                __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) => {
                    ipTextBox.outputText.SetText(TheOtherRolesPlugin.Ip.Value);
                    ipTextBox.SetText(TheOtherRolesPlugin.Ip.Value);
                })));

                ipTextBox.ClearOnFocus = false; 
                ipTextBox.OnEnter = ipTextBox.OnChange = new Button.ButtonClickedEvent();
                ipTextBox.OnFocusLost = new Button.ButtonClickedEvent();
                ipTextBox.OnChange.AddListener((UnityAction)onEnterOrIpChange);
                ipTextBox.OnFocusLost.AddListener((UnityAction)onFocusLost);

            }

            if (portField == null || portField.gameObject == null)
            {
                portField = UnityEngine.Object.Instantiate(template.gameObject, __instance.transform);
                portField.gameObject.name = "PortTextBox";
                var arrow = portField.transform.FindChild("arrowEnter");
                if (arrow == null || arrow.gameObject == null) return;
                UnityEngine.Object.DestroyImmediate(arrow.gameObject);

                portField.transform.localPosition = new Vector3(0, -1.75f, -100f);

                var portTextBox = portField.GetComponent<TextBoxTMP>();
                portTextBox.characterLimit = 5;
                portTextBox.SetText(TheOtherRolesPlugin.Port.Value.ToString());
                __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) => {
                    portTextBox.outputText.SetText(TheOtherRolesPlugin.Port.Value.ToString());
                    portTextBox.SetText(TheOtherRolesPlugin.Port.Value.ToString()); 
                })));


                portTextBox.ClearOnFocus = false;
                portTextBox.OnEnter = portTextBox.OnChange = new Button.ButtonClickedEvent();
                portTextBox.OnFocusLost = new Button.ButtonClickedEvent();
                portTextBox.OnChange.AddListener((UnityAction)onEnterOrPortFieldChange);
                portTextBox.OnFocusLost.AddListener((UnityAction)onFocusLost);
            }

            void onEnterOrPortFieldChange()
            {
                var portTextBox = portField.GetComponent<TextBoxTMP>();
                ushort port = 0;
                if (ushort.TryParse(portTextBox.text, out port)) {
                    TheOtherRolesPlugin.Port.Value = port;
                    portTextBox.outputText.color = Color.white;
                } else {
                    portTextBox.outputText.color = Color.red;
                }
            }

            void onEnterOrIpChange()
            {
                TheOtherRolesPlugin.Ip.Value = ipField.GetComponent<TextBoxTMP>().text;
            }

            void onFocusLost()
            {
                TheOtherRolesPlugin.UpdateRegions();
                __instance.ChooseOption(ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            }
        }
    }
}