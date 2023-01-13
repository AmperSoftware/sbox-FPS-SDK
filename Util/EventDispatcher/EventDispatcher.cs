using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Amper.FPS;

public enum DispatchType { None = 0, Client = 1, Server = 2 }

public delegate void EventDelegate( DispatchableEventBase args );

/// <summary>
/// Provides sending, subscribing to and recieving of custom <see cref="DispatchableEventBase"/> events.
/// </summary>
public partial class EventDispatcher
{
	private static JsonSerializerOptions serializerOptions;

	static EventDispatcher()
	{
		serializerOptions = new JsonSerializerOptions
		{
			Converters = {
				new ResourceJsonConverter(),
				new ClientJsonConverter(),
				new EntityJsonConverter()
			}
		};
	}

	///<summary>
	///Object to hold the events subscribed delegates.
	///</summary>
	private class EventHolder
	{
		public List<EventSubscription> persistentEvent = new ();
		public List<EventSubscription> tempEvent = new();

		public void Invoke( DispatchableEventBase args )
		{
			//Check if delegates are valid before invoking them.
			for ( int i = persistentEvent.Count - 1; i >= 0; i-- ) 
			{
				var e = persistentEvent[i];
				if ( e.IsValid() ) 
				{
					try
					{
						e.Invoke( args );
					}
					catch(Exception ex)
					{
						Log.Error( ex );
					}
				}
				else
				{
					persistentEvent.RemoveAt( i );
				}
			}

			for ( int i = tempEvent.Count - 1; i >= 0; i-- ) 
			{
				var e = tempEvent[i];
				if ( e.IsValid() )
				{
					try
					{
						e.Invoke( args );
					}
					catch(Exception ex)
					{
						Log.Error( ex );
					}
				}
			}
			tempEvent.Clear();
		}
	}

	private static Dictionary<string, EventHolder> eventSubscribers = new Dictionary<string, EventHolder>();
	private static Dictionary<string, Type> typeHints = new Dictionary<string, Type>();

	/// <summary>
	/// Subscribe to this Event Type
	/// </summary>
	/// <param name="callback">Callback recieving the events data as an argument</param>
	/// <param name="once">Should this fire only once?</param>
	/// <param name="sourceTarget">Optional source object to use instead of automatically retrieved one</param>
	/// <returns>Subscribed delegate. Used to unsubscribe</returns>
	public static void Subscribe<T>(Action<T> callback, object sourceTarget = null, bool once = false) where T : DispatchableEventBase
	{
		Type type = typeof( T );
		var eventHolder = GetEventDelegate( type.Name );

		//A lambda needs to wrap the provided callback so we can cast the argument and successfully add the subscription.
		EventDelegate eventDelegate = ( args ) => callback( (T)args );

		//Make event subscription struct to store the casted delegate call, the original and the target entity.
		EventSubscription subscription = new( eventDelegate, callback, sourceTarget );

		if ( once )
		{
			eventHolder.tempEvent.Add( subscription );
		}
		else
		{
			eventHolder.persistentEvent.Add( subscription );
		}

		//Store the type reference for easier deserialisation later
		if ( !typeHints.ContainsKey( type.Name ) )
		{
			typeHints.Add( type.Name, typeof( T ) );
		}
	}

	/// <summary>
	/// Unsubscribe from an event.
	/// </summary>
	/// <typeparam name="T">Original subscription type</typeparam>
	/// <param name="eventDelegate">Delegate that was subscribed to an event previously</param>
	public static void Unsubscribe<T>(Action<T> eventDelegate ) where T : DispatchableEventBase
	{
		var eventHolder = GetEventDelegate( typeof( T ).Name );
		for ( int i = eventHolder.persistentEvent.Count - 1; i >= 0; i-- )
		{
			if( eventHolder.persistentEvent[i].IsEqualOriginal(eventDelegate) )
			{
				eventHolder.persistentEvent.RemoveAt( i );
			}
		}
		for ( int i = eventHolder.tempEvent.Count - 1; i >= 0; i-- )
		{
			if ( eventHolder.tempEvent[i].IsEqualOriginal( eventDelegate ) )
			{
				eventHolder.tempEvent.RemoveAt( i );
			}
		}
	}

