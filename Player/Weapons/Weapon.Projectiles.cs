using Sandbox;

namespace Amper.FPS;

public partial class SDKWeapon
{
	public T FireProjectile<T>( Vector3 origin, Vector3 velocity, float damage ) where T : Projectile, new()
	{
		return Projectile.Create<T>( origin, velocity, Owner, this, damage );
	}

	public virtual void GetProjectileFireSetup( Vector3 offset, out Vector3 origin, out Vector3 direction, bool hitTeammates = false, float maxRange = 2000 )
	{
		var attackRotation = GetAttackRotation();
		var attackOrigin = GetAttackOrigin();

		var forward = attackRotation.Forward;
		var right = attackRotation.Right;
		var up = attackRotation.Up;

		// Trace the point at which the attacker is currently looking.
		var spread = GetSpread();
		var trDirection = GetAttackDirectionWithSpread( spread );
		var trTarget = attackOrigin + trDirection * maxRange;

		// Trace forward and find what's in front of us, and aim at that
		var tr = SetupFireBulletTrace( attackOrigin, trTarget ).Run();

		// Calculate the initial projectile setup
		origin = attackOrigin + forward * offset.x + right * offset.y + up * offset.z;
		direction = tr.EndPosition - attackOrigin;

		// Find angles that will get us to our desired end point
		// Only use the trace end if it wasn't too close, which results
		// in visually bizarre forward angles
		if ( tr.Fraction <= 0.1f )
			direction = trTarget - attackOrigin;

		direction = direction.Normal;
	}
}
