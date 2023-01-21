using System;
using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual void FullWalkMoveUnderwater()
	{
		if ( Player.WaterLevelType == WaterLevelType.Waist )
			CheckWaterJump();

		// If we are falling again, then we must not trying to jump out of water any more.
		if ( Velocity.z < 0 && Player.WaterJumpTime != 0 )
			Player.WaterJumpTime = 0.0f;

		// Was jump button pressed?
		if ( Input.Down( InputButton.Jump ) )
			CheckJumpButton();

		// Perform regular water movement
		WaterMove();

		// Redetermine position vars
		CategorizePosition();

		// If we are on ground, no downward velocity.
		if ( Player.GroundEntity.IsValid() ) 
		{
			Velocity.z = 0;
		}
	}

	protected void WaterMove()
	{
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );

		var wishvel = forward * ForwardMove + right * SideMove;

		// if we have the jump key down, move us up as well
		if ( Input.Down( InputButton.Jump ) )
		{
			wishvel.z += MaxSpeed;
		}

		// Sinking after no other movement occurs
		else if ( ForwardMove == 0 && SideMove == 0 && UpMove == 0 )
		{
			wishvel.z -= 60;
		}
		else  // Go straight up by upmove amount.
		{
			// exaggerate upward movement along forward as well
			float upwardMovememnt = ForwardMove * forward.z * 2;
			upwardMovememnt = Math.Clamp( upwardMovememnt, 0, MaxSpeed );
			wishvel.z += UpMove + upwardMovememnt;
		}

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		// Cap speed.
		if ( wishspeed > MaxSpeed )
		{
			wishvel *= MaxSpeed / wishspeed;
			wishspeed = MaxSpeed;
		}

		// Slow us down a bit.
		wishspeed *= 0.8f;

		// Water friction
		var temp = Velocity;
		var speed = temp.Length;

		var newspeed = 0f;
		if ( speed != 0 )
		{
			newspeed = speed - Time.Delta * speed * sv_friction * Player.SurfaceFriction;
			if ( newspeed < 0.1f )
			{
				newspeed = 0;
			}

			Velocity *= newspeed / speed;
		}
		else
		{
			newspeed = 0;
		}

		// water acceleration
		if ( wishspeed >= .1f )
		{
			var addspeed = wishspeed - newspeed;
			if ( addspeed > 0 )
			{
				wishvel = wishvel.Normal;

				var accelspeed = sv_accelerate * wishspeed * Time.Delta * Player.SurfaceFriction;
				if ( accelspeed > addspeed ) accelspeed = addspeed;

				Velocity += accelspeed * wishvel;
			}
		}

		Velocity += Player.BaseVelocity;

		// Now move
		// assume it is a stair or a slope, so press down from stepheight above
		var dest = Position + Velocity * Time.Delta;

		var pm = TraceBBox( Position, dest );
		if ( pm.Fraction == 1 )
		{
			var start = dest.WithZ( dest.z + Player.StepSize + 1 );

			pm = TraceBBox( start, dest );
			if ( !pm.StartedSolid )
			{
				// walked up the step, so just keep result and exit
				Position = pm.EndPosition;
				Velocity -= Player.BaseVelocity;
				return;
			}

			// Try moving straight along out normal path.
			TryPlayerMove();
		}
		else
		{
			if ( !Player.GroundEntity.IsValid() ) 
			{
				TryPlayerMove();
				Velocity -= Player.BaseVelocity;
				return;
			}

			StepMove();
		}

		Velocity -= Player.BaseVelocity;
	}

	public virtual void WaterJump( )
	{
		if ( Player.WaterJumpTime > 10000 )
			Player.WaterJumpTime = 10000;

		if ( !Player.IsJumpingFromWater )
			return;

		Player.WaterJumpTime -= 1000.0f * Time.Delta;

		if ( Player.WaterJumpTime <= 0 || Player.WaterLevelType == WaterLevelType.NotInWater ) 
		{
			Player.WaterJumpTime = 0;
			Player.RemoveFlag( PlayerFlags.FL_WATERJUMP );
		}

		Velocity[0] = Player.WaterJumpVelocity[0];
		Velocity[1] = Player.WaterJumpVelocity[1];
	}

	public virtual bool CheckWaterJumpButton()
	{
		// See if we are water jumping.  If so, decrement count and return.
		if ( Player.IsJumpingFromWater )
		{
			Player.WaterJumpTime -= Time.Delta;
			if ( Player.WaterJumpTime < 0 )
			{
				Player.WaterJumpTime = 0;
			}

			return true;
		}

		// In water above our waist.
		if ( Player.WaterLevelType >= WaterLevelType.Waist )
		{
			// Swimming, not jumping.
			SetGroundEntity( null );

			// We move up a certain amount.
			Velocity.z = 100;

			// Play swimming sound.
			if ( Player.NextSwimSoundTime <= 0 )
			{
				// Don't play sound again for 1 second.
				Player.NextSwimSoundTime = 1000;
				Player.OnWaterWade();
			}

			return true;
		}

		return false;
	}
}
