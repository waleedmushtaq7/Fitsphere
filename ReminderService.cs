using FitSphere.Models;

namespace FitSphere.Services;

/// <summary>
/// Schedules one weekly alarm per training weekday at the chosen time (Android AlarmManager).
/// The notification text names the plan(s) scheduled for that weekday.
/// </summary>
public class ReminderService
{
    const int BaseCode = 7000;

#if ANDROID
    static Android.Content.Context Ctx => Android.App.Application.Context;

    static Android.App.PendingIntent Pi(int dow, string title = null, string text = null)
    {
        var intent = new Android.Content.Intent(Ctx, typeof(ReminderReceiver));
        if (title != null) { intent.PutExtra("title", title); intent.PutExtra("text", text); }
        return Android.App.PendingIntent.GetBroadcast(Ctx, BaseCode + dow, intent,
            Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.UpdateCurrent);
    }

    static Android.App.AlarmManager Alarm => (Android.App.AlarmManager)Ctx.GetSystemService(Android.Content.Context.AlarmService);
#endif

    public Task CancelAllAsync()
    {
#if ANDROID
        for (int d = 0; d < 7; d++) Alarm.Cancel(Pi(d));
#endif
        return Task.CompletedTask;
    }

    public async Task RescheduleAsync()
    {
        var u = Svc.Auth.Current;
        await CancelAllAsync();
        if (u == null || !u.ReminderEnabled || !u.NotificationsEnabled) return;
#if ANDROID
        var plans = await Svc.Workouts.PlansAsync();
        if (!TimeSpan.TryParse(u.ReminderTime, out var time)) time = new TimeSpan(18, 0, 0);

        for (int d = 0; d < 7; d++)
        {
            var dow = (DayOfWeek)d;
            var names = plans.Where(p => p.HasDay(dow)).Select(p => p.Name).ToList();
            if (names.Count == 0) continue;

            var now = DateTime.Now;
            var target = now.Date.Add(time).AddDays(((d - (int)now.DayOfWeek) + 7) % 7);
            if (target <= now) target = target.AddDays(7);
            long ms = new DateTimeOffset(target).ToUnixTimeMilliseconds();

            var pi = Pi(d, "Workout time", $"Today: {string.Join(", ", names)}. Let's get it done!");
            Alarm.SetInexactRepeating(Android.App.AlarmType.RtcWakeup, ms, 7L * 24 * 60 * 60 * 1000, pi);
        }
#endif
    }
}
