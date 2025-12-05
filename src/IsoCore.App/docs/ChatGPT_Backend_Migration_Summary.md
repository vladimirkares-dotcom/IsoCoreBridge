ChatGPT, here is what is currently implemented in the clean IsoCoreBridge project and what you can safely migrate next (backend only, no XAML or build changes).

## Current State

Latest updates (2025-12-05):
- Přidána stránka Nastavení firmy (`BrandingPage`) + `CompanySettingsViewModel`; `AppStateService` nyní drží kmenovou firmu, výchozí telefonní předvolbu, e-mailovou doménu a adresu (ulice/město/PSČ/stát) a vystavuje `CompanySettingsChanged`.
- `UsersViewModel` při tvorbě předvyplňuje firmu/předvolbu z nastavení, generuje login z jména+příjmení (bez diakritiky, formát `jmeno.prijmeni`, unikátní suffix při kolizi) a e-mail `login@{doména}`. Manuální editace loginu/e-mailu jsou respektovány; po uložení se edit page vrací na seznam.
- Seznam uživatelů správně mapuje jméno/roli pro nové uživatele; levé menu má položku „Nastavení firmy“ (dříve Branding).
- Domain models: `ProjectInfo`, `BuildingObjectInfo`, `CalcProfile`, `PerformanceEntry`, `ProjectStatus`, `UserAccount`, `Roles` (role constants + display names).
- Core services/storage: `EncryptionService` (DPAPI-backed AES key), `EncryptedProjectStorageService` + `IProjectStorage` + `ProjectStorageManager` (encrypted project list persistence), `ProjectPathService` (local folders), `PasswordHasher` (SHA256), `AppStateService` (current project/user/date) with `ProjectRegistry` + `AppState` classes in `src/IsoCore.App/State`.
- ViewModels present: `DashboardViewModel`, `ProjectsViewModel`, `OverviewsViewModel`, `UsersViewModel`, `UserDetailViewModel`, `ChangePasswordViewModel`, `ProjectDetailPageViewModel`, `SettingsPageViewModel`, `SplashViewModel`, helper `RoleOption`, base `ViewModelBase`.
- Settings: `SettingsPageViewModel` exposes current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) via `RefreshFromAppState()` reading `App.AppState.CurrentUser`; `SettingsPage.xaml.cs` creates the viewmodel with `App.UserAuthService`, sets `DataContext`, and refreshes on `Loaded`.
- Change password: `ChangePasswordViewModel` validates old/new/confirm passwords (empty checks, match, length â‰Ą 6), guards with `IsBusy`, clears messages via helpers `SetError`/`SetSuccess`, verifies the old password with `_authService.LoginAsync`, updates with `_authService.SetPasswordAsync`, and exposes `ErrorMessage`/`SuccessMessage`; `ChangePasswordPage.xaml` hosts password boxes, bound error/success TextBlocks, and a button calling `OnChangePasswordClicked` that copies box values into the viewmodel and calls `ChangePasswordAsync`.
- Projects: `ProjectsViewModel` exposes `Projects` (from `ProjectRegistry`), `IsBusy`, `LoadProjectsAsync()` (guards null registry/IsBusy), `SelectedProject` synced to `AppState.CurrentProject` (setter calls `_appState.SetCurrentProject`, listens to `CurrentProjectChanged`); `ProjectsPage.xaml` binds `ItemsSource` to `Projects` and `SelectedItem` to `SelectedProject`, `SelectionChanged` calls code-behind to set current project, and `Loaded` triggers `LoadProjectsAsync`.
- Settings: `SettingsPageViewModel` exposes current-user snapshot (`CurrentUsername`, `CurrentDisplayName`, `CurrentRoleName`, `CurrentLogin`) initialized from `App.AppState.CurrentUser` via `RefreshFromAppState()`; `SettingsPage.xaml.cs` wires `DataContext` to the viewmodel and refreshes on `Loaded`.
- Change password: `ChangePasswordViewModel` uses `IUserAuthService` + `App.AppState.CurrentUser` to validate old/new/confirm password, verifies the old password via `LoginAsync`, changes it via `SetPasswordAsync`, and exposes `ErrorMessage`, `SuccessMessage`, `IsBusy`; `ChangePasswordPage.xaml.cs` wires `DataContext` to the viewmodel.
- Views (XAML pages) exist for Dashboard, MainPage, Projects, etc.; not changed here.
- Auth: `IUserAuthService` implemented by `JsonUserAuthService` (offline, file-backed, hardened against missing/corrupt stores); login/role checks use local storage only. `LoginResult` provides `Success`, `ErrorCode`, `ErrorMessage`, `User` (Domain `UserAccount`).
- Roles: `IRoleService` + `RoleService` wrap `IsoCore.Domain.Roles` into `RoleOption` for listing, default role (worker/delnik), and lookup by id.
- DI style: no `Microsoft.Extensions`/`ServiceCollection`; services are exposed as static properties on `App.xaml.cs` (e.g., `RoleService`, `UserAuthService`).
- User directory: `IUserDirectoryService` + `UserDirectoryService` in `src/IsoCore.App/Services/Users` manage local user folders and JSON index under `%LOCALAPPDATA%\IsoCoreBridge`; offline-only, hardened IO (directories created safely, read/write failures are best-effort).
- Login viewmodel: `LoginViewModel` (backend-only) exposes `Username`, `Password`, `ErrorMessage`, `IsBusy`, `LoginAsync()`; trims username, blocks empty username/password with `"Zadejte uĹľivatelskĂ© jmĂ©no i heslo."`, uses `IsBusy` to prevent concurrent logins, calls `App.UserAuthService.LoginAsync`, sets `App.AppState.SetCurrentUser`, raises `LoginSucceeded` on success.
- Login event pipeline/UI: `LoginViewModel` raises `LoginSucceeded`; `Views/LoginPage.xaml.cs` creates the viewmodel, sets it as `DataContext`, subscribes to the event, and navigates to `MainPage` via the main window frame; on `Loaded` it finds `LoginButton` with `FindName`, wires Click to the async login, disables while busy, and unsubscribes Click/`LoginSucceeded` on `Unloaded`. `LoginPage.xaml` has a minimal structural form (header â€śPĹ™ihlĂˇĹˇenĂ­â€ť, TextBoxes bound to `Username`/`Password`, TextBlock bound to `ErrorMessage`, and `Button` `x:Name="LoginButton"` with content â€śPĹ™ihlĂˇsitâ€ť; no inline styling).
- Splash/auth gate: `SplashViewModel.NavigateFromSplash()` ensures `App.MainWindow.Content` is a `Frame` and routes to `LoginPage` if `App.AppState.CurrentUser` is null, otherwise to `MainPage`; `_navigationTriggered` + `OnStartupProgressChanged` ensure single navigation after startup completes.
- Users backend: `UsersViewModel` exposes `IsBusy`, `Users` collection, and operations: `LoadUsersAsync` (reloads from `App.UserAuthService.GetUsersAsync` into `UserListItem`), `SaveUserAsync` (builds `UserAccount` from `SelectedUser`, calls `App.UserAuthService.UpdateUserAsync`, reloads), `DeleteUserAsync` (calls `App.UserAuthService.DeleteUserAsync`, clears selection, reloads), `CreateUserAsync` (builds `UserAccount` from `Edit*` fields with defaults like `Role = Roles.Technik` when empty and display/login fallback to `EditUsername.Trim()`, then calls `App.UserAuthService.CreateUserAsync` and reloads). `SelectedUser` setter syncs all `Edit*` fields (clears when null, copies values when non-null) and clears `UserFormError` on selection change to avoid stale errors. `UserFormError` is a public bindable property; `UsersPage.xaml.cs` uses a helper `SetUserFormError` that sets the property directly (no reflection) until UI binding is updated.
- No Google Drive/cloud sync services (`ICloudSyncService`, `GoogleDriveSyncService`, `GoogleDriveAuthService`, `DriveConfigLoader`, `StartupOrchestrator`, etc.) in this repo. App launches cleanly (Debug/x64) with `JsonUserAuthService` hardened to avoid startup crashes.

