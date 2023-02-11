using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

public struct ExtendedDamageInfo
{
	public Entity Attacker { get; set; }
	public Entity Inflictor { get; set; }
	public Entity Weapon { get; set; }
	public Vector3 Force { get; set; }
	public float ForceScale { get; set; }
	public float Damage { get; set; }
	public List<string> Tags { get; set; }
	public PhysicsBody Body { get; set; }
	public Hitbox Hitbox { get; set; }
	public int BoneIndex { get; set; }
	/// <summary>
	/// The position at which this damage has impacted with the victim. I.e. position that bullet has 
	/// hit in the victim's hitboxes. The blood at the target will appear at this position.
	/// </summary>
	public Vector3 HitPosition { get; set; }
	/// <summary>
	/// The position from which this damage originated. I.e. the origin of an explosion that damaged the player.
	/// </summary>
	public Vector3 OriginPosition { get; set; }
	/// <summary>
	/// The position which we will report to the client that received damage.
	/// </summary>
	public Vector3 ReportPosition { get; set; }

	public ExtendedDamageInfo()
	{
		ForceScale = 1;
	}
	public static ExtendedDamageInfo Create( float damage )
	{
		ExtendedDamageInfo result = default;
		result.Damage = damage;
		result.ForceScale = 1;
		result.Tags = new List<string>();

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

	public ExtendedDamageInfo SetTags( List<string> tags )
	{
		Tags = tags;
		return this;
	}

	public ExtendedDamageInfo SetTags( IEnumerable<string> tags )
	{
		Tags = tags.ToList();
		return this;
	}

	public ExtendedDamageInfo WithTags( IEnumerable<string> tags )
	{
		foreach ( var tag in tags )
		{
			Tags.Add( tag );
		}
		return this;
	}

	public ExtendedDamageInfo WithTag( string tag )
	{
		Tags.Add( tag );

		return this;
	}

	public ExtendedDamageInfo WithoutTags( IEnumerable<string> tags )
	{
		foreach ( var tag in tags )
		{
			Tags.Remove( tag );
		}

		return this;
	}

	public ExtendedDamageInfo WithoutTag( string tag )
	{
		Tags.Remove( tag );
		return this;
	}

	public bool HasTag( string tag )
	{
		return Tags != null && Tags.Contains( tag );
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

	public ExtendedDamageInfo WithForceScale(float mult)
	{
		ForceScale = mult;
		return this;
	}

	public ExtendedDamageInfo WithDamage( float damage )
	{
		Damage = damage;
		return this;
	}

	/// <summary>
	/// The position at which this damage has impacted with the victim. I.e. position that bullet has 
	/// hit in the victim's hitboxes. The blood at the target will appear at this position.
	/// </summary>
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
			Tags = new(Tags)
		};

		return info;
	}

	public override string ToString()
	{
		return $"ExtendedDamageInfo[Dmg: {Damage}, Pos: {HitPosition}, Weapon: {Weapon}]";
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
