using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual void StartGravity()
	{
		float ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Velocity.z -= (ent_gravity * GetCurrentGravity() * 0.5f * Time.Delta);
		Velocity.z += Player.BaseVelocity.z * Time.Delta;

		var temp = Player.BaseVelocity;
		temp.z = 0;
		Player.BaseVelocity = temp;

		CheckVelocity();
	}

	public virtual void FinishGravity()
	{
		if ( Player.WaterJumpTime != 0 )
			return;

		var ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Velocity[2] -= (ent_gravity * GetCurrentGravity() * Time.Delta * 0.5f);
		CheckVelocity();
	}
}