## Current Backend Migration Status
- RoleService: added in `src/IsoCore.App/Services/Auth` with `IRoleService` to list roles, resolve default (worker/delnik), and lookup by id using `IsoCore.Domain.Roles`; exposed via `App.RoleService`.
- Auth: `JsonUserAuthService` added in `src/IsoCore.App/Services/Auth`, implements `IUserAuthService`, stores users in local `%LOCALAPPDATA%\IsoCoreBridge\users.encrypted`, hashes passwords with `PasswordHasher`, no cloud/Drive/Firebase dependencies. Initialization is hardened (missing/corrupt files fall back to empty store, decryption failures degrade gracefully, no startup exceptions).
- User directory: `UserDirectoryService` added in `src/IsoCore.App/Services/Users`, manages local user files/index (`users/*.json`, `auth/accounts.json`) under `%LOCALAPPDATA%\IsoCoreBridge`, offline-only with best-effort IO and safe constructor.
- Login pipeline/wiring: fully wired end-to-end â€” `SplashViewModel` routes to `LoginPage` or `MainPage` based on `App.AppState.CurrentUser`; `LoginPage.xaml.cs` hosts `LoginViewModel`, wires the login button with `IsBusy` + button disabling and cleans up handlers on `Unloaded`; `LoginViewModel` trims/validates inputs, calls `App.UserAuthService.LoginAsync`, updates `App.AppState.SetCurrentUser`, and raises `LoginSucceeded` on success. Minimal structural login UI is present.
- Users backend: implemented (load/save/delete/create, `SelectedUser` â†” `Edit*` sync, public `UserFormError`) using `App.UserAuthService`; `UsersPage.xaml.cs` helper sets `UserFormError` directly (reflection removed) pending UI binding updates.
- Settings backend: implemented snapshot of current user (`SettingsPageViewModel` + `SettingsPage.xaml.cs`, refresh on `Loaded`) and password change backend (`ChangePasswordViewModel` + `ChangePasswordPage.xaml/.cs`) via `IUserAuthService` and `App.AppState.CurrentUser`, with validation, message helpers, bound error/success display, and button wiring to `ChangePasswordAsync`.
- Projects backend wiring: `ProjectsViewModel` loads projects asynchronously from `ProjectRegistry`, guards with `IsBusy`, and syncs `SelectedProject` with `AppState.CurrentProject`; `ProjectsPage` binds list selection to `SelectedProject`, calls `LoadProjectsAsync` on `Loaded`, and updates current project on selection.
- Dashboard: `DashboardPage` + `DashboardViewModel` are wired to `AppState` (CurrentProject/CurrentUser/ProjectRegistry), expose project stats and current project/user labels, and listen to AppState changes for cards (progress, current project, projects overview).
- DI style: services exposed as static properties on `App.xaml.cs` (no `Microsoft.Extensions`/container usage), e.g., `App.RoleService`, `App.UserAuthService`.
- Build/startup: `dotnet build .\src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64` succeeds; app starts without crashes in VS/Debug x64 after auth hardening.
- Stav balĂ­ÄŤkĹŻ: USERS (S-balĂ­ÄŤek) â€“ 100 % hotovo; SETTINGS + ChangePassword â€“ 100 % hotovo; PROJECTS (P-balĂ­ÄŤek) â€“ P1â€“P5 hotovo (load + vĂ˝bÄ›r + AppState sync, ProjectsPage hinty/akce), P6 UI/UX zbĂ˝vĂˇ; DASHBOARD (D-balĂ­ÄŤek) â€“ D1â€“D6 hotovo (analĂ˝za + data + navigace + kontrola funkÄŤnĂ­ parity, inline styly v Theme.xaml, zĂˇkladnĂ­ grid/spacing layout polish v D5, start shell v D6 â€“ aplikace startuje pĹ™Ă­mo do DashboardPage, Ĺˇablona Hello World odstranÄ›na); D6+ (finĂˇlnĂ­ modernĂ­ vizuĂˇlnĂ­ design ve stylu VL4D v krocĂ­ch D6â€“P1 aĹľ D6â€“P5) hotovo â€“ dashboard je plnÄ› doladÄ›nĂ˝. LEFT NAV (E1) â€“ P1â€“P5 hotovo (struktura + mezery, typografie/layout, pokus o pokroÄŤilĂ˝ template v E1â€“P3/E1â€“P4 nĂˇslednÄ› rollback; E1â€“P5 stabilizovĂˇno jednoduchĂ˝m stylem, pokroÄŤilĂ© efekty pĹ™esunuty do budoucĂ­ho bloku).

