using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual float JumpImpulse => 268;
	public virtual bool WishJump() => Input.Pressed( InputButton.Jump );
	public virtual bool CanJump() => Player.CanJump();

	/// <summary>
	/// Returns true if any other buttons did anything.
	/// </summary>
	public virtual bool CheckOtherJumpButtons()
	{
		if ( CheckWaterJumpButton() )
			return true;

		if ( CheckAirDashButton() )
			return true;

		return false;
	}

	public virtual bool CheckJumpButton()
	{
		// Check if any other effects on jump wish to be executed.
		if ( CheckOtherJumpButtons() )
			return false;

		if ( !CanJump() )
			return false;

		Jump();
		return true;
	}

	public virtual void Jump()
	{
		SetGroundEntity( null );

		PreventBunnyJumping();
		Player.DoJumpSound( Position, Player.SurfaceData, 1 );
		Player.SetAnimParameter( "b_jump", true );

		var startz = Velocity[2];
		if ( Player.IsDucking || Player.IsDucked ) 
		{
			Velocity.z = JumpImpulse;
		}
		else
			Velocity.z += JumpImpulse;

		FinishGravity();
		OnJump( Velocity.z - startz );
	}

	public virtual void PreventBunnyJumping()
	{
		// Speed at which bunny jumping is limited
		float maxscaledspeed = MaxSpeed;
		if ( maxscaledspeed <= 0.0f )
			return;

		// Current player speed
		float spd = Velocity.Length;
		if ( spd <= maxscaledspeed )
			return;

		// Apply this cropping fraction to velocity
		float fraction = (maxscaledspeed / spd);

		Velocity *= fraction;
	}

	public virtual void OnJump(float impulse ) { }
}
