"""Direction artistique par Grok Imagine (API de xAI) : génère des images à partir des prompts du dépôt.

Usage (depuis la racine du dépôt, avec `python`, jamais `python3`) :
    python Docs/outils/grok_image.py village-jour                  une image du prompt Docs/da/prompts/village-jour.md
    python Docs/outils/grok_image.py village-jour -n 4             quatre variantes
    python Docs/outils/grok_image.py village-jour --format 16:9    autre format (défaut : celui du prompt, sinon 16:9)
    python Docs/outils/grok_image.py --texte "a low-poly tavern…" --nom taverne-essai
    python Docs/outils/grok_image.py village-jour --depuis Docs/references/village-vision-grok-01.webp
                                                                   retouche d'une image existante (le prompt dit quoi changer)
    python Docs/outils/grok_image.py --liste                       prompts disponibles

La clé se lit dans la variable d'environnement XAI_API_KEY (créée sur console.x.ai par Quentin). Elle ne s'écrit
jamais dans le dépôt, ni dans un fichier du projet, ni dans une conversation. Chaque image est facturée.

Un prompt est un fichier Markdown de Docs/da/prompts/ : des lignes d'en-tête facultatives `format: 16:9`,
`style: …` (ajouté en tête du texte envoyé), `eviter: …` (ajouté en fin, « Avoid: … »), une ligne vide, puis le texte
(en anglais de préférence). Les lignes qui commencent par `#` sont des commentaires.

Sorties : Docs/references/<nom>-<AAAAMMJJ>-<NN>.<ext> et une ligne dans Docs/da/journal.md (date, prompt, modèle,
fichiers). Bibliothèque standard seulement.
"""
import argparse
import base64
import datetime
import io
import json
import mimetypes
import os
import sys
import urllib.error
import urllib.request

RACINE = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PROMPTS = os.path.join(RACINE, "Docs", "da", "prompts")
SORTIES = os.path.join(RACINE, "Docs", "references")
JOURNAL = os.path.join(RACINE, "Docs", "da", "journal.md")
API = "https://api.x.ai/v1/images/"
MODELE = "grok-imagine-image-2.0"
N_MAX = 10


def lire_prompt(nom):
    """Renvoie (texte envoyé, format) à partir de Docs/da/prompts/<nom>.md."""
    chemin = os.path.join(PROMPTS, nom + ".md")
    if not os.path.exists(chemin):
        sys.exit("prompt introuvable : " + chemin + " (voir --liste)")
    entete, corps, dans_corps = {}, [], False
    for ligne in io.open(chemin, encoding="utf-8").read().splitlines():
        if ligne.startswith("#"):
            continue
        if not dans_corps:
            if not ligne.strip():
                dans_corps = bool(entete) or dans_corps
                continue
            cle, sep, valeur = ligne.partition(":")
            if sep and cle.strip().lower() in ("format", "style", "eviter"):
                entete[cle.strip().lower()] = valeur.strip()
                continue
            dans_corps = True
        corps.append(ligne)
    texte = " ".join(l.strip() for l in corps if l.strip())
    if entete.get("style"):
        texte = entete["style"].rstrip(". ") + ". " + texte
    if entete.get("eviter"):
        texte = texte.rstrip() + " Avoid: " + entete["eviter"].rstrip(". ") + "."
    return texte, entete.get("format")


