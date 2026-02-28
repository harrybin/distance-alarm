using DistanceAlarm.ViewModels;

namespace DistanceAlarm.Views;

public partial class WearOsMainPage : ContentPage
{
    public WearOsMainPage(WearOsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
