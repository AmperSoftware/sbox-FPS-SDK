using System.Collections.Generic;

namespace Amper.FPS;

public abstract class BaseMoveController
{
	public HashSet<MoveComponent> Components = new();
	public SDKPlayer Player;

	public void PreTick( SDKPlayer player )
	{
		Player = player;

		foreach ( var comp in Components )
		{
			if ( !comp.ShouldSimulateFor( player ) )
				continue;

			comp.CopyFrom( player );
			comp.PreTick( player );
			comp.CopyTo( player );
		}
	}

	public void Tick( SDKPlayer player )
	{
		Player = player;

		foreach ( var comp in Components )
		{
			if ( !comp.ShouldSimulateFor( player ) )
				continue;

			comp.CopyFrom( player );
			comp.Tick( player );
			comp.CopyTo( player );
		}
	}

	public void PostTick( SDKPlayer player )
	{
		Player = player;

		foreach ( var comp in Components )
		{
			if ( !comp.ShouldSimulateFor( player ) )
				continue;

			comp.CopyFrom( player );
			comp.PostTick( player );
			comp.CopyTo( player );
		}
	}

	public void FrameSimulate( SDKPlayer player )
	{
		Player = player;

		foreach ( var comp in Components )
		{
			comp.CopyFrom( player );
			comp.FrameSimulate( player );
			comp.CopyTo( player );
		}
	}

	public void AddComponent<T>() where T : MoveComponent, new()
	{
		var c = new T();
		AddComponent( c );
	}

	public void AddComponent( MoveComponent component )
	{
		component.Outer = this;
		Components.Add( component );
	}
}

public partial class MoveComponent
{
	public BaseMoveController Outer;

	public Vector3 Position;
	public Rotation Rotation;
	public Vector3 EyeLocalPosition;
	public Rotation EyeLocalRotation;
	public Vector3 Velocity;
	public Vector3 BaseVelocity;

	public virtual bool ShouldSimulateFor( SDKPlayer player ) => true;

	public virtual void PreTick( SDKPlayer player ) { }
	public virtual void Tick( SDKPlayer player ) { }
	public virtual void PostTick( SDKPlayer player ) { }
	public virtual void FrameSimulate( SDKPlayer player ) { }

	public virtual void CopyFrom( SDKPlayer player )
	{
		Position = player.Position;
		Rotation = player.Rotation;
		EyeLocalPosition = player.EyeLocalPosition;
		EyeLocalRotation = player.EyeLocalRotation;
		Velocity = player.Velocity;
		BaseVelocity = player.BaseVelocity;
	}

	public virtual void CopyTo( SDKPlayer player )
	{
		Position = player.Position;
		Rotation = player.Rotation;
		EyeLocalPosition = player.EyeLocalPosition;
		EyeLocalRotation = player.EyeLocalRotation;
		Velocity = player.Velocity;
		BaseVelocity = player.BaseVelocity;
	}
}
