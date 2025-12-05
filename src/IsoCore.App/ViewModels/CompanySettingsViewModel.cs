using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IsoCore.App.Services;

namespace IsoCore.App.ViewModels;

public sealed class CompanySettingsViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;
    private string _companyName;
    private string _phonePrefix;
    private string _emailDomain;
    private string _companyStreet;
    private string _companyCity;
    private string _companyZip;
    private string _companyCountry;
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isSaving;

    public CompanySettingsViewModel(IAppStateService appState)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));

        _companyName = _appState.CoreCompanyName;
        _phonePrefix = _appState.DefaultPhonePrefix;
        _emailDomain = _appState.EmailDomain;
        _companyStreet = _appState.CompanyStreet;
        _companyCity = _appState.CompanyCity;
        _companyZip = _appState.CompanyZip;
        _companyCountry = _appState.CompanyCountry;

        SaveCommand = new RelayCommand(async _ => await SaveAsync().ConfigureAwait(false), _ => !IsSaving);
    }

    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string PhonePrefix
    {
        get => _phonePrefix;
        set => SetProperty(ref _phonePrefix, value);
    }

    public string EmailDomain
    {
        get => _emailDomain;
        set => SetProperty(ref _emailDomain, value);
    }

    public string CompanyStreet
    {
        get => _companyStreet;
        set => SetProperty(ref _companyStreet, value);
    }

    public string CompanyCity
    {
        get => _companyCity;
        set => SetProperty(ref _companyCity, value);
    }

    public string CompanyZip
    {
        get => _companyZip;
        set => SetProperty(ref _companyZip, value);
    }

    public string CompanyCountry
    {
        get => _companyCountry;
        set => SetProperty(ref _companyCountry, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value) && SaveCommand is RelayCommand rc)
            {
                rc.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand { get; }

    public async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var name = CompanyName?.Trim();
            var prefix = string.IsNullOrWhiteSpace(PhonePrefix) ? "+420" : PhonePrefix.Trim();
            var domain = EmailDomain?.Trim() ?? string.Empty;

            _appState.UpdateCompanySettings(
                name,
                prefix,
                domain,
                CompanyStreet,
                CompanyCity,
                CompanyZip,
                CompanyCountry);
            StatusMessage = "Nastavení uloženo.";
        }
        catch
        {
            ErrorMessage = "Nastavení se nepodařilo uložit.";
        }
        finally
        {
            await Task.Delay(1).ConfigureAwait(false);
            IsSaving = false;
        }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
