namespace Amper.FPS;

partial class GameMovement
{
	public virtual float AirSpeedCap => 30;

	//
	// Ducking
	//

	//
	// Jumping
	//
	public virtual float WaterJumpHeight => 8;



	public const float PLAYER_MAX_SAFE_FALL_SPEED = 580;
	public const float PLAYER_MIN_BOUNCE_SPEED = 200;

	public const float NON_JUMP_VELOCITY = 140;
	public const int MAX_CLIP_PLANES = 5;
	public const float DIST_EPSILON = 0.3125f;

	public virtual float GetCurrentGravity()
	{
		return sv_gravity * SDKGame.Current.GetGravityMultiplier();
	}
}
