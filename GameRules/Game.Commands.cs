using Sandbox;

namespace Amper.FPS;

partial class SDKGame
{
	[ConCmd.Server( "lastweapon" )]
	public static void Command_LastWeapon()
	{
		if ( ConsoleSystem.Caller.Pawn is SDKPlayer player )
			player.SwitchToNextBestWeapon();
	}

	[ConCmd.Admin("noclip", Help ="Disable electromagnetic energies to be able to pass through walls")]
	public static void Command_Noclip()
	{
		var client = ConsoleSystem.Caller;
		if ( !client.IsValid() )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( !player.IsValid() ) 
			return;

		// If player is not in noclip, enable it.
		if ( player.MoveType != MoveType.NoClip )
		{
			player.SetParent( null );
			player.MoveType = MoveType.NoClip;
			Log.Info( $"noclip ON for {client.Name}" );
			return;
		}

		player.MoveType = MoveType.Walk;
		Log.Info( $"noclip OFF for {client.Name}" );
	}

	[ConCmd.Server("kill", Help = "On-Demand Heart Attack")]
	public static void Command_Suicide()
	{
		var client = ConsoleSystem.Caller;
		if ( !client.IsValid() )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( !player.IsValid() )
			return;

		player.CommitSuicide( explode: false );
	}

	[ConCmd.Server( "explode", Help = "Spontaneous Combustion!" )]
	public static void Command_Explode()
	{
		var client = ConsoleSystem.Caller;
		if ( !client.IsValid() )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( !player.IsValid() )
			return;

		player.CommitSuicide( explode: true );
	}

	[ConCmd.Admin( "god" )]
	public static void Command_God()
	{
		var client = ConsoleSystem.Caller;
		if ( !client.IsValid() )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( !player.IsValid() )
			return;

		player.IsInGodMode = !player.IsInGodMode;
		Log.Info( $"God Mode {(player.IsInGodMode ? "enabled" : "disabled")} for {client.Name}" );
	}

	[ConCmd.Admin( "buddha" )]
	public static void Command_Buddha()
	{
		var client = ConsoleSystem.Caller;
		if ( client == null )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( player == null )
			return;

		player.IsInBuddhaMode = !player.IsInBuddhaMode;
		Log.Info( $"Buddha Mode {(player.IsInBuddhaMode ? "enabled" : "disabled")} for {client.Name}" );
	}

	[ConCmd.Admin( "respawn" )]
	public static void Command_Respawn()
	{
		var client = ConsoleSystem.Caller;
		if ( client == null )
			return;

		var player = client.Pawn as SDKPlayer;
		if ( player == null )
			return;

		player.Respawn();
	}

	[ConCmd.Admin( "ent_create" )]
	public static void Command_Respawn( string entity )
	{
		var client = ConsoleSystem.Caller;
		if ( client == null )
			return;

		var player = client.Pawn;
		if ( player == null )
			return;
		
		var tr = Trace.Ray( player.GetEyePosition(), player.GetEyePosition() + player.GetEyeRotation().Forward * 2000 )
			.Ignore( player )
			.Run();

		if ( !tr.Hit )
			return;

		var ent = TypeLibrary.Create<Entity>( entity );
		if ( ent == null )
			return;

		ent.Position = tr.EndPosition + Vector3.Up * 10;
	}
	[ConVar.Server] public static float sv_damageforce_scale { get; set; } = 1;
}
