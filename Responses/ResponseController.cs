using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox;

namespace Amper.FPS;

public interface IResponseSpeaker<Concepts, Contexts> where Concepts : Enum where Contexts : Enum
{
	public ResponseController<Concepts, Contexts> ResponseController { get; set; }
	public void SpeakConceptIfAllowed( Concepts concept );
	public void ModifyResponseCriteria( ResponseCriteria<Contexts> criteriaSet );
	public void PlayResponse( ResponseController<Concepts, Contexts>.Response response );
}

public class ResponseController<Concepts, Contexts> where Concepts : Enum where Contexts : Enum
{
	IResponseSpeaker<Concepts, Contexts> Me { get; set; }

	public ResponseController( IResponseSpeaker<Concepts, Contexts> speaker )
	{
		Me = speaker;
	}

	List<ResponseData<Concepts, Contexts>> LoadedData { get; set; } = new();
	Dictionary<string, Criterion> CriterionDictionary { get; set; } = new();
	Dictionary<Concepts, List<Response>> ResponseDictionary { get; set; } = new();

	public void Load( ResponseData<Concepts, Contexts> data )
	{
		if ( !data.IsValid() ) 
			return;

		Reset();
		ParseLoadData( data );
	}

	public void Reset()
	{
		LoadedData.Clear();
		CriterionDictionary.Clear();
		ResponseDictionary.Clear();
	}

	void ParseLoadData( ResponseData<Concepts, Contexts> data )
	{
		if ( LoadedData.Contains( data ) )
		{
			// Log.Info( $"<Responses> Warning: Attempt to load \"{data.ResourcePath}\" will cause recursion. Haulting..." );
			return;
		}

		LoadedData.Add( data );

		// Try loading prefab first.
		if ( !string.IsNullOrEmpty( data.Base ) )
		{
			if ( ResourceLibrary.TryGet<ResponseData<Concepts, Contexts>>( data.Base, out var responseData ) ) 
				ParseLoadData( responseData );
		}

		ParseCriteriaFromData( data );
		ParseResponsesFromData( data );
		OrderResponseList();
	}

	private readonly Dictionary<string, CompareSign> SignStrings = new()
	{
		{"=",   CompareSign.Equal },
		{"!=",  CompareSign.NotEqual },
		{"<",   CompareSign.Less },
		{">",   CompareSign.Greater },
		{"<=",	CompareSign.LessOrEqual },
		{">=",	CompareSign.GreaterOrEqual }
	};

	void ParseCriteriaFromData( ResponseData<Concepts, Contexts> data )
	{
		var loadedCount = 0;

		if ( data.Criteria == null )
			return;

		foreach ( var pair in data.Criteria )
		{
			var name = pair.Key;
			if ( CriterionDictionary.ContainsKey( name ) )
			{
				// Log.Info( $"<Responses> Criterion \"{name}\" already exists. Skipping..." );
				continue;
			}

			var criterionData = pair.Value;
			var context = criterionData.Context;
			var rawValue = criterionData.Value;

			// Trim raw value so signs are calculated properly.
			rawValue = rawValue.Trim();

			// Figure out what type of comparison we're providing in the asset.
			var sign = CompareSign.Equal;
			var substrCount = 0;

			foreach ( var signPair in SignStrings )
			{
				if ( rawValue.StartsWith( signPair.Key ) )
				{
					sign = signPair.Value;
					substrCount = signPair.Key.Length;
					break;
				}
			}


			if ( substrCount > 0 )
			{
				// Substring the sign from the value field.
				rawValue = rawValue.Substring( substrCount );
				// Trim again to remove potential white spaced between sign and value.
				rawValue = rawValue.Trim();
			}

			// Create a new criterion entry.
			var parsedData = new Criterion
			{
				Name = name,
				Context = context,
				Sign = sign,
				Value = rawValue
			};

			// Add to dictionary.
			// Log.Info( $"<Responses> Parsed Criterion \"{name}\" (\"{context}\" {sign} {rawValue})" );
			CriterionDictionary.Add( name, parsedData );
			loadedCount++;
		}

		// Log.Info( $"<Responses> Finished loading criterions for \"{data.ResourceName}\" (Loaded: {loadedCount})" );
	}

