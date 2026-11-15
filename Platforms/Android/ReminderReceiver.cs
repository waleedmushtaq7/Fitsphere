using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace FitSphere;

/// <summary>Receives the weekly alarm and shows a workout reminder notification.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public const string ChannelId = "fitsphere_reminders";

    public override void OnReceive(Context context, Intent intent)
    {
        var title = intent.GetStringExtra("title") ?? "FitSphere";
        var text = intent.GetStringExtra("text") ?? "Time for your workout.";

        var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            nm.CreateNotificationChannel(new NotificationChannel(ChannelId, "Workout reminders", NotificationImportance.Default));

        var launch = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
        var pi = PendingIntent.GetActivity(context, 0, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var n = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetSmallIcon(Android.Resource.Drawable.IcPopupReminder)
            .SetAutoCancel(true)
            .SetContentIntent(pi)
            .Build();
        nm.Notify(4242, n);
    }
}
