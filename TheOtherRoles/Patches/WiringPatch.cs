using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;
using System.Reflection;

namespace TheOtherRoles.Patches
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
    class AddWireingTasksPatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            AddWireTasks(PlayerControl.GameOptions.MapId);
        }
        public static void AddWireTasks(int mapId)
        {
            if (!CustomOptionHolder.additionalWireTask.getBool()) return;
            // Airshipの場合
            if (mapId == 4)
            {
                ActivateWiring("task_wiresHallway2", 2);
                ActivateWiring("task_electricalside2", 3).Room = SystemTypes.Armory;
                ActivateWiring("task_wireShower", 4);
                ActivateWiring("taks_wiresLounge", 5);
                ActivateWiring("panel_wireHallwayL", 6);
                ActivateWiring("task_wiresStorage", 7);
                ActivateWiring("task_electricalSide", 8).Room = SystemTypes.VaultRoom;
                ActivateWiring("task_wiresMeeting", 9);
            }
        }
        protected static Console ActivateWiring(string consoleName, int consoleId)
        {
            Console console = ActivateConsole(consoleName);

            if (!console.TaskTypes.Contains(TaskTypes.FixWiring))
            {
                var list=console.TaskTypes.ToList();
                list.Add(TaskTypes.FixWiring);
                console.TaskTypes = list.ToArray();
            }
            console.ConsoleId = consoleId;
            return console;
        }
        protected static Console ActivateConsole(string objectName)
        {
            GameObject obj = UnityEngine.GameObject.Find(objectName);
            obj.layer = LayerMask.NameToLayer("ShortObjects");
            Console console = obj.GetComponent<Console>();
            PassiveButton button = obj.GetComponent<PassiveButton>();
            CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
            if (!console)
            {
                console = obj.AddComponent<Console>();
                console.checkWalls = true;
                console.usableDistance = 0.7f;
                console.TaskTypes = new TaskTypes[0];
                console.ValidTasks = new UnhollowerBaseLib.Il2CppReferenceArray<TaskSet>(0);
                var list = ShipStatus.Instance.AllConsoles.ToList();
                list.Add(console);
                ShipStatus.Instance.AllConsoles = new UnhollowerBaseLib.Il2CppReferenceArray<Console>(list.ToArray());
            }
            if (console.Image == null)
            {
                console.Image = obj.GetComponent<SpriteRenderer>();
                console.Image.material = new Material(ShipStatus.Instance.AllConsoles[0].Image.material);
            }
            // if (!button)
            // {
            //     button = obj.AddComponent<PassiveButton>();
            //     button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
            //         console.Use();
            //     }));
            // }
            if (!collider)
            {
                collider = obj.AddComponent<CircleCollider2D>();
                collider.radius = 0.4f;
                collider.isTrigger = true;
            }
            // if(!PlayerControl.LocalPlayer.cache.ContainsKey(collider))
            //     PlayerControl.LocalPlayer.cache.Add(collider, new UnhollowerBaseLib.Il2CppReferenceArray<IUsable>(new IUsable[1] { console.gameObject.GetComponent<IUsable>() }));
            return console;
        }
    }

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.PickRandomConsoles))]
    class NormalPlayerTaskPickRandomConsolesPatch
    {
        private static int numWireTask {get {return (int)CustomOptionHolder.numWireTask.getFloat();}}
        static void Postfix(NormalPlayerTask __instance, TaskTypes taskType, byte[] consoleIds)
        {
            if (taskType != TaskTypes.FixWiring || !CustomOptionHolder.randomWireTask.getBool()) return;
            List<Console> orgList = ShipStatus.Instance.AllConsoles.Where((global::Console t) => t.TaskTypes.Contains(taskType)).ToList<global::Console>();
            List<Console> list = new List<Console>(orgList);

            __instance.MaxStep = numWireTask;
            __instance.Data = new byte[numWireTask];
            for (int i = 0; i < __instance.Data.Length; i++)
            {
                if(list.Count == 0)
                    list = new List<Console>(orgList);
                int index = list.RandomIdx<global::Console>();
                __instance.Data[i] = (byte)list[index].ConsoleId;
                list.RemoveAt(index);
            }
        }
    }
    public static class Extensions
    {
        public static int RandomIdx<T>(this IEnumerable<T> self)
        {
            return UnityEngine.Random.Range(0, self.Count<T>());
        }
    }

}