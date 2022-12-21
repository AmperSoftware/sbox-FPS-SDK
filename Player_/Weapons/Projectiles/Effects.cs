using Sandbox;

namespace Amper.FPS;

public partial class Projectile
{
	public virtual string ExplosionParticleName => "";
	public virtual string ExplosionSoundEffect => "";


	/// <summary>
	/// Display clientside particle effects on detonation.
	/// </summary>
	[ClientRpc]
	public virtual void DoExplosionEffect( Vector3 position, Vector3 normal )
	{
		Game.AssertClient();

		var boom = Particles.Create( ExplosionParticleName, position );
		boom.SetForward( 0, normal );
		Sound.FromWorld( ExplosionSoundEffect, Position );
	}

	[ClientRpc]
	public void DoScorchTrace( Vector3 position, Vector3 normal )
	{
		var tr = Trace.Ray( position + normal * 10, position - normal * 10 )
			.Ignore( this )
			.WorldOnly()
			.Run();

		if ( tr.Hit )
		{
		}
	}

	public virtual string TrailAttachment => "trail";
	public virtual string TrailParticleName => "";
	public Particles Trail { get; set; }

	[ClientRpc]
	public virtual void CreateTrails()
	{
		DeleteTrails( true );

		if ( !string.IsNullOrEmpty( TrailParticleName ) )
			Trail = Particles.Create( TrailParticleName, this, TrailAttachment );
	}

	[ClientRpc]
	public virtual void DeleteTrails( bool immediate = false )
	{
		Trail?.Destroy( true );
	}
}
