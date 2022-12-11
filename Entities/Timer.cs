using Sandbox;
using System;
using System.Collections.Generic;

namespace Amper.FPS;

/// <summary>
/// Timer entity, used to count down the time for the purposes of a gamemode.
/// </summary>
public partial class Timer : Entity
{
	public new static List<Timer> All { get; set; } = new();

	/// <summary>
	/// Maximum amount of time this timer can reach.
	/// </summary>
	public float MaxTimerLength { get; set; } = 600;
	/// <summary>
	/// Start counting down from this value.
	/// </summary>
	public float StartTime { get; set; } = 300;
	/// <summary>
	/// Automatically start this timer when it's created.
	/// </summary>
	public bool StartActive { get; set; }
	/// <summary>
	/// If true, this timer will not be shown on the timer HUD.
	/// </summary>
	[Net] public bool HideFromHUD { get; set; }
	/// <summary>
	/// Should this timer reset on round restart?
	/// </summary>
	public bool ResetOnRoundStart { get; set; } = true;

	/// <summary>
	/// Defines if this timer is currently paused? Setting this to true will make timer freeze at it's current value.
	/// </summary>
	[Net] public bool Paused { get; set; }
	/// <summary>
	/// Count time relative to this value.
	/// </summary>
	[Net] public float AbsoluteTime { get; set; }
	/// <summary>
	/// The amount of time since the timer started counting.
	/// </summary>
	[Net] public TimeSince TimeSinceStartedCounting { get; set; }
	/// <summary>
	/// If this timer is owned by some team, this value will contain it.
	/// </summary>
	[Net] public int OwnerTeamNumber { get; set; }

	public bool IsVisibleOnHUD => !HideFromHUD;

	public Timer()
	{
		All.Add( this );
		EventDispatcher.Subscribe<RoundRestartEvent>( OnRoundRestart, this );
	}

	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
		TimeSinceStartedCounting = 0;
		Paused = true;

		SetTime( StartTime );

		if ( StartActive )
			Start();

		// Add this to timer registry.
		All.Add( this );
	}

	protected override void OnDestroy()
	{
		All.Remove( this );
		base.OnDestroy();
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	public void Start()
	{
		if ( !Game.IsServer )
			return;

		if ( Paused )
			OnStarted.Fire( this );

		Paused = false;
		TimeSinceStartedCounting = 0;
	}

	public void Restart()
	{
		if ( !Game.IsServer )
			return;

		OnRestarted.Fire( this );

		SetTime( StartTime );
		Start();
	}

	public void Pause()
	{
		if ( !Game.IsServer )
			return;

		if ( !Paused )
			OnPaused.Fire( this );

		AbsoluteTime = GetRemainingTime();
		Paused = true;
	}

	public void SetTime( float time )
	{
		AbsoluteTime = time;
		lastSecond = AbsoluteTime.FloorToInt();
	}

	public float GetRemainingTime()
	{
		if ( Paused ) return AbsoluteTime;
		return MathF.Max( 0, AbsoluteTime - TimeSinceStartedCounting );
	}

	public string GetTimeString()
	{
		return TimeSpan.FromSeconds( GetRemainingTime() ).ToString( @"mm\:ss" );
	}

	public bool IsElapsed()
	{
		return GetRemainingTime() == 0;
	}

	int lastSecond = -1;

	[Event.Tick.Server]
	void Tick()
	{
		if ( Paused )
			return;

		float timeLeft = GetRemainingTime();
		int second = timeLeft.FloorToInt();

		if ( second != lastSecond )
		{
			//
			// Outputs
			//
			switch ( second )
			{
				case 300: On5MinRemain.Fire( this ); break;
				case 240: On4MinRemain.Fire( this ); break;
				case 180: On3MinRemain.Fire( this ); break;
				case 120: On2MinRemain.Fire( this ); break;
				case 60: On1MinRemain.Fire( this ); break;
				case 30: On30SecRemain.Fire( this ); break;
				case 20: On20SecRemain.Fire( this ); break;
				case 10: On10SecRemain.Fire( this ); break;
				case 5: On5SecRemain.Fire( this ); break;
				case 4: On4SecRemain.Fire( this ); break;
				case 3: On3SecRemain.Fire( this ); break;
				case 2: On2SecRemain.Fire( this ); break;
				case 1: On1SecRemain.Fire( this ); break;
			}

			lastSecond = second;
		}

		if ( timeLeft == 0 )
		{
			Pause();
			OnFinished.Fire( this );
		}
	}

	public void OnRoundRestart( RoundRestartEvent args )
	{
		if ( ResetOnRoundStart )
			Restart();
	}

	protected Output On5MinRemain { get; set; }
	protected Output On4MinRemain { get; set; }
	protected Output On3MinRemain { get; set; }
	protected Output On2MinRemain { get; set; }
	protected Output On1MinRemain { get; set; }
	protected Output On30SecRemain { get; set; }
	protected Output On20SecRemain { get; set; }
	protected Output On10SecRemain { get; set; }
	protected Output On5SecRemain { get; set; }
	protected Output On4SecRemain { get; set; }
	protected Output On3SecRemain { get; set; }
	protected Output On2SecRemain { get; set; }
	protected Output On1SecRemain { get; set; }

	protected Output OnRestarted { get; set; }
	protected Output OnPaused { get; set; }
	protected Output OnStarted { get; set; }
	protected Output OnFinished { get; set; }
}
