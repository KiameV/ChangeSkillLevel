using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ForceDoJob
{
    [StaticConstructorOnStartup]
    class Main
    {
        public static Pawn ChoicesForPawn = null;

        static Main()
        {
            var harmony = HarmonyInstance.Create("com.forcedojob.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message(
                "Change Skill Levels Harmony Patches:" + Environment.NewLine +
                "  Prefix:" + Environment.NewLine +
                "    SkillRecord.Interval");
        }
    }

    [HarmonyPatch(typeof(SkillRecord), "Interval")]
    static class Patch_SkillRecord_Learn
    {
        static bool Prefix(SkillRecord __instance, ref int __state)
        {
            __state = -1;
            if (Settings.CanLoseLevel)
                return true;

            if (__instance.xpSinceLastLevel == 0)
                return false;

            __state = __instance.levelInt;
            return true;
        }

        static void Postfix(SkillRecord __instance, ref int __state)
        {
            if (__state > 0 && __instance.levelInt != __state)
            {
                __instance.levelInt = __state;
                __instance.xpSinceLastLevel = 0;
            }
        }
    }

    [HarmonyPatch(typeof(SkillRecord), "XpRequiredToLevelUpFrom")]
    static class Patch_SkillRecord_XpRequiredToLevelUpFrom
    {
        static bool Prefix(ref float __result, int startingLevel)
        {
            if (Settings.HasCustomCurve)
            {
                __result = Settings.CustomCurve.Evaluate(startingLevel);
                return false;
            }
            return true;
        }
    }
}