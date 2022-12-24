using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.FPS;

public class ParticleContainer : IValid
{
	public EntityParticle Particle { get; private set; }

	ModelEntity Entity;
	Func<string> AttachmentDelegate;
	Func<bool> EnabledDelegate;
	bool Follow;

	List<Binding> Bindings = new();
	bool IsDestroyed;

	Binding ActiveBinding;

	public ParticleContainer( ModelEntity entity, Func<string> attachmentDelegate, bool follow = true, Func<bool> enabledDelegate = null )
	{
		Assert.NotNull( entity );
		Assert.NotNull( attachmentDelegate );

		Entity = entity;
		Follow = follow;
		AttachmentDelegate = attachmentDelegate;
		EnabledDelegate = enabledDelegate;

		Event.Register( this );
	}

	public ParticleContainer( ModelEntity entity, string attachment, bool follow = true, Func<bool> enabledDelegate = null )
		: this( entity, () => attachment, follow, enabledDelegate ) { }

	[Event.Tick.Client]
	void Tick()
	{
		if ( !Entity.IsValid() )
		{
			Destroy( true );
			return;
		}

		UpdateActiveBinding();
		UpdateEffectAttachment();
	}

	public void UpdateActiveBinding()
	{
		var activeBinding = FindActiveBinding();
		if ( ActiveBinding != activeBinding )
		{
			if ( ActiveBinding != null )
			{
				// Call that this binding has stopped.
				ActiveBinding.OnStopped?.Invoke( Particle );
				StopEffect();
			}

			if ( activeBinding != null )
			{
				StartEffect( activeBinding.EffectNameDelegate.Invoke() );
				activeBinding.OnCreated?.Invoke( Particle );
			}

			ActiveBinding = activeBinding;
		}
	}

	string lastAttachment;
	public void UpdateEffectAttachment()
	{
		if ( Particle == null )
			return;

		var newAttach = AttachmentDelegate.Invoke();
		if ( newAttach != lastAttachment )
		{
			DebugMsg( "Attachment Changed!" );
			RestartEffect();
		}
	}

	public EntityParticle StartEffect( string effectname, bool immediate = false )
	{
		StopEffect();

		lastAttachment = AttachmentDelegate.Invoke();
		Particle = Entity.CreateParticle( effectname, lastAttachment, Follow );
		return Particle;
	}

	public void StopEffect( bool immediate = false )
	{
		Particle?.Destroy( immediate );
		Particle = null;
	}

	public void ForceStopActiveBinding()
	{
		if ( ActiveBinding != null )
		{
			ActiveBinding.OnStopped?.Invoke( Particle );
			ActiveBinding = null;
		}
	}

	public void RestartEffect( bool stopImmediate = false )
	{
		// Nothing to restart.
		if ( Particle == null )
			return;

		var effectName = Particle.EffectName;
		StartEffect( effectName, stopImmediate );
	}

	Binding FindActiveBinding()
	{
		// if enable delegate returns false, 
		// means we need to have no particles on right now.
		if ( EnabledDelegate != null && !EnabledDelegate.Invoke() ) 
			return null;

		for ( var i = 0; i < Bindings.Count; i++ )
		{
			var binding = Bindings[i];
			if ( binding == null )
				continue;

			if ( !binding.ConditionDelegate.Invoke() )
				continue;

			return binding;
		}

		return null;
	}

	public void Destroy( bool immediate = false )
	{
		Particle?.Destroy( immediate );
		IsDestroyed = true;

		Event.Unregister( this );
	}

	public class Binding
	{
		public Func<string> EffectNameDelegate;
		public Func<bool> ConditionDelegate;
		public int Priority;

		public Action<EntityParticle> OnCreated;
		public Action<EntityParticle> OnStopped;
		public Action<EntityParticle> OnTick;
	}

	public Binding Bind( string effectName, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null, Action<EntityParticle> onStopped = null, Action<EntityParticle> onTick = null )
	{
		return Bind( () => effectName, priority, condition, onCreated, onStopped, onTick );
	}

	public Binding Bind( Func<string> nameDelegate, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null, Action<EntityParticle> onStopped = null, Action<EntityParticle> onTick = null )
	{
		return AddBinding( nameDelegate, priority, condition, onCreated, onStopped, onTick );
	}

	Binding AddBinding( Func<string> nameDelegate, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null, Action<EntityParticle> onStopped = null, Action<EntityParticle> onTick = null )
	{
		Assert.NotNull( nameDelegate );
		Assert.NotNull( condition );

		var binding = new Binding()
		{
			EffectNameDelegate = nameDelegate,
			ConditionDelegate = condition,
			Priority = priority,
			OnCreated = onCreated,
			OnStopped = onStopped,
			OnTick = onTick
		};

		Bindings.Add( binding );
		Bindings = Bindings.OrderByDescending( x => x.Priority ).ToList();

		return binding;
	}

	void DebugMsg( string message )
	{
		if ( !cl_debug_entity_containers )
			return;

		Log.Info( $"{Entity}: {message}" );
	}

	public bool IsValid => !IsDestroyed;
	public bool IsVisible => Particle?.IsVisible ?? false;

	[ConVar.Client] public static bool cl_debug_entity_containers { get; set; }
}
