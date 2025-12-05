IsoCoreBridge – Project State Map
High-level snapshot for AI assistants and developers
Last updated: 2025-11-24

## Solution structure
- Projects: `IsoCore.App` (WinUI 3 desktop app), `IsoCore.Domain` (domain models).
- Key subfolders: `Services`, `State`, `ViewModels`, `Views`, `docs`, `Assets`.
- Dependency injection is NOT using Microsoft.Extensions; all services are exposed as static properties in `App.xaml.cs`.

## Backend services – Current state
- **Auth Services**: `IUserAuthService` + `JsonUserAuthService` (offline, `users.encrypted`, hardened IO; `LoginResult` with `Success`, `ErrorCode`, `ErrorMessage`, `User`), `IRoleService` + `RoleService` (wraps `IsoCore.Domain.Roles` into `RoleOption`), `IUserDirectoryService` + `UserDirectoryService` (offline, local JSON users/index under `%LOCALAPPDATA%\IsoCoreBridge`, safe IO).
- **Storage Services**: `EncryptionService` (DPAPI + AES), `EncryptedProjectStorageService`, `IProjectStorage`, `ProjectStorageManager` (encrypted project list), `ProjectPathService` (local project directory layout), `IProjectsStorageService` + `ProjectsStorageService` (encrypted projects store abstraction). Stable.
- **State**: `AppState`, `AppStateService`, `ProjectRegistry` manage current project/user/date; ProjectRegistry is dispatcher-safe for UI-bound collections. Stable.
- **Login wiring**: `LoginPage` code-behind owns `LoginViewModel`, subscribes to `LoginSucceeded`, and navigates to `MainPage` via the app’s main frame.
- **Splash/auth gate**: `SplashViewModel.NavigateFromSplash()` ensures `App.MainWindow.Content` is a `Frame`, then routes to `LoginPage` if `App.AppState.CurrentUser` is null or to `MainPage` otherwise; guarded by a `_navigationTriggered` flag so it runs once after startup progress completes.

## Navigation system status
- `PageRoute` integrated in `MainShellPage`; `NavigateTo(PageRoute route)` centralizes routing.
- Basic routes (Dashboard / Projects / SettingsUsers) stable; click handlers now funnel through `NavigateTo`.

## ViewModels & UI structure
- Present: `DashboardViewModel`, `ProjectsViewModel`, `OverviewsViewModel`, `UsersViewModel`, `UserDetailViewModel`, `ChangePasswordViewModel`, `ProjectDetailPageViewModel`, `SettingsPageViewModel`, `SplashViewModel` (manages progress and one-time navigation via `NavigateFromSplash`), `LoginViewModel` (backend-only, trims username, validates non-empty username/password, uses `IsBusy` to prevent concurrent logins, raises `LoginSucceeded`), helper `RoleOption`, base `ViewModelBase`.
- UI/XAML pages exist and build cleanly, but this document does not track UI layout.
- `LoginPage` hosts `LoginViewModel`, finds `LoginButton` on `Loaded`, hooks Click to an async handler using `IsBusy` + button disabling, and detaches both Click and `LoginSucceeded` subscriptions on `Unloaded`. `LoginPage.xaml` contains a minimal structural form (header “Přihlášení”, TextBoxes bound to `Username`/`Password`, TextBlock bound to `ErrorMessage`, `Button` `x:Name="LoginButton"`).
- `UsersViewModel` includes `IsBusy`, `Users` collection of `UserListItem`, `SelectedUser` synced to `Edit*` properties (EditUsername, EditDisplayName, EditRole, EditLogin, EditWorkerNumber, EditTitleBefore, EditFirstName, EditLastName, EditTitleAfter, EditJobTitle, EditCompanyName, EditCompanyAddress, EditPhoneNumber, EditNote, EditIsActive, `UserFormError`), and operations `LoadUsersAsync`, `SaveUserAsync`, `DeleteUserAsync`, `CreateUserAsync` wired to `App.UserAuthService`; `UserFormError` is public/bindable, cleared when selection changes; `UsersPage.xaml.cs` helper `SetUserFormError` sets it directly.
- `SettingsPageViewModel` exposes bindable current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) refreshed from `App.AppState.CurrentUser`; `SettingsPage.xaml.cs` sets `DataContext`, refreshes on `Loaded`.
- `ChangePasswordViewModel` exposes `OldPassword`, `NewPassword`, `ConfirmPassword`, `ErrorMessage`, `SuccessMessage`, `IsBusy`; validates inputs (empty checks, match, length), guards with `IsBusy`, verifies old password via `LoginAsync`, changes the current user’s password via `SetPasswordAsync`, uses `SetError`/`SetSuccess` helpers; `ChangePasswordPage.xaml` shows password boxes + bound error/success TextBlocks, and code-behind forwards box values then calls `ChangePasswordAsync`.
- `ProjectsViewModel` exposes `Projects` (`ObservableCollection<ProjectInfo>`), `IsBusy`, `SelectedProject` synced s `AppState.CurrentProject` (setter volá `_appState.SetCurrentProject`, poslouchá `CurrentProjectChanged`), `LoadProjectsAsync()` načítá z `ProjectRegistry` s hlídáním `IsBusy`; `ProjectsPage.xaml` binduje `ItemsSource`/`SelectedItem`, `SelectionChanged` volá code-behind k nastavení current projektu, `Loaded` spouští `LoadProjectsAsync`. Project mutations are dispatcher-safe via ProjectRegistry.
- `DashboardViewModel` napojen na `AppState` (CurrentProject, CurrentUser, ProjectRegistry), drží `Projects`, statistiky (Total/Preparation/Execution/Completed), texty pro aktuální projekt a uživatele; `DashboardPage` poslouchá změny AppState a zobrazuje karty progresu/aktuálního projektu/seznamu projektů.

