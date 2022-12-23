using Sandbox;
using System.Collections.Generic;

namespace Amper.FPS;

public struct ExtendedDamageInfo
{
	/// <summary>
	/// Attacker of the inflicted damage.
	/// </summary>
	public Entity Attacker;
	/// <summary>
	/// Entity that inflicted damage. (building or a projectile)
	/// </summary>
	public Entity Inflictor;
	/// <summary>
	/// Weapon that caused the inflicted weapon.
	/// </summary>
	public Entity Weapon;
	/// <summary>
	/// Force which is applied by damage.
	/// </summary>
	public Vector3 Force;
	/// <summary>
	/// How much damage we have inflicted.
	/// </summary>
	public float Damage;
	/// <summary>
	/// Custom tags applied by this damage.
	/// </summary>
	public HashSet<string> Tags;
	/// <summary>
	/// Plysics body-receiver of the damage.
	/// </summary>
	public PhysicsBody Body;
	/// <summary>
	/// Hitbox that received 
	/// </summary>
	public Hitbox Hitbox; 
	/// <summary>
	/// Index of the receiving bone.
	/// </summary>
	public int BoneIndex; 
	/// <summary>
	/// The position at which this damage has impacted with the victim. I.e. position that bullet has 
	/// hit in the victim's hitboxes. The blood at the target will appear at this position.
	/// </summary>
	public Vector3 HitPosition;
	/// <summary>
	/// The position from which this damage originated. I.e. the origin of an explosion that damaged the player.
	/// </summary>
	public Vector3 OriginPosition;
	/// <summary>
	/// The position which we will report to the client that received damage.
	/// </summary>
	public Vector3 ReportPosition;

	public static ExtendedDamageInfo Create( float damage )
	{
		return new ExtendedDamageInfo { Damage = damage };
	}

	public ExtendedDamageInfo WithAttacker( Entity attacker )
	{
		Attacker = attacker;
		return this;
	}

	public ExtendedDamageInfo WithInflictor( Entity inflictor )
	{
		Inflictor = inflictor;
		return this;
	}

	public ExtendedDamageInfo WithWeapon( Entity weapon )
	{
		Weapon = weapon;
		return this;
	}

	public ExtendedDamageInfo WithTag( string tag )
	{
		Tags.Add( tag );
		return this;
	}

	public ExtendedDamageInfo WithoutTag( string tag )
	{
		Tags.Remove( tag );
		return this;
	}

	public bool HasTag( string tag )
	{
		return Tags.Contains( tag );
	}

	public ExtendedDamageInfo WithHitBody( PhysicsBody body )
	{
		Body = body;
		return this;
	}

	public ExtendedDamageInfo WithHitbox( Hitbox hitbox )
	{
		Hitbox = hitbox;
		return this;
	}

	public ExtendedDamageInfo WithBone( int bone )
	{
		BoneIndex = bone;
		return this;
	}

	public ExtendedDamageInfo WithDamage( float damage )
	{
		Damage = damage;
		return this;
	}

	public ExtendedDamageInfo WithHitPosition( Vector3 position )
	{
		HitPosition = position;
		return this;
	}

	public ExtendedDamageInfo WithAllPositions( Vector3 position )
	{
		HitPosition = position;
		OriginPosition = position;
		ReportPosition = position;
		return this;
	}

	/// <summary>
	/// The position from which this damage originated. I.e. the origin of an explosion that damaged the player.
	/// </summary>
	public ExtendedDamageInfo WithOriginPosition( Vector3 position )
	{
		OriginPosition = position;
		return this;
	}

	/// <summary>
	/// The position which we will report to the client that received damage.
	/// </summary>
	public ExtendedDamageInfo WithReportPosition( Vector3 position )
	{
		ReportPosition = position;
		return this;
	}

	public ExtendedDamageInfo WithForce( Vector3 force )
	{
		Force = force;
		return this;
	}

	public ExtendedDamageInfo UsingTraceResult( TraceResult result )
	{
		HitPosition = result.EndPosition;
		OriginPosition = result.StartPosition;
		Hitbox = result.Hitbox;
		BoneIndex = result.Bone;
		Body = result.Body;
		return this;
	}

	public DamageInfo ToDamageInfo()
	{
		DamageInfo info = new()
		{
			Attacker = Attacker,
			Weapon = Weapon,
			Position = HitPosition,
			Force = Force,
			Damage = Damage,
			Hitbox = Hitbox,
			BoneIndex = BoneIndex,
		};

		return info;
	}

	public static implicit operator DamageInfo( ExtendedDamageInfo info ) => info.ToDamageInfo();
}

public interface IAcceptsExtendedDamageInfo
{
	public void TakeDamage( ExtendedDamageInfo info );
}

public static class ExtendedDamageInfoExtensions
{
	public static void TakeDamage( this Entity entity, ExtendedDamageInfo info )
	{
		if ( entity is IAcceptsExtendedDamageInfo target )
		{
			target.TakeDamage( info );
			return;
		}

		entity.TakeDamage( info.ToDamageInfo() );
	}
}
