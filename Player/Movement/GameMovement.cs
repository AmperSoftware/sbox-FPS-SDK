﻿using Sandbox;
using System;

namespace Amper.Source1;

public partial class GameMovement
{
	protected Source1Player Player;

	public virtual void FrameSimulate( Source1Player player )
	{
		// Check if we've supplied proper player.
		if ( !player.IsValid() )
			return;

		// Store variables.
		Player = player;
		SetupMoveData( player );

		// Do frame updates
		FrameUpdate();
		ApplyMoveData( player );
	}

	public virtual void FrameUpdate() { }

	public virtual void Simulate( Source1Player player )
	{
		// Check if we've supplied proper player.
		if ( !player.IsValid() )
			return;

		// Store variables.
		Player = player;
		SetupMoveData( player );

		// Do movement.
		Update();
		ShowDebugOverlay();
		ApplyMoveData( player );
	}

	public float MaxSpeed;
	public Vector3 Position;
	public Vector3 Velocity;
	public QAngle ViewAngles;

	public float ForwardMove;
	public float SideMove;
	public float UpMove;

	public virtual void SetupMoveData( Source1Player player )
	{
		MaxSpeed = player.MaxSpeed;
		Position = player.Position;
		Velocity = player.Velocity;

		ViewAngles = Input.Rotation;
		ForwardMove = Input.Forward * MaxSpeed;
		SideMove = -Input.Left * MaxSpeed;
		UpMove = Input.Up * MaxSpeed;
	}

	public virtual void ApplyMoveData( Source1Player player )
	{
		player.Position = Position;
		player.EyeRotation = ViewAngles;
		player.Velocity = Velocity;
	}

	public virtual void Update()
	{
		CheckParameters();
		ReduceTimers();

		if ( CanStuck() )
		{
			if ( CheckStuck() )
				return;
		}

		if ( Player.IsInAir )
			Player.FallVelocity = -Velocity.z;

		UpdateViewOffset();
		SimulateModifiers();
		Player.SimulateFootsteps( Position, Velocity );

		switch ( Player.MoveType )
		{
			case MoveType.MOVETYPE_ISOMETRIC:
			case MoveType.MOVETYPE_WALK:
				FullWalkMove();
				break;

			case MoveType.MOVETYPE_NOCLIP:
				FullNoClipMove(sv_noclip_speed, sv_noclip_accelerate);
				break;
		}
	}

	public virtual void UpdateViewOffset()
	{
		// reset x,y
		Player.EyeLocalPosition = GetPlayerViewOffset( false );

		if ( Player.DuckTime == 0 )
			return;

		// this updates z offset.
		SetDuckedEyeOffset( Util.SimpleSpline( Player.DuckProgress ) );
	}

	public virtual void SimulateModifiers()
	{
		SimulateDucking();
	}

	protected string DescribeAxis( int axis )
	{
		switch ( axis )
		{
			case 0: return "X";
			case 1: return "Y";
			case 2: default: return "Z";
		}
	}

	public void CheckVelocity()
	{
		for ( int i = 0; i < 3; i++ )
		{
			if ( float.IsNaN( Velocity[i] ) )
			{
				Log.Info( $"Got a NaN velocity {DescribeAxis( i )}" );
				Velocity[i] = 0;
			}

			if ( float.IsNaN( Position[i] ) )
			{
				Log.Info( $"Got a NaN position {DescribeAxis( i )}" );
				Position[i] = 0;
			}

			if ( Velocity[i] > sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too high on {DescribeAxis( i )}" );
				Velocity[i] = sv_maxvelocity;
			}

			if ( Velocity[i] < -sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too low on {DescribeAxis( i )}" );
				Velocity[i] = -sv_maxvelocity;
			}
		}
	}

	public virtual void ReduceTimers()
	{
		float frame_msec = 1000.0f * Time.Delta;
		int nFrameMsec = (int)frame_msec;

		if ( Player.m_nJumpTimeMsecs > 0 )
		{
			Player.m_nJumpTimeMsecs -= nFrameMsec;
			if ( Player.m_nJumpTimeMsecs < 0 )
				Player.m_nJumpTimeMsecs = 0;
		}
	}

