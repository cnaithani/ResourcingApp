using CRToolKit.Data;
using CRToolKit.Interfaces;
using System.Threading;

namespace CRToolKit;

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
    protected  override void OnStart()
    {
        base.OnStart();
        
    }

}

