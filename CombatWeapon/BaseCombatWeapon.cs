using Sandbox;

namespace Amper.FPS;

public abstract partial class BaseCombatWeapon : AnimatedEntity
{
	public BaseCombatCharacter CharacterOwner => Owner as BaseCombatCharacter;
	public BasePlayer PlayerOwner => Owner as BasePlayer;

	public virtual bool AllowsAutoSwitchFrom() { return true; }

	public void SetWeaponVisible( bool visible )
	{
		var vm = PlayerOwner?.GetViewModel( ViewModelIndex );

		EnableDrawing = visible;
		vm.EffectEntity.EnableDrawing = false;
	}

	//
	// Weapon Behavior
	//

	/// <summary>
	/// Called each frame by the player PreThink
	/// </summary>
	public virtual void ItemPreFrame() {}
	/// <summary>
	/// Called each frame by the player PostThink
	/// </summary>
	public virtual void ItemPostFrame() {}
	/// <summary>
	/// Called each frame by the player PostThink, if the player's not ready to attack yet
	/// </summary>
	public virtual void ItemBusyFrame() { }
	/// <summary>
	/// Called each frame by the player PreThink, if the weapon is holstered
	/// </summary>
	public virtual void ItemHolsterFrame() { }
	/// <summary>
	/// Called when no buttons pressed
	/// </summary>
	public virtual void WeaponIdle() { }
	/// <summary>
	/// Called when they have the attack button down
	/// but they are out of ammo. The default implementation
	/// either reloads, switches weapons, or plays an empty sound.
	/// </summary>
	public virtual void HandleFireOnEmpty() { }
	public virtual bool CanPerformSecondaryAttack() { return false; }
	public virtual bool ShouldBlockPrimaryFire() { return false; }
}
