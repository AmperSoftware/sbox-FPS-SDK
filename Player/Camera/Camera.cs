using Sandbox;
using System;

namespace Amper.FPS;

public partial class SDKCamera : CameraMode
{
	public override void Build( ref CameraSetup camSetup )
	{
		var player = SDKPlayer.LocalPlayer;
		if ( player == null )
			return;

		DefaultFieldOfView = camSetup.FieldOfView;

		base.Build( ref camSetup );

		camSetup.ViewModel.FieldOfView = ViewModelFieldOfView;
	}

	public override void Update()
	{
		var player = SDKPlayer.LocalPlayer;
		if ( player == null ) 
			return;

		//
		// Reset default values
		//

		Viewer = player;
		Position = player.EyePosition;
		Rotation = player.EyeRotation;
		ZNear = 1;
		FieldOfView = 0;
		ViewModelFieldOfView = cl_viewmodel_fov;

		CalculateView( player );

		LastFieldOfView = FieldOfView;
	}

	public virtual void CalculateView( SDKPlayer player )
	{
		if ( player.IsObserver )
		{
			CalculateObserverView( player );
		}
		else
		{
			CalculatePlayerView( player );
		}

		CalculateFieldOfView( player );
		CalculateScreenShake( player );
	}


	public virtual void CalculatePlayerView( SDKPlayer player )
	{
		var punch = player.ViewPunchAngle;
		Rotation *= Rotation.From( punch.x, punch.y, punch.z );
		SmoothViewOnStairs( player );

		if( cl_thirdperson )
		{
			Viewer = null;

			var angles = (QAngle)Rotation;
			angles.x += cl_thirdperson_pitch;
			angles.y += cl_thirdperson_yaw;
			angles.z += cl_thirdperson_roll;
			Rotation = angles;

			var tpPos = Position - Rotation.Forward * cl_thirdperson_distance;
			var tr = Trace.Ray( Position, tpPos )
				.Size( 5 )
				.WorldOnly()
				.Run();

			Position = tr.EndPosition;
		}
	}

	[ConVar.Client] public static float cl_thirdperson_pitch { get; set; } = 0;
	[ConVar.Client] public static float cl_thirdperson_yaw { get; set; } = 0;
	[ConVar.Client] public static float cl_thirdperson_roll { get; set; } = 0;

	[ConVar.Client] public static bool cl_thirdperson { get; set; }
	[ConVar.Client] public static float cl_thirdperson_distance { get; set; } = 120;

	public virtual void CalculateObserverView( SDKPlayer player )
	{
		switch( player.ObserverMode )
		{
			case ObserverMode.Roaming:
				CalculateRoamingCamView( player );
				break;

			case ObserverMode.InEye:
				CalculateInEyeCamView( player );
				break;

			case ObserverMode.Chase:
				CalculateChaseCamView( player );
				break;

			case ObserverMode.Deathcam:
				CalculateDeathCamView( player );
				break;
		}
	}


	float m_flOldPlayerZ;
	float m_flOldPlayerViewOffsetZ;
	[ConVar.Client] public static bool cl_smoothstairs { get; set; } = true;

	public virtual void SmoothViewOnStairs( SDKPlayer player )
	{
		var pGroundEntity = player.GroundEntity;
		float flCurrentPlayerZ = player.Position.z;
		float flCurrentPlayerViewOffsetZ = player.EyeLocalPosition.z;

		// Smooth out stair step ups
		// NOTE: Don't want to do this when the ground entity is moving the player
		if ( pGroundEntity.IsValid() && (flCurrentPlayerZ != m_flOldPlayerZ) && cl_smoothstairs &&
			 m_flOldPlayerViewOffsetZ == flCurrentPlayerViewOffsetZ )
		{
			int dir = (flCurrentPlayerZ > m_flOldPlayerZ) ? 1 : -1;

			float steptime = Time.Delta;
			if ( steptime < 0 )
			{
				steptime = 0;
			}

			m_flOldPlayerZ += steptime * 150 * dir;

			const float stepSize = 18.0f;

			if ( dir > 0 )
			{
				if ( m_flOldPlayerZ > flCurrentPlayerZ )
				{
					m_flOldPlayerZ = flCurrentPlayerZ;
				}
				if ( flCurrentPlayerZ - m_flOldPlayerZ > stepSize )
				{
					m_flOldPlayerZ = flCurrentPlayerZ - stepSize;
				}
			}
			else
			{
				if ( m_flOldPlayerZ < flCurrentPlayerZ )
				{
					m_flOldPlayerZ = flCurrentPlayerZ;
				}
				if ( flCurrentPlayerZ - m_flOldPlayerZ < -stepSize )
				{
					m_flOldPlayerZ = flCurrentPlayerZ + stepSize;
				}
			}

			Position += Vector3.Up * (m_flOldPlayerZ - flCurrentPlayerZ);
		}
		else
		{
			m_flOldPlayerZ = flCurrentPlayerZ;
			m_flOldPlayerViewOffsetZ = flCurrentPlayerViewOffsetZ;
		}
	}

	public virtual void CalculateScreenShake( SDKPlayer player )
	{
		if ( !Game.IsClient )
			return;

		Vector3 shakeAppliedOffset = 0;

		for ( var i = ScreenShake.All.Count - 1; i >= 0; i-- )
		{
			var shake = ScreenShake.All[i];
			if ( shake.EndTime == 0 )
			{
				// Shouldn't be any such shakes in the list.
				Assert.True( false );
				continue;
			}

			if ( Time.Now > shake.EndTime
				|| shake.Duration <= 0 
				|| shake.Amplitude <= 0 
				|| shake.Frequency <= 0 )
			{
				ScreenShake.All.RemoveAt( i );
				continue;
			}

			if ( Time.Now > shake.NextShake )
			{
				shake.NextShake = Time.Now + (1f / shake.Frequency);
				shake.Offset = Vector3.Random * shake.Amplitude;
			}

			// Ramp down amplitude over duration (fraction goes from 1 to 0 linearly with slope 1/duration)
			var fraction = (shake.EndTime - Time.Now) / shake.Duration;
			// Ramp up frequency over duration
			var freq = (fraction > 0) ? shake.Frequency / fraction : 0;

			// square fraction to approach zero more quickly
			fraction *= fraction;

			var angle = Time.Now * freq;
			if ( angle > float.MaxValue ) 
				angle = float.MaxValue;

			fraction = fraction * MathF.Sin( angle );
			shakeAppliedOffset += shake.Offset * fraction;

			shake.Amplitude -= shake.Amplitude * (Time.Delta / (shake.Duration * shake.Frequency));
		}

		Position += shakeAppliedOffset;

		// TODO:
		// Controller rumble?
	}
}
