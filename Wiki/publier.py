"""Publie la version joueur du wiki sur le serveur : génère le wiki, zippe Wiki/public, l'envoie par scp et l'échange
d'un coup avec /var/www/deathless/wiki/ (même procédure que Launcher/publish.ps1 -AvecWiki), puis vérifie en HTTP.
Prérequis : clé SSH autorisée sur root@srv617344.hstgr.cloud (aucun mot de passe ici).   Usage : python Wiki/publier.py"""
import os, subprocess, sys, tempfile, urllib.request, zipfile

ICI = os.path.dirname(os.path.abspath(__file__))
SERVEUR, WEB, URL = "root@srv617344.hstgr.cloud", "/var/www/deathless", "http://srv617344.hstgr.cloud/deathless/wiki/"

subprocess.run([sys.executable, os.path.join(ICI, "build.py")], check=True)
public = os.path.join(ICI, "public")
zip_path = os.path.join(tempfile.gettempdir(), "deathless-wiki.zip")
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
    for r, _, fs in os.walk(public):
        for f in fs:
            p = os.path.join(r, f)
            z.write(p, os.path.relpath(p, public).replace(os.sep, "/"))
subprocess.run(["scp", "-q", zip_path, SERVEUR + ":" + WEB + "/deathless-wiki.zip"], check=True)
subprocess.run(["ssh", "-o", "BatchMode=yes", SERVEUR,
                "cd '%s' && rm -rf wiki.nouveau wiki.ancien && python3 -m zipfile -e deathless-wiki.zip wiki.nouveau && "
                "chmod -R a+rX wiki.nouveau && (if [ -d wiki ]; then mv wiki wiki.ancien; fi) && mv wiki.nouveau wiki && "
                "rm -rf wiki.ancien deathless-wiki.zip" % WEB], check=True)
os.remove(zip_path)
with urllib.request.urlopen(URL) as r:
    print("Wiki en ligne : HTTP %d, %d octets" % (r.status, len(r.read())))
