"""Nyxar, le Nécromancien (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

L'ancien possesseur de Nyxessa : ses éclats sont des **gemmes corrompues** (désaccordées d'un quart de ton), son
arrivée un bourdon dissonant sous un chœur qui ne s'accorde pas, avec du métal frotté. Quand un éclat se brise,
l'énergie libérée retourne à Nyx : le chœur se redresse et les gemmes redeviennent justes. Détail : § 3.15 du cahier.

Sons écrits (Assets/Audio/Deathless/Nyxar/, WAV 44,1 kHz mono 16 bits, graines 2151 à 2199) :
  nyxar_arrivee         apparition (nuit 12, 2D) : bourdon dissonant, chœur faux, métal frotté, gemmes corrompues
  nyxar_eclat_brise_1   un éclat se brise : verre, cri bref, puis l'énergie file vers Nyx en gemmes justes (3D)
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


QUART = 2 ** (0.5 / 12.0)


def arrivee(graine):
    rng = random.Random(graine)
    buf = da.tampon(3.5)
    b = da.bourdon(rng, 3.5, [N("E1"), N("F1")], 0.2, 350.0)
    da.ajouter(buf, env(b, 0.3, 2.0, 1.2), 0.0, 0.5)
    for f in (N("E3"), N("E3") * QUART, N("B3") * QUART):
        c = da.choeur(rng, 2.8, lambda u, f=f: f * (1 - 0.03 * u), lambda u: ("ou", "a", u), nombre=2, souffle=0.5)
        da.ajouter(buf, env(c, 0.8, 1.0, 1.0), 0.2, 0.25)
    m = da.metal_frotte(rng, 3.0, 131.0, attaque=0.8, tremble=0.5)
    da.ajouter(buf, env(m, 0.01, 2.0, 0.99), 0.3, 0.3)
    da.peau(rng, buf, 45.0, 0.8, 0.0, 0.8, 1.5, 1.0)
    for t, n in ((0.8, "E5"), (1.6, "B4"), (2.4, "G5")):
        da.gemme(rng, buf, N(n) * QUART, 0.15, 1.0, t, eclat=0.5, durete=0.4)               # éclats corrompus
    return buf


def eclat_brise(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.0)
    fele = da.passe_haut(da.bruit(rng, da.idx(0.06)), 900.0)
    da.ajouter(buf, env(fele, 0.0005, 0.005, 0.055, 2.5), 0.0, 0.8)
    eclats = sorted(rng.uniform(500.0, 2500.0) * QUART for _ in range(16))
    da.scintillement(rng, buf, 0.0, 0.3, eclats, 20, 0.12, (0.1, 0.3), densite=lambda u: u ** 2)
    v = da.voix(rng, 0.45, lambda u: 330.0 * (1 - 0.5 * u), "a", souffle=0.35, rauque=0.6, sous_harm=0.5)
    da.ajouter(buf, env(da.saturer(v, 3.0), 0.004, 0.1, 0.35), 0.0, 0.4)
    c = da.choeur(rng, 1.3, lambda u: N("E3") * (1 + u), lambda u: ("o", "a", u), nombre=3, souffle=0.45)
    da.ajouter(buf, env(c, 0.2, 0.5, 0.6), 0.45, 0.4)                                        # l'énergie retourne à Nyx
    da.gemme(rng, buf, N("E5"), 0.2, 0.9, 1.1, eclat=0.5)
    da.gemme(rng, buf, N("B5"), 0.15, 0.8, 1.2, eclat=0.5)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("nyxar_arrivee", E, -12.0, None, lambda: arrivee(2151)),
    ("nyxar_eclat_brise_1", E, -12.0, None, lambda: eclat_brise(2152)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