## Recent updates (Completed)
- **PageRoute refactor (MainShellPage):** `MainShellPage.xaml.cs` now routes via `NavigateTo(PageRoute route)`; Dashboard/Projects/Users click handlers call `NavigateTo`; redundant direct navigation removed; `_currentRoute` added to track state.
- **B-3b Left Navigation Cleanup:** Left menu shows only „Hlavní“ (Dashboard, Projekty, Přehledy) and „Nastavení“ (Uživatelé + disabled placeholders); sections „Data“, „Kontroling“, „Ceníky“, „Pomůcky“ are wrapped in a single XAML comment block.
- **A-2a CoreCompanyName → AppState:** Added `string CoreCompanyName { get; }` to `IAppStateService`, implemented in `AppStateService` with default "Stavby mostů a.s."; removed the hardcoded constant from `UsersViewModel`; `EmploymentType` now uses `_appState.CoreCompanyName`.

## D6â€“P5 â€“ Final VL4D Balance Pass (Completed)

**Changes performed:**
- Unified dashboard header spacing by aligning `IcbdDashboardHeaderGrid` bottom margin with the global `IcbdSpacingMedium` token.
- Ensured the visual rhythm between the header and the dashboard cards grid matches the spacing cadence established in D6â€“P1 through D6â€“P4.
- Verified that no padding overrides or layout regressions were introduced.

