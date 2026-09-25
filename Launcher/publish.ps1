# Publication d'une version de Deathless pour le launcher.
#
#   .\Launcher\publish.ps1 -BuildDir "Builds\Deathless-v1-2026-09-25" -Version 1 -Nom "0.1" `
#       -Remote "root@srv617344.hstgr.cloud:/var/www/deathless/" -BaseUrl "http://srv617344.hstgr.cloud/deathless/"
#
# 1. zippe le dossier de build (contenu à la racine du zip) en deathless-v<Version>.zip ;
# 2. calcule son empreinte SHA-256 et sa taille, écrit version.json ;
# 3. vérifie Launcher\changelog.json (ou -Changelog) et le copie à côté ;
# 4. si -AvecLauncher est donné, zippe aussi le launcher construit (bin\Release\net48) en DeathlessLauncher.zip ;
# 5. envoie le tout par scp (clé SSH : aucun mot de passe n'est demandé ni stocké ici), version.json en dernier ;
# 6. si -BaseUrl est donné, relit version.json et changelog.json en HTTP pour vérifier la publication.
# Sans -Remote, rien ne part : les fichiers restent dans Builds\publish\ (hors dépôt), pour contrôle.
param(
    [Parameter(Mandatory = $true)] [string] $BuildDir,
    [Parameter(Mandatory = $true)] [string] $Version,
    [string] $Nom = "",
    [string] $Notes = "",
    [string] $Changelog = (Join-Path $PSScriptRoot "changelog.json"),
    [switch] $AvecLauncher,
    [string] $Remote = "",
    [string] $BaseUrl = ""
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path (Join-Path $BuildDir "Deathless.exe"))) {
    throw "Deathless.exe introuvable dans $BuildDir : donnez le dossier du build Windows."
}

$outDir = Join-Path (Split-Path -Parent $PSScriptRoot) "Builds\publish"
New-Item -ItemType Directory -Force $outDir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-Zip([string] $source, [string] $zipPath) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $full = (Resolve-Path $source).Path.TrimEnd("\")
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -Path $full -Recurse -File | Where-Object { $_.Extension -ne ".log" -and $_.Name -ne "installed.json" -and $_.FullName -notmatch "DoNotShip" } | ForEach-Object {
            $relative = $_.FullName.Substring($full.Length + 1).Replace("\", "/")
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally {
        $zip.Dispose()
    }
}

# --- Le jeu ---
$zipName = "deathless-v$Version.zip"
$zipPath = Join-Path $outDir $zipName
Write-Host "Compression de $BuildDir -> $zipPath"
# Le contenu du dossier est à la racine du zip (Deathless.exe, Deathless_Data\...). Les logs éventuels sont exclus.
New-Zip $BuildDir $zipPath

$hash = (Get-FileHash -Algorithm SHA256 $zipPath).Hash.ToLowerInvariant()
$size = (Get-Item $zipPath).Length
$manifest = [ordered]@{ version = "$Version"; nom = $Nom; zip = $zipName; sha256 = $hash; size = $size; notes = $Notes } | ConvertTo-Json
$manifestPath = Join-Path $outDir "version.json"
[System.IO.File]::WriteAllText($manifestPath, $manifest + "`n", $utf8)
Write-Host "version.json :"
Write-Host $manifest

# --- Notes de version ---
$changelogPath = ""
if ($Changelog -ne "" -and (Test-Path $Changelog)) {
    $text = [System.IO.File]::ReadAllText((Resolve-Path $Changelog).Path, [System.Text.Encoding]::UTF8)
    $parsed = $text | ConvertFrom-Json
    if ($null -eq $parsed.versions) { throw "$Changelog : liste « versions » introuvable." }
    foreach ($v in $parsed.versions) {
        if ("$($v.version)" -eq "") { throw "$Changelog : une entrée n'a pas de « version »." }
        if ("$($v.date)" -ne "" -and "$($v.date)" -notmatch '^\d{4}-\d{2}-\d{2}$') { throw "$Changelog : date « $($v.date) » attendue au format AAAA-MM-JJ." }
    }
    $shown = if ($Nom -ne "") { $Nom } else { "$Version" }
    if (-not ($parsed.versions | Where-Object { "$($_.version)" -eq $shown })) {
        Write-Warning "changelog.json n'a pas d'entrée pour la version $shown : le launcher affichera les notes de version.json."
    }
    $changelogPath = Join-Path $outDir "changelog.json"
    [System.IO.File]::WriteAllText($changelogPath, $text, $utf8)
    Write-Host "changelog.json : $($parsed.versions.Count) version(s)"
} else {
    Write-Warning "Pas de changelog.json ($Changelog) : le launcher affichera les notes de version.json."
}

# --- Le launcher (facultatif) ---
$launcherZip = ""
if ($AvecLauncher) {
    $launcherDir = Join-Path $PSScriptRoot "bin\Release\net48"
    if (-not (Test-Path (Join-Path $launcherDir "DeathlessLauncher.exe"))) { throw "Launcher non construit : dotnet build .\Launcher\DeathlessLauncher.csproj -c Release" }
    $launcherZip = Join-Path $outDir "DeathlessLauncher.zip"
    New-Zip $launcherDir $launcherZip
    Write-Host "Launcher : $launcherZip"
}

# --- Envoi : zip, changelog, launcher, puis version.json en dernier ---
if ($Remote -ne "") {
    Write-Host "Envoi vers $Remote"
    foreach ($file in @($zipPath, $changelogPath, $launcherZip, $manifestPath)) {
        if ($file -eq "") { continue }
        & scp $file "$Remote"
        if ($LASTEXITCODE -ne 0) { throw "scp a echoue pour $file." }
    }
    Write-Host "Publie."
} else {
    Write-Host "Aucun -Remote : rien n'a ete envoye (fichiers dans $outDir)."
}

if ($BaseUrl -ne "") {
    if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }
    $online = Invoke-RestMethod -Uri ($BaseUrl + "version.json?t=" + [DateTime]::UtcNow.Ticks)
    if ("$($online.version)" -eq "$Version" -and "$($online.sha256)" -eq $hash) {
        Write-Host "Verification en ligne : OK (version $Version)."
    } else {
        Write-Warning "Le version.json en ligne ne correspond pas (version $($online.version), sha $($online.sha256))."
    }
    if ($changelogPath -ne "") {
        try {
            $notesOnline = Invoke-RestMethod -Uri ($BaseUrl + "changelog.json?t=" + [DateTime]::UtcNow.Ticks)
            Write-Host "changelog.json en ligne : $($notesOnline.versions.Count) version(s)."
        } catch {
            Write-Warning "changelog.json illisible en ligne : $($_.Exception.Message)"
        }
    }
}
