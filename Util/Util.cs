using Sandbox;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Amper.FPS;

public static partial class Util
{
	public static string GetMapDisplayName( string map )
	{
		var mapname = map.Split( "." ).Last();

		// split words by "_"
		var words = mapname.Split( "_" );
		for ( var i = 0; i < words.Length; i++ )
		{
			var word = words[i];

			if ( word.Length == 0 )
				continue;

			// capitalize first letter in the word
			words[i] = string.Concat( word[0].ToString().ToUpper(), word.AsSpan( 1 ) );
		}

		// If there are at least two words in the map name, cut the first one
		if ( words.Length > 1 )
			words = words.Skip( 1 ).ToArray();

		// join all the words back separated by whitespace
		var name = string.Join( " ", words );
		return name;
	}

	public static string Compress( this string s )
	{
		var bytes = Encoding.Unicode.GetBytes( s );
		using ( var msi = new MemoryStream( bytes ) )
		using ( var mso = new MemoryStream() )
		{
			using ( var gs = new GZipStream( mso, CompressionMode.Compress ) )
			{
				msi.CopyTo( gs );
			}
			return Convert.ToBase64String( mso.ToArray() );
		}
	}

	public static string Decompress( this string s )
	{
		var bytes = Convert.FromBase64String( s );
		using ( var msi = new MemoryStream( bytes ) )
		using ( var mso = new MemoryStream() )
		{
			using ( var gs = new GZipStream( msi, CompressionMode.Decompress ) )
			{
				gs.CopyTo( mso );
			}
			return Encoding.Unicode.GetString( mso.ToArray() );
		}
	}

	public static float RemapClamped( this float val, float A, float B, float C = 0, float D = 1 )
	{
		if ( A == B )
			return fsel( val - B, D, C );
		float cVal = (val - A) / (B - A);
		cVal = Math.Clamp( cVal, 0.0f, 1.0f );

		return C + (D - C) * cVal;
	}

	public static float RemapVal( this float val, float A, float B, float C = 0, float D = 1 )
	{
		if ( A == B )
			return fsel( val - B, D, C );
		return C + (D - C) * (val - A) / (B - A);
	}

	private static float fsel( float c, float x, float y ) => c >= 0 ? x : y;

	// hermite basis function for smooth interpolation
	// Similar to Gain() above, but very cheap to call
	// value should be between 0 & 1 inclusive
	public static float SimpleSpline( float value )
	{
		float valueSquared = value * value;

		// Nice little ease-in, ease-out spline-like curve
		return (3 * valueSquared - 2 * valueSquared * value);
	}

	public static string JPGToPNG( string jpg )
	{
		if ( !string.IsNullOrEmpty( jpg ) )
		{
			string noExtension = Path.Combine( Path.GetDirectoryName( jpg ), Path.GetFileNameWithoutExtension( jpg ) );
			return $"/{noExtension}.png";
		}
		return "";
	}

	/// <summary>
	/// YRES(y) macro from Source SDK.
	/// </summary>
	public static float ResY( this float y ) => y * SDKGame.Current.ScreenSize.y / 480;
	/// <summary>
	/// XRES(y) macro from Source SDK.
	/// </summary>
	public static float ResX( this float x ) => x * SDKGame.Current.ScreenSize.x / 640;

	public static int NearestToInt(this float num)
	{
		return (int)(num < 0 ? (num - 0.5) : (num + 0.5));
	}

	public static float ApproachAngle( this float value, float target, float speed )
	{
		float delta = target - value;

		// Speed is assumed to be positive
		if ( speed < 0 )
			speed = -speed;

		if ( delta < -180 )
			delta += 360;
		else if ( delta > 180 )
			delta -= 360;

		if ( delta > speed )
			value += speed;
		else if ( delta < -speed )
			value -= speed;
		else
			value = target;

		return value;
	}

	public static float AngleDifference( this float srcAngle, float destAngle )
	{
		float delta;

		delta = (destAngle - srcAngle) % 360;
		if ( destAngle > srcAngle )
		{
			if ( delta >= 180 )
				delta -= 360;
		}
		else
		{
			if ( delta <= -180 )
				delta += 360;
		}
		return delta;
	}

	public static bool InRange( this float value, float min, float max )
	{
		// if min is greater than max, 
		// swap the values.
		if ( min > max )
		{
			var buff = min;
			min = max;
			max = buff;
		}

		return value >= min && value <= max;
	}
}