	protected void ReduceTimer( ref int timer, int delta )
	{
		if ( timer > 0 )
		{
			timer -= delta;
			if ( timer < 0 )
				timer = 0;
		}
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void AirAccelerate( Vector3 wishdir, float wishspeed, float accel )
	{
		if ( !CanAccelerate() )
			return;

		var wishspd = wishspeed;

		if ( wishspd > AirSpeedCap )
			wishspd = AirSpeedCap;

		// See if we are changing direction a bit
		var currentspeed = Velocity.Dot( wishdir );

		// Reduce wishspeed by the amount of veer.
		var addspeed = wishspd - currentspeed;

		// If not going to add any speed, done.
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = accel * wishspeed * Time.Delta * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}

	public virtual bool CanAccelerate()
	{
		if ( Player.WaterJumpTime != 0 )
			return false;

		return true;
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void Accelerate( Vector3 wishdir, float wishspeed, float acceleration )
	{
		if ( !CanAccelerate() )
			return;

		// See if we are changing direction a bit
		var currentspeed = Velocity.Dot( wishdir );

		var addspeed = wishspeed - currentspeed;
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}


	public const int COORD_FRACTIONAL_BITS = 5;
	public const int COORD_DENOMINATOR = (1 << COORD_FRACTIONAL_BITS);
	public const int COORD_RESOLUTION = 1 / COORD_DENOMINATOR;

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = Position;
		var end = Position;

		start.z += 2;
		end.z -= Player.m_flStepSize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction > 0 &&
			trace.Fraction < 1 &&
			!trace.StartedSolid &&
			trace.Normal[2] >= 0.7f )
		{
			var flDelta = MathF.Abs( Position.z - trace.EndPosition.z );

			if ( flDelta > 0.5f * COORD_RESOLUTION )
			{
				Position = trace.EndPosition;
			}
		}
	}
	public virtual void CategorizePosition()
	{
		Player.SurfaceFriction = 1.0f;
		// CheckWater();

		if ( Player.IsObserver )
			return;

		var offset = 2;

		var point = Position + Vector3.Down * offset;
		var bumpOrigin = Position;

		float zvel = Velocity.z;
		bool bMovingUp = zvel > 0;
		bool bMovingUpRapidly = zvel > NON_JUMP_VELOCITY;
		float flGroundEntityVelZ = 0;
		if ( bMovingUpRapidly )
		{
			var ground = Player.GroundEntity;
			if ( ground != null )
			{
				flGroundEntityVelZ = ground.Velocity.z;
				bMovingUpRapidly = (zvel - flGroundEntityVelZ) > NON_JUMP_VELOCITY;
			}
		}

		// Was on ground, but now suddenly am not
		if ( bMovingUpRapidly ||
			(bMovingUp && Player.MoveType == MoveType.MOVETYPE_LADDER) )
		{
			SetGroundEntity( null );
		}
		else
		{
			// Try and move down.
			var trace = TraceBBox( bumpOrigin, point );

			// Was on ground, but now suddenly am not.  If we hit a steep plane, we are not on ground
			if ( trace.Entity == null || trace.Normal[2] < .7f )
			{
				// Test four sub-boxes, to see if any of them would have found shallower slope we could actually stand on
				trace = TryTouchGroundInQuadrants( bumpOrigin, point, trace );

				if ( trace.Entity == null || trace.Normal[2] < .7f )
				{
					SetGroundEntity( null );

					if ( Velocity.z > 0 &&
						Player.MoveType != MoveType.MOVETYPE_NOCLIP )
					{
						Player.SurfaceFriction = 0.25f;
					}
				}
				else
				{
					SetGroundEntity( trace );
				}
			}
			else
			{
				SetGroundEntity( trace );
			}
		}
	}

	public TraceResult TryTouchGroundInQuadrants( Vector3 start, Vector3 end, TraceResult pm )
	{
		Vector3 mins, maxs;
		Vector3 minsSrc = GetPlayerMins();
		Vector3 maxsSrc = GetPlayerMaxs();

		float fraction = pm.Fraction;
		Vector3 endpos = pm.EndPosition;

		// Check the -x, -y quadrant
		mins = minsSrc;
		maxs = new( MathF.Min( 0, maxsSrc.x ), MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, +y quadrant
		maxs = maxsSrc;
		mins = new( MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the -x, +y quadrant
		mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
		maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, -y quadrant
		mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
		maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && pm.Normal.z < 0.7f )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		pm.Fraction = fraction;
		pm.EndPosition = endpos;
		return pm;
	}

	public virtual void CheckParameters()
	{
		if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
				Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			float spd = (ForwardMove * ForwardMove) +
					(SideMove * SideMove) +
					(UpMove * UpMove);

			if ( (spd != 0) && (spd > MaxSpeed * MaxSpeed) )
			{
				var ratio = MaxSpeed / MathF.Sqrt( spd );
				ForwardMove *= ratio;
				SideMove *= ratio;
				UpMove *= ratio;
			}
		}

		// Remember last water level
		LastWaterLevelType = Player.WaterLevelType;

		// if we are going upwards with this speed, we can't be standing on anything.
		if ( Velocity.z > 250 )
			SetGroundEntity( null );

		DecayViewPunchAngle();

		if ( !Player.IsAlive ) 
		{
			var v_angle = ViewAngles;
			v_angle = v_angle + Player.ViewPunchAngle;

			// Now adjust roll angle
			if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
				 Player.MoveType != MoveType.MOVETYPE_NOCLIP )
			{
				// ViewAngles.Roll = CalcRoll( v_angle, Velocity, sv_rollangle, sv_rollspeed );
			}
			else
			{
				ViewAngles.Roll = 0; // v_angle[ ROLL ];
			}

			ViewAngles.Pitch = v_angle.Pitch;
			ViewAngles.Yaw = v_angle.Yaw;
		}
		else
		{
			// mv->m_vecAngles = mv->m_vecOldAngles;
		}

		if ( ViewAngles.Yaw > 180 )
		{
			ViewAngles.Yaw -= 360;
		}
	}
}