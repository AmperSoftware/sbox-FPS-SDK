using Sandbox;

namespace Amper.FPS;

partial class SDKGame
{
	/// <summary>
	/// This team has won the round! This will be Unassigned any other time.
	/// </summary>
	[Net] public int Winner { get; set; }
	[Net] public int WinReason { get; set; }

	/// <summary>
	/// This declares the winner and enters the round in humiliation mode?
	/// </summary>
	public void DeclareWinner( int winner, int reason )
	{
		// If we're already in humiliation, don't do anything.
		if ( State == GameState.RoundEnd ) return;
		TransitionToState( GameState.RoundEnd );

		Winner = winner;
		WinReason = reason;

		if ( !Score.ContainsKey( winner ) ) Score[winner] = 0;
		Score[winner]++;

		OnTeamWin( winner );

		// play lose song for all opponents
		foreach ( var index in TeamManager.Teams.Keys )
		{
			if ( index == winner ) 
				continue;

			if ( !TeamManager.IsPlayable( index ) )
				continue;

			OnTeamLose( index );
		}

		// TODO: For spectators the sound depends on who the user last spectated.
	}

	public virtual void OnTeamWin( int team ) { PlayTeamWinSong( team ); }
	public virtual void OnTeamLose( int team ) { PlayTeamLoseSong( team ); }

	public virtual void PlayTeamWinSong( int team ) { }
	public virtual void PlayTeamLoseSong( int team ) { }

	[ConCmd.Server( "mp_forceteamwin" )]
	public static void Command_ForceTeamWin( int team )
	{
		Current.DeclareWinner( team, 0 );
	}

	[ConVar.Replicated] public static float mp_chattime { get; set; } = 15f;
}
