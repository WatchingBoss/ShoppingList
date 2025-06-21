using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components; // Required for NavigationManager
using ShoppingListApp.Client.Web;
using ShoppingListApp.Client.Core.Data; // Core data services
using ShoppingListApp.Client.Core.Services; // Core business logic services
using Microsoft.EntityFrameworkCore; // For DbContext options
using System; // For Uri
using Microsoft.Extensions.Logging; // For ILogger

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Set up root components
// Assuming App.razor in Client.Web is the main app component that might host ItemListView
// If ItemListView is the absolute root, it would be specified here.
// Default template usually has an App.razor that then uses Router.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for SyncService
builder.Services.AddScoped(sp => {
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var httpClient = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
    // This base address assumes the API is hosted at the same origin as the WASM app.
    // For development with separate API server (e.g. http://localhost:5000), change BaseAddress here
    // and ensure server CORS policy allows the WASM app's origin (e.g. http://localhost:5XXX).
    // var baseAddressForApi = builder.HostEnvironment.IsDevelopment() ? "http://localhost:5258" : navigationManager.BaseUri; // Example for different dev API URL for server
    // var httpClient = new HttpClient { BaseAddress = new Uri(baseAddressForApi) };


    // It's good to log the actual base address being used.
    var logger = sp.GetRequiredService<ILogger<HttpClient>>(); // Get ILogger for HttpClient
    logger.LogInformation("HttpClient BaseAddress for SyncService (Web): {BaseAddress}", httpClient.BaseAddress);

    return httpClient;
});

// Setup SQLite for Blazor WASM
// Using AddDbContextFactory for Blazor applications is recommended.
// "DataSource=shopping_local_wasm_shared.db;Mode=Memory;Cache=Shared" creates a shared in-memory DB that persists as long as the app instance is alive.
// It's not persistent across browser refreshes or sessions without IndexedDB integration.
// For this project, this provides a functional local store for the app's lifetime.
builder.Services.AddDbContextFactory<LocalDbContext>(options =>
    options.UseSqlite("Data Source=shopping_local_wasm_shared.db;Mode=Memory;Cache=Shared")
           .LogTo(Console.WriteLine, LogLevel.Information)); // Log EF Core operations to console

// Register services
builder.Services.AddScoped<ShoppingRepository>();
builder.Services.AddScoped<SyncService>();

// Add basic logging
// The default WebAssemblyHostBuilder.CreateDefault(args) already sets up basic logging.
// We can customize it further if needed. For example, to set a minimum level.
builder.Logging.SetMinimumLevel(LogLevel.Information);
// Already added by default: builder.Logging.AddBrowserConsole();


await builder.Build().RunAsync();
