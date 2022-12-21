using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer
{
	#region Footsteps
	public virtual void OnFootstep( bool right, Vector3 origin, string sound, float volume )
	{

	}

	public virtual void OnLandStep( Vector3 origin, string sound, float volume )
	{

	}

	public virtual void OnJumpStep( Vector3 origin, string sound, float volume )
	{

	}
	#endregion

	#region Water
	public virtual void OnWaterWade()
	{
		// Log.Info( "OnWaterWade()" );
		PlayWaterWadeSound();
	}

	public virtual void OnEnterWater()
	{
		// Log.Info( "OnEnterWater()" );
		PlayWaterWadeSound();

		// The player has just entered the water.  Determine if we should play a splash sound.
		bool bPlaySplash = false;

		var vecVelocity = Velocity;
		if ( vecVelocity.z <= -200.0f )
		{
			// If the player has significant downward velocity, play a splash regardless of water depth.  (e.g. Jumping hard into a puddle)
			bPlaySplash = true;
		}
		else
		{
			// Look at the water depth below the player.  If it's significantly deep, play a splash to accompany the sinking that's about to happen.
			var vecStart = Position;
			var vecEnd = vecStart;
			vecEnd.z -= 20; // roughly thigh deep

			var tr = Trace.Ray( vecStart, vecEnd )
				.WithAnyTags( CollisionTags.Solid )
				.Ignore( this )
				.Run();

			if ( tr.Fraction >= 1 ) bPlaySplash = true;
		}

		if ( bPlaySplash ) PlayWaterSplashSound();
	}

	public virtual void OnLeaveWater()
	{
		// Log.Info( "OnLeaveWater()" );
		PlayWaterWadeSound();
	}

	public virtual void OnEnterUnderwater()
	{
		// Log.Info( "OnEnterUnderwater()" );
		StartUnderwaterSound();
	}

	public virtual void OnLeaveUnderwater()
	{
		// Log.Info( "OnLeaveUnderwater()" );
		PlayWaterWadeSound();
		StopUnderwaterSound();
	}

	protected virtual void PlayWaterWadeSound()
	{
		PlaySound( "player.footstep.wade" );
	}

	protected virtual void PlayWaterSplashSound()
	{
		PlaySound( "physics.watersplash" );
	}

	Sound UnderwaterSound { get; set; }

	protected virtual void StartUnderwaterSound()
	{
		if ( !IsLocalPawn ) return;
		UnderwaterSound = PlaySound( "player.underwater" );
	}

	protected virtual void StopUnderwaterSound()
	{
		if ( !IsLocalPawn ) return;
		UnderwaterSound.Stop();
	}
	#endregion
}
