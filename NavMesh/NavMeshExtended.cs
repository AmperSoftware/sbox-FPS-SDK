using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

public class NavMeshExtended
{
	public static NavMeshExtended Current { get; private set; }

	public NavMeshExtended()
	{
		Current = this;
	}

	public bool Precomputed = false;

	Dictionary<uint, NavArea> _areas = new();
	Dictionary<uint, List<uint>> _adjacent = new();
	public IReadOnlyCollection<NavArea> Areas => _areas.Values;

	public void PrecomputeNavMesh()
	{
		if ( Precomputed )
		{
			Log.Error( "Nav Mesh was already precomputed." );
			return;
		}

		Reset();

		Log.Info( "Nav Mesh PrecomputeNavMesh!" );
		if ( !NavMesh.IsLoaded )
			return;

		float startTime = Time.Now;
		var seedPoints = CollectPrecomputeEntitySeeds();

		// We didn't collect any spawn points.
		if ( !seedPoints.Any() )
		{
			// just spam a bunch of random seeds,
			// eventualy we'll hit something.
			seedPoints = CollectRandomPrecomputeSeeds();
		}

		foreach ( var point in seedPoints )
		{
			FindAreasNearPoint( point );
		}

		Precomputed = true;
		Log.Info( "Nav Mesh Precomputed!" );
		OnNavMeshPrecomputed();
	}

	public virtual void OnNavMeshPrecomputed() { }

	public void Reset()
	{
		_areas.Clear();
		_adjacent.Clear();
	}

	private IEnumerable<Vector3> CollectPrecomputeEntitySeeds()
	{
		// Source 1 Base Spawn Points
		var spawnPoints = Entity.All.OfType<SDKSpawnPoint>();
		foreach ( var spawn in spawnPoints )
			yield return spawn.Position;

		// s&box Spawn Points
		var sboxPoints = Entity.All.OfType<Sandbox.SpawnPoint>();
		foreach ( var spawn in sboxPoints )
			yield return spawn.Position;
	}

	private IEnumerable<Vector3> CollectRandomPrecomputeSeeds( int count = 10, float range = 3000 )
	{
		// Always sample from 0,0,0 first.
		// Maps are usually centered.
		yield return 0;

		// Spawm a whole bunch of random points.
		for ( var i = 0; i < count; i++ )
			yield return Vector3.Random * range;
	}

	private void FindAreasNearPoint( Vector3 pos )
	{
		// Getting closest nav to this point.
		var area = NavArea.GetClosestNav( pos );
		if ( area == null )
			return;

		FindAndProcessAdjacentAreas( area );
	}

	bool IsAreaProcessed( NavArea area ) => _areas.ContainsKey( area.ID );

	int MaxAdjacentAttempts => 500;

	private void FindAndProcessAdjacentAreas( NavArea area, int depth = 0 )
	{
		// Already processed this area.
		if ( IsAreaProcessed( area ) ) 
			return;

		_areas.Add( area.ID, area );

		int countToFound = area.AdjacentCount;
		if ( countToFound == 0 )
			return;

		int areasFound = 0;
		var attempts = 0;
		List<uint> ids = new();

		while ( areasFound < countToFound )
		{
			attempts++;

			if ( attempts > MaxAdjacentAttempts )
				break;

			var adjArea = area.GetRandomAdjacent();
			if ( adjArea == null )
				continue;

			// Already found this area.
			if ( ids.Contains( adjArea.ID ) )
				continue;

			areasFound++;
			ids.Add( adjArea.ID );

			FindAndProcessAdjacentAreas( adjArea, depth + 1 );
		}

		_adjacent.Add( area.ID, ids );
	}

	public static NavArea FindById( uint id )
	{
		if ( Current == null )
			return null;

		if ( Current._areas.TryGetValue( id, out var area ) ) 
			return area;

		return null;
	}

	public static IEnumerable<NavArea> CollectAdjacentAreas( NavArea area )
	{
		if ( Current == null )
			yield break;

		if ( Current._adjacent.TryGetValue( area.ID, out var list ) )
		{
			if ( list == null )
				yield break;

			foreach ( var adjId in list )
			{
				var adjArea = FindById( adjId );
				if ( adjArea != null )
					yield return adjArea;
			}
		}
	}

	public static float TravelDistance( Vector3 startPos, Vector3 endPos, float maxPathLength = 2000 )
	{
		var path = NavMesh.PathBuilder( startPos )
			.WithMaxDistance( maxPathLength )
			.Build( endPos );

		return path.TotalLength;
	}

	public virtual void Update() { }
}

public static class NavAreaExtensions
{
	public static IEnumerable<NavArea> GetAdjacentAreas( this NavArea area )
	{
		if ( area == null )
			return null;

		return NavMeshExtended.CollectAdjacentAreas( area );
	}
}
