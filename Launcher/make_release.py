"""Publication d'une version de Deathless côté serveur (VPS), appelée par Launcher/publish.ps1 et Launcher/publish-diff.ps1
(ou en local, pour fabriquer un dossier web de test).

    python3 make_release.py <dossier_build> <dossier_web> <version> <notes> [--nom 0.4.2] [--changelog fichier.json]
    python3 make_release.py --zip <deathless-vN.zip> <dossier_web> <version> <notes> [--nom …] [--changelog …]

Produit dans <dossier_web> :
  - deathless-v<version>.zip : le build complet, contenu à la racine (logs, installed.json et dossiers « DoNotShip »
    exclus). Avec --zip, le zip donné est repris tel quel (déjà envoyé par publish.ps1 : le jeu ne voyage qu'une fois) ;
  - v<version>/ : chaque fichier du build, compressé en gzip (<chemin>.gz) quand c'est utile, sinon tel quel, et
    v<version>/fichiers.json (chemin, taille, SHA-256, taille compressée) : le launcher n'y prend que les fichiers
    nouveaux ou modifiés ;
  - changelog.json (si --changelog), puis version.json en dernier, avec "dossier": "v<version>/".
Chaque étape est atomique (fichier ou dossier .part renommé) : le launcher ne voit jamais une version incomplète.
Ménage, une fois la publication réussie : seules la version publiée et la précédente (celle de l'ancien version.json)
gardent leur zip et leur dossier ; les autres sont supprimés.
"""
import gzip
import hashlib
import json
import os
import re
import shutil
import sys
import tempfile
import zipfile

EXE = "Deathless.exe"
GAIN_MINIMAL = 0.95  # un fichier n'est compressé que s'il perd au moins 5 %


def exclu(chemin):
    nom = os.path.basename(chemin)
    return nom.endswith(".log") or nom == "installed.json" or "DoNotShip" in chemin


def atomic_json(data, path):
    part = path + ".part"
    with open(part, "w", encoding="utf-8") as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    os.replace(part, path)


