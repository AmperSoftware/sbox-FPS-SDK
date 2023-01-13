using Sandbox;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amper.FPS;

public class EntityJsonConverter : JsonConverter<Entity>
{
	public override Entity Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) => Entity.FindByIndex( reader.GetInt32() );
	public override void Write( Utf8JsonWriter writer, Entity entity, JsonSerializerOptions options ) => writer.WriteNumberValue( entity.NetworkIdent );

	public override bool CanConvert( Type typeToConvert )
	{
		// Allow conversion of Entity and subclasses of Entity.
		return typeToConvert.IsSubclassOf( typeof( Entity ) ) || base.CanConvert( typeToConvert );
	}
}

public class ClientJsonConverter : JsonConverter<IClient>
{
	public override IClient Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		var netId = reader.GetInt32();
		return Game.Clients.FirstOrDefault( x => x.NetworkIdent == netId );
	}

	public override void Write( Utf8JsonWriter writer, IClient client, JsonSerializerOptions options ) => writer.WriteNumberValue( client.NetworkIdent );
}

public class ResourceJsonConverter : JsonConverter<GameResource>
{
	public override GameResource Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		return ResourceLibrary.Get<GameResource>( reader.GetInt32() );
	}

	public override void Write( Utf8JsonWriter writer, GameResource asset, JsonSerializerOptions options )
	{
		writer.WriteNumberValue( asset.ResourceId );
	}

	public override bool CanConvert( Type typeToConvert )
	{
		// Allow conversion of Entity and GameResource of Entity.
		return typeToConvert.IsSubclassOf( typeof( GameResource ) ) || base.CanConvert( typeToConvert );
	}
}
