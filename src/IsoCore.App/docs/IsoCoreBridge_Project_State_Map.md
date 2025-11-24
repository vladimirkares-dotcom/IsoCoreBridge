IsoCoreBridge – Project State Map
High-level snapshot for AI assistants and developers
Last updated: 2025-11-24

## Solution structure
- Projects: `IsoCore.App` (WinUI 3 desktop app), `IsoCore.Domain` (domain models).
- Key subfolders: `Services`, `State`, `ViewModels`, `Views`, `docs`, `Assets`.
- Dependency injection is NOT using Microsoft.Extensions; all services are exposed as static properties in `App.xaml.cs`.

## Backend services – Current state
- **Auth Services**: `IUserAuthService` + `JsonUserAuthService` (offline, `users.encrypted`, hardened IO; `LoginResult` with `Success`, `ErrorCode`, `ErrorMessage`, `User`), `IRoleService` + `RoleService` (wraps `IsoCore.Domain.Roles` into `RoleOption`), `IUserDirectoryService` + `UserDirectoryService` (offline, local JSON users/index under `%LOCALAPPDATA%\IsoCoreBridge`, safe IO).
- **Storage Services**: `EncryptionService` (DPAPI + AES), `EncryptedProjectStorageService`, `IProjectStorage`, `ProjectStorageManager` (encrypted project list), `ProjectPathService` (local project directory layout). Stable.
- **State**: `AppState`, `AppStateService`, `ProjectRegistry` manage current project/user/date. Stable.
- **Login wiring**: `LoginPage` code-behind owns `LoginViewModel`, subscribes to `LoginSucceeded`, and navigates to `MainPage` via the app’s main frame.
- **Splash/auth gate**: `SplashViewModel.NavigateFromSplash()` ensures `App.MainWindow.Content` is a `Frame`, then routes to `LoginPage` if `App.AppState.CurrentUser` is null or to `MainPage` otherwise; guarded by a `_navigationTriggered` flag so it runs once after startup progress completes.

## ViewModels & UI structure
- Present: `DashboardViewModel`, `ProjectsViewModel`, `OverviewsViewModel`, `UsersViewModel`, `UserDetailViewModel`, `ChangePasswordViewModel`, `ProjectDetailPageViewModel`, `SettingsPageViewModel`, `SplashViewModel` (manages progress and one-time navigation via `NavigateFromSplash`), `LoginViewModel` (backend-only, trims username, validates non-empty username/password, uses `IsBusy` to prevent concurrent logins, raises `LoginSucceeded`), helper `RoleOption`, base `ViewModelBase`.
- UI/XAML pages exist and build cleanly, but this document does not track UI layout.
- `LoginPage` hosts `LoginViewModel`, finds `LoginButton` on `Loaded`, hooks Click to an async handler using `IsBusy` + button disabling, and detaches both Click and `LoginSucceeded` subscriptions on `Unloaded`. `LoginPage.xaml` contains a minimal structural form (header “Přihlášení”, TextBoxes bound to `Username`/`Password`, TextBlock bound to `ErrorMessage`, `Button` `x:Name="LoginButton"`).
- `UsersViewModel` includes `IsBusy`, `Users` collection of `UserListItem`, `SelectedUser` synced to `Edit*` properties (EditUsername, EditDisplayName, EditRole, EditLogin, EditWorkerNumber, EditTitleBefore, EditFirstName, EditLastName, EditTitleAfter, EditJobTitle, EditCompanyName, EditCompanyAddress, EditPhoneNumber, EditNote, EditIsActive, `UserFormError`), and operations `LoadUsersAsync`, `SaveUserAsync`, `DeleteUserAsync`, `CreateUserAsync` wired to `App.UserAuthService`; `UserFormError` is public/bindable, cleared when selection changes; `UsersPage.xaml.cs` helper `SetUserFormError` sets it directly.
- `SettingsPageViewModel` exposes bindable current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) refreshed from `App.AppState.CurrentUser`; `SettingsPage.xaml.cs` sets `DataContext`, refreshes on `Loaded`.
- `ChangePasswordViewModel` exposes `OldPassword`, `NewPassword`, `ConfirmPassword`, `ErrorMessage`, `SuccessMessage`, `IsBusy`; validates inputs (empty checks, match, length), guards with `IsBusy`, verifies old password via `LoginAsync`, changes the current user’s password via `SetPasswordAsync`, uses `SetError`/`SetSuccess` helpers; `ChangePasswordPage.xaml` shows password boxes + bound error/success TextBlocks, and code-behind forwards box values then calls `ChangePasswordAsync`.
- `ProjectsViewModel` exposes `Projects` (`ObservableCollection<ProjectInfo>`), `IsBusy`, `SelectedProject` synced s `AppState.CurrentProject` (setter volá `_appState.SetCurrentProject`, poslouchá `CurrentProjectChanged`), `LoadProjectsAsync()` načítá z `ProjectRegistry` s hlídáním `IsBusy`; `ProjectsPage.xaml` binduje `ItemsSource`/`SelectedItem`, `SelectionChanged` volá code-behind k nastavení current projektu, `Loaded` spouští `LoadProjectsAsync`.
- `DashboardViewModel` napojen na `AppState` (CurrentProject, CurrentUser, ProjectRegistry), drží `Projects`, statistiky (Total/Preparation/Execution/Completed), texty pro aktuální projekt a uživatele; `DashboardPage` poslouchá změny AppState a zobrazuje karty progresu/aktuálního projektu/seznamu projektů.

