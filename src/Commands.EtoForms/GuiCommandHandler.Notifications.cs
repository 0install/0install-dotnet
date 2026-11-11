// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.EtoForms;

partial class GuiCommandHandler
{
    /// <inheritdoc/>
    protected override void LogHandler(LogSeverity severity, string? message, Exception? exception)
    {
        // Do not bother user with connectivity errors for background update checks
        if (Background && exception is WebException) return;

        base.LogHandler(severity, message, exception);
    }

    /// <summary>
    /// Displays <see cref="Log"/> entries as notification messages detached from the main GUI.
    /// </summary>
    protected override void DisplayLogEntry(LogSeverity severity, string message)
    {
        if (TryGetBranding() is {} branding)
            ShowNotification(branding, title: branding.Name ?? "Zero Install", message);
    }

    /// <summary>Tray icon used for showing notifications detached from the main GUI. Only access on the UI thread.</summary>
    private TrayIndicator? _notificationTrayIcon;

    /// <summary>
    /// Displays a notification message detached from the main GUI.
    /// </summary>
    /// <param name="branding">Branding to apply to the tray icon.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message text.</param>
    /// <param name="wait"><c>true</c> to block until the notification has been shown; <c>false</c> to return immediately (required when called from a <see cref="Log.Handler"/>, which may run while the UI thread waits for the caller).</param>
    /// <remarks>Uses <see cref="TryShowSystemNotification"/> if possible, otherwise falls back to an Eto.Forms tray icon.</remarks>
    private void ShowNotification(FeedBranding branding, string title, string message, bool wait = false)
    {
        try
        {
            if (TryShowSystemNotification(branding, title, message)) return;
        }
        catch
        {
            // Fall back to tray icon notification (not logged, since this may run inside a Log.Handler)
        }

        void Show()
        {
            try
            {
                _notificationTrayIcon ??= new() {Title = branding.Name ?? "Zero Install", Image = branding.Icon};
                _notificationTrayIcon.Show();
                new Notification {Title = title, Message = message}.Show(_notificationTrayIcon);
            }
            catch
            {
                // Notifications are only shown with best effort
            }
        }

        if (wait) Application.Instance.Invoke(Show);
        else Application.Instance.AsyncInvoke(Show);
    }

    /// <summary>
    /// Hook for displaying a notification message using a platform-specific mechanism (e.g., Windows toast notifications, which stick around even after the process ends).
    /// </summary>
    /// <param name="branding">Branding to apply to the notification.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message text.</param>
    /// <returns><c>true</c> if the notification was shown; <c>false</c> to fall back to an Eto.Forms tray icon notification.</returns>
    /// <remarks>May be called from any thread, including from within a <see cref="Log.Handler"/>. Must not log.</remarks>
    protected virtual bool TryShowSystemNotification(FeedBranding branding, string title, string message) => false;

    /// <summary>
    /// Waits for notifications that were queued without waiting to be shown. Without this they could get lost when the process exits.
    /// </summary>
    private static void FlushNotifications()
    {
        try
        {
            // The UI thread processes invocations in order
            Application.Instance?.Invoke(() => {});
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            // Application is already shutting down
        }
    }
}
