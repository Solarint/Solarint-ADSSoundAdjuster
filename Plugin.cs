using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Solarint_ADSSoundAdjuster
{
    [BepInPlugin("solarint.adsSound", "Solarint.AimDownSightsVolume", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            try
            {
                Settings.Init(Config);
                new PlayerAimSoundVolumePatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    internal class PlayerAimSoundVolumePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            _AimingInterruptedByOverlapField = AccessTools.Field(typeof(Player.FirearmController), "AimingInterruptedByOverlap");
            return AccessTools.Method(typeof(Player.FirearmController), "SetAim", new[] { typeof(bool) }); ;
        }

        private static FieldInfo _AimingInterruptedByOverlapField;

        [PatchPrefix]
        public static bool PatchPrefix(ref Player.FirearmController __instance, bool value, ref Player ____player)
        {
            if (__instance.Blindfire)
            {
                return false;
            }
            if (__instance.Item.IsOneOff)
            {
                value = false;
            }
            _AimingInterruptedByOverlapField.SetValue(__instance, false);
            bool isAiming = __instance.IsAiming;
            __instance.CurrentOperation.SetAiming(value);
            ____player.ProceduralWeaponAnimation.CheckShouldMoveWeaponCloser();
            ____player.Boolean_0 &= !value;
            if (isAiming == __instance.IsAiming)
            {
                return false;
            }
            float num = __instance.TotalErgonomics / 100f - 1f;
            float volume = (1.5f * num * num + 0.25f) * (1f - ____player.Skills.DrawSound);
            ____player.method_46(volume * Settings.VolumePercent.Value / 100f);
            return false;
        }
    }

    internal class Settings
    {
        private const string GeneralSectionTitle = "General";

        public static ConfigEntry<float> VolumePercent;

        public static void Init(ConfigFile Config)
        {
            VolumePercent = Config.Bind(
                GeneralSectionTitle,
                "Aim Down Sights Volume",
                50f,
                new ConfigDescription(
                    "What percent ADS volume will be played at.",
                    new AcceptableValueRange<float>(0f, 100f)
                ));

        }
    }
}
