using Sandbox;
using Sandbox.Internal;

namespace Amper.FPS;

partial class SDKPlayer
{
	/// <summary>
	/// The entity we are currently hovering. The distance to this entity is
	/// stored in <see cref="HoveredDistance"/>
	/// </summary>
	public Entity HoveredEntity { get; private set; }
	public float HoveredDistance { get; private set; }

	protected virtual Entity FindHovered( out float distance )
	{
		distance = 0;
		var tr = Trace.Ray( this.GetEyePosition(), this.GetEyePosition() + this.GetEyeRotation().Forward * 5000 )
			.Ignore( this )
			.WithAnyTags( CollisionTags.Solid )
			.WithAnyTags( CollisionTags.Interactable )
			.Run();

		if ( !tr.Entity.IsValid() )
			return null;

		if ( tr.Entity.IsWorld )
			return null;

		distance = tr.Distance;
		return tr.Entity;
	}

	protected virtual void SimulateHover()
	{
		// The entity we're currently looking at.
		HoveredEntity = FindHovered( out var distance );
		HoveredDistance = distance;
	}

	//
	// Use
	//

	public Entity UseEntity { get; protected set; }
	public virtual string UseButton => "Use";
	public virtual void SimulateUse()
	{
		using var _ = Prediction.Off();

		if ( Input.Pressed( UseButton ) )
		{
			if ( !AttemptUse() )
			{
				OnUseFailed();
				return;
			}
		}

		// Don't have anything to use.
		if ( !UseEntity.IsValid() )
			return;

		if ( !Input.Down( UseButton ) )
		{
			StopUsing();
			return;
		}

		//
		// Double check that we can still use this entity.
		//

		if ( !CanContinueUsing( UseEntity ) )
		{
			StopUsing();
			return;
		}

		//
		// If use returns true then we can keep using it
		//
		if ( UseEntity is IUse use && use.OnUse( this ) )
			return;

		StopUsing();
	}

	public bool IsUseableEntity( Entity entity )
	{
		if ( !entity.IsValid() )
			return false;

		if ( entity is not IUse use ) 
			return false;

		if ( !use.IsUsable( this ) ) 
			return false;

		return true;
	}

	public bool CanUse()
	{
		return IsAlive;
	}

	public Entity FindUseEntity()
	{
		var tr = Trace.Ray( this.GetEyePosition(), this.GetEyePosition() + this.GetEyeRotation().Forward * 1024 )
			.Ignore( this )
			.WithAnyTags( CollisionTags.Solid )
			.WithAnyTags( CollisionTags.Interactable )
			.Run();

		var useObject = tr.Entity;
		var usable = IsUseableEntity( useObject );
		while ( useObject.IsValid() && !usable && useObject.Parent.IsValid() )
		{
			useObject = useObject.Parent;
			usable = IsUseableEntity( useObject );
		}

		if ( usable )
		{
			var delta = tr.EndPosition - tr.StartPosition;
			var centerZ = useObject.WorldSpaceBounds.Center.z;
			delta.z = IntervalDistance( tr.EndPosition.z, centerZ + CollisionBounds.Mins.z, centerZ + CollisionBounds.Maxs.z );
			var dist = delta.Length;

			if ( dist < sv_player_use_distance )
			{
				if ( sv_debug_player_use2 )
				{
					DebugOverlay.Line( this.GetEyePosition(), tr.EndPosition, Color.Green, 30, true );
					DebugOverlay.Sphere( tr.EndPosition, 16, Color.Green, 30 );
				}

				return useObject;
			}
		}

		return null;
	}

	// Source SDK function.
	float IntervalDistance( float x, float x0, float x1 )
	{
		// swap so x0 < x1
		if ( x0 > x1 )
		{
			float tmp = x0;
			x0 = x1;
			x1 = tmp;
		}

		if ( x < x0 )
			return x0 - x;
		else if ( x > x1 )
			return x - x1;
		return 0;
	}

	public virtual bool AttemptUse()
	{
		if ( !CanUse() )
			return false;

		var useEntity = FindUseEntity();
		UseEntity = useEntity;

		return useEntity.IsValid();
	}

	public virtual void OnUseFailed()
	{
		if ( IsLocalPawn )
		{
			PlaySound( "sounds/player_use_fail.sound" );
		}
	}

	protected void StopUsing()
	{
		UseEntity = null;
	}

	public virtual bool CanUse( Entity entity )
	{
		if ( !IsAlive )
			return false;

		if ( entity is not IUse use )
			return false;

		if ( !use.IsUsable( this ) )
			return false;

		return true;
	}

	public bool CanContinueUsing( Entity entity )
	{
		return entity == FindUseEntity();
	}

	[ConVar.Replicated] public static float sv_player_use_distance { get; set; } = 100;
	[ConVar.Replicated] public static bool sv_debug_player_use2 { get; set; }
}
