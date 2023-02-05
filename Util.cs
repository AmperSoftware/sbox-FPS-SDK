using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Internal.Globals;
using System;

namespace Amper.FPS;

public static class Source1Extensions
{
	public static Vector3 GetEyePosition( this IEntity ent ) => ent.AimRay.Position;
	public static Vector3 GetEyeForward( this IEntity ent ) => ent.AimRay.Forward;
	public static Rotation GetEyeRotation( this IEntity ent ) => Rotation.LookAt(ent.AimRay.Forward);

	public static Vector3 GetLocalEyePosition( this IEntity ent ) => ent.Transform.PointToLocal(ent.GetEyePosition());
	public static Rotation GetLocalEyeRotation( this IEntity ent ) => ent.Transform.RotationToLocal( ent.GetEyeRotation() );
	public async static void Reset( this DoorEntity door )
	{
		if ( !Game.IsServer ) return;
		var startsLocked = door.SpawnSettings.HasFlag( DoorEntity.Flags.StartLocked );

		// unlock the door to force change.
		var lastSpeed = door.Speed;
		// Close the door at a very high speed, so it visually closes immediately.
		door.Speed = 10000;
		door.Close();

		// wait some time
		await GameTask.DelaySeconds( 0.1f );

		// reset speed back.
		door.Speed = lastSpeed;
		if ( startsLocked ) door.Lock();
	}

	public static bool IsValid( this GameResource resource ) => resource != null;
	public static void NetInfo( this Logger logger, FormattableString message ) => logger.Info( $"[{(Game.IsServer ? "SV" : "CL")}] {message}" );
	public static void NetInfo( this Logger logger, object message ) => logger.Info( $"[{(Game.IsServer ? "SV" : "CL")}] {message}" );
	public static void NetScreenText(this DebugOverlay overlay, string message, int line = 0, float time = 0f, int clientLineOffset = 1)
	{
		if ( clientLineOffset == 0 )
			clientLineOffset = 1;
				
		if ( Game.IsServer )
		{
			overlay.ScreenText( message, Vector2.One * 100, line, Color.Yellow, time );
		}
		else
		{
			overlay.ScreenText( message, Vector2.One * 100, line + clientLineOffset, Color.Cyan, time );
		}
	}
	public static void NetScreenText( this DebugOverlay overlay, object message, int line = 0, float time = 0f, int clientLineOffset = 1 ) => overlay.NetScreenText( message?.ToString(), line, time, clientLineOffset );
}

public static class CollisionTags
{
	/// <summary>
	/// Never collides with anything.
	/// </summary>
	public const string NotSolid = "notsolid";
	/// <summary>
	/// Everything that is solid.
	/// </summary>
	public const string Solid = "solid";
	/// <summary>
	/// Trigger that isn't collideable but can still send touch events.
	/// </summary>
	public const string Trigger = "trigger";
	/// <summary>
	/// A ladder.
	/// </summary>
	public const string Ladder = "ladder";
	/// <summary>
	/// Water pool.
	/// </summary>
	public const string Water = "water";
	/// <summary>
	/// Never collides with anything except solid and other debris.
	/// </summary>
	public const string Debris = "debris";
	/// <summary>
	/// Just like debris, but also sends touch events to players.
	/// </summary>
	public const string Interactable = "interactable";
	/// <summary>
	/// This is a player.
	/// </summary>
	public const string Player = "player";
	/// <summary>
	/// A fired projectile.
	/// </summary>
	public const string Projectile = "projectile";
	/// <summary>
	/// This is a weapon players can interact with.
	/// </summary>
	public const string Weapon = "weapon";
	/// <summary>
	/// Driveable vehicle.
	/// </summary>
	public const string Vehicle = "vehicle";
	/// <summary>
	/// Physics prop, collideable by player movement by default.
	/// </summary>
	public const string Prop = "prop";
	/// <summary>
	/// A non playable entity.
	/// </summary>
	public const string NPC = "npc";

	public const string Clip = "clip";
	public const string PlayerClip = "playerclip";
	public const string BulletClip = "bulletclip";
	public const string ProjectileClip = "projectileclip";
	public const string NPCClip = "npcclip";
}

public static class DamageTags
{
	public const string Generic = "generic";
	public const string Bullet = "bullet";
	public const string Blast = "explosion";
	public const string Slash = "slash";
	public const string Burn = "burn";
	public const string Vehicle = "vehicle";
	public const string Fall = "fall";
	public const string Blunt = "blunt";
	public const string Shock = "electric";
	public const string Drown = "water";

	public const string AlwaysGib = "always_gib";
	public const string DoNotGib = "never_gib";
}
