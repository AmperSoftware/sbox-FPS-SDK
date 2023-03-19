using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.FPS;

partial class SDKWeapon
{
	/// <summary>
	/// Weapons may modify the damage info if they have a need to do it. 
	/// </summary>
	public virtual void ApplyDamageModifications( Entity victim, ref ExtendedDamageInfo info, TraceResult trace ) { }

	public ExtendedDamageInfo CreateDamageInfo( TraceResult tr, float damage )
	{
		// If this projectile has an owner, report their position
		// otherwise fallback to our own position.

		return ExtendedDamageInfo.Create( damage )
			.UsingTraceResult( tr )
			.WithReportPosition( Player.GetAttackPosition() )
			//.WithForce( SDKGame.Current.CalculateForceFromDamage( tr.Direction, damage ) )
			.WithAttacker( Owner )
			.WithInflictor( Owner )
			.WithWeapon( this )
			.WithTag( DamageTags.Bullet );
	}

	/// <summary>
	/// Fire a bullet from this weapon.
	/// </summary>
	public virtual void FireBullet( float damage, int seedOffset = 0 )
	{
		if ( Game.IsServer ) FireBulletServer( damage, seedOffset );
		FireBulletClient( damage, seedOffset );
	}

	//
	// Server
	//

	/// <summary>
	/// This does all the serverside code for the fired shot. This is where we deal damage.
	/// </summary>
	protected virtual TraceResult FireBulletServer( float damage, int seedOffset = 0 )
	{
		Game.AssertServer();
		var tr = TraceFireBullet( seedOffset );

		// We didn't hit any entity, early out.
		var entity = tr.Entity;
		if ( entity == null )
			return tr;

		OnHitEntity( tr.Entity, tr );

		var info = CreateDamageInfo( tr, damage );
		ApplyDamageModifications( entity, ref info, tr );
		entity.TakeDamage( info );

		return tr;
	}

	//
	// Client
	//

	[ClientRpc]
	void FireBulletClient( float damage, int seedOffset = 0 )
	{
		FireBulletEffects( damage, seedOffset );
	}


	/// <summary>
	/// Do client-side effects, related to firing a bullet.
	/// </summary>
	protected virtual TraceResult FireBulletEffects( float damage, int seedOffset = 0 )
	{
		Game.AssertClient();

		var tr = TraceFireBullet( seedOffset );

		// Create particle from the trace.
		CreateBulletTracer( tr.EndPosition );

		// If we hit some entity, do some effects on hit.
		if ( tr.Entity.IsValid() )
			OnHitEntity( tr.Entity, tr );

		return tr;
	}

	public virtual void OnHitEntity( Entity entity, TraceResult tr )
	{
		if ( !Game.IsServer ) return;

		// hack to play particle at HitPosition.
		var endPos = tr.EndPosition;
		tr.EndPosition = tr.HitPosition;

		tr.Surface.DoBulletImpact( tr );

		tr.EndPosition = endPos;
	}

	public virtual TraceResult TraceFireBullet( int seedOffset = 0 )
	{
		Game.SetRandomSeed( Time.Tick + seedOffset );

		var spread = Vector3.Random.WithZ( 0 ) * GetSpread();

		Vector3 origin = GetAttackOrigin();
		Vector3 direction = GetAttackDirectionWithSpread( spread );

		var target = origin + direction * GetRange();

		using ( LagCompensation() )
		{
			var tr = SetupFireBulletTrace( origin, target ).Run();
			DrawDebugTrace( tr );

			return tr;
		}
	}

	public virtual Trace SetupFireBulletTrace( Vector3 Origin, Vector3 Target )
	{
		var tr = Trace.Ray( Origin, Target )
			.Ignore( this )
			.Ignore( Owner )

			// Collides with:
			.WithAnyTags( CollisionTags.Solid )
			.WithAnyTags( CollisionTags.BulletClip )
			.WithAnyTags( CollisionTags.Debris )

			// Doesn't colide with:
			.WithoutTags( CollisionTags.NotSolid )

			.UseHitboxes();

		return tr;
	}

	[ConVar.Replicated] public static bool sv_debug_hitscan_traces { get; set; }
	[ConVar.Replicated] public static bool sv_debug_hitscan_hits { get; set; }

	protected virtual void DrawDebugTrace( TraceResult tr, float time = 5 )
	{
		var drawTrace = sv_debug_hitscan_traces;
		var drawHit = drawTrace || sv_debug_hitscan_hits;

		if ( drawTrace )
		{
			DebugOverlay.Line( tr.StartPosition, tr.EndPosition, Game.IsServer ? Color.Yellow : Color.Green, time, true );

			DebugOverlay.Text(
				$"Distance: {tr.Distance}\n" +
				$"HitBox: {tr.Hitbox}\n" +
				$"Entity: {tr.Entity}\n" +
				$"Fraction: {tr.Fraction}",
				tr.EndPosition,
				time );
		}

		if ( drawHit )
		{
			DebugOverlay.Sphere( tr.EndPosition, 2f, Color.Red, time, true );
		}
	}
}
