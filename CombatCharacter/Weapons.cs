using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class CombatCharacter
{
	[Net] public IList<SDKWeapon> Weapons { get; set; }
	[Net, Predicted] public SDKWeapon ActiveWeapon { get; set; }
	[Predicted] SDKWeapon LastActiveWeapon { get; set; }

	public virtual bool SwitchToNextBestWeapon( SDKWeapon current )
	{
		CBaseCombatWeapon* pNewWeapon = g_pGameRules->GetNextBestWeapon( this, pCurrent );

		if ( (pNewWeapon != NULL) && (pNewWeapon != pCurrent) )
		{
			return Weapon_Switch( pNewWeapon );
		}

		return false;
	}

	public virtual SDKWeapon GetNextBestWeapon( SDKWeapon current )
	{

	}
}
