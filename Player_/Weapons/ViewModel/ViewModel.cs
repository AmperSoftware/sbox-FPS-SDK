using Sandbox;
using System.Linq;

namespace Amper.FPS;

public partial class SDKViewModel : BaseViewModel
{
	public SDKWeapon Weapon { get; set; }
	public SDKPlayer Player => Weapon?.Player;

	public override void Spawn()
	{
		base.Spawn();
		EnableViewmodelRendering = true;
	}

	[Event.Client.PostCamera]
	public void PostCameraSetup( )
	{
		var visible = ShouldDraw();
		EnableDrawing = visible;

		if ( visible )
			CalculateView();
	}
	

	public virtual bool ShouldDraw() => true;
	public virtual void CalculateView() { }

	public virtual void SetWeaponModel( string viewmodel, SDKWeapon weapon )
	{
		ClearWeapon( weapon );

		SetModel( viewmodel );
		Weapon = weapon;
	}

	public override void OnNewModel( Model model )
	{
		ClearAttachments();

		if ( Model != null )
			SetupAttachments();
	}

	public virtual void ClearWeapon( SDKWeapon weapon )
	{
		if ( Weapon != weapon )
			return;

		ClearAttachments();


		Model = null;
		Weapon = null;
	}

	public virtual void SetupAttachments() { }

	public ModelEntity CreateAttachment<T>( string model = "" ) where T : ModelEntity, new()
	{
		var attach = new T { Owner = Owner, EnableViewmodelRendering = EnableViewmodelRendering };
		attach.SetParent( this, true );
		attach.SetModel( model );
		return attach;
	}

	public ModelEntity CreateAttachment( string model = "" )
	{
		return CreateAttachment<ModelEntity>( model );
	}

	public virtual void ClearAttachments()
	{
		foreach ( var attach in Children.Where( x => x.IsAuthority ).ToArray() ) 
			attach.Delete();
	}

	protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
	{
		base.OnAnimGraphTag( tag, fireMode );
		Weapon?.OnViewModelAnimGraphTag( tag, fireMode );
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
		Weapon?.OnViewModelAnimEventGeneric( name, intData, floatData, vectorData, stringData );
	}
}
