"""Publie le wiki sur le serveur : génère le wiki, envoie la version joueur (Wiki/public) dans /var/www/deathless/wiki/
et, avec --dev, la version développeur (Wiki/site) dans /var/www/deathless/wiki-dev/ (pages en noindex), puis vérifie
en HTTP. Chaque version est préparée à côté (<dossier>.nouveau) puis échangée d'un coup.

Envoi léger : les pages et les icônes partent à chaque fois ; les médias lourds (sons/ et videos/) ne partent que s'ils
ont changé depuis la dernière publication (empreintes gardées dans Wiki/.publication.json, hors dépôt). Sinon, le
serveur reprend ceux déjà en ligne par liens physiques (cp -al), sans les recopier.
Prérequis : clé SSH autorisée sur root@srv617344.hstgr.cloud.   Usage : python Wiki/publier.py [--dev] [--tout]"""
import hashlib, json, os, subprocess, sys, tempfile, urllib.request, zipfile

ICI = os.path.dirname(os.path.abspath(__file__))
SERVEUR, WEB, URL = "root@srv617344.hstgr.cloud", "/var/www/deathless", "http://srv617344.hstgr.cloud/deathless/wiki/"
MEDIAS = ("sons", "videos")
ETAT = os.path.join(ICI, ".publication.json")


def empreinte_medias(source):
    h = hashlib.sha256()
    for m in MEDIAS:
        base = os.path.join(source, m)
        for r, _, fs in sorted(os.walk(base)):
            for f in sorted(fs):
                p = os.path.join(r, f)
                h.update(os.path.relpath(p, source).replace(os.sep, "/").encode())
                h.update(str(os.path.getsize(p)).encode())
    return h.hexdigest()


def publier(source, dossier, etat):
    medias_changes = "--tout" in sys.argv or etat.get(dossier) != empreinte_medias(source)
    zip_path = os.path.join(tempfile.gettempdir(), "deathless-%s.zip" % dossier)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        for r, _, fs in os.walk(source):
            rel = os.path.relpath(r, source).replace(os.sep, "/")
            if not medias_changes and rel.split("/")[0] in MEDIAS:
                continue
            for f in fs:
                p = os.path.join(r, f)
                z.write(p, os.path.relpath(p, source).replace(os.sep, "/"))
    subprocess.run(["scp", "-q", zip_path, SERVEUR + ":" + WEB + "/%s.zip" % dossier], check=True)
    reprise = "" if medias_changes else "".join(
        " && (if [ -d {d}/%s ]; then cp -al {d}/%s {d}.nouveau/%s; fi)" % (m, m, m) for m in MEDIAS)
    subprocess.run(["ssh", "-o", "BatchMode=yes", SERVEUR,
                    ("cd '{w}' && rm -rf {d}.nouveau {d}.ancien && python3 -m zipfile -e {d}.zip {d}.nouveau" + reprise +
                     " && chmod -R a+rX {d}.nouveau && (if [ -d {d} ]; then mv {d} {d}.ancien; fi) && mv {d}.nouveau {d}"
                     " && rm -rf {d}.ancien {d}.zip").format(w=WEB, d=dossier)], check=True)
    taille = os.path.getsize(zip_path)
    os.remove(zip_path)
    etat[dossier] = empreinte_medias(source)
    with urllib.request.urlopen(URL.replace("/wiki/", "/%s/" % dossier)) as r:
        print("%s en ligne : HTTP %d, %d octets (envoi %.1f Mo%s)" % (
            dossier, r.status, len(r.read()), taille / 1048576, ", médias compris" if medias_changes else ", médias repris"))


subprocess.run([sys.executable, os.path.join(ICI, "build.py")], check=True)
etat = json.load(open(ETAT, encoding="utf-8")) if os.path.exists(ETAT) else {}
publier(os.path.join(ICI, "public"), "wiki", etat)
if "--dev" in sys.argv:
    publier(os.path.join(ICI, "site"), "wiki-dev", etat)
json.dump(etat, open(ETAT, "w", encoding="utf-8"), indent=2)
