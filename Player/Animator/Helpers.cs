using Sandbox;

namespace Amper.FPS;

partial class PlayerAnimator
{
	/// <summary>
	/// We'll convert Position to a local position to the players eyes and set
	/// the param on the animgraph.
	/// </summary>
	public virtual void SetLookAt( string name, Vector3 Position )
	{
		var localPos = (Position - Player.GetEyePosition()) * Player.Rotation.Inverse;
		SetAnimParameter( name, localPos );
	}

	/// <summary>
	/// Sets the param on the animgraph
	/// </summary>
	public virtual void SetAnimParameter( string name, Vector3 val )
	{
		Player?.SetAnimParameter( name, val );
	}

	/// <summary>
	/// Sets the param on the animgraph
	/// </summary>
	public virtual void SetAnimParameter( string name, float val )
	{
		Player?.SetAnimParameter( name, val );
	}

	/// <summary>
	/// Sets the param on the animgraph
	/// </summary>
	public virtual void SetAnimParameter( string name, bool val )
	{
		Player?.SetAnimParameter( name, val );
	}

	/// <summary>
	/// Sets the param on the animgraph
	/// </summary>
	public virtual void SetAnimParameter( string name, int val )
	{
		Player?.SetAnimParameter( name, val );
	}

	/// <summary>
	/// Calls SetParam( name, true ). It's expected that your animgraph
	/// has a "name" param with the auto reset property set.
	/// </summary>
	public virtual void Trigger( string name )
	{
		SetAnimParameter( name, true );
	}

	/// <summary>
	/// Resets all params to default values on the animgraph
	/// </summary>
	public virtual void ResetParameters()
	{
		Player?.ResetAnimParameters();
	}
}