**Modified files:**
- `src/IsoCore.App/Styles/Theme.xaml`

**Build result:**
- `dotnet build src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64`  
  â†’ success, 0 errors, 0 warnings.

**Status:**
- D6â€“P1 aĹľ D6â€“P5 hotovo, dashboard design je nynĂ­ 100 % dokonÄŤenĂ˝ a stabilnĂ­ (VL4D Forge styl).
- DalĹˇĂ­ krok: UI/UX blok pro levĂ© navigaÄŤnĂ­ menu.

## E1â€“P5 â€“ Left nav stabilization (Completed)

**Changes performed:**
- ZruĹˇen pokroÄŤilĂ˝ ControlTemplate s hover/pressed/focus + accent barem (E1â€“P3/E1â€“P4) kvĹŻli runtime AccessViolationException ve WinUI 3.
- IcbdMenuButton vrĂˇcen na jednoduchĂ˝, template-less styl (bez VSM), ponechĂˇvĂˇ VL4D-aligned spacing/typografii z E1â€“P1/E1â€“P2.
- PokroÄŤilĂ© Forge efekty pro navigaci budou znovu navrĹľeny v budoucĂ­m bloku (mimo E1).

**Modified files:**
- `src/IsoCore.App/Styles/LeftMenuStyles.xaml`

**Build result:**
- `dotnet build src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64`  
  â†’ success, 0 errors, 1 warning (locked IsoCore.Domain.dll).

**Status:**
- E1â€“P1 aĹľ E1â€“P5 hotovo; aktuĂˇlnĂ­ baseline navigaÄŤnĂ­ho UI je stabilnĂ­. DalĹˇĂ­ prĂˇce: novĂ˝ blok pro pokroÄŤilĂ© Forge vizuĂˇlnĂ­ efekty v navigaci.

## D6â€“P5 â€“ Final VL4D Balance Pass (Completed)

**Changes performed:**
- Unified dashboard header spacing by aligning `IcbdDashboardHeaderGrid` bottom margin with the global `IcbdSpacingMedium` token.
- Ensured the visual rhythm between the header and the dashboard cards grid matches the spacing cadence established in D6â€“P1 through D6â€“P4.
- Verified that no padding overrides or layout regressions were introduced.

**Modified files:**
- `src/IsoCore.App/Styles/Theme.xaml`

**Build result:**
- `dotnet build src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64`  
  â†’ success, 0 errors, 0 warnings.

**Status:**
- D6â€“P1 aĹľ D6â€“P5 hotovo, dashboard design je nynĂ­ 100 % dokonÄŤenĂ˝ a stabilnĂ­ (VL4D Forge styl).
- DalĹˇĂ­ krok: UI/UX blok pro levĂ© navigaÄŤnĂ­ menu.

## Next Steps (planned)
- Remaining UI tasks: richer validation/UX for Login, UI/UX polish for Users/Settings pages, and replacing the template MainPage with the real dashboard/shell; PROJECTS P5 (UI/UX pro pĹ™ehled/navigaci projektĹŻ).
- USERS: Exposed `UserFormError` as public bindable property â€” DONE (UsersPage helper now sets it directly).
- SETTINGS: DONE (current-user snapshot, SettingsPage wiring, ChangePassword validation + message helpers, and ChangePasswordPage wiring).
- PROJECTS: P1â€“P4 DONE (async load, selection â†” AppState sync, ProjectsPage wiring); P5 (UI/UX v pĹ™ehledech) je dalĹˇĂ­m krokem.

