using System;
using System.Collections.Generic;
using ScavLib.util;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scav.ExpiesCurse
{
    internal static class InjuryApplier
    {
        private static readonly Dictionary<string, int> SoundCycleIndexes = new Dictionary<string, int>();

        public static readonly IReadOnlyDictionary<string, Func<SeverityMode, InjuryResult>> Injuries =
            new Dictionary<string, Func<SeverityMode, InjuryResult>>(StringComparer.OrdinalIgnoreCase)
            {
                { "bleeding_wound", BleedingWound },
                { "shrapnel", Shrapnel },
                { "infection", Infection },
                { "fracture", Fracture },
                { "dislocation", Dislocation },
                { "internal_bleeding", InternalBleeding },
                { "venom", Venom },
                { "radiation", Radiation },
                { "sickness", Sickness },
                { "hunger", Hunger },
                { "thirst", Thirst },
                { "happiness", Happiness },
                { "hearing", Hearing },
                { "brain_damage", BrainDamage },
            };

        public static SeverityMode Severity { get; set; } = SeverityMode.Random;

        public static InjuryResult ApplyRandom(SeverityMode severity, out string injuryName)
        {
            var remaining = new List<string>(Injuries.Keys);
            while (remaining.Count > 0)
            {
                var index = Random.Range(0, remaining.Count);
                injuryName = remaining[index];
                remaining.RemoveAt(index);

                var result = Injuries[injuryName](severity);
                if (result.Applied)
                    return result;
            }

            injuryName = "random";
            return InjuryResult.Skip("all injury types are already too severe");
        }

        private static InjuryResult BleedingWound(SeverityMode severity)
        {
            var limb = GetRandomUsableLimb(l => l.bleedAmount < 70f && l.pain < 85f && l.skinHealth > 15f && l.muscleHealth > 15f);
            if (limb == null) return InjuryResult.Skip("all usable limbs are already heavily wounded");

            var skinDamage = Roll(45f, 75f, severity);
            var muscleDamage = Roll(55f, 85f, severity);
            var bleed = Roll(20f, 45f, severity);
            var pain = Roll(45f, 75f, severity);

            LimbUtil.DamageSkin(limb, skinDamage);
            LimbUtil.DamageMuscle(limb, muscleDamage);
            LimbUtil.SetBleedRaw(limb, limb.bleedAmount + bleed);
            LimbUtil.SetPainRaw(limb, limb.pain + pain);
            PlayInjurySound("loudStab");

            return InjuryResult.Apply($"bleeding wound {LimbName(limb)}: skin -{skinDamage:F0}, muscle -{muscleDamage:F0}, bleed +{bleed:F0}, pain +{pain:F0}");
        }

        private static InjuryResult Shrapnel(SeverityMode severity)
        {
            var limb = GetRandomUsableLimb(l => l.bleedAmount < 70f && l.pain < 85f && l.skinHealth > 15f && l.muscleHealth > 15f && l.shrapnel < 4);
            if (limb == null) return InjuryResult.Skip("all usable limbs are already heavily wounded or full of shrapnel");

            var skinDamage = Roll(45f, 75f, severity);
            var muscleDamage = Roll(55f, 85f, severity);
            var bleed = Roll(16f, 38f, severity);
            var pain = Roll(45f, 75f, severity);
            var shrapnel = 5;

            LimbUtil.DamageSkin(limb, skinDamage);
            LimbUtil.DamageMuscle(limb, muscleDamage);
            LimbUtil.SetBleedRaw(limb, limb.bleedAmount + bleed);
            LimbUtil.SetPainRaw(limb, limb.pain + pain);
            limb.shrapnel += shrapnel;
            PlayInjurySound("glassshard");

            return InjuryResult.Apply($"shrapnel {LimbName(limb)}: skin -{skinDamage:F0}, muscle -{muscleDamage:F0}, bleed +{bleed:F0}, pain +{pain:F0}, shrapnel +{shrapnel}");
        }

        private static InjuryResult Infection(SeverityMode severity)
        {
            var limb = GetRandomUsableLimb(l => l.infectionAmount < 75f);
            if (limb == null) return InjuryResult.Skip("all usable limbs already have high infection");

            var infection = Roll(40f, 60f, severity);
            LimbUtil.SetInfectionRaw(limb, limb.infectionAmount + infection);
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"infection {LimbName(limb)}: infection +{infection:F0}");
        }

        private static InjuryResult Fracture(SeverityMode severity)
        {
            var limb = GetRandomUsableLimb(l => !l.broken && l != LimbUtil.GetLimb(LimbSlot.Head));
            if (limb == null) return InjuryResult.Skip("all usable limbs are already broken");

            limb.BreakBone();
            limb.boneHealTimer = Roll(6f, 18f, severity);
            LimbUtil.SetPainRaw(limb, limb.pain + Roll(55f, 75f, severity));

            return InjuryResult.Apply($"fracture {LimbName(limb)}, timer {limb.boneHealTimer:F0}");
        }

        private static InjuryResult Dislocation(SeverityMode severity)
        {
            var limb = GetRandomUsableLimb(l => !l.dislocated);
            if (limb == null) return InjuryResult.Skip("all usable limbs are already dislocated");

            limb.Dislocate();
            var skinDamage = Roll(10f, 24f, severity);
            var bleed = Roll(6f, 16f, severity);
            LimbUtil.DamageSkin(limb, skinDamage);
            LimbUtil.SetBleedRaw(limb, limb.bleedAmount + bleed);
            LimbUtil.SetPainRaw(limb, limb.pain + Roll(50f, 70f, severity));

            return InjuryResult.Apply($"dislocation {LimbName(limb)}: skin -{skinDamage:F0}, bleed +{bleed:F0}");
        }

        private static InjuryResult InternalBleeding(SeverityMode severity)
        {
            if (PlayerUtil.GetInternalBleeding() >= 20f || PlayerUtil.GetHemothorax() >= 60f)
                return InjuryResult.Skip("internal bleeding or hemothorax is already high");

            var bleeding = Roll(14f, 18f, severity);
            var hemothorax = Roll(8f, 18f, severity);
            var thorax = LimbUtil.GetLimb(LimbSlot.Thorax);

            PlayerUtil.SetInternalBleedingRaw(PlayerUtil.GetInternalBleeding() + bleeding);
            PlayerUtil.SetHemothoraxRaw(PlayerUtil.GetHemothorax() + hemothorax);
            if (thorax != null)
                LimbUtil.SetPainRaw(thorax, thorax.pain + Roll(20f, 35f, severity));
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"internal bleeding +{bleeding:F0}, hemothorax +{hemothorax:F0}");
        }

        private static InjuryResult Venom(SeverityMode severity)
        {
            if (PlayerUtil.GetVenomTotal() >= 85f || PlayerUtil.GetVenomCurrent() >= 75f)
                return InjuryResult.Skip("venom is already high");

            var total = Roll(60f, 85f, severity);
            var current = Roll(45f, 65f, severity);

            PlayerUtil.SetVenomTotalRaw(PlayerUtil.GetVenomTotal() + total);
            PlayerUtil.SetVenomCurrentRaw(PlayerUtil.GetVenomCurrent() + current);
            PlayInjurySound("spiderbite");

            return InjuryResult.Apply($"venom total +{total:F0}, current +{current:F0}");
        }

        private static InjuryResult Radiation(SeverityMode severity)
        {
            if (PlayerUtil.GetRadiationSickness() > 15f)
                return InjuryResult.Skip("radiation sickness is already high");

            var radiation = Roll(6f, 9f, severity);
            PlayerUtil.SetRadiationSicknessRaw(PlayerUtil.GetRadiationSickness() + radiation);
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"radiation sickness +{radiation:F0}, now {PlayerUtil.GetRadiationSickness():F0}");
        }

        private static InjuryResult Sickness(SeverityMode severity)
        {
            if (PlayerUtil.GetSicknessAmount() >= 75f)
                return InjuryResult.Skip("sickness is already high");

            var sickness = Roll(45f, 70f, severity);
            PlayerUtil.SetSicknessAmountRaw(PlayerUtil.GetSicknessAmount() + sickness);
            PlayInjurySound("burp");

            return InjuryResult.Apply($"sickness +{sickness:F0}, now {PlayerUtil.GetSicknessAmount():F0}");
        }

        private static InjuryResult Hunger(SeverityMode severity)
        {
            if (PlayerUtil.GetHunger() <= 25f)
                return InjuryResult.Skip("hunger is already dangerously low");

            var value = Roll(10f, 25f, severity);
            PlayerUtil.SetHungerRaw(value);
            PlayInjurySound("thirstdown");
            return InjuryResult.Apply($"hunger set to {PlayerUtil.GetHunger():F0}");
        }

        private static InjuryResult Thirst(SeverityMode severity)
        {
            if (PlayerUtil.GetThirst() <= 25f)
                return InjuryResult.Skip("thirst is already dangerously low");

            var value = Roll(10f, 25f, severity);
            PlayerUtil.SetThirstRaw(value);
            PlayInjurySound("thirstdown");
            return InjuryResult.Apply($"thirst set to {PlayerUtil.GetThirst():F0}");
        }

        private static InjuryResult Happiness(SeverityMode severity)
        {
            if (PlayerUtil.GetHappinessBase() <= -60f)
                return InjuryResult.Skip("happiness is already very low");

            var value = Roll(15f, 30f, severity);
            PlayerUtil.SetHappinessRaw(Mathf.Max(PlayerUtil.GetHappinessBase() - value, -60f));
            PlayInjurySound("mooddown");
            return InjuryResult.Apply($"base happiness -{value:F0}, now {PlayerUtil.GetHappinessBase():F0}");
        }

        private static InjuryResult Hearing(SeverityMode severity)
        {
            if (PlayerUtil.GetHearingLoss() >= 80f)
                return InjuryResult.Skip("hearing loss is already high");

            var value = Roll(60f, 80f, severity);
            var consciousness = Roll(30f, 25f, severity);
            var head = LimbUtil.GetLimb(LimbSlot.Head);
            PlayerUtil.SetHearingLossRaw(PlayerUtil.GetHearingLoss() + value);
            PlayerUtil.SetConsciousnessRaw(consciousness);
            if (head != null)
                LimbUtil.SetPainRaw(head, head.pain + 100f);
            CreateHarmlessExplosion();
            return InjuryResult.Apply($"hearing loss +{value:F0}, consciousness set to {consciousness:F0}, head pain +100");
        }

        private static InjuryResult BrainDamage(SeverityMode severity)
        {
            if (PlayerUtil.GetBrainHealth() < 80f)
                return InjuryResult.Skip("brain health is already low");

            var brainDamage = Roll(4f, 7f, severity);
            var consciousness = Roll(30f, 25f, severity);
            var head = LimbUtil.GetLimb(LimbSlot.Head);

            PlayerUtil.SetBrainHealthRaw(PlayerUtil.GetBrainHealth() - brainDamage);
            PlayerUtil.SetConsciousnessRaw(consciousness);
            if (head != null)
                LimbUtil.SetPainRaw(head, head.pain + 100f);

            CreateHarmlessExplosion();
            return InjuryResult.Apply($"brain health -{brainDamage:F1}, consciousness set to {consciousness:F0}, head pain +100");
        }

        private static void PlayInjurySound(params string[] soundIds)
        {
            PlayInjurySound(1f, soundIds);
        }

        private static void PlayInjurySound(float volume, params string[] soundIds)
        {
            if (!GameUtil.IsInGame) return;
            if (soundIds == null || soundIds.Length == 0) return;

            var position = GameUtil.GetPlayerPosition();
            var key = string.Join("|", soundIds);
            SoundCycleIndexes.TryGetValue(key, out var startIndex);

            for (var i = 0; i < soundIds.Length; i++)
            {
                var index = (startIndex + i) % soundIds.Length;
                var soundId = soundIds[index];

                try
                {
                    var source = Sound.Play(soundId, position, false, false, null, volume, 1f, false, false);
                    if (source != null)
                    {
                        SoundCycleIndexes[key] = (index + 1) % soundIds.Length;
                        return;
                    }
                }
                catch
                {
                    // Sound IDs vary by build; missing candidates are safe to ignore.
                }
            }
        }

        private static void CreateHarmlessExplosion()
        {
            if (GameUtil.GetWorld() == null) return;

            WorldGeneration.CreateExplosion(new ExplosionParams
            {
                position = GameUtil.GetPlayerPosition(),
                range = 3f,
                velocity = 0f,
                structuralDamage = 0f,
                skinDamageChance = 0f,
                boneBreakChance = 0f,
                dislocationChance = 0f,
                disfigureChance = 0f,
                bleedChance = 0f,
                shrapnelChance = 0f,
                skinDamage = new RangeF(0f, 0f),
                muscleDamage = new RangeF(0f, 0f),
                bleedAmount = new RangeF(0f, 0f),
                sound = "explosion"
            });
        }

        private static Limb GetRandomUsableLimb(Func<Limb, bool> predicate = null)
        {
            var limbs = LimbUtil.GetAllLimbs();
            for (var i = limbs.Count - 1; i >= 0; i--)
            {
                var limb = limbs[i];
                if (limb == null || limb.dismembered || (predicate != null && !predicate(limb)))
                    limbs.RemoveAt(i);
            }

            return limbs.Count == 0 ? null : limbs[Random.Range(0, limbs.Count)];
        }

        private static string LimbName(Limb limb)
        {
            return limb != null ? limb.name : "unknown limb";
        }

        private static float Roll(float min, float max, SeverityMode severity)
        {
            switch (severity)
            {
                case SeverityMode.Min:
                    return min;
                case SeverityMode.Medium:
                    return (min + max) / 2f;
                case SeverityMode.High:
                    return max;
                default:
                    return Random.Range(min, max);
            }
        }
    }

    internal enum SeverityMode
    {
        Random,
        Min,
        Medium,
        High
    }

    internal sealed class InjuryResult
    {
        public bool Applied { get; }
        public string Message { get; }

        private InjuryResult(bool applied, string message)
        {
            Applied = applied;
            Message = message;
        }

        public static InjuryResult Apply(string message) => new InjuryResult(true, message);
        public static InjuryResult Skip(string message) => new InjuryResult(false, $"skipped, {message}");
    }
}
