using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using ResourcingToolKit.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using System.Reflection;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace ResourcingToolKit;

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


        var asmb = Assembly.GetExecutingAssembly();
        using var stream = asmb.GetManifestResourceStream("ResourcingToolKit.appsettings.json");

        var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
        builder.Configuration.AddConfiguration(config);

        builder.Services.AddTransient<MainPage>();

        builder = RegisterAppServices(builder);
        var app =  builder.Build();
		
		return app;
    }
    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
        mauiAppBuilder.Services.AddSingleton<ICommonDeviceHelper, CommonDeviceHelper>();
        return mauiAppBuilder;
    }
}

