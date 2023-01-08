using Sandbox;

namespace Amper.FPS;

public abstract partial class SDKGameModeLogic : Entity
{
	public SDKGameModeLogic()
	{
		EventDispatcher.Subscribe<RoundEndEvent>( RoundEnd, this );
		EventDispatcher.Subscribe<RoundActiveEvent>( RoundActivate, this );
		EventDispatcher.Subscribe<RoundRestartEvent>( RoundRestart, this );
	}

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;
	}

	public virtual void Reset() { }

	[Event.Tick.Server] public virtual void Tick() { }
	[Event.Entity.PostSpawn] public virtual void PostLevelSetup() { }

	public virtual void RoundEnd( RoundEndEvent args ) { }
	public virtual void RoundActivate( RoundActiveEvent args ) { }
	public virtual void RoundRestart( RoundRestartEvent args ) { Reset(); }
}
