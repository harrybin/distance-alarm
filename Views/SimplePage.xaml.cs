namespace DistanceAlarm.Views;

public partial class SimplePage : ContentPage
{
    public SimplePage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SimplePage constructor starting...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SimplePage constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SimplePage constructor failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}