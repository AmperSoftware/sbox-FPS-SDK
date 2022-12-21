namespace Amper.FPS;

partial class GameMovement
{
	public virtual Vector3 GetPlayerMins( bool ducked ) { return Player.GetPlayerMinsScaled( ducked ); }
	public virtual Vector3 GetPlayerMaxs( bool ducked ) { return Player.GetPlayerMaxsScaled( ducked ); }
	public virtual Vector3 GetPlayerViewOffset( bool ducked ) { return Player.GetPlayerViewOffsetScaled( ducked ); }
	public virtual Vector3 GetPlayerExtents( bool ducked ) { return Player.GetPlayerExtentsScaled( ducked ); }

	public virtual Vector3 GetPlayerMins() { return GetPlayerMins( Player.IsDucked ); }
	public virtual Vector3 GetPlayerMaxs() { return GetPlayerMaxs( Player.IsDucked ); }
	public virtual Vector3 GetPlayerViewOffset() { return GetPlayerViewOffset( Player.IsDucked ); }
	public virtual Vector3 GetPlayerExtents() { return GetPlayerExtents( Player.IsDucked ); }
}
