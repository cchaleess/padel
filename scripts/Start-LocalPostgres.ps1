param(
    [string]$BinariesPath = (Join-Path $PSScriptRoot '../.local/pgsql'),
    [ValidateRange(1024, 65535)]
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'
$workspacePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localPath = Join-Path $workspacePath '.local'
$dataPath = Join-Path $localPath 'postgres-data'
$binPath = Join-Path ([IO.Path]::GetFullPath($BinariesPath)) 'bin'
$pgCtl = Join-Path $binPath 'pg_ctl.exe'

if (-not (Test-Path -LiteralPath $pgCtl)) {
    throw "No se encuentran los binarios PostgreSQL en $binPath. Consulta README.md."
}

New-Item -ItemType Directory -Force -Path $localPath | Out-Null

if (-not (Test-Path -LiteralPath (Join-Path $dataPath 'PG_VERSION'))) {
    $passwordPath = Join-Path $localPath 'postgres-init-password.txt'
    try {
        Set-Content -LiteralPath $passwordPath -Value 'padelmatch_local_only' -Encoding ASCII
        & (Join-Path $binPath 'initdb.exe') -D $dataPath -U padelmatch --auth=scram-sha-256 --encoding=UTF8 --locale=C --pwfile=$passwordPath
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo inicializar PostgreSQL.' }
    }
    finally {
        if (Test-Path -LiteralPath $passwordPath) { Remove-Item -LiteralPath $passwordPath }
    }
}

& $pgCtl status -D $dataPath
if ($LASTEXITCODE -ne 0) {
    & $pgCtl start -D $dataPath -l (Join-Path $localPath 'postgres.log') -o "-h 127.0.0.1 -p $Port" -w
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo arrancar PostgreSQL. Revisa .local/postgres.log y el puerto.' }
}

$previousPassword = $env:PGPASSWORD
try {
    $env:PGPASSWORD = 'padelmatch_local_only'
    $exists = & (Join-Path $binPath 'psql.exe') -h 127.0.0.1 -p $Port -U padelmatch -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'padelmatch'"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar PostgreSQL.' }
    if ($exists -ne '1') {
        & (Join-Path $binPath 'createdb.exe') -h 127.0.0.1 -p $Port -U padelmatch padelmatch
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear la base padelmatch.' }
    }
}
finally {
    $env:PGPASSWORD = $previousPassword
}

Write-Output "PostgreSQL disponible en 127.0.0.1:$Port, base padelmatch. Datos: $dataPath"
