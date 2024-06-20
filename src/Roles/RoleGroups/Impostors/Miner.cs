using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Miner : Impostor
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Miner));
    [UIComponent(UI.Cooldown)]
    private Cooldown minerAbilityCooldown;
    private Vector2 lastEnteredVentLocation = Vector2.zero;

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(LotusActionType.VentEntered)]
    private void EnterVent(Vent vent)
    {
        lastEnteredVentLocation = vent.transform.position;
    }

    [RoleAction(LotusActionType.OnPet)]
    public void MinerVentAction()
    {
        if (minerAbilityCooldown.NotReady()) return;
        minerAbilityCooldown.Start();

        if (lastEnteredVentLocation == Vector2.zero) return;
        log.Trace($"{MyPlayer.Data.PlayerName}:{lastEnteredVentLocation}", "MinerTeleport");
        Utils.Teleport(MyPlayer.NetTransform, new Vector2(lastEnteredVentLocation.x, lastEnteredVentLocation.y + 0.3636f));
    }


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).SubOption(sub =>
            sub.Name("Miner Ability Cooldown")
                .BindFloat(minerAbilityCooldown.SetDuration)
                .AddFloatRange(5, 50, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build());
}