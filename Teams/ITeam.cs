using Sandbox;

namespace Amper.FPS;

/// <summary>
/// This class can have a team.
/// </summary>
public interface ITeam
{
	public int TeamNumber { get; }

	/// <summary>
	/// Returns true only if both entities are team entities, and both are in the same team. 
	/// </summary>
	static public bool IsSame( IEntity one, IEntity two )
	{
		if ( one is not ITeam teamOne ) return false;
		if ( two is not ITeam teamTwo ) return false;

		return teamOne.TeamNumber == teamTwo.TeamNumber;
	}

	public static bool IsTeammate( Entity one, Entity two )
	{
		return one != two && IsSame( one, two );
	}

	public static bool IsEnemy( Entity one, Entity two )
	{
		return one != two && !IsSame( one, two );
	}
}
