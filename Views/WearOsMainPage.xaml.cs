using DistanceAlarm.ViewModels;

namespace DistanceAlarm.Views;

public partial class WearOsMainPage : ContentPage
{
    private readonly WearOsViewModel _viewModel;

    public WearOsMainPage(WearOsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsMainPage OnAppearing error: {ex.Message}");
        }
    }
}