## Styling zĂˇsady (novĂ© UI prvky)
- NovĂ© UI prvky nepouĹľĂ­vajĂ­ inline styly (Margin, FontSize, Opacity apod.); vizuĂˇlnĂ­ vzhled patĹ™Ă­ do spoleÄŤnĂ˝ch stylĹŻ (napĹ™. Theme.xaml) a na strĂˇnkĂˇch se pouĹľĂ­vĂˇ pĹ™es `StaticResource`.
- SdĂ­lenĂ˝ vizuĂˇlnĂ­ jazyk (typografie, mezery, karty/sekce) na UsersPage, SettingsPage, ProjectsPage je potĹ™eba dodrĹľovat pĹ™i dalĹˇĂ­ch zmÄ›nĂˇch.
### F5 – ProjectsPage UI Unification (Update)

- Two-by-two grid layout completed:
  - Left top: main actions card
  - Left bottom: recent changes card
  - Right top: project list card
  - Right bottom: project detail card
- All primary/secondary buttons unified under IcbdButtonSecondaryStyle.
- All action buttons now use HorizontalAlignment="Stretch" to keep consistent width.
- Right-top card: increased row spacing for cleaner hierarchy.
- Filter / Search / Sort buttons prepared for layout unification (final spacing polish moved to the end of F-series).
- Page builds cleanly; layout is stable and visually consistent.
- Remaining fine-tuning (icon buttons, micro-spacing corrections) deferred to the final polish phase after all functional blocks.
### F5 – ProjectsPage UI Unification (Update)

- Two-by-two grid layout completed:
  - Left top: main actions card
  - Left bottom: recent changes card
  - Right top: project list card
  - Right bottom: project detail card
- All primary/secondary buttons unified under `IcbdButtonSecondaryStyle`.
- All action buttons now use `HorizontalAlignment="Stretch"` to keep consistent width.
- Right-top card: increased row spacing for cleaner hierarchy.
- Filter / Search / Sort buttons prepared for layout unification (final spacing polish moved to the end of F-series).
- Page builds cleanly; layout is stable and visually consistent.
- Remaining fine-tuning (icon buttons, micro-spacing corrections) deferred to the final polish phase after all functional blocks.
### F6 – Project detail layout & projects header polish

- Added a functional "Zpět" button to ProjectDetailPage, navigating back to ProjectsPage using the existing navigation mechanism.
- Reworked ProjectDetailPage to use the shared MenuPage layout and dashboard card style.
- Introduced a structured "Základní informace" section showing project name, code, description and created/updated dates (read-only bindings to the current project).
- Fixed jumbled basic info fields by switching to vertical label/value rows with shared spacing and typography.
- Updated the "Seznam projektů" card header so Filtr/Hledat/Řazení remain in a single row with equal-width secondary buttons.
- Build remains clean and UI is now ready for the upcoming Project CRUD implementation (create/edit/delete projects on top of the ProjectInfo model).

### F7-A - ViewModel dispatcher marshalling

- Eliminated WinRT COMException 0x8001010E by ensuring PropertyChanged notifications are raised on the UI thread.
- ViewModelBase now uses DispatcherQueue marshalling when invoked from background threads.
- DispatcherQueue is initialized once at startup via the main window; behavior falls back to current thread if uninitialized (tests/early boot).
- Maintains existing SetProperty/INotifyPropertyChanged surface while making notifications thread-safe for WinUI.


### F7-B - ProjectRegistry dispatcher marshalling

- ObservableCollection<ProjectInfo> mutations in ProjectRegistry now run on the UI thread via DispatcherQueue.
- Prevented WinRT COMException 0x8001010E triggered by background-thread collection changes during project load.
- Uses the same dispatcher initialized at startup; behavior remains compatible with existing ProjectsViewModel callers.


### F7-C - Projects storage hardening

- Invalid or incompatible projects JSON now resets to an empty store with a backup of the corrupted file, avoiding crashes.
- Project load/save no longer triggers side effects like opening File Explorer; storage runs silently during startup and project load.


