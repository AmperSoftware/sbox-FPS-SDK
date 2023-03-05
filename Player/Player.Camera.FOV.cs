using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amper.FPS;

public partial class SDKPlayer
{
	protected float DesiredFieldOfView { get; private set; }
	protected float LastDesiredFieldOfView { get; private set; }
	protected float FieldOfViewChangeTime { get; private set; }

	protected float FieldOfViewAnimateStart { get; private set; }
	protected TimeSince TimeSinceFieldOfViewAnimateStart { get; private set; }

	public void RetrieveFieldOfViewFromPlayer( )
	{
		//
		// Desired FOV Value
		//

		DesiredFieldOfView = Screen.CreateVerticalFieldOfView(Game.Preferences.FieldOfView);
		if ( ForcedFieldOfView > 0 )
			DesiredFieldOfView = ForcedFieldOfView;

		//
		// Desired FOV Change Time
		//

		FieldOfViewChangeTime = 0;
		if ( ForcedFieldOfViewChangeTime > 0 )
		{
			// If our fov change time is set to something, but we've already reached out desired FOV, then reset the speed to zero.
			// We will be changing fov instantly until we're set speed again.
			if ( Camera.FieldOfView == DesiredFieldOfView && ForcedFieldOfViewChangeTime > 0 )
				ForcedFieldOfViewChangeTime = 0;

			FieldOfViewChangeTime = ForcedFieldOfViewChangeTime;
		}
	}

	public virtual void CalculateFieldOfView(  )
	{
		if ( cl_debug_fov )
			DebugFieldOfView( );

		//
		// Some FOV changes require the screen animate from some other value.
		// This property sets what FOV value we should start animating from.
		//

		if ( ForcedFieldOfViewStartWith.HasValue )
		{
			LastDesiredFieldOfView = ForcedFieldOfViewStartWith.Value;
			ForcedFieldOfViewStartWith = null;
		}

		// Retrieve FOV values from the player, if they choose to override FOV.
		RetrieveFieldOfViewFromPlayer( );

		if ( LastDesiredFieldOfView != DesiredFieldOfView )
		{
			FieldOfViewAnimateStart = LastDesiredFieldOfView;
			TimeSinceFieldOfViewAnimateStart = 0;
		}

		//
		// Animating FOV here
		//

		if ( FieldOfViewChangeTime > 0 )
		{
			float lerp = Math.Clamp( TimeSinceFieldOfViewAnimateStart / FieldOfViewChangeTime, 0f, 1f );
			Camera.FieldOfView = FieldOfViewAnimateStart.LerpTo( DesiredFieldOfView, lerp );
		}
		else
		{
			// just set instantly, there shouldn't be any transition.
			Camera.FieldOfView = DesiredFieldOfView;
		}


		LastDesiredFieldOfView = DesiredFieldOfView;
	}

	public void DebugFieldOfView()
	{
		DebugOverlay.ScreenText(
			$"[FOV]\n" +
			$"Last Value            {LastDesiredFieldOfView}\n" +
			$"Desired               {DesiredFieldOfView}\n" +
			$"Change Time           {FieldOfViewChangeTime}\n" +
			$"Animate Start         {FieldOfViewAnimateStart}\n" +
			$"\n" +

			$"Requester             {ForcedFieldOfViewRequester}\n" +
			$"Force Start           {ForcedFieldOfViewStartWith}\n",
			new Vector2( 60, 250 )
			);
	}
	[ConVar.Client] public static bool cl_debug_fov { get; set; }
}
