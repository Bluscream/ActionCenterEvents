using System;
using ActionCenterListener;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;

class Program
{
    static void Main(string[] args)
    {
        Utils.SetConsoleTitle("ActionCenterEvents");
        var poller = new ActionCenterPoller();
        var shutdownRequested = false;
        
        try
        {
            Console.WriteLine($"Database path: {poller._dbPath}");
            poller.OnNotification += notif =>
            {
                if (notif.Payload != null)
                {
                    Console.WriteLine($"[{notif.Timestamp}] {notif.AppId}: {notif.Payload.ToastTitle} - {notif.Payload.ToastBody}");
                }
            };

            Console.WriteLine("Press CTRL+C to exit...");
            
            // Set up CTRL+C handler
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent the process from terminating immediately
                Console.WriteLine("\nShutting down...");
                shutdownRequested = true;
            };

            // Keep the application running
            while (!shutdownRequested)
            {
                Thread.Sleep(100);
            }
        }
        finally
        {
            poller.Dispose();
        }
    }
}
