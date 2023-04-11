using Sandbox;

namespace Amper.FPS;

public abstract partial class Projectile : ModelEntity, ITeam
{
	[Net] public int TeamNumber { get; set; }
	public ExtendedDamageInfo DamageInfo { get; set; }
	[Net] public TimeSince TimeSinceCreated { get; set; }

	/// <summary>
	/// The weapon that launched this projectile.
	/// </summary>
	public Entity Launcher { get; set; }
	/// <summary>
	/// The velocity at which this projectile was fired.
	/// </summary>
	public Vector3 OriginalVelocity { get; set; }
	/// <summary>
	/// The position at which this projectile was originally launched.
	/// </summary>
	public Vector3 OriginalPosition { get; set; }
	/// <summary>
	/// The launcher that launched this projectile originally.
	/// </summary>
	public Entity OriginalLauncher { get; set; }
	/// <summary>
	/// Entity that owned this projectile originally.
	/// </summary>
	public Entity OriginalOwner { get; set; }
	/// <summary>
	/// How much base damage will this projectile deal.
	/// </summary>
	public float Damage { get; set; }
	/// <summary>
	/// How much this projectile is affected by gravity (only works in Fly movetype.)
	/// </summary>
	public float Gravity { get; set; }
	/// <summary>
	/// The entity that will receive 100% of damage upon explosion.
	/// </summary>
	public Entity Enemy { get; set; }
	/// <summary>
	/// Did we touch world geometry?
	/// </summary>
	public bool Touched { get; set; }

	public float? AutoDestroyTime { get; set; }
	public float? AutoExplodeTime { get; set; }
	public bool FaceVelocity { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( CollisionTags.Projectile );

		TimeSinceCreated = 0;
		Damage = 0;
		Gravity = 0;
		MoveType = ProjectileMoveType.None;
		Predictable = false;
		DamageInfo = ExtendedDamageInfo.Create( 0f );

		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;

		AutoDestroyTime = 30;
		FaceVelocity = true;
	}

	[Event.Tick.Server]
	public virtual void Tick()
	{
		SimulateCollisions();
		SimulateMovement();
		UpdateFaceRotation();

		if ( AutoExplodeTime.HasValue )
		{
			if ( TimeSinceCreated > AutoExplodeTime.Value )
				Explode();
		}

		if ( AutoDestroyTime.HasValue )
		{
			if ( TimeSinceCreated > AutoDestroyTime.Value )
				Delete();
		}
	}

	public virtual void OnInitialized()
	{
		EnableDrawing = true;

		OriginalPosition = Position;
		OriginalVelocity = Velocity;
		OriginalLauncher = Launcher;
		OriginalOwner = Owner;
		
		// Copy base velocity too, some projectile use it
		BaseVelocity = Velocity;
		
		UpdateFaceRotation();
		CreateTrails();
	}

	public virtual void OnTraceTouch( Entity other, TraceResult result ) { }

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( !Touched )
		{
			var other = eventData.Other.Entity;
			if ( other.IsValid() && other.IsWorld ) 
			{
				Touched = true;
				Damage *= TouchedDamageMultiplier;
			}
		}
	}

	public virtual float TouchedDamageMultiplier => 1;

	public void UpdateFaceRotation()
	{
		if ( !FaceVelocity )
			return;

		if ( Velocity.Length <= 0 )
			return;
		
		Rotation = Rotation.LookAt( Velocity.Normal );
	}

	public virtual void Explode()
	{
		Game.AssertServer();

		Explode( Position );
	}

	public virtual void Explode( Vector3 position )
	{
		Game.AssertServer();

		var origin = position + Vector3.Up * 16;
		var target = position + Vector3.Down * 16;

		var tr = Trace.Ray( origin, target )
			.WorldOnly()
			.Run();

		Explode( tr );
	}

	public virtual void Explode( TraceResult trace )
	{
		Game.AssertServer();

		DamageInfo = DamageInfo.WithHitPosition( trace.EndPosition );
		DoExplosionEffect( Position, trace.Normal );

		if ( Owner.IsValid() ) 
		{
			var radius = new RadiusDamageInfo( DamageInfo, Radius, this, AttackerRadius, AttackerDamage, Enemy );
			SDKGame.Current.ApplyRadiusDamage( radius );
		}

		DoScorchTrace( Position, trace.Normal );
		Delete();
	}

	public virtual void ApplyDamageModifications( ref ExtendedDamageInfo info ) { }

	public virtual void Explode( Entity other, TraceResult trace )
	{
		// Save this entity as enemy, they will take 100% damage.
		Enemy = other;

		// Invisible.
		EnableDrawing = false;

		// Pull out a bit.
		if ( trace.Fraction != 1 ) 
			Position = trace.EndPosition + trace.Normal;

		Explode( trace );
	}

	public virtual float Radius => 146;
	public virtual float AttackerRadius => Radius;
	public virtual float AttackerDamage => DamageInfo.Damage;

	public virtual bool IsDestroyable => false;

	public static T Create<T>(Vector3 origin, Vector3 velocity, Entity owner, Entity launcher, float damage) where T : Projectile, new()
	{
		T ent = new();

		ent.Position = origin;
		ent.Velocity = velocity;

		ent.Owner = owner;
		ent.Launcher = launcher;

		ent.DamageInfo = ent.DamageInfo
			.WithDamage( damage)
			.WithAttacker( owner )
			.WithWeapon( launcher )
			.WithInflictor( ent );

		// Set the projectile's team to owner's team if it has a team.
		if ( owner is ITeam ownerTeam )
			ent.TeamNumber = ownerTeam.TeamNumber;

		ent.OnInitialized();
		return ent;
	}
}

public enum ProjectileMoveType
{
	None,
	Fly,
	Physics,
	Custom
};
