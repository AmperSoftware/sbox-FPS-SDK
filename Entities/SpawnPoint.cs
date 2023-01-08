using Sandbox;

namespace Amper.FPS;

public partial class SDKSpawnPoint : Entity
{
	/// <summary>
	/// Can this player spawn on this spawn point.
	/// </summary>
	/// <param name="player"></param>
	public virtual bool CanSpawn( SDKPlayer player )
	{
		return true;
	}
}
