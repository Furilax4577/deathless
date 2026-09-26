"""Bouclier de Nyxessa et villageois sorcier (Deathless, lot 2 du cahier des charges son, 26/09/2026).

Le bouclier est un mur de gemmes en lévitation, nourri par l'énergie de Nyxessa : il garde la **signature de gemme**
de la relique (partiels 1 : 2,756 : 5,404 : 8,933 avec jumeaux désaccordés), mais **une octave plus grave** (mi4 à si5)
et en grappes serrées. C'est un mur, pas une étoile. Ses états (plein, entamé, critique) s'entendent à un accord qui
glisse vers le bas et à un frisson qui s'accélère ; le fer de Morgrim est une **plaque** de métal grave, jamais une
gemme. Le sorcier n'a pas de voix : sa respiration d'incantation est un souffle rythmé. Détail : Docs/son-cahier-des-charges.md
(§ 3.3).

Sons écrits (Assets/Audio/Deathless/Bouclier/, WAV 44,1 kHz mono 16 bits, 3D, graines 1351 à 1399) :
  bouclier_leve                    le mur de gemmes se lève en spirale et se serre, accord tenu
  bouclier_touche_1..4             coup encaissé : choc sourd absorbé, mur qui frémit, petit retour de la riposte
  bouclier_etat_entame             passage à l'orange (< 40 %) : l'accord glisse d'un demi-ton, frisson rapide
  bouclier_etat_critique           passage au rouge (< 15 %) : même geste plus bas, craquements de verre
  bouclier_brise                   le bouclier cède : craquement, bris en cascade, souffle qui s'effondre
  bouclier_breche_1..2             coup de brèche de Morgrim martache : fer sur verre, gemmes qui crient faux
  bouclier_palier                  palier du bouclier : le mur se densifie, accord tenu
  sorcier_incantation_boucle       incantation (boucle 3 s) : souffle-respiration, motif lent de trois gemmes
  sorcier_canalisation_boucle      lien d'énergie avec la relique (boucle 4 s) : bourdon ondulant, gemmes qui voyagent
  sorcier_canalisation_eclat_1..2  un coup avance la recharge d'un missile : gemme brève, souffle qui monte

Usage : python -B synth_bouclier.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_bouclier_* et dl_sorcier_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
MUR = da.penta(4, 5)            # mi4 à ré6 : les gemmes du mur


def gemme_glissee(rng, buf, f0, f1, amp, duree, debut, frisson):
    """Gemme dont la hauteur glisse (le bouclier faiblit) : fondamental et partiel 2,756, chacun doublé d'un jumeau
    écarté de `frisson` Hz (le frisson s'accélère quand le bouclier s'use)."""
    for r, a in ((1.0, 1.0), (2.756, 0.4)):
        for ecart, poids in ((0.0, 0.6), (frisson, 0.4)):
            da.sinus_glisse(buf, f0 * r + ecart, f1 * r + ecart, amp * a * poids, duree, debut, 0.0006,
                            duree * 0.95, 0.6, rng.uniform(0, 2 * math.pi))


def leve(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.7)
    tourbillon = da.passe_bande(da.bruit(rng, da.idx(1.2)),
                                lambda u: (500.0 + 1500.0 * u) * (1.0 + 0.35 * math.sin(2 * math.pi * 4.0 * u)), 1.4)
    da.ajouter(buf, da.enveloppe(tourbillon, 0.25, 0.55, 0.4, 1.4), 0.0, 0.35)   # souffle qui tourne en montant
    da.gemme(rng, buf, N("E4"), 0.25, 0.4, 0.003, eclat=0.6)
    da.scintillement(rng, buf, 0.0, 1.0, MUR, 100, 0.12, (0.15, 0.4),
                     densite=lambda u: u ** 0.75, registre=lambda u: u)
    for n, a in (("E4", 0.4), ("B4", 0.32), ("E5", 0.28)):                       # le mur est levé
        da.gemme(rng, buf, N(n), a, 1.3, 1.05, eclat=0.8)
    return buf


def touche(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    da.mode(buf, 250.0 * rng.uniform(0.9, 1.1), 0.5, 0.05, 0.0, 0.0008)          # le coup, absorbé
    da.mode(buf, 400.0 * rng.uniform(0.9, 1.1), 0.3, 0.035, 0.0, 0.0008)
    da.ajouter(buf, da.enveloppe(da.passe_bas(da.bruit(rng, da.idx(0.04)), 1500.0), 0.0006, 0.004, 0.035, 2.0),
               0.0, 0.6)
    for k in range(6):                                                            # le mur frémit
        f = MUR[rng.randrange(len(MUR) - 2)]
        da.gemme(rng, buf, f, rng.uniform(0.12, 0.2), rng.uniform(0.15, 0.25), 0.004 + 0.006 * k,
                 eclat=0.7, jumeau=True, durete=0.3)
    da.choc(rng, buf, 0.06, 0.25, 0.001, 3500.0, 0.8)                             # la riposte
    da.ajouter(buf, da.souffle(rng, 0.08, 1500.0, 3200.0, 1.3, 0.01, 0.07, 1.5), 0.06, 0.25)
    return buf


def etat(graine, accord, craque):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    demi = 2 ** (-1 / 12.0)
    for n, a in accord:
        gemme_glissee(rng, buf, N(n), N(n) * demi, a, 0.8, 0.0, 6.0)
    da.choc(rng, buf, 0.0, 0.3, 0.0012, 2500.0, 0.8)
    if craque:
        for k in range(5):                                                        # craquements de verre
            da.choc(rng, buf, 0.05 + 0.06 * k + rng.uniform(0, 0.03), rng.uniform(0.2, 0.4), 0.0007,
                    rng.uniform(3000.0, 6000.0), 1.2)
    return buf


def brise(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.3)
    craque = da.passe_haut(da.bruit(rng, da.idx(0.1)), 700.0)
    da.ajouter(buf, da.enveloppe(craque, 0.0006, 0.01, 0.09, 2.5), 0.0, 0.9)
    da.choc(rng, buf, 0.0, 0.8, 0.004, 1800.0, 0.5)
    da.sub(buf, 90.0, 35.0, 0.55, 1.4)
    eclats = sorted(rng.uniform(330.0, 1400.0) for _ in range(30))              # le mur tombe : grave, hors gamme
    da.scintillement(rng, buf, 0.0, 1.2, eclats, 70, 0.22, (0.1, 0.45),
                     densite=lambda u: u ** 2.0, registre=lambda u: 1.0 - 0.8 * u)
    da.ajouter(buf, da.souffle(rng, 1.4, 3000.0, 200.0, 1.2, 0.02, 1.35, 1.4), 0.0, 0.45)
    return buf


def breche(graine, f_fer):
    rng = random.Random(graine)
    buf = da.tampon(0.9)
    da.plaque(rng, buf, f_fer, 0.55, 0.45, 0.0, durete=1.4)                      # le fer du marteau
    da.mode(buf, 140.0, 0.4, 0.08, 0.0, 0.0008)                                   # la masse
    for n in ("E5", "F5", "F#5"):                                                 # les gemmes crient faux
        da.gemme(rng, buf, N(n), 0.22, 0.6, 0.012, eclat=0.9, durete=0.0)
    for k in range(4):
        da.choc(rng, buf, 0.03 + 0.05 * k, 0.25, 0.0007, rng.uniform(3000.0, 6000.0), 1.2)
    return buf


def palier(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    da.gemme(rng, buf, N("B4"), 0.2, 0.3, 0.003, eclat=0.6)
    da.scintillement(rng, buf, 0.0, 0.8, MUR, 40, 0.14, (0.15, 0.35), densite=lambda u: u ** 0.5)
    for n, a in (("E4", 0.4), ("B4", 0.34), ("E5", 0.3)):
        da.gemme(rng, buf, N(n), a, 1.0, 0.8, eclat=0.9)
    return buf


def incantation(graine):
    """Boucle de 3 s : souffle qui respire (2 cycles de 1,5 s), trois gemmes lentes (mi5, si5, mi6) repliées sur la
    boucle pour que leur résonance passe la jointure."""
    rng = random.Random(graine)
    duree, fondu = 3.0, 0.3
    n = da.idx(duree + fondu)
    souffle = da.passe_bande(da.bruit(rng, n), 1000.0, 0.9)
    souffle = [v * (0.25 + 0.75 * math.sin(math.pi * (i / da.RATE) / 1.5) ** 2) for i, v in enumerate(souffle)]
    continu = da.fondre_boucle([v * 0.5 for v in souffle], duree, fondu)
    evenements = da.tampon(duree + 1.5)
    for t, n_ in ((0.15, "E5"), (1.15, "B5"), (2.15, "E6")):
        da.gemme(rng, evenements, N(n_), 0.25, 1.1, t, eclat=0.7, durete=0.3)
    return [a + b for a, b in zip(continu, da.plier(evenements, duree))]


def canalisation(graine):
    """Boucle de 4 s : bourdon de gemmes (si4 + mi5) avec vibrato lent de 0,5 Hz (2 cycles), jumeaux à 0,5 Hz ;
    huit petites gemmes voyagent le long du lien (si6 / mi7 alternés, une toutes les 0,5 s)."""
    rng = random.Random(graine)
    duree = 4.0
    n = da.idx(duree)
    bourdon = [0.0] * n
    for f, a in ((N("B4"), 0.22), (N("E5"), 0.18), (N("B4") * 2.756, 0.06), (N("E5") * 2.756, 0.05)):
        for ecart, ph in ((0.0, 0.0), (0.5, 1.1)):
            fb = round((f + ecart) * duree) / duree      # nombre entier de périodes sur la boucle : jointure exacte
            phase = ph
            for i in range(n):
                t = i / da.RATE
                phase += 2 * math.pi * fb * (1 + 0.004 * math.sin(2 * math.pi * 0.5 * t)) / da.RATE
                bourdon[i] += a * 0.5 * math.sin(phase)
    evenements = da.tampon(duree + 0.5)
    for k in range(8):
        da.gemme(rng, evenements, N("B6") if k % 2 == 0 else N("E7"), 0.1, 0.35, 0.05 + 0.5 * k,
                 eclat=0.6, jumeau=False, durete=0.3)
    return [a + b for a, b in zip(bourdon, da.plier(evenements, duree))]


def canalisation_eclat(graine, n_):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    da.gemme(rng, buf, N(n_), 0.4, 0.3, 0.0, eclat=1.1)
    da.ajouter(buf, da.souffle(rng, 0.25, 1000.0, 4000.0, 1.4, 0.03, 0.2, 1.5), 0.0, 0.12)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("bouclier_leve", 2, -14.0, None, lambda: leve(1351)),
    ("bouclier_touche_1", 2, -15.0, None, lambda: touche(1352)),
    ("bouclier_touche_2", 2, -15.0, None, lambda: touche(1353)),
    ("bouclier_touche_3", 2, -15.0, None, lambda: touche(1354)),
    ("bouclier_touche_4", 2, -15.0, None, lambda: touche(1355)),
    ("bouclier_etat_entame", 2, -16.0, None, lambda: etat(1356, (("E5", 0.4), ("B5", 0.3)), False)),
    ("bouclier_etat_critique", 2, -15.0, None, lambda: etat(1357, (("B4", 0.42), ("E5", 0.32)), True)),
    ("bouclier_brise", 2, -12.5, None, lambda: brise(1358)),
    ("bouclier_breche_1", 2, -13.0, None, lambda: breche(1359, 820.0)),
    ("bouclier_breche_2", 2, -13.0, None, lambda: breche(1360, 960.0)),
    ("bouclier_palier", 2, -14.0, None, lambda: palier(1361)),
    ("sorcier_incantation_boucle", 2, -18.0, da.BOUCLE, lambda: incantation(1362)),
    ("sorcier_canalisation_boucle", 2, -20.0, da.BOUCLE, lambda: canalisation(1363)),
    ("sorcier_canalisation_eclat_1", 2, -17.0, None, lambda: canalisation_eclat(1364, "B6")),
    ("sorcier_canalisation_eclat_2", 2, -17.0, None, lambda: canalisation_eclat(1365, "E7")),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
