using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKCamera
{
	public void CalculateChaseCamView( SDKPlayer player )
	{
		var target = player.ObserverTarget;

		if ( target == null )
			return;

		// TODO:
		// VALVE:
		// If our target isn't visible, we're at a camera point of some kind.
		// Instead of letting the player rotate around an invisible point, treat
		// the point as a fixed camera.

		var specPos = target.GetEyePosition() - Rotation.Forward * 96;

		var tr = Trace.Ray( target.GetEyePosition(), specPos )
			.Ignore( target )
			.WithAnyTags( CollisionTags.Solid )
			.Run();

		Position = tr.EndPosition;
	}

	public virtual float ChaseDistanceMin => 16;
	public virtual float ChaseDistanceMax => 96;
}
