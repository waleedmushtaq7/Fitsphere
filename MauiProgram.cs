using Microsoft.Maui.Handlers;

namespace FitSphere;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if ANDROID
        // Remove the default Android underline so inputs can sit inside our rounded field containers.
        void Flat(Android.Views.View v) =>
            v.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        EntryHandler.Mapper.AppendToMapping("Flat", (h, v) => Flat(h.PlatformView));
        TimePickerHandler.Mapper.AppendToMapping("Flat", (h, v) => Flat(h.PlatformView));
#endif
        return builder.Build();
    }
}
