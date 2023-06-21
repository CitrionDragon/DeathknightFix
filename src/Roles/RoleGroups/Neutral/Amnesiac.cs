using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Extensions;
using Lotus.Factions.Crew;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using Object = UnityEngine.Object;

namespace Lotus.Roles.RoleGroups.Neutral;

public class Amnesiac : CustomRole, IVariableRole
{
    private static Amalgamation _amalgamation = new();

    private bool stealExactRole;
    private bool hasArrowsToBodies;

    private Remote<IndicatorComponent>? arrowComponent;

    protected override void PostSetup()
    {
        if (!hasArrowsToBodies) return;
        IndicatorComponent? indicator = MyPlayer.NameModel().GCH<IndicatorHolder>().LastOrDefault();
        if (indicator == null) return;
        arrowComponent = MyPlayer.NameModel().GCH<IndicatorHolder>().GetRemote(indicator);
    }

    [UIComponent(UI.Indicator)]
    private string Arrows() => hasArrowsToBodies ? Object.FindObjectsOfType<DeadBody>()
        .Where(b => !Game.MatchData.UnreportableBodies.Contains(b.ParentId))
        .Select(b => RoleUtils.CalculateArrow(MyPlayer, b.TruePosition, RoleColor)).Fuse("") : "";

    [RoleAction(RoleActionType.AnyReportedBody)]
    public void AmnesiacRememberAction(PlayerControl reporter, GameData.PlayerInfo reported, ActionHandle handle)
    {
        VentLogger.Trace($"Reporter: {reporter.name} | Reported: {reported.GetNameWithRole()} | Self: {MyPlayer.name}", "");

        if (reporter.PlayerId != MyPlayer.PlayerId) return;
        CustomRole targetRole = reported.GetCustomRole();
        Copycat.FallbackTypes.GetOptional(targetRole.GetType()).IfPresent(r => targetRole = r());

        if (!stealExactRole)
        {
            if (targetRole.SpecialType is SpecialType.NeutralKilling)
                targetRole = CustomRoleManager.Static.Hitman;
            else if (targetRole.SpecialType is SpecialType.Neutral)
                targetRole = CustomRoleManager.Static.Opportunist;
            else if (targetRole.Faction is Crewmates)
                targetRole = CustomRoleManager.Static.Sheriff;
            else
                targetRole = CustomRoleManager.Static.Jester;
        }

        CustomRole newRole = CustomRoleManager.GetCleanRole(targetRole);

        MatchData.AssignRole(MyPlayer, newRole);

        CustomRole role = MyPlayer.GetCustomRole();
        if (role.RealRole is RoleTypes.Crewmate or RoleTypes.Scientist) role.RoleAbilityFlags |= RoleAbilityFlag.CannotVent;
        role.DesyncRole = RoleTypes.Impostor;
        arrowComponent?.Delete();
        handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub.KeyName("Steals Exact Role", Translations.Options.StealsExactRole)
                .Bind(v => stealExactRole = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub.KeyName("Has Arrows to Bodies", Translations.Options.HasArrowsToBody)
                .AddOnOffValues()
                .BindBool(b => hasArrowsToBodies = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(new Color(0.51f, 0.87f, 0.99f))
            .RoleFlags(RoleFlag.CannotWinAlone)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.CannotVent)
            .SpecialType(SpecialType.Neutral)
            .DesyncRole(RoleTypes.Impostor)
            .Faction(FactionInstances.Neutral)
            .LinkedRoles(_amalgamation);

    [Localized(nameof(Amnesiac))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(StealsExactRole))]
            public static string StealsExactRole = "Steals Exact Role";

            [Localized(nameof(HasArrowsToBody))]
            public static string HasArrowsToBody = "Has Arrows to Bodies";
        }
    }

    public CustomRole Variation() => _amalgamation;

    public bool AssignVariation() => RoleUtils.RandomSpawn(_amalgamation);
}