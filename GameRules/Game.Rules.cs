using Sandbox;
using System.Linq;

namespace Amper.FPS;

partial class SDKGame
{
	#region Teams
	public virtual bool AreTeamChangesAllowed() => State != GameState.GameOver;
	/// <summary>
	/// Is this player allowed to change their team?
	/// </summary>
	public virtual bool CanPlayerChangeTeams( SDKPlayer player ) => true;
	/// <summary>
	/// Is anyone allowed to change their team from this one?
	/// </summary>
	public virtual bool CanChangeTeamFrom( int currentTeam ) => true;
	/// <summary>
	/// Is anyone allowed to change their team to this one?
	/// </summary>
	public virtual bool CanChangeTeamTo( int newTeam ) => TeamManager.IsJoinable( newTeam );

	/// <summary>
	/// Can this player change their team to the new team?
	/// </summary>
	public virtual bool CanPlayerChangeTeamTo( SDKPlayer player, int newTeam )
	{
		// We're already on this team.
		if ( player.TeamNumber == newTeam )
			return false;

		if ( !AreTeamChangesAllowed() )
			return false;

		// This player is not allowed to change their team.
		if ( !CanPlayerChangeTeams( player ) )
			return false;

		// Changing teams from this one is not allowed.
		if ( !CanChangeTeamFrom( player.TeamNumber ) )
			return false;

		// Changing teams to this one is not allowed.
		if ( !CanChangeTeamTo( newTeam ) )
			return false;

		return true;
	}

	/// <summary>
	/// Override the team that the player will join on request. This is done AFTER all the checks to join a team have passed. 
	/// The new team is NOT being checked.
	/// </summary>
	public virtual int GetTeamAssignmentOverride( SDKPlayer player, int team, bool autoBalance ) => team;

	#endregion

	#region Damage

	/// <summary>
	/// How much damage we should take for landing with this amount of velocity.
	/// </summary>
	public virtual float GetPlayerFallDamage( SDKPlayer player, float velocity )
	{
		var damage = velocity - player.MaxSafeFallSpeed;
		return damage * player.DamageForFallSpeed;
	}

	public virtual bool CanEntityTakeDamage( Entity victim, Entity attacker, ExtendedDamageInfo info )
	{
		// Dead things can't take damage.
		if ( victim.LifeState != LifeState.Alive )
			return false;

		// Attacker can always damage themselves.
		if ( attacker == victim )
			return true;

		// If friendly fire is turned off, then we can't damage teammates.
		if ( !mp_friendly_fire )
		{
			if ( ITeam.IsSame( victim, attacker ) )
				return false;
		}

		return true;
	}

	[ConVar.Replicated] public static bool mp_friendly_fire { get; set; }

	/// <summary>
	/// Global damage multiplicator.
	/// </summary>
	public virtual float GetDamageMultiplier() => 1;
	/// <summary>
	/// The fraction of the base damage that objects will receive on the edge of the explosion. 50% by default.
	/// </summary>
	public virtual float GetRadiusDamageFalloff() => 0.5f;

	#endregion

	#region Respawns

	/// <summary>
	/// Are respawns allowed right now in general?
	/// </summary>
	public virtual bool AreRespawnsAllowed()
	{
		// always allow respawns on waiting for players
		if ( IsWaitingForPlayers )
			return true;

		// no respawns in round end section, if you die - you die.
		if ( IsRoundEnded )
			return false;

		// if round is not yet active, we can respawn all we want.
		if ( !IsRoundActive )
			return true;

		if ( TeamWipeCausesRoundEnd() )
			return false;

		return true;
	}

	/// <summary>
	/// Can this team respawn?
	/// </summary>
	public virtual bool CanTeamRespawn( int team ) => true;

	/// <summary>
	/// Can this player respawn right now?
	/// </summary>
	public virtual bool CanPlayerRespawn( SDKPlayer player )
	{
		if ( !TeamManager.IsPlayable( player.TeamNumber ) ) 
			return false;

		return true;
	}

	#endregion

	#region Gamemodes Logic

	/// <summary>
	/// Will eliminating the entire enemy team cause the round to end?
	/// </summary>
	public virtual bool TeamWipeCausesRoundEnd() => false;
	
	// Do we have enough players to start the round?
	public virtual bool IsEnoughPlayersToStartRound()
	{
		foreach ( var pair in TeamManager.Teams )
		{
			// not enough members in one team
			if ( !IsEnoughPlayersInTeamToStartRound( pair.Key ) )
				return false;
		}
		return true;
	}

	public virtual bool IsEnoughPlayersInTeamToStartRound( int team )
	{
		if ( TeamWipeCausesRoundEnd() )
		{
			// In gamemodes where team wipe causes round end, in each playable
			// team there should be at least one player.
			if ( TeamManager.IsPlayable( team ) )
			{
				if ( TeamManager.GetPlayers( team ).Count() < 1 )
					return false;
			}
		}

		return true;
	}

	#endregion

	public virtual float GetGravityMultiplier() => 1;
	public virtual bool AllowThirdPersonCamera() => false;

	/// <summary>
	/// Can all players use weapons?
	/// </summary>
	public virtual bool CanWeaponsAttack() => true;

}
