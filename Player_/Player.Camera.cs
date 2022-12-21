using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer
{
	public virtual void CalculateView()
	{
		if ( IsObserver )
		{
			CalculateObserverView();
		}
		else
		{
			CalculatePlayerView();
		}

		CalculateFieldOfView( player );
		CalculateScreenShake( player );
	}

	public virtual void CalculatePlayerView()
	{
		Camera.Position = EyePosition;
		Camera.Rotation = EyeRotation;

		SmoothViewOnStairs();

		var punch = player.ViewPunchAngle;
		Rotation *= Rotation.From( punch.x, punch.y, punch.z );
		SmoothViewOnStairs( player );

		if ( cl_thirdperson )
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


	float m_flOldPlayerZ;
	float m_flOldPlayerViewOffsetZ;
	[ConVar.Client] public static bool cl_smoothstairs { get; set; } = true;

	public virtual void SmoothViewOnStairs()
	{
		var pGroundEntity = GroundEntity;
		float flCurrentPlayerZ = Position.z;
		float flCurrentPlayerViewOffsetZ = GetLocalEyePosition().z;

		// Smooth out stair step ups
		// NOTE: Don't want to do this when the ground entity is moving the player
		if ( pGroundEntity.IsValid() && 
			flCurrentPlayerZ != m_flOldPlayerZ && 
			cl_smoothstairs &&
			m_flOldPlayerViewOffsetZ == flCurrentPlayerViewOffsetZ
		) {
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
}
