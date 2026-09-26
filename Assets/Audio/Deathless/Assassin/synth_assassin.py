"""Assassin, dague et arbalète (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Thème Ombre : sons courts, aigus pour la lame, étouffés pour la furtivité (le monde s'éloigne dans un chuchotement),
et la fumée qui chuinte. Détail : § 3.11 du cahier.

Sons écrits (Assets/Audio/Deathless/Assassin/, WAV 44,1 kHz mono 16 bits, graines 1901 à 1949) :
  dague_elan_1     souffle de lame très court et aigu (3D)
  furtif_entree    passage en furtif : souffle qui s'étouffe, chuchotement sans mot (2D)
  fumee            la grenade éclate : petit verre, chuintement qui s'étale, bouffées graves (3D)
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


def dague(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.2)
    da.ajouter(buf, da.souffle(rng, 0.07, 1500.0, 5000.0, 1.4, 0.008, 0.06, 1.6), 0.0, 0.8)
    return buf


def furtif(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.7)
    da.ajouter(buf, da.souffle(rng, 0.6, 1500.0, 350.0, 1.0, 0.02, 0.58, 1.5), 0.0, 0.5)
    ch = da.voix(rng, 0.5, 130.0, lambda u: ("e", "ou", u), souffle=0.95, vibrato=(3.0, 0.01))
    da.ajouter(buf, env(ch, 0.03, 0.15, 0.32), 0.02, 0.35)
    return buf


def fumee(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.5)
    for k in range(4):
        da.choc(rng, buf, 0.004 * k, 0.4, 0.0008, rng.uniform(3000.0, 5000.0), 1.4)
    chuinte = da.passe_bande(da.bruit(rng, da.idx(1.4)), lambda u: 3500.0 - 2000.0 * u, 0.7)
    da.ajouter(buf, env(chuinte, 0.03, 0.3, 1.07), 0.01, 0.5)
    for k in range(3):
        b = da.passe_bas(da.bruit_brun(rng, da.idx(0.35)), 250.0)
        da.ajouter(buf, env(b, 0.05, 0.05, 0.25), 0.05 + 0.3 * k, 0.4)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("dague_elan_1", E, -17.0, None, lambda: dague(1901)),
    ("furtif_entree", E, -18.0, None, lambda: furtif(1902)),
    ("fumee", E, -15.0, None, lambda: fumee(1903)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
