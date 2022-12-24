using Sandbox;
using System.Linq;

namespace Amper.FPS;

public partial class GameRules : GameManager
{
	public new static GameRules Current;
	public GameMovement Movement = new();

	public GameRules()
	{
		Current = this;

		DeclareGameTeams();
		SetupGameVariables();
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
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

		Think();
		NextTickTime = Time.Now + 0.1f;
	}

	public virtual void Think()
	{
		ThinkStates();

		if ( Game.IsServer )
		{
			UpdateAllClientsData();
		}
	}

	public virtual void Upkeep()
	{
	}

	public virtual BasePlayer CreatePlayerForClient( IClient cl ) => new BasePlayer();

	public override void ClientJoined( IClient cl )
	{
		var player = CreatePlayerForClient( cl );
		cl.Pawn = player;
	}


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
		CreateStandardEntities();
	}

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
	public bool HasPlayers() => All.OfType<SDKPlayer>().Any( x => x.IsReadyToPlay() );

	public Vector2 ScreenSize;
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
	
	}

	[ConVar.Client] public static bool cl_show_prediction_errors { get; set; }
	void Simualate( IClient cl )
	{
		base.Simulate( cl );

		if ( Game.IsClient && cl_show_prediction_errors && !Prediction.FirstTime )
		{
			DebugOverlay.ScreenText( $"Prediction Error! Rerunning ticks... (Tick: {Time.Tick})", new Vector2( Screen.Width - 400, 120 ), 0, Color.Red, .6f );
		}
	}
}
