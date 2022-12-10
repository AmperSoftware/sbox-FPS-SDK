using Sandbox;

namespace Amper.FPS;

partial class SDKGame
{
	public void UpdateAllClientsData()
	{
		var clients = Game.Clients;
		foreach ( var client in clients )
		{
			var pawn = client.Pawn as SDKPlayer;
			if ( pawn == null )
				continue;

			UpdateClientData( client, pawn );
		}
	}

	public virtual void UpdateClientData( IClient client, SDKPlayer player )
	{
		client.SetValue( "f_health", player.Health );
		client.SetValue( "f_maxhealth", player.MaxHealth );
		client.SetValue( "n_teamnumber", player.TeamNumber );
		client.SetValue( "b_alive", player.IsAlive );
	}
}

public static class ClientExtensions
{
	public static float GetHealth( this IClient client ) => client.GetValue<float>( "f_health" );
	public static float GetMaxHealth( this IClient client ) => client.GetValue<float>( "f_maxhealth" );
	public static bool IsAlive( this IClient client ) => client.GetValue<bool>( "b_alive" );
	public static int GetTeamNumber( this IClient client ) => client.GetValue<int>( "n_teamnumber" );
}
