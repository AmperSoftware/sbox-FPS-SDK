using Sandbox;
using Sandbox.Diagnostics;
using System;

namespace Amper.FPS;

partial class GameRules
{
	[Net] public TimeSince TimeSinceStateChange { get; set; }
	[Net] public BaseGameState State { get; set; }
	BaseGameState LastState { get; set; }

	public virtual void ChangeStateTo( BaseGameState state )
	{
		Assert.NotNull( state );
		State = state;
	}

	public virtual void SimulatePreGame()
	{
		// If we have players that are ready to play, 
		// start waiting for players timer.
		if ( HasPlayers() && WaitingForPlayersEnabled() ) 
		{
			StartWaitingForPlayers();
		}
	}

	public virtual void StartedPreGame()
	{
		EventDispatcher.InvokeEvent<GameRestartEvent>();
	}

	public virtual void EndedPreGame() { }

	//
	// ReadyUp
	//

	public virtual void SimulateReadyUp() { }
	public virtual void StartedReadyUp() { }
	public virtual void EndedReadyUp() { }

	//
	// PreRound
	//

	public virtual void SimulatePreRound() 
	{
		if ( TimeSinceStateChange > GetPreRoundFreezeTime() )
		{
			TransitionToState( EGameState.Gameplay );
		}
	}

	public virtual void StartedPreRound()
	{
		EventDispatcher.InvokeEvent<RoundRestartEvent>();
	}

	public virtual void EndedPreRound() { }
	public virtual float GetPreRoundFreezeTime() { return 6; }

	//
	// Gameplay
	//

	public virtual void StartedGameplay()
	{
		EventDispatcher.InvokeEvent<RoundActiveEvent>();
	}

	public virtual void SimulateGameplay() { }
	public virtual void EndedGameplay() { }

	//
	// TeamWin
	//

	public virtual void SimulateRoundEnd()
	{
		if ( TimeSinceStateChange > mp_chattime )
			RestartRound();
	}

	public virtual void StartedRoundEnd()
	{
		EventDispatcher.InvokeEvent<RoundEndEvent>();
	}

	public virtual void EndedRoundEnd() { }

	//
	// GameOver
	//

	public virtual void StartedGameOver()
	{
		EventDispatcher.InvokeEvent<GameOverEvent>();
	}

	void InvokeStateUpdate( BaseGameState state )
	{
	}

	void InvokeStateStarted( BaseGameState state )
	{
	}

	void InvokeStateEnded( BaseGameState state )
	{
	}
}

[Obsolete]
public enum EGameState
{
	/// <summary>
	/// The game was just initialized.
	/// </summary>
	Initialized,
	/// <summary>
	/// No players have yet joined the game. We are waiting for someone to join.
	/// </summary>
	PreGame,
	/// <summary>
	/// The game is about to start, we are waiting for players to get ready
	/// </summary>
	ReadyUp,
	/// <summary>
	/// The round is about to start, we make a short freezetime before the game is on.
	/// </summary>
	PreRound,
	/// <summary>
	/// Round is on, playing normally.
	/// </summary>
	Gameplay,
	/// <summary>
	/// The round has ended
	/// </summary>
	RoundEnd,
	/// <summary>
	/// Game is over, showing the scoreboard etc
	/// </summary>
	GameOver
}
