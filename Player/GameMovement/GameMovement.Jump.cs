﻿using Sandbox;
using System;

namespace Amper.Source1;

public partial class GameMovement
{
	public float JumpTime { get; set; }

	public virtual bool WishJump()
	{
		return Input.Pressed( InputButton.Jump );
	}

	public virtual bool CanJump()
	{
		if ( IsInAir )
			return false;

		if ( Player.IsDucked )
			return false;

		// Yeah why not.
		return true;
	}

	/// <summary>
	/// Returns true if we succesfully made a jump.
	/// </summary>
	public virtual bool CheckJumpButton()
	{
		if ( !CheckWaterJumpButton() )
			return false;

		if ( !CanJump() )
			return false;

		ClearGroundEntity();

		Player.DoJumpSound( Position, Player.SurfaceData, 1 );

		AddEvent( "jump" );

		float startz = Velocity.z;
		Velocity = Velocity.WithZ( JumpImpulse );

		if ( IsDucking ) 
			Velocity = Velocity.WithZ( Velocity.z + startz );

		FinishGravity();
		OnJump( Velocity.z - startz );

		return true;
	}

	public virtual float JumpImpulse => 321;
	public virtual void OnJump( float velocity ) { }
}
