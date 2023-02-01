using Sandbox;
using System;

namespace Amper.FPS;

partial class SDKWeapon
{
	[Net, Predicted] public bool IsReloading { get; set; }
	[Net, Predicted] public float NextReloadCycleTime { get; set; }
	[Net, Predicted] public bool FullReloadCycle { get; set; }

	public virtual bool WishReload() => Input.Down( InputButton.Reload );

	public virtual void SimulateReload()
	{
		// Player has requested a reload.
		if ( WishReload() || ShouldAutoReload() )
			Reload();

		// We're in the process of reloading.
		if ( IsReloading )
			ContinueReload();
	}

	/// <summary>
	/// Starts reloading.
	/// </summary>
	public virtual void Reload()
	{
		// Can't reload now.
		if ( !CanReload() )
			return;

		// We're already reloading.
		if ( IsReloading )
			return;

		// Play the reload start animation.
		// If weapon reloads entire clip at once, this will trigger the full animation.
		// If it reloads clip one by one, this will raise our hand, and insert animation will be triggered by b_insert.
		SendAnimParametersOnReloadStart();

		// We're now reloading.
		IsReloading = true;

		// Schedule our next reload. Full cycle will begin after we reach the first one.
		FullReloadCycle = false;
		NextReloadCycleTime = Time.Now + GetReloadStartTime();
	}

	public virtual void ContinueReload()
	{
		if ( !CanReload() )
		{
			// If we can't reload, and we're reloading now. Finish our reloading.
			if ( IsReloading )
				StopReload();

			// Then prevent reloading from happening again, until we can reload again.
			return;
		}

		// We have reached a new reload cycle.
		if ( NextReloadCycleTime <= Time.Now )
		{
			FinishedReloadCycle();
		}
	}

	public virtual void FinishedReloadCycle()
	{
		// If we have made a full reload cycle, then add clip to the magazine
		if ( FullReloadCycle )
			ReloadRefillClip();

		// If we still can reload, start a new cycle.
		if ( CanReload() )
		{
			StartReloadCycle();
		}
	}

	public virtual void ReloadRefillClip()
	{
		var neededClips = GetClipsPerReloadCycle();

		// If we have infinite clips, just fulfill our clip count and early out.
		if ( sv_infinite_clips )
		{
			Clip += neededClips;
			return;
		}

		var addedClips = TakeFromReserve( neededClips );
		Clip += addedClips;
	}

	[ConVar.Replicated] public static bool sv_infinite_clips { get; set; }

	public virtual int GetClipsPerReloadCycle()
	{
		if ( IsReloadingEntireClip() )
			return Math.Max( GetClipSize() - Clip, 0 );

		return 1;
	}

	public virtual void StopReload()
	{
		if ( !IsReloading )
			return;

		SendAnimParametersOnReloadStop();
		IsReloading = false;
	}

	public virtual void StartReloadCycle()
	{
		if ( !IsReloading )
			return;

		FullReloadCycle = true;
		SendAnimParametersOnReloadInsert();
		NextReloadCycleTime = Time.Now + GetReloadTime();
	}

	public virtual bool CanReload()
	{
		// If we don't need ammo, we can't reload.
		if ( !NeedsAmmo() )
			return false;

		// Don't have any reserve.
		if ( Reserve <= 0 )
			return false;

		// Our clip is full
		if ( Clip >= GetClipSize() )
			return false;

		// We're not done shooting yet.
		if ( NextPrimaryAttackTime >= Time.Now )
			return false;

		return true;
	}

	public virtual void SendAnimParametersOnReloadInsert()
	{
	}

	public virtual void SendAnimParametersOnReloadStart()
	{
		//Log.NetInfo( "SendAnimParametersOnReloadStart" );
		SendAnimParameter( "reload", true );
	}

	public virtual void SendAnimParametersOnReloadStop()
	{
		SendAnimParameter( "reload", false );
	}
}
