using Sandbox;
using System.Collections.Generic;

namespace Amper.FPS;

public interface IHasEffectEntity
{
	public ModelEntity GetEffectEntity();
}

public static class EntityParticleExtensions
{
	public static EntityParticle CreateParticle( this ModelEntity entity, string effect, bool follow = true, float lifeTime = -1 )
	{
		return new( entity, effect, "", 0, follow, lifeTime );
	}

	public static EntityParticle CreateParticle( this ModelEntity entity, string effect, string attachment, bool follow = true, float lifeTime = -1 )
	{
		return new( entity, effect, attachment, 0, follow, lifeTime );
	}

	public static EntityParticle CreateParticle( this ModelEntity entity, string effect, Vector3 offset, bool follow = true, float lifeTime = -1 )
	{
		return new( entity, effect, "", 0, follow, lifeTime );
	}

	public static EntityParticle CreateParticle( this ModelEntity entity, string effect, string attachment, Vector3 offset, bool follow = true, float lifeTime = -1 )
	{
		return new( entity, effect, attachment, offset, follow, lifeTime );
	}
}

public class EntityParticle : IValid
{
	public readonly ModelEntity Entity;
	public readonly string EffectName;

	public Particles Particle { get; private set; }
	
	float LifeTime;
	float? ExpirationTime;

	bool IsDestroyed;
	bool IsEmitting;
	bool StartEmittingOnValidEffectEntity;

	Dictionary<int, ControlPoint> Points = new();

	public EntityParticle( ModelEntity entity, string effect, string attachment, Vector3 offset, bool follow = true, float lifeTime = -1 )
	{
		if ( lifeTime < 0 ) lifeTime = cl_entity_particle_auto_dispose_time;

		Entity = entity;
		EffectName = effect;
		IsDestroyed = true;

		LifeTime = lifeTime;
		ExpirationTime = Time.Now + LifeTime;
		StartEmittingOnValidEffectEntity = true;

		// Attach the first control point.
		SetControlPoint( 0, GetEffectEntity(), attachment, offset, follow );
		Event.Register( this );

	}

	[Event.Tick.Client]
	void Tick()
	{
		// The entity we're attached is no longer valid.
		if ( !Entity.IsValid() )
		{
			DebugMsg( $"Entity is invalid, destroy ourselves." );
			Destroy( true );
			return;
		}

		var effectEntity = GetEffectEntity();

		if ( !effectEntity.IsValid() )
		{
			DebugMsg( $"Effect entity is invalid, stop emitting until we have a valid effect entity again." );
			StopEmitting();

			// But also restart effect on valid effect entity.
			StartEmittingOnValidEffectEntity = true;
			return;
		}

		if ( StartEmittingOnValidEffectEntity )
		{
			StartEmittingOnValidEffectEntity = false;
			DebugMsg( $"Particle was disabled due to invalid effect entity, restarting now." );
			StartEmitting();
		}

		UpdateVisibility();
		UpdateEffectEntity();
	}

	public ModelEntity GetEffectEntity()
	{
		var currEffectEntity = Entity;

		if ( Entity is IHasEffectEntity effectEntityHolder )
			currEffectEntity = effectEntityHolder.GetEffectEntity();

		return currEffectEntity;
	}

	void UpdateVisibility()
	{
		// Particle can draw if the first control point is connected
		// to an entity that is currently not being view through first person.

		var canDraw = false;

		// Get first control point.
		var point = GetControlPoint( 0 );
		if ( point != null )
		{
			canDraw = true;

			// If our main entity is not drawing, we can't draw too.
			if ( !Entity.EnableDrawing ) 
				canDraw = false;
			
			var effectEntity = point.Entity;
			if ( effectEntity.IsValid() )
			{
				// If the entity we're attached to isn't visible, don't draw.
				if ( !effectEntity.EnableDrawing )
					canDraw = false;

				if ( effectEntity.IsFirstPersonMode )
					canDraw = false;
			}
		}

		Particle.EnableDrawing = canDraw;
	}

