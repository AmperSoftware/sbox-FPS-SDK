namespace Amper.FPS;

partial class SDKWeapon
{
	//
	// Properties
	//

	public virtual int GetAmmoPerShot() => 1;
	public virtual int GetBulletsPerShot() => 1;
	public virtual float GetDamage() => 1;
	public virtual int GetTracerFrequency() => 1;
	public virtual int GetRange() => 4096;
	public virtual float GetSpread() => 0;
	public virtual int GetClipSize() => 1;
	public virtual int GetReserveSize() => 1;
	public virtual bool IsReloadingEntireClip() => false;

	// 
	// Timings
	//

	public virtual float GetAttackTime() => 1;
	public virtual float GetReloadStartTime() => 1;
	public virtual float GetReloadTime() => 1;
	public virtual float GetDeployTime() => 1;
}
