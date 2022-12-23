using System;

namespace Amper.FPS;

partial class BaseCombatWeapon
{
	/// <summary>
	/// Can this weapon be deployed right now?
	/// </summary>
	public virtual bool CanDeploy( BaseCombatCharacter player ) => true;
	/// <summary>
	/// Can this weapon be holstered right now?
	/// </summary>
	public virtual bool CanHolster( BaseCombatCharacter player ) => true;

	public virtual bool OnDeploy( BaseCombatCharacter owner )
	{
		// Weapons that don't autoswitch away when they run out of ammo 
		// can still be deployed when they have no ammo.
		if ( !HasAmmo() && AllowsAutoSwitchFrom() )
			return false;

		var player = CharacterOwner;
		if ( player != null )
		{
			// Dead men deploy no weapons
			if ( !player.IsAlive )
				return false;

			SetupViewModel();

		}

		const float deployTime = 1;

		var remainingPrimaryTime = MathF.Max( NextPrimaryAttackTime - Time.Now - deployTime, 0f );
		var remainingSecondaryTime = MathF.Max( NextSecondaryAttackTime - Time.Now - deployTime, 0f );

		NextAttackTime = Time.Now + deployTime + remainingTime;
		NextPrimaryAttackTime = Time.Now + deployTime + remainingPrimaryTime;
		NextSecondaryAttackTime = Time.Now + deployTime + remainingSecondaryTime;

		EnableDrawing = true;

		SetupViewModel();
		SetupAnimParameters();
	}

	public virtual void Send
}