def appeler(point, corps, cle):
    requete = urllib.request.Request(API + point, data=json.dumps(corps).encode("utf-8"), method="POST",
                                     headers={"Content-Type": "application/json", "Authorization": "Bearer " + cle})
    try:
        with urllib.request.urlopen(requete, timeout=180) as reponse:
            return json.loads(reponse.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        # Le corps de l'erreur dit pourquoi (modèle, format, crédit) ; la clé n'y figure pas.
        sys.exit("erreur de l'API (" + str(e.code) + ") : " + e.read().decode("utf-8", "replace")[:600])
    except urllib.error.URLError as e:
        sys.exit("API injoignable : " + str(e.reason))


def image_en_donnees(chemin):
    """Image locale en URI de données (l'API accepte une URL publique ou une URI base64)."""
    mime = mimetypes.guess_type(chemin)[0] or "image/png"
    with open(chemin, "rb") as f:
        return "data:" + mime + ";base64," + base64.b64encode(f.read()).decode("ascii")


def enregistrer(donnees, nom):
    """Écrit chaque image renvoyée (URL à télécharger ou base64) dans Docs/references/ ; renvoie les chemins."""
    os.makedirs(SORTIES, exist_ok=True)
    jour = datetime.date.today().strftime("%Y%m%d")
    chemins, rang = [], 1
    for image in donnees.get("data", []):
        if image.get("b64_json"):
            contenu, ext = base64.b64decode(image["b64_json"]), ".png"
        elif image.get("url"):
            with urllib.request.urlopen(image["url"], timeout=180) as r:
                contenu = r.read()
                ext = mimetypes.guess_extension((r.headers.get("Content-Type") or "").split(";")[0]) or ".jpg"
            if ext == ".jpe":
                ext = ".jpg"
        else:
            continue
        while os.path.exists(os.path.join(SORTIES, "%s-%s-%02d%s" % (nom, jour, rang, ext))):
            rang += 1
        chemin = os.path.join(SORTIES, "%s-%s-%02d%s" % (nom, jour, rang, ext))
        with open(chemin, "wb") as f:
            f.write(contenu)
        chemins.append(chemin)
    return chemins


def journaliser(nom, texte, fichiers, depuis):
    os.makedirs(os.path.dirname(JOURNAL), exist_ok=True)
    nouveau = not os.path.exists(JOURNAL)
    with io.open(JOURNAL, "a", encoding="utf-8", newline="\n") as f:
        if nouveau:
            f.write("# Journal des images générées (Grok Imagine)\n\nÉcrit par `Docs/outils/grok_image.py`.\n")
        f.write("\n## %s, %s\n\n" % (datetime.datetime.now().strftime("%d/%m/%Y %H:%M"), nom))
        f.write("- Modèle : `%s`%s\n" % (MODELE, (" ; retouche de `%s`" % os.path.relpath(depuis, RACINE).replace("\\", "/")) if depuis else ""))
        f.write("- Fichiers : " + ", ".join("`%s`" % os.path.relpath(c, RACINE).replace("\\", "/") for c in fichiers) + "\n")
        f.write("- Texte envoyé : " + texte + "\n")


def main():
    p = argparse.ArgumentParser(description="Images de direction artistique par Grok Imagine.")
    p.add_argument("prompt", nargs="?", help="nom d'un fichier de Docs/da/prompts/ (sans .md)")
    p.add_argument("--texte", help="texte envoyé tel quel, à la place d'un fichier de prompt")
    p.add_argument("--nom", help="nom des fichiers de sortie (défaut : le nom du prompt)")
    p.add_argument("-n", type=int, default=1, help="nombre d'images (1 à %d)" % N_MAX)
    p.add_argument("--format", help="format d'image, par exemple 16:9, 1:1, 9:16")
    p.add_argument("--depuis", help="image de départ à retoucher (chemin local)")
    p.add_argument("--liste", action="store_true", help="liste les prompts disponibles")
    a = p.parse_args()

    if a.liste:
        for f in sorted(os.listdir(PROMPTS)) if os.path.isdir(PROMPTS) else []:
            if f.endswith(".md"):
                print(f[:-3])
        return
    if not a.prompt and not a.texte:
        p.error("donner un nom de prompt ou --texte")
    cle = os.environ.get("XAI_API_KEY")
    if not cle:
        sys.exit("XAI_API_KEY absente : crée la clé sur console.x.ai et pose-la dans les variables d'environnement "
                 "de Windows (jamais dans le dépôt).")

    texte, format_prompt = (a.texte, None) if a.texte else lire_prompt(a.prompt)
    nom = a.nom or a.prompt or "essai"
    n = max(1, min(N_MAX, a.n))
    if a.depuis:
        corps = {"model": MODELE, "prompt": texte, "image": {"url": image_en_donnees(a.depuis), "type": "image_url"}}
        reponse = appeler("edits", corps, cle)
    else:
        corps = {"model": MODELE, "prompt": texte, "n": n, "aspect_ratio": a.format or format_prompt or "16:9"}
        reponse = appeler("generations", corps, cle)
    fichiers = enregistrer(reponse, nom)
    if not fichiers:
        sys.exit("aucune image dans la réponse : " + json.dumps(reponse)[:400])
    journaliser(nom, texte, fichiers, a.depuis)
    for c in fichiers:
        print(os.path.relpath(c, RACINE).replace("\\", "/"))


if __name__ == "__main__":
    main()
