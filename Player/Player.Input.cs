using Sandbox;
using System;

namespace Amper.FPS;

public partial class SDKPlayer
{
	[Obsolete("Use InputViewAngles")]
	public Angles ViewAngles { get => InputViewAngles; set => InputViewAngles = value; }

	[ClientInput] public Vector3 InputMoveDirection { get; set; }
	[ClientInput] public Angles InputViewAngles { get; set; }
	[ClientInput] public SDKWeapon InputActiveWeapon { get; set; }
	Angles? ForcedViewAngles { get; set; }

	/// <summary>
	/// Called from the gamemode, clientside only.
	/// </summary>
	public override void BuildInput()
	{
		if ( Input.StopProcessing )
			return;

		// Update our current viewangles by the look delta.
		InputViewAngles += Input.AnalogLook;

		// If we have a foced view angle, switch to it.
		if ( ForcedViewAngles.HasValue )
		{
			// Copy the angle and reset the field.
			Input.AnalogLook = ForcedViewAngles.Value;
			ForcedViewAngles = null;
		}

		InputViewAngles = InputViewAngles.WithPitch( InputViewAngles.pitch.Clamp( -89f, 89f ) );
	}

	// 

	/// <summary>
	/// Forces the player to change override their input view angles
	/// and look at a specific angle. Can be called from both server 
	/// and client with the same effect.
	/// </summary>
	public void ForceViewAngles( Angles angles )
	{
		if ( Game.IsServer ) ForceViewAnglesRPC( angles );
		if ( Game.IsClient ) ForcedViewAngles = angles;
	}

	[ClientRpc]
	private void ForceViewAnglesRPC( Angles angles )
	{
		ForceViewAngles( angles );
	}
}
