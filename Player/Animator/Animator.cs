using System;
using Sandbox;

namespace Amper.FPS;

public partial class PlayerAnimator : BaseNetworkable
{
	protected SDKPlayer Player;

	Rotation EyeRotation;

	public void Simulate( SDKPlayer player )
	{
		Player = player;
		Update();
	}

	public virtual void Update()
	{
		SetAnimParameter( "b_grounded", Player.IsGrounded );
		SetAnimParameter( "b_swimming", Player.IsUnderwater );

		UpdateMovement();

		// Update player's rotation, but also remember and preserve player's
		// eye rotation so it doesn't spin when rotation is applied.
		EyeRotation = Player.GetEyeRotation();
		UpdateRotation();
		//Player.GetEyeRotation() = EyeRotation;

		UpdateLookAt();
		UpdateDucking();
	}

	public virtual Rotation GetIdealRotation()
	{
		return Rotation.LookAt( Player.GetEyeRotation().Forward.WithZ( 0 ).Normal, Vector3.Up );
	}

	public virtual void UpdateRotation()
	{
		if ( LegShuffleEnabled )
		{
			UpdateLegShuffle();
			return;
		}

		var idealRotation = GetIdealRotation();

		// If we're moving, rotate to our ideal rotation
		Player.Rotation = Rotation.Slerp( Player.Rotation, idealRotation, Time.Delta * 10 );
		// Clamp the foot rotation to within 90 degrees of the ideal rotation
		Player.Rotation = Player.Rotation.Clamp( idealRotation, 60 );
	}

	public virtual void UpdateLookAt()
	{
		float pitch = -Player.GetEyeRotation().Pitch();
		float yaw = Player.GetEyeRotation().Yaw();
		Vector3 lookAtPos = Player.GetEyePosition() + Player.GetEyeRotation().Forward * 200;

		SetAnimParameter( "body_pitch", pitch );
		SetAnimParameter( "body_yaw", yaw );
		SetLookAt( "aim_body", lookAtPos );
	}

	public virtual void UpdateMovement()
	{
		var velocity = Player.Velocity;
		var forward = Player.Rotation.Forward.Dot( velocity );
		var sideward = Player.Rotation.Right.Dot( velocity );

		var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

		SetAnimParameter( "move_direction", angle );
		SetAnimParameter( "move_speed", velocity.Length );
		SetAnimParameter( "move_groundspeed", velocity.WithZ( 0 ).Length );

		SetAnimParameter( "move_y", sideward );
		SetAnimParameter( "move_x", forward );
	}

	public virtual void UpdateDucking()
	{
		SetAnimParameter( "f_duck", Player.DuckProgress );
		SetAnimParameter( "b_ducked", Player.IsDucked );
		SetAnimParameter( "b_crouch", Player.IsDucked );
	}
}
