using Sandbox;
using System;

namespace Amper.FPS;

partial class PlayerAnimator
{
	public virtual bool LegShuffleEnabled => false;
	public virtual float LegShuffleMaxYawDiff => 45;
	public virtual float LegShuffleYawSpeed => 10;

	public virtual void UpdateLegShuffle()
	{
		// TODO:
	}
}
