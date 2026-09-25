# Publication d'une version de Deathless pour le launcher.
#
#   .\Launcher\publish.ps1 -BuildDir "Builds\Deathless-v1-2026-09-25" -Version 1 -Nom "0.1" `
#       -Remote "root@srv617344.hstgr.cloud:/var/www/deathless/" -BaseUrl "http://srv617344.hstgr.cloud/deathless/"
#
# 1. zippe le dossier de build (contenu à la racine du zip) en deathless-v<Version>.zip ;
# 2. calcule son empreinte SHA-256 et sa taille, écrit version.json ;
# 3. vérifie Launcher\changelog.json (ou -Changelog) et le copie à côté ;
# 4. si -AvecLauncher est donné, zippe aussi le launcher construit (bin\Release\net48) en DeathlessLauncher.zip, avec le
#    fond animé Assets\Screenshots\menu_nuit_boucle.mp4 copié en fond.mp4 s'il existe, et son image 0 (fond0.png,
#    extraite par le launcher lui-même : l'image fixe de départ, identique à la première image de la vidéo) ;
# 4 bis. si -AvecWiki est donné, régénère le wiki (python Wiki\build.py) et zippe sa version joueur (Wiki\public) en
#    deathless-wiki.zip ; sur le serveur, il est décompressé dans wiki.nouveau puis échangé avec wiki/ ;
# 5. envoie le tout par scp (clé SSH : aucun mot de passe n'est demandé ni stocké ici) ; sur le serveur, make_release.py
#    fabrique v<N>/ (un fichier gzip par fichier du build et fichiers.json : mise à jour incrémentielle), écrit
#    changelog.json puis version.json en dernier, et supprime les versions plus anciennes que la précédente ;
# 6. si -BaseUrl est donné, relit version.json et changelog.json en HTTP pour vérifier la publication.
# Sans -Remote, rien ne part : les fichiers restent dans Builds\publish\ (hors dépôt), pour contrôle.
param(
    [string] $BuildDir = "",
    [string] $Version = "",
    [string] $Nom = "",
    [string] $Notes = "",
    [string] $Changelog = (Join-Path $PSScriptRoot "changelog.json"),
    [switch] $AvecLauncher,
    [switch] $AvecWiki,
    [switch] $LauncherSeul,
    [string] $Remote = "",
    [string] $BaseUrl = ""
)

$ErrorActionPreference = "Stop"
# -LauncherSeul : seul DeathlessLauncher.zip est refait et envoyé ; version.json, le zip du jeu, v<N>/ et changelog.json
# en ligne ne changent pas (les joueurs à jour n'ont rien à retélécharger).
if ($LauncherSeul) {
    $AvecLauncher = $true
} else {
    if ($BuildDir -eq "" -or $Version -eq "") { throw "-BuildDir et -Version sont obligatoires (sauf avec -LauncherSeul)." }
    if (-not (Test-Path (Join-Path $BuildDir "Deathless.exe"))) {
        throw "Deathless.exe introuvable dans $BuildDir : donnez le dossier du build Windows."
    }
}

