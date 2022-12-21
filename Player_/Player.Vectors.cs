namespace Amper.FPS;

partial class SDKPlayer
{
	public virtual Vector3 GetPlayerMins( bool ducked )
	{
		if ( IsObserver ) 
			return ViewVectors.ObserverHullMin;
		else 
			return ducked ? ViewVectors.DuckHullMin : ViewVectors.HullMin;
	}

	public Vector3 GetPlayerMinsScaled( bool ducked )
	{
		return GetPlayerMins( ducked ) * Scale;
	}

	public virtual Vector3 GetPlayerMaxs( bool ducked )
	{
		if ( IsObserver ) 
			return ViewVectors.ObserverHullMax;
		else 
			return ducked ? ViewVectors.DuckHullMax : ViewVectors.HullMax;
	}

	public Vector3 GetPlayerMaxsScaled( bool ducked )
	{
		return GetPlayerMaxs( ducked ) * Scale;
	}

	public virtual Vector3 GetPlayerExtents( bool ducked )
	{
		var mins = GetPlayerMins( ducked );
		var maxs = GetPlayerMaxs( ducked );

		return mins.Abs() + maxs.Abs();
	}

	public Vector3 GetPlayerExtentsScaled( bool ducked )
	{
		return GetPlayerExtents( ducked ) * Scale;
	}

	public virtual Vector3 GetPlayerViewOffset( bool ducked )
	{
		return ducked ? ViewVectors.DuckViewOffset : ViewVectors.ViewOffset;
	}

	public Vector3 GetPlayerViewOffsetScaled( bool ducked )
	{
		return GetPlayerViewOffset( ducked ) * Scale;
	}

	public virtual Vector3 GetDeadViewHeight()
	{
		return ViewVectors.DeadViewOffset;
	}

	public Vector3 GetDeadViewHeightScaled()
	{
		return GetDeadViewHeight() * Scale;
	}

	public virtual ViewVectors ViewVectors => new()
	{
		ViewOffset = new( 0, 0, 64 ),

		HullMin = new( -16, -16, 0 ),
		HullMax = new( 16, 16, 72 ),

		DuckHullMin = new( -16, -16, 0 ),
		DuckHullMax = new( 16, 16, 36 ),
		DuckViewOffset = new( 0, 0, 28 ),

		ObserverHullMin = new( -10, -10, -10 ),
		ObserverHullMax = new( 10, 10, 10 ),

		DeadViewOffset = new( 0, 0, 14 )
	};
}

public struct ViewVectors
{
	public Vector3 ViewOffset { get; set; }

	public Vector3 HullMin { get; set; }
	public Vector3 HullMax { get; set; }

	public Vector3 DuckHullMin { get; set; }
	public Vector3 DuckHullMax { get; set; }
	public Vector3 DuckViewOffset { get; set; }

	public Vector3 ObserverHullMax { get; set; }
	public Vector3 ObserverHullMin { get; set; }

	public Vector3 DeadViewOffset { get; set; }
}
