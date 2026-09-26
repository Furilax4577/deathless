"""Planches de contrôle des sons de Deathless : mesure chaque WAV des familles et écrit, pour chaque lot du plan de
production, un tableau Markdown par script (Docs/son-lotN-controle.md).

Le lot et la cible de chaque fichier sont lus dans la liste SONS de son script (nom, lot, cible, fondu, fabrique) :
aucune table à tenir ici.

Mesures (deathless_audio.analyser) : durée ; crête (dBFS) ; RMS moyen sur tout le fichier (dBFS) ; niveau perçu = RMS
maximal sur 50 ms (dBFS, c'est lui que vise la cible) ; silence de tête (ms avant le premier échantillon au-dessus de
-40 dBFS, 20 ms au plus) ; part de l'énergie par bande de fréquences (spectre moyen, fenêtres de Hann).
Vérifications : 44,1 kHz, mono, crête <= -1,4 dBFS, tête <= 20 ms, niveau perçu à 1 dB de la cible au plus (ou en
dessous quand la crête plafonne), fin sans clic pour un son, jointure sans saut pour une boucle (le saut entre le
dernier et le premier échantillon ne dépasse pas 4 fois l'écart type des différences d'un échantillon au suivant).

Usage : python -B controle.py [N ...]   (lots à contrôler ; par défaut : tous les lots trouvés dans les scripts).
"""
import glob
import importlib.util
import math
import os
import sys

ICI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ICI)
import deathless_audio as da  # noqa: E402

RACINE = os.path.abspath(os.path.join(ICI, "..", "..", ".."))


def charger_script(chemin):
    spec = importlib.util.spec_from_file_location(os.path.splitext(os.path.basename(chemin))[0], chemin)
    module = importlib.util.module_from_spec(spec)
    sys.dont_write_bytecode = True
    spec.loader.exec_module(module)
    return module


def jointure(chemin):
    """Saut à la jointure d'une boucle, rapporté à l'écart type des différences d'échantillons."""
    x = da.lire(chemin)[0]
    d = [b - a for a, b in zip(x, x[1:])]
    ecart = math.sqrt(sum(v * v for v in d) / len(d)) or 1e-9
    return abs(x[0] - x[-1]) / ecart


def ligne(chemin, cible, boucle):
    m = da.analyser(chemin)
    problemes = []
    if m["taux"] != 44100:
        problemes.append("taux %d" % m["taux"])
    if m["canaux"] != 1:
        problemes.append("%d canaux" % m["canaux"])
    if m["crete_db"] > -1.39:
        problemes.append("crête")
    if m["tete_ms"] > 20.0:
        problemes.append("silence de tête")
    if boucle:
        saut = jointure(chemin)
        if saut > 4.0:
            problemes.append("jointure (%.1f)" % saut)
    elif m["fin"] > 0.002:
        problemes.append("clic de fin")
    if m["niveau_db"] > cible + 1.0 or (m["niveau_db"] < cible - 1.0 and m["crete_db"] < -1.6):
        problemes.append("niveau")
    bandes = " · ".join("%d" % round(b) for b in m["bandes"])
    return ("| `%s` | %.2f s%s | %.1f | %.1f | %.1f | %.1f | %.1f ms | %s | %s |" % (
        os.path.basename(chemin), m["duree"], " (boucle)" if boucle else "", m["crete_db"], m["rms_db"],
        m["niveau_db"], cible, m["tete_ms"], bandes,
        "ok" if not problemes else "**" + ", ".join(problemes) + "**"), not problemes)


def doc_script(module):
    return (module.__doc__ or "").strip().splitlines()[0]


def main():
    scripts = sorted(glob.glob(os.path.join(ICI, "*", "synth_*.py")))
    familles = []   # (famille, chemin du script, module)
    for s in scripts:
        familles.append((os.path.basename(os.path.dirname(s)), s, charger_script(s)))
    tous = sorted({lot for _, _, m in familles for _, lot, _, _, _ in m.SONS})
    lots = [int(a) for a in sys.argv[1:]] or tous
    for lot in lots:
        lignes = [
            "# Planche de contrôle des sons de Deathless, lot %d" % lot,
            "",
            "Générée par `python -B Assets/Audio/Deathless/controle.py %d` : ne pas modifier à la main, relancer le "
            "script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), "
            "§ 5." % lot,
            "",
            "Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS "
            "maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**) ; **tête** = temps avant le premier "
            "échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes "
            + ", ".join(b[2] for b in da.BANDES) + ". Une **boucle** est vérifiée à sa jointure (pas de saut entre la "
            "fin et le début) au lieu du fondu de fin.",
            "",
        ]
        total = bons = 0
        for famille, chemin_script, module in sorted(familles):
            sons = [(nom, cible, fondu) for nom, l, cible, fondu, _ in module.SONS if l == lot]
            if not sons:
                continue
            rel = os.path.relpath(chemin_script, RACINE).replace(os.sep, "/")
            lignes += ["## %s — `%s`" % (famille, rel), "", doc_script(module), "",
                       "| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |",
                       "|---|---|---|---|---|---|---|---|---|"]
            for nom, cible, fondu in sorted(sons):
                chemin = os.path.join(os.path.dirname(chemin_script), nom + ".wav")
                if not os.path.isfile(chemin):
                    lignes.append("| `%s.wav` | | | | | %.1f | | | **fichier absent** |" % (nom, cible))
                    total += 1
                    continue
                texte, ok = ligne(chemin, cible, fondu == da.BOUCLE)
                lignes.append(texte)
                total += 1
                bons += ok
            lignes.append("")
        lignes.insert(6, "**%d fichiers, %d conformes.**" % (total, bons))
        lignes.insert(7, "")
        sortie = os.path.join(RACINE, "Docs", "son-lot%d-controle.md" % lot)
        with open(sortie, "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(lignes))
        print("planche écrite :", sortie, "(%d fichiers, %d conformes)" % (total, bons))


if __name__ == "__main__":
    main()
