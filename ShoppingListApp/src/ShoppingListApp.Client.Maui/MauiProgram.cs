using Microsoft.Extensions.Logging;
using ShoppingListApp.Client.Core.Data;
using ShoppingListApp.Client.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.IO; // Required for Path
using System;    // Required for Environment

namespace ShoppingListApp.Client.Maui
{
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
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif
            // Configure HttpClient for SyncService
            // The server address will depend on your deployment and whether you're running on an emulator or device.
            // For Android emulator, 10.0.2.2 often maps to the host machine's localhost.
            // For Windows, localhost or the machine's IP might be used.
            // This needs to be configured carefully for actual testing.
            builder.Services.AddScoped(sp => {
                var mauiDevice = DeviceInfo.Current;
                string baseAddress;

                if (mauiDevice.Platform == DevicePlatform.Android)
                {
                    baseAddress = "http://10.0.2.2:5000"; // Port might need to match your server's HTTP port if not using HTTPS during dev
                     // If server is on HTTPS (recommended), use https://10.0.2.2:5001 (or actual HTTPS port)
                     // And configure HttpClientHandler for bypassing SSL certificate validation in DEV if using self-signed certs.
                }
                else if (mauiDevice.Platform == DevicePlatform.WinUI)
                {
                     baseAddress = "http://localhost:5000"; // Or https://localhost:5001
                }
                else // iOS, MacCatalyst etc. May need different configurations or IP of host machine.
                {
                    baseAddress = "http://localhost:5000"; // Placeholder, adjust as needed
                }

                // It's good practice to ensure your server is configured to listen on these addresses/ports,
                // and that firewall rules allow connections.

                var logger = sp.GetRequiredService<ILogger<HttpClient>>();
                logger.LogInformation("HttpClient BaseAddress for SyncService: {BaseAddress}", baseAddress);

                return new HttpClient { BaseAddress = new Uri(baseAddress) };
            });


            // Setup SQLite database path
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "shopping_local.db");
            builder.Services.AddDbContext<LocalDbContext>(options =>
                options.UseSqlite($"Filename={dbPath}"));

            // Register services
            builder.Services.AddScoped<ShoppingRepository>();
            builder.Services.AddScoped<SyncService>();

            // Add a simple way to get the ILogger factory for non-DI created classes if needed elsewhere, or inject ILogger<T> directly
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddDebug(); // Example: Log to debug output
                // Add other providers as needed
            });


            return builder.Build();
        }
    }
}
