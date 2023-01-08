using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual void SetGroundEntity( TraceResult? pm )
	{
		var newGround = pm.HasValue ? pm.Value.Entity : null;

		var oldGround = Player.GroundEntity;
		var vecBaseVelocity = Player.BaseVelocity;

		if ( !oldGround.IsValid() && newGround.IsValid() )
		{
			// Subtract ground velocity at instant we hit ground jumping
			vecBaseVelocity -= newGround.Velocity;
			vecBaseVelocity.z = newGround.Velocity.z;
		}
		else if ( oldGround.IsValid() && !newGround.IsValid() )
		{
			// Add in ground velocity at instant we started jumping
			vecBaseVelocity += oldGround.Velocity;
			vecBaseVelocity.z = oldGround.Velocity.z;
		}

		Player.BaseVelocity = vecBaseVelocity;
		Player.GroundEntity = newGround;

		// If we are on something...

		if ( newGround.IsValid() ) 
		{
			CategorizeGroundSurface( pm.Value );
			Velocity.z = 0;

			OnLandOnGround( newGround );
		}
		else
		{
			OnLeaveGround( oldGround );
		}
	}

	public virtual void OnLandOnGround( Entity newGround )
	{
		// Then we are not in water jump sequence
		Player.WaterJumpTime = 0;
		Player.AirDuckCount = 0;
		Player.AirDashCount = 0;
	}

	public virtual void OnLeaveGround( Entity oldGround )
	{

	}

	public virtual void CategorizeGroundSurface( TraceResult pm )
	{
		Player.SurfaceData = pm.Surface;
		Player.SurfaceFriction = pm.Surface.Friction;

		// HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
		// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
		// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.
		Player.SurfaceFriction *= 1.25f;
		if ( Player.SurfaceFriction > 1.0f )
			Player.SurfaceFriction = 1.0f;
	}
}
