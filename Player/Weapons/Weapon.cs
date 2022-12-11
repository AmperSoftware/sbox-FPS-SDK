using Sandbox;
using System;

namespace Amper.FPS;

public partial class SDKWeapon : AnimatedEntity, ITeam
{
	public SDKPlayer Player => Owner as SDKPlayer;

	[Net, Predicted] public bool IsDeployed { get; set; }
	[Net] public IClient OriginalOwner { get; set; }
	[Net] public int ViewModelIndex { get; set; }
	[Net] public int SlotNumber { get; set; }
	[Net] public int TeamNumber { get; set; }

	public SDKViewModel ViewModel { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( CollisionTags.Solid );
		Tags.Add( CollisionTags.Weapon );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public virtual bool CanDeploy( SDKPlayer player ) => true;
	public virtual bool CanHolster( SDKPlayer player ) => true;

	public virtual bool CanEquip( SDKPlayer player ) => true;
	public virtual bool CanDrop( SDKPlayer player ) => true;

	public virtual bool RemoveOnRoundRestart() => false;

	public override void Simulate( IClient cl )
	{
		SimulateReload();
		SimulateAttack();

		if ( sv_debug_weapons && IsLocalPawn )
			DebugScreenText( Time.Delta );
	}

	public virtual bool ShouldAutoReload()
	{
		if ( !CanAttack() )
			return false;

		return Clip == 0 && NeedsAmmo();
	}


	/// <summary>
	/// The weapon has been picked up by someone
	/// </summary>
	public virtual void OnEquip( SDKPlayer owner )
	{
		if ( !IsValid )
			return;

		if ( !OriginalOwner.IsValid() )
			OriginalOwner = owner.Client;

		SetParent( owner, true );
		Owner = owner;
		PhysicsEnabled = false;
		TeamNumber = owner.TeamNumber;

		EnableAllCollisions = false;
		EnableDrawing = false;
	}

	/// <summary>
	/// The weapon has been dropped by the owner
	/// </summary>
	public virtual void OnDrop( SDKPlayer owner )
	{
		if ( !IsValid )
			return;

		SetParent( null );
		Owner = null;
		PhysicsEnabled = true;

		EnableDrawing = true;
		EnableAllCollisions = true;
	}

	public virtual void OnDeploy( SDKPlayer owner )
	{
		if ( !IsValid )
			return;

		var deployTime = GetDeployTime();
		var remainingTime = MathF.Max( NextAttackTime - Time.Now - deployTime, 0f );
		var remainingPrimaryTime = MathF.Max( NextPrimaryAttackTime - Time.Now - deployTime, 0f );
		var remainingSecondaryTime = MathF.Max( NextSecondaryAttackTime - Time.Now - deployTime, 0f );

		NextAttackTime = Time.Now + deployTime + remainingTime;
		NextPrimaryAttackTime = Time.Now + deployTime + remainingPrimaryTime;
		NextSecondaryAttackTime = Time.Now + deployTime + remainingSecondaryTime;

		EnableDrawing = true;
		IsDeployed = true;

		SetupViewModel();
		SetupAnimParameters();
	}

	public virtual void SetupAnimParameters()
	{
		SendAnimParameter( "b_deploy" );
	}

	public virtual void OnHolster( SDKPlayer owner )
	{
		if ( !IsValid )
			return;

		EnableDrawing = false;
		//NextAttackTime = Time.Now;
		IsDeployed = false;

		ClearViewModel();
	}

	public virtual void SetupViewModel()
	{
		ViewModel = GetViewModelEntity();
		ViewModel?.SetWeaponModel( GetViewModelPath(), this );
	}

	public virtual void ClearViewModel()
	{
		ViewModel?.ClearWeapon( this );
		ViewModel = null;
	}

	public virtual SDKViewModel GetViewModelEntity()
	{
		var player = Owner as SDKPlayer;
		return player?.GetViewModel( ViewModelIndex );
	}

	public virtual string GetViewModelPath() => "";

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

	public virtual void SendPlayerAnimParameter( string name, bool value = true ) => Player?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, int value ) => Player?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, float value ) => Player?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Vector3 value ) => Player?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Rotation value ) => Player?.SetAnimParameter( name, value );
	public virtual void SendPlayerAnimParameter( string name, Transform value ) => Player?.SetAnimParameter( name, value );

	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, bool value = true ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, int value ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, float value ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Vector3 value ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Rotation value ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }
	[ClientRpc] public virtual void SendViewModelAnimParameter( string name, Transform value ) { Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value ); }

	protected override void OnDestroy()
	{
		if ( Player.IsValid() && Player.ActiveWeapon == this )
			OnHolster( Player );

		ClearViewModel();
		base.OnDestroy();
	}

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnViewModelAnimGraphTag( string tag, AnimGraphTagEvent type ) { }

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnPlayerAnimGraphTag( string tag, AnimGraphTagEvent type ) { }

	public virtual void OnViewModelAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }
	public virtual void OnPlayerAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }

	public virtual void RenderHud( Vector2 screenSize )
	{
		var center = screenSize * .5f;
		DrawCrosshair( screenSize, center );
	}

	public virtual void DrawCrosshair( Vector2 screenSize, Vector2 center ) { }

	protected virtual void DebugScreenText( float interval ) { }
	[ConVar.Replicated] public static bool sv_debug_weapons { get; set; }

	[Event.Tick]
	void TickInternal()
	{
		Tick();
		if ( Game.IsServer ) ServerTick();
		if ( Game.IsClient ) ClientTick();
	}

	public virtual void Tick() { }
	public virtual void ClientTick() { }
	public virtual void ServerTick() { }

	[Event.Client.Frame] public virtual void Frame() { }
}
