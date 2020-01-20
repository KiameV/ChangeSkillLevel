using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ForceDoJob
{
    public class SettingsController : Mod
    {
        private Vector2 scrollPosition = new Vector2(0, 0);
        private List<CurveValueBuffer> buffers = null;
        private List<string> skillLossCapBuffers = null;
        private bool showGraph = false;
        private float previousY = 0;

        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "Change Skill Level";
        }

        public void ResetCaps()
        {
            skillLossCapBuffers = new List<string>(Settings.SkillLossCaps.Count);
            for (int i = 0; i < Settings.SkillLossCaps.Count; ++i)
                skillLossCapBuffers.Add(Settings.SkillLossCaps[i].ToString());
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            if (Settings.CustomCurve == null)
                Settings.CustomCurve = CreateDefaultCurve();

            if (skillLossCapBuffers == null)
            {
                ResetCaps();
            }

            if (buffers == null|| buffers.Count == 0)
                buffers = this.CreateBuffers();

            Widgets.BeginScrollView(new Rect(20, rect.yMin, 450, 500), ref this.scrollPosition, new Rect(0, 0, 434, this.previousY));

            this.previousY = 0;
            Widgets.CheckboxLabeled(new Rect(0, this.previousY, 250, 30), "Allow Skill Experience Loss", ref Settings.AllowSkillLoss);
            this.previousY += 32;

            if (Settings.AllowSkillLoss)
            {
                Widgets.CheckboxLabeled(new Rect(20, this.previousY, 300, 30), "Allow Skills to Lose Level", ref Settings.CanLoseLevel);
                this.previousY += 32;

                Widgets.CheckboxLabeled(new Rect(20, this.previousY, 300, 30), "Customize Experience Loss Per Tick", ref Settings.AdjustSkillLossCaps);
                if (Settings.AdjustSkillLossCaps)
                {
                    this.previousY += 32;
                    for (int i = 0; i < skillLossCapBuffers.Count; ++i)
                    {
                        skillLossCapBuffers[i] = Widgets.TextEntryLabeled(new Rect(40, this.previousY, 150, 30), $"Level {(i + 10).ToString()}: ", skillLossCapBuffers[i]);
                        this.previousY += 32;
                    }

                    if (Widgets.ButtonText(new Rect(40, this.previousY, 100, 30), "Apply"))
                    {
                        for (int i = 0; i < skillLossCapBuffers.Count; ++i)
                        {
                            if (float.TryParse(skillLossCapBuffers[i], out float v) && v >= -50f && v <= 0f)
                            {
                                Settings.SkillLossCaps[i] = v;
                                Messages.Message("Skill Loss Per Tick Set", MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Log.Error((i + 10).ToString() + "'s Skill Loss Cap must be between -50 and 0");
                                Messages.Message("Error while setting skill loss", MessageTypeDefOf.NegativeEvent);
                            }
                        }
                    }

                    if (Widgets.ButtonText(new Rect(160, this.previousY, 100, 30), "Reset"))
                    {
                        ResetCaps();
                    }
                    this.previousY += 10;
                }
                this.previousY += 32;
            }

            Widgets.CheckboxLabeled(new Rect(0, this.previousY, 250, 30), "Customize Experience Needed", ref Settings.HasCustomCurve);
            this.previousY += 32;
            if (Settings.HasCustomCurve)
            {
                Widgets.Label(new Rect(0, this.previousY, 250, 30), "Experience Curve");
                this.previousY += 32;

                Widgets.CheckboxLabeled(new Rect(425, this.previousY, 200, 30), "Show Graph", ref this.showGraph);
                if (this.showGraph)
                    SimpleCurveDrawer.DrawCurve(new Rect(425, this.previousY + 50, 250, 250), Settings.CustomCurve);

                for (int i = 0; i < this.buffers.Count; ++i)
                {
                    var b = this.buffers[i];
                    Widgets.Label(new Rect(0, this.previousY, 150, 30), "Start Level");
                    if (i == 0)
                    {
                        b.StartLevel = 0;
                        Widgets.Label(new Rect(120, this.previousY, 100, 30), "0");
                    }
                    else
                        Widgets.TextFieldNumeric(new Rect(120, this.previousY, 100, 30), ref b.StartLevel, ref b.SLBuffer, 0, 20);
                    if (i > 0 && Widgets.ButtonText(new Rect(255, this.previousY, 30, 30), "-", true, false))
                    {
                        this.buffers.RemoveAt(i);
                        break;
                    }
                    if (Widgets.ButtonText(new Rect(290, this.previousY, 30, 30), "+", true, false))
                    {
                        this.buffers.Insert(i + 1, new CurveValueBuffer(b.StartLevel + 1, 1f));
                        break;
                    }
                    this.previousY += 30;
                    Widgets.Label(new Rect(0, this.previousY, 120, 30), "Exp Needed");
                    Widgets.TextFieldNumeric(new Rect(120, this.previousY, 100, 30), ref b.ExpNeeded, ref b.ENBuffer);
                    this.previousY += 40;
                }
                this.previousY += 440;

                if (Widgets.ButtonText(new Rect(30, this.previousY, 125, 30), "Apply", true, false, this.buffers.Count > 0))
                {
                    HashSet<int> levels = new HashSet<int>();
                    foreach (var b in this.buffers)
                    {
                        if (b.ExpNeeded == 0)
                        {
                            Messages.Message("Each Exp Need must be greater than 0.", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        if (levels.Contains(b.StartLevel))
                        {
                            Messages.Message("Each Start Level much be unique.", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        levels.Add(b.StartLevel);
                    }
                    levels.Clear();
                    levels = null;

                    Settings.CustomCurve = new SimpleCurve();
                    foreach (var i in buffers)
                    {
                        Settings.CustomCurve.Add(new CurvePoint(i.StartLevel, i.ExpNeeded));
                    }
                    this.buffers.Clear();
                    this.buffers = this.CreateBuffers();
                }

                if (Widgets.ButtonText(new Rect(200, this.previousY, 125, 30), "Reset".Translate()))
                {
                    Settings.CustomCurve = this.CreateDefaultCurve();
                    this.buffers.Clear();
                    this.buffers = this.CreateBuffers();
                }
            }
            Widgets.EndScrollView();
        }

        private SimpleCurve CreateDefaultCurve()
        {
            return new SimpleCurve
            {
                {
                    new CurvePoint(0f, 1000f),
                    true
                },
                {
                    new CurvePoint(9f, 10000f),
                    true
                },
                {
                    new CurvePoint(19f, 30000f),
                    true
                }
            };
        }

        private List<CurveValueBuffer> CreateBuffers()
        {
            List<CurveValueBuffer> buffers = new List<CurveValueBuffer>(Settings.CustomCurve.PointsCount * 2);
            foreach (var p in Settings.CustomCurve)
                buffers.Add(new CurveValueBuffer((int)p.x, p.y));
            return buffers;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }

        private class CurveValueBuffer
        {
            public int StartLevel = 0;
            public float ExpNeeded = 0f;
            public string SLBuffer = "";
            public string ENBuffer = "";
            public CurveValueBuffer(int startLevel, float expNeeded)
            {
                this.StartLevel = startLevel;
                this.ExpNeeded = expNeeded;
                SLBuffer = ((int)startLevel).ToString();
                ENBuffer = expNeeded.ToString();
            }
        }
    }

    public class Settings : ModSettings
    {
        private List<SimpleCurveValues> values = null;

        public static bool AllowSkillLoss = true;
        public static bool AdjustSkillLossCaps = false;
        public static List<float> SkillLossCaps = new List<float>(11);
        public static bool CanLoseLevel = false;
        public static bool HasCustomCurve = false;
        public static SimpleCurve CustomCurve = null;

        public Settings()
        {
            SkillLossCaps.Add(-0.1f);
            SkillLossCaps.Add(-0.2f);
            SkillLossCaps.Add(-0.4f);
            SkillLossCaps.Add(-0.6f);
            SkillLossCaps.Add(-1f);
            SkillLossCaps.Add(-1.8f);
            SkillLossCaps.Add(-2.8f);
            SkillLossCaps.Add(-4f);
            SkillLossCaps.Add(-6f);
            SkillLossCaps.Add(-8f);
            SkillLossCaps.Add(-12f);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (CustomCurve != null)
                {
                    values = new List<SimpleCurveValues>(CustomCurve.PointsCount);
                    foreach (CurvePoint p in CustomCurve)
                    {
                        values.Add(
                            new SimpleCurveValues()
                            {
                                StartLevel = p.x,
                                ExpNeeded = p.y
                            });
                    }
                }
            }

            Scribe_Values.Look<bool>(ref AllowSkillLoss, "ChangeSkillLevel.AllowSkillLoss", true, true);
            Scribe_Values.Look<bool>(ref AdjustSkillLossCaps, "ChangeSkillLevel.AdjustSkillLossCaps", false, true);
            Scribe_Collections.Look(ref SkillLossCaps, "ChangeSkillLevel.SkillLossCaps", LookMode.Value, new object[0]);
            Scribe_Values.Look<bool>(ref CanLoseLevel, "ChangeSkillLevel.CanLoseLevel", false, true);
            Scribe_Values.Look<bool>(ref HasCustomCurve, "ChangeSkillLevel.HasCustomCurve", false, true);
            Scribe_Collections.Look(ref this.values, "ChangeSkillLevel.CurvePoints", LookMode.Deep, new object[0]);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (HasCustomCurve)
                {
                    if (this.values == null || this.values.Count == 0)
                    {
                        HasCustomCurve = false;
                    }
                    else
                    {
                        CustomCurve = new SimpleCurve();
                        foreach (SimpleCurveValues v in this.values)
                        {
                            CustomCurve.Add(new CurvePoint(v.StartLevel, v.ExpNeeded));
                        }
                    }
                }
            }
            if ((Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving) &&
                this.values != null)
            {
                this.values.Clear();
                this.values = null;
            }
        }

        private class SimpleCurveValues : IExposable
        {
            public float StartLevel = 0f;
            public float ExpNeeded = 0f;

            public void ExposeData()
            {
                Scribe_Values.Look(ref this.StartLevel, "startLevel");
                Scribe_Values.Look(ref this.ExpNeeded, "expNeeded");
            }
        }
    }
}
