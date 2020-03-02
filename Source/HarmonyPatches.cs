using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace ChangeSkillLevel
{
    [StaticConstructorOnStartup]
    class Main
    {
        public static Pawn ChoicesForPawn = null;

        static Main()
        {
            var harmony = new Harmony("com.changeskilllevel.rimworld.mod");
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

            if (!Settings.AllowSkillLoss ||
                (!Settings.CanLoseLevel && __instance.xpSinceLastLevel == 0))
            {
                return false;
            }

            __state = __instance.levelInt;

            if (Settings.AdjustSkillLossCaps &&
                __instance.GetType().GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance) is Pawn pawn)
            {
                float exp = (!pawn.story.traits.HasTrait(TraitDefOf.GreatMemory)) ? 1f : 0.5f;
                int i = __instance.levelInt - 10;
                if (i >= 0)
                {
                    var multiplier = Settings.SkillLossCaps[i];
                    if (multiplier < 0)
                    {
                        __instance.Learn(exp * multiplier);

                        if (__state > 0 && __instance.levelInt != __state)
                        {
                            if (Settings.CanLoseLevel)
                            {
                                __instance.levelInt = __state - 1;
                                __instance.xpSinceLastLevel = SettingsController.CreateDefaultCurve().Evaluate(__instance.levelInt);
                            }
                            else
                            {
                                __instance.levelInt = __state;
                                __instance.xpSinceLastLevel = 0;
                            }
                        }
                    }
                }
                return false;
            }

            return true;
        }

        static void Postfix(SkillRecord __instance, ref int __state)
        {
            if (__state > 0 && !Settings.CanLoseLevel && __instance.levelInt != __state)
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