## Modulový stav
- USERS (S-balíček): hotovo (uživatelé, CRUD, hesla, UserFormError binding).
- SETTINGS + ChangePassword: hotovo (snapshot aktuálního uživatele, změna hesla, stránky navázané na VM).
- PROJECTS (P-balíček): P1–P5 hotovo (async načtení, výběr ↔ AppState sync, základní hinty/akce na ProjectsPage); P6 (UI/UX pro navigaci/přehled) zbývá.
- DASHBOARD (D-balíček): D1–D6 hotovo (analýza, data, navigace, shell) + D6–P1 až D6–P5 hotovo (finální moderní vizuální design ve stylu VL4D, karty, typografie, mezery); dashboard je 100 % dokončený, další práce: UI/UX pro levé navigační menu.

## Completed migration steps (chronological)
- Migrated `RoleService` and `IRoleService` (offline role model).
- Added hardened `JsonUserAuthService` implementing `IUserAuthService` with offline `users.encrypted` storage.
- Added offline `UserDirectoryService` for local user JSON files and directories.
- Added backend-only `LoginViewModel` using `App.UserAuthService.LoginAsync`, `LoginResult`, `LoginSucceeded`, `IsBusy`, and input validation.
- Added `LoginPage.xaml` minimal structural login form and `LoginPage.xaml.cs` wiring `LoginViewModel` to navigate to `MainPage` on `LoginSucceeded`, with button disable/enable and handler cleanup.
- Completed splash routing: `SplashViewModel` now routes to `LoginPage` or `MainPage` based on `App.AppState.CurrentUser` once startup steps finish.
- Implemented Users backend operations (load/save/delete/create, `SelectedUser` ↔ `Edit*` sync, public `UserFormError`) using `App.UserAuthService`; `UsersPage.xaml.cs` now sets `UserFormError` directly via helper (no reflection).
- Implemented SETTINGS backend: `SettingsPageViewModel` snapshot of current user wired to `SettingsPage`, and `ChangePasswordViewModel` + `ChangePasswordPage` providing password-change logic for the logged-in user via `IUserAuthService`.
- PROJECTS P1–P4: async loader in `ProjectsViewModel`, selection ↔ `AppState.CurrentProject` sync, ProjectsPage bound to `Projects`/`SelectedProject`, selection changes update AppState.
- Stabilized build pipeline (Debug/x64, win-x64).
- D6–P5: Final VL4D balance pass na DashboardPage – sjednocení spodního okraje header gridu s kartovým layoutem pomocí `IcbdSpacingMedium`, vizuální rytmus headeru a karet ustálen, build bez chyb.

## Next planned backend steps
- Remaining UI/UX wiring: richer validation for Login (focus/error presentation) and UI polish for Users/Settings/Projects (P5); replace the template MainPage with the real dashboard/shell using existing viewmodels.
- Cloud/Drive sync planned later; not implemented in the clean project.

## Styling zásady
- Nové UI prvky nepoužívají inline styly (Margin, FontSize, Opacity apod.); vizuální vzhled patří do společných stylů (např. Theme.xaml) a na stránkách se používá přes `StaticResource`.
- Stránky UsersPage, SettingsPage, ProjectsPage sdílí jednotný vizuální jazyk (typografie, mezery, sekční rozvržení) a další změny by ho měly zachovat.

## Notes for AI assistants
This file describes the current known state of the project. Assistants must respect: no editing `.csproj`, no touching XAML unless explicitly asked, services via `App` static properties, no Microsoft.Extensions DI, no cloud services. Paste this as context when starting a new conversation.
