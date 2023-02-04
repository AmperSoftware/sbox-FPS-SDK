using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual float AirDuckBodyShiftModifier => 1;
	public virtual float IdealDuckSpeed => 1;
	public virtual float TimeToDuck => .2f;
	public virtual float TimeBetweenDucks => 0;

	public virtual float DuckingSpeedModifier => 1;
	public virtual float UnduckingSpeedModifier => 1;

	public virtual float CalculateDuckSpeed( bool ducking )
	{
		var speed = Player.DuckSpeed;

		speed *= ducking ? DuckingSpeedModifier : UnduckingSpeedModifier;
		return speed;
	}

	public virtual bool WishDuck() => Input.Down( InputButton.Duck );

	public virtual void SimulateDucking()
	{
		// Reduce duck-spam penalty over time
		Player.DuckSpeed = Player.DuckSpeed.Approach( IdealDuckSpeed, Time.Delta );

		if ( WishDuck() && IsDuckingEnabled() )
		{
			OnDucking();
		}
		else
		{
			OnUnducking();
		}

		HandleDuckingSpeedCrop();
	}

	public virtual void OnDucking()
	{
		if ( !CanDuck() )
			return;

		var speed = CalculateDuckSpeed( true );
		Player.DuckTime = Player.DuckTime.Approach( TimeToDuck, Time.Delta * speed );

		if ( Player.DuckTime >= TimeToDuck || Player.IsInAir )
			FinishDuck();
	}

	public virtual void OnUnducking()
	{
		if ( !CanUnduck() )
			return;

		var speed = CalculateDuckSpeed( false );
		Player.DuckTime = Player.DuckTime.Approach( 0, Time.Delta * speed );

		if ( Player.DuckTime <= 0 || Player.IsInAir )
			FinishUnDuck();
	}

	public virtual bool CanDuck() => Player.CanDuck();

	public virtual bool CanUnduck()
	{
		if ( !Player.CanUnduck() )
			return false;

		var newOrigin = Position;

		if ( Player.GroundEntity.IsValid() )
		{
			newOrigin += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			// If in air an letting go of crouch, make sure we can offset origin to make
			//  up for uncrouching
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			viewDelta *= -1;
			newOrigin += viewDelta;
		}

		bool saveducked = Player.IsDucked;
		Player.IsDucked = false;

		var trace = TraceBBox( Position, newOrigin );
		Player.IsDucked = saveducked;

		if ( trace.StartedSolid || (trace.Fraction != 1.0f) )
			return false;

		return true;
	}

	protected virtual void SetDuckedEyeOffset( float duckFraction )
	{
		duckFraction = Util.SimpleSpline( duckFraction );

		var vDuckHullMin = GetPlayerMins( true );
		var vStandHullMin = GetPlayerMins( false );

		float fMore = vDuckHullMin.z - vStandHullMin.z;

		var vecDuckViewOffset = GetPlayerViewOffset( true );
		var vecStandViewOffset = GetPlayerViewOffset( false );
		var temp = Player.EyeLocalPosition;

		temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction) +
					(vecStandViewOffset.z * (1 - duckFraction));

		Player.EyeLocalPosition = temp;
	}

	public virtual void FinishDuck()
	{
		if ( Player.IsDucked )
			return;

		Player.IsDucked = true;
		Player.DuckTime = TimeToDuck;
		Player.LastDuckTime = Time.Now;

		if ( Player.IsGrounded )
		{
			Position -= GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Position += viewDelta * AirDuckBodyShiftModifier;

			Player.AirDuckCount++;
		}

		// See if we are stuck?
		FixPlayerCrouchStuck( true );
		CategorizePosition();
	}

	public virtual void FinishUnDuck()
	{
		if ( !Player.IsDucked )
			return;

		Player.IsDucked = false;
		Player.DuckTime = 0;

		if ( Player.IsGrounded )
		{
			Position += GetPlayerMins( true ) - GetPlayerMins( false );
		}
		else
		{
			var hullSizeNormal = GetPlayerMaxs( false ) - GetPlayerMins( false );
			var hullSizeCrouch = GetPlayerMaxs( true ) - GetPlayerMins( true );
			var viewDelta = hullSizeNormal - hullSizeCrouch;
			Position -= viewDelta * AirDuckBodyShiftModifier;
		}

		// Recategorize position since ducking can change origin
		CategorizePosition();
	}

	public virtual void FixPlayerCrouchStuck( bool upward )
	{
		int direction = upward ? 1 : 0;

		var trace = TraceBBox( Position, Position );
		if ( trace.Entity == null )
			return;

		var test = Position;
		for ( int i = 0; i < 36; i++ )
		{
			var org = Position;
			org.z += direction;

			Position = org;
			trace = TraceBBox( Position, Position );
			if ( trace.Entity == null )
				return;
		}

		Position = test;
	}

	public virtual float GetDuckSpeedModifier( float fraction )
	{
		return Player.DuckingSpeedModifier * fraction + 1 - fraction;
	}

	public virtual void HandleDuckingSpeedCrop()
	{
		if ( Player.ObserverMode == ObserverMode.Roaming )
			return;

		if ( Player.IsInAir )
			return;

		if ( !Player.IsDucking )
			return;

		float frac = GetDuckSpeedModifier( Player.DuckProgress );
		ForwardMove *= frac;
		SideMove *= frac;
		UpMove *= frac;

		MaxSpeed *= frac;
	}

	public virtual bool IsDuckingEnabled()
	{
		if ( Player.DuckSpeed < 0.2f )
			return false;

		if ( !Player.IsDucked && Time.Now < (Player.LastDuckTime + TimeBetweenDucks) )
			return false;

		return true;
	}
}
