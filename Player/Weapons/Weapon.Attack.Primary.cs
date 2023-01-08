using Sandbox;

namespace Amper.FPS;

partial class SDKWeapon
{
	public virtual bool WishPrimaryAttack() => Input.Down( InputButton.PrimaryAttack );

	/// <summary>
	/// This simulates weapon's primary attack.
	/// Override this if need to change the overall logic of how attacks are calculated.
	/// </summary>
	public virtual void SimulatePrimaryAttack()
	{
		if ( !WishPrimaryAttack() )
			return;

		if ( !CanPrimaryAttack() )
			return;

		PrimaryAttack();
	}

	/// <summary>
	/// Can we do a primary attack right now?
	/// </summary>
	public virtual bool CanPrimaryAttack()
	{
		if ( !CanAttack() )
			return false;

		if ( NextPrimaryAttackTime >= Time.Now )
			return false;

		return true;
	}

	/// <summary>
	/// This is what happens when succefully initiate a primary attack.
	/// This play the required animations, sounds, consumes ammo and calculates next attack time.
	/// If you wish to change what the attack actually does (i.e. Fires a Rocket instead of a Bullet), override Attack().
	/// </summary>
	public virtual void PrimaryAttack()
	{
		// Handle dry fire, if we don't have any ammo.
		if ( !HasEnoughAmmoToAttack() )
		{
			// Play some dry fire effects.
			OnDryFire();
			return;
		}

		// Note when we last fired.
		LastAttackTime = Time.Now;

		// Calculate when we need to attack next.
		CalculateNextAttackTime();
		// Consume ammo for this attack.
		ConsumeAmmoOnAttack();
		// Stop reloading if we are firing already.
		StopReload();


		Attack();


		// Adds recoil after shot.
		DoRecoil();
		// Play the appropriate attack animations.
		SendAnimParametersOnAttack();
		// Creates a muzzle particle effect.
		CreateMuzzleFlash();
		// Play the appropriate sound.
		PlayAttackSound();
	}
}
