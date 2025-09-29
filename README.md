# Windows Service Monitor Utility

A lightweight and robust .NET 8 console application designed to ensure a critical Windows Service remains running. This tool is built for automated environments and is ideal for execution via Windows Task Scheduler, especially when using managed service accounts (gMSA).

The application is highly configurable, allowing all operational parameters, such as timings and log paths, to be controlled via an external `appsettings.json` file.

---

## Key Features

- **Two Operational Modes:**
  - **Check & Start:** A one-time execution mode that checks if the target service is stopped and starts it if necessary.
  - **Timed Monitor:** Runs as a continuous background process, periodically restarting (if running) or starting (if stopped) the target service at a configurable interval.

- **Fully Configurable:** No hard-coded values. Control timings, log file locations, and more through `appsettings.json`.
  - Set the service check interval (e.g., every 30 minutes).
  - Set the total application runtime before it automatically shuts down (e.g., 23 hours), perfect for daily scheduled tasks.

- **Built for Automation:**
  - **Self-Healing Task Management:** When a new instance starts, it automatically finds and terminates any previous, lingering instances of itself, ensuring a fresh start.
  - **Administrator-Aware:** Checks for the necessary administrative privileges required to manage services.
  - **No Manual Intervention:** Designed to run completely unattended in the background.

- **Detailed Logging:**
  - Uses **Serilog** to create detailed, date-stamped log files.
  - Logs every action, from startup checks to service status changes and errors, providing a clear audit trail.
  - Logs are written to both the console and a file specified in the configuration.

## How to Use

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
1. Configuration
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Modify the `appsettings.json` file to suit your needs:

```json
{
  "Settings": {
    "LogFolderPath": "C:\\Logs\\ServiceMonitor",
    "ServiceCheckIntervalMinutes": 30,
    "AppRunDurationHours": 23
  }
}

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
2. Execution
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The application is controlled via command-line arguments. Run it from an Administrator command prompt or PowerShell.
Mode 1: Check and Start
Checks the "Print Spooler" service once and starts it if it's not running. The application then exits.
code
Bash
ServiceMonitor.exe "Print Spooler"
Mode 2: Timed Monitor
Starts monitoring the Windows Update service (wuauserv). It will restart/start the service every 30 minutes (as configured) and the monitor itself will automatically shut down after 23 hours.
code
Bash
ServiceMonitor.exe "wuauserv" -timed

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
3. Recommended Setup with Windows Task Scheduler
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Create a new task set to run Daily.
Configure the task to run as a service account (preferably a gMSA) that has "Log on as a batch job" rights and permissions to manage the target service.
Set the task to "Run with highest privileges".
In the "Actions" tab, point it to ServiceMonitor.exe and provide the service name and -timed argument.
This setup creates a reliable, self-healing daily cycle where the Task Scheduler starts a fresh instance of the monitor each day.

Prerequisites:

.NET 8.0 SDK (to build) or .NET 8.0 Runtime (to run)
Windows Operating System
Administrative privileges for execution

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
