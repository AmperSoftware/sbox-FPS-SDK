using Sandbox;
using System.Collections.Generic;

namespace Amper.FPS;

partial class GameRules
{
	[Net] public IDictionary<int, int> Score { get; set; }
	[Net] public int TotalRoundsPlayed { get; protected set; }

	public void RestartGame()
	{
		Score.Clear();
		TotalRoundsPlayed = 0;

		OnGameRestart();
		RestartRound();
	}

	public virtual void OnGameRestart() { }

	[ConCmd.Admin( "mp_restartgame" )]
	public static void Command_RestartGame()
	{
		Current?.RestartGame();
	}

	[ConVar.Replicated] public static int mp_maxrounds { get; set; } = 0;
}
