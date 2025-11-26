using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;

namespace IsoCore.App.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private static DispatcherQueue? _dispatcherQueue;

    public static void InitializeDispatcherQueue(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        var handler = PropertyChanged;
        if (handler is null)
        {
            return;
        }

        var dispatcher = _dispatcherQueue;

        if (dispatcher == null || dispatcher.HasThreadAccess)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
            return;
        }

        dispatcher.TryEnqueue(() => handler(this, new PropertyChangedEventArgs(propertyName)));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
