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
            var skinDamage = Roll(45f, 75f, severity);
            var muscleDamage = Roll(55f, 85f, severity);
            var bleed = Roll(20f, 45f, severity);
            var pain = Roll(45f, 75f, severity);

            var limb = GetRandomUsableLimb(l => CanApplyLimbDamage(l, skinDamage, muscleDamage, bleed, pain));
            if (limb == null) return InjuryResult.Skip("bleeding wound would exceed safety limits on all usable limbs");

            LimbUtil.DamageSkin(limb, skinDamage);
            LimbUtil.DamageMuscle(limb, muscleDamage);
            LimbUtil.SetBleedRaw(limb, limb.bleedAmount + bleed);
            LimbUtil.SetPainRaw(limb, limb.pain + pain);
            PlayInjurySound("loudStab");

            return InjuryResult.Apply($"bleeding wound {LimbName(limb)}: skin -{skinDamage:F0}, muscle -{muscleDamage:F0}, bleed +{bleed:F0}, pain +{pain:F0}");
        }

        private static InjuryResult Shrapnel(SeverityMode severity)
        {
            var skinDamage = Roll(45f, 75f, severity);
            var muscleDamage = Roll(55f, 85f, severity);
            var bleed = Roll(16f, 38f, severity);
            var pain = Roll(45f, 75f, severity);
            var shrapnel = 5;

            var limb = GetRandomUsableLimb(l => CanApplyLimbDamage(l, skinDamage, muscleDamage, bleed, pain) && l.shrapnel + shrapnel < 10);
            if (limb == null) return InjuryResult.Skip("shrapnel would exceed safety limits on all usable limbs");

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
            var infection = Roll(40f, 60f, severity);
            var limb = GetRandomUsableLimb(l => l.infectionAmount + infection < 75f);
            if (limb == null) return InjuryResult.Skip("infection would exceed safety limits on all usable limbs");

            LimbUtil.SetInfectionRaw(limb, limb.infectionAmount + infection);
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"infection {LimbName(limb)}: infection +{infection:F0}");
        }

        private static InjuryResult Fracture(SeverityMode severity)
        {
            var pain = Roll(55f, 75f, severity);
            var head = LimbUtil.GetLimb(LimbSlot.Head);
            var limb = GetRandomUsableLimb(l => !l.broken && l != head && l.pain + pain < 85f);
            if (limb == null) return InjuryResult.Skip("fracture would exceed safety limits on all usable limbs");

            limb.BreakBone();
            limb.boneHealTimer = Roll(6f, 18f, severity);
            LimbUtil.SetPainRaw(limb, limb.pain + pain);

            return InjuryResult.Apply($"fracture {LimbName(limb)}, timer {limb.boneHealTimer:F0}");
        }

        private static InjuryResult Dislocation(SeverityMode severity)
        {
            var skinDamage = Roll(10f, 24f, severity);
            var bleed = Roll(6f, 16f, severity);
            var pain = Roll(50f, 70f, severity);

            var limb = GetRandomUsableLimb(l => !l.dislocated && CanApplyLimbDamage(l, skinDamage, 0f, bleed, pain));
            if (limb == null) return InjuryResult.Skip("dislocation would exceed safety limits on all usable limbs");

            limb.Dislocate();
            LimbUtil.DamageSkin(limb, skinDamage);
            LimbUtil.SetBleedRaw(limb, limb.bleedAmount + bleed);
            LimbUtil.SetPainRaw(limb, limb.pain + pain);

            return InjuryResult.Apply($"dislocation {LimbName(limb)}: skin -{skinDamage:F0}, bleed +{bleed:F0}");
        }

        private static InjuryResult InternalBleeding(SeverityMode severity)
        {
            if (PlayerUtil.GetInternalBleeding() >= 20f || PlayerUtil.GetHemothorax() >= 60f)
                return InjuryResult.Skip("internal bleeding or hemothorax is already high");

            var bleeding = Roll(14f, 18f, severity);
            var hemothorax = Roll(8f, 18f, severity);
            var thorax = LimbUtil.GetLimb(LimbSlot.Thorax);
            var pain = Roll(20f, 35f, severity);

            if (PlayerUtil.GetInternalBleeding() + bleeding >= 20f || PlayerUtil.GetHemothorax() + hemothorax >= 60f || (thorax != null && thorax.pain + pain >= 85f))
                return InjuryResult.Skip("internal bleeding would exceed safety limits");

            PlayerUtil.SetInternalBleedingRaw(PlayerUtil.GetInternalBleeding() + bleeding);
            PlayerUtil.SetHemothoraxRaw(PlayerUtil.GetHemothorax() + hemothorax);
            if (thorax != null)
                LimbUtil.SetPainRaw(thorax, thorax.pain + pain);
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"internal bleeding +{bleeding:F0}, hemothorax +{hemothorax:F0}");
        }

        private static InjuryResult Venom(SeverityMode severity)
        {
            if (PlayerUtil.GetVenomTotal() >= 85f || PlayerUtil.GetVenomCurrent() >= 75f)
                return InjuryResult.Skip("venom is already high");

            var total = Roll(60f, 85f, severity);
            var current = Roll(45f, 65f, severity);

            if (PlayerUtil.GetVenomTotal() + total > 85f || PlayerUtil.GetVenomCurrent() + current > 75f)
                return InjuryResult.Skip("venom would exceed safety limits");

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
            if (PlayerUtil.GetRadiationSickness() + radiation > 15f)
                return InjuryResult.Skip("radiation sickness would exceed safety limits");

            PlayerUtil.SetRadiationSicknessRaw(PlayerUtil.GetRadiationSickness() + radiation);
            PlayInjurySound("fleshrip");

            return InjuryResult.Apply($"radiation sickness +{radiation:F0}, now {PlayerUtil.GetRadiationSickness():F0}");
        }

        private static InjuryResult Sickness(SeverityMode severity)
        {
            if (PlayerUtil.GetSicknessAmount() >= 75f)
                return InjuryResult.Skip("sickness is already high");

            var sickness = Roll(45f, 70f, severity);
            if (PlayerUtil.GetSicknessAmount() + sickness >= 75f)
                return InjuryResult.Skip("sickness would exceed safety limits");

            PlayerUtil.SetSicknessAmountRaw(PlayerUtil.GetSicknessAmount() + sickness);
            PlayInjurySound("burp");

            return InjuryResult.Apply($"sickness +{sickness:F0}, now {PlayerUtil.GetSicknessAmount():F0}");
        }

        private static InjuryResult Hunger(SeverityMode severity)
        {
            if (PlayerUtil.GetHunger() <= 30f)
                return InjuryResult.Skip("hunger is already dangerously low");

            var value = Roll(20f, 30f, severity);
            PlayerUtil.SetHungerRaw(value);
            PlayInjurySound("thirstdown");
            return InjuryResult.Apply($"hunger set to {PlayerUtil.GetHunger():F0}");
        }

        private static InjuryResult Thirst(SeverityMode severity)
        {
            if (PlayerUtil.GetThirst() <= 35f)
                return InjuryResult.Skip("thirst is already dangerously low");

            var value = Roll(25f, 35f, severity);
            PlayerUtil.SetThirstRaw(value);
            PlayInjurySound("thirstdown");
            return InjuryResult.Apply($"thirst set to {PlayerUtil.GetThirst():F0}");
        }

        private static InjuryResult Happiness(SeverityMode severity)
        {
            if (PlayerUtil.GetHappinessBase() <= -60f)
                return InjuryResult.Skip("happiness is already very low");

            var value = Roll(15f, 30f, severity);
            if (PlayerUtil.GetHappinessBase() - value < -60f)
                return InjuryResult.Skip("happiness would exceed safety limits");

            PlayerUtil.SetHappinessRaw(PlayerUtil.GetHappinessBase() - value);
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

            if (PlayerUtil.GetHearingLoss() + value > 80f || (head != null && head.pain + 100f > 100f))
                return InjuryResult.Skip("hearing loss would exceed safety limits");

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

            if (PlayerUtil.GetBrainHealth() - brainDamage < 80f || (head != null && head.pain + 100f > 100f))
                return InjuryResult.Skip("brain damage would exceed safety limits");

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
            var limbs = new List<Limb>(LimbUtil.GetAllLimbs());
            for (var i = limbs.Count - 1; i >= 0; i--)
            {
                var limb = limbs[i];
                if (limb == null || limb.dismembered || (predicate != null && !predicate(limb)))
                    limbs.RemoveAt(i);
            }

            return limbs.Count == 0 ? null : limbs[Random.Range(0, limbs.Count)];
        }

        private static bool CanApplyLimbDamage(Limb limb, float skinDamage, float muscleDamage, float bleed, float pain)
        {
            return limb.skinHealth - skinDamage > 15f
                && limb.muscleHealth - muscleDamage > 15f
                && limb.bleedAmount + bleed < 70f
                && limb.pain + pain < 85f;
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
