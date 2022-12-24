using Sandbox;
using System.Linq;

namespace Amper.FPS;

partial class BaseCombatCharacter : IHasTeamNumber
{
	[Net] public int TeamNumber { get; set; }
}
