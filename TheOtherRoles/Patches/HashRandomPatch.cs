// 元ネタ
// https://github.com/yukieiji/ExtremeRoles
using System;
using HarmonyLib;

namespace TheOtherRoles.Patches
{
    [HarmonyPatch(typeof(HashRandom), nameof(HashRandom.FastNext))]
    public static class HashRandomFastNextPatch
    {
        public static bool Prefix(
            ref int __result,
            [HarmonyArgument(0)] int maxInt)
        {
            if (RandomGenerator.useStrongGen)
            {
                __result = RandomGenerator.Instance.Next(maxInt);
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(
        typeof(HashRandom),
        nameof(HashRandom.Next),
        new Type[] { typeof(int) })]
    public static class HashRandomNextPatch
    {
        public static bool Prefix(
            ref int __result,
            [HarmonyArgument(0)] int maxInt)
        {
            if (RandomGenerator.useStrongGen)
            {
                __result = RandomGenerator.Instance.Next(maxInt);
                return false;
            }
            return true;
        }
    }
}
