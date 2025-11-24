ChatGPT, here is what is currently implemented in the clean IsoCoreBridge project and what you can safely migrate next (backend only, no XAML or build changes).

## Current State
- Domain models: `ProjectInfo`, `BuildingObjectInfo`, `CalcProfile`, `PerformanceEntry`, `ProjectStatus`, `UserAccount`, `Roles` (role constants + display names).
- Core services/storage: `EncryptionService` (DPAPI-backed AES key), `EncryptedProjectStorageService` + `IProjectStorage` + `ProjectStorageManager` (encrypted project list persistence), `ProjectPathService` (local folders), `PasswordHasher` (SHA256), `AppStateService` (current project/user/date) with `ProjectRegistry` + `AppState` classes in `src/IsoCore.App/State`.
- ViewModels present: `DashboardViewModel`, `ProjectsViewModel`, `OverviewsViewModel`, `UsersViewModel`, `UserDetailViewModel`, `ChangePasswordViewModel`, `ProjectDetailPageViewModel`, `SettingsPageViewModel`, `SplashViewModel`, helper `RoleOption`, base `ViewModelBase`.
- Settings: `SettingsPageViewModel` exposes current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) via `RefreshFromAppState()` reading `App.AppState.CurrentUser`; `SettingsPage.xaml.cs` creates the viewmodel with `App.UserAuthService`, sets `DataContext`, and refreshes on `Loaded`.
- Change password: `ChangePasswordViewModel` validates old/new/confirm passwords (empty checks, match, length ≥ 6), guards with `IsBusy`, clears messages via helpers `SetError`/`SetSuccess`, verifies the old password with `_authService.LoginAsync`, updates with `_authService.SetPasswordAsync`, and exposes `ErrorMessage`/`SuccessMessage`; `ChangePasswordPage.xaml` hosts password boxes, bound error/success TextBlocks, and a button calling `OnChangePasswordClicked` that copies box values into the viewmodel and calls `ChangePasswordAsync`.
- Projects: `ProjectsViewModel` exposes `Projects` (from `ProjectRegistry`), `IsBusy`, `LoadProjectsAsync()` (guards null registry/IsBusy), `SelectedProject` synced to `AppState.CurrentProject` (setter calls `_appState.SetCurrentProject`, listens to `CurrentProjectChanged`); `ProjectsPage.xaml` binds `ItemsSource` to `Projects` and `SelectedItem` to `SelectedProject`, `SelectionChanged` calls code-behind to set current project, and `Loaded` triggers `LoadProjectsAsync`.
- Settings: `SettingsPageViewModel` exposes current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) initialized from `App.AppState.CurrentUser` via `RefreshFromAppState()`; `SettingsPage.xaml.cs` wires `DataContext` to the viewmodel and refreshes on `Loaded`.
- Change password: `ChangePasswordViewModel` uses `IUserAuthService` + `App.AppState.CurrentUser` to validate old/new/confirm password, verifies the old password via `LoginAsync`, changes it via `SetPasswordAsync`, and exposes `ErrorMessage`, `SuccessMessage`, `IsBusy`; `ChangePasswordPage.xaml.cs` wires `DataContext` to the viewmodel.
- Views (XAML pages) exist for Dashboard, MainPage, Projects, etc.; not changed here.
- Auth: `IUserAuthService` implemented by `JsonUserAuthService` (offline, file-backed, hardened against missing/corrupt stores); login/role checks use local storage only. `LoginResult` provides `Success`, `ErrorCode`, `ErrorMessage`, `User` (Domain `UserAccount`).
- Roles: `IRoleService` + `RoleService` wrap `IsoCore.Domain.Roles` into `RoleOption` for listing, default role (worker/delnik), and lookup by id.
- DI style: no `Microsoft.Extensions`/`ServiceCollection`; services are exposed as static properties on `App.xaml.cs` (e.g., `RoleService`, `UserAuthService`).
- User directory: `IUserDirectoryService` + `UserDirectoryService` in `src/IsoCore.App/Services/Users` manage local user folders and JSON index under `%LOCALAPPDATA%\IsoCoreBridge`; offline-only, hardened IO (directories created safely, read/write failures are best-effort).
- Login viewmodel: `LoginViewModel` (backend-only) exposes `Username`, `Password`, `ErrorMessage`, `IsBusy`, `LoginAsync()`; trims username, blocks empty username/password with `"Zadejte uživatelské jméno i heslo."`, uses `IsBusy` to prevent concurrent logins, calls `App.UserAuthService.LoginAsync`, sets `App.AppState.SetCurrentUser`, raises `LoginSucceeded` on success.
- Login event pipeline/UI: `LoginViewModel` raises `LoginSucceeded`; `Views/LoginPage.xaml.cs` creates the viewmodel, sets it as `DataContext`, subscribes to the event, and navigates to `MainPage` via the main window frame; on `Loaded` it finds `LoginButton` with `FindName`, wires Click to the async login, disables while busy, and unsubscribes Click/`LoginSucceeded` on `Unloaded`. `LoginPage.xaml` has a minimal structural form (header “Přihlášení”, TextBoxes bound to `Username`/`Password`, TextBlock bound to `ErrorMessage`, and `Button` `x:Name="LoginButton"` with content “Přihlásit”; no inline styling).
- Splash/auth gate: `SplashViewModel.NavigateFromSplash()` ensures `App.MainWindow.Content` is a `Frame` and routes to `LoginPage` if `App.AppState.CurrentUser` is null, otherwise to `MainPage`; `_navigationTriggered` + `OnStartupProgressChanged` ensure single navigation after startup completes.
- Users backend: `UsersViewModel` exposes `IsBusy`, `Users` collection, and operations: `LoadUsersAsync` (reloads from `App.UserAuthService.GetUsersAsync` into `UserListItem`), `SaveUserAsync` (builds `UserAccount` from `SelectedUser`, calls `App.UserAuthService.UpdateUserAsync`, reloads), `DeleteUserAsync` (calls `App.UserAuthService.DeleteUserAsync`, clears selection, reloads), `CreateUserAsync` (builds `UserAccount` from `Edit*` fields with defaults like `Role = Roles.Technik` when empty and display/login fallback to `EditUsername.Trim()`, then calls `App.UserAuthService.CreateUserAsync` and reloads). `SelectedUser` setter syncs all `Edit*` fields (clears when null, copies values when non-null) and clears `UserFormError` on selection change to avoid stale errors. `UserFormError` is a public bindable property; `UsersPage.xaml.cs` uses a helper `SetUserFormError` that sets the property directly (no reflection) until UI binding is updated.
- No Google Drive/cloud sync services (`ICloudSyncService`, `GoogleDriveSyncService`, `GoogleDriveAuthService`, `DriveConfigLoader`, `StartupOrchestrator`, etc.) in this repo. App launches cleanly (Debug/x64) with `JsonUserAuthService` hardened to avoid startup crashes.

