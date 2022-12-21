using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.FPS;

partial class SDKWeapon
{
	public virtual bool WishSecondaryAttack() => Input.Down( InputButton.SecondaryAttack );

	/// <summary>
	/// This simulates weapon's secondary attack.
	/// Override this if need to change the overall logic of how attacks are calculated.
	/// </summary>
	public virtual void SimulateSecondaryAttack()
	{
		if ( !WishSecondaryAttack() )
			return;

		if ( !CanSecondaryAttack() )
			return;

		SecondaryAttack();
	}

	/// <summary>
	/// Can we do a secondary attack right now?
	/// </summary>
	public virtual bool CanSecondaryAttack()
	{
		if ( !CanAttack() )
			return false;

		if ( NextSecondaryAttackTime >= Time.Now )
			return false;

		return true;
	}

	/// <summary>
	/// This is what happens when succefully initiate a secpmdary attack.
	/// </summary>
	public virtual void SecondaryAttack()
	{
	}
}
