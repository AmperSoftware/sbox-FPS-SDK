using Sandbox;

namespace Amper.FPS;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class Player : CombatCharacter
{
	/// <summary>
	/// CPrediction::RunCommand
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}
}
