using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

partial class SDKGame
{
	protected Dictionary<int, SDKSpawnPoint> LastSpawnPoint { get; set; } = new();

	/// <summary>
	/// Try to place the player on the spawn point.	This functions returns true if nothing occupies the player's
	/// space and they can safely spawn without getting stuck. `transform` will contain the transform data of the position where the 
	/// player would've spawned.
	/// </summary>
	public virtual bool TryFitOnSpawnpoint( SDKPlayer player, Entity spawnPoint, out Transform transform )
	{
		transform = spawnPoint.Transform;

		// trying to land the player on the ground
		var origin = spawnPoint.Position;

		var up = origin + Vector3.Up * 64;
		var down = origin + Vector3.Down * 64;

		// 
		// Land the player on the ground
		//

		var mins = player.GetPlayerMinsScaled( false );
		var maxs = player.GetPlayerMaxsScaled( false );

		// Trace down so maybe we can find a spot to land on.
		var tr = SetupSpawnTrace( player, up, down, mins, maxs ).Run();

		// we landed on something, update our transform position.
		if ( tr.Hit )
		{
			transform.Position = tr.EndPosition;
			origin = tr.EndPosition;
		}

		// 
		// Check if nothing occupies our spawn space.
		//

		tr = SetupSpawnTrace( player, origin, origin, mins, maxs ).Run();
		return !tr.Hit;
	}

	public virtual Trace SetupSpawnTrace( SDKPlayer player, Vector3 from, Vector3 to, Vector3 mins, Vector3 maxs )
	{
		return Trace.Ray( from, to )
			.Size( mins, maxs )
			.WithAnyTags( CollisionTags.Solid )			// Not inside a solid object
			.WithAnyTags( CollisionTags.Clip )			// General clip brush
			.WithAnyTags( CollisionTags.PlayerClip )	// Player movement clip brush
			.WithAnyTags( CollisionTags.Player )		// In another player.
			.Ignore( player );
	}

	public virtual void FindAndMovePlayerToSpawnPoint( SDKPlayer player )
	{
		// try to find a valid spawn point for this player
		var team = player.TeamNumber;

		// get all available spawn points in the list.
		var points = All.OfType<SDKSpawnPoint>().ToList();
		var count = points.Count;

		//
		// TEAM SPAWN POINTS
		//

		// go through all source1base spawn points and see which one can spawn us.
		if ( count > 0 ) 
		{
			// we'll use this if we can't find any point that could place us
			// without getting stuck
			SDKSpawnPoint firstEligiblePoint = null;

			// figuring out at which point we should start.
			var index = -1;
			if ( LastSpawnPoint.TryGetValue( team, out var lastSpawnPoint ) )
				index = points.IndexOf( lastSpawnPoint );

			// looping through all points in the list.
			for ( int i = 0; i < count; i++ )
			{
				index++;
				if ( index >= count ) index = 0;

				var point = points[index];
				if ( point.CanSpawn( player ) )
				{
					// this point can spawn us!

					// remember it for the future, if it's the first one we found.
					if ( firstEligiblePoint == null )
						firstEligiblePoint = point;

					if ( TryFitOnSpawnpoint( player, point, out var transform ) )
					{
						player.Transform = transform;
						player.ForceViewAngles( new Angles( 0, transform.Rotation.Yaw(), 0 ) );
						LastSpawnPoint[team] = point;
						return;
					}
				}
			}

			// we couldn't find a spawn point that wont get us stuck. But we did find a spawn point that can spawn us.
			// Place us there even if we get stuck.
			if ( firstEligiblePoint != null )
			{
				TryFitOnSpawnpoint( player, firstEligiblePoint, out var transform );
				player.Transform = transform;
				player.ForceViewAngles( new Angles( 0, transform.Rotation.Yaw(), 0 ) );
				LastSpawnPoint[team] = firstEligiblePoint;
				return;
			}
		}


		//
		// SBOX DEFAULT SPAWN POINTS
		//

		// We weren't able to find any valid spawn point.
		// try to seek some default sbox ones.
		var sboxpoints = All
			.OfType<SpawnPoint>()
			.OrderBy( x => Guid.NewGuid() )
			.ToList();

		// there are default sbox points on this map!
		if ( sboxpoints.Count > 0 ) 
		{
			// find the one we can spawn at
			foreach ( var point in sboxpoints )
			{
				if ( TryFitOnSpawnpoint( player, point, out var transform ) )
				{
					player.Transform = transform;
					player.ForceViewAngles( new Angles( 0, transform.Rotation.Yaw(), 0 ) );
					return;
				}
			}

			// nothing could fit us, place us at a random point,
			// even if we get stuck
			var rndpoint = Game.Random.FromList( sboxpoints );
			TryFitOnSpawnpoint( player, rndpoint, out var transform2 );
			player.Transform = transform2;
			player.ForceViewAngles( new Angles( 0, transform2.Rotation.Yaw(), 0 ) );
			return;
		}

		//
		// THIS MAP HAS NO SPAWN POINTS
		//

		// Spawn at 0,0,0. There's nothing we can do really.
		player.Transform = new( 0, Rotation.Identity );
		player.ForceViewAngles( Angles.Zero );
	}
}