## Current Backend Migration Status
- RoleService: added in `src/IsoCore.App/Services/Auth` with `IRoleService` to list roles, resolve default (worker/delnik), and lookup by id using `IsoCore.Domain.Roles`; exposed via `App.RoleService`.
- Auth: `JsonUserAuthService` added in `src/IsoCore.App/Services/Auth`, implements `IUserAuthService`, stores users in local `%LOCALAPPDATA%\IsoCoreBridge\users.encrypted`, hashes passwords with `PasswordHasher`, no cloud/Drive/Firebase dependencies. Initialization is hardened (missing/corrupt files fall back to empty store, decryption failures degrade gracefully, no startup exceptions).
- User directory: `UserDirectoryService` added in `src/IsoCore.App/Services/Users`, manages local user files/index (`users/*.json`, `auth/accounts.json`) under `%LOCALAPPDATA%\IsoCoreBridge`, offline-only with best-effort IO and safe constructor.
- Login pipeline/wiring: fully wired end-to-end — `SplashViewModel` routes to `LoginPage` or `MainPage` based on `App.AppState.CurrentUser`; `LoginPage.xaml.cs` hosts `LoginViewModel`, wires the login button with `IsBusy` + button disabling and cleans up handlers on `Unloaded`; `LoginViewModel` trims/validates inputs, calls `App.UserAuthService.LoginAsync`, updates `App.AppState.SetCurrentUser`, and raises `LoginSucceeded` on success. Minimal structural login UI is present.
- Users backend: implemented (load/save/delete/create, `SelectedUser` ↔ `Edit*` sync, public `UserFormError`) using `App.UserAuthService`; `UsersPage.xaml.cs` helper sets `UserFormError` directly (reflection removed) pending UI binding updates.
- Settings backend: implemented snapshot of current user (`SettingsPageViewModel` + `SettingsPage.xaml.cs`, refresh on `Loaded`) and password change backend (`ChangePasswordViewModel` + `ChangePasswordPage.xaml/.cs`) via `IUserAuthService` and `App.AppState.CurrentUser`, with validation, message helpers, bound error/success display, and button wiring to `ChangePasswordAsync`.
- Projects backend wiring: `ProjectsViewModel` loads projects asynchronously from `ProjectRegistry`, guards with `IsBusy`, and syncs `SelectedProject` with `AppState.CurrentProject`; `ProjectsPage` binds list selection to `SelectedProject`, calls `LoadProjectsAsync` on `Loaded`, and updates current project on selection.
- Dashboard: `DashboardPage` + `DashboardViewModel` are wired to `AppState` (CurrentProject/CurrentUser/ProjectRegistry), expose project stats and current project/user labels, and listen to AppState changes for cards (progress, current project, projects overview).
- DI style: services exposed as static properties on `App.xaml.cs` (no `Microsoft.Extensions`/container usage), e.g., `App.RoleService`, `App.UserAuthService`.
- Build/startup: `dotnet build .\src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64` succeeds; app starts without crashes in VS/Debug x64 after auth hardening.
- Stav balíčků: USERS (S-balíček) – 100 % hotovo; SETTINGS + ChangePassword – 100 % hotovo; PROJECTS (P-balíček) – P1–P5 hotovo (load + výběr + AppState sync, ProjectsPage hinty/akce), P6 UI/UX zbývá; DASHBOARD (D-balíček) – D1–D6 hotovo (analýza + data + navigace + kontrola funkční parity, inline styly v Theme.xaml, základní grid/spacing layout polish v D5, start shell v D6 – aplikace startuje přímo do DashboardPage, šablona Hello World odstraněna); D6+ (finální moderní vizuální design ve stylu VL4D v krocích D6–P1 až D6–P5) hotovo – dashboard je plně doladěný.

