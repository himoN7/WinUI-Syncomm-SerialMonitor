using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Syncomm_Serial_Monitor.Services
{
    public class NotificationService
    {
        public async Task CopyToClipboardAsync(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            
            // Small delay to ensure clipboard is set
            await Task.Delay(100);
        }

        public void ShowNotification(string message, string severity)
        {
            // This would typically integrate with a notification system
            // For now, we'll just use debug output
            System.Diagnostics.Debug.WriteLine($"{severity}: {message}");
        }
    }
} 