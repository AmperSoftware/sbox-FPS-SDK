using Sandbox;

namespace Amper.FPS;

public enum ChatType
{
	Global,
	Team
}

partial class SDKGame
{
	[ConCmd.Server( "say_all" )]
	public static void Command_SendMessage( string message )
	{
		Current.OnChatMessageSent( ConsoleSystem.Caller, message, ChatType.Global );
	}

	[ConCmd.Server( "say_team" )]
	public static void Command_SendTeamMessage( string message )
	{
		Current.OnChatMessageSent( ConsoleSystem.Caller, message, ChatType.Team );
	}

	public virtual void OnChatMessageSent( IClient sender, string message, ChatType type )
	{
		var userName = "Server";
		if ( sender != null )
			userName = sender.Name;

		switch( type )
		{
			case ChatType.Global:
				Log.Info( $"{userName}: {message}" );
				break;

			case ChatType.Team:
				Log.Info( $"(TEAM) {userName}: {message}" );
				break;
		}
	}
}
