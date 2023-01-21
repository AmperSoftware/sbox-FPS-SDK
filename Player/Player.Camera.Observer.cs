using Sandbox;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amper.FPS;

public partial class SDKPlayer
{
	public virtual float FreezeCamDistanceMin => 96;
	public virtual float FreezeCamDistanceMax => 200;
	public virtual float ChaseDistanceMin => 16;
	public virtual float ChaseDistanceMax => 96;
	Vector3 LastDeathcamPosition { get; set; }
	bool WillPlayFreezeCamSound { get; set; }
	bool WillFreezeGameScene { get; set; }

	public void CalculateChaseCamView( )
	{
		Camera.FirstPersonViewer = null;

		var target = ObserverTarget;

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

		Camera.Position = tr.EndPosition;
	}

	public void CalculateDeathCamView()
	{
		Camera.FirstPersonViewer = null;

		var killer = LastAttacker;

		// if we dont have a killer use chase cam
		if ( killer == null || this == killer )
			return;

		var deathAnimTime = DeathAnimationTime;
		if ( TimeSinceDeath > deathAnimTime )
		{
			CalculateFreezeCamView( );
			return;
		}

		//
		// Force look at enemy
		//

		float rotLerp = TimeSinceDeath / (deathAnimTime / 2);
		rotLerp = Math.Clamp( rotLerp, 0, 1.0f );

		var toKiller = killer.GetEyePosition() - Position;
		toKiller = toKiller.Normal;

		var rotToKiller = Rotation.LookAt( toKiller );
		Camera.Rotation = Rotation.Lerp( Rotation, rotToKiller, rotLerp );

		//
		// Zoom out from our target
		//

		float posLerp = TimeSinceDeath / deathAnimTime;
		posLerp = Math.Clamp( posLerp, 0, 1.0f );

		var target = Position + -toKiller * posLerp * ChaseDistanceMax * Easing.QuadraticInOut( posLerp );

		var tr = Trace.Ray( Position, target )
			.WithAnyTags( CollisionTags.Solid )
			.Run();

		target = tr.EndPosition;
		if ( tr.Hit ) target += toKiller * 6;

		Camera.Position = target;

		// position is going to be reset next tick, remember it to use in freezecam.
		LastDeathcamPosition = Position;

		WillPlayFreezeCamSound = true;
		WillFreezeGameScene = true;
	}

	public void CalculateFreezeCamView( )
	{
		Camera.FirstPersonViewer = null;

		var killer = LastAttacker;

		if ( killer == null )
			return;

		// get time for death animation
		var deathAnimTime = DeathAnimationTime;
		// get time for freeze cam to move to the player
		var travelTime = sv_spectator_freeze_traveltime;

		// time that has passed while we are in freeze cam
		var timeInFreezeCam = TimeSinceDeath - deathAnimTime;
		timeInFreezeCam = MathF.Max( 0, timeInFreezeCam );

		// get lerp of the travel
		var travelLerp = Math.Clamp( timeInFreezeCam / travelTime, 0, 1 );

		// getting origin position and killer eye position
		var originPos = LastDeathcamPosition;
		var killerPos = killer.GetEyePosition();

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


		Camera.Position = originPos.LerpTo( targetPos, travelLerp * Easing.EaseIn( travelLerp ) );
		Camera.Rotation = Rotation.LookAt( toTarget );

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

		float fov = Camera.FieldOfView;
		if ( fov <= 0 ) fov = DefaultFieldOfView;

		if ( WillFreezeGameScene && travelLerp >= 1 )
		{
			WillFreezeGameScene = false;
			FreezeCameraPanel.Freeze( sv_spectator_freeze_time, Position, Rotation, fov );
		}
	}

	public virtual void PlayFreezeCamSound()
	{
		Sound.FromScreen( "player.freeze_cam" );
	}

	public void CalculateInEyeCamView( )
	{
		var target = ObserverTarget;

		// dont do anything, we don't have target.
		if ( target == null )
			return;

		if ( target.LifeState != LifeState.Alive )
		{
			CalculateChaseCamView( );
			return;
		}

		Camera.Position = target.GetEyePosition();
		Camera.Rotation = target.GetEyeRotation();
		Camera.FirstPersonViewer = target;
	}

	public void CalculateRoamingCamView( )
	{
		Camera.FirstPersonViewer = null;
	}
}
