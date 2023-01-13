using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amper.FPS;

public partial class SDKPlayer
{
	[ConVar.Client] public static bool cl_smoothstairs { get; set; } = true;
	float m_flOldPlayerZ;
	float m_flOldPlayerViewOffsetZ;

	public virtual void SmoothViewOnStairs()
	{
		var pGroundEntity = GroundEntity;
		float flCurrentPlayerZ = Position.z;
		float flCurrentPlayerViewOffsetZ = this.GetLocalEyePosition().z;

		// Smooth out stair step ups
		// NOTE: Don't want to do this when the ground entity is moving the player
		if ( pGroundEntity.IsValid() &&
			flCurrentPlayerZ != m_flOldPlayerZ &&
			cl_smoothstairs &&
			m_flOldPlayerViewOffsetZ == flCurrentPlayerViewOffsetZ
		)
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

	public virtual void CalculateScreenShake()
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
