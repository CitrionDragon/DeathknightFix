using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Roles.Overrides;
using Lotus.Victory;
using Lotus.Roles.Managers.Interfaces;
using VentLib.Options.Game;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes;

public interface IGameMode
{
    public string Name { get; set; }
    public CoroutineManager CoroutineManager { get; }
    public MatchData MatchData { get; protected internal set; }
    public RoleOperations RoleOperations { get; }
    public Roles.Managers.RoleManager RoleManager { get; }

    IEnumerable<GameOptionTab> EnabledTabs();

    void Assign(PlayerControl player, CustomRole role);
    void AssignRoles(List<PlayerControl> players);

    protected internal void Activate();

    protected internal void Deactivate();

    protected internal void FixedUpdate();

    protected internal void Setup();

    protected internal void SetupWinConditions(WinDelegate winDelegate);

    Remote<GameOptionOverride> AddOverride(byte playerId, GameOptionOverride optionOverride) => MatchData.Roles.AddOverride(playerId, optionOverride);

    internal void InternalActivate()
    {
        Activate();
        EnabledTabs().ForEach(GameOptionController.AddTab);
    }

    internal void InternalDeactivate()
    {
        Deactivate();
        EnabledTabs().ForEach(GameOptionController.RemoveTab);
    }

    void Trigger(LotusActionType action, ActionHandle handle, params object[] arguments);
}