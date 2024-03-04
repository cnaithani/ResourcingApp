using ResourcingToolKit.Data;
using ResourcingToolKit.Interfaces;
using System.Threading;

namespace ResourcingToolKit;

public partial class App : Application
{
    static AppDatabase database;
    static bool isDatabaseInitialized = false;
    public App()
	{
        try
        {
            InitializeComponent();
        }
        catch(Exception e)
        {
            var str = e.Message;
        }
		
        if(Preferences.Get("APPTHEME","Light")== "Dark")
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Light;
        }
        


        Task.Run(async () =>
        {
            if (database == null)
            {
               await  InitiateDB();
            }
            await database.UpdateDatabase();
            isDatabaseInitialized = true;
        });

        MainPage = new AppShell();

    } 

    public static  AppDatabase Database
    {
        get
        {
            if (database == null)
            {
                InitiateDB().ConfigureAwait(false);
            }
            return database;
        }
    }

    private static async Task InitiateDB()
    {
        if (database == null)
        {
            while (App.Current.Handler == null)
            {
                await System.Threading.Tasks.Task.Delay(500);
            }
            var commonDeviceHandler = App.Current.Handler.MauiContext.Services.GetServices<ICommonDeviceHelper>().FirstOrDefault();
            database = new AppDatabase(await commonDeviceHandler.GetDBFile());
        }
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            window.MinimumWidth = 800;
            window.MaximumWidth = 800;
            window.MinimumHeight = 600;
            window.MaximumHeight = 600;

            // Get display size
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

            // Center the window
            window.X = (displayInfo.Width - window.MinimumWidth) / 2;
            window.Y = (displayInfo.Height - window.MaximumHeight) / 2;
        }

        return window;
    }

}

