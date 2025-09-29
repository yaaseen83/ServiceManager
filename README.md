```markdown
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

### 1. Configuration

Before running, modify the `appsettings.json` file to suit your environment:

```json
{
  "Settings": {
    "LogFolderPath": "C:\\Logs\\ServiceMonitor",
    "ServiceCheckIntervalMinutes": 30,
    "AppRunDurationHours": 23
  }
}
```
- **LogFolderPath**: The full path to the directory where log files will be created. The application needs write permissions to this folder.
- **ServiceCheckIntervalMinutes**: In timed mode, how often (in minutes) the application checks on the target service.
- **AppRunDurationHours**: In timed mode, the total number of hours the application will run before automatically shutting down.

### 2. Execution

The application is controlled via command-line arguments. It must be run from a shell with **Administrator** privileges (e.g., an elevated Command Prompt or PowerShell).

**Syntax:** `ServiceMonitor.exe "<ServiceName>" [-timed]`

#### Mode 1: Check and Start

This mode checks the service once and starts it if it's not running. The application then exits.

**Example:**
```bash
ServiceMonitor.exe "Print Spooler"
```

#### Mode 2: Timed Monitor

This mode starts a long-running monitor. It will restart or start the service based on the `ServiceCheckIntervalMinutes` setting and will automatically shut down after the duration specified by `AppRunDurationHours`.

**Example:**
```bash
# The service name for Windows Update is "wuauserv"
ServiceMonitor.exe "wuauserv" -timed
```

### 3. Recommended Setup with Windows Task Scheduler

This application is designed to be managed by the Windows Task Scheduler for a "set it and forget it" monitoring solution.

1.  **Open Task Scheduler** and select **Create Task...**.
2.  **General Tab:**
    -   Give the task a descriptive name (e.g., "Monitor Windows Update Service").
    -   Under "Security options", click **Change User or Group...** and set it to a dedicated service account (a **gMSA** is highly recommended). This account must be a local administrator or have sufficient rights to manage services.
    -   Select the radio button for **"Run whether user is logged on or not"**.
    -   Check the box for **"Run with highest privileges"**.

3.  **Triggers Tab:**
    -   Click **New...** and create a trigger. For a daily cycle, select **"Daily"** and set a start time (e.g., 3:00 AM).

4.  **Actions Tab:**
    -   Click **New...** and set the Action to **"Start a program"**.
    -   **Program/script:** Browse to the location of `ServiceMonitor.exe`.
    -   **Add arguments (optional):** Enter the target service name and the timed flag. Example: `"wuauserv" -timed`
    -   **Start in (optional):** It is a best practice to set this to the directory where your executable is located. This ensures the `appsettings.json` file is found correctly.

5.  **Settings Tab:**
    -   Ensure the default setting **"Stop the task if it runs longer than..."** is either disabled or set to a value greater than your `AppRunDurationHours`.
    -   For added reliability, you can configure the task to restart if it fails.

This setup creates a reliable, self-healing daily cycle where the Task Scheduler launches a fresh instance of the monitor each day, which then runs for its configured 23-hour duration before cleanly exiting.

## Prerequisites

- **.NET 8.0 Runtime** (to run the compiled executable).
- **.NET 8.0 SDK** (to build the project from source).
- **Windows Operating System** (Server or Desktop).
- **Administrative Privileges** for execution.
```