## UI / Design – Left menu
- B-3b implemented: interface shows only „Hlavní“ (Dashboard, Projekty, Přehledy) and „Nastavení“ (Uživatelé + disabled placeholders); sections „Data“, „Kontroling“, „Ceníky“, „Pomůcky“ are commented out.

## Modulový stav
- USERS (S-balíček): hotovo (uživatelé, CRUD, hesla, UserFormError binding). A-2a: CoreCompanyName spravuje AppState; EmploymentType logika stabilní. Known issue: nového uživatele nelze vytvořit – pending analýza (pozdější krok).
- SETTINGS + ChangePassword: hotovo (snapshot aktuálního uživatele, změna hesla, stránky navázané na VM).
- PROJECTS (P-balíček): P1-P5 hotovo (async načtení, výběr ↔ AppState sync, základní hinty/akce na ProjectsPage); P6 (UI/UX pro navigaci/přehled) zbývá. Projekty jsou async-loaded přes ProjectsStorageService + ProjectRegistry (dispatcher-safe); UI má kompaktní dvouřádkový layout seznamu (kód + název, status), dialog pro založení projektu (auto výběr), dialog pro editaci (validace unikátního kódu), dialog pro smazání s potvrzením, a double-click open na řádku seznamu. Edit/Delete jsou auto-enabled/disabled dle výběru.
- DASHBOARD (D-balíček): D1–D6 hotovo (analýza, data, navigace, shell) + D6–P1 až D6–P5 hotovo (finální moderní vizuální design ve stylu VL4D, karty, typografie, mezery); dashboard je 100 % dokončený.
- PROJECT DETAIL (F9): ProjectDetailPageViewModel poskytuje odvozen?, read-only vlastnosti z AppState.CurrentProject (titulek, k?d, n?zev, stavov? text, popis, form?tovan? created/updated). Header je dvousloupcov? (titulek+stav vlevo, timestampy + tla??tko "Zp?t" vpravo). Obsah: sekce Z?kladn? informace; placeholder karta "Stavebn? objekty" (p?ipraveno na BuildingObjectInfo list); textov? placeholdery pro "V?po?ty a souhrny" a "Pozn?mky a dokumenty". Str?nka je stabiln?, read-only, p?ipraven? pro budouc? SO CRUD, v?po?ty/den?k/exporty.

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
- E1–P5: Stabilizace levého menu – rollback riskantního ControlTemplate (AccessViolation), zachování spacing/typografie z E1–P1/E1–P2, IcbdMenuButton nyní jednoduchý stabilní styl; pokročilé Forge efekty odloženy do budoucího bloku.

## Next planned backend steps
- Remaining UI/UX wiring: richer validation for Login (focus/error presentation) and UI polish for Users/Settings/Projects (P5); replace the template MainPage with the real dashboard/shell using existing viewmodels.
- Cloud/Drive sync planned later; not implemented in the clean project.

## Styling zásady
- Nové UI prvky nepoužívají inline styly (Margin, FontSize, Opacity apod.); vizuální vzhled patří do společných stylů (např. Theme.xaml) a na stránkách se používá přes `StaticResource`.
- Stránky UsersPage, SettingsPage, ProjectsPage sdílí jednotný vizuální jazyk (typografie, mezery, sekční rozvržení) a další změny by ho měly zachovat.

## Notes for AI assistants
This file describes the current known state of the project. Assistants must respect: no editing `.csproj`, no touching XAML unless explicitly asked, services via `App` static properties, no Microsoft.Extensions DI, no cloud services. Paste this as context when starting a new conversation.

**F5 status:** Core layout finished, functional consistency achieved.  
Remaining tasks = only visual polish, postponed to final UI pass.


**F5 status:** Core layout finished, functional consistency achieved.  
Remaining tasks = only visual polish, postponed to final UI pass.

**F6 status:** ProjectDetailPage navigation and layout completed; ProjectsPage header filters polished. Ready to proceed with ProjectInfo-based CRUD (create/edit/delete) in the next block.

**F7 status:** Done / stable — threading/dispatcher fixes around Projects & Users; projects storage abstraction added; project creation dialog + double-click open implemented.

**F8 status:** Done / stable — Projects CRUD on dispatcher-safe infra (create/edit/delete dialogs with validation, async load via ProjectsStorageService + ProjectRegistry, compact list layout, selection-based button enablement).


**F9 status:** Done / stable - ProjectDetailPage read-only detail view (viewmodel-backed header, Zakladni informace, Stavebni objekty placeholder card, future sections for vypocty/souhrny and poznamky/dokumenty).

**G-block status (ProjectDetail/BuildingObjects):** Implemented – first iteration done, edit UI pending. G1-G5 wired: ProjectDetailPage bound to ProjectDetailPageViewModel and AppState (header + timestamps); BuildingObjects list bound to CurrentProject.BuildingObjects with selection-driven detail panel; command state (HasBuildingObjects/HasNoBuildingObjects, CanExecute) drives button enablement; AddBuildingObjectCommand creates a new BuildingObjectInfo (default name "Nový stavební objekt", Status=Draft, HasNaip=false), attaches it to the current project and selects it; DeleteBuildingObjectCommand removes the selected object from the current project/VM collection and updates selection (next/prev/null). EditBuildingObjectCommand exists but real edit UX will come in G6. Known UX note: after create, focus shifts to detail panel making the project context less visible; to be refined with the edit UI in G6.
