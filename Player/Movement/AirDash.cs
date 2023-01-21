namespace Amper.FPS;

partial class GameMovement
{
	public virtual float AirDashImpulse => 289;
	public virtual bool CanAirDash() => Player.CanAirDash();

	public virtual bool CheckAirDashButton()
	{
		if ( !CanAirDash() )
			return false;

		AirDash();
		return true;
	}

	public virtual void AirDash()
	{
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );

		// Get the wish direction.
		forward = forward.WithZ( 0 ).Normal;
		right = right.WithZ( 0 ).Normal;

		// Find the direction,velocity in the x,y plane.
		var wishvelocity = forward * ForwardMove + right * SideMove;

		// Update the velocity on the scout.
		Velocity = wishvelocity;
		Velocity.z = AirDashImpulse;

		Player.SetAnimParameter( "b_jump", true );

		// Air dashing reset air ducks so we can crouch jump again to reach the ledge.
		Player.AirDuckCount = 0;
		Player.AirDashCount++;
	}
}
