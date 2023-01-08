using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class SDKGame
{
	//
	// Game Sounds
	//

	public static void PlaySoundToTeam( int team, string sound, SoundBroadcastChannel channel, bool stopprevious = true )
	{
		var clients = All.OfType<SDKPlayer>()
			.Where( x => x.TeamNumber == team )
			.Select( x => x.Client )
			.Where( x => x != null );

		PlaySoundRPC( To.Multiple( clients ), sound, channel, stopprevious );
	}

	public static void PlaySoundToAll( string sound, SoundBroadcastChannel channel, bool stopprevious = true )
	{
		PlaySoundRPC( To.Everyone, sound, channel, stopprevious );
	}

	Dictionary<SoundBroadcastChannel, Sound> BroadcastSounds { get; set; } = new();

	[ClientRpc]
	public static void PlaySoundRPC( string sound, SoundBroadcastChannel channel, bool stopprevious = true )
	{
		var snd = Sound.FromScreen( sound );

		if ( Current == null ) return;
		if ( Current.BroadcastSounds == null ) return;

		if ( stopprevious && Current.BroadcastSounds.TryGetValue( channel, out var instance ) ) instance.Stop();
		Current.BroadcastSounds[channel] = snd;
	}
}

public enum SoundBroadcastChannel
{
	Generic,
	Ambience,
	Soundtrack,
	Announcer
}