	void ParseResponsesFromData( ResponseData<Concepts, Contexts> data )
	{
		var loadedCount = 0;

		foreach ( var response in data.Responses )
		{

			var concept = response.Concept;
			var criteria = response.Criteria;
			var soundEvent = response.SoundEvent;

			var parsedData = new Response
			{
				Concept = concept,
				Criteria = criteria,
				SoundEvent = soundEvent
			};

			// Add to dictionary.
			// Log.Info( $"<Responses> Parsed Response for concept \"{concept}\" ({criteria?.Count ?? 0} criterions)" );
			loadedCount++;

			if ( !ResponseDictionary.ContainsKey( concept ) )
				ResponseDictionary[concept] = new();

			ResponseDictionary[concept].Add( parsedData );
		}

		// Log.Info( $"<Responses> Finished loading responses for \"{data.ResourceName}\" (Loaded: {loadedCount})" );
	}

	void OrderResponseList()
	{
		var i = 0;
		foreach ( var pair in ResponseDictionary )
		{
			var key = pair.Key;
			var list = pair.Value;

			list = list.OrderByDescending( x => x.CriteriaCount ).ToList();
			ResponseDictionary[key] = list;
			foreach ( var response in ResponseDictionary[key] )
			{
				i++;
				// Log.Info( $"{i}: {response.Concept} ({response.CriteriaCount})" );
			}
		}
	}

	public struct Criterion
	{
		public string Name;
		public Contexts Context;
		public CompareSign Sign;
		public string Value;
	}

	public struct Response
	{
		public Concepts Concept;
		public List<string> Criteria { get; set; }
		public string SoundEvent { get; set; }
		public int CriteriaCount => Criteria?.Count ?? 0;
	}

	public enum CompareSign
	{
		Equal,
		NotEqual,
		Less,
		Greater,
		LessOrEqual,
		GreaterOrEqual
	}

	public void Speak( Concepts concept )
	{
		if ( Me == null )
			return;

		var criteriaSet = new ResponseCriteria<Contexts>();
		Me.ModifyResponseCriteria( criteriaSet );

		if ( !TryFindMatchingResponse( concept, criteriaSet, out var response ) )
			return;

		Me.PlayResponse( response );
	}

	bool TryFindMatchingResponse( Concepts concept, ResponseCriteria<Contexts> criteriaSet, out Response response )
	{
		response = default;

		if ( !ResponseDictionary.TryGetValue( concept, out var responseList ) )
			return false;

		foreach ( var entry in responseList )
		{
			if ( !CriterionsMatchCriteria( entry.Criteria, criteriaSet ) ) 
				continue;

			response = entry;
			return true;
		}

		return false;
	}

	bool CriterionsMatchCriteria( List<string> criterions, ResponseCriteria<Contexts> criteria )
	{
		// No criterions for this.
		if ( criterions == null )
			return true;

		foreach ( var criterionName in criterions )
		{
			// Provided criterion not found in the dictionary, skip it.
			if ( !CriterionDictionary.TryGetValue( criterionName, out var criterion ) )
			{
				// Log.Info( $"Criterion \"{criterionName}\" was not found in the dictionary." );
				continue;
			}

			var criterionContext = criterion.Context;
			var compareSign = criterion.Sign;
			var compareValue = criterion.Value;

			if ( !CompareContextValue( criteria, criterionContext, compareSign, compareValue ) )
			{
				// Log.Info( $"{criterionName} doesn't match" );
				return false;
			}
		}

		return true;
	}

	bool CompareContextValue( ResponseCriteria<Contexts> criteria, Contexts context, CompareSign sign, string compareValue )
	{
		if ( !criteria.Table.TryGetValue( context, out var contextValue ) ) 
			return false;

		//
		// The following compares can be done without casting to number.
		//
		var compareInt = contextValue.CompareTo( compareValue );
		// Log.Info( $"{context}: {compareValue} / {contextValue} - {compareInt}" );

		switch(sign)
		{
			case CompareSign.Equal: return compareInt == 0;
			case CompareSign.NotEqual: return compareInt != 0;
			case CompareSign.Less: return compareInt < 0;
			case CompareSign.Greater: return compareInt > 0;
			case CompareSign.LessOrEqual: return compareInt <= 0;
			case CompareSign.GreaterOrEqual: return compareInt >= 0;
		}

		return false;
	}
}

public class ResponseCriteria<Contexts> where Contexts : Enum
{
	public Dictionary<Contexts, string> Table { get; set; } = new();

	public void Set( Contexts context, string value ) => Table[context] = value;
	public void Set( Contexts context, bool value ) => Set( context, value ? "1" : "0" );
}
