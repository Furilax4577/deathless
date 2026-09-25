# Publication différentielle d'une version de Deathless (connexion lente, ex. Starlink).
#
#   .\Launcher\publish-diff.ps1 -BuildDir "Builds\Deathless-v2-2026-10-02" -Version 2 -Nom "0.2"
#
# Au lieu d'envoyer tout le zip comme publish.ps1 :
# 1. rsync (lancé dans WSL) met à jour sur le serveur une copie décompressée du build (/root/deathless-staging/current) :
#    seuls les fichiers modifiés partent, et pour chacun seulement les blocs qui ont changé ;
# 2. changelog.json (Launcher\changelog.json par défaut) est envoyé à côté ;
# 3. le serveur refait le zip, l'empreinte SHA-256, publie changelog.json puis version.json (Launcher/make_release.py,
#    copié en /root/) ;
# 4. version.json est relu en HTTP pour vérifier.
# Le launcher ne voit aucune différence. publish.ps1 reste la voie complète (premier envoi, secours).
# Prérequis : WSL avec rsync et une clé SSH autorisée sur root.
param(
    [Parameter(Mandatory = $true)] [string] $BuildDir,
    [Parameter(Mandatory = $true)] [string] $Version,
    [string] $Nom = "",
    [string] $Notes = "",
    [string] $Changelog = (Join-Path $PSScriptRoot "changelog.json"),
    [string] $Server = "srv617344.hstgr.cloud",
    [string] $Staging = "/root/deathless-staging/current/",
    [string] $Web = "/var/www/deathless",
    [string] $BaseUrl = "http://srv617344.hstgr.cloud/deathless/"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path (Join-Path $BuildDir "Deathless.exe"))) {
    throw "Deathless.exe introuvable dans $BuildDir : donnez le dossier du build Windows."
}

function To-Wsl([string] $path) {
    # C:\Dev\... -> /mnt/c/Dev/...
    $full = (Resolve-Path $path).Path
    return "/mnt/" + $full.Substring(0, 1).ToLower() + $full.Substring(2).Replace("\", "/")
}

# Chemin WSL du build, avec la barre finale : rsync copie le CONTENU du dossier.
$wslPath = (To-Wsl $BuildDir).TrimEnd("/") + "/"

# Le script serveur est recopié à chaque fois (petit) : il reste à jour avec le dépôt.
& wsl -e rsync -az (To-Wsl (Join-Path $PSScriptRoot "make_release.py")) "root@${Server}:/root/make_release.py"
if ($LASTEXITCODE -ne 0) { throw "Envoi de make_release.py echoue." }

$changelogArg = ""
if ($Changelog -ne "" -and (Test-Path $Changelog)) {
    $parsed = [System.IO.File]::ReadAllText((Resolve-Path $Changelog).Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    if ($null -eq $parsed.versions) { throw "$Changelog : liste « versions » introuvable." }
    & wsl -e rsync -z (To-Wsl $Changelog) "root@${Server}:/root/deathless-changelog.json"
    if ($LASTEXITCODE -ne 0) { throw "Envoi de changelog.json echoue." }
    $changelogArg = " --changelog /root/deathless-changelog.json"
} else {
    Write-Warning "Pas de changelog.json ($Changelog) : le launcher affichera les notes de version.json."
}

Write-Host "Envoi differentiel de $BuildDir vers ${Server}:$Staging"
# --checksum : un build refait a des dates neuves partout ; on compare le contenu, pas la date.
& wsl -e rsync -rz --checksum --delete --stats --exclude "*DoNotShip*" --exclude "*.log" --exclude "installed.json" $wslPath "root@${Server}:$Staging"
if ($LASTEXITCODE -ne 0) { throw "rsync a echoue." }

Write-Host "Zip, changelog.json et version.json sur le serveur"
$safeNotes = $Notes.Replace("'", "'\''")
$safeNom = $Nom.Replace("'", "'\''")
& wsl -e ssh "root@$Server" "python3 /root/make_release.py $Staging $Web $Version '$safeNotes' --nom '$safeNom'$changelogArg"
if ($LASTEXITCODE -ne 0) { throw "La creation du zip sur le serveur a echoue." }

if ($BaseUrl -ne "") {
    if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }
    $online = Invoke-RestMethod -Uri ($BaseUrl + "version.json?t=" + [DateTime]::UtcNow.Ticks)
    if ("$($online.version)" -eq "$Version") {
        Write-Host "Verification en ligne : OK (version $Version, $($online.size) octets)."
    } else {
        Write-Warning "Le version.json en ligne annonce la version $($online.version)."
    }
}
