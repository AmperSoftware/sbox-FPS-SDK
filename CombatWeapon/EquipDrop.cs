namespace Amper.FPS;

partial class BaseCombatWeapon
{

	/// <summary>
	/// Can this weapon be equipped right now?
	/// </summary>
	public virtual bool CanEquip( BaseCombatCharacter player ) => true;
	/// <summary>
	/// Can this weapon be dropped right now?
	/// </summary>
	public virtual bool CanDrop( BaseCombatCharacter player ) => true;
}
