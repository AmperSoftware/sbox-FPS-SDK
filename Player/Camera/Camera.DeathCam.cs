using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKCamera
{
	Vector3 LastDeathcamPosition { get; set; }

	public void CalculateDeathCamView( SDKPlayer player )
	{
		var killer = player.LastAttacker;

		// if we dont have a killer use chase cam
		if ( killer == null || player == killer )
			return;

		var deathAnimTime = player.DeathAnimationTime;
		if ( player.TimeSinceDeath > deathAnimTime )
		{
			CalculateFreezeCamView( player );
			return;
		}

		//
		// Force look at enemy
		//

		float rotLerp = player.TimeSinceDeath / (deathAnimTime / 2);
		rotLerp = Math.Clamp( rotLerp, 0, 1.0f );

		var toKiller = killer.EyePosition - Position;
		toKiller = toKiller.Normal;

		var rotToKiller = Rotation.LookAt( toKiller );
		Rotation = Rotation.Lerp( Rotation, rotToKiller, rotLerp );

		//
		// Zoom out from our target
		//

		float posLerp = player.TimeSinceDeath / deathAnimTime;
		posLerp = Math.Clamp( posLerp, 0, 1.0f );

		var target = Position + -toKiller * posLerp * ChaseDistanceMax * Easing.QuadraticInOut( posLerp );

		var tr = Trace.Ray( Position, target )
			.WithAnyTags( CollisionTags.Solid )
			.Run();

		target = tr.EndPosition;
		if ( tr.Hit ) target += toKiller * 6;

		Position = target;

		// position is going to be reset next tick, remember it to use in freezecam.
		LastDeathcamPosition = Position;

		WillPlayFreezeCamSound = true;
		WillFreezeGameScene = true;
	}
}
