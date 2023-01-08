using Sandbox;

namespace Amper.FPS;

partial class SDKWeapon
{
	[Net, Predicted] public float NextAttackTime { get; set; }
	[Net, Predicted] public float NextPrimaryAttackTime { get; set; }
	[Net, Predicted] public float NextSecondaryAttackTime { get; set; }
	[Net, Predicted] public float LastAttackTime { get; set; }


	/// <summary>
	/// This simulates weapon's attack abilities.
	/// </summary>
	public virtual void SimulateAttack()
	{
		SimulatePrimaryAttack();
		SimulateSecondaryAttack();
	}

	public virtual bool CanAttack()
	{
		if ( !Owner.IsValid() )
			return false;

		if ( !SDKGame.Current.CanWeaponsAttack() )
			return false;

		if ( !Player.CanAttack() )
			return false;

		if ( NextAttackTime >= Time.Now )
			return false;

		return true;
	}

	public float CalculateNextAttackTime() => CalculateNextAttackTime( GetAttackTime() );

	public virtual float CalculateNextAttackTime( float attackTime )
	{
		// Fixes:
		// https://www.youtube.com/watch?v=7puuYqq_rgw

		var curAttack = NextPrimaryAttackTime;
		var deltaAttack = Time.Now - curAttack;

		if ( deltaAttack < 0 || deltaAttack > Game.TickInterval )
		{
			curAttack = Time.Now;
		}

		NextPrimaryAttackTime = curAttack + attackTime;
		return curAttack;
	}

	/// <summary>
	/// When weapon fired while having no ammo.
	/// </summary>
	public virtual void OnDryFire()
	{
		if ( !PlayEmptySound() )
			return;

		NextAttackTime = Time.Now + 0.2f;
	}

	/// <summary>
	/// Procedure to play empty fire sound, if game needs it.
	/// If your weapon needs dry fire sounds, play it in this function and return true.
	/// Otherwise return false.
	/// </summary>
	public virtual bool PlayEmptySound() => false;

	public virtual void SendAnimParametersOnAttack()
	{
		SendAnimParameter( "fire" );
	}


	/// <summary>
	/// Return the position in the worldspace, from which the attack is made.
	/// </summary>
	public virtual Vector3 GetAttackOrigin()
	{
		if ( Player == null )
			return Vector3.Zero;

		return Player.GetAttackPosition();
	}

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	public virtual Rotation GetAttackRotation()
	{
		if ( Player == null )
			return Rotation.Identity;

		return Player.GetAttackRotation();
	}

	public virtual Vector3 GetAttackDirection() => GetAttackRotation().Forward;

	/// <summary>
	/// Return the diretion of the attack of this weapon.
	/// </summary>
	/// <returns></returns>
	public virtual Vector3 GetAttackDirectionWithSpread( Vector2 spread )
	{
		var rotation = GetAttackRotation();

		var forward = rotation.Forward;
		var right = rotation.Right;
		var up = rotation.Up;

		var dir = forward + spread.x * right + spread.y * up;
		dir = dir.Normal;
		return dir;
	}

	public virtual Vector3 GetAttackDirectionWithSpread( float spread )
	{
		var spreadVec = Vector2.Random * spread;
		return GetAttackDirectionWithSpread( spreadVec );
	}

	public virtual bool HasEnoughAmmo( int howMuch )
	{
		if ( !NeedsAmmo() )
			return true;

		return Clip >= howMuch;
	}

	public virtual bool HasEnoughAmmoToAttack()
	{
		var ammoPerAttack = GetAmmoPerShot();
		return HasEnoughAmmo( ammoPerAttack );
	}

	/// <summary>
	/// Consume ammo for this attack.
	/// </summary>
	public virtual void ConsumeAmmoOnAttack()
	{
		if ( !NeedsAmmo() )
			return;

		// Drain ammo.
		TakeAmmo( GetAmmoPerShot() );
	}

	/// <summary>
	/// This summons all the "attack" projectiles that this weapon executes.
	/// </summary>
	public virtual void Attack()
	{
		for ( var i = 0; i < GetBulletsPerShot(); i++ )
		{
			FireBullet( GetDamage(), i );
		}
	}

	public virtual void PlayAttackSound() { }

	[ConVar.Replicated] public static bool sv_infinite_ammo { get; set; }
}