$outDir = Join-Path (Split-Path -Parent $PSScriptRoot) "Builds\publish"
New-Item -ItemType Directory -Force $outDir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# $exclure : chemins relatifs (avec /) à laisser hors du zip ; un chemin finissant par / exclut tout le dossier.
function New-Zip([string] $source, [string] $zipPath, [string[]] $exclure = @()) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $full = (Resolve-Path $source).Path.TrimEnd("\")
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -Path $full -Recurse -File | Where-Object { $_.Extension -ne ".log" -and $_.Name -ne "installed.json" -and $_.FullName -notmatch "DoNotShip" } | ForEach-Object {
            $relative = $_.FullName.Substring($full.Length + 1).Replace("\", "/")
            foreach ($x in $exclure) { if ($relative -eq $x -or ($x.EndsWith("/") -and $relative.StartsWith($x))) { return } }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally {
        $zip.Dispose()
    }
}

if (-not $LauncherSeul) {
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
}

# --- Le launcher (facultatif) ---
$launcherZip = ""
if ($AvecLauncher) {
    $launcherDir = Join-Path $PSScriptRoot "bin\Release\net48"
    if (-not (Test-Path (Join-Path $launcherDir "DeathlessLauncher.exe"))) { throw "Launcher non construit : dotnet build .\Launcher\DeathlessLauncher.csproj -c Release" }
    $launcherZip = Join-Path $outDir "DeathlessLauncher.zip"
    # Ce qu'un lancement depuis bin\ a pu laisser à côté de l'exe ne part pas chez les joueurs.
    New-Zip $launcherDir $launcherZip @("changelog.cache.json", "Game/", "Game.nouveau/", "fond.mp4", "fond0.png", "fond.png", "fond.jpg")
    # Fond animé : toujours repris de la vidéo du dépôt (un fond.mp4 resté dans bin\ pourrait être ancien).
    $video = Join-Path (Split-Path -Parent $PSScriptRoot) "Assets\Screenshots\menu_nuit_boucle.mp4"
    $zip = [System.IO.Compression.ZipFile]::Open($launcherZip, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $old = $zip.GetEntry("fond.mp4")
        if ($null -ne $old) { $old.Delete() }
        if (Test-Path $video) {
            # Déjà compressée : stockée telle quelle.
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $video, "fond.mp4", [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
            Write-Host "Fond anime : $video -> fond.mp4 ($([int]((Get-Item $video).Length / 1MB)) Mo)"
            # Image fixe de départ : l'image 0 de cette vidéo, extraite par le launcher construit (sans fenêtre).
            $image0 = Join-Path $outDir "fond0.png"
            $exe = Join-Path $launcherDir "DeathlessLauncher.exe"
            $p = Start-Process -FilePath $exe -ArgumentList @("--image0", "`"$video`"", "`"$image0`"") -Wait -PassThru -NoNewWindow
            if ($p.ExitCode -ne 0 -or -not (Test-Path $image0)) { throw "Extraction de l'image 0 de la video echouee." }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $image0, "fond0.png", [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
            Write-Host "Image 0 de la video -> fond0.png"
        } else {
            Write-Warning "Pas de $video : le launcher gardera l'image de fond fixe."
        }
    } finally {
        $zip.Dispose()
    }
    Write-Host "Launcher : $launcherZip"
}

# --- Le wiki, version joueur (facultatif) ---
$wikiZip = ""
if ($AvecWiki) {
    $racine = Split-Path -Parent $PSScriptRoot
    & python (Join-Path $racine "Wiki\build.py")
    if ($LASTEXITCODE -ne 0) { throw "Generation du wiki echouee." }
    $public = Join-Path $racine "Wiki\public"
    if (-not (Test-Path (Join-Path $public "index.html"))) { throw "Wiki\public\index.html introuvable apres la generation." }
    $wikiZip = Join-Path $outDir "deathless-wiki.zip"
    New-Zip $public $wikiZip
    Write-Host "Wiki joueur : $wikiZip ($((Get-ChildItem $public -Recurse -File).Count) fichiers)"
}

# --- Launcher seul : envoi du seul DeathlessLauncher.zip, puis contrôle en ligne ---
if ($LauncherSeul) {
    if ($Remote -ne "") {
        & scp $launcherZip "$Remote"
        if ($LASTEXITCODE -ne 0) { throw "scp a echoue pour $launcherZip." }
        Write-Host "Launcher seul publie."
    } else {
        Write-Host "Aucun -Remote : rien n'a ete envoye ($launcherZip)."
    }
    if ($BaseUrl -ne "") {
        if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }
        $enLigne = Invoke-WebRequest -UseBasicParsing -Method Head -Uri ($BaseUrl + "DeathlessLauncher.zip")
        $local = (Get-Item $launcherZip).Length
        if ("$($enLigne.Headers['Content-Length'])" -eq "$local") { Write-Host "Verification en ligne : DeathlessLauncher.zip, $local octets, OK." }
        else { Write-Warning "DeathlessLauncher.zip en ligne : $($enLigne.Headers['Content-Length']) octets au lieu de $local." }
    }
    return
}

# --- Envoi : zip, launcher, wiki ; puis, sur le serveur, make_release.py fabrique v<N>/ (fichiers gzip et
#     fichiers.json, pour la mise à jour incrémentielle), écrit changelog.json puis version.json en dernier, et ne garde
#     que la version publiée et la précédente. Sans -Remote, le même script tourne en local, dans Builds\publish. ---
$safeNotes = $Notes.Replace("'", "'\''")
$safeNom = $Nom.Replace("'", "'\''")
if ($Remote -ne "") {
    Write-Host "Envoi vers $Remote"
    $i = $Remote.IndexOf(":")
    $hote = $Remote.Substring(0, $i)
    $web = $Remote.Substring($i + 1).TrimEnd("/")
    foreach ($file in @($zipPath, $launcherZip, $wikiZip)) {
        if ($file -eq "") { continue }
        & scp $file "$Remote"
        if ($LASTEXITCODE -ne 0) { throw "scp a echoue pour $file." }
    }
    & scp (Join-Path $PSScriptRoot "make_release.py") "${hote}:/root/make_release.py"
    if ($LASTEXITCODE -ne 0) { throw "scp a echoue pour make_release.py." }
    $optionChangelog = ""
    if ($changelogPath -ne "") {
        & scp $changelogPath "${hote}:/root/deathless-changelog.json"
        if ($LASTEXITCODE -ne 0) { throw "scp a echoue pour changelog.json." }
        $optionChangelog = " --changelog /root/deathless-changelog.json"
    }
    if ($wikiZip -ne "") {
        # Le wiki est décompressé à côté puis échangé d'un coup (python3, rien à installer).
        $commande = "cd '$web' && rm -rf wiki.nouveau wiki.ancien && python3 -m zipfile -e deathless-wiki.zip wiki.nouveau && chmod -R a+rX wiki.nouveau && (if [ -d wiki ]; then mv wiki wiki.ancien; fi) && mv wiki.nouveau wiki && rm -rf wiki.ancien deathless-wiki.zip"
        & ssh $hote $commande
        if ($LASTEXITCODE -ne 0) { throw "Installation du wiki sur le serveur echouee." }
        Write-Host "Wiki installe dans $web/wiki/"
    }
    Write-Host "Serveur : fichiers de la version, changelog.json, version.json, menage"
    & ssh $hote "python3 /root/make_release.py --zip '$web/$zipName' '$web' '$Version' '$safeNotes' --nom '$safeNom'$optionChangelog"
    if ($LASTEXITCODE -ne 0) { throw "make_release.py a echoue sur le serveur." }
    Write-Host "Publie."
} else {
    $arguments = @((Join-Path $PSScriptRoot "make_release.py"), "--zip", $zipPath, $outDir, "$Version", $Notes, "--nom", $Nom)
    if ($changelogPath -ne "") { $arguments += @("--changelog", $changelogPath) }
    & python @arguments
    if ($LASTEXITCODE -ne 0) { throw "make_release.py a echoue." }
    Write-Host "Aucun -Remote : rien n'a ete envoye ; dossier web complet dans $outDir (servable en local pour tester)."
}

if ($BaseUrl -ne "") {
    if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }
    $online = Invoke-RestMethod -Uri ($BaseUrl + "version.json?t=" + [DateTime]::UtcNow.Ticks)
    if ("$($online.version)" -eq "$Version" -and "$($online.sha256)" -eq $hash) {
        Write-Host "Verification en ligne : OK (version $Version)."
    } else {
        Write-Warning "Le version.json en ligne ne correspond pas (version $($online.version), sha $($online.sha256))."
    }
    try {
        $fichiers = Invoke-RestMethod -Uri ($BaseUrl + "$($online.dossier)fichiers.json?t=" + [DateTime]::UtcNow.Ticks)
        Write-Host "Mise a jour incrementielle en ligne : $($online.dossier)fichiers.json, $($fichiers.fichiers.Count) fichiers."
    } catch {
        Write-Warning "fichiers.json illisible en ligne : $($_.Exception.Message) (le launcher prendra le zip)."
    }
    if ($wikiZip -ne "") {
        try {
            $page = Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl + "wiki/")
            Write-Host "Wiki en ligne : HTTP $($page.StatusCode), $($page.Content.Length) caracteres."
        } catch {
            Write-Warning "Wiki illisible en ligne : $($_.Exception.Message)"
        }
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
