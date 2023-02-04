using System;
using System.Collections.Generic;
using Sandbox;

namespace Amper.FPS;

public class PostProcessingManager
{
	Dictionary<Type, RenderHook> Effects = new();
	Dictionary<Type, bool> EnabledCached = new();
	Dictionary<Type, bool> ForceEnabled = new();

	public void FrameSimulate()
	{
		Update();

		foreach ( var pair in ForceEnabled )
		{
			var type = pair.Key;
			var enabled = pair.Value;

			SetVisible( type, enabled );
		}
	}

	public virtual void Update() { }

	public T GetOrCreate<T>() where T : RenderHook, new()
	{
		var type = typeof( T );
		return (T)GetOrCreate( type );
	}

	public RenderHook GetOrCreate( Type type )
	{
		RenderHook effect;

        if ( Effects.TryGetValue( type, out effect ) && effect != null )
			return effect;

		effect = Camera.Main.FindHook(type);
		if(effect == null)
		{
            effect = TypeLibrary.Create<RenderHook>(type);
            Camera.Main.AddHook(effect);
        }
		Effects[type] = effect;
		SetVisible( effect, false );
		return effect;
	}

	public bool IsVisible<T>() where T : RenderHook
	{
		var type = typeof( T );

		EnabledCached.TryGetValue( type, out var enabled );
		return enabled;
	}

	public void SetVisible<T>( bool visible ) where T : RenderHook, new()
	{
		if ( !Game.IsClient )
			return;

		var cachedVisible = IsVisible<T>();
		if ( visible == cachedVisible )
			return;

		var effect = GetOrCreate<T>();
		SetVisible( effect, visible );
	}

	public void SetVisible( RenderHook effect, bool visible )
	{
		if ( effect == null )
			return;

		Log.Info( $"SetVisible {effect.GetType().Name} {visible}" );
		effect.Enabled = visible;

		var type = effect.GetType();
		EnabledCached[type] = visible;
	}

	public void SetVisible( Type type, bool visible )
	{
		var effect = GetOrCreate( type );
		if ( effect == null )
			return;

		SetVisible( effect, visible );
	}

	public void SetForced( string name, bool enabled )
	{
		var type = TypeLibrary.GetType( name );
		ForceEnabled[type.TargetType] = enabled;
	}

	[ConCmd.Client( "r_postprocess_force" )]
	public static void Command_ForcePostProcessing( string name, bool enabled )
	{
		var manager = SDKGame.Current.PostProcessingManager;
		if ( manager == null )
			return;

		try
		{
			manager.SetForced( name, enabled );
			Log.Info( $"Changed \"{name}\" effect visibility: {enabled}." );
		}
		catch
		{
			Log.Info( $"Failed to change visibility of \"{name}\" effect." );
		}
	}
}
