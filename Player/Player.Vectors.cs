using System;

namespace Amper.FPS;

partial class SDKPlayer
{
	public ViewState ViewStanding;
	public ViewState ViewDucking;
	public ViewState ViewDead;
	public ViewState ViewObserver;

	[Obsolete]
	public virtual Vector3 GetPlayerMins( bool ducked )
	{
		if ( IsObserver ) 
			return _ViewVectors.ObserverHullMin;
		else 
			return ducked ? _ViewVectors.DuckHullMin : _ViewVectors.HullMin;
	}

	[Obsolete]
	public Vector3 GetPlayerMinsScaled( bool ducked )
	{
		return GetPlayerMins( ducked ) * Scale;
	}

	[Obsolete]
	public virtual Vector3 GetPlayerMaxs( bool ducked )
	{
		if ( IsObserver ) 
			return _ViewVectors.ObserverHullMax;
		else 
			return ducked ? _ViewVectors.DuckHullMax : _ViewVectors.HullMax;
	}

	[Obsolete]
	public Vector3 GetPlayerMaxsScaled( bool ducked )
	{
		return GetPlayerMaxs( ducked ) * Scale;
	}

	[Obsolete]
	public virtual Vector3 GetPlayerExtents( bool ducked )
	{
		var mins = GetPlayerMins( ducked );
		var maxs = GetPlayerMaxs( ducked );

		return mins.Abs() + maxs.Abs();
	}

	[Obsolete]
	public Vector3 GetPlayerExtentsScaled( bool ducked )
	{
		return GetPlayerExtents( ducked ) * Scale;
	}

	[Obsolete]
	public virtual Vector3 GetPlayerViewOffset( bool ducked )
	{
		return ducked ? _ViewVectors.DuckViewOffset : _ViewVectors.ViewOffset;
	}

	[Obsolete]
	public Vector3 GetPlayerViewOffsetScaled( bool ducked )
	{
		return GetPlayerViewOffset( ducked ) * Scale;
	}

	[Obsolete]
	public virtual Vector3 GetDeadViewHeight()
	{
		return _ViewVectors.DeadViewOffset;
	}

	[Obsolete]
	public Vector3 GetDeadViewHeightScaled()
	{
		return GetDeadViewHeight() * Scale;
	}

	[Obsolete]
	public virtual ViewState _ViewVectors => new();
}

public struct ViewState
{
	public Vector3 Offset;
	public BBox Hull;

	public ViewState( Vector3 offset, BBox bodyHull )
	{
		Offset = offset;
		Hull = bodyHull;
	}

	public ViewState( Vector3 offset, Vector3 bodyMins, Vector3 bodyMaxs )
		: this( offset, new BBox( bodyMins, bodyMaxs ) )
	{
	}
}
