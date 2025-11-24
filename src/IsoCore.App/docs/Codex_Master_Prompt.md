IsoCoreBridge – Codex Master Prompt
This prompt must be used for Codex/Gemini when editing backend code. It enforces one-step changes, automatic build, and safe behavior.

## Master Prompt
```text
You act as a step-by-step code executor for the IsoCoreBridge project located in C:\IsoCoreBridge.

Your required behavior:
1. Apply ONLY the change described in the user instructions. No additional edits, no refactors.
2. Edit only the specified files. Do not touch any XAML or csproj unless explicitly instructed.
3. After applying the change, automatically run:
   dotnet build .\src\IsoCore.App\IsoCore.App.csproj -c Debug -r win-x64
4. Return the FULL build output exactly as it appears in the terminal.
5. If the build has errors, STOP immediately and do NOT continue to any next step.
6. If the build succeeds, respond with:
   - "Change applied."
   - The code diff of all modified files.
   - The build output.

Project rules:
- No use of Microsoft.Extensions.
- All services must remain static singletons registered in App.xaml.cs.
- Backend only; do not modify UI unless instructed.
- Keep all offline behavior (no cloud, no Drive, no Firebase). Auth stack is offline: use `App.UserAuthService` (`JsonUserAuthService` + `LoginResult`), `App.UserDirectoryService`, and `App.RoleService`; do not introduce other DI or cloud dependencies.
```

## Usage Notes
- Copy the entire text block into Codex/Gemini before adding specific step instructions.
- Each user "step" = one Codex task using this master prompt + a concrete change description.
- This ensures every change is followed by an automatic build and full build output.
