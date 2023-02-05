using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer
{
	[ConVar.Replicated] public static bool sv_falldamage { get; set; } = true;
	/// <summary>
	/// Our current fall velocity.
	/// </summary>
	public float FallVelocity { get; set; }

	/// <summary>
	/// What happens we land on the group with a given velocity.
	/// </summary>
	public virtual void OnLanded( float velocity )
	{
		TakeFallDamage( velocity );
		LandingEffects( velocity );
	}

	/// <summary>
	/// Apply damage from falling on the ground.
	/// </summary>
	public virtual void TakeFallDamage( float velocity )
	{
		var fallDamage = SDKGame.Current.GetPlayerFallDamage( this, velocity );
		if ( fallDamage <= 0 )
			return;

		DoFallPainSound();

		if ( !sv_falldamage )
			return;

		var fallDmgInfo = ExtendedDamageInfo.Create( fallDamage )
			.WithTag( DamageTags.Fall )
			.WithInflictor( this )
			.WithAllPositions( Position );

		TakeDamage( fallDmgInfo );
	}

	public virtual void DoFallPainSound()
	{
		PlaySound( "player.fallpain" );
	}

	/// <summary>
	/// Vertical velocity at which we will take maximum fall damage that is enough to fully kill us.
	/// </summary>
	public virtual float FatalFallSpeed => 1024;
	/// <summary>
	/// Maximum vertical velocity at which we wont take damage when falling down.
	/// </summary>
	public virtual float MaxSafeFallSpeed => 580;
	/// <summary>
	/// How much damage we should apply per unit of vertical velocity.
	/// </summary>
	public virtual float DamageForFallSpeed => 100 / (FatalFallSpeed - MaxSafeFallSpeed);

	public virtual void LandingEffects( float velocity )
	{
		if ( velocity <= 0 )
			return;

		var volume = .5f;
		if ( velocity > MaxSafeFallSpeed / 2 )
		{
			volume = velocity.RemapClamped( MaxSafeFallSpeed / 2, MaxSafeFallSpeed, .85f, 1 );
		}

		// Play the landing footstep sound when we land on the groun.
		DoLandSound( Position, SurfaceData, volume );

		// If we go past the max safe velocity threshold,
		// knock the screen around a little bit.
		if ( velocity >= MaxSafeFallSpeed )
		{
			ApplyViewPunchImpulse( 0, 0, velocity * 0.013f );
		}
	}
}
