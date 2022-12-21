using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKCamera
{
	public void CalculateInEyeCamView( SDKPlayer player )
	{
		var target = player.ObserverTarget;

		// dont do anything, we don't have target.
		if ( target == null )
			return;

		if ( target.LifeState != LifeState.Alive )
		{
			CalculateChaseCamView( player );
			return;
		}

		Position = target.EyePosition;
		Rotation = target.EyeRotation;
		Viewer = target;
	}
}
