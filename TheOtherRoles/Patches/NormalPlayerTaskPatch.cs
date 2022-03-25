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