- `npm run typecheck`
  cwd: mobile
- `npx expo-doctor`
  cwd: mobile
- `dotnet restore PadelMatch.slnx --disable-parallel`
- `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`
- `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`
- `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`
- `powershell -NoProfile -Command "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 }"`
