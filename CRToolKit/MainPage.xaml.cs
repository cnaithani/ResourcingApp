
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Maui.Storage;

namespace CRToolKit;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		
		
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

    private async void btnBrowse_Clicked(object sender, EventArgs e)
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
        {
            lblPath.Text = result.Folder.Path;
        }
    }
}


