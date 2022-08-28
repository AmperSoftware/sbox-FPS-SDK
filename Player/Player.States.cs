using Sandbox;
using System;

namespace Amper.FPS;

partial class Source1Player
{

	public bool InWater => WaterLevelType >= WaterLevelType.Feet;
	public bool IsGrounded => GroundEntity.IsValid();
	public bool IsInAir => !IsGrounded;
	public bool IsUnderwater => WaterLevelType >= WaterLevelType.Eyes;
	public bool IsAlive => LifeState == LifeState.Alive;
	public bool IsDead => !IsAlive;

	[Net] public bool IsInGodMode { get; set; }
	[Net] public bool IsInBuddhaMode { get; set; }
}
