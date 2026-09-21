- `dotnet restore PadelMatch.slnx --disable-parallel`
- `dotnet build PadelMatch.slnx -c Release --no-restore -m:1`
- `dotnet test PadelMatch.slnx -c Release --no-build --no-restore -m:1`
- `npm ci --no-audit --no-fund`
  cwd: mobile
- `npm run typecheck`
  cwd: mobile
- `npx --yes expo-doctor`
  cwd: mobile
