using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using Lotus.API.Odyssey;
using Lotus.Chat;
using Lotus.Roles;
using Lotus.Extensions;
using Lotus.Managers.Templates.Models;
using Lotus.Roles.Interfaces;
using VentLib.Options;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.Templates;

public class TemplateLegacy
{
    private static readonly Regex Regex = new("(?:\\$|@|%|\\^)((?:[A-Za-z0-9_]|\\.\\S)*)");

    public string Text;
    public string? Tag;

    private TemplateLegacy()
    {
    }

    public TemplateLegacy(string text)
    {
        Text = text.Replace("\\n", "\n");
        text = text.Replace("\\|", "\\?");
        if (!text.Contains(" | ")) return;

        string[] split = text.Split(" | ");
        Tag = split[0];
        Text = split[1].Replace("\\?", "|");
    }

    public static TemplateLegacy FromTOH(string text)
    {
        string[] split = text.Split(":");
        string tag = split[0];
        string t = split[1..].Fuse(":");
        return new TemplateLegacy { Text = t, Tag = tag };
    }

    public Template ConvertToTemplate()
    {
        return new Template { Text = Text, Tag = Tag };
    }

    public string Format(object obj)
    {
        return Regex.Replace(Text, match =>
        {
            var split = match.Value.Split(".");
            if (split.Length > 1)
                return VariableValues.TryGetValue(split[0], out var dynSupplier)
                    ? dynSupplier(obj, split[1])
                    : match.Value;
            return TemplateValues.TryGetValue(match.Value, out var funcSupplier)
                ? funcSupplier(obj)
                : match.Value;
        });
    }

    private static readonly Dictionary<string, Func<object, String>> TemplateValues = new()
    {
        { "$RoomCode", _ => GameCode.IntToGameName(AmongUsClient.Instance.GameId) },
        { "$Host", _ => DataManager.Player.Customization.name },
        { "$AUVersion", _ => UnityEngine.Application.version },
        { "$ModVersion", _ => ProjectLotus.PluginVersion + (ProjectLotus.DevVersion ? " " + ProjectLotus.DevVersionStr : "") },
        { "$Map", _ => Constants.MapNames[GameOptionsManager.Instance.CurrentGameOptions.MapId] },
        { "$Gamemode", _ => Game.CurrentGamemode.Name },
        { "$Date", _ => DateTime.Now.ToShortDateString() },
        { "$Time", _ => DateTime.Now.ToShortTimeString() },
        { "$Players", _ => PlayerControl.AllPlayerControls.ToArray().Select(p => p.name).Join() },
        { "$PlayerCount", _ => PlayerControl.AllPlayerControls.Count.ToString() },
        { "@Name", player => ((PlayerControl) player).name },
        { "@Color", player => ModConstants.ColorNames[((PlayerControl) player).cosmetics.bodyMatProperties.ColorId] },
        { "@Role", player =>
            {
                CustomRole role = ((PlayerControl)player).GetCustomRole();
                return role.RoleColor.Colorize(role.RoleName);
            }
        },
        { "@Blurb", player => ((PlayerControl) player).GetCustomRole().Blurb },
        { "@Description", player => ((PlayerControl) player).GetCustomRole().Description },
        { "@Options", player => OptionUtils.OptionText(((PlayerControl) player).GetCustomRole().RoleOptions) },
        { "@Faction", player => ((PlayerControl) player).GetCustomRole().Faction.Name() },
        { "@Subroles", player => ((PlayerControl) player).GetSubroles().Select(r => r.RoleColor.Colorize(r.RoleName)).Fuse() },
        { "@Modifiers", player => ((PlayerControl) player).GetSubroles().Select(r => r.RoleColor.Colorize(r.RoleName)).Fuse() },
        { "@Mods", player => ((PlayerControl) player).GetSubroles().Select(r => r.RoleColor.Colorize(r.RoleName)).Fuse() },
        { "@ModsDescriptive", player => ModifierText((PlayerControl) player) },
        { "^Role_Name", role => ((CustomRole) role).RoleName },
        { "^Role_Description", role => ((CustomRole) role).Description },
        { "^Role_Blurb", role => ((CustomRole) role).Blurb },
        { "^Role_Options", role => OptionUtils.OptionText(((CustomRole) role).RoleOptions) },
        { "^Role_Faction", role => ((CustomRole) role).Faction.Name() },
        { "^Role_Basis", role => ((CustomRole) role).RealRole.ToString() }
    };

    public static readonly Dictionary<string, string> TemplateVariables = new()
    {
        { "RoomCode", "The current room code." },
        { "Host", "The host's name." },
        { "AUVersion", "The current version of Among Us." },
        { "ModVersion", "The current mod version." },
        { "ModName", "The current mod name." },
        { "Map", "The current map name." },
        { "Gamemode", "The current gamemode name." },
        { "Date", "The current date (based on the host)." },
        { "Time", "The current time (based on the host)." },
        { "Players", "A list of all player names separated by a comma." },
        { "PlayerCount", "A count of all players currently in the lobby." },
        { "AllRoles", "A categorized list of all roles in the mod." },
        { "AllModifiers", "A list of all modifiers in the mod." },
        { "MyRole", "Echoes the text of /myrole which shows the player's role and role description." },

        { "Name", "The player's name." },
        { "Color", "The player's color." },
        { "Role", "The player's role." },
        { "Blurb", "The player's role blurb." },
        { "Description", "The player's role description." },
        { "Death", "The player's cause of death." },
        { "Options", "The player's role options." },
        { "Faction", "The player's faction." },
        { "Modifiers", "Identical to @Subroles, shows the player's modifiers as a list of names" },
        { "ModsDescriptive", "Uses the modifier-info template to display descriptive info about each of a player's modifiers" },
    };

    private static readonly Dictionary<string, Func<object, string, string>> VariableValues = new()
    {
        { "%Option", (_, qualifier) => OptionManager.GetManager().GetOption(qualifier)?.GetValueText() ?? "Unknown Option" },
    };

    private static string ModifierText(PlayerControl player)
    {
        if (PluginDataManager.TemplateManager.HasTemplate("modifier-info"))
        {
            return player.GetSubroles().Select(sr => !PluginDataManager.TemplateManager.TryFormat(sr, "modifier-info", out string text) ? "" : text).Fuse("\n\n");
        }

        return player.GetSubroles().Select(sr =>
        {
            string identifierText = sr is ISubrole subrole ? sr.RoleColor.Colorize(subrole.Identifier() ?? "") + " " : "";
            return $"{identifierText}{sr.RoleColor.Colorize(sr.RoleName)}\n{sr.Description}";
        }).Fuse("\n\n");
    }

}