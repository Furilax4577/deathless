"""Squelettes (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Os et poussière : **os creux** (petits tubes frappés, modes impairs, secs), cliquetis, terre qui s'ouvre (peau grave,
bruit brun), gravier et sable. Jamais de voix humaine. À la mort, une seule gemme lointaine et très douce : la magie de
Nyxessa qui quitte le squelette. Détail : § 3.13 du cahier.

Sons écrits (Assets/Audio/Deathless/Squelettes/, WAV 44,1 kHz mono 16 bits, 3D, graines 2001 à 2049) :
  squelette_sortie_1        la terre s'ouvre, les mottes retombent, les os s'assemblent
  squelette_preparation_1   crécelle d'os qui s'accélère jusqu'à l'impact (0,7 s)
  squelette_touche_1        os creux frappé, éclats d'os
  squelette_mort_1          les os s'effondrent en grappe, poussière, soupir de sable, gemme lointaine
  squelette_aube_1          à l'aube : la poussière monte et se disperse, pluie d'os légers
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
E = "echantillons"   # lot : échantillons de la direction sombre (26/09/2026), pour juger le bain avant la production


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


def sortie(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.2)
    da.peau(rng, buf, 62.0, 0.8, 0.0, 0.3, 1.4, 0.8)
    terre = da.passe_bas(da.bruit_brun(rng, da.idx(0.5)), 300.0)
    da.ajouter(buf, env(terre, 0.005, 0.1, 0.39), 0.0, 0.6)
    da.gravier(rng, buf, 0.05, 0.7, 45, 0.35, densite=lambda u: u ** 1.6)                   # les mottes retombent
    da.cliquetis(rng, buf, 0.45, 0.5, 10, 0.3)                                              # les os s'assemblent
    return buf


def preparation(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.7)
    t, ecart = 0.0, 0.08
    while t < 0.66:
        da.os_creux(rng, buf, rng.uniform(900.0, 1600.0), 0.4 * (0.5 + t), t, 0.02)
        t += ecart
        ecart = max(0.018, ecart * 0.86)
    da.ajouter(buf, da.souffle(rng, 0.7, 300.0, 1200.0, 1.2, 0.6, 0.1, 1.2), 0.0, 0.3)
    return buf


def touche(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.25)
    da.os_creux(rng, buf, rng.uniform(450.0, 650.0), 0.7, 0.0, 0.05)
    da.os_creux(rng, buf, rng.uniform(800.0, 950.0), 0.4, 0.003, 0.04)
    da.cliquetis(rng, buf, 0.01, 0.08, 4, 0.2)
    return buf


def mort(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    for _ in range(16):
        u = rng.random() ** 1.8
        da.os_creux(rng, buf, rng.uniform(380.0, 1200.0), rng.uniform(0.2, 0.5), u * 0.5, rng.uniform(0.03, 0.06))
    poussiere = da.passe_bas(da.bruit_brun(rng, da.idx(0.9)), 500.0)
    da.ajouter(buf, env(poussiere, 0.01, 0.1, 0.79, 1.8), 0.02, 0.4)
    da.ajouter(buf, da.souffle(rng, 0.6, 2000.0, 600.0, 1.2, 0.1, 0.5, 1.5), 0.2, 0.2)       # soupir de sable
    da.gemme(rng, buf, N("E5"), 0.05, 0.4, 0.45, eclat=0.3, durete=0.0)                      # la magie s'en va
    return buf


def aube(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.5)
    da.ajouter(buf, da.souffle(rng, 1.3, 400.0, 3000.0, 1.0, 0.2, 1.1, 1.3), 0.0, 0.6)
    for _ in range(10):
        da.os_creux(rng, buf, rng.uniform(700.0, 1600.0), rng.uniform(0.1, 0.3), rng.uniform(0.0, 0.9), 0.03)
    da.gravier(rng, buf, 0.1, 1.0, 30, 0.12)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("squelette_sortie_1", E, -14.0, None, lambda: sortie(2001)),
    ("squelette_preparation_1", E, -16.0, None, lambda: preparation(2002)),
    ("squelette_touche_1", E, -16.0, None, lambda: touche(2003)),
    ("squelette_mort_1", E, -15.0, None, lambda: mort(2004)),
    ("squelette_aube_1", E, -15.0, None, lambda: aube(2005)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
