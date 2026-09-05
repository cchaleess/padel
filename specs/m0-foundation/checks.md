- `dotnet restore PadelMatch.slnx --disable-parallel`
- `dotnet tool restore`
- `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`
- `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`
- `dotnet ef migrations has-pending-model-changes --project backend/PadelMatch.Infrastructure --startup-project backend/PadelMatch.Api --configuration Release --no-build -- --environment Development`
- `npm ci --no-audit --no-fund`
  cwd: mobile
- `npm run typecheck`
  cwd: mobile
- `npx expo install --check`
  cwd: mobile
- `npx --yes expo-doctor`
  cwd: mobile
- `npm run export -- --max-workers 2`
  cwd: mobile
