using Sandbox;
using Sandbox.UI;

namespace Amper.FPS;

public class InputGlyph : Image
{
	public string Button { get; set; }
	public InputGlyphSize Size { get; set; }

	static Texture UnboundTexture = Texture.Load( FileSystem.Mounted, "/ui/unbound.png" );

	public Texture GetGlyphTexture()
	{ 
		if ( string.IsNullOrEmpty( Input.GetButtonOrigin( Button ) ) )
			return UnboundTexture;

		// this key is unbound.
		var style = GlyphStyle.Knockout;

		// texture doesnt exist, or can't be generated
		var texture = Input.GetGlyph( Button, Size, style );
		if ( texture == null )
			return UnboundTexture;

		return texture;
	}

	public override void Tick()
	{
		base.Tick();

		SetClass( "large", Size == InputGlyphSize.Large );
		SetClass( "medium", Size == InputGlyphSize.Medium );
		SetClass( "small", Size == InputGlyphSize.Small );

		var texture = GetGlyphTexture();

		Texture = texture;
		if ( texture == null )
			return;

		float width = texture.Width;
		float height = texture.Height;
		var aspectRatio = width / height;

		Style.AspectRatio = aspectRatio;

		if ( Input.Pressed( Button ) )
			SetClass( "pressed", true );

		if ( Input.Released( Button ) )
			SetClass( "pressed", false );
	}
}
