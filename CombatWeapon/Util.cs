using Sandbox;

namespace Amper.FPS;

partial class BaseCombatWeapon
{
	public virtual void SendAnimParameter( string name, bool value = true )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendAnimParameter( string name, int value )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendAnimParameter( string name, float value )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendAnimParameter( string name, Vector3 value )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendAnimParameter( string name, Rotation value )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendAnimParameter( string name, Transform value )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendPlayerAnimParameter( string name, bool value = true ) => CharacterOwner?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, int value ) => CharacterOwner?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, float value ) => CharacterOwner?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Vector3 value ) => CharacterOwner?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Rotation value ) => CharacterOwner?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Transform value ) => CharacterOwner?.SetAnimParameter( name, value );

	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, bool value = true ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, int value ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, float value ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Vector3 value ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Rotation value ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Transform value ) { PlayerOwner?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }

}
