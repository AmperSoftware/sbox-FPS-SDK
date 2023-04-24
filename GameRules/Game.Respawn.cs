using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class SDKGame
{
	protected Dictionary<int, Entity> LastSpawnPoint { get; set; } = new();
	
	/// <summary>
	/// Try to place the player on the spawn point.	This functions returns true if nothing occupies the player's
	/// space and they can safely spawn without getting stuck. `transform` will contain the transform data of the position where the 
	/// player would've spawned.
	/// </summary>
	public virtual bool TryFitOnSpawnpoint( SDKPlayer player, Entity spawnPoint, out Transform transform )
	{
		transform = spawnPoint.Transform;
		var origin = transform.Position;
		
		// 
		// Land the player on the ground
		//
		
		var up = origin + Vector3.Up * 64;
		var down = origin + Vector3.Down * 64;
		
		var mins = player.GetPlayerMinsScaled();
		var maxs = player.GetPlayerMaxsScaled();
		
		var trace = SetupSpawnTrace( player, up, down, mins, maxs );

		// Trace down so maybe we can find a spot to land on.
		if ( trace.Run() is { Hit: true } result )
		{
			// we landed on something, update our origin position.
			origin = result.EndPosition;
		}

		// Check if something occupies our spawn space.
		
		result = trace.FromTo( origin, origin ).Run();
		
		if ( result.Hit )
		{
			return false;
		}
		
		// Set spawn position
		transform.Position =  origin;
		
		return true;
	}

	public virtual Trace SetupSpawnTrace( SDKPlayer player, Vector3 from, Vector3 to, Vector3 mins, Vector3 maxs )
	{
		return Trace.Ray( from, to )
			.Size( mins, maxs )
			.WithAnyTags( 
				CollisionTags.Solid,		// Not inside a solid object.
				CollisionTags.Clip,			// General clip brush.
				CollisionTags.PlayerClip,	// Player movement clip brush.
				CollisionTags.Player )		// In another player.
			.Ignore( player );
	}

	public virtual void FindAndMovePlayerToSpawnPoint( SDKPlayer player )
	{
		IEnumerable<Entity> points = null;
		var team = player.TeamNumber;
		
		if ( All.OfType<SDKSpawnPoint>().ToList() is { Count: > 0 } sdkPoints )
		{
			//
			// TEAM SPAWN POINTS
			//
			
			// figuring out at which point we should start.
			var index = sdkPoints.IndexOf( LastSpawnPoint.GetOrCreate( team ) as SDKSpawnPoint );

			// go through all source1base spawn points and see which one can spawn us.
			points = sdkPoints.Select( _ => sdkPoints[++index % sdkPoints.Count] )
				.Where( point => point.CanSpawn( player ) );
		}
		else if ( All.OfType<SpawnPoint>().ToList() is { Count: > 0 } sboxPoints )
		{
			// In case we weren't able to find any sdk spawn points
			
			//
			// SBOX DEFAULT SPAWN POINTS
			//
			
			points = sboxPoints.OrderBy( _ => Guid.NewGuid() );
		}
		
		//
		// MOVE PLAYER TO SPAWN POINT
		//
		
		// Set up a default transform in case we find no spawn points.
		Transform transform = default;

		LastSpawnPoint[team] = points?.First( point => TryFitOnSpawnpoint( player, point, out transform ) );
		
		player.Transform = transform;
		player.ForceViewAngles( new Angles().WithYaw( transform.Rotation.Yaw() ) );
	}
}