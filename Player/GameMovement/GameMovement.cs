﻿using Sandbox;
using System;

namespace Amper.Source1;

public partial class Source1GameMovement : PawnController
{
	Source1Player Player { get; set; }
	protected float MaxSpeed { get; set; }

	/// <summary>
	/// Forward direction of the player's movement.
	/// </summary>
	protected Vector3 Forward { get; set; }
	/// <summary>
	/// Right direction of the player's movement.
	/// </summary>
	protected Vector3 Right { get; set; }
	/// <summary>
	/// Up direction of the player's movement.
	/// </summary>
	protected Vector3 Up { get; set; }

	/// <summary>
	/// How much should we move forward?
	/// </summary>
	protected float ForwardMove { get; set; }
	/// <summary>
	/// How much should we move to the side?
	/// </summary>
	protected float RightMove { get; set; }
	/// <summary>
	/// How much should we move up?
	/// </summary>
	protected float UpMove { get; set; }
	/// <summary>
	/// Local eye position that is not modified by any of view punches.
	/// </summary>
	protected Vector3 PureLocalEyePosition { get; set; }

	public override void FrameSimulate()
	{
		base.FrameSimulate();

		EyeRotation = Input.Rotation;
		if ( Player == null )
			return;

		UpdateViewOffset();
	}

	public virtual void PawnChanged( Source1Player player, Source1Player prev ) { }

	public override void Simulate()
	{
		if ( Player != Pawn )
		{
			var newPlayer = Pawn as Source1Player;
			PawnChanged( newPlayer, Player );
			Player = newPlayer;
		}

		ProcessMovement();
		ShowDebugOverlay();
	}

	public virtual void ProcessMovement()
	{
		if ( Player == null )
			return;

		MaxSpeed = Player.MaxSpeed;

		PlayerMove();
	}

	public virtual void PlayerMove()
	{
		EyeRotation = Input.Rotation;
		Forward = Input.Rotation.Forward;
		Right = Input.Rotation.Right;
		Up = Input.Rotation.Up;

		var speed = MaxSpeed;
		ForwardMove = Input.Forward * speed;
		RightMove = -Input.Left * speed;
		UpMove = Input.Up * speed;

		ReduceTimers();
		CheckParameters();

		if ( !Player.CanMove() )
		{
			ForwardMove = 0;
			RightMove = 0;
			UpMove = 0;
		}

		// Decrease velocity if we move vertically too quickly.
		if ( Velocity.z > 250 )
		{
			ClearGroundEntity();
		}

		// remember last level type
		LastWaterLevelType = Player.WaterLevelType;

		// If we are not on ground, store how fast we are moving down
		if ( IsInAir )
		{
			Player.FallVelocity = -Velocity.z;
		}

		SimulateModifiers();
		UpdateViewOffset();
		Player.SimulateFootsteps( Position, Velocity );

		if ( IsAlive ) 
		{
			if ( !LadderMove() && Player.MoveType == MoveType.MOVETYPE_LADDER )
			{
				// Clear ladder stuff unless player is dead or riding a train
				// It will be reset immediately again next frame if necessary
				Player.MoveType = MoveType.MOVETYPE_WALK;
			}
		}

		switch ( Pawn.MoveType )
		{
			case MoveType.None:
				break;

			case MoveType.MOVETYPE_ISOMETRIC:
			case MoveType.MOVETYPE_WALK:
				FullWalkMove();
				break;

			case MoveType.MOVETYPE_NOCLIP:
				FullNoClipMove( sv_noclip_speed, sv_noclip_accelerate );
				break;

			case MoveType.MOVETYPE_LADDER:
				FullLadderMove();
				break;

			case MoveType.MOVETYPE_OBSERVER:
				FullObserverMove();
				break;
		}
	}

	Vector3 LastEyeLocalPosition { get; set; }
	[ConVar.Client] public static float cl_viewoffset_lerp_speed { get; set; } = 15;

