using Sandbox;

namespace Amper.FPS;

public partial class PlayerSpawnPoint : Entity
{
	/// <summary>
	/// Can this player spawn on this spawn point.
	/// </summary>
	/// <param name="player"></param>
	public virtual bool CanSpawn( BasePlayer player )
	{
		return true;
	}
}
