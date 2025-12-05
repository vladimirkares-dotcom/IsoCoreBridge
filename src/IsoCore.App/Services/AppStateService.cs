using System;
using IsoCore.App.State;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public interface IAppStateService
{
    ProjectInfo? CurrentProject { get; }
    DateOnly CurrentDate { get; }
    ProjectRegistry ProjectRegistry { get; }
    BuildingObjectInfo? CurrentBuildingObject { get; }
    BuildingObjectEditSession? CurrentBuildingObjectEditSession { get; set; }
    UserAccount? CurrentUser { get; }
    bool IsAdmin { get; }
    string CoreCompanyName { get; }
    string DefaultPhonePrefix { get; }
    string EmailDomain { get; }
    string CompanyStreet { get; }
    string CompanyCity { get; }
    string CompanyZip { get; }
    string CompanyCountry { get; }
    string? PendingPasswordChangeUsername { get; }
    event EventHandler? CompanySettingsChanged;

    event EventHandler<ProjectInfo?>? CurrentProjectChanged;
    event EventHandler<DateOnly>? CurrentDateChanged;
    event EventHandler<BuildingObjectInfo?>? CurrentBuildingObjectChanged;
    event EventHandler? BuildingObjectEditSessionChanged;
    event EventHandler? CurrentUserChanged;

    void SetCurrentProject(ProjectInfo? project);
    void SetCurrentDate(DateOnly date);
    void SetCurrentBuildingObject(BuildingObjectInfo? obj);
    void SetCurrentUser(UserAccount? user);
    void Logout();
    void RequestPasswordChange(string username);
    void ClearPasswordChangeRequest();
    void UpdateCompanySettings(string? companyName, string? phonePrefix, string? emailDomain, string? street, string? city, string? zip, string? country);
}

public class AppStateService : IAppStateService
{
    private readonly ProjectRegistry _projectRegistry;
    private ProjectInfo? _currentProject;
    private DateOnly _currentDate = DateOnly.FromDateTime(DateTime.Today);
    private BuildingObjectInfo? _currentBuildingObject;
    private BuildingObjectEditSession? _currentBuildingObjectEditSession;
    private UserAccount? _currentUser;
    private string? _pendingPasswordChangeUsername;
    private const string DefaultCompanyName = "Stavby mostů a.s.";
    private const string DefaultPhonePrefixValue = "+420";
    private const string DefaultEmailDomainValue = "example.com";
    private const string DefaultStreetValue = "";
    private const string DefaultCityValue = "";
    private const string DefaultZipValue = "";
    private const string DefaultCountryValue = "Česká republika";
    private string _coreCompanyName = DefaultCompanyName;
    private string _defaultPhonePrefix = DefaultPhonePrefixValue;
    private string _emailDomain = DefaultEmailDomainValue;
    private string _companyStreet = DefaultStreetValue;
    private string _companyCity = DefaultCityValue;
    private string _companyZip = DefaultZipValue;
    private string _companyCountry = DefaultCountryValue;

    public AppStateService(ProjectRegistry projectRegistry)
    {
        _projectRegistry = projectRegistry ?? throw new ArgumentNullException(nameof(projectRegistry));
    }

    public ProjectInfo? CurrentProject => _currentProject;
    public DateOnly CurrentDate => _currentDate;
    public ProjectRegistry ProjectRegistry => _projectRegistry;
    public BuildingObjectInfo? CurrentBuildingObject => _currentBuildingObject;
    public BuildingObjectEditSession? CurrentBuildingObjectEditSession
    {
        get => _currentBuildingObjectEditSession;
        set
        {
            if (!Equals(_currentBuildingObjectEditSession, value))
            {
                _currentBuildingObjectEditSession = value;
                BuildingObjectEditSessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    public UserAccount? CurrentUser => _currentUser;
    public bool IsAdmin => string.Equals(_currentUser?.Role, Roles.Administrator, StringComparison.OrdinalIgnoreCase);
    public string CoreCompanyName => _coreCompanyName;
    public string DefaultPhonePrefix => _defaultPhonePrefix;
    public string EmailDomain => _emailDomain;
    public string CompanyStreet => _companyStreet;
    public string CompanyCity => _companyCity;
    public string CompanyZip => _companyZip;
    public string CompanyCountry => _companyCountry;

    public event EventHandler<ProjectInfo?>? CurrentProjectChanged;
    public event EventHandler<DateOnly>? CurrentDateChanged;
    public event EventHandler<BuildingObjectInfo?>? CurrentBuildingObjectChanged;
    public event EventHandler? BuildingObjectEditSessionChanged;
    public event EventHandler? CurrentUserChanged;
    public event EventHandler? CompanySettingsChanged;

    public void SetCurrentProject(ProjectInfo? project)
    {
        if (!Equals(_currentProject, project))
        {
            _currentProject = project;
            CurrentProjectChanged?.Invoke(this, _currentProject);
        }
    }

    public void SetCurrentDate(DateOnly date)
    {
        if (_currentDate != date)
        {
            _currentDate = date;
            CurrentDateChanged?.Invoke(this, _currentDate);
        }
    }

    public void SetCurrentBuildingObject(BuildingObjectInfo? obj)
    {
        if (!Equals(_currentBuildingObject, obj))
        {
            _currentBuildingObject = obj;
            CurrentBuildingObjectChanged?.Invoke(this, _currentBuildingObject);
        }
    }

    public void SetCurrentUser(UserAccount? user)
    {
        if (_currentUser?.Id == user?.Id)
        {
            return;
        }

        _currentUser = user;
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Logout() => SetCurrentUser(null);

    public string? PendingPasswordChangeUsername => _pendingPasswordChangeUsername;

    public void RequestPasswordChange(string username)
    {
        _pendingPasswordChangeUsername = username;
    }

    public void ClearPasswordChangeRequest()
    {
        _pendingPasswordChangeUsername = null;
    }

    public void UpdateCompanySettings(string? companyName, string? phonePrefix, string? emailDomain, string? street, string? city, string? zip, string? country)
    {
        var normalizedName = string.IsNullOrWhiteSpace(companyName) ? DefaultCompanyName : companyName.Trim();
        var normalizedPrefix = string.IsNullOrWhiteSpace(phonePrefix) ? DefaultPhonePrefixValue : phonePrefix.Trim();
        var normalizedDomain = emailDomain?.Trim() ?? string.Empty;
        var normalizedStreet = street?.Trim() ?? DefaultStreetValue;
        var normalizedCity = city?.Trim() ?? DefaultCityValue;
        var normalizedZip = zip?.Trim() ?? DefaultZipValue;
        var normalizedCountry = string.IsNullOrWhiteSpace(country) ? DefaultCountryValue : country.Trim();

        if (string.Equals(_coreCompanyName, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_defaultPhonePrefix, normalizedPrefix, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_emailDomain, normalizedDomain, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_companyStreet, normalizedStreet, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_companyCity, normalizedCity, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_companyZip, normalizedZip, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_companyCountry, normalizedCountry, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _coreCompanyName = normalizedName;
        _defaultPhonePrefix = normalizedPrefix;
        _emailDomain = normalizedDomain;
        _companyStreet = normalizedStreet;
        _companyCity = normalizedCity;
        _companyZip = normalizedZip;
        _companyCountry = normalizedCountry;

        CompanySettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
