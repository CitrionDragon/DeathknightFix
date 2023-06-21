using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Managers.History.Events;
using Lotus.Roles;
using Lotus.Extensions;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers.History;

public class PlayerHistory
{
    public byte PlayerId;
    public UniquePlayerId UniquePlayerId;
    public string Name;
    public string ColorName;
    public CustomRole Role;
    public List<CustomRole> Subroles;
    public uint Level;
    public GameData.PlayerOutfit Outfit;
    public ulong GameID;
    public IDeathEvent? CauseOfDeath;
    public PlayerStatus Status;

    public PlayerHistory(FrozenPlayer frozenPlayer)
    {
        PlayerId = frozenPlayer.PlayerId;
        Name = frozenPlayer.Name;
        ColorName = frozenPlayer.ColorName;
        Role = frozenPlayer.Role;
        Subroles = frozenPlayer.Subroles;
        UniquePlayerId = UniquePlayerId.FromFriendCode(frozenPlayer.FriendCode);
        Level = frozenPlayer.Level;
        Outfit = frozenPlayer.Outfit;
        GameID = frozenPlayer.GameID;
        CauseOfDeath = frozenPlayer.CauseOfDeath;
        if (frozenPlayer.NullablePlayer != null && frozenPlayer.NullablePlayer.IsAlive()) Status = PlayerStatus.Alive;
        else if (frozenPlayer.NullablePlayer == null || frozenPlayer.NullablePlayer.Data.Disconnected) Status = PlayerStatus.Disconnected;
        else Status = Game.MatchData.GameHistory.Events
                .FirstOrOptional(ev => ev is ExiledEvent exiledEvent && exiledEvent.Player().PlayerId == PlayerId)
                .Map(_ => PlayerStatus.Exiled)
                .OrElse(PlayerStatus.Dead);
    }
}

public enum PlayerStatus
{
    Alive,
    Exiled,
    Dead,
    Disconnected
}