	void UpdateEffectEntity()
	{
		var effectEntity = GetEffectEntity();

		// See if any particle control points are currently attached to the effect entity.
		foreach ( var pair in Points )
		{
			var index = pair.Key;
			var point = pair.Value;

			// This control point doesn't use effect entity so there's no point in checking it.
			if ( !point.UseEffectEntity )
				continue;

			var shouldReset = false;

			// Check if the effect entity has changed.
			if ( point.Entity != effectEntity ) 
				shouldReset = true;

			if ( point.LastEntityModel != point.Entity.Model )
				shouldReset = true;

			if ( !shouldReset )
				return;

			var attachment = point.Attachment;
			var follow = point.Follow;
			var offset = point.Offset;

			DebugMsg( $"Changing {index} control point (effect entity changed.)" );
			SetControlPoint( index, effectEntity, attachment, offset, follow );
		}
	}

	public void StartEmitting()
	{
		StopEmitting();

		DebugMsg( "Started Emitting" );
		IsEmitting = true;

		Particle = Particles.Create( EffectName );
		foreach ( var index in Points.Keys )
			ApplyControlPoint( index );
	}

	public void StopEmitting( bool immediate = false )
	{
		DebugMsg( "Stopped Emitting" );
		IsEmitting = false;

		Particle?.Destroy( immediate );
		Particle = null;
	}

	public void Destroy( bool immediate = false )
	{
		StopEmitting( immediate );

		IsDestroyed = false;
		Event.Unregister( this );
		DebugMsg( $"Unlinking to events." );
	}

	void ApplyControlPoint( int index )
	{
		if ( Particle == null )
			return;

		var point = GetControlPoint( index );
		if ( point == null )
			return;
		DebugMsg( $"ApplyControlPoint: " + index );

		if ( point.Entity.IsValid() )
		{
			DebugMsg( $"ApplyControlPoint: Entity is valid." );
			if ( string.IsNullOrEmpty( point.Attachment ) )
			{
				Particle.SetEntity( index, point.Entity, point.Offset, point.Follow );
				DebugMsg( $"ApplyControlPoint: Attachment is null." );
			}
			else
			{
				Particle.SetEntityAttachment( index, point.Entity, point.Attachment, point.Offset, point.Follow ? ParticleAttachment.AttachmentFollow : ParticleAttachment.Attachment );
				DebugMsg( $"ApplyControlPoint: Attaching to {point.Attachment}." );
			}
		}
		else
		{
			Particle.SetPosition( index, point.Offset );
		}
	}

	public void SetControlPoint( int point, ModelEntity entity, string attachment, Vector3 offset, bool follow = true )
	{
		Points[point] = new ControlPoint
		{
			Entity = entity,
			Attachment = attachment,
			Follow = follow,
			Offset = offset,
			UseEffectEntity = entity == GetEffectEntity(),
			LastEntityModel = entity.Model
		};


		ApplyControlPoint( point );
	}

	public void SetControlPoint( int point, ModelEntity entity, string attachment, bool follow = true )
	{
		SetControlPoint( point, entity, attachment, 0, follow );
	}

	public void SetControlPoint( int point, ModelEntity entity, Vector3 offset, bool follow = true )
	{
		SetControlPoint( point, entity, "", offset, follow );
	}

	public void SetControlPoint( int point, ModelEntity entity, bool follow = true )
	{
		SetControlPoint( point, entity, "", 0, follow );
	}

	public void SetControlPoint( int point, Vector3 offset, bool follow = true )
	{
		SetControlPoint( point, null, "", offset, follow );
	}

	/// <summary>
	/// Calling this function will make particle not be automatically deleted 
	/// to cull particles.
	/// </summary>
	public void MakePersistent()
	{
		ExpirationTime = null;
	}

	public ControlPoint GetControlPoint( int point )
	{
		if ( Points.TryGetValue( point, out var cp ) )
			return cp;

		return null;
	}

	public class ControlPoint
	{
		public ModelEntity Entity;
		public string Attachment;
		public bool Follow;
		public Vector3 Offset;

		public bool UseEffectEntity;
		public Model LastEntityModel;
	}

	public bool IsVisible => Particle?.EnableDrawing ?? false;
	public bool IsValid => !IsDestroyed;

	void DebugMsg( string message )
	{
		if ( !cl_debug_entity_particles )
			return;

		Log.Info( $"{Entity} (\"{EffectName}\"): {message}" );
	}

	[ConVar.Client] public static float cl_entity_particle_auto_dispose_time { get; set; } = 10;
	[ConVar.Client] public static bool cl_debug_entity_particles { get; set; }
}
