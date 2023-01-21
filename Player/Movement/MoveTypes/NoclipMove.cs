using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	public virtual void FullObserverMove()
	{
		var mode = Player.ObserverMode;

		if ( mode == ObserverMode.InEye || mode == ObserverMode.Chase )
		{
			var target = Player.ObserverTarget;
			if ( target.IsValid() )
			{
				Position = target.Position;
				Player.ViewAngles = target.Rotation.Angles();
				Velocity = target.Velocity;
			}

			return;
		}

		if ( mode != ObserverMode.Roaming )
			// don't move in fixed or death cam mode
			return;

		if ( sv_spectator_noclip )
		{
			// roam in noclip mode
			FullNoClipMove( sv_spectator_speed, sv_spectator_accelerate );
			return;
		}

		// do a full clipped free roam move:
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );

		// Copy movement amounts
		float factor = sv_spectator_speed;
		if ( Input.Down( InputButton.Run ) )
			factor /= 2.0f;

		float fmove = ForwardMove * factor;
		float smove = SideMove * factor;

		forward = forward.Normal;
		right = right.Normal;

		var wishvel = Vector3.Zero;
		for ( int i = 0; i < 3; i++ )
			wishvel[i] = forward[i] * fmove + right[i] + smove;
		wishvel[2] += UpMove;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		//
		// Clamp to server defined max speed
		//

		float maxspeed = sv_maxvelocity;
		if ( wishspeed > maxspeed )
		{
			wishvel *= MaxSpeed / wishspeed;
			wishspeed = maxspeed;
		}

		// Set pmove velocity, give observer 50% acceration bonus
		Accelerate( wishdir, wishspeed, sv_spectator_accelerate );

		float spd = Velocity.Length;
		if ( spd < 1 )
		{
			Velocity = 0;
			return;
		}

		float friction = sv_friction;

		// Add the amount to the drop amount.
		float drop = spd * friction * Time.Delta;

		// scale the velocity
		float newspeed = spd - drop;

		if ( newspeed < 0 )
			newspeed = 0;

		// Determine proportion of old speed we are using.
		newspeed /= spd;

		Velocity *= newspeed;
		CheckVelocity();

		TryPlayerMove();
	}

	public virtual void FullNoClipMove( float factor, float maxacceleration )
	{
		float maxspeed = sv_maxspeed * factor;
		QAngle angles = Player.ViewAngles;
		angles.AngleVectors( out var forward, out var right, out var up );

		if ( Input.Down( InputButton.Run ) )
			factor /= 2.0f;

		float fmove = ForwardMove * factor;
		float smove = SideMove * factor;

		forward = forward.Normal;
		right = right.Normal;

		var wishvel = Vector3.Zero;
		for ( int i = 0; i < 3; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		//
		// Clamp to server defined max speed
		//
		if ( wishspeed > maxspeed )
		{
			wishvel *= maxspeed / wishspeed;
			wishspeed = maxspeed;
		}

		if ( maxacceleration > 0 )
		{
			// Set pmove velocity
			Accelerate( wishdir, wishspeed, maxacceleration );

			float spd = Velocity.Length;
			if ( spd < 1 )
			{
				Velocity = 0;
				return;
			}

			// Bleed off some speed, but if we have less than the bleed
			//  threshhold, bleed the theshold amount.
			float control = (spd < maxspeed / 4) ? (maxspeed / 4) : spd;

			float friction = sv_friction * Player.SurfaceFriction;

			// Add the amount to the drop amount.
			float drop = control * friction * Time.Delta;

			// scale the velocity
			float newspeed = spd - drop;
			if ( newspeed < 0 )
				newspeed = 0;

			// Determine proportion of old speed we are using.
			newspeed /= spd;
			Velocity *= newspeed;
		}
		else
		{
			Velocity = wishvel;
		}

		// Just move ( don't clip or anything )
		Position += Time.Delta * Velocity;

		// Zero out velocity if in noaccel mode
		if ( maxacceleration < 0f )
		{
			Velocity = 0;
		}
	}
}
