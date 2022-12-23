using Sandbox;

namespace Amper.FPS;

public enum WeaponSoundType
{
	Empty,
	Single,
	Double,
	Burst,
	Reload,
	MeleeMiss,
	MeleeHit,
	MeleeHitWorld,
	Special1,
	Special2,
	Special3,
	Taunt,
	Deploy
}

partial class BaseCombatWeapon
{
	public bool SoundsEnabled { get; set; }

	public void PlayWeaponSound( WeaponSoundType type )
	{
		if ( !SoundsEnabled )
			return;

		var sound = GetSound( type );
		if ( string.IsNullOrEmpty( sound ) )
			return;

		PlaySound( sound );
	}

	public virtual string GetSound( WeaponSoundType type )
	{
		return "";
	}


	/// <summary>
	/// This will play an unprecited sound. If you're playing a sound serverside on a predicted 
	/// entity (like weapons on pawns) it will not be played on the client because it will be culled by prediction. 
	/// This function solves this.
	/// </summary>
	public Sound PlayUnpredictedSound( string name )
	{
		using ( Prediction.Off() ) return PlaySound( name );
	}

	public new Sound PlaySound( string soundName )
	{
		var originEnt = Owner ?? this;
		return Sound.FromEntity( soundName, originEnt );
	}
}
