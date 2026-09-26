"""Jauge KayKit : part des assets du jeu qui viennent encore des packs KayKit, par famille. Compte les fichiers sous
Assets/ (hors .meta, captures, éditeur), classés par extension ; est « KayKit » tout fichier sous un dossier dont le nom
contient « KayKit ». Écrit Wiki/data/kaykit.json, lu par build.py pour la balise {jauge-kaykit}. Le cap du projet est de
descendre cette jauge à 0 % (décision de Quentin, 26/09/2026). Usage : python Wiki/jauge_kaykit.py"""
import json, os, datetime

ICI = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(os.path.dirname(ICI), "Assets")
FAMILLES = [
    ("Modèles 3D", {".fbx", ".obj", ".glb", ".gltf"}),
    ("Textures", {".png", ".jpg", ".jpeg", ".tga", ".psd"}),
    ("Animations", {".anim", ".controller"}),
    ("Matériaux et shaders", {".mat", ".shader", ".hlsl", ".shadergraph"}),
    ("Sons", {".wav", ".ogg", ".mp3"}),
]
EXCLUS = ("Screenshots", "/Editor/", "\\Editor\\", "/UI/", "\\UI\\", "/Fonts/", "\\Fonts\\", "/Icones/", "\\Icones\\")


def mesurer():
    comptes = {nom: [0, 0] for nom, _ in FAMILLES}
    for racine, _, fichiers in os.walk(ASSETS):
        chemin = racine.replace("\\", "/") + "/"
        if any(x.replace("\\", "/") in chemin for x in EXCLUS):
            continue
        kaykit = "kaykit" in chemin.lower()
        for f in fichiers:
            ext = os.path.splitext(f)[1].lower()
            for nom, exts in FAMILLES:
                if ext in exts:
                    comptes[nom][1] += 1
                    if kaykit:
                        comptes[nom][0] += 1
    familles = []
    total_k = total = 0
    for nom, _ in FAMILLES:
        k, t = comptes[nom]
        total_k += k
        total += t
        familles.append({"nom": nom, "kaykit": k, "total": t, "pourcent": round(100.0 * k / t, 1) if t else 0.0})
    return {
        "date": datetime.date.today().isoformat(),
        "pourcent": round(100.0 * total_k / total, 1) if total else 0.0,
        "kaykit": total_k, "total": total, "familles": familles,
    }


def ecrire():
    donnees = mesurer()
    os.makedirs(os.path.join(ICI, "data"), exist_ok=True)
    with open(os.path.join(ICI, "data", "kaykit.json"), "w", encoding="utf-8") as f:
        json.dump(donnees, f, ensure_ascii=False, indent=2)
    return donnees


if __name__ == "__main__":
    d = ecrire()
    print("KayKit : %.1f %% (%d fichiers sur %d)" % (d["pourcent"], d["kaykit"], d["total"]))
    for fam in d["familles"]:
        print("  %-22s %5.1f %%  (%d / %d)" % (fam["nom"], fam["pourcent"], fam["kaykit"], fam["total"]))
