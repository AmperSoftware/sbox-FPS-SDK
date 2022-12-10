using Sandbox;
using System;
using System.Collections.Generic;

namespace Amper.FPS;

public struct ExtendedDamageInfo
{
	public Entity Attacker { get; set; }
	public Entity Inflictor { get; set; } 
	public Entity Weapon { get; set; } 
	public Vector3 Force { get; set; } 
	public float Damage { get; set; }
	public IReadOnlyCollection<string> Tags => GetTags();
	private HashSet<string> _tags { get; set; }
	public PhysicsBody Body { get; set; } 
	public Hitbox Hitbox { get; set; } 
	public int BoneIndex { get; set; } 
	public int KillType { get; set; }
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

	public static ExtendedDamageInfo Create( float damage )
	{
		ExtendedDamageInfo result = default;
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

	public ExtendedDamageInfo WithFlags( HashSet<string> flags ) => WithTags( flags );

	[Obsolete( "Temporary fix for pain month (2022/12) issues, switch to tags instead!" )]
	public ExtendedDamageInfo WithFlag( string flag )
	{
		_tags.Add( flag );
		return this;
	}

	[Obsolete( "Temporary fix for pain month (2022/12) issues, switch to tags instead!" )]
	public ExtendedDamageInfo WithoutFlag( string flag )
	{
		_tags.Add( flag );
		return this;
	}

	public ExtendedDamageInfo SetTags(HashSet<string> tags)
	{
		_tags = tags;

		return this;
	}
	public ExtendedDamageInfo SetTags(IEnumerable<string> tags)
	{
		HashSet<string> newTags = new();
		foreach(var tag in tags)
		{
			newTags.Add( tag );
		}
		_tags = newTags;

		return this;
	}
	public ExtendedDamageInfo WithTags(IEnumerable<string> tags)
	{
		foreach ( var tag in tags )
		{
			_tags.Add( tag );
		}

		return this;
	}
	public ExtendedDamageInfo WithTags( params string[] tags ) => WithTags( tags );

	public ExtendedDamageInfo WithTag(string tag)
	{
		_tags.Add( tag );

		return this;
	}

	public ExtendedDamageInfo WithoutTags( IEnumerable<string> tags)
	{
		foreach ( var tag in tags )
		{
			_tags.Remove( tag );
		}

		return this;
	}
	public ExtendedDamageInfo WithoutTags( params string[] tags ) => WithoutTags( tags );

	public ExtendedDamageInfo WithoutTag(string tag)
	{
		_tags.Remove( tag );
		return this;
	}

	public bool HasTag(string tag)
	{
		return _tags.Contains( tag );
	}

	private IReadOnlyCollection<string> GetTags()
	{
		List<string> tags = new();
		foreach(var tag in _tags)
		{
			tags.Add( tag );
		}

		return tags;
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

	[Obsolete( "No longer needed as of pain month (2022/12), use tags instead!" )]
	public ExtendedDamageInfo WithCustomKillType( int killType )
	{
		_tags.Add( $"legacy_custom_{killType}");
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
