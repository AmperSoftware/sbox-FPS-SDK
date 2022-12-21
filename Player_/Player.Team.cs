using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer : ITeam
{
	[Net] public int TeamNumber { get; set; }
	[Net] public int TeamChanges { get; set; }

	public virtual bool ChangeTeam( int team, bool autoTeam, bool silent, bool autobalance = false )
	{
		Game.AssertServer();

		// Desired team doesn't exist, don't bother changing to it.
		if ( !TeamManager.TeamExists( team ) )
			return false;

		// The player is not allowed to change their team right now.
		if ( !SDKGame.Current.CanPlayerChangeTeamTo( this, team ) )
			return false;

		// see if gamemode wants to override our team with something else.
		team = SDKGame.Current.GetTeamAssignmentOverride( this, team, autobalance );

		// cant change team if we're already on this team.
		if ( TeamNumber == team ) 
			return false;

		TeamNumber = team;
		TeamChanges++;

		// die if we're alive
		CommitSuicide( false );

		// Enter observer mode if the team we just joined is not playable.
		if ( !TeamManager.IsPlayable( TeamNumber ) ) 
		{
			StartObserverMode( LastObserverMode );
		}

		AttemptRespawn();
		SDKGame.Current.PlayerChangeTeam( this, team );
		return true;
	}
}
