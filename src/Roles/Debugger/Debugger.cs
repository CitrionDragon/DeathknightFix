using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Player;
using Lotus.Options;
using Lotus.Roles.Internals.Attributes;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles.Internals.Enums;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities.Attributes;

namespace Lotus.Roles.Debugger;

[LoadStatic]
public class Debugger: CustomRole
{
    private RoleTypes baseRole;
    private bool customSyncOptions;
    private HideAndSeekTimerBar timerBar;
    private int counter = 1;

    private Component progressTracker;



    [UIComponent(UI.Name)]
    public static string TestMeetingText()
    {
        return "<size=1>ABC\nDEF\nGHI\nJKL\nMNO</size>";
    }

    [RoleAction(LotusActionType.OnPet)]
    private void OnPet()
    {
        VentLogger.Old("OnPet Called", "DebuggerCall");
        LogStats();
        counter++;
        TestTest();
    }

    private void CustomWinTest()
    {
        ManualWin manualWin = new(new List<PlayerControl> { MyPlayer }, ReasonType.RoleSpecificWin);
        manualWin.Activate();
    }

    private void RangeTest()
    {
        Vector2 location = MyPlayer.GetTruePosition();
        foreach (PlayerControl player in Players.GetPlayers(PlayerFilter.Alive).Where(p => p.PlayerId != MyPlayer.PlayerId))
            VentLogger.Old($"Distance from {MyPlayer.name} to {player.name} :: {Vector2.Distance(location, player.GetTruePosition())}", "DebuggerDistance");
    }

    private void TestTest()
    {
        MyPlayer.RpcSetRole(RoleTypes.Impostor);
    }

    private void LogStats()
    {
        VentLogger.Old($"{MyPlayer.GetNameWithRole()} | Dead? {MyPlayer.Data.IsDead} | AURole: {MyPlayer.Data.Role.name} | Custom Role: {MyPlayer.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {MyPlayer.GetSubrole()?.RoleName}", "DebuggerStats");
        VentLogger.Old($"Stats | Total Players: {Players.GetPlayers().Count()} | Alive Players: {Players.GetPlayers(PlayerFilter.Alive).Count()}", "DebuggerStats");
        VentLogger.Old("-=-=-=-=-=-=-=-=-=-=-=-= Other Players =-=-=-=-=-=-=-=-=-=-=-=-", "DebuggerStats");
        foreach (PlayerControl player in Players.GetPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId))
            VentLogger.Old($"{player.GetNameWithRole()} | Dead? {player.Data.IsDead} | AURole: {player.Data.Role.name} | Custom Role: {player.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {player.GetSubrole()?.RoleName}", "DebuggerStats");

        VentLogger.Old("-=-=-=-=-=-=-=-= End Of Debugger =-=-=-=-=-=-=-=-", "DebuggerStats");
    }


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.HiddenTab)
            .Name("<b><color=#FF0000>D</color><color=#FFBF00>e</color><color=#7FFF00>b</color><color=#00FF3F>u</color><color=#00FEFF>g</color><color=#003FFF>g</color><color=#7F00FF>e</color><color=#FF00BF>r</color></b>")
            .SubOption(sub => sub
                .Name("Base Role")
                .Bind(v => baseRole = (RoleTypes)Convert.ToUInt16(v))
                .Value(v => v.Text("Crewmate").Value(0).Build())
                .Value(v => v.Text("Impostor").Value(1).Build())
                .Value(v => v.Text("Scientist").Value(2).Build())
                .Value(v => v.Text("Engineer").Value(3).Build())
                .Value(v => v.Text("GuardianAngel").Value(4).Build())
                .Value(v => v.Text("Shapeshifter").Value(5).Build())
                .Value(v => v.Text("CrewmateGhost").Value(6).Build())
                .Value(v => v.Text("ImpostorGhost").Value(7).Build())
                .Build())
            .SubOption(sub => sub
                .Name("Use Custom Sync Options")
                .BindBool(v => customSyncOptions = v)
                .AddOnOffValues(false)
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor(new Color(0.84f, 1f, 0.64f))
            .VanillaRole(RoleTypes.Crewmate)
            .RoleFlags(RoleFlag.Hidden | RoleFlag.Unassignable | RoleFlag.DontRegisterOptions);

}