## D6–P5 – Final VL4D Balance Pass (Completed)

**Changes performed:**
- Unified dashboard header spacing by aligning `IcbdDashboardHeaderGrid` bottom margin with the global `IcbdSpacingMedium` token.
- Ensured the visual rhythm between the header and the dashboard cards grid matches the spacing cadence established in D6–P1 through D6–P4.
- Verified that no padding overrides or layout regressions were introduced.

**Modified files:**
- `src/IsoCore.App/Styles/Theme.xaml`

**Build result:**
- `dotnet build src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64`  
  → success, 0 errors, 0 warnings.

**Status:**
- D6–P1 až D6–P5 hotovo, dashboard design je nyní 100 % dokončený a stabilní (VL4D Forge styl).
- Další krok: UI/UX blok pro levé navigační menu.

## Next Steps (planned)
- Remaining UI tasks: richer validation/UX for Login, UI/UX polish for Users/Settings pages, and replacing the template MainPage with the real dashboard/shell; PROJECTS P5 (UI/UX pro přehled/navigaci projektů).
- USERS: Exposed `UserFormError` as public bindable property — DONE (UsersPage helper now sets it directly).
- SETTINGS: DONE (current-user snapshot, SettingsPage wiring, ChangePassword validation + message helpers, and ChangePasswordPage wiring).
- PROJECTS: P1–P4 DONE (async load, selection ↔ AppState sync, ProjectsPage wiring); P5 (UI/UX v přehledech) je dalším krokem.

## Styling zásady (nové UI prvky)
- Nové UI prvky nepoužívají inline styly (Margin, FontSize, Opacity apod.); vizuální vzhled patří do společných stylů (např. Theme.xaml) a na stránkách se používá přes `StaticResource`.
- Sdílený vizuální jazyk (typografie, mezery, karty/sekce) na UsersPage, SettingsPage, ProjectsPage je potřeba dodržovat při dalších změnách.
