using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer
{
	/// <summary>
	/// This is called before all the calculations are made. Even if the damage doesn't go through!
	/// </summary>
	public virtual void OnAttackedBy( Entity attacker, ExtendedDamageInfo info ) { }

	/// <summary>
	/// When the entity takes damage, this function gets called.
	/// Later by default it calls all the sub procedures that handles different effects that happen when
	/// player takes damage.
	/// </summary>
	/// <param name="info"></param>
	public virtual void TakeDamage( ExtendedDamageInfo info )
	{
		if ( !Game.IsServer )
			return;

		// We have been attacked by someone.
		OnAttackedBy( info.Attacker, info );

		// Check if player can receive damage from attacker.
		var attacker = info.Attacker;
		if ( !CanTakeDamage( attacker, info ) )
			return;

		// Apply damage modifications that are exclusive to the Player.
		ApplyOnPlayerDamageModifyRules( ref info );

		// Apply all global damage modifications.
		SDKGame.Current.ApplyOnDamageModifyRules( ref info, this );

		// Remember this damage as the one we taken last.
		// This is NOT networked!
		LastDamageInfo = info;
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;
		TimeSinceTakeDamage = 0;

		Health -= info.Damage;

		// We might want to avoid dying, do so.
		if ( ShouldPreventDeath( info ) )
			PreventDeath( info );

		if ( Health <= 0f )
			OnKilled();

		// Do all the reactions to this damage.
		OnTakeDamageReaction( info );

		// Make an rpc to do stuff clientside.
		TakeDamageRPC( info.Attacker, info.Weapon, info.Damage, info.Tags.ToArray(), info.HitPosition, info.Force );

		// Let SDKGame know about this.
		SDKGame.Current.PlayerHurt( this, info );
		DrawDebugDamage( info );
	}

	[ConVar.Replicated] public static bool sv_debug_take_damage { get; set; }

	private void DrawDebugDamage( ExtendedDamageInfo info )
	{
		if ( !sv_debug_take_damage )
			return;

		DebugOverlay.Sphere( info.HitPosition, 4, Color.Red, 3 );
		DebugOverlay.Sphere( info.OriginPosition, 4, Color.Green, 3 );
		DebugOverlay.Sphere( info.ReportPosition, 6, Color.Magenta, 3 );
		DebugOverlay.Line( info.OriginPosition, info.HitPosition, Color.Yellow, 3 );
	}

	[ClientRpc]
	void TakeDamageRPC( Entity attacker, Entity weapon, float damage, string[] tags, Vector3 position, Vector3 force )
	{
		OnTakeDamageEffects( attacker, weapon, damage, tags, position, force );
	}

	public virtual void OnTakeDamageEffects( Entity attacker, Entity weapon, float damage, string[] tags, Vector3 position, Vector3 force ) { }

	public bool PreventDeath( ExtendedDamageInfo info )
	{
		// We take damage, but we dont allow ourselves to die.
		if ( (Health - info.Damage) <= 0 )
		{
			Health = 1;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Check if this player is allowed to take damage from a given attacker.
	/// </summary>
	public virtual bool CanTakeDamage( Entity attacker, ExtendedDamageInfo info )
	{
		// Gods take no damage!
		if ( IsInGodMode )
			return false;

		return SDKGame.Current.CanEntityTakeDamage( this, attacker, info );
	}

	/// <summary>
	/// If mod requires us to be pushed by the damage, apply the impulse here.
	/// </summary>
	public virtual void ApplyPushFromDamage( ExtendedDamageInfo info ) { }

	/// <summary>
	/// Modify how player accepts damage.
	/// </summary>
	public virtual void ApplyOnPlayerDamageModifyRules( ref ExtendedDamageInfo info ) { }

	/// <summary>
	/// Punch player's view when they get punched.
	/// </summary>
	public virtual void ApplyViewPunchFromDamage( ExtendedDamageInfo info )
	{
		ApplyViewPunchImpulse( -2 );
	}

	public virtual bool ShouldPreventDeath( ExtendedDamageInfo info )
	{
		return IsInBuddhaMode;
	}

	/// <summary>
	/// Will this damage apply knockback on us?
	/// </summary>
	public virtual bool ShouldApplyPushFromDamage( ExtendedDamageInfo info ) => true;
	/// <summary>
	/// Will this damage punch our view?
	/// </summary>
	public virtual bool ShouldApplyViewPunchFromDamage( ExtendedDamageInfo info ) => true;
	/// <summary>
	/// Should this damage play player flinch animations?
	/// </summary>
	public virtual bool ShouldFlinchFromDamage( ExtendedDamageInfo info ) => true;
	/// <summary>
	/// Should we bleed from this type of damage?
	/// </summary>
	public virtual bool ShouldBleedFromDamage( ExtendedDamageInfo info ) => true;

	/// <summary>
	/// How will the player react to taking damage? By default this applies abs velocity to the player,
	/// kicks the view of the player and makes it flinch.
	/// </summary>
	public virtual void OnTakeDamageReaction( ExtendedDamageInfo info )
	{
		// Apply velocity to the player from the damage.
		if ( ShouldApplyPushFromDamage( info ) )
			ApplyPushFromDamage( info );

		// Apply view kick.
		if ( ShouldApplyViewPunchFromDamage( info ) )
			ApplyViewPunchFromDamage( info );

		if ( ShouldFlinchFromDamage( info ) )
			PlayFlinchFromDamage( info );

		if ( ShouldBleedFromDamage( info ) )
			SendBloodDispatchRPC( info );
	}

	/// <summary>
	/// Play flinch animation on the player from the damage type.
	/// </summary>
	public virtual void PlayFlinchFromDamage( ExtendedDamageInfo info )
	{
		// flinch the model.
		SetAnimParameter( "b_flinch", true );
	}

	/// <summary>
	/// Play bleeding particle effects on the origin with a given normal vector. Origin 
	/// is the position of the damage impact on the player's hitbox.
	/// </summary>
	public virtual void DispatchBloodEffects( Vector3 origin, Vector3 normal ) { }

	private void SendBloodDispatchRPC( ExtendedDamageInfo info )
	{
		var inflictor = info.Inflictor;
		if ( !inflictor.IsValid() )
			return;

		var inflictorPos = inflictor.WorldSpaceBounds.Center - Vector3.Up * 10;
		var dir = inflictorPos - WorldSpaceBounds.Center;
		dir = dir.Normal;

		DispatchBloodRPC( info.HitPosition, -dir );
	}

	[ClientRpc]
	private void DispatchBloodRPC( Vector3 origin, Vector3 normal )
	{
		DispatchBloodEffects( origin, normal );
	}
}
