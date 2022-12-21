using Sandbox;

namespace Amper.FPS;

/// <summary>
/// Simple class for counting down a short interval of time.
/// Upon creation, the timer is invalidated. Invalidated countdown timers are considered to have elapsed.
/// </summary>
public class CountdownTimer
{
	float ElapseTime = -1;
	float Duration = 0;
	bool FinishCalled = false;

	/// <summary>
	/// Start the timer with this amount of seconds.
	/// </summary>
	public void Start( float time )
	{
		Invalidate();
		ElapseTime = Time.Now + time;
		Duration = time;
	}

	public void Start( float min, float max )
	{
		Start( Game.Random.Float( min, max ) );
	}

	/// <summary>
	/// Reset the timer, start it again.
	/// </summary>
	public void Reset()
	{
		ElapseTime = Time.Now + Duration;
	}

	/// <summary>
	/// Stop the timer.
	/// </summary>
	public void Invalidate()
	{
		ElapseTime = -1;
		FinishCalled = false;
	}

	/// <summary>
	/// Has the timer been started?
	/// </summary>
	public bool HasStarted() => ElapseTime > 0;

	/// <summary>
	/// Has the timer elapsed?
	/// </summary>
	public bool IsElapsed() => Time.Now > ElapseTime;

	/// <summary>
	/// How much time has elapsed exactly?
	/// </summary>
	public float GetElapsedTime() => Time.Now - ElapseTime + Duration;

	/// <summary>
	/// How much time is remaining?
	/// </summary>
	public float GetRemainingTime() => ElapseTime - Time.Now;

	/// <summary>
	/// Has this timer finished? (NOTE: This is called only once per countdown 
	/// so you can track down the exact tick when this timer has elapsed)
	/// </summary>
	public bool HasFinished()
	{
		if ( FinishCalled )
			return false;

		if ( HasStarted() && IsElapsed() )
		{
			FinishCalled = true;
			return true;
		}

		return false;
	}
}
