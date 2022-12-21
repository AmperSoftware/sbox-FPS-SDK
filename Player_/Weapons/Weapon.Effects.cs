using Sandbox;

namespace Amper.FPS;

partial class SDKWeapon : IHasEffectEntity
{
	public virtual ModelEntity GetEffectEntity()
	{
		return IsLocalPawn && IsFirstPersonMode
			? Player?.GetViewModel( ViewModelIndex )
			: this;
	}

	public virtual void DoRecoil() { }

	private static int TracerCount { get; set; }
	public virtual string GetParticleTracerEffect() => "";

	[ClientRpc]
	public void CreateBulletTracer( Vector3 endPos )
	{
		// get the tracer particle.
		string particle = GetParticleTracerEffect();
		if ( string.IsNullOrEmpty( particle ) )
			return;

		var traceFreq = GetTracerFrequency();

		// This weapon doesn't have any tracers.
		if ( traceFreq <= 0 )
			return;

		// Throttle tracer particles.
		if ( TracerCount++ % traceFreq != 0 )
			return;

		// Grab the entity we're supposed to draw effects from.
		var attachEnt = GetEffectEntity();
		if ( !attachEnt.IsValid() )
			return;

		// Create the particle effect
		Particles tracer = Particles.Create( particle, attachEnt, "muzzle" );
		tracer.SetPosition( 1, endPos );
	}

	public virtual string GetMuzzleFlashEffect() => "";

	[ClientRpc]
	public virtual void CreateMuzzleFlash()
	{
		// get the tracer particle.
		string particle = GetMuzzleFlashEffect();
		if ( string.IsNullOrEmpty( particle ) )
			return;

		// Grab the entity we're supposed to draw effects from.
		var attachEnt = GetEffectEntity();
		if ( !attachEnt.IsValid() )
			return;

		Particles.Create( particle, attachEnt, "muzzle" );
	}
	
	/// <summary>
	/// This will play an unprecited sound. If you're playing a sound serverside on a predicted 
	/// entity (like weapons on pawns) it will not be played on the client because it will be culled by prediction. 
	/// This function solves this.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Sound PlayUnpredictedSound( string name )
	{
		using ( Prediction.Off() ) return PlaySound( name );
	}

	public new Sound PlaySound( string soundName )
	{
		var originEnt = Owner ?? this;
		return Sound.FromEntity( soundName, originEnt );
	}
}
