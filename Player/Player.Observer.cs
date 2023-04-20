using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class SDKPlayer
{
	[Net] public ObserverMode ObserverMode { get; private set; }
	[Net] public Entity ObserverTarget { get; private set; }
	public ObserverMode LastObserverMode { get; set; }
	public bool IsForcedObserverMode { get; private set; }

	public bool IsSpectating => ObserverMode >= ObserverMode.InEye;
	public bool IsObserver => ObserverMode != ObserverMode.None;

	//
	// State Shortcuts
	//

	public bool IsInDeathcam => ObserverMode == ObserverMode.Deathcam;
	public bool IsInEye => ObserverMode == ObserverMode.InEye;
	public bool IsChasing => ObserverMode == ObserverMode.Chase;
	public bool IsRoaming => ObserverMode == ObserverMode.Roaming;

	public virtual void SimulateObserver()
	{
		if ( Game.IsServer )
		{
			if ( IsSpectating )
			{
				// change the mode
				if ( Input.Pressed( "Jump" ) )
				{
					SwitchToNextObserverMode();
				}

				// next target
				if ( Input.Pressed( "Attack1" ) )
				{
					var target = FindNextObserverTarget( false );
					if ( target != null ) SetObserverTarget( target );
				}

				// prev target
				if ( Input.Pressed( "Attack2" ) )
				{
					var target = FindNextObserverTarget( true );
					if ( target != null ) SetObserverTarget( target );
				}
			}

			ValidateObserverSettings();
		}
	}

	public virtual bool SetObserverTarget( Entity target )
	{
		if ( !CanObserveTarget( target ) )
			return false;

		ObserverTarget = target;

		if ( IsRoaming )
		{
			var start = target.GetEyePosition();
			var dir = target.GetEyeRotation().Forward.WithZ( 0 );
			var end = start + dir * -64;

			var tr = Trace.Ray( start, end )
				.Size( GetPlayerMins( false ), GetPlayerMaxs( false ) )
				.WithAnyTags( CollisionTags.Solid )
				.Run();

			Position = tr.EndPosition;
			Rotation = target.GetEyeRotation();
			Velocity = 0;
		}

		return true;
	}

	public void SwitchToNextObserverMode()
	{
		var mode = ObserverMode + 1;
		var count = Enum.GetValues( typeof( ObserverMode ) ).Length;

		if ( (int)mode >= count )
			mode = ObserverMode.InEye;

		SetObserverMode( mode );
	}

	public void ValidateObserverSettings()
	{
		// If we're forced into observer mode
		if ( IsForcedObserverMode )
		{
			if ( !CanObserveTarget( ObserverTarget ) )
			{
				var target = FindNextObserverTarget( false );
				if ( target.IsValid() )
				{
					IsForcedObserverMode = false;
					SetObserverMode( LastObserverMode );
					SetObserverTarget( target );
				}
			}

			return;
		}

		// safe point, we don't want to have anything below "InEye" to be
		// our last observer mode.
		if ( LastObserverMode < ObserverMode.InEye )
		{
			LastObserverMode = ObserverMode.Roaming;
		}

		if ( IsInDeathcam )
		{
			var deathCamTime = DeathAnimationTime + sv_spectator_freeze_traveltime + sv_spectator_freeze_time;
			if ( TimeSinceDeath > deathCamTime )
			{
				StartObserverMode( LastObserverMode );
			}
		}

		// if we're spectating make sure our observer target
		// is one we can spectate.
		if ( IsSpectating )
		{
			ValidateObserverTarget();
		}
	}

	public void ValidateObserverTarget()
	{
		if ( !CanObserveTarget( ObserverTarget ) )
		{
			var target = FindNextObserverTarget( false );
			SetObserverTarget( target );
		}
	}

	public virtual Entity FindNextObserverTarget( bool reverse )
	{
		var ents = FindObserverableEntities().ToList();
		var count = ents.Count;

		// There's nothing to spectate.
		if ( count == 0 ) return null;
		var index = ents.IndexOf( ObserverTarget ); ;
		var delta = reverse ? -1 : 1;

		for ( int i = 0; i < count; i++ )
		{
			index += delta;

			// Put slot on the other side of the list if we overflow the list.
			if ( index >= count ) index = 0;
			else if ( index < 0 ) index = count - 1;

			var target = ents[index];

			if ( !CanObserveTarget( target ) )
				continue;

			return target;
		}

		return null;
	}

	/// <summary>
	/// stop spectating
	/// </summary>
	public void StopObserverMode()
	{
		IsForcedObserverMode = false;
		if ( ObserverMode == ObserverMode.None )
			return;

		if ( ObserverMode > ObserverMode.Deathcam )
			LastObserverMode = ObserverMode;

		ObserverMode = ObserverMode.None;
	}

	/// <summary>
	/// Start observing in this mode.
	/// </summary>
	public void StartObserverMode( ObserverMode mode )
	{
		Tags.Remove( CollisionTags.Solid );
		UsePhysicsCollision = false;
		EnableDrawing = false;

		Health = 1;
		LifeState = LifeState.Dead;

		SetObserverMode( mode );
	}

	public void SetObserverMode( ObserverMode mode )
	{
		// if we were spectating before, remember our last observer mode
		if ( IsSpectating )
			LastObserverMode = ObserverMode;

		// set the new one
		ObserverMode = mode;

		// update the movetype, depending on whether we are or are not
		// in observer mode
		if ( ObserverMode == ObserverMode.None )
			MoveType = MoveType.None;
		else
			MoveType = MoveType.Observer;
	}

	public virtual float DeathAnimationTime => 3;

	public virtual IEnumerable<Entity> FindObserverableEntities()
	{
		return All.OfType<SDKPlayer>().Where( x => x.IsAlive );
	}

	public virtual bool CanObserveTarget( Entity target )
	{
		if ( target == null )
			return false;

		// We can't observe ourselves.
		if ( target == this )
			return false;

		// don't watch invisible players
		if ( !target.EnableDrawing )
			return false;

		if ( target is SDKPlayer player )
		{
			if ( !player.IsAlive )
			{
				// allow watching until 3 seconds after death to see death animation
				if ( TimeSinceDeath > DeathAnimationTime )
					return false;
			}

			// we can't observe other players, unless we're in deathcam
			if ( TeamManager.IsPlayable( TeamNumber ) )
			{
				if ( player.TeamNumber != TeamNumber )
					return false;
			}
		}

		return true;
	}

	[ConVar.Replicated] public static float sv_spectator_freeze_traveltime { get; set; } = 0.4f;
	[ConVar.Replicated] public static float sv_spectator_freeze_time { get; set; } = 4f;
}

public enum ObserverMode
{
	/// <summary>
	/// Not in spectator mode
	/// </summary>
	None,
	/// <summary>
	/// Special mode for death cam animation
	/// </summary>
	Deathcam,
	/// <summary>
	/// Follow a player in first person view
	/// </summary>
	InEye,
	/// <summary>
	/// Follow a player in third person view
	/// </summary>
	Chase,
	/// <summary>
	/// Free roaming
	/// </summary>
	Roaming
}