	public void SmoothLocalViewOffset()
	{
		var newEyePos = EyeLocalPosition;
		var oldEyePos = LastEyeLocalPosition;

		EyeLocalPosition = oldEyePos.LerpTo( newEyePos, Time.Delta * cl_viewoffset_lerp_speed );
		LastEyeLocalPosition = EyeLocalPosition;
	}

	public virtual void SimulateModifiers()
	{
		SimulateDucking();
	}

	public virtual void UpdateViewOffset()
	{
		// reset x,y
		EyeLocalPosition = GetPlayerViewOffset( false );

		// this updates z offset.
		SetDuckedEyeOffset( Easing.QuadraticInOut( DuckProgress ) );

		SmoothLocalViewOffset();
	}

	public virtual void SetDuckedEyeOffset( float duckFraction )
	{
		Vector3 vDuckHullMin = GetPlayerMins( true );
		Vector3 vStandHullMin = GetPlayerMins( false );

		float fMore = vDuckHullMin.z - vStandHullMin.z;

		Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
		Vector3 vecStandViewOffset = GetPlayerViewOffset( false );
		Vector3 temp = EyeLocalPosition;

		temp.z = (vecDuckViewOffset.z - fMore) * duckFraction + vecStandViewOffset.z * (1 - duckFraction);

		EyeLocalPosition = temp;
	}

	public virtual void ReduceTimers()
	{
		if ( JumpTime > 0 )
			JumpTime = Math.Max( JumpTime - Time.Delta, 0 );
	}

	public virtual void CheckParameters()
	{
		if ( Player.MoveType != MoveType.MOVETYPE_ISOMETRIC &&
			Player.MoveType != MoveType.MOVETYPE_NOCLIP &&
			Player.MoveType != MoveType.MOVETYPE_OBSERVER )
		{
			var speed = ForwardMove * ForwardMove + RightMove * RightMove + UpMove * UpMove;

			if ( speed != 0 && speed > MaxSpeed * MaxSpeed )
			{
				var ratio = MaxSpeed / MathF.Sqrt( speed );

				ForwardMove *= ratio;
				RightMove *= ratio;
				UpMove *= ratio;
			}
		}
	}

