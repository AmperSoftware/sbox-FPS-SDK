using Sandbox;

namespace Amper.FPS;

public partial class BaseViewModel : BaseNetworkable
{
	/// <summary>
	/// Index of the ViewModel.
	/// </summary>
	[Net] 
	public int Index { get; set; }
	/// <summary>
	/// Weapon currently represented by this viewmodel.
	/// </summary>
	[Net, Predicted, Change( "OnDataChanged" )] 
	public BaseCombatWeapon Weapon { get; private set; }
	[Net, Predicted] 
	public bool EnableDrawing { get; set; }
	public Entity Owner { get; set; }
	public ViewModelModel EffectEntity { get; private set; }

	public BaseViewModel()
	{
		Event.Register( this );

		if ( Game.IsClient )
		{
			var effect = new AnimatedEntity();
			effect.EnableViewmodelRendering = true;
		}
	}

	[Event.Client.PostCamera]
	public virtual void PostCameraUpdate()
	{
		if ( Owner == null )
			return;

		var effect = EffectEntity;
		if ( effect == null )
			return;

		effect.Position = Camera.Position;
		effect.Rotation = Camera.Rotation;
	}

	public virtual void SetWeapon( BaseCombatWeapon weapon )
	{
		Weapon = weapon;
		UpdateModelFromWeapon( weapon );
	}

	public virtual void OnDataChanged()
	{
		UpdateModelFromWeapon( Weapon );
	}

	public virtual void UpdateModelFromWeapon( BaseCombatWeapon weapon )
	{
		EffectEntity?.SetModel( weapon.GetViewModel( Index ) );
	}

	public virtual void SetupAttachments() { }

	public T CreateAttachment<T>( string model = "" ) where T : ModelEntity, new()
	{
		var attach = new T { Owner = Owner, EnableViewmodelRendering = true };
		attach.SetParent( EffectEntity, true );
		attach.SetModel( model );
		return attach;
	}

	public ModelEntity CreateAttachment( string model = "" )
	{
		return CreateAttachment<ModelEntity>( model );
	}

	public class ViewModelModel : AnimatedEntity
	{
	}

	~BaseViewModel()
	{
		Event.Unregister( this );
	}
}
