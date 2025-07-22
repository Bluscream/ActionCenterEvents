# ActionCenterEvents

A Windows application that monitors Action Center notifications and executes custom scripts/events when notifications are received.

![](https://files.catbox.moe/568ier.png)

## Features

- Monitors Windows Action Center notifications in real-time
- Executes custom scripts when notifications are received
- Logs all notifications to CSV file
- Supports both console and non-console (background) modes
- Configurable environment variable prefix
- Global and user-specific event directories

## Configuration

The application can be configured via JSON files or command-line arguments:

### JSON Configuration Files

- **Program directory**: `ActionCenterEvents.json`
- **User profile**: `%USERPROFILE%\ActionCenterEvents.json`

### Configuration Options

```json
{
  "EnvironmentVariablePrefix": "ACTIONCENTER_",
  "Console": false,
  "csv": true
}
```

- `EnvironmentVariablePrefix`: Prefix for environment variables passed to executed scripts (default: "ACTIONCENTER_")
- `Console`: Whether to show console window (default: false for winexe builds)
- `csv`: Whether to log notifications to CSV file (default: true)

### Command-Line Arguments

- `--console` or `--console true`: Enable console window
- `--console false`: Disable console window
- `--csv` or `--csv true`: Enable CSV logging (default)
- `--csv false`: Disable CSV logging
- `--envprefix PREFIX`: Set environment variable prefix

**Note**: Boolean flags like `--console` and `--csv` default to `true` when the switch is present without a value.

## Event Scripts

Place executable files (`.exe`, `.bat`, `.cmd`, `.ps1`, etc.) in one of these directories:

- **Global**: `%PROGRAMDIR%\Events\OnActionCenterNotification\`
- **User**: `%USERPROFILE%\Events\OnActionCenterNotification\`

### Environment Variables

Scripts receive these environment variables (with configurable prefix):

- `{PREFIX}APPID`: Application ID that sent the notification
- `{PREFIX}TITLE`: Notification title
- `{PREFIX}BODY`: Notification body text
- `{PREFIX}PAYLOAD`: Full notification payload
- `{PREFIX}TIMESTAMP`: Notification timestamp
- `{PREFIX}DATETIME`: Current date/time

**Note:** Environment variables are not supported for Windows shortcut files (`.lnk`). If a `.lnk` file is executed, a warning will be logged and no environment variables will be passed to the target.

## Logging

All notifications are logged to: `%TEMP%\ActionCenterEvents.csv`

## Running Modes

### Console Mode
- Shows console window with real-time output
- Use CTRL+C to stop the application
- Enable with `--console true`

### Background Mode (Default)
- Runs silently in the background
- No console window
- Use shutdown scripts to stop the application

## Shutdown

### Console Mode
Press `CTRL+C` to stop the application.

### Background Mode
Use Task Manager or `taskkill` to stop the application process.

## Building

```bash
dotnet build -c Release
```

The application is built as a Windows executable (`winexe`) by default, which runs without a console window unless explicitly enabled.
