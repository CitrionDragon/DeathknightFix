using System.Collections.Generic;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class DebugOptions
{
    private static Color _optionColor = new(1f, 0.59f, 0.38f);
    private static List<GameOption> additionalOptions = new();

    public bool NoGameEnd;
    public bool NameBasedRoleAssignment;

    public List<GameOption> AllOptions = new();

    public DebugOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(DebugOptionTranslations.DebugOptionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(Builder("NoGameEnd")
            .Name(DebugOptionTranslations.NoGameEndText)
            .BindBool(b => NoGameEnd = b)
            .IsHeader(true)
            .BuildAndRegister());

        AllOptions.Add(Builder("Name Based Role Assignment")
            .Name(DebugOptionTranslations.NameBasedRoleAssignmentText)
            .BindBool(b => NameBasedRoleAssignment = b)
            .BuildAndRegister());

        AllOptions.Add(Builder("Advanced Role Assignment")
            .Name(DebugOptionTranslations.AdvancedRoleAssignment)
            .BindBool(b => ProjectLotus.AdvancedRoleAssignment = b)
            .BuildAndRegister());

        additionalOptions.ForEach(o =>
        {
            o.Register();
            AllOptions.Add(o);
        });
    }

    /// <summary>
    /// Adds additional options to be registered when this group of options is loaded. This is mostly used for ordering
    /// in the main menu, as options passed in here will be rendered along with this group.
    /// </summary>
    /// <param name="option">Option to render</param>
    public static void AddAdditionalOption(GameOption option)
    {
        additionalOptions.Add(option);
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().AddOnOffValues(false).Builder(key, _optionColor);

    [Localized("Debug")]
    private static class DebugOptionTranslations
    {
        [Localized("SectionTitle")]
        public static string DebugOptionTitle = "Debug Options";

        [Localized("NoGameEnd")]
        public static string NoGameEndText = "Prevent Game Over";

        [Localized("NameRoleAssignment")]
        public static string NameBasedRoleAssignmentText = "Name-Based Role Assignment";

        [Localized("AdvancedRoleAssignment")]
        public static string AdvancedRoleAssignment = "Advanced Role Assignment";
    }
}