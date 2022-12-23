using Sandbox;
using TFS2;

namespace Amper.FPS;

partial class BaseCombatWeapon
{
	public int ViewModelIndex { get; set; }

	public virtual void SetupViewModel()
	{
		var owner = PlayerOwner;
		if ( owner == null )
			return;

		var vm = owner.GetViewModel( ViewModelIndex );
		if ( vm == null )
			return;

		vm.SetWeapon( GetViewModel( ViewModelIndex ), this );
	}

	public virtual string GetViewModel( int index = 0 )
	{
		return "";
	}
}
