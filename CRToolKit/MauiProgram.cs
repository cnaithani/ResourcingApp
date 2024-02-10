using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using CRToolKit.Interfaces;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace CRToolKit;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        builder.ConfigureSyncfusionCore();
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjU2OTA4OEAzMjMyMmUzMDJlMzBIcVQrbGkvMkpjWUQ0L0srdjB6QU1nTnBEblllWU5zRjBEMk5UQ2dUMlhjPQ==");


        builder = RegisterAppServices(builder);
        var app =  builder.Build();
		
        //Task.Run(async () =>
        //{
        //    await Task.Delay(1000);
        //}).Wait();
		
		return app;
    }
    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
        mauiAppBuilder.Services.AddSingleton<ICommonDeviceHelper, CommonDeviceHelper>();
        return mauiAppBuilder;
    }
}

