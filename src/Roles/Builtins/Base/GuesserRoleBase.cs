﻿using System;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Vanilla.Meetings;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Patches.Meetings;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Trackers;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Victory;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.Builtins.Base;

public abstract class GuesserRoleBase : CustomRole
{
    private static Color guesserColor = new(0.83f, 1f, 0.42f);
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GuesserRoleBase));

    private MeetingPlayerSelector voteSelector = new();

    private int guessesPerMeeting;
    private bool hasMadeGuess;
    private byte guessingPlayer = byte.MaxValue;
    private bool skippedVote;
    private CustomRole? guessedRole;
    private int guessesThisMeeting;

    protected int CorrectGuesses;

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath)]
    public void ResetPreppedPlayer()
    {
        hasMadeGuess = false;
        voteSelector.Reset();
        guessingPlayer = byte.MaxValue;
        skippedVote = false;
        guessedRole = null;
        guessesThisMeeting = 0;
    }

    [RoleAction(LotusActionType.Vote)]
    public void SelectPlayerToGuess(Optional<PlayerControl> player, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        if (skippedVote || hasMadeGuess) return;
        handle.Cancel();
        VoteResult result = voteSelector.CastVote(player);
        switch (result.VoteResultType)
        {
            case VoteResultType.None:
                break;
            case VoteResultType.Skipped:
                skippedVote = true;
                break;
            case VoteResultType.Selected:
                guessingPlayer = result.Selected;
                log.Trace($"Guesser selected: {Players.FindPlayerById(guessingPlayer)?.name}", "GuesserSelect");
                GuesserHandler(Translations.PickedPlayerText.Formatted(Players.FindPlayerById(result.Selected)?.name)).Send(MyPlayer);
                break;
            case VoteResultType.Confirmed:
                log.Trace("Guesser confirmed their selection.", "GuesserSelect");
                if (guessedRole == null)
                {
                    log.Trace($"Confirmed selection, but no guessed role, resetting guess", "GuesserSelect");
                    voteSelector.Reset();
                    voteSelector.CastVote(player);
                }
                else hasMadeGuess = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!hasMadeGuess) return;

        if (++guessesThisMeeting < guessesPerMeeting)
        {
            hasMadeGuess = false;
            voteSelector.Reset();
        }

        PlayerControl? guessed = Players.FindPlayerById(guessingPlayer);
        if (guessed == null || guessedRole == null)
        {
            GuesserHandler(Translations.ErrorCompletingGuess).Send(MyPlayer);
            ResetPreppedPlayer();
            return;
        }

        PlayerControl dyingPlayer = MyPlayer;
        if (guessed.PrimaryRole().GetType() == guessedRole.GetType())
        {
            MyPlayer.InteractWith(guessed, new UnblockedInteraction(new FatalIntent(), (CustomRole)null));
            GuesserHandler(Translations.GuessAnnouncementMessage.Formatted(guessed.name)).Send();
            CorrectGuesses++;
            dyingPlayer = guessed;
        }
        else HandleBadGuess();
        meetingDelegate.CurrentVotes().ToArray()
            .Select(cv => (cv.Key, cv.Value.Filter().Where(v => dyingPlayer.PlayerId == v).ToArray()))
            .Where(cv => !cv.Item2.IsEmpty())
            .ForEach(cv =>
            {
                cv.Item2.ForEach(vote => meetingDelegate.RemoveVote(cv.Key, new Optional<byte>(vote)));
                Players.PlayerById(cv.Key).IfPresent(p => CastVotePatch.ClearVote(MeetingHud.Instance, p));
            });
        CheckEndGamePatch.ForceCheckEndGame();
    }

    protected virtual void HandleBadGuess()
    {
        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), (CustomRole)null));
        GuesserHandler(Translations.GuessAnnouncementMessage.Formatted(MyPlayer.name)).Send();
    }

    [RoleAction(LotusActionType.Chat)]
    public void DoGuesserVoting(PlayerControl player, string message, GameState state)
    {
        DevLogger.Log($"Message: {message} | Guessing player: {guessingPlayer}");
        if (state is not GameState.InMeeting) return;
        if (player.PlayerId != MyPlayer.PlayerId) return;
        if (guessingPlayer == byte.MaxValue) return;
        if (!(message.StartsWith("/role") || message.StartsWith("/r"))) return;
        string[] split = message.Replace("/role", "/r").Split(" ");
        if (split.Length == 1)
        {
            GuesserHandler(Translations.TypeRText).Send(MyPlayer);
            return;
        }

        string roleName = split[1..].Fuse(" ");
        CustomRole? role = IRoleManager.Current.AllCustomRoles().FirstOrOptional(r => string.Equals(r.RoleName, roleName, StringComparison.CurrentCultureIgnoreCase))
            .CoalesceEmpty(() => IRoleManager.Current.AllCustomRoles().FirstOrOptional(r => r.RoleName.ToLower().Contains(roleName.ToLower())))
            .OrElse(null!);
        if (role == null!)
        {
            GuesserHandler(Translations.UnknownRole.Formatted(roleName)).Send(MyPlayer);
            return;
        }

        guessedRole = role;

        GuesserHandler(Translations.PickedRoleText.Formatted(Players.FindPlayerById(guessingPlayer)?.name, guessedRole.RoleName)).Send(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Guesses per Meeting", Translations.Options.GuesserPerMeeting)
                .AddIntRange(1, 10, 1, 0)
                .BindInt(i => guessesPerMeeting = i)
                .Build());

    protected ChatHandler GuesserHandler(string message) => ChatHandler.Of(message, guesserColor.Colorize(Translations.GuesserTitle)).LeftAlign();

    [Localized(nameof(GuesserRoleBase))]
    private class Translations
    {
        [Localized(nameof(GuesserRoleBase))]
        public static string GuesserTitle = "Guesser";

        [Localized(nameof(PickedRoleText))]
        public static string PickedRoleText = "You are about to guess {0} as {1}. If you are certain about this, vote {0} again to finalize your guess. Otherwise you can pick another player by voting a different player.. OR pick a different role by typing /r [rolename]";

        [Localized(nameof(PickedPlayerText))]
        public static string PickedPlayerText = "You are guessing {0}'s role. To guess their role type /r [rolename].";

        [Localized(nameof(TypeRText))]
        public static string TypeRText = "Please type /r [roleName] to guess that role.";

        [Localized(nameof(UnknownRole))]
        public static string UnknownRole = "Unknown role {0}. You can use /perc to view all enabled roles.";

        [Localized(nameof(FinishedGuessingText))]
        public static string FinishedGuessingText = "You have confirmed your guess. If you are not dead, you may now vote normally.";

        [Localized(nameof(ErrorCompletingGuess))]
        public static string ErrorCompletingGuess = "Error completing guess. You may try and guess again.";

        [Localized(nameof(GuessAnnouncementMessage))]
        public static string GuessAnnouncementMessage = "The guesser has made a guess. {0} died.";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            public static string GuesserPerMeeting = "Guesses per Meeting";
        }
    }


    protected abstract override RoleModifier Modify(RoleModifier roleModifier);
}