### F7-D - Async polish for projects loading

- ProjectRegistry.LoadFromStorageAsync now uses ConfigureAwait(false) on the storage I/O call to make background execution explicit before UI marshalling of collection updates.
- ProjectsPage Loaded handler follows the standard WinUI async pattern (plain await), with heavy work remaining inside the view model.


### F7-Final - UI-thread marshaling fixes

- ProjectRegistry dispatcher helper now awaits without ConfigureAwait(false), ensuring collection updates stay on the UI thread and avoid COMException 0x8001010E.
- UsersViewModel.LoadUsersAsync mirrors the safe pattern: fetch users off the UI thread, then marshal Users collection mutations via DispatcherQueue to the UI thread.

### F7-Final - Threading & projects storage stabilization

- ProjectRegistry now resolves DispatcherQueue at call time and updates Projects on the UI thread, eliminating COMException 0x8001010E.
- UsersViewModel.LoadUsersAsync performs I/O off the UI thread and marshals Users mutations via DispatcherQueue (no cross-thread collection access).
- Introduced IProjectsStorageService + ProjectsStorageService as the abstraction for encrypted local project storage.
- ProjectsPage supports creating projects via dialog (code + name) wired to ProjectsViewModel.CreateAndAddProjectAsync through the ProjectRegistry async pipeline.
- Double-clicking a project row reuses the same “Open project” flow as the button and navigates to ProjectDetailPage with dispatcher-safe calls.
- Status: F7 completed and stable; future collection changes must keep the UI-thread marshalling pattern.

### F8 - Projects CRUD on dispatcher-safe infra

- Projects page now supports full CRUD on top of the F7 dispatcher-safe VM/state layer.
- Async projects loading via ProjectsViewModel.LoadProjectsAsync + ProjectRegistry.LoadFromStorageAsync with UI-thread marshalling.
- “Založit projekt” dialog (code + name) with validation, routed through CreateAndAddProjectAsync.
- “Editovat projekt” dialog with pre-filled values, non-empty + unique code validation, and ProjectInfo update via ProjectRegistry.
- “Smazat projekt” confirmation dialog with dispatcher-safe removal and persistence.
- Double-click on a project row opens it through the existing navigation pipeline (Projects → ProjectDetailPage).
- UX polish: compact two-row list item layout (code + name, status); Edit/Delete auto-enabled/disabled based on selection.
- Builds on F7-Final (dispatcher-safe PropertyChanged + ObservableCollection changes); COMException 0x8001010E fixes remain intact.


### F9 - Project detail (ProjectDetailPage)

- Refactored bindings to use ProjectDetailPageViewModel properties instead of AppState.CurrentProject directly (ProjectDisplayName, ProjectCode, ProjectName, StatusText, Description, CreatedAtText, UpdatedAtText).
- Header rebuilt as a two-column grid: left shows project title + short status; right stacks created/updated timestamps with the existing secondary "Zpět" button.
- Introduced three content sections: read-only "Základní informace"; a placeholder "Stavební objekty" border card ready for a future BuildingObject list; and text-only placeholders for "Výpočty a souhrny" plus "Poznámky a dokumenty" for future calculations/reports/diary and documents.
- Page remains read-only but is fully wired via the viewmodel/AppState and ready for future CRUD over building objects and calculation features.
### G-series - ProjectDetail/BuildingObjects (current)

- G1-G3: ProjectDetailPage binds the project header (name/code/status/description + created/updated timestamps) and the BuildingObjects list to ProjectDetailPageViewModel backed by AppState.CurrentProject; selection drives the right-hand detail panel and button enablement via HasBuildingObjects/HasNoBuildingObjects and command CanExecute.
- G4: AddBuildingObjectCommand creates a new BuildingObjectInfo (default name "Nový stavební objekt", Status = Draft, HasNaip = false), attaches it to the current project's collection, and selects it so the detail panel shows the new item.
- G5: DeleteBuildingObjectCommand removes the selected building object from the current project's collection (and VM list if different), then selects the next item, previous, or clears selection when empty; buttons auto-disable when nothing is selected.
- EditBuildingObjectCommand is scaffolded but the real edit UX is still TODO (planned for G6). UX note: after creating a new object, focus shifts to the detail panel and the project context feels less visible; this will be refined alongside the edit UI in G6.
