using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual void FullWalkMove()
	{
		if ( !InWater() )
		{
			StartGravity();
		}

		// If we are leaping out of the water, just update the counters.
		if ( Player.WaterJumpTime != 0 )
		{
			// Try to jump out of the water (and check to see if we still are).
			WaterJump();
			TryPlayerMove();

			CheckWater();
			return;
		}

		// If we are swimming in the water, see if we are nudging against a place we can jump up out
		// of, and, if so, start out jump.  Otherwise, if we are not moving up, then reset jump timer to 0.
		// Also run the swim code if we're a ghost or have the TF_COND_SWIMMING_NO_EFFECTS condition
		if ( InWater() )
		{
			FullWalkMoveUnderwater();
			return;
		}

		if ( WishJump() )
		{
			CheckJumpButton();
		}

		CheckVelocity();

		if ( Player.GroundEntity.IsValid() )
		{
			Friction();
			WalkMove();
		}
		else
		{
			AirMove();
		}

		// Set final flags.
		CategorizePosition();

		// Add any remaining gravitational component.
		if ( !InWater() )
		{
			FinishGravity();
		}

		// Make sure velocity is valid.
		CheckVelocity();

		CheckFalling();
	}

	public void CheckFalling()
	{
		if ( Player.IsInAir || Player.FallVelocity <= 0 || !Player.IsAlive )
			return;

		// let any subclasses know that the player has landed and how hard
		OnLand( Player.FallVelocity );

		//
		// Clear the fall velocity so the impact doesn't happen again.
		//
		Player.FallVelocity = 0;
	}

	public virtual void OnLand( float velocity )
	{
		// Take specified amount of fall damage when landed.
		Player.OnLanded( velocity );
	}

	public virtual void WalkMove()
	{
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );
		var oldGround = Player.GroundEntity;

		var fmove = ForwardMove;
		var smove = SideMove;

		// Get the wish direction.
		forward = forward.WithZ( 0 ).Normal;
		right = right.WithZ( 0 ).Normal;

		Vector3 wishvel = 0;
		for ( int i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;

		wishvel[2] = 0;

		var wishspeed = wishvel.Length;
		var wishdir = wishvel.Normal;

		DebugOverlay.ScreenText( wishvel.ToString(), -4 );
		DebugOverlay.ScreenText( wishdir.ToString(), -3 );

		//
		// Clamp to server defined max speed
		//
		if ( wishspeed != 0 && wishspeed > MaxSpeed )
		{
			wishspeed = MaxSpeed;
		}

		var acceleration = sv_accelerate;

		// if our wish speed is too low, we must increase acceleration or we'll never overcome friction
		// Reverse the basic friction calculation to find our required acceleration
		var wishspeedThreshold = 100 * sv_friction / sv_accelerate;
		if ( wishspeed > 0 && wishspeed < wishspeedThreshold )
		{
			float speed = Velocity.Length;
			float flControl = (speed < sv_stopspeed) ? sv_stopspeed : speed;
			acceleration = (flControl * sv_friction) / wishspeed + 1;
		}

		// Set pmove velocity
		Velocity[2] = 0;
		Accelerate( wishdir, wishspeed, acceleration );
		Velocity[2] = 0;

		// Clamp the players speed in x,y.
		float newSpeed = Velocity.Length;
		if ( newSpeed > MaxSpeed )
		{
			float flScale = MaxSpeed / newSpeed;
			Velocity[0] *= flScale;
			Velocity[1] *= flScale;
		}

		Velocity += Player.BaseVelocity;
		var spd = Velocity.Length;

		if ( spd < 1 )
		{
			Velocity = 0;
			Velocity -= Player.BaseVelocity;
			return;
		}

		// first try just moving to the destination	
		var dest = Vector3.Zero;
		dest[0] = Position[0] + Velocity[0] * Time.Delta;
		dest[1] = Position[1] + Velocity[1] * Time.Delta;
		dest[2] = Position[2];

		var trace = TraceBBox( Position, dest );
		// didn't hit anything.
		if ( trace.Fraction == 1 )
		{
			Position = trace.EndPosition;
			Velocity -= Player.BaseVelocity;

			StayOnGround();
			return;
		}

		if ( oldGround == null && Player.GetWaterLevel() == 0 )
		{
			Velocity -= Player.BaseVelocity;
			return;
		}

		// If we are jumping out of water, don't do anything more.
		if ( Player.WaterJumpTime != 0 )
		{
			Velocity -= Player.BaseVelocity;
			return;
		}

		StepMove();
		Velocity -= Player.BaseVelocity;

		StayOnGround();
	}

	/// <summary>
	/// Remove ground friction from velocity
	/// </summary>
	public virtual void Friction()
	{
		// If we are in water jump cycle, don't apply friction
		if ( Player.IsJumpingFromWater )
			return;

		// Calculate speed
		var speed = Velocity.Length;
		if ( speed < 0.1f )
			return;

		var drop = 0f;

		if ( Player.GroundEntity != null )
		{
			var friction = sv_friction * Player.SurfaceFriction;
			var control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

			// Add the amount to the drop amount.
			drop += control * friction * Time.Delta;
		}

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 )
			newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Velocity *= newspeed;
		}
	}

	public virtual void AirMove()
	{
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );

		var fmove = ForwardMove;
		var smove = SideMove;

		forward[2] = 0;
		right[2] = 0;
		forward = forward.Normal;
		right = right.Normal;

		Vector3 wishvel = 0;
		for ( var i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;
		wishvel[2] = 0;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		if ( wishspeed != 0 && wishspeed > MaxSpeed )
		{
			wishvel *= MaxSpeed / wishspeed;
			wishspeed = MaxSpeed;
		}

		AirAccelerate( wishdir, wishspeed, sv_airaccelerate );

		Velocity += Player.BaseVelocity;
		TryPlayerMove();
		Velocity -= Player.BaseVelocity;
	}
}
