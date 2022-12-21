using Sandbox;

namespace Amper.FPS;

partial class SDKPlayer
{
	[Net, Predicted] public float ForcedFieldOfView { get; private set; }
	[Net, Predicted] public Entity ForcedFieldOfViewRequester { get; private set; }
	[Net, Predicted] public float ForcedFieldOfViewChangeTime { get; set; }
	[Net, Predicted] public float? ForcedFieldOfViewStartWith { get; set; }

	/// <summary>
	/// Requests a change of the FOV with a smooth ease in transition.
	/// </summary>
	public void SetFieldOfView( Entity requester, float fov, float speed = 0, float startWith = -1 )
	{
		if ( fov > 0 && !requester.IsValid() )
		{
			Log.Error( "SetFieldOfView - requester must be set to a valid entity." );
			return;
		}

		ForcedFieldOfView = fov;
		ForcedFieldOfViewChangeTime = speed;
		ForcedFieldOfViewRequester = requester;
		ForcedFieldOfViewStartWith = startWith > 0 ? startWith : null;
	}

	public void ResetFieldOfView( float speed = 0, float startWith = -1 )
	{
		SetFieldOfView( null, 0, speed, startWith );
	}

	/// <summary>
	/// If current field of view is requested by this entity, we will reset it.
	/// </summary>
	public void ResetFieldOfViewFromRequester( Entity requester, float speed = 0, float startWith = -1 )
	{
		if ( ForcedFieldOfViewRequester != requester )
			return;

		ResetFieldOfView( speed, startWith );
	}
}
