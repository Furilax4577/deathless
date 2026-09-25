"""Publie la version joueur du wiki sur le serveur : génère le wiki, zippe Wiki/public, l'envoie par scp et l'échange
d'un coup avec /var/www/deathless/wiki/ (même procédure que Launcher/publish.ps1 -AvecWiki), puis vérifie en HTTP.
Avec --dev, publie aussi la version développeur (Wiki/site, sons compris) dans /var/www/deathless/wiki-dev/ (pages en
noindex). Prérequis : clé SSH autorisée sur root@srv617344.hstgr.cloud.   Usage : python Wiki/publier.py [--dev]"""
import os, subprocess, sys, tempfile, urllib.request, zipfile

ICI = os.path.dirname(os.path.abspath(__file__))
SERVEUR, WEB, URL = "root@srv617344.hstgr.cloud", "/var/www/deathless", "http://srv617344.hstgr.cloud/deathless/wiki/"

subprocess.run([sys.executable, os.path.join(ICI, "build.py")], check=True)
def publier(source, dossier):
    zip_path = os.path.join(tempfile.gettempdir(), "deathless-%s.zip" % dossier)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        for r, _, fs in os.walk(source):
            for f in fs:
                p = os.path.join(r, f)
                z.write(p, os.path.relpath(p, source).replace(os.sep, "/"))
    subprocess.run(["scp", "-q", zip_path, SERVEUR + ":" + WEB + "/%s.zip" % dossier], check=True)
    subprocess.run(["ssh", "-o", "BatchMode=yes", SERVEUR,
                    "cd '{w}' && rm -rf {d}.nouveau {d}.ancien && python3 -m zipfile -e {d}.zip {d}.nouveau && "
                    "chmod -R a+rX {d}.nouveau && (if [ -d {d} ]; then mv {d} {d}.ancien; fi) && mv {d}.nouveau {d} && "
                    "rm -rf {d}.ancien {d}.zip".format(w=WEB, d=dossier)], check=True)
    os.remove(zip_path)
    with urllib.request.urlopen(URL.replace("/wiki/", "/%s/" % dossier)) as r:
        print("%s en ligne : HTTP %d, %d octets" % (dossier, r.status, len(r.read())))


publier(os.path.join(ICI, "public"), "wiki")
if "--dev" in sys.argv:
    publier(os.path.join(ICI, "site"), "wiki-dev")
