using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKCamera
{
	protected float DefaultFieldOfView { get; private set; }
	protected float LastFieldOfView { get; set; }

	protected float DesiredFieldOfView { get; private set; }
	protected float LastDesiredFieldOfView { get; private set; }
	protected float FieldOfViewChangeTime { get; private set; }

	protected float FieldOfViewAnimateStart { get; private set; }
	protected TimeSince TimeSinceFieldOfViewAnimateStart { get; private set; }

	public void RetrieveFieldOfViewFromPlayer( SDKPlayer player )
	{
		//
		// Desired FOV Value
		//

		DesiredFieldOfView = DefaultFieldOfView;
		if ( player.ForcedFieldOfView > 0 )
			DesiredFieldOfView = player.ForcedFieldOfView;

		//
		// Desired FOV Change Time
		//

		FieldOfViewChangeTime = 0;
		if ( player.ForcedFieldOfViewChangeTime > 0 )
		{
			// If our fov change time is set to something, but we've already reached out desired FOV, then reset the speed to zero.
			// We will be changing fov instantly until we're set speed again.
			if ( LastFieldOfView == DesiredFieldOfView && player.ForcedFieldOfViewChangeTime > 0 )
				player.ForcedFieldOfViewChangeTime = 0;

			FieldOfViewChangeTime = player.ForcedFieldOfViewChangeTime;
		}
	}

	public virtual void CalculateFieldOfView( SDKPlayer player )
	{
		if ( cl_debug_fov )
			DebugFieldOfView( player );

		//
		// Some FOV changes require the screen animate from some other value.
		// This property sets what FOV value we should start animating from.
		//

		if ( player.ForcedFieldOfViewStartWith.HasValue )
		{
			LastFieldOfView = player.ForcedFieldOfViewStartWith.Value;
			player.ForcedFieldOfViewStartWith = null;
		}

		// Retrieve FOV values from the player, if they choose to override FOV.
		RetrieveFieldOfViewFromPlayer( player );

		if ( LastDesiredFieldOfView != DesiredFieldOfView )
		{
			FieldOfViewAnimateStart = LastFieldOfView;
			TimeSinceFieldOfViewAnimateStart = 0;
		}

		//
		// Animating FOV here
		//

		if ( FieldOfViewChangeTime > 0 )
		{
			float lerp = Math.Clamp( TimeSinceFieldOfViewAnimateStart / FieldOfViewChangeTime, 0f, 1f );
			FieldOfView = FieldOfViewAnimateStart.LerpTo( DesiredFieldOfView, lerp );
		}
		else
		{
			// just set instantly, there shouldn't be any transition.
			FieldOfView = DesiredFieldOfView;
		}
		

		LastDesiredFieldOfView = DesiredFieldOfView;
	}

	[ConVar.Client] public static float cl_viewmodel_fov { get; set; } = 75;

	public void DebugFieldOfView( SDKPlayer player )
	{
		DebugOverlay.ScreenText(
			$"[FOV]\n" +
			$"Default               {DefaultFieldOfView}\n" +
			$"Last Value            {LastFieldOfView}\n" +
			$"Desired               {DesiredFieldOfView}\n" +
			$"Change Time           {FieldOfViewChangeTime}\n" +
			$"Animate Start         {FieldOfViewAnimateStart}\n" +
			$"\n" +

			$"Requester             {player.ForcedFieldOfViewRequester}\n" +
			$"Force Start           {player.ForcedFieldOfViewStartWith}\n",
			new Vector2( 60, 250 ) 
			);
	}
	[ConVar.Client] public static bool cl_debug_fov { get; set; }
}
