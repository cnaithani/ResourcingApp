using CommunityToolkit.Maui.Storage;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml;
using ResourcingToolKit.ViewModel;
using ResourcingToolKit.Models;


namespace ResourcingToolKit.Views;

public partial class AppSettings : ContentPage
{
    public AppSettingsVM currentVM;
    public AppSettings()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        currentVM = new AppSettingsVM();
        BindingContext = currentVM;

        var setting = await App.Database.database.Table<Models.AppSettings>().FirstOrDefaultAsync();
        if (setting != null)
        {
            txtTemplate.Text = setting.TemplateFile;
            txtProcessing.Text = setting.ProcessingFolder;
        }
    }

    async void Ok_Clicked(System.Object sender, System.EventArgs e)
    {
        var setting = await App.Database.database.Table<Models.AppSettings>().FirstOrDefaultAsync();
        var exists = true;
        if (setting == null) { 
            setting = new Models.AppSettings();
            exists = false;
        }
        setting.TemplateFile = txtTemplate.Text;
        setting.ProcessingFolder = txtProcessing.Text;
        try
        {
            if (!exists)
                await App.Database.database.InsertAsync(setting);
            else
                await App.Database.database.UpdateAsync(setting);
        }
        catch (Exception ex)
        {
            var str = ex.ToString();
        }


        (App.Current.MainPage as Shell).GoToAsync("//Main/Home", true);
    }

    private async void btnBrowse_Clicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(); 
            //FolderPicker.Default.PickAsync(CancellationToken.None);
        if (!String.IsNullOrEmpty(result.FileName))
        {
            if (!result.FileName.ToLower().EndsWith("docx"))
            {
                await DisplayAlert("Error", "Incorrect file type!", "OK");
            }
            else
                txtTemplate.Text = result.FullPath;
        }
    }

    private async void btnBrowseFolder_Clicked(object sender, EventArgs e)
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
        {
            txtProcessing.Text = result.Folder.Path;
        }
    }
}
