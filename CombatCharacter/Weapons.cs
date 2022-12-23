using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class BaseCombatCharacter
{
	[Net] 
	public IList<BaseCombatWeapon> Weapons { get; set; }
	[Net, Predicted] 
	public BaseCombatWeapon ActiveWeapon { get; set; }
	[Predicted] 
	BaseCombatWeapon LastActiveWeapon { get; set; }
	[Net, Predicted]
	public float NextAttackTime { get; set; }

	public virtual bool SwitchToNextBestWeapon( BaseCombatWeapon current )
	{
		CBaseCombatWeapon* pNewWeapon = g_pGameRules->GetNextBestWeapon( this, pCurrent );

		if ( (pNewWeapon != NULL) && (pNewWeapon != pCurrent) )
		{
			return Weapon_Switch( pNewWeapon );
		}

		return false;
	}

	public virtual BaseCombatWeapon GetNextBestWeapon( BaseCombatWeapon current )
	{

	}
}
