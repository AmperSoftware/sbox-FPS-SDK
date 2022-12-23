using Sandbox;

namespace Amper.FPS;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class BasePlayer : BaseCombatCharacter
{
	public override void Spawn()
	{
		base.Spawn();

		CreateViewModels();
		MoveType = MoveType.Walk;
		Scale = 1;
	}

	/// <summary>
	/// CPrediction::RunCommand
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}
}
