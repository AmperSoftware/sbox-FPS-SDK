using Sandbox;

namespace Amper.FPS;

partial class GameRules
{
	[ConCmd.Server( "lastweapon" )]
	public static void Command_LastWeapon()
	{
		if ( ConsoleSystem.Caller?.Pawn is SDKPlayer player )
			player.SwitchToNextBestWeapon();
	}

	[ConCmd.Server( "noclip", Help = "Disable electromagnetic energies to be able to pass through walls" )]
	public static void Command_Noclip()
	{
		if ( ConsoleSystem.Caller?.Pawn is not BasePlayer player )
			return;

		// If player is not in noclip, enable it.
		if ( player.MoveType != MoveType.NoClip )
		{
			player.SetParent( null );
			player.MoveType = MoveType.NoClip;
			return;
		}

		player.MoveType = MoveType.Walk;
	}

	[ConCmd.Server( "kill", Help = "On-Demand Heart Attack" )]
	public static void Command_Suicide()
	{
		if ( ConsoleSystem.Caller?.Pawn is not BasePlayer player )
			return;

		player.CommitSuicide( explode: false );
	}

	[ConCmd.Server( "explode", Help = "Spontaneous Combustion!" )]
	public static void Command_Explode()
	{
		if ( ConsoleSystem.Caller?.Pawn is not BasePlayer player )
			return;

		player.CommitSuicide( explode: true );
	}

	[ConCmd.Admin( "respawn" )]
	public static void Command_Respawn()
	{
		if ( ConsoleSystem.Caller?.Pawn is not BasePlayer player )
			return;

		player.Respawn();
	}

	[ConCmd.Admin( "ent_create" )]
	public static void Command_Respawn( string entity )
	{
		if ( ConsoleSystem.Caller?.Pawn is not BasePlayer player )
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
}
