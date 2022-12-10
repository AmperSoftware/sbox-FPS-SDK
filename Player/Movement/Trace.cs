using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	/// <summary>
	/// Traces the current bbox and returns the result.
	/// liftFeet will move the start position up by this amount, while keeping the top of the bbox at the same
	/// position. This is good when tracing down because you won't be tracing through the ceiling above.
	/// </summary>
	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end )
	{
		return TraceBBox( start, end, GetPlayerMins(), GetPlayerMaxs() );
	}

	/// <summary>
	/// Traces the bbox and returns the trace result.
	/// LiftFeet will move the start position up by this amount, while keeping the top of the bbox at the same 
	/// position. This is good when tracing down because you won't be tracing through the ceiling above.
	/// </summary>
	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		if( r_debug_movement_trace )
			DebugOverlay.Box( end, mins, maxs, Game.IsServer ? Color.Blue : Color.Red, 0 );

		return SetupBBoxTrace( start, end, mins, maxs ).Run();
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end )
	{
		return SetupBBoxTrace( start, end, GetPlayerMins(), GetPlayerMaxs() );
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		return Trace.Ray( start, end )
			.Size( mins, maxs )

			// Collides with:
			.WithAnyTags( CollisionTags.Solid )
			.WithAnyTags( CollisionTags.Ladder )
			.WithAnyTags( CollisionTags.Clip )
			.WithAnyTags( CollisionTags.PlayerClip )

			// Doesn't collide with:
			.WithoutTags( CollisionTags.NotSolid )
			.WithoutTags( CollisionTags.Debris )
			.WithoutTags( CollisionTags.Weapon )
			.WithoutTags( CollisionTags.Projectile )

			.Ignore( Player );
	}

	[ConVar.Client] public static bool r_debug_movement_trace { get; set; }
}