	/// <summary>
	/// Unsubscribe an Objects subscriptions from a single event.
	/// </summary>
	/// <typeparam name="T">Original subscription type</typeparam>
	/// <param name="ob">Object to remove subscriptions for</param>
	public static void Unsubscribe<T>( object ob ) where T : DispatchableEventBase
	{
		var eventHolder = GetEventDelegate( typeof( T ).Name );
		for ( int i = eventHolder.persistentEvent.Count - 1; i >= 0; i-- )
		{
			if ( eventHolder.persistentEvent[i].IsEqualTarget( ob ) )
			{
				eventHolder.persistentEvent.RemoveAt( i );
			}
		}
		for ( int i = eventHolder.tempEvent.Count - 1; i >= 0; i-- )
		{
			if ( eventHolder.tempEvent[i].IsEqualTarget( ob ) )
			{
				eventHolder.tempEvent.RemoveAt( i );
			}
		}
	}

	/// <summary>
	/// Unsubscribe an objects subscriptions from all events.
	/// </summary>
	/// <param name="ob">Object to remove all subscriptions for</param>
	public static void UnsubscribeAll( object ob )
	{
		foreach ( var eventHolder in eventSubscribers.Values ) 
		{
			for ( int i = eventHolder.persistentEvent.Count - 1; i >= 0; i-- )
			{
				if ( eventHolder.persistentEvent[i].IsEqualTarget( ob ) )
				{
					eventHolder.persistentEvent.RemoveAt( i );
				}
			}
			for ( int i = eventHolder.tempEvent.Count - 1; i >= 0; i-- )
			{
				if ( eventHolder.tempEvent[i].IsEqualTarget( ob ) )
				{
					eventHolder.tempEvent.RemoveAt( i );
				}
			}
		}
	}

	private static EventHolder GetEventDelegate(string name)
	{
		if ( !eventSubscribers.TryGetValue( name, out EventHolder action ) ) 
		{
			action = new EventHolder();
			eventSubscribers.Add(name, action);
		}
		return action;
	}

	private static void DispatchEvent( string name, DispatchableEventBase args )
	{
		var eventHolder = GetEventDelegate( name );
		eventHolder.Invoke( args );
	}

	/// <summary>
	/// Invoke this dispatchable event with the provided data. Uses event defined dispatch type.
	/// </summary>
	/// <param name="args">The event object to send</param>
	public static void InvokeEvent( DispatchableEventBase args )
	{
		Type type = args.GetType();
		var eventAttribute = TypeLibrary.GetAttribute<EventDispatcherEventAttribute>( type );
		InvokeEvent( args, eventAttribute.dispatchTypes );
	}

	public static void InvokeEvent<T>() where T : DispatchableEventBase, new()
	{
		InvokeEvent( new T() );
	}

	/// <summary>
	/// Invoke this dispatchable event with the provided data, using the provided dispatch types.
	/// </summary>
	/// <param name="args">The event object to send</param>
	/// <param name="dispatchTypes">Dispatch types for this invokation</param>
	public static void InvokeEvent( DispatchableEventBase args, DispatchType dispatchTypes )
	{
		Type type = args.GetType();
		string name = type.Name;
		// Log.Info( $"Sending Event: {name}..." );

		//Check if this event should be sent to clients and/or server.
		if ( (dispatchTypes & DispatchType.Client) == DispatchType.Client )
		{
			if( Game.IsClient )
			{
				RecieveEventLocal( name, args );
			}
			else
			{
				string data = JsonSerializer.Serialize( args, type, serializerOptions );
				RecieveEvent( name, data);
			}
		}

		if ( (dispatchTypes & DispatchType.Server) == DispatchType.Server )
		{
			if ( Game.IsServer )
			{
				RecieveEventLocal( name, args );
			}
			//Client to server not possible yet
			/*
			else
			{
				string data = JsonSerializer.Serialize( args, type );
				RecieveEvent( data, name );
			}
			*/
		}
	}

	private static void RecieveEventLocal( string typeName, DispatchableEventBase args )
	{
		// Log.Info( $"Recieve Event Local: {typeName}..." );
		DispatchEvent( typeName, args );
	}

	/// <summary>
	/// Recieves dispatched events from Server to Client
	/// </summary>
	[ClientRpc]
	public static void RecieveEvent(string typename, string data)
	{
		// Log.Info( $"Got Remote Event: {typename}" );

		//Get the type needed for deserialisation.
		Type typeHint;
		if ( !typeHints.TryGetValue( typename, out typeHint ) ) 
		{
			var type = TypeLibrary.GetType<DispatchableEventBase>( typename );
			typeHint = type.TargetType;
		}

		DispatchableEventBase arguments = (DispatchableEventBase)JsonSerializer.Deserialize(data, typeHint, serializerOptions );

		DispatchEvent( typename, arguments );
	}

}

/// <summary>
/// Base type for Event objects used with <see cref="EventDispatcher"/>
/// </summary>
public abstract class DispatchableEventBase
{

}
