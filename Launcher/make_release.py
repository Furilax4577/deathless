"""Côté serveur (VPS), appelé par Launcher/publish-diff.ps1 après l'envoi différentiel (rsync) du build.

Zippe le dossier de build décompressé tenu à jour sur le serveur (contenu à la racine du zip, logs et dossiers
« DoNotShip » exclus, comme publish.ps1) en deathless-v<version>.zip, calcule l'empreinte SHA-256 et la taille, publie
changelog.json s'il est donné, puis écrit version.json. Le launcher ne voit aucune différence avec une publication
complète.

Usage : python3 make_release.py <dossier_build> <dossier_web> <version> <notes> [--nom 0.1] [--changelog fichier.json]

Ordre d'écriture : zip, puis changelog.json, puis version.json en dernier, chacun de façon atomique (fichier .part
renommé) : le launcher ne voit jamais une version dont le zip manque.
"""
import hashlib
import json
import os
import shutil
import sys
import zipfile

EXE = "Deathless.exe"


def atomic_json(data, path):
    part = path + ".part"
    with open(part, "w", encoding="utf-8") as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    os.replace(part, path)


def main():
    args = sys.argv[1:]
    options = {}
    for key in ("--nom", "--changelog"):
        if key in args:
            i = args.index(key)
            options[key] = args[i + 1]
            del args[i:i + 2]
    if len(args) != 4:
        sys.exit(__doc__)
    build, web, version, notes = args
    if not os.path.isfile(os.path.join(build, EXE)):
        sys.exit("%s introuvable dans %s : donnez le dossier du build Windows." % (EXE, build))

    changelog = None
    if "--changelog" in options:
        with open(options["--changelog"], encoding="utf-8-sig") as handle:
            changelog = json.load(handle)
        if not isinstance(changelog.get("versions"), list):
            sys.exit("changelog.json : liste « versions » introuvable.")

    os.makedirs(web, exist_ok=True)
    name = "deathless-v%s.zip" % version
    temp = os.path.join(web, name + ".part")
    with zipfile.ZipFile(temp, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for root, dirs, files in os.walk(build):
            dirs[:] = sorted(d for d in dirs if "DoNotShip" not in d)
            for f in sorted(files):
                if f.endswith(".log") or f == "installed.json":
                    continue
                full = os.path.join(root, f)
                z.write(full, os.path.relpath(full, build).replace(os.sep, "/"))
    digest = hashlib.sha256()
    with open(temp, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    size = os.path.getsize(temp)
    os.replace(temp, os.path.join(web, name))

    written = [os.path.join(web, name)]
    if changelog is not None:
        atomic_json(changelog, os.path.join(web, "changelog.json"))
        written.append(os.path.join(web, "changelog.json"))

    manifest = {"version": version, "nom": options.get("--nom", ""), "zip": name, "sha256": digest.hexdigest(),
                "size": size, "notes": notes}
    atomic_json(manifest, os.path.join(web, "version.json"))
    written.append(os.path.join(web, "version.json"))
    for path in written:
        os.chmod(path, 0o644)
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
