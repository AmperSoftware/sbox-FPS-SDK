using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.FPS;

public partial class ScreenShake : BaseNetworkable
{
	public const float MaxAmplitude = 16;
	public const int MaxShakes = 32;

	public static List<Entry> All = new();

	public static void Shake( Vector3 center, float amplitude, float frequency, float duration, float radius, ShakeCommand command, bool airshake = false )
	{
		if ( amplitude > MaxAmplitude )
			amplitude = MaxAmplitude;

		foreach ( var client in Game.Clients )
		{
			var pawn = client.Pawn;
			
			if ( !pawn.IsValid() || pawn is not SDKPlayer ply )
				continue;

			if ( !airshake && command == ShakeCommand.Start && !ply.GroundEntity.IsValid() )
				continue;

			var localAmplitude = ComputeShakeAmplitude( center, ply.WorldSpaceBounds.Center, amplitude, radius );

			if ( localAmplitude < 0 )
				continue;

			TransmitShakeEvent( client, localAmplitude, frequency, duration, command );
		}
	}

	public static void TransmitShakeEvent( IClient player, float localAmplitude, float frequency, float duration, ShakeCommand command )
	{
		if ( (localAmplitude > 0) || command == ShakeCommand.Stop )
		{
			if ( command == ShakeCommand.Stop )
				localAmplitude = 0;

			ReceiveShakeEvent( To.Single( player ), localAmplitude, frequency, duration, command );
		}
	}

	[ClientRpc]
	public static void ReceiveShakeEvent( float localAmplitude, float frequency, float duration, ShakeCommand command )
	{
		if ( command == ShakeCommand.Start && All.Count < MaxShakes )
		{
			All.Add( new()
			{
				Amplitude = localAmplitude,
				Frequency = frequency,
				Duration = duration,
				NextShake = 0,
				EndTime = Time.Now + duration,
				Command = command
			} );
		}
		else if ( command == ShakeCommand.Stop )
		{
			All.Clear();
		}
		else if ( command == ShakeCommand.Amplitude )
		{
			var shake = FindLongest();
			if ( shake != null )
			{
				shake.Amplitude = localAmplitude;
			}
		}
		else if ( command == ShakeCommand.Frequency )
		{
			var shake = FindLongest();
			if ( shake != null )
			{
				shake.Frequency = frequency;
			}
		}
	}

	static Entry FindLongest()
	{
		return All.OrderByDescending( x => x.Duration ).FirstOrDefault();
	}

	static float ComputeShakeAmplitude( Vector3 center, Vector3 shakePt, float amplitude, float radius ) 
	{
		if ( radius <= 0 ) 
			return amplitude;

		float localAmplitude = -1;
		var delta = center - shakePt;
		float distance = delta.Length;

		if ( distance <= radius ) 
		{
			// Make the amplitude fall off over distance
			float flPerc = 1f - (distance / radius);
			localAmplitude = amplitude* flPerc;
		}

		return localAmplitude;
	}

	public class Entry
	{
		public float Amplitude;
		public float Frequency;
		public float Duration;
		public float NextShake;
		public float EndTime;
		public ShakeCommand Command;
		public Vector3 Offset;
	}
}

public enum ShakeCommand
{
	/// <summary>
	/// Starts the screen shake for all players within the radius.
	/// </summary>
	Start,
	/// <summary>
	/// Stops the screen shake for all players within the radius.
	/// </summary>
	Stop,
	/// <summary>
	/// Modifies the amplitude of an active screen shake for all players within the radius.
	/// </summary>
	Amplitude,
	/// <summary>
	/// Modifies the frequency of an active screen shake for all players within the radius.
	/// </summary>
	Frequency
};
