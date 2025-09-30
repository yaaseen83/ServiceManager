using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using ServiceManagerSettings;
using Timer = System.Timers.Timer;

public class ServiceManagerApp
{
    private static string? serviceName;
    private static Timer? serviceTimer;
    private static Timer? shutdownTimer;
    private static Settings? appSettings;

    public static async Task Main(string[] args)
    {
        // --- SETUP PHASE ---
        // Configure logging and settings first, so all actions can be recorded.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        appSettings = new Settings();
        configuration.GetSection("Settings").Bind(appSettings);

        Directory.CreateDirectory(appSettings.LogFolderPath);
        string logFilePath = Path.Combine(appSettings.LogFolderPath, "service-monitor-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("==================================================");
            Log.Information("Application Starting...");

            // --- REQUIREMENT #2: STOP AND REPLACE LOGIC ---
            // Find and terminate any other running instances of this application.
            TerminateOtherInstances();

            if (args.Length == 0)
            {
                Log.Warning("No command-line arguments provided. Exiting.");
                // Display usage info
                return;
            }

            serviceName = args[0];
            Log.Information("Service to monitor: {ServiceName}", serviceName);

            if (!IsAdministrator())
            {
                Log.Error("This application requires administrative privileges. Exiting.");
                return;
            }

            if (args.Length > 1 && args[1].ToLower() == "-timed")
            {
                // --- REQUIREMENT #1: CONFIGURABLE TIMINGS ---
                Log.Information("Timed mode activated. Service check interval: {Interval} minutes.", appSettings.ServiceCheckIntervalMinutes);

                SetupAutomaticShutdown();

                serviceTimer = new Timer(appSettings.ServiceCheckIntervalMinutes * 60 * 1000);
                serviceTimer.Elapsed += OnTimedEvent;
                serviceTimer.AutoReset = true;
                serviceTimer.Enabled = true;

                await ManageService(); // Perform initial check

                Log.Information("Timed monitor is running. This instance will automatically close after {Hours} hours.", appSettings.AppRunDurationHours);
                await Task.Delay(Timeout.Infinite);
            }
            else
            {
                Log.Information("Standard mode activated: Check and start service if stopped.");
                await CheckAndStartService();
                Log.Information("Standard mode task complete.");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred. Application will shut down.");
        }
        finally
        {
            Log.Information("Application Exiting...");
            Log.Information("==================================================\n");

            serviceTimer?.Dispose();
            shutdownTimer?.Dispose();

            await Log.CloseAndFlushAsync();
        }
    }

    private static void TerminateOtherInstances()
    {
        Process currentProcess = Process.GetCurrentProcess();
        // Get all processes with the same name as the current one, but in a different session or with a different PID.
        Process[] otherInstances = Process.GetProcessesByName(currentProcess.ProcessName)
                                          .Where(p => p.Id != currentProcess.Id)
                                          .ToArray();

        if (otherInstances.Any())
        {
            Log.Warning("Found {Count} other running instance(s) of this application. Terminating them now.", otherInstances.Length);
            foreach (var instance in otherInstances)
            {
                try
                {
                    Log.Information("Terminating process with PID {ProcessId} started at {StartTime}.", instance.Id, instance.StartTime);
                    instance.Kill();
                    instance.WaitForExit(5000); // Wait up to 5 seconds for it to close.
                    Log.Information("Process {ProcessId} terminated successfully.", instance.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to terminate process with PID {ProcessId}.", instance.Id);
                }
            }
        }
        else
        {
            Log.Information("No other instances found. Proceeding with normal startup.");
        }
    }

    private static void SetupAutomaticShutdown()
    {
        // Use the configured value for the shutdown timer.
        long shutdownMilliseconds = (long)appSettings!.AppRunDurationHours * 60 * 60 * 1000;
        shutdownTimer = new Timer(shutdownMilliseconds);
        shutdownTimer.Elapsed += OnShutdownTimerElapsed;
        shutdownTimer.AutoReset = false;
        shutdownTimer.Enabled = true;
        Log.Information("Automatic shutdown timer started. Application will exit in {Hours} hours.", appSettings.AppRunDurationHours);
    }

    private static void OnShutdownTimerElapsed(object? source, System.Timers.ElapsedEventArgs e)
    {
        Log.Information("The configured {Hours}-hour runtime limit has been reached. Shutting down application as planned.", appSettings!.AppRunDurationHours);
        Environment.Exit(0);
    }

    // ... (The service-related methods: CheckAndStartService, OnTimedEvent, ManageService, IsAdministrator are UNCHANGED) ...

    private static async Task CheckAndStartService()
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            Log.Information("Checking status of service '{ServiceName}'...", serviceName);
            Log.Information("Current status: {Status}", sc.Status);

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                Log.Warning("Service is stopped. Attempting to start...");
                await Task.Run(() =>
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                });
                Log.Information("Service '{ServiceName}' started successfully.", serviceName);
            }
            else
            {
                Log.Information("Service is already running or in a pending state. No action needed.");
            }
        }
        catch (InvalidOperationException)
        {
            Log.Error("Service '{ServiceName}' was not found on this computer.", serviceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while checking and starting service '{ServiceName}'.", serviceName);
        }
    }

    private static void OnTimedEvent(object? source, System.Timers.ElapsedEventArgs e)
    {
        Log.Information("Timer triggered at {SignalTime:G}. Executing timed check.", e.SignalTime);
        _ = ManageService();
    }

    private static async Task ManageService()
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            Log.Information("Performing timed check on service '{ServiceName}'. Current status: {Status}", serviceName, sc.Status);

            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    Log.Information("Service is running. Attempting to restart...");
                    await Task.Run(() =>
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                        Log.Debug("Service stopped successfully.");
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
                    });
                    Log.Information("Service '{ServiceName}' restarted successfully.", serviceName);
                    break;

                case ServiceControllerStatus.Stopped:
                    Log.Warning("Service is stopped. Attempting to start...");
                    await Task.Run(() =>
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
                    });
                    Log.Information("Service '{ServiceName}' started successfully.", serviceName);
                    break;

                default:
                    Log.Warning("Service is in a transitional state: {Status}. No action will be taken.", sc.Status);
                    break;
            }
        }
        catch (InvalidOperationException)
        {
            Log.Error("Service '{ServiceName}' was not found during the timed check.", serviceName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during the timed action on service '{ServiceName}'.", serviceName);
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            Log.Information("Administrator check: {IsAdmin}", isAdmin);
            return isAdmin;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not determine administrator status.");
            return false;
        }
    }
}