"""Coups critiques (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

L'éclat d'or reste bref (plaque mince de métal, T60 0,3 s), mais il frappe plus bas et plus lourd, sur une peau et
un souffle qui tranche : un critique se sent dans le ventre avant de briller. Détail : § 3.6 du cahier.

Sons écrits (Assets/Audio/Deathless/Critiques/, WAV 44,1 kHz mono 16 bits, 3D, graines 1451 à 1499) :
  critique_1          coup critique : éclat d'or grave, peau, souffle
  critique_meilleur   meilleur critique (× 5) : deux éclats en quinte, lame d'air, peau de guerre, cri bref du chœur
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


def critique(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    da.plaque(rng, buf, 1650.0, 0.45, 0.3, 0.0, durete=1.2)
    da.peau(rng, buf, 85.0, 0.6, 0.0, 0.18, 1.5, 0.8)
    da.ajouter(buf, da.souffle(rng, 0.12, 4000.0, 1200.0, 1.2, 0.004, 0.11, 1.8), 0.0, 0.3)
    return buf


def meilleur(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    da.plaque(rng, buf, 1320.0, 0.4, 0.45, 0.0, durete=1.4)
    da.plaque(rng, buf, 1980.0, 0.3, 0.4, 0.01, durete=0.5)
    da.ajouter(buf, da.souffle(rng, 0.18, 5000.0, 1500.0, 1.3, 0.004, 0.17, 1.8), 0.0, 0.45)
    da.peau(rng, buf, 55.0, 0.8, 0.0, 0.4, 1.6, 1.0)
    c = da.choeur(rng, 0.35, 196.0, "a", nombre=3, souffle=0.35, octaves=(1.0, 0.5))
    da.ajouter(buf, env(da.saturer(c, 2.0), 0.005, 0.08, 0.26), 0.01, 0.3)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("critique_1", E, -14.0, None, lambda: critique(1451)),
    ("critique_meilleur", E, -12.5, None, lambda: meilleur(1452)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
