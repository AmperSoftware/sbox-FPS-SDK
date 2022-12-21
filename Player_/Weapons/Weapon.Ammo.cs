using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKWeapon
{
	[Net, Predicted] public int Clip { get; set; }
	[Net, Predicted] public int Reserve { get; set; }

	public virtual void Regenerate()
	{
		Clip = GetClipSize();
		Reserve = GetReserveSize();
	}

	public virtual int TakeFromReserve( int ammoNeeded )
	{
		var taken = Math.Min( ammoNeeded, Reserve );
		Reserve -= taken;
		return taken;
	}

	public virtual int TakeAmmo( int amount )
	{
		if ( sv_infinite_ammo )
			return amount;

		var taken = Math.Min( amount, Clip );
		Clip -= taken;
		return taken;
	}

	public virtual bool HasAmmo() => Clip > 0 || !NeedsAmmo();
	public virtual bool NeedsAmmo() => true;
}
