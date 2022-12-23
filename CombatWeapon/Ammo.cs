using Sandbox;

namespace Amper.FPS;

partial class BaseCombatWeapon
{
	[Net, Predicted] public int Ammo { get; set; }
	[Net, Predicted] public int Reserve { get; set; }

	/// <summary>
	/// Returns true if the weapon actually uses ammo
	/// </summary>
	public virtual bool UsesAmmo() => true;

	/// <summary>
	/// Returns true if the weapon currently has ammo or doesn't need ammo
	/// </summary>
	public virtual bool HasAmmo()
	{
		if ( !UsesAmmo() )
			return false;

		return Ammo > 0;
	}
}
