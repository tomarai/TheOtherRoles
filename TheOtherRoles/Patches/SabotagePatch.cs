using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.GameHistory;
using System.Reflection;

namespace TheOtherRoles.Patches
{
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.RepairDamage))]
    class HeliSabotageSystemRepairDamagePatch
    {
        static void Postfix(HeliSabotageSystem __instance, PlayerControl player, byte amount)
        {
            HeliSabotageSystem.Tags tags = (HeliSabotageSystem.Tags)(amount & 240);
         	if (tags != HeliSabotageSystem.Tags.ActiveBit)
            {
			if (tags == HeliSabotageSystem.Tags.DamageBit)
                {
                    __instance.Countdown = CustomOptionHolder.airshipReactorDuration.getFloat();
                }
            }
        }
    }

}