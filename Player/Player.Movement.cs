using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKPlayer
{
	[ClientInput] public Angles ViewAngles { get; set; }
	//[ClientInput] public Angles ActiveWeapon { get; set; }

	//
	// View Angles
	//
	Angles? _forceViewAngles { get; set; }

	/// <summary>
	/// Forces the player to change override their input view angles
	/// and look at a specific angle. Can be called from both server 
	/// and client with the same effect.
	/// </summary>
	public void ForceViewAngles( Angles angles )
	{
		if ( Game.IsServer ) ForceViewAnglesRPC( angles );
		if ( Game.IsClient ) _forceViewAngles = angles;
	}

	[ClientRpc]
	private void ForceViewAnglesRPC( Angles angles )
	{
		ForceViewAngles( angles );
	}

	[Net, Predicted] public float MaxSpeed { get; set; }
	[Net, Predicted] public MoveType MoveType { get; set; }
	public float SurfaceFriction { get; set; } = 1;
	public Surface SurfaceData { get; set; }

	//
	// Jumps and Air Dash
	//

	public virtual int MaxAirDashes => 0;
	[Net, Predicted] public int AirDashCount { get; set; }


	//
	// Ducking
	//

	public virtual int MaxAirDucks => 1;
	public bool IsDucking => DuckTime > 0;
	public float DuckProgress => Math.Clamp( DuckTime / SDKGame.Current.Movement.TimeToDuck, 0, 1 );
	[Net, Predicted] public float DuckTime { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
	[Net, Predicted] public bool IsDucked { get; set; }
	[Net, Predicted] public int AirDuckCount { get; set; }
	[Net, Predicted] public float LastDuckTime { get; set; }

	//
	// Water
	//

	[Net, Predicted] public float NextSwimSoundTime { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }
	[Net, Predicted] public Vector3 WaterJumpVelocity { get; set; }
	[Net, Predicted] public float WaterJumpTime { get; set; }
	[Net, Predicted] public float WaterEntryTime { get; set; }
	public bool IsJumpingFromWater => WaterJumpTime != 0;

	//
	// Stuck
	//

	public int LastStuckOffsetIndex { get; set; }
	public float[] LastStuckCheckTime { get; set; } = new float[2];

	public virtual float StepSize => GameMovement.sv_stepsize;

	[Net, Predicted] public PlayerFlags Flags { get; set; }

	public void AddFlags( PlayerFlags flag ) { Flags |= flag; }
	public void RemoveFlag( PlayerFlags flag ) { Flags &= ~flag; }


	[ConVar.Replicated] public static bool mp_freeze_on_round_start { get; set; } = true;

	/// <summary>
	/// Can the player move right now?
	/// </summary>
	public virtual bool CanMove()
	{
		if ( SDKGame.Current.IsWaitingForPlayers )
			return true;

		if ( mp_freeze_on_round_start )
		{
			if ( SDKGame.Current.IsRoundStarting )
				return false;
		}

		return true;
	}

	/// <summary>
	/// Can the player jump right now?
	/// </summary>
	public virtual bool CanJump()
	{
		// Can't jump if they're not alive.
		if ( !IsAlive )
			return false;

		// Our active weapon doesn't let us jump.
		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerJump() )
			return false;

		// We are in the air, we can't jump. (air jumps are handled by a separate function.)
		if ( IsInAir )
			return false;

		return true;
	}

	/// <summary>
	/// Can we perform a jump in the air?
	/// </summary>
	public virtual bool CanAirDash()
	{
		// We're not alive lol.
		if ( !IsAlive )
			return false;

		// Weapon doesn't allow us to make an air dash.
		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerAirDash() )
			return false;

		// Air dash can only be executed in air.
		if ( IsGrounded )
			return false;

		// We have run out of our maximum air dashes.
		if ( AirDashCount >= MaxAirDashes )
			return false;

		return true;
	}

	/// <summary>
	/// Can we duck?
	/// </summary>
	public virtual bool CanDuck()
	{
		// Can't duck while we're not alive.
		if ( !IsAlive )
			return false;

		// Weapon doesn't allow us to duck.
		if ( ActiveWeapon.IsValid() && !ActiveWeapon.CanOwnerDuck() )
			return false;

		// Are we in the air?
		if ( IsInAir )
		{
			// We can only duck so many times.
			if ( AirDuckCount >= MaxAirDucks )
				return false;
		}

		return true;
	}

	/// <summary>
	/// Can we unduck?
	/// </summary>
	public virtual bool CanUnduck()
	{
		if ( !IsAlive )
			return false;

		// There is no logic made here 
		// that would force us to keep ducking. But you
		// can add it here if you need to.

		return true;
	}
}

[Flags]
public enum PlayerFlags
{
	FL_FROZEN = 1 << 0,
	FL_ONTRAIN = 1 << 1,
	FL_WATERJUMP = 1 << 2
}

/// <summary>
/// These are movetypes that are used in the Source SDK.
/// Garry recently removed them from sbox natively so we need
/// to keep the here manually.
/// </summary>
public enum MoveType
{
	None,
	Isometric,
	Walk,
	Step,
	Fly,
	FlyGravity,
	Physics,
	Push,
	NoClip,
	Ladder,
	Observer
}
