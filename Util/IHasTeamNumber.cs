using Sandbox;

namespace Amper.FPS;

/// <summary>
/// This class can have a team.
/// </summary>
public interface IHasTeamNumber
{
	public int TeamNumber { get; }
}

public static class TeamExtensions
{
	public static bool IsSameTeam( this IHasTeamNumber me, Entity them )
	{
		// Entities without an assigned team are automatically our enemies.
		if ( them is not IHasTeamNumber teamOne )
			return false;

		return true;
	}

	public static bool IsOtherTeam( this IHasTeamNumber me, Entity them )
	{
		return !me.IsSameTeam( them );
	}

	public static bool IsFriend( this IHasTeamNumber me, Entity them )
	{
		return me != them && me.IsSameTeam( them );
	}

	public static bool IsEnemy( this IHasTeamNumber me, Entity them )
	{
		return me != them && me.IsOtherTeam( them );
	}
}
