using System;

namespace IsoCore.App.ViewModels;

public class SplashViewModel : ViewModelBase
{
    private const int TotalSteps = 9;
    private int _completedSteps;
    private string _statusText = "Připravuji spuštění.";
    private double _progressValue;
    private bool _navigationTriggered;

    public double ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (SetProperty(ref _progressValue, value))
            {
                OnPropertyChanged(nameof(ProgressLabel));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ProgressLabel => $"{Math.Min(100, (int)Math.Round(ProgressValue))}%";

    public void NavigateFromSplash()
    {
        var frame = App.MainWindow.Content as Frame;
        if (frame == null)
        {
            frame = new Frame();
            App.MainWindow.Content = frame;
        }

        var targetPage = App.AppState.CurrentUser == null
            ? typeof(LoginPage)
            : typeof(MainPage);

        frame.Navigate(targetPage);
    }

    public void OnStartupProgressChanged(string status)
    {
        StatusText = status;
        if (_completedSteps < TotalSteps)
        {
            _completedSteps++;
        }

        var targetProgress = TotalSteps == 0 ? 100 : _completedSteps * (100.0 / TotalSteps);
        ProgressValue = Math.Min(100, targetProgress);

        if (!_navigationTriggered && _completedSteps >= TotalSteps)
        {
            _navigationTriggered = true;
            NavigateFromSplash();
        }
    }
}
