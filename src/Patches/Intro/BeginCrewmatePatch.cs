using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Extra;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.RoleGroups.Neutral;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Roles.Internals.Enums;
using UnityEngine;
using VentLib.Localization;
using VentLib.Logging;

namespace Lotus.Patches.Intro;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //チーム表示変更
        DevLogger.Log("Begin Crewmate");
        CustomRole role = PlayerControl.LocalPlayer.GetCustomRole();

        switch (role.SpecialType)
        {
            case SpecialType.NeutralKilling:
            case SpecialType.Undead:
            case SpecialType.Neutral:
                __instance.TeamTitle.text = role.ColoredRoleName();
                __instance.TeamTitle.color = Color.white;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "";
                __instance.BackgroundBar.material.color = role.RoleColor;
                break;
            case SpecialType.Madmate:
                __instance.TeamTitle.text = Localizer.Translate("Roles.Madmate.RoleName");
                __instance.TeamTitle.color = CustomRoleManager.Static.Madmate.RoleColor;
                __instance.ImpostorText.text = "";
                StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                break;
        }
        switch (role)
        {
            case Terrorist:
                var sound = ShipStatus.Instance.CommonTasks.FirstOrDefault(task => task.TaskType == TaskTypes.FixWiring)?.MinigamePrefab.OpenSound;
                PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                break;

            case Executioner:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                break;

            case Vampire:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                break;

            case Repairman:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                break;

            case Sheriff:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                break;
            case Arsonist:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;

            case Copycat:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                break;

            case Mayor:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;

            case GM:
                __instance.TeamTitle.text = role.RoleName;
                __instance.TeamTitle.color = role.RoleColor;
                __instance.BackgroundBar.material.color = role.RoleColor;
                __instance.ImpostorText.gameObject.SetActive(false);
                break;

        }
    }

    private static AudioClip? GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.FirstOrDefault(role => role.Role == roleType)?.IntroSound;
    }

    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await System.Threading.Tasks.Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            DevLogger.Log("???");
            await System.Threading.Tasks.Task.Delay(20);
            milliseconds += 20;
            float time = milliseconds / (float)500;
            Color lerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                VentLogger.Trace("Exit The Loop (GTranslated)", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = lerpingColor;
        }
    }
}