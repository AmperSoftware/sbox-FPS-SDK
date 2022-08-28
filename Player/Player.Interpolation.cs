using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKPlayer
{
	[ConVar.Client] public static bool cl_use_sbox_player_interpolation { get; set; }

	Vector3 LastPosition { get; set; }
	Vector3 NetworkPosition { get; set; }

	Vector3 LastEyeLocalPosition { get; set; }
	Vector3 NetworkEyeLocalPosition { get; set; }

	float InterpolationTime { get; set; }

	public void InterpolateFrame()
	{
		Host.AssertClient();

		if ( cl_use_sbox_player_interpolation )
			return;

		InterpolationTime += Time.Delta;
		float tickTime = Global.TickInterval;
		var lerpTime = Math.Clamp( InterpolationTime / tickTime, 0, 1 );

		Position = LastPosition.LerpTo( NetworkPosition, lerpTime );
		EyeLocalPosition = LastEyeLocalPosition.LerpTo( NetworkEyeLocalPosition, lerpTime );
	}

	public void StartInterpolating()
	{
		if ( !IsClient )
			return;

		if ( cl_use_sbox_player_interpolation )
			return;

		ResetInterpolation();
		InterpolationTime = 0;

		LastPosition = Position;
		LastEyeLocalPosition = EyeLocalPosition;
	}

	public void StopInterpolating()
	{
		if ( !IsClient )
			return;

		if ( cl_use_sbox_player_interpolation )
			return;

		NetworkPosition = Position;
		NetworkEyeLocalPosition = EyeLocalPosition;
	}
}
