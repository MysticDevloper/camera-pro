using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Camera Pro";
}