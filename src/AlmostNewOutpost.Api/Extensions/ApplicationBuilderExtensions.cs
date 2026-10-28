using AlmostNewOutpost.Api.Options;
using AlmostNewOutpost.Api.Repositories;
using Microsoft.Extensions.Options;

namespace AlmostNewOutpost.Api.Extensions;

/// <summary>
/// The HTTP pipeline (the order requests pass through) and the startup database check.
/// </summary>
public static class ApplicationBuilderExtensions
{
    public static WebApplication UseOutpostPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // In Development the developer exception page shows full error details.
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // Everywhere else an unhandled error becomes a plain 500 ProblemDetails, no stack trace.
            app.UseExceptionHandler();
        }

        // Turns empty error responses (like a 401 from the JWT check) into ProblemDetails.
        app.UseStatusCodePages();

        app.UseHttpsRedirection();
        app.UseCors(CorsOptions.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Development only: try the database once at startup and log one clear message if it fails.
    /// The API keeps running so /api/health can report "unreachable".
    /// </summary>
    public static async Task CheckDatabaseConnectionAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseCheck");

        try
        {
            ICategoryRepository categories = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            await categories.GetAllAsync();
            logger.LogInformation("Connected to the database.");
        }
        catch (Exception exception) when (exception is not OptionsValidationException)
        {
            // A missing setting (OptionsValidationException) should stop the app with its own message.
            logger.LogError(
                "Cannot reach almost_new_outpost. Is MySQL84 running and has database/almost_new_outpost.sql been run? ({Reason})",
                exception.Message);
        }
    }
}