def sha256_fichier(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def fichiers_du_build(build):
    for root, dirs, files in os.walk(build):
        dirs[:] = sorted(d for d in dirs if "DoNotShip" not in d)
        for f in sorted(files):
            full = os.path.join(root, f)
            rel = os.path.relpath(full, build).replace(os.sep, "/")
            if not exclu(rel):
                yield full, rel


def faire_zip(build, destination):
    part = destination + ".part"
    with zipfile.ZipFile(part, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for full, rel in fichiers_du_build(build):
            z.write(full, rel)
    os.replace(part, destination)


def faire_dossier(build, web, version, nom):
    """v<version>/ : fichiers gzip (si utile) + fichiers.json. Renvoie le nombre de fichiers et les tailles."""
    final = os.path.join(web, "v%s" % version)
    part = final + ".part"
    shutil.rmtree(part, ignore_errors=True)
    os.makedirs(part)
    liste, brut, servi = [], 0, 0
    for full, rel in fichiers_du_build(build):
        with open(full, "rb") as handle:
            data = handle.read()
        entree = {"chemin": rel, "taille": len(data), "sha256": hashlib.sha256(data).hexdigest()}
        cible = os.path.join(part, *rel.split("/"))
        os.makedirs(os.path.dirname(cible), exist_ok=True)
        compresse = gzip.compress(data, compresslevel=9, mtime=0)
        if len(compresse) < GAIN_MINIMAL * len(data):
            with open(cible + ".gz", "wb") as handle:
                handle.write(compresse)
            entree["gz"] = len(compresse)
            servi += len(compresse)
        else:
            with open(cible, "wb") as handle:
                handle.write(data)
            servi += len(data)
        brut += len(data)
        liste.append(entree)
    atomic_json({"version": version, "nom": nom, "fichiers": liste}, os.path.join(part, "fichiers.json"))
    shutil.rmtree(final, ignore_errors=True)
    os.replace(part, final)
    return len(liste), brut, servi


def version_de(nom_entree):
    # Seulement les noms de publication (deathless-v12.zip, v12/) : un autre dossier (wiki, wiki-dev…) n'est jamais touché.
    m = re.fullmatch(r"deathless-v([0-9][0-9A-Za-z._-]*)\.zip", nom_entree) or re.fullmatch(r"v([0-9][0-9A-Za-z._-]*)", nom_entree)
    return m.group(1) if m else None


def menage(web, garder):
    """Supprime les zips deathless-v*.zip et dossiers v*/ dont la version n'est pas dans garder."""
    supprimes = []
    for entree in sorted(os.listdir(web)):
        chemin = os.path.join(web, entree)
        v = version_de(entree)
        if v is None or v in garder:
            continue
        if entree.endswith(".zip") and os.path.isfile(chemin):
            os.remove(chemin)
            supprimes.append(entree)
        elif os.path.isdir(chemin) and entree.startswith("v") and not entree.endswith(".part"):
            shutil.rmtree(chemin)
            supprimes.append(entree + "/")
    return supprimes


def permissions(web, noms):
    for nom in noms:
        chemin = os.path.join(web, nom)
        if os.path.isdir(chemin):
            for root, dirs, files in os.walk(chemin):
                os.chmod(root, 0o755)
                for f in files:
                    os.chmod(os.path.join(root, f), 0o644)
        elif os.path.exists(chemin):
            os.chmod(chemin, 0o644)


def main():
    args = sys.argv[1:]
    options = {}
    for key in ("--nom", "--changelog", "--zip"):
        if key in args:
            i = args.index(key)
            options[key] = args[i + 1]
            del args[i:i + 2]
    attendus = 3 if "--zip" in options else 4
    if len(args) != attendus:
        sys.exit(__doc__)
    if "--zip" in options:
        build, (web, version, notes) = None, args
    else:
        build, web, version, notes = args
    nom = options.get("--nom", "")

    changelog = None
    if "--changelog" in options:
        with open(options["--changelog"], encoding="utf-8-sig") as handle:
            changelog = json.load(handle)
        if not isinstance(changelog.get("versions"), list):
            sys.exit("changelog.json : liste « versions » introuvable.")

    os.makedirs(web, exist_ok=True)
    precedente = None
    try:
        with open(os.path.join(web, "version.json"), encoding="utf-8-sig") as handle:
            precedente = str(json.load(handle).get("version", "")) or None
    except (OSError, ValueError):
        pass

    name = "deathless-v%s.zip" % version
    zip_path = os.path.join(web, name)
    temporaire = None
    try:
        if "--zip" in options:
            source = os.path.abspath(options["--zip"])
            if source != os.path.abspath(zip_path):
                shutil.copyfile(source, zip_path + ".part")
                os.replace(zip_path + ".part", zip_path)
            temporaire = tempfile.mkdtemp(prefix="deathless-build-")
            with zipfile.ZipFile(zip_path) as z:
                z.extractall(temporaire)
            build = temporaire
        if not os.path.isfile(os.path.join(build, EXE)):
            sys.exit("%s introuvable dans le build." % EXE)
        if "--zip" not in options:
            faire_zip(build, zip_path)
        nombre, brut, servi = faire_dossier(build, web, version, nom)
    finally:
        if temporaire:
            shutil.rmtree(temporaire, ignore_errors=True)

    sha = sha256_fichier(zip_path)
    size = os.path.getsize(zip_path)
    if changelog is not None:
        atomic_json(changelog, os.path.join(web, "changelog.json"))
    manifest = {"version": version, "nom": nom, "zip": name, "sha256": sha, "size": size, "notes": notes,
                "dossier": "v%s/" % version}
    atomic_json(manifest, os.path.join(web, "version.json"))
    permissions(web, [name, "v%s" % version, "version.json", "changelog.json"])

    garder = {version} | ({precedente} if precedente else set())
    supprimes = menage(web, garder)
    print(json.dumps(manifest, ensure_ascii=False, indent=2))
    print("v%s/ : %d fichiers, %.1f Mo bruts, %.1f Mo servis (gzip par fichier) ; zip %.1f Mo"
          % (version, nombre, brut / 1048576, servi / 1048576, size / 1048576))
    print("Gardées : %s ; supprimées : %s" % (", ".join(sorted(garder)), ", ".join(supprimes) if supprimes else "rien"))


if __name__ == "__main__":
    main()
