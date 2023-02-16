using Sandbox;
using Sandbox.Diagnostics;
using System;

namespace Amper.FPS;

partial class SDKPlayer
{
	[ConVar.Client] public static float cl_thirdperson_pitch { get; set; } = 0;
	[ConVar.Client] public static float cl_thirdperson_yaw { get; set; } = 0;
	[ConVar.Client] public static float cl_thirdperson_roll { get; set; } = 0;
	[ConVar.Client] public static float cl_thirdperson_distance { get; set; } = 120;

	/// <summary>
	/// Is this player in third person?
	/// </summary>
	public virtual bool IsThirdPerson => false;
	public virtual void CalculateView()
	{
		if ( IsObserver )
		{
			CalculateObserverView();
		}
		else
		{
			CalculatePlayerView();
		}

		CalculateFieldOfView();
		CalculateScreenShake();
	}

	public virtual void CalculatePlayerView()
	{	
		Camera.Position = this.GetEyePosition();
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FirstPersonViewer = this;

		SmoothViewOnStairs();
		
		var punch = ViewPunchAngle;
		Camera.Rotation *= Rotation.From( punch.x, punch.y, punch.z );
		SmoothViewOnStairs();

		if ( IsThirdPerson )
		{
			var angles = (QAngle)Rotation;
			angles.x += cl_thirdperson_pitch;
			angles.y += cl_thirdperson_yaw;
			angles.z += cl_thirdperson_roll;
			Camera.Rotation = angles;

			var tpPos = Position - Rotation.Forward * cl_thirdperson_distance;
			var tr = Trace.Ray( Position, tpPos )
				.Size( 5 )
				.WorldOnly()
				.Run();

			Camera.Position = tr.EndPosition;
		}
	}

	public virtual void CalculateObserverView( )
	{
		switch ( ObserverMode )
		{
			case ObserverMode.Roaming:
				CalculateRoamingCamView(  );
				break;

			case ObserverMode.InEye:
				CalculateInEyeCamView(  );
				break;

			case ObserverMode.Chase:
				CalculateChaseCamView(  );
				break;

			case ObserverMode.Deathcam:
				CalculateDeathCamView(  );
				break;
		}
	}
}
