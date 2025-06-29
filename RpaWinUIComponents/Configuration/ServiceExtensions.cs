using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using RpaWinUIComponents.AdvancedDataGrid.ViewModels;
using System;

namespace RpaWinUIComponents.Configuration;

/// <summary>
/// Extension methods for configuring RpaWinUIComponents services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds AdvancedDataGrid services to the service collection
    /// </summary>
    public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IClipboardService, ClipboardService>();
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<INavigationService, NavigationService>();

        // Register ViewModels
        services.AddTransient<AdvancedDataGridViewModel>();

        return services;
    }

    /// <summary>
    /// Adds AdvancedDataGrid services with custom logger factory
    /// </summary>
    public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services, ILoggerFactory loggerFactory)
    {
        services.AddSingleton(loggerFactory);
        return services.AddAdvancedDataGrid();
    }

    /// <summary>
    /// Configures RpaWinUIComponents with default settings for WinUI 3 applications
    /// </summary>
    public static IServiceCollection AddRpaWinUIComponents(this IServiceCollection services)
    {
        // Add logging if not already configured
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add AdvancedDataGrid
        services.AddAdvancedDataGrid();

        // Add SmartListBox (when implemented)
        // services.AddSmart
        return services;
    }

    /// <summary>
    /// Configures RpaWinUIComponents for testing with minimal logging
    /// </summary>
    public static IServiceCollection AddRpaWinUIComponentsForTesting(this IServiceCollection services)
    {
        // Add minimal logging for testing
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Critical);
        });

        // Add services without extensive logging
        services.AddAdvancedDataGrid();

        return services;
    }

    /// <summary>
    /// Creates a HostBuilder with RpaWinUIComponents configured
    /// </summary>
    public static IHostBuilder CreateRpaWinUIHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddRpaWinUIComponents();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            });
    }

    /// <summary>
    /// Extension method for configuring dependency injection in App.xaml.cs
    /// </summary>
    public static void ConfigureRpaWinUIComponents(this IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
        }

        services.AddRpaWinUIComponents();
    }
}