using HarmonyLib;
using Lotus.Factions.Crew;
using Lotus.Extensions;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles2;
using Lotus.Roles2.Definitions;

namespace Lotus.Patches.Intro;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
class BeginImpostorPatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        UnifiedRoleDefinition role = PlayerControl.LocalPlayer.PrimaryRole();
        if (role.Faction is not Crewmates || role is NoOpDefinition) return true;

        yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
        yourTeam.Add(PlayerControl.LocalPlayer);


        // ReSharper disable once RemoveRedundantBraces
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!pc.AmOwner) yourTeam.Add(pc);
        }

        __instance.BeginCrewmate(yourTeam);
        __instance.overlayHandle.color = Palette.CrewmateBlue;
        return false;
    }

    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
    }
}