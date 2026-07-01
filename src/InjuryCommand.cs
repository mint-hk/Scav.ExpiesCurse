using System.Collections.Generic;
using ScavLib.command;
using ScavLib.util;
using Scav.WorldSettingsHelper;

namespace Scav.ExpiesCurse
{
    internal sealed class InjuryCommand : BaseCommand
    {
        private static readonly List<string> InjuryNames = new List<string>(InjuryApplier.Injuries.Keys);

        public override string Name => "ri";
        public override string Description => "Scav.ExpiesCurse test commands.";

        public override (string, string)[] ArgDescription => new[]
        {
            ("string injury", "Injury name, 'list', or 'random'."),
            ("string severity", "Optional severity: min, medium, high, random.")
        };

        public override Dictionary<int, List<string>> ArgAutofill => new Dictionary<int, List<string>>
        {
            { 0, BuildAutofill() },
            { 1, new List<string> { "random", "min", "medium", "high" } }
        };

        public override void Execute(string[] args)
        {
            if (args.Length < 2 || IsHelp(args[1]))
            {
                PrintHelp();
                return;
            }

            var requested = args[1].ToLowerInvariant();
            var severity = ParseSeverity(args);
            var delayed = ParseDelay(args);
            if (requested == "list")
            {
                PrintInjuryList();
                return;
            }

            if (requested == "status")
            {
                PrintStatus();
                return;
            }

            if (!PlayerUtil.IsAlive())
            {
                GameUtil.Log("[Expie's Curse] Player is not alive or no world is loaded.");
                return;
            }

            if (requested == "random")
            {
                ExecuteInjury(delayed, () => ApplyRandomInjury(severity));
                return;
            }

            if (!InjuryApplier.Injuries.TryGetValue(requested, out var injury))
            {
                GameUtil.Log($"[Expie's Curse] Unknown injury '{args[1]}'. Use 'ri list'.");
                return;
            }

            ExecuteInjury(delayed, () =>
            {
                var result = injury(severity);
                return $"{requested}: {result.Message}";
            });
        }

        private static bool IsHelp(string value)
        {
            return value == "help" || value == "?" || value == "--help";
        }

        private static List<string> BuildAutofill()
        {
            var values = new List<string> { "list", "random", "status" };
            values.AddRange(InjuryNames);
            return values;
        }

        private static void PrintHelp()
        {
            GameUtil.Log("Usage: ri <list|random|injury> [min|medium|high|random] [delay=true|false]");
            GameUtil.Log("Example: ri bleeding_wound high true");
        }

        private static void PrintInjuryList()
        {
            GameUtil.Log("[Expie's Curse] Available injuries:");
            foreach (var name in InjuryNames)
                GameUtil.Log($"  {name}");
        }

        private static void PrintStatus()
        {
            var world = WorldGeneration.world;
            var settings = WorldGeneration.runSettings;

            GameUtil.Log("[Expie's Curse] Status:");
            GameUtil.Log($"  IsInGame: {GameUtil.IsInGame}");
            GameUtil.Log($"  IsWorldLoaded: {GameUtil.IsWorldLoaded}");
            GameUtil.Log($"  World exists object: {world != null}");
            GameUtil.Log($"  World worldExists: {(world != null && world.worldExists)}");
            GameUtil.Log($"  runSettings exists: {settings != null}");

            if (settings == null)
                return;

            GameUtil.Log($"  runSettings count: {settings.Count}");
            PrintSetting(settings, ExpiesCursePlugin.SettingEnabled);
            PrintSetting(settings, ExpiesCursePlugin.SettingSeverity);
            PrintSetting(settings, ExpiesCursePlugin.SettingInterval);
            GameUtil.Log($"  Parsed curse severity: {ParseCurseSeverity(settings).ToString().ToLowerInvariant()}");
            if (world != null)
                GameUtil.Log($"  Total run time: {WorldGeneration.TotalRunTime():F1}");
            GameUtil.Log($"  Last curse run time: {ExpiesCursePlugin.LastCurseRunTime:F1}");
            GameUtil.Log($"  Curse timer seconds: {ExpiesCursePlugin.CurseTimerSeconds:F1}");
            GameUtil.Log($"  Curse injury queued: {ExpiesCursePlugin.CurseInjuryQueued}");
        }

        private static void PrintSetting(Dictionary<string, object> settings, string key)
        {
            if (settings.TryGetValue(key, out var value))
                GameUtil.Log($"  {key}: {value} ({value?.GetType().Name ?? "null"})");
            else
                GameUtil.Log($"  {key}: <missing>");
        }

        private static SeverityMode ParseCurseSeverity(Dictionary<string, object> settings)
        {
            var intValue = WorldSettingsApi.GetInt(ExpiesCursePlugin.SettingSeverity, 1);

            if (intValue >= 2) return SeverityMode.Min;
            if (intValue <= 0) return SeverityMode.High;
            return SeverityMode.Random;
        }

        private static string ApplyRandomInjury(SeverityMode severity)
        {
            var result = InjuryApplier.ApplyRandom(severity, out var name);
            return $"{name} ({severity.ToString().ToLowerInvariant()}): {result.Message}";
        }

        private static SeverityMode ParseSeverity(string[] args)
        {
            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "min":
                        return SeverityMode.Min;
                    case "medium":
                    case "med":
                        return SeverityMode.Medium;
                    case "high":
                    case "max":
                        return SeverityMode.High;
                    case "random":
                        return SeverityMode.Random;
                }
            }

            return SeverityMode.Random;
        }

        private static bool ParseDelay(string[] args)
        {
            for (var i = 2; i < args.Length; i++)
            {
                var value = args[i].ToLowerInvariant();
                if (value == "true" || value == "delay" || value == "delayed" || value == "delay=true")
                    return true;
            }

            return false;
        }

        private static void ExecuteInjury(bool delayed, System.Func<string> applyInjury)
        {
            if (!delayed)
            {
                GameUtil.Log($"[Expie's Curse] {applyInjury()}");
                return;
            }

            GameUtil.Log("[Expie's Curse] Delayed injury queued for 10 seconds.");
            ExpiesCursePlugin.RunDelayedInjurySafe(() => GameUtil.Log($"[Expie's Curse] {applyInjury()}"));
        }
    }
}
