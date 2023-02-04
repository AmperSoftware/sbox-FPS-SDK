using Sandbox;
using Sandbox.UI;

namespace Amper.FPS;

public partial class FreezeCameraPanel : Panel
{
	public static FreezeCameraPanel Instance { get; set; }
	public float FreezeTime { get; set; }
	public TimeSince TimeSinceFrozen { get; set; }
	public bool IsFrozen => TimeSinceFrozen < FreezeTime;

	public bool WillFreeze { get; set; }

	Texture ColorTexture { get; set; }
	Texture DepthTexture { get; set; }

	Vector3 Position { get; set; }
	Rotation Rotation { get; set; }
	float FieldOfView { get; set; }

	Vector2 Size { get; set; }

	public FreezeCameraPanel()
	{
		Instance = this;
	}

	public override void Tick()
	{
		SetClass( "visible", ShouldDraw() );
	}

	public static void Freeze( float time, Vector3 position, Rotation rotation, float fov )
	{
		Instance?.SetupFreeze( time, position, rotation, fov );
	}

	public void SetupFreeze( float time, Vector3 position, Rotation rotation, float fov )
	{
		Size = new Vector2( Screen.Width, Screen.Height );

		ColorTexture?.Dispose();
		DepthTexture?.Dispose();

		ColorTexture = Texture.CreateRenderTarget()
			.WithSize( Size )
			.WithFormat( ImageFormat.RGBA32323232F )
			.WithScreenMultiSample()
			.Create();

		DepthTexture = Texture.CreateRenderTarget()
			.WithSize( Size )
			.WithDepthFormat()
			.WithScreenMultiSample()
			.Create();


		TimeSinceFrozen = 0;
		FreezeTime = time;

		Position = position;
		Rotation = rotation;
		FieldOfView = fov; 
		
		Style.SetBackgroundImage( ColorTexture );
		WillFreeze = true;
	}

	public bool ShouldDraw()
	{
		return IsFrozen;
	}

	public override void DrawBackground( ref RenderState state )
	{
		base.DrawBackground( ref state );

		if ( WillFreeze )
		{
			// Fill the texture with background 
			// Render.Draw.DrawScene( ColorTexture, DepthTexture, Map.Scene, Render.Attributes, new Rect( 0, 0, Size.x, Size.y), Position, Rotation, FieldOfView, 0.1f, 9999, false);
			WillFreeze = false;
		}
	}
}
