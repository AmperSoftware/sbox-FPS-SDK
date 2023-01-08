using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKCamera
{
	bool WillPlayFreezeCamSound { get; set; }
	bool WillFreezeGameScene { get; set; }

	public virtual float FreezeCamDistanceMin => 96;
	public virtual float FreezeCamDistanceMax => 200;

	public void CalculateFreezeCamView( SDKPlayer player )
	{
		var killer = player.LastAttacker;

		if ( killer == null )
			return;

		// get time for death animation
		var deathAnimTime = player.DeathAnimationTime;
		// get time for freeze cam to move to the player
		var travelTime = SDKPlayer.sv_spectator_freeze_traveltime;

		// time that has passed while we are in freeze cam
		var timeInFreezeCam = player.TimeSinceDeath - deathAnimTime;
		timeInFreezeCam = MathF.Max( 0, timeInFreezeCam );

		// get lerp of the travel
		var travelLerp = Math.Clamp( timeInFreezeCam / travelTime, 0, 1 );

		// getting origin position and killer eye position
		var originPos = LastDeathcamPosition;
		var killerPos = killer.EyePosition;

		// direction to target from us.
		var toTarget = killerPos - originPos;
		toTarget = toTarget.Normal;

		// getting distance from that we need to keep from killer's eyes.
		var distFromTarget = FreezeCamDistanceMin;

		// final position, this is where the freezecam will end.
		var targetPos = killerPos - toTarget * distFromTarget;

		//
		// making sure there are no walls in between us
		//

		var tr = Trace.Ray( killerPos, targetPos )
			.WithAnyTags( CollisionTags.Solid )
			.Ignore( killer )
			.Run();

		targetPos = tr.EndPosition;
		if ( tr.Hit ) targetPos += toTarget * MathF.Min( 5, tr.Distance );


		Position = originPos.LerpTo( targetPos, travelLerp * Easing.EaseIn( travelLerp ) );
		Rotation = Rotation.LookAt( toTarget );

		//
		// Playing freezecam sound .3s before we reach destination.
		//

		var freezeSoundLength = .3f;
		var freezeSoundStartTime = travelTime - freezeSoundLength;

		if ( WillPlayFreezeCamSound && timeInFreezeCam > freezeSoundStartTime )
		{
			WillPlayFreezeCamSound = false;
			PlayFreezeCamSound();
		}

		//
		// Freezing screen when we reach lerp 1.
		//

		float fov = FieldOfView;
		if ( fov <= 0 ) fov = DefaultFieldOfView;

		if ( WillFreezeGameScene && travelLerp >= 1 )
		{
			WillFreezeGameScene = false;
			FreezeCameraPanel.Freeze( SDKPlayer.sv_spectator_freeze_time, Position, Rotation, fov );
		}
	}

	public virtual void PlayFreezeCamSound()
	{
		Sound.FromScreen( "player.freeze_cam" );
	}
}
