using DistanceAlarm.ViewModels;

namespace DistanceAlarm.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is SettingsViewModel viewModel)
        {
            await viewModel.LoadSettingsCommand.ExecuteAsync(null);
        }
    }
}