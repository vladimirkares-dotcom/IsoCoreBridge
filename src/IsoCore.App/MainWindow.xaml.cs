using System;
using System.Runtime.InteropServices;
using IsoCore.App.Views;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace IsoCore.App;

internal static class Win32
{
    public const int SW_MAXIMIZE = 3;

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var hwnd = WindowNative.GetWindowHandle(this);
        Win32.ShowWindow(hwnd, Win32.SW_MAXIMIZE);
        Title = "IsoCoreBridge";
        RootFrame.Navigate(typeof(MainShellPage));
    }
}
