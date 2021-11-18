using HarmonyLib;
using System;
using static TheOtherRoles.TheOtherRoles;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TheOtherRoles.Patches {
    class MadmateTaskHelper
    {
        public static void SetMadmateTasks()
        {
            PlayerControl me = PlayerControl.LocalPlayer;
            if (me == null)
                return;
            GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(me.PlayerId);
            if (playerById == null)
                return;
            me.clearAllTasks();
            List<byte> list = new List<byte>(10);
            SetTasksToList(
                ref list,
                ShipStatus.Instance.CommonTasks.ToList<NormalPlayerTask>(),
                Mathf.RoundToInt(CustomOptionHolder.madmateCommonTasks.getFloat()));
            SetTasksToList(
                ref list,
                ShipStatus.Instance.LongTasks.ToList<NormalPlayerTask>(),
                Mathf.RoundToInt(CustomOptionHolder.madmateLongTasks.getFloat()));
            SetTasksToList(
                ref list,
                ShipStatus.Instance.NormalTasks.ToList<NormalPlayerTask>(),
                Mathf.RoundToInt(CustomOptionHolder.madmateShortTasks.getFloat()));

            byte[] taskTypeIds = list.ToArray();
            playerById.Tasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>(taskTypeIds.Length);
            for (int i = 0; i < taskTypeIds.Length; i++) {
                playerById.Tasks.Add(new GameData.TaskInfo(taskTypeIds[i], (uint)i));
                playerById.Tasks[i].Id = (uint)i;
            }
            for (int i = 0; i < playerById.Tasks.Count; i++) {
                GameData.TaskInfo taskInfo = playerById.Tasks[i];
                NormalPlayerTask normalPlayerTask = UnityEngine.Object.Instantiate<NormalPlayerTask>(ShipStatus.Instance.GetTaskById(taskInfo.TypeId), me.transform);
                normalPlayerTask.Id = taskInfo.Id;
                normalPlayerTask.Owner = me;
                normalPlayerTask.Initialize();
                me.myTasks.Add(normalPlayerTask);
            }
        }

        private static void SetTasksToList(
            ref List<byte> list,
            List<NormalPlayerTask> playerTasks,
            int numConfiguredTasks)
        {
            playerTasks.Shuffle(0);
            int numTasks = Math.Min(playerTasks.Count, numConfiguredTasks);
            for (int i = 0; i < numTasks; i++) {
                list.Add((byte)playerTasks[i].Index);
            }
        }
    }

    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> self, int startAt = 0)
        {
            for (int i = startAt; i < self.Count - 1; i++) {
                T value = self[i];
                int index = UnityEngine.Random.Range(i, self.Count);
                self[i] = self[index];
                self[index] = value;
            }
        }

        public static void Shuffle<T>(this System.Random r, IList<T> self)
        {
            for (int i = 0; i < self.Count; i++) {
                T value = self[i];
                int index = r.Next(self.Count);
                self[i] = self[index];
                self[index] = value;
            }
        }
    }
}

