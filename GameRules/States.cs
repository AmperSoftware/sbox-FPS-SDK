using Sandbox;
using Sandbox.Diagnostics;
using System;

namespace Amper.FPS;

partial class GameRules
{
	[Net] public TimeSince TimeSinceStateChange { get; private set; }
	[Net] public BaseGameState State { get; private set; }
	BaseGameState LastState { get; set; }

	public virtual void ChangeStateTo( BaseGameState state )
	{
		Assert.NotNull( state );
		State = state;
	}

	void UpdateState()
	{
		if ( LastState != State )
		{
			// Invoke ended callback on our previous state, if exists.
			LastState?.OnEnd();

			// Invoke start callback on our new state
			State?.OnStart();
		}

		// Invoke update on the state if needed.
		State?.Update();
	}
}

[Obsolete]
public enum EGameState
{
	Initialized,
	PreGame,
	ReadyUp,
	PreRound,
	Gameplay,
	RoundEnd,
	GameOver
}
