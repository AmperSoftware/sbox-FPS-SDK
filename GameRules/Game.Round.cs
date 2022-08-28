using Sandbox;
using System.Linq;

namespace Amper.FPS;

partial class GameRules
{
	public bool IsRoundStarting => State == GameState.PreRound;
	public bool IsRoundActive => State == GameState.Gameplay;
	public bool IsRoundEnded => State == GameState.RoundEnd;

	/// <summary>
	/// Restart the round.
	/// </summary>
	public void RestartRound()
	{
		if ( !IsServer ) 
			return;

		IsWaitingForPlayers = false;

		ResetObjectives();
		ClearMap();
		RespawnPlayers( true );

		// Reset the winner.
		Winner = 0;
		WinReason = 0;

		if ( !IsEnoughPlayersToStartRound() )
		{
			StartWaitingForPlayers();
		}

		TransitionToState( GameState.PreRound );
		OnRoundRestart();
	}

	public virtual void OnRoundRestart() { }

	public virtual void CalculateObjectives() { }
	public virtual void ResetObjectives() { }


	[ConCmd.Server( "mp_restartround" )]
	public static void Command_RestartRound()
	{
		Current?.RestartRound();
	}

	public virtual void ClearMap()
	{
		// reset all the doors to their initial state.
		var doors = All.OfType<DoorEntity>();
		foreach ( var door in doors ) door.Reset();

		Map.Reset( MapCleanUpFilter );
	}

	public virtual bool MapCleanUpFilter( string classname, Entity entity )
	{
		if ( entity is Source1Weapon weapon && weapon.RemoveOnRoundRestart() )
			return true;

		return false;
	}
}
