- `dotnet restore PadelMatch.slnx --disable-parallel`
- `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`
- `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1 --filter FullyQualifiedName~DevSession`
- `npm run typecheck`
  cwd: mobile
- `powershell -NoProfile -Command "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'scripts/dev-sim.ps1'), [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 }"`
