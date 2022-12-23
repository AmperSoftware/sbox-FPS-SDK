using Sandbox;

namespace Amper.FPS;

public class BaseGameState : BaseNetworkable
{
	public virtual void OnStart() { }
	public virtual void Update() { }
	public virtual void OnEnd() { }

	public void ChangeTo<T>() where T: BaseGameState, new()
	{
		ChangeTo( new T() );
	}

	public void ChangeTo( BaseGameState instance )
	{
		GameRules.Current.State = instance;
	}
}


