using Sandbox;
using System;

namespace Amper.FPS;

partial class GameMovement
{
	public const float PUNCH_DAMPING = 9;
	public const float PUNCH_SPRING_CONSTANT = 65;

	public virtual void DecayViewPunchAngle()
	{
		if ( Player.ViewPunchAngle.LengthSquared > 0.001f || Player.ViewPunchAngleVelocity.LengthSquared > 0.001f )
		{
			Player.ViewPunchAngle += Player.ViewPunchAngleVelocity * Time.Delta;
			float damping = 1 - (PUNCH_DAMPING * Time.Delta);

			if ( damping < 0 )
			{
				damping = 0;
			}

			Player.ViewPunchAngleVelocity *= damping;

			float springForceMagnitude = PUNCH_SPRING_CONSTANT * Time.Delta;
			springForceMagnitude = Math.Clamp( springForceMagnitude, 0, 2 );
			Player.ViewPunchAngleVelocity -= Player.ViewPunchAngle * springForceMagnitude;

			// don't wrap around
			Player.ViewPunchAngle = new Vector3(
				Math.Clamp( Player.ViewPunchAngle.x, -89, 89 ),
				Math.Clamp( Player.ViewPunchAngle.y, -179, 179 ),
				Math.Clamp( Player.ViewPunchAngle.z, -89, 89 ) );
		}
		else
		{
			Player.ViewPunchAngle = 0;
			Player.ViewPunchAngleVelocity = 0;
		}
	}

	public void DecayAngles( ref Vector3 angle, float exp, float lin, float time )
	{
		exp *= time;
		lin *= time;

		angle *= MathF.Exp( -exp );

		var mag = angle.Length;
		if ( mag > lin )
		{
			angle *= (1 - lin / mag);
		}
		else
		{
			angle = 0;
		}
	}
}
