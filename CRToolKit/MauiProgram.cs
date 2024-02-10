using CRToolKit.Interfaces;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

namespace CRToolKit;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
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
		/*
        Task.Run(async () =>
        {
            await Task.Delay(4000);
        }).Wait();
		*/
		return app;
    }
    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<ICommonDeviceHelper, CommonDeviceHelper>();
        return mauiAppBuilder;
    }
}

