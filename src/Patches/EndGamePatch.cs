using System.Collections.Generic;
using HarmonyLib;
using Lotus.API.Odyssey;
using VentLib.Logging;

namespace Lotus.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    public static Dictionary<byte, string> SummaryText = new();

    public static string KillLog = "";
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Game.State = GameState.InLobby;
        Game.Cleanup();

        SummaryText = new();
        /*foreach (var id in TOHPlugin.PlayerStates.Keys)
            SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);*/
        KillLog = ": ";/*GetString("KillLog") + ":";*/
        /*foreach (var kvp in TOHPlugin.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
        {
            var date = kvp.Value.RealKiller.Item1;
            if (date == DateTime.MinValue) continue;
            var killerId = kvp.Value.GetRealKiller();
            var targetId = kvp.Key;
            /*KillLog += $"\n{date:T} {TOHPlugin.AllPlayerNames[targetId]}({Utils.GetDisplayRoleName(targetId)}{Utils.GetSubRolesText(targetId)}) [{Utils.GetVitalText(kvp.Key)}]";#1#
            if (killerId != byte.MaxValue && killerId != targetId)
                KillLog += $"\n\t\t⇐ {TOHPlugin.AllPlayerNames[killerId]}({Utils.GetDisplayRoleName(killerId)}{Utils.GetSubRolesText(killerId)})";
        }*/

        KillLog = "asdoksdoksadpsako";
        VentLogger.Old("-----------ゲーム終了-----------", "Phase");
    }
}