using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace Amper.FPS;

[UseTemplate]
public class ColorFormattedString : Panel
{
	public ColorFormattedString Clear()
	{
		DeleteChildren( true );
		return this;
	}

	public ColorFormattedString AddText( string text )
	{
		Add.Label( text );
		return this;
	}

	public ColorFormattedString AddColoredText( string text, string color )
	{
		var label = Add.Label( text );
		label.Style.Set( "color", color );
		return this;
	}

	public ColorFormattedString AddTextWithClasses( string text, string classes )
	{
		var label = Add.Label( text );
		label.Classes = classes;
		return this;
	}
}
