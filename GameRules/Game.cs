﻿using Sandbox;
using System.Linq;

namespace Amper.Source1;

public partial class GameRules : Game
{
	public new static GameRules Current { get; set; }

	public GameMovement Movement { get; set; }
	public PostProcessingManager PostProcessingManager { get; set; }

	public GameRules()
	{
		Current = this;

		Movement = new();
		PostProcessingManager = new();
	}

	public override void Spawn()
	{
		base.Spawn();

		DeclareGameTeams();
		SetupGameVariables();
	}

	public override void FrameSimulate( Client cl )
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
		if ( Time.Now < NextTickTime )
			return;

		Tick();

		NextTickTime = Time.Now + 0.1f;
	}

	public virtual void Tick()
	{
		SimulateStates();

		if ( IsServer )
		{
			CheckWaitingForPlayers();
			UpdateAllClientsData();
		}
	}

	[ConVar.Client] public static bool cl_show_prediction_errors { get; set; }

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( IsClient && cl_show_prediction_errors && !Prediction.FirstTime )
		{
			DebugOverlay.ScreenText( $"Prediction Error! Rerunning ticks... (Tick: {Time.Tick})", new Vector2( Screen.Width - 400, 120 ), 0, Color.Red, .6f );
		}
	}

	public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
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
	}

	/// <summary>
	/// Amount of seconds until this player is able to respawn.
	/// </summary>
	/// <param name="player"></param>
	public virtual float GetPlayerRespawnTime( Source1Player player ) => 0;

	/// <summary>
	/// This player was just killed.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerDeath( Source1Player player, ExtendedDamageInfo info ) { }

	/// <summary>
	/// This player was just hurt.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="info"></param>
	public virtual void PlayerHurt( Source1Player player, ExtendedDamageInfo info ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	/// <param name="player"></param>
	public virtual void PlayerRespawn( Source1Player player ) { }

	/// <summary>
	/// On player respawned
	/// </summary>
	public virtual void PlayerChangeTeam( Source1Player player, int team ) { }

	/// <summary>
	/// Create standard game entities.
	/// </summary>
	public virtual void CreateStandardEntities() { }

	/// <summary>
	/// Respawn all players.
	/// </summary>
	public virtual void RespawnPlayers( bool forceRespawn, bool teamonly = false, int team = 0 )
	{
		var players = All.OfType<Source1Player>().ToList();

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
	public virtual bool AreRespawnConditionsMet( Source1Player player ) => true;
	public bool HasPlayers() => All.OfType<Source1Player>().Any( x => x.IsReadyToPlay() );

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

		var player = Local.Pawn as Source1Player;
		if ( player == null )
			return;

		//
		// scale the screen using a matrix, so the scale math doesn't invade everywhere
		// (other than having to pass the new scale around)
		//

		var scale = Screen.Height / 1080.0f;
		var screenSize = Screen.Size / scale;
		var matrix = Matrix.CreateScale( scale );
		ScreenSize = screenSize;

		using ( Render.Draw2D.MatrixScope( matrix ) )
		{
			player.RenderHud( screenSize );
		}
	}

	public override void BuildInput( InputBuilder input )
	{
		Event.Run( "buildinput", input );

		Local.Pawn?.BuildInput( input );

		LastCamera?.BuildInput( input );
	}
}
