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
            ShowNotification(branding, title: branding.Name ?? "Zero Install", message, severity);
    }

    /// <summary>How long a notification shown on <see cref="_notificationTrayIcon"/> is displayed before the tray icon is hidden again.</summary>
    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(10);

    /// <summary>Tray icon used for showing notifications detached from the main GUI. Only access on the UI thread.</summary>
    private TrayIndicator? _notificationTrayIcon;

    /// <summary>Hides <see cref="_notificationTrayIcon"/> after <see cref="NotificationDuration"/>. Only access on the UI thread.</summary>
    private UITimer? _notificationTimer;

    /// <summary>When <see cref="_notificationTrayIcon"/> is due to be hidden. Only access on the UI thread.</summary>
    private DateTime _notificationHideAt;

    /// <summary>
    /// Displays a notification message detached from the main GUI.
    /// </summary>
    /// <param name="branding">Branding to apply to the tray icon.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message text.</param>
    /// <param name="severity">The severity of the log entry the message is based on; <c>null</c> for a plain message.</param>
    /// <param name="wait"><c>true</c> to block until the notification has been shown; <c>false</c> to return immediately (required when called from a <see cref="Log.Handler"/>, which may run while the UI thread waits for the caller).</param>
    /// <remarks>
    /// Uses <see cref="TryShowSystemNotification"/> if possible, otherwise falls back to an Eto.Forms <see cref="Notification"/>.
    /// The <see cref="Eto.Widget.Style"/> of that <see cref="Notification"/> is set to the name of the <paramref name="severity"/> (e.g., <c>"Warn"</c>), so platform-specific code can apply matching icons via <see cref="Eto.Style.Add{THandler}(string, Eto.StyleHandler{THandler})"/>.
    /// </remarks>
    private void ShowNotification(FeedBranding branding, string title, string message, LogSeverity? severity = null, bool wait = false)
    {
        try
        {
            if (TryShowSystemNotification(branding, title, message, severity)) return;
        }
        catch
        {
            // Fall back to Eto.Forms notification (not logged, since this may run inside a Log.Handler)
        }

        void Show()
        {
            try
            {
                var notification = new Notification {Title = title, Message = message, Style = severity?.ToString()};
                if (notification.RequiresTrayIndicator)
                {
                    _notificationTrayIcon ??= new() {Title = branding.Name ?? "Zero Install", Image = branding.GetTrayImage()};
                    _notificationTrayIcon.Show();
                    notification.Show(_notificationTrayIcon);
                    ScheduleHideNotificationTrayIcon();
                }
                else notification.Show();
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
    /// Hides <see cref="_notificationTrayIcon"/> once the latest notification shown on it has timed out. Must be called on the UI thread.
    /// </summary>
    private void ScheduleHideNotificationTrayIcon()
    {
        _notificationHideAt = DateTime.UtcNow + NotificationDuration;

        if (_notificationTimer == null)
        {
            _notificationTimer = new() {Interval = NotificationDuration.TotalSeconds};
            _notificationTimer.Elapsed += delegate
            {
                _notificationTimer.Stop();
                _notificationTrayIcon?.Hide();
            };
        }
        else _notificationTimer.Stop(); // Restart countdown

        _notificationTimer.Start();
    }

    /// <summary>
    /// Hook for displaying a notification message using a platform-specific mechanism (e.g., Windows toast notifications, which stick around even after the process ends).
    /// </summary>
    /// <param name="branding">Branding to apply to the notification.</param>
    /// <param name="title">The title of the message.</param>
    /// <param name="message">The message text.</param>
    /// <param name="severity">The severity of the log entry the message is based on; <c>null</c> for a plain message. May be ignored.</param>
    /// <returns><c>true</c> if the notification was shown; <c>false</c> to fall back to an Eto.Forms notification.</returns>
    /// <remarks>May be called from any thread, including from within a <see cref="Log.Handler"/>. Must not log.</remarks>
    protected virtual bool TryShowSystemNotification(FeedBranding branding, string title, string message, LogSeverity? severity) => false;

    /// <summary>
    /// Waits for notifications that were queued without waiting to be shown and for tray icon notifications to time out, then removes the tray icon.
    /// Without this, notifications could get lost or leave a ghost tray icon behind when the process exits.
    /// </summary>
    private void DisposeNotifications()
    {
        try
        {
            if (Application.Instance is not {} app) return;

            // The UI thread processes invocations in order, so this runs after any notifications queued so far
            var (remaining, uiThreadId) = app.Invoke(() => (
                _notificationTrayIcon is {Visible: true} ? _notificationHideAt - DateTime.UtcNow : TimeSpan.Zero,
                Environment.CurrentManagedThreadId));

            // Keep the notification visible until it times out (only when not blocking the UI thread)
            if (remaining > TimeSpan.Zero && uiThreadId != Environment.CurrentManagedThreadId)
                Thread.Sleep(remaining > NotificationDuration ? NotificationDuration : remaining);

            app.Invoke(() =>
            {
                _notificationTimer?.Dispose();
                _notificationTimer = null;
                _notificationTrayIcon?.Hide();
                _notificationTrayIcon?.Dispose();
                _notificationTrayIcon = null;
            });
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            // Application is already shutting down
        }
    }
}
