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
        static void Postfix(NormalPlayerTask __instance, TaskTypes taskType, byte[] consoleIds)
        {
            if (taskType != TaskTypes.FixWiring || !CustomOptionHolder.randomWireTask.getBool()) return;
            List<Console>list = ShipStatus.Instance.AllConsoles.Where((global::Console t) => t.TaskTypes.Contains(taskType)).ToList<global::Console>();
            for (int i = 0; i < __instance.Data.Length; i++)
            {
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