using Sandbox;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Amper.FPS;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class SDKPlayer : AnimatedEntity, IHasMaxHealth, IAcceptsExtendedDamageInfo
{
	public virtual Vector3 EyePosition => Source1Extensions.GetEyePosition( this );
	public virtual Rotation EyeRotation => Source1Extensions.GetEyeRotation( this );
	public static SDKPlayer LocalPlayer => Game.LocalPawn as SDKPlayer;

	public override void Spawn()
	{
		base.Spawn();

		Animator = new PlayerAnimator();
		CameraMode = new SDKCamera();

		TeamNumber = 0;
		LastObserverMode = ObserverMode.Chase;
	}

	[Net] public PlayerAnimator Animator { get; set; }
	public CameraMode CameraMode
	{
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
	}

	[Net] public float MaxHealth { get; set; }

	// These are from Entity, but they're not networked by default.
	// Client needs to be aware about these things.
	[Net] public new Entity LastAttacker { get; set; }
	[Net] public new Entity LastAttackerWeapon { get; set; }

	public virtual float GetMaxHealth() => 100;

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Animator?.Simulate( this );
		SDKGame.Current.Movement?.FrameSimulate( this );
		ActiveWeapon?.FrameSimulate( cl );

		InterpolateFrame();
	}

	/// <summary>
	/// Code runs here on the SERVER and SIMULATED CLIENT ONLY. Use this for code that
	/// relies on client's input.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		if ( IsObserver )
			SimulateObserver();

		//
		// Movements
		//

		UpdateMaxSpeed();
		SimulateMovement();
		Animator?.Simulate( this );

		SimulateActiveWeapon( cl );
		SimulatePassiveChildren( cl );

		SimulateHover();
		SimulateUse();
	}

	/// <summary>
	/// Code runs here on BOTH CLIENT AND SERVER for ALL CLIENTS. You want to put here stuff that 
	/// doesn't rely on client's input.
	/// </summary>
	[Event.Tick]
	public virtual void Tick()
	{
		UpdateLastKnownArea();
		DrawDebugPredictionHistory();
	}

	public virtual void SimulateMovement()
	{
		StartInterpolating();
		SDKGame.Current.Movement?.Simulate( this );
		StopInterpolating();
	}

	public virtual void Respawn()
	{
		//
		// Tags
		//
		Tags.Clear();
		Tags.Add( CollisionTags.Solid );
		Tags.Add( CollisionTags.Player );
		Tags.Add( TeamManager.GetTag( TeamNumber ) );

		//
		// Life State
		//
		LifeState = LifeState.Alive;
		Health = GetMaxHealth();
		MaxHealth = Health;
		TimeSinceRespawned = 0;
		EnableLagCompensation = true;

		LastAttacker = null;
		LastAttackerWeapon = null;
		LastDamageInfo = default;

		//
		// Rendering
		//
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;
		UseAnimGraph = true;

		//
		// Movement
		//
		Velocity = Vector3.Zero;
		MoveType = SDKMoveType.Walk;
		FallVelocity = 0;
		BaseVelocity = 0;
		UpdateMaxSpeed();

		//
		// Teamplay
		// 
		if ( TeamManager.IsPlayable( TeamNumber ) ) StopObserverMode();
		else StartObserverMode( LastObserverMode );

		EnableHitboxes = true;
		SetCollisionBounds( GetPlayerMins( false ), GetPlayerMaxs( false ) );

		//
		// Misc
		//
		TimeSinceSprayed = sv_spray_cooldown + 1;

		// move the player to the spawn point
		SDKGame.Current.FindAndMovePlayerToSpawnPoint( this );
		ResetInterpolation();

		if ( !IsObserver )
		{
			// let SDKGame know that we have respawned.
			SDKGame.Current.PlayerRespawn( this );
		}
	}

	public float GetFOV()
	{
		var camFov = ((SDKCamera)CameraMode).FieldOfView;
		if ( camFov > 0 )
			return camFov;

		// Fallback to 90, this is most likely just bots.
		return 90;
	}

	public override void OnKilled()
	{
		DeleteAllChildren();

		UseAnimGraph = false;
		EnableAllCollisions = false;
		EnableDrawing = false;
		TimeSinceDeath = 0;
		LifeState = LifeState.Dead;

		StopUsing();
		StartObserverMode( ObserverMode.Deathcam );

		OnKilledRPC();

		SDKGame.Current.PlayerDeath( this, LastDamageInfo );
	}

	public void DeleteAllChildren()
	{
		for ( var i = Children.Count - 1; i >= 0; i-- )
		{
			var child = Children[i];
			if ( !child.IsValid() )
				continue;

			child.Delete();
		}
	}

	[ClientRpc]
	void OnKilledRPC()
	{
		OnKilledEffects();
	}

	public virtual void OnKilledEffects() { }

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );
		SetCollisionBounds( GetPlayerMins( false ), GetPlayerMaxs( false ) );
	}

	public void SetCollisionBounds( Vector3 mins, Vector3 maxs )
	{
		var lastEnableHitboxes = EnableHitboxes;
		var lastMoveType = MoveType;

		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, mins, maxs );

		MoveType = lastMoveType;
		EnableHitboxes = lastEnableHitboxes;

		using ( Prediction.Off() )
			if ( IsServer ) SetCollisionBoundsClient( mins, maxs );
	}

	[ClientRpc]
	void SetCollisionBoundsClient( Vector3 mins, Vector3 maxs )
	{
		SetCollisionBounds( mins, maxs );
	}


	[Net] public TimeSince TimeSinceRespawned { get; set; }
	[Net] public TimeSince TimeSinceDeath { get; set; }
	[Net] public TimeSince TimeSinceTakeDamage { get; set; }

	public ExtendedDamageInfo LastDamageInfo { get; set; }


	public void UpdateMaxSpeed()
	{
		MaxSpeed = CalculateMaxSpeed();

		if ( MaxSpeed <= 0 )
			Velocity = 0;
	}

	/// <summary>
	/// Called before movement is calculated, we update our max speed values based on current effects.
	/// I.e. if we're sprinting.
	/// </summary>
	public virtual float CalculateMaxSpeed() => GameMovement.sv_maxspeed;

	public virtual void SimulatePassiveChildren( IClient client )
	{
		var children = Children.OfType<IPassiveChild>().ToArray();

		foreach ( var child in children )
		{
			child.PassiveSimulate( client );
		}
	}


	public virtual bool IsReadyToPlay() => TeamManager.IsPlayable( TeamNumber );

	public virtual void CommitSuicide( bool explode = false )
	{
		if ( !IsAlive )
			return;
		
		Health = 0;
		List<string> tags = new() { DamageFlags.Generic };

		if ( explode )
		{
			// If we set to explode ourselves, gib!
			tags.Add( DamageFlags.Blast );
			tags.Add( DamageFlags.AlwaysGib);
		}

		var info = ExtendedDamageInfo.Create( 1 )
			.WithAttacker( this )
			.WithInflictor( this )
			.WithAllPositions( Position )
			.WithTags( tags );

		TakeDamage( info );
	}

	public virtual float DuckingSpeedModifier => 0.33f;

	/// <summary>
	/// Called from the gamemode, clientside only.
	/// </summary>
	public override void BuildInput()
	{
		if ( Input.StopProcessing )
			return;

		if ( _forceViewAngles.HasValue )
		{
			Input.AnalogLook = _forceViewAngles.Value;
			_forceViewAngles = null;
		}

		ActiveWeapon?.BuildInput();

		if ( Input.StopProcessing )
			return;
	}

	public virtual void AttemptRespawn()
	{
		// See if we're allowed to respawn right now.
		if ( !SDKGame.Current.AreRespawnsAllowed() )
			return;

		// team is not allowed to respawn right now.
		if ( !SDKGame.Current.CanTeamRespawn( TeamNumber ) )
			return;

		// can the player respawn right now.
		if ( !SDKGame.Current.CanPlayerRespawn( this ) )
			return;

		Respawn();
	}

	protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
	{
		ActiveWeapon?.OnPlayerAnimGraphTag( tag, fireMode );
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		ActiveWeapon?.OnPlayerAnimEventGeneric( name, intData, floatData, vectorData, stringData );
	}

	public virtual void RenderHud( Vector2 screenSize )
	{
		ActiveWeapon?.RenderHud( screenSize );
	}

	[ConVar.Replicated] public static bool r_debug_prediction_history { get; set; }

	private void DrawDebugPredictionHistory()
	{
		if ( !r_debug_prediction_history )
			return;

		if ( IsClient )
		{
			if ( Prediction.FirstTime )
			{
				DebugOverlay.Box( this, Color.Green, .1f );
			}
			else
			{
				DebugOverlay.Box( this, Color.Yellow, .1f );
			}
		}
		else
		{
			DebugOverlay.Box( this, Color.Red, .1f );
		}
	}

	public NavArea LastKnownArea;

	public void UpdateLastKnownArea()
	{
		if ( !IsServer )
			return;

		if ( !NavMesh.IsLoaded )
		{
			ClearLastKnownArea();
			return;
		}

		var flags = GetNavAreaFlags.CheckLineOfSight | GetNavAreaFlags.CheckGround;
		var area = NavArea.GetClosestNav( Position, NavAgentHull.Default, flags, 50 );
		if ( area == null )
			return;

		if ( !IsAreaTraversable( area ) )
			return;

		if ( area != LastKnownArea )
		{
			if ( LastKnownArea != null )
			{
				// m_lastNavArea->DecrementPlayerCount( m_registeredNavTeam, entindex() );
				// m_lastNavArea->OnExit( this, NULL );
			}

			// RegisteredNavTeam = TeamNumber;
			// area->IncrementPlayerCount( m_registeredNavTeam, entindex() );
			// area->OnEnter( this, NULL );

			OnNavAreaChanged( area, LastKnownArea );
			LastKnownArea = area;
		}
	}

	public void ClearLastKnownArea()
	{
		OnNavAreaChanged( null, LastKnownArea );

		if ( LastKnownArea != null )
		{
			// m_lastNavArea->DecrementPlayerCount( m_registeredNavTeam, entindex() );
			// m_lastNavArea->OnExit( this, NULL );
			LastKnownArea = null;
		}
	}

	public virtual bool IsAreaTraversable( NavArea area ) => true;
	public virtual void OnNavAreaChanged( NavArea enteredArea, NavArea leftArea ) { }
}
