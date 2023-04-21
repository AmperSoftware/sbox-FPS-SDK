using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class SDKPlayer
{
	public IEnumerable<SDKWeapon> Weapons => Children.OfType<SDKWeapon>();
	[Net] public SDKWeapon ActiveWeapon { get; set; }
	[ClientInput] public Entity RequestedActiveWeapon { get; set; }
	[Predicted] SDKWeapon LastActiveWeapon { get; set; }

	/// <summary>
	/// Can this player attack using their weapons?
	/// </summary>
	public virtual bool CanAttack() => true;

	public virtual void SimulateActiveWeapon( IClient cl )
	{
		if ( RequestedActiveWeapon != ActiveWeapon && RequestedActiveWeapon is SDKWeapon weapon )
		{
			SwitchToWeapon( weapon );
		}

		if ( LastActiveWeapon != ActiveWeapon )
		{
			OnSwitchedActiveWeapon( LastActiveWeapon, ActiveWeapon );
			LastActiveWeapon = ActiveWeapon;
		}

		if ( !ActiveWeapon.IsValid() )
			return;

		if ( ActiveWeapon.IsAuthority )
			ActiveWeapon.Simulate( cl );
	}

	public virtual void OnSwitchedActiveWeapon( SDKWeapon lastWeapon, SDKWeapon newWeapon )
	{
		if ( lastWeapon.IsValid() )
		{
			if ( IsEquipped( lastWeapon ) )
				lastWeapon.OnHolster( this );

			// If we change a weapon always clean their viewmodel, as 
			// a fallback in case OnHolster on client doesn't get called.
			// i.e. if weapon is removed serverside.
			lastWeapon?.ClearViewModel();
		}

		if ( newWeapon.IsValid() )
		{
			newWeapon.OnDeploy( this );
		}
	}

	public bool SwitchToWeapon( SDKWeapon weapon, bool switchRequested = false )
	{
		if ( !weapon.IsValid() )
			return false;
		
		// Cant switch to something we don't have equipped.
		if ( !IsEquipped( weapon ) )
			return false;

		// Check if we can switch to this weapon.
		if ( !CanDeploy( weapon ) )
			return false;

		// We already have some weapon out.
		if ( ActiveWeapon.IsValid() )
		{
			// Check if we can switch from this weapon.
			if ( !CanHolster( ActiveWeapon ) )
				return false;
		}

		ActiveWeapon = weapon;
		if(switchRequested)
			RequestedActiveWeapon = weapon;
		return true;
	}

	public virtual bool EquipWeapon( SDKWeapon weapon, bool makeActive = false )
	{
		Game.AssertServer();

		if ( !weapon.IsValid() )
			return false;

		if ( weapon.Owner != null )
			return false;

		if ( !CanEquip( weapon ) )
			return false;

		if ( !PreEquipWeapon( weapon, makeActive ) )
			return false;

		weapon.OnEquip( this );

		if ( makeActive )
			SwitchToWeapon( weapon );

		return true;
	}

	/// <summary>
	/// Prepare weapon to being equipped. Return false to prevent from being equipped.
	/// </summary>
	protected virtual bool PreEquipWeapon( SDKWeapon weapon, bool makeActive )
	{
		// See if have another weapon in this weapon's slot.
		// If we have, throw it away.
		var slotWeapon = GetWeaponInSlot( weapon.SlotNumber );
		if ( slotWeapon.IsValid() )
		{
			// Check if we can drop a weapon.
			if ( !CanDrop( weapon ) )
				return false;

			ThrowWeapon( slotWeapon );
		}

		return true;
	}

	public virtual bool CanEquip( SDKWeapon weapon ) => weapon.CanEquip( this );
	public virtual bool CanDrop( SDKWeapon weapon ) => weapon.CanDrop( this );

	public virtual bool CanDeploy( SDKWeapon weapon ) => weapon.CanDeploy( this );
	public virtual bool CanHolster( SDKWeapon weapon ) => weapon.CanHolster( this );

	public virtual bool IsEquipped( SDKWeapon weapon ) => Children.Contains( weapon );

	public virtual void DeleteAllWeapons()
	{
		var weapons = Children.OfType<SDKWeapon>().ToArray();
		foreach ( var child in weapons )
			child.Delete();
	}

	public virtual void SwitchToNextBestWeapon()
	{
		var weapons = Children.OfType<SDKWeapon>()
			.Where( x => x != ActiveWeapon && CanDeploy( x ) )
			.OrderBy( x => x.SlotNumber );

		var first = weapons.FirstOrDefault();
		if ( first.IsValid() )
		{
			SwitchToWeapon( first );
			return;
		}
	}

	public virtual Vector3 GetAttackPosition() => this.GetEyePosition();
	public virtual Rotation GetAttackRotation()
	{
		var eyeAngles = this.GetEyeRotation();
		var punch = ViewPunchAngle;
		eyeAngles *= Rotation.From( punch.x, punch.y, punch.z );
		return eyeAngles;
	}

	public T GetWeaponOfType<T>() where T : SDKWeapon => Children.OfType<T>().FirstOrDefault();
	public bool HasWeaponOfType<T>() where T : SDKWeapon => GetWeaponOfType<T>() != null;

	public List<SDKViewModel> ViewModels { get; set; } = new();

	public SDKViewModel GetViewModel( int index = 0 )
	{
		if ( !Game.IsClient )
			return null;

		if ( index < ViewModels.Count )
		{
			if ( ViewModels[index].IsValid() )
				return ViewModels[index];
		}

		var i = ViewModels.Count;
		while ( i <= index )
		{
			ViewModels.Add( null );
			i++;
		}

		var vm = CreateViewModel();
		vm.Position = Position;
		vm.Owner = this;

		ViewModels[index] = vm;
		return vm;
	}

	public virtual SDKViewModel CreateViewModel() => new SDKViewModel();

	public int GetActiveSlot()
	{
		if ( ActiveWeapon.IsValid() )
			return ActiveWeapon.SlotNumber;

		return 0;
	}

	public SDKWeapon GetWeaponInSlot( int slot )
	{
		return Children.OfType<SDKWeapon>().Where( x => x.SlotNumber == slot ).FirstOrDefault();
	}

	public virtual bool ThrowWeapon( SDKWeapon weapon, float force = 400 )
	{
		var origin = WorldSpaceBounds.Center;
		var vecForce = this.GetEyeRotation().Forward * 100 + Vector3.Up * 100;
		vecForce = vecForce.Normal;
		vecForce *= 400;

		if ( DropWeapon( weapon, origin, vecForce ) )
		{
			weapon.ApplyLocalAngularImpulse( new Vector3( Game.Random.Float( -600, 600 ), Game.Random.Float( -600, 600 ), 0 ) );
			return true;
		}

		return false;
	}

	public virtual bool DropWeapon( SDKWeapon weapon, Vector3 origin, Vector3 force )
	{
		if ( !weapon.IsValid() )
			return false;

		// We can't drop something that we dont have equipped.
		if ( !IsEquipped( weapon ) )
			return false;

		// We can't drop this weapon.
		if ( !CanDrop( weapon ) )
			return false;

		// This is the weapon we have equipped right now.
		if ( ActiveWeapon == weapon )
		{
			// Holster it immediately to negate all effects.
			ActiveWeapon.OnHolster( this );
			ActiveWeapon = null;
		}

		// Drop it.
		weapon.OnDrop( this );
		weapon.Position = origin;

		// Account our own velocity when throwing stuff.
		var velocity = force + Velocity;
		weapon.Rotation = Rotation.LookAt( velocity );
		weapon.ApplyAbsoluteImpulse( velocity );

		return true;
	}

	public virtual void RegenerateAllWeapons()
	{
		foreach ( var weapon in Weapons )
			weapon.Regenerate();
	}

	public Entity GetRenderedWeaponModel()
	{
		return IsFirstPersonMode
			? GetViewModel()
			: ActiveWeapon;
	}
}
