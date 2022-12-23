using Sandbox;

namespace Amper.FPS;

/// <summary>
/// This should contain all of the combat entry points / functionality 
/// that are common between NPCs and players
/// </summary>
public abstract partial class BaseCombatCharacter : AnimatedEntity, IHasMaxHealth
{
	[Net, Predicted]
	public float MaxHealth { get; set; }
	[Net]
	public TimeSince TimeSinceSpawned { get; set; }
	[Net, Predicted]
	public MoveType MoveType { get; set; }
	public bool IsAlive => LifeState == LifeState.Alive;

	public override void Spawn()
	{
		base.Spawn();

		// SetBlocksLOS( false );
		TimeSinceSpawned = 0;

		// not standing on a nav area yet
		ClearLastKnownArea();
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
		ActiveWeapon?.FrameSimulate( cl );
	}

	public NavArea LastKnownArea;

	public void ClearLastKnownArea()
	{
		OnNavAreaChanged( null, LastKnownArea );

		if ( LastKnownArea != null )
		{
			// m_lastNavArea->DecrementPlayerCount( m_registeredNavTeam, entindex() );
			// m_lastNavArea->OnExit( this, NULL );
			LastKnownArea = null;
		}
	}

	public void UpdateLastKnownArea()
	{
		if ( !Game.IsServer )
			return;

		if ( !NavMesh.IsLoaded )
		{
			ClearLastKnownArea();
			return;
		}

		var flags = GetNavAreaFlags.CheckLineOfSight | GetNavAreaFlags.CheckGround;
		var area = NavArea.GetClosestNav( Position, NavAgentHull.Default, flags, 50 );
		if ( area == null )
			return;

		if ( !IsAreaTraversable( area ) )
			return;

		if ( area != LastKnownArea )
		{
			if ( LastKnownArea != null )
			{
				// m_lastNavArea->DecrementPlayerCount( m_registeredNavTeam, entindex() );
				// m_lastNavArea->OnExit( this, NULL );
			}

			// RegisteredNavTeam = TeamNumber;
			// area->IncrementPlayerCount( m_registeredNavTeam, entindex() );
			// area->OnEnter( this, NULL );

			OnNavAreaChanged( area, LastKnownArea );
			LastKnownArea = area;
		}
	}

	public virtual bool IsAreaTraversable( NavArea area ) => true;
	public virtual void OnNavAreaChanged( NavArea enteredArea, NavArea leftArea ) { }
}
