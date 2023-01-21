using Sandbox;

namespace Amper.FPS;

partial class GameMovement
{
	protected virtual void ShowDebugOverlay()
	{
		if ( sv_debug_movement && Player.Client.IsListenServerHost && Game.IsServer )
		{
			DebugOverlay.ScreenText( CreateDebugString(), new Vector2( 60, 150 ) );
		}
	}

	protected virtual string CreateDebugString()
	{
		var str = 
			$"[PLAYER]\n" +
			$"GroundEntity          {Player.GroundEntity}\n" +
			$"MoveType              {Player.MoveType}\n" +
			$"Flags                 {Player.Flags}\n" +
			$"Team                  {Player.TeamNumber}\n" +
			$"ObserverMode          {Player.ObserverMode}\n" +
			$"HoveredEntity         {Player.HoveredEntity}\n" +
			$"Last Known Area:      #{(Player.LastKnownArea?.ID.ToString()) ?? "~"}\n" +
			$"Tags:                 {string.Join( ",", Player.Tags.List )}\n" +
			$"\n" +

			$"[MOVEMENT]\n" +
			$"ForwardMove           {ForwardMove}\n" +
			$"SideMove              {SideMove}\n" +
			$"UpMove                {UpMove}\n" +
			$"Velocity              {Velocity}\n" +
			$"Speed                 {Velocity.Length}\n" +
			$"Fall Velocity         {Player.FallVelocity}\n" +
			$"\n" +

			$"[DUCK]\n" +
			$"DuckTime              {Player.DuckTime}\n" +
			$"IsDucked              {Player.IsDucked}\n" +
			$"IsDucking             {Player.IsDucking}\n" +
			$"AirDuckCount          {Player.AirDuckCount}\n" +
			$"DuckSpeed             {Player.DuckSpeed}\n" +
			$"\n" +

			$"[WATER]\n" +
			$"Water Level           {Player.WaterLevelType}\n" +
			$"WaterJumpTime         {Player.WaterJumpTime}\n" +
			$"WaterLevel            {Player.GetWaterLevel()}\n";

		return str;
	}

	[ConVar.Replicated] public static bool sv_debug_movement { get; set; }
}
