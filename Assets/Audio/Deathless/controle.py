"""Planche de contrôle des sons de Deathless : mesure chaque WAV des familles et écrit un tableau Markdown par script.

Mesures (deathless_audio.analyser) : durée ; crête (dBFS) ; RMS moyen sur tout le fichier (dBFS) ; niveau perçu = RMS
maximal sur 50 ms (dBFS, c'est lui que vise la cible de la famille) ; silence de tête (ms avant le premier échantillon
au-dessus de -40 dBFS, 20 ms au plus) ; part de l'énergie par bande de fréquences (spectre moyen, fenêtres de Hann).
Vérifications : 44,1 kHz, mono, crête <= -1,4 dBFS, tête <= 20 ms, fin sans clic, niveau perçu à 1 dB de la cible au plus
(ou en dessous quand la crête plafonne).

Usage : python -B controle.py [dossier_des_familles] [fichier_md]
  par défaut : Assets/Audio/Deathless et Docs/son-lot1-controle.md (chemins relatifs à la racine du dépôt).
"""
import glob
import os
import sys

ICI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ICI)
import deathless_audio as da  # noqa: E402

RACINE = os.path.abspath(os.path.join(ICI, "..", "..", ".."))

# Cible de niveau perçu de chaque son du lot 1 (dB), reprise des scripts (voir le cahier des charges, § 5).
CIBLES = {
    "interface_survol": -24.0, "interface_clic": -18.0, "interface_retour": -19.0, "interface_refus": -18.0,
    "interface_confirmation": -16.0, "interface_decompte": -17.0, "interface_onglet": -21.0,
    "interface_pret": -16.0, "interface_pret_annule": -18.0, "interface_tous_prets": -14.0,
    "nyxessa_tir": -14.0, "nyxessa_frappee": -15.0, "nyxessa_alerte": -13.0, "nyxessa_palier": -12.5,
    "nyxessa_charge_portail": -14.0, "nyxessa_retour_energie": -14.0, "nyxessa_onde": -17.0,
    "nyxessa_destruction": -12.5,
}


def base(nom):
    racine = os.path.splitext(nom)[0]
    parties = racine.rsplit("_", 1)
    return parties[0] if len(parties) == 2 and parties[1].isdigit() else racine


def ligne(chemin):
    m = da.analyser(chemin)
    nom = os.path.basename(chemin)
    cible = CIBLES.get(base(nom))
    problemes = []
    if m["taux"] != 44100:
        problemes.append("taux %d" % m["taux"])
    if m["canaux"] != 1:
        problemes.append("%d canaux" % m["canaux"])
    if m["crete_db"] > -1.39:
        problemes.append("crête")
    if m["tete_ms"] > 20.0:
        problemes.append("silence de tête")
    if m["fin"] > 0.002:
        problemes.append("clic de fin")
    if cible is not None and (m["niveau_db"] > cible + 1.0 or
                              (m["niveau_db"] < cible - 1.0 and m["crete_db"] < -1.6)):
        problemes.append("niveau")
    bandes = " · ".join("%d" % round(b) for b in m["bandes"])
    return ("| `%s` | %.2f s | %.1f | %.1f | %.1f | %s | %.1f ms | %s | %s |" % (
        nom, m["duree"], m["crete_db"], m["rms_db"], m["niveau_db"],
        "%.1f" % cible if cible is not None else "—", m["tete_ms"], bandes,
        "ok" if not problemes else "**" + ", ".join(problemes) + "**"), not problemes)


def doc_script(chemin_script):
    texte = open(chemin_script, encoding="utf-8").read()
    return texte.split('"""')[1].strip().splitlines()[0] if '"""' in texte else ""


def main():
    dossier = sys.argv[1] if len(sys.argv) > 1 else ICI
    sortie = sys.argv[2] if len(sys.argv) > 2 else os.path.join(RACINE, "Docs", "son-lot1-controle.md")
    lignes = [
        "# Planche de contrôle des sons de Deathless, lot 1",
        "",
        "Générée par `python -B Assets/Audio/Deathless/controle.py` : ne pas modifier à la main, relancer le script "
        "après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.",
        "",
        "Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal "
        "sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**) ; **tête** = temps avant le premier échantillon "
        "au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes "
        + ", ".join(b[2] for b in da.BANDES) + ".",
        "",
    ]
    total = bons = 0
    for famille in sorted(os.listdir(dossier)):
        chemin_famille = os.path.join(dossier, famille)
        if not os.path.isdir(chemin_famille):
            continue
        wavs = sorted(glob.glob(os.path.join(chemin_famille, "*.wav")))
        scripts = sorted(glob.glob(os.path.join(chemin_famille, "synth_*.py")))
        if not wavs:
            continue
        rel = os.path.relpath(scripts[0], RACINE).replace(os.sep, "/") if scripts else famille
        lignes += ["## %s — `%s`" % (famille, rel), ""]
        if scripts:
            lignes += [doc_script(scripts[0]), ""]
        lignes += ["| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |",
                   "|---|---|---|---|---|---|---|---|---|"]
        for w in wavs:
            texte, ok = ligne(w)
            lignes.append(texte)
            total += 1
            bons += ok
        lignes.append("")
    lignes.insert(6, "**%d fichiers, %d conformes.**" % (total, bons))
    lignes.insert(7, "")
    with open(sortie, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lignes))
    print("planche écrite :", sortie, "(%d fichiers, %d conformes)" % (total, bons))


if __name__ == "__main__":
    main()
