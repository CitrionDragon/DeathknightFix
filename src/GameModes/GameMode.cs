using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Reactive.Actions;
using Lotus.Managers;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Victory;
using Lotus.Roles.Managers.Interfaces;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities;

namespace Lotus.GameModes;

public abstract class GameMode : IGameMode
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GameMode));

    protected Dictionary<LotusActionType, List<LotusAction>> LotusActions = new();

    public GameMode()
    {
        SetupRoleActions();
    }

    public abstract string Name { get; set; }

    public MatchData MatchData { get; set; } = new();
    public CoroutineManager CoroutineManager { get; } = new();
    public RoleOperations RoleOperations { get; }
    public Roles.Managers.RoleManager RoleManager { get; }
    public abstract IEnumerable<GameOptionTab> EnabledTabs();

    public virtual void Activate() { }

    public virtual void Deactivate() { }

    public virtual void FixedUpdate() { }

    public abstract void Setup();

    public abstract void Assign(PlayerControl player, CustomRole role);

    public abstract void AssignRoles(List<PlayerControl> players);

    public abstract void SetupWinConditions(WinDelegate winDelegate);

    public void Trigger(LotusActionType action, ActionHandle handle, params object[] arguments)
    {
        List<LotusAction>? actions = LotusActions.GetValueOrDefault(action);
        if (actions == null) return;

        arguments = arguments.AddToArray(handle);

        foreach (LotusAction lotusAction in actions)
        {
            if (handle.Cancellation is not (ActionHandle.CancelType.None or ActionHandle.CancelType.Soft)) return;
            lotusAction.Execute(arguments);
        }
    }

    private void SetupRoleActions()
    {
        Enum.GetValues<LotusActionType>().Do(action => this.LotusActions.Add(action, new List<LotusAction>()));
        this.GetType().GetMethods(AccessFlags.InstanceAccessFlags)
            .SelectMany(method => method.GetCustomAttributes<LotusActionAttribute>().Select(a => (a, method)))
            .Where(t => t.a.Subclassing || t.method.DeclaringType == this.GetType())
            .Select(t => new LotusAction(t.Item1, t.method))
            .Do(AddLotusAction);
    }

    private void AddLotusAction(LotusAction action)
    {
        List<LotusAction> currentActions = this.LotusActions.GetValueOrDefault(action.ActionType, new List<LotusAction>());

        log.Log(LogLevel.All, $"Registering Action {action.ActionType} => {action.Method.Name} (from: \"{action.Method.DeclaringType}\")", "RegisterAction");
        if (action.ActionType is LotusActionType.FixedUpdate &&
            currentActions.Count > 0)
            throw new ConstraintException("LotusActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

        if (action.Attribute.Subclassing || action.Method.DeclaringType == this.GetType())
            currentActions.Add(action);

        this.LotusActions[action.ActionType] = currentActions;
    }
}