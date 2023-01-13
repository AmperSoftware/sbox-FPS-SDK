using Sandbox;
using System.Linq;

namespace Amper.FPS;

public partial class SDKGame : GameManager
{
	public new static SDKGame Current { get; set; }

	public GameMovement Movement { get; set; }
	public PostProcessingManager PostProcessingManager { get; set; }
	public NavMeshExtended NavMesh { get; set; }

	public SDKGame()
	{
		Current = this;
		Movement = new();

		if ( Game.IsClient )
		{
			PostProcessingManager = new();
		}

		if ( Game.IsServer )
		{
			NavMesh = new();
		}
	}

	public override void Spawn()
	{
		base.Spawn();

		DeclareGameTeams();
		SetupGameVariables();
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
		PostProcessingManager?.FrameSimulate();
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		DeclareGameTeams();
		SetupGameVariables();
	}

	public virtual void DeclareGameTeams()
	{
		// By default all games have these two teams.

		TeamManager.DeclareTeam( 0, "unassigned", "UNASSIGNED", Color.White, false, false );
		TeamManager.DeclareTeam( 1, "spectator", "Spectator", Color.White, false, true );
	}

	float NextTickTime { get; set; }

	[Event.Tick]
	public void TickInternal()
	{
		Upkeep();

		if ( Time.Now < NextTickTime )
			return;

		Tick();

		NextTickTime = Time.Now + 0.1f;
	}

	public virtual void Tick()
	{
		SimulateStates();

		if ( Game.IsServer )
		{
			CheckWaitingForPlayers();
			UpdateAllClientsData();
		}
	}

	public virtual void Upkeep()
	{
		NavMesh?.Update();
	}


	[ConVar.Client] public static bool cl_show_prediction_errors { get; set; }

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( Game.IsClient && cl_show_prediction_errors && !Prediction.FirstTime )
		{
			DebugOverlay.ScreenText( $"Prediction Error! Rerunning ticks... (Tick: {Time.Tick})", new Vector2( Screen.Width - 400, 120 ), 0, Color.Red, .6f );
		}
	}

	public override void ClientJoined( IClient cl )
	{
		var player = CreatePlayerForClient( cl );
		cl.Pawn = player;
		player.Respawn();
	}

	public virtual SDKPlayer CreatePlayerForClient( IClient cl ) => new SDKPlayer();

	public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
	{
		if ( client.Pawn.IsValid() )
		{
			client.Pawn.Delete();
			client.Pawn = null;
		}
	}

	public virtual void SetupGameVariables() { }

	public override void PostLevelLoaded()
	{
		CalculateObjectives();
		CreateStandardEntities();

		// NavMesh?.PrecomputeNavMesh();
	}

	/// <summary>
	/// Amount of seconds until this player is able to respawn.
	/// </summary>
	/// <param name="player"></param>
	public virtual float GetPlayerRespawnTime( SDKPlayer player ) => 0;

	/// <summary>
	/// This player was just killed.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerDeath( SDKPlayer player, ExtendedDamageInfo info ) { }

	/// <summary>
	/// This player was just hurt.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerHurt( SDKPlayer player, ExtendedDamageInfo info ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	/// <param name="player"></param>
	public virtual void PlayerRespawn( SDKPlayer player ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	public virtual void PlayerChangeTeam( SDKPlayer player, int team ) { }

	/// <summary>
	/// Create standard game entities.
	/// </summary>
	public virtual void CreateStandardEntities() { }

	/// <summary>
	/// Respawn all players.
	/// </summary>
	public virtual void RespawnPlayers( bool forceRespawn, bool teamonly = false, int team = 0 )
	{
		var players = All.OfType<SDKPlayer>().ToList();

		foreach ( var player in players )
		{
			// if we only respawn 
			if ( teamonly && player.TeamNumber != team )
				continue;

			if ( !player.IsReadyToPlay() )
				continue;

			if ( !forceRespawn )
			{
				if ( player.IsAlive )
					continue;

				if ( !AreRespawnConditionsMet( player ) )
					continue;
			}

			player.Respawn();
		}
	}

	/// <summary>
	/// Player can technically respawn, but we must wait for certain condition to happen in order to 
	/// be respawned. (i.e. respawn waves)
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public virtual bool AreRespawnConditionsMet( SDKPlayer player ) => true;
	public bool HasPlayers() => All.OfType<SDKPlayer>().Any( x => x.IsReadyToPlay() );

	public virtual float DamageForce( Vector3 size, float damage, float scale )
	{
		float force = damage * ((48 * 48 * 82) / (size.x * size.y * size.z)) * scale;

		if ( force > 1000 )
			force = 1000;

		return force;
	}

	public Vector2 ScreenSize { get; private set; }
	public override void RenderHud()
	{
		base.RenderHud();

		var player = Game.LocalPawn as SDKPlayer;
		if ( player == null )
			return;

		// Update screen size in case of resolution change

		ScreenSize = Screen.Size;
		player.RenderHud( ScreenSize );
	}

	public override void BuildInput()
	{
		Event.Run( "buildinput" );
		Game.LocalPawn?.BuildInput();
		//LastCamera?.BuildInput();
	}
}
