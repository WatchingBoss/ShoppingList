using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure DbContext for MariaDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=your_mariadb_server;Port=3306;Database=shoppinglistdb;Uid=your_user;Pwd=your_password;"; // User should replace this
builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers(); // Added for API controllers

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Ensure database is created on startup during development
    // This is a simple way to ensure DB exists and schema is applied for development.
    // For production, proper migration strategies should be used.
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            dbContext.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        // Log the error or handle it as needed
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring the database is created.");
        // Depending on the severity, you might want to stop the application
        // or provide a fallback mechanism.
    }
}

app.UseHttpsRedirection();

app.UseRouting(); // Ensure UseRouting is called before UseAuthorization and UseEndpoints (or MapControllers).

app.UseAuthorization(); // Added for potential future auth needs

app.MapControllers(); // Added to map API controllers

app.Run();
