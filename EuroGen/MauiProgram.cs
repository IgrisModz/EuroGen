﻿#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using EuroGen.Services;

namespace EuroGen;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows =>
            {
                windows.OnWindowCreated(window =>
                {
                    // Obtenir la fenêtre native
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));

                    if (appWindow != null)
                    {
                        // Désactiver le redimensionnement
                        switch (appWindow.Presenter)
                        {
                            case OverlappedPresenter overlappedPresenter:
                                overlappedPresenter.IsMaximizable = false;
                                break;
                        }

                        // Centrer la fenêtre
                        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
                        var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                        var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                        appWindow.Move(new PointInt32(centerX, centerY));
                    }
                });
            });
        });
#endif

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddSingleton<LocalizationService>();

        builder.Services.AddSingleton<ThemeService>();

        builder.Services.AddSingleton<DrawService>();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddMudServices();

        VersionTracking.Track();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
