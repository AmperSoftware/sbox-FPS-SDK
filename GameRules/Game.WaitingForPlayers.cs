using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKGame
{
	[Net] public bool IsWaitingForPlayers { get; set; }
	public TimeSince TimeSinceWaitingForPlayersStart { get; set; }
	public float TimeUntilWaitingForPlayersEnds => MathF.Max( 0, mp_waiting_for_players_time - TimeSinceWaitingForPlayersStart );
	public float WaitingForPlayersTime { get; set; }

	public virtual bool WaitingForPlayersEnabled() => false;

	public virtual void CheckWaitingForPlayers()
	{
		if ( IsWaitingForPlayers )
		{
			if ( mp_waiting_for_players_cancel )
				mp_waiting_for_players_cancel = false;
			else if ( TimeSinceWaitingForPlayersStart <= WaitingForPlayersTime || !IsEnoughPlayersToStartRound() )
				return;

			StopWaitingForPlayers();
			// Restart round immediately.
			RestartRound();
		}
	}

	public void StartWaitingForPlayers()
	{
		if ( !Game.IsServer )
			return;

		if ( IsWaitingForPlayers )
			return;

		IsWaitingForPlayers = true;
		TimeSinceWaitingForPlayersStart = 0;
		WaitingForPlayersTime = mp_waiting_for_players_time;

		OnWaitingForPlayersStarted();
	}

	public void StopWaitingForPlayers()
	{
		if ( !Game.IsServer )
			return;

		if ( !IsWaitingForPlayers )
			return;

		IsWaitingForPlayers = false;
		OnWaitingForPlayersEnded();
	}

	public virtual void OnWaitingForPlayersStarted() { }
	public virtual void OnWaitingForPlayersEnded() { }

	[ConVar.Replicated] public static bool mp_waiting_for_players_cancel { get; set; }
	[ConVar.Replicated] public static bool mp_waiting_for_players_restart { get; set; }
	[ConVar.Replicated] public static float mp_waiting_for_players_time { get; set; } = 30;
}