	public virtual void StepMove( Vector3 dest )
	{
		var mover = new MoveHelper( Position, Velocity );
		mover.Trace = SetupBBoxTrace( 0, 0 );
		mover.MaxStandableAngle = sv_maxstandableangle;

		mover.TryMoveWithStep( Time.Delta, sv_stepsize );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public virtual void TryPlayerMove()
	{
		MoveHelper mover = new MoveHelper( Position, Velocity );
		mover.Trace = SetupBBoxTrace( 0, 0 );
		mover.MaxStandableAngle = sv_maxstandableangle;

		mover.TryMove( Time.Delta );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public virtual bool CanAccelerate()
	{
		if ( IsJumpingFromWater )
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
		var speed = Velocity.Dot( wishdir );

		var addspeed = wishspeed - speed;
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public virtual void AirAccelerate( Vector3 wishdir, float wishSpeed, float acceleration )
	{
		if ( !CanAccelerate() )
			return;

		var speedCap = GetAirSpeedCap();

		var wishSpeedCapped = wishSpeed;
		if ( wishSpeedCapped > speedCap )
			wishSpeedCapped = speedCap;

		// See if we are changing direction a bit
		var currentspeed = Velocity.Dot( wishdir );

		// Reduce wishspeed by the amount of veer.
		var addspeed = wishSpeedCapped - currentspeed;

		// If not going to add any speed, done.
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * wishSpeed * Time.Delta * Player.SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Velocity += accelspeed * wishdir;
	}

	public virtual float GetAirSpeedCap() => 30;

	/// <summary>
	/// Remove ground friction from velocity
	/// </summary>
	public virtual void Friction()
	{
		// If we are in water jump cycle, don't apply friction
		if ( IsJumpingFromWater ) 
			return;

		// Calculate speed
		var speed = Velocity.Length;
		if ( speed < 0.1f ) 
			return;

		float control, drop = 0;
		if ( IsGrounded )
		{
			var friction = sv_friction * Player.SurfaceFriction;

			control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

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

	public virtual void CategorizePosition()
	{
		Player.SurfaceFriction = 1.0f;
		CheckWater();

		if ( Player.IsObserver )
			return;

		var point = Position - Vector3.Up * 2;
		var bumpOrigin = Position;

		float zvel = Velocity.z;
		bool bMovingUp = zvel > 0;
		bool bMovingUpRapidly = zvel > sv_maxnonjumpvelocity;
		float flGroundEntityVelZ = 0;

		if ( bMovingUpRapidly )
		{
			if ( IsGrounded )
			{
				flGroundEntityVelZ = GroundEntity.Velocity.z;
				bMovingUpRapidly = (zvel - flGroundEntityVelZ) > sv_maxnonjumpvelocity;
			}
		}


		if ( bMovingUpRapidly || (bMovingUp && Player.MoveType == MoveType.MOVETYPE_LADDER) )
		{
			ClearGroundEntity();
		}
		else
		{
			var trace = TraceBBox( bumpOrigin, point );
			if ( trace.Entity == null || Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle )
			{
				trace = TryTouchGroundInQuadrants( bumpOrigin, point, trace );
				if ( trace.Entity == null || Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle )
				{
					ClearGroundEntity();

					if ( Velocity.z > 0 && Player.MoveType != MoveType.MOVETYPE_NOCLIP )
					{
						Player.SurfaceFriction = 0.25f;
					}
				}
				else
				{
					UpdateGroundEntity( trace );
				}
			}
			else
			{
				UpdateGroundEntity( trace );
			}
		}
	}

	public TraceResult TryTouchGroundInQuadrants( Vector3 start, Vector3 end, TraceResult pm )
	{
		bool isDucked = false;

		Vector3 mins, maxs;
		Vector3 minsSrc = GetPlayerMins( isDucked );
		Vector3 maxsSrc = GetPlayerMaxs( isDucked );

		float fraction = pm.Fraction;
		Vector3 endpos = pm.EndPosition;

		// Check the -x, -y quadrant
		mins = minsSrc;
		maxs = new( MathF.Min( 0, maxsSrc.x ), MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, +y quadrant
		maxs = maxsSrc;
		mins = new( MathF.Max( 0, minsSrc.x ), MathF.Max( 0, minsSrc.y ), minsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the -x, +y quadrant
		mins = new( minsSrc.x, MathF.Max( 0, minsSrc.y ), minsSrc.z );
		maxs = new( MathF.Min( 0, maxsSrc.x ), maxsSrc.y, maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		// Check the +x, -y quadrant
		mins = new( MathF.Max( 0, minsSrc.x ), minsSrc.y, minsSrc.z );
		maxs = new( maxsSrc.x, MathF.Min( 0, maxsSrc.y ), maxsSrc.z );

		pm = TraceBBox( start, end, mins, maxs );
		if ( pm.Entity != null && Vector3.GetAngle( Vector3.Up, pm.Normal ) >= sv_maxstandableangle )
		{
			pm.Fraction = fraction;
			pm.EndPosition = endpos;
			return pm;
		}

		pm.Fraction = fraction;
		pm.EndPosition = endpos;
		return pm;
	}


	/// <summary>
	/// We have a new ground entity
	/// </summary>
	public virtual void UpdateGroundEntity( TraceResult tr )
	{
		var newGround = tr.Entity;
		var oldGround = GroundEntity;

		var vecBaseVelocity = BaseVelocity;

		if ( oldGround == null && newGround != null )
		{
			// Subtract ground velocity at instant we hit ground jumping
			vecBaseVelocity -= newGround.Velocity;
			vecBaseVelocity.z = newGround.Velocity.z;
		}
		else if ( oldGround != null && newGround != null ) 
		{
			// Add in ground velocity at instant we started jumping
			vecBaseVelocity += oldGround.Velocity;
			vecBaseVelocity.z = oldGround.Velocity.z;
		}

		BaseVelocity = vecBaseVelocity;
		GroundEntity = newGround;

		// If we are on something...
		if ( newGround != null ) 
		{
			CategorizeGroundSurface( tr );

			// Then we are not in water jump sequence
			WaterJumpTime = 0;

			Velocity = Velocity.WithZ( 0 );
		}
	}

	/// <summary>
	/// We're no longer on the ground, remove it
	/// </summary>
	public virtual void ClearGroundEntity()
	{
		if ( GroundEntity == null ) 
			return;

		GroundEntity = null;
		GroundNormal = Vector3.Up;
		Player.SurfaceFriction = 1.0f;
	}

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = Position + Vector3.Up * 2;
		var end = Position + Vector3.Down * sv_stepsize;

		// See how far up we can go without getting stuck
		var trace = TraceBBox( Position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return;
		if ( trace.Fraction >= 1 ) return;
		if ( trace.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) >= sv_maxstandableangle ) return;

		Position = trace.EndPosition;
	}

	public Entity TestPlayerPosition( Vector3 pos, ref TraceResult pm )
	{
		pm = TraceBBox( pos, pos );
		return pm.Entity;
	}

	public virtual void CategorizeGroundSurface( TraceResult pm )
	{
		Player.SurfaceData = pm.Surface;
		Player.SurfaceFriction = pm.Surface.Friction;

		Player.SurfaceFriction *= 1.25f;
		if ( Player.SurfaceFriction > 1.0f )
			Player.SurfaceFriction = 1.0f;
	}

	public bool IsAlive => Pawn.LifeState == LifeState.Alive;
	public bool IsDead => !IsAlive;
	public bool IsGrounded => GroundEntity != null;
	public bool IsInAir => !IsGrounded;

	protected virtual void ShowDebugOverlay()
	{
		if ( sv_debug_movement && Player.Client.IsListenServerHost && Host.IsServer ) 
		{
			DebugOverlay.ScreenText( 
				$"[PLAYER]\n" +
				$"LifeState             {Player.LifeState}\n" +
				$"TeamNumber            {Player.TeamNumber}\n" +
				$"LastAttacker          {Player.LastAttacker}\n" +
				$"LastAttackerWeapon    {Player.LastAttackerWeapon}\n" +
				$"GroundEntity          {Player.GroundEntity}\n" +
				$"\n" +

				$"[MOVEMENT]\n" +
				$"Direction             {new Vector3( Input.Forward, -Input.Left, Input.Up )}\n" +
				$"WishVelocity          {WishVelocity}\n" +
				$"SurfaceFriction       {Player.SurfaceFriction}\n" +
				$"MoveType              {Player.MoveType}\n" +
				$"Speed                 {Velocity.Length}\n" +
				$"MaxSpeed              {MaxSpeed}\n" +
				$"Fall Velocity         {Player.FallVelocity}\n" +
				$"\n" +

				$"[DUCKING]\n" +
				$"IsDucked              {Player.IsDucked}\n" +
				$"IsDucking             {IsDucking}\n" +
				$"DuckTime              {DuckTime}\n" +
				$"\n" +

				$"[OBSERVER]\n" +
				$"ObserverMode          {Player.ObserverMode}\n" +
				$"LastObserverMode      {Player.LastObserverMode}\n" +
				$"ForcedObserverMode    {Player.IsForcedObserverMode}\n" +
				$"ObserverTarget        {Player.ObserverTarget}",
				new Vector2( 60, 250 ) );
		}
	}

	[ConVar.Replicated] public static bool sv_debug_movement { get; set; }
}