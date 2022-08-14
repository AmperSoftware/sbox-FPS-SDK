﻿using Sandbox;

namespace Amper.Source1;

public struct ExtendedDamageInfo
{
	public Entity Attacker { get; set; }
	public Entity Inflictor { get; set; } 
	public Entity Weapon { get; set; } 
	public Vector3 Force { get; set; } 
	public float Damage { get; set; }
	public DamageFlags Flags { get; set; }
	public PhysicsBody Body { get; set; } 
	public int HitboxIndex { get; set; } 
	public int BoneIndex { get; set; } 
	public int KillType { get; set; }
	public Vector3 HitPosition { get; set; }
	public Vector3 OriginPosition { get; set; }

	public static ExtendedDamageInfo Create( float damage )
	{
		ExtendedDamageInfo result = default( ExtendedDamageInfo );
		result.Damage = damage;
		return result;
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

	public ExtendedDamageInfo WithFlag( DamageFlags flag )
	{
		Flags |= flag;
		return this;
	}

	public ExtendedDamageInfo WithoutFlag( DamageFlags flag )
	{
		Flags &= ~flag;
		return this;
	}

	public ExtendedDamageInfo WithHitBody( PhysicsBody body )
	{
		Body = body;
		return this;
	}

	public ExtendedDamageInfo WithHitbox( int hitbox )
	{
		HitboxIndex = hitbox;
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

	public ExtendedDamageInfo WithOriginPosition( Vector3 position )
	{
		OriginPosition = position;
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
		HitboxIndex = result.HitboxIndex;
		BoneIndex = result.Bone;
		Body = result.Body;
		return this;
	}

	public DamageInfo ToDamageInfo()
	{
		return new()
		{
			Attacker = Attacker,
			Weapon = Weapon,
			Position = HitPosition,
			Force = Force,
			Damage = Damage,
			Flags = Flags,
			Body = Body,
			HitboxIndex = HitboxIndex,
			BoneIndex = BoneIndex,
		};
	}
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
