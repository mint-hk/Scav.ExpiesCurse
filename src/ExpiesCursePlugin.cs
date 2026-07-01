using BepInEx;
using HarmonyLib;
using ScavLib.util;
using ScavLib.command;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Scav.WorldSettingsHelper;

namespace Scav.ExpiesCurse
{
    [BepInDependency("com.kanisuko.scavlib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.mint-hk.scav.worldsettingshelper", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ExpiesCursePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mint-hk.scav.expiescurse";
        public const string PluginName = "Scav.ExpiesCurse";
        public const string PluginVersion = "0.1.0";
        public const string SettingEnabled = "expies_curse_enabled";
        public const string SettingSeverity = "expies_curse_severity";
        public const string SettingInterval = "expies_curse_interval";
        public static ExpiesCursePlugin Instance { get; private set; }
        public static bool ShowImpendingDoomMoodle { get; private set; }

        private static readonly string[] DelayedInjuryLines =
        {
            "Oh no... not now. I can feel it coming.",
            "Wait... wait, no. Something's wrong.",
            "Fuck... it's happening again.",
            "No, no, no... I know this feeling.",
            "Something's coming. I can feel it under my skin.",
            "Oh fuck... not like this.",
            "Hold on... why does everything feel wrong?",
            "No... please, give me a second.",
            "It's coming. I don't know what, but it's coming.",
            "Wait... is it already time?",
            "My body knows before I do. Something's coming.",
            "Oh no... I can feel it starting.",
            "Fuck, fuck... I need a second.",
            "No... this isn't normal.",
            "Something bad is about to happen. I can feel it.",
            "Wait... don't do this to me now.",
            "Oh fuck... my body feels wrong.",
            "No, please... not right now.",
            "It's starting. I can feel it.",
            "Wait... wait... I don't feel right."
        };

        private Harmony _harmony;
        private float _curseTimerSeconds;
        private float _lastCurseRunTime = float.NaN;
        private bool _curseInjuryQueued;
        private bool _loggedMissingSettings;

        public static float CurseTimerSeconds => Instance != null ? Instance._curseTimerSeconds : 0f;
        public static float LastCurseRunTime => Instance != null ? Instance._lastCurseRunTime : float.NaN;
        public static bool CurseInjuryQueued => Instance != null && Instance._curseInjuryQueued;

        private void Awake()
        {
            Instance = this;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
            RegisterWorldSettings();

            if (!CommandRegistry.TryRegister(new InjuryCommand(), PluginName, out var error))
                Logger.LogError($"Failed to register injury command: {error}");

            Logger.LogInfo("Expie's Curse loaded. Use 'ri list' in the developer console.");
        }

        private void Update()
        {
        }

        private static void RegisterWorldSettings()
        {
            WorldSettingsApi.AddBool(SettingEnabled, "Enable Expie's curse", false);
            WorldSettingsApi.AddFloat(SettingInterval, "Delay between injuries", 10f, 1f, 30f, true, " min");
            WorldSettingsApi.AddDropdown(SettingSeverity, "Injury Severity", new[] { "Max", "Default", "Min" }, 1);
        }

        public static void TickCurseTimer()
        {
            Instance?.UpdateCurseTimer();
        }

        public static void ResetCurseTimer()
        {
            if (Instance == null)
                return;

            Instance._curseTimerSeconds = 0f;
            Instance._lastCurseRunTime = float.NaN;
            Instance._curseInjuryQueued = false;
        }

        private void UpdateCurseTimer()
        {
            var world = WorldGeneration.world;
            if (world == null || !world.worldExists)
            {
                _curseTimerSeconds = 0f;
                _lastCurseRunTime = float.NaN;
                _curseInjuryQueued = false;
                _loggedMissingSettings = false;
                return;
            }

            if (WorldGeneration.runSettings == null)
            {
                _curseTimerSeconds = 0f;
                _lastCurseRunTime = float.NaN;
                _curseInjuryQueued = false;
                if (!_loggedMissingSettings)
                {
                    _loggedMissingSettings = true;
                    GameUtil.Notify("Expie's curse: run settings missing", false);
                }
                return;
            }

            var enabled = WorldSettingsApi.GetBool(SettingEnabled, false);
            var intervalMinutes = Mathf.Clamp(WorldSettingsApi.GetFloat(SettingInterval, 10f), 1f, 30f);
            var severity = GetCurseSeverity();

            if (!enabled)
            {
                _curseTimerSeconds = 0f;
                _lastCurseRunTime = float.NaN;
                _curseInjuryQueued = false;
                return;
            }

            var runTime = WorldGeneration.TotalRunTime();
            if (float.IsNaN(_lastCurseRunTime) || runTime < _lastCurseRunTime)
                _lastCurseRunTime = runTime;

            _curseTimerSeconds = runTime - _lastCurseRunTime;

            if (_curseInjuryQueued)
                return;

            if (_curseTimerSeconds < intervalMinutes * 60f)
                return;

            _curseTimerSeconds = 0f;
            _lastCurseRunTime = runTime;
            _curseInjuryQueued = true;

            GameUtil.Log($"[Expie's Curse] Triggered; applying delayed {severity.ToString().ToLowerInvariant()} injury.");
            RunDelayedInjurySafe(() =>
            {
                try
                {
                    var result = InjuryApplier.ApplyRandom(severity, out var injuryName);
                    GameUtil.Log($"[Expie's Curse] {injuryName} ({severity.ToString().ToLowerInvariant()}): {result.Message}");
                }
                catch (Exception ex)
                {
                    Instance?.Logger.LogError($"Failed to apply delayed injury: {ex}");
                }
                finally
                {
                    _curseInjuryQueued = false;
                }
            });
        }

        private static SeverityMode GetCurseSeverity()
        {
            var value = WorldSettingsApi.GetInt(SettingSeverity, 1);
            if (value >= 2) return SeverityMode.Min;
            if (value <= 0) return SeverityMode.High;
            return SeverityMode.Random;
        }

        public void RunDelayedInjury(Action applyInjury)
        {
            StartCoroutine(DelayedInjuryCoroutineStatic(applyInjury));
        }

        public static void RunDelayedInjurySafe(Action applyInjury)
        {
            if (Instance != null)
            {
                Instance.RunDelayedInjury(applyInjury);
                return;
            }

            var runnerObject = new GameObject("ExpiesCurse.DelayRunner");
            DontDestroyOnLoad(runnerObject);
            var runner = runnerObject.AddComponent<DelayedInjuryRunner>();
            runner.Run(applyInjury);
        }

        internal static IEnumerator DelayedInjuryCoroutineStatic(Action applyInjury)
        {
            ShowImpendingDoomMoodle = true;
            try
            {
                var body = GameUtil.GetBody();
                if (body != null && body.talker != null)
                    body.talker.Talk(DelayedInjuryLines[UnityEngine.Random.Range(0, DelayedInjuryLines.Length)], null, true, true);

                var startTime = WorldGeneration.TotalRunTime();
                while (WorldGeneration.TotalRunTime() - startTime < 10f)
                {
                    var world = WorldGeneration.world;
                    if (world == null || !world.worldExists || !PlayerUtil.IsAlive())
                        yield break;

                    yield return null;
                }

                applyInjury?.Invoke();
            }
            finally
            {
                ShowImpendingDoomMoodle = false;
            }
        }
    }

    internal sealed class DelayedInjuryRunner : MonoBehaviour
    {
        public void Run(Action applyInjury)
        {
            StartCoroutine(RunAndDestroy(applyInjury));
        }

        private IEnumerator RunAndDestroy(Action applyInjury)
        {
            yield return ExpiesCursePlugin.DelayedInjuryCoroutineStatic(applyInjury);
            Destroy(gameObject);
        }
    }

    [HarmonyPatch(typeof(MoodleManager), "AddAllMoodles")]
    internal static class ImpendingDoomMoodlePatch
    {
        private static readonly FieldInfo IconsField = typeof(MoodleManager).GetField("icons", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static void Postfix(MoodleManager __instance)
        {
            if (!ExpiesCursePlugin.ShowImpendingDoomMoodle || __instance == null)
                return;

            var icon = GetSafeIconKey(__instance);
            if (icon == null)
                return;

            try
            {
                __instance.AddMoodle(
                    3,
                    icon,
                    "Sense of impending doom",
                    "Something terrible is about to happen.",
                    true,
                    true);
            }
            catch
            {
                // A missing/changed icon key should never break the game's moodle update loop.
            }
        }

        private static string GetSafeIconKey(MoodleManager manager)
        {
            var icons = IconsField?.GetValue(manager) as Dictionary<string, Sprite>;
            if (icons == null || icons.Count == 0)
                return null;

            foreach (var preferred in new[] { "warning", "horrified", "shock", "pain", "mooddown", "healthpanel-alert" })
            {
                if (icons.ContainsKey(preferred))
                    return preferred;
            }

            foreach (var key in icons.Keys)
                return key;

            return null;
        }
    }

    [HarmonyPatch(typeof(WorldGeneration), "UpdateWorld")]
    internal static class CurseTimerWorldUpdatePatch
    {
        private static void Postfix()
        {
            ExpiesCursePlugin.TickCurseTimer();
        }
    }

    [HarmonyPatch(typeof(MoodleManager), "Update")]
    internal static class CurseTimerMoodleUpdatePatch
    {
        private static void Postfix()
        {
            ExpiesCursePlugin.TickCurseTimer();
        }
    }
}
