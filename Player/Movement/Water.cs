using Sandbox;

namespace Amper.FPS;

public enum WaterLevelType
{
	NotInWater,
	Feet,
	Waist,
	Eyes
}

partial class GameMovement
{
	WaterLevelType LastWaterLevelType { get; set; }

	protected void CheckWaterJump()
	{
		// Determine movement angles
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out _, out _ );

		// Already water jumping.
		if ( Player.WaterJumpTime != 0 ) 
			return;

		// Don't hop out if we just jumped in
		if ( Velocity[2] < -180 ) 
			return; // only hop out if we are moving up

		// See if we are backing up
		var flatvelocity = Velocity;
		flatvelocity[2] = 0;

		// Must be moving
		var curspeed = flatvelocity.Length;
		flatvelocity = flatvelocity.Normal;

		// see if near an edge
		var flatforward = forward;
		flatforward[2] = 0;
		flatforward = flatforward.Normal;

		// Are we backing into water from steps or something?  If so, don't pop forward
		if ( curspeed != 0 && Vector3.Dot( flatvelocity, flatforward ) < 0 )
			return;

		var vecStart = Position + (GetPlayerMins() + GetPlayerMaxs()) * .5f;
		var vecEnd = vecStart + flatforward * 24;

		var tr = TraceBBox( vecStart, vecEnd );
		if ( tr.Fraction == 1 )
			return;

		vecStart.z = Position.z + GetPlayerViewOffset().z + WaterJumpHeight;
		vecEnd = vecStart + flatforward * 24;
		Player.WaterJumpVelocity = tr.Normal * -50;

		tr = TraceBBox( vecStart, vecEnd );
		if ( tr.Fraction < 1 )
			return;

		// Now trace down to see if we would actually land on a standable surface.
		vecStart = vecEnd;
		vecEnd.z -= 1024;

		tr = TraceBBox( vecStart, vecEnd );
		if ( tr.Fraction < 1 && tr.Normal.z >= 0.7f )
		{
			Velocity.z = 256;
			Player.AddFlags( PlayerFlags.FL_WATERJUMP );
			Player.WaterJumpTime = 2000;
		}
	}

	public bool InWater()
	{
		return Player.WaterLevelType > WaterLevelType.Feet;
	}

	public virtual bool CheckWater()
	{
		var vPlayerExtents = GetPlayerExtents();
		var vPlayerView = GetPlayerViewOffset();

		// Assume that we are not in water at all.
		Player.WaterLevelType = WaterLevelType.NotInWater;

		var fraction = Player.GetWaterLevel();
		var playerHeight = vPlayerExtents.z;
		var viewHeight = vPlayerView.z;

		var viewFraction = viewHeight / playerHeight;
		if ( fraction > viewFraction )
		{
			Player.WaterLevelType = WaterLevelType.Eyes;
		}
		else if ( fraction >= 0.5f )
		{
			Player.WaterLevelType = WaterLevelType.Waist;
		}
		else if ( fraction > 0 )
		{
			Player.WaterLevelType = WaterLevelType.Feet;
		}

		if ( LastWaterLevelType == WaterLevelType.NotInWater && Player.WaterLevelType != WaterLevelType.NotInWater )
		{
			Player.WaterEntryTime = Time.Now;
		}

		return Player.WaterLevelType > WaterLevelType.Feet;
	}

}
