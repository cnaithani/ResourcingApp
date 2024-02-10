namespace CRToolKit;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		
        /*
        Task.Run(async () =>
        {
            await Task.Delay(2000);
        }).Wait();
		*/
		
    }

	

    void Theme_Toggled(System.Object sender, Microsoft.Maui.Controls.ToggledEventArgs e)
    {
		if (Application.Current.UserAppTheme == AppTheme.Dark)
		{
			Application.Current.UserAppTheme = AppTheme.Light;
            Preferences.Set("APPTHEME", "Light");

        }
		else
		{
            Application.Current.UserAppTheme = AppTheme.Dark;
            Preferences.Set("APPTHEME", "Dark");
        }
    }
}


