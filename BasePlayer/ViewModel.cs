using System.Collections.Generic;

namespace Amper.FPS;

partial class BasePlayer
{
	public IDictionary<int, BaseViewModel> ViewModels { get; set; }

	public BaseViewModel GetViewModel( int index = 0 )
	{
		if ( ViewModels.TryGetValue( index, out var vm ) )
			return vm;

		return null;
	}

	public virtual void CreateViewModels()
	{
		// Create a base view model for our hands
		CreateViewModel<BaseViewModel>( 0 );
	}

	public virtual void CreateViewModel<T>( int index ) where T : BaseViewModel, new()
	{
		if ( GetViewModel( index ) != null )
			return;

		var vm = new T();
		vm.Owner = this;
		vm.Index = index;
		ViewModels.Add( index, vm );
	}
}
