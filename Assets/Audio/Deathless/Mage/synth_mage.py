"""Mage, feu (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Le feu est couleur feu : il **gronde** (bruit brun passé en bas, qui ondule) et **crépite** (impulsions brèves en bande
2 à 6 kHz), il ne tinte jamais. L'explosion est un coup de peau grave dans une gerbe de crépitements ; le cône est un
chalumeau qui rugit en boucle. Détail : § 3.9 du cahier.

Sons écrits (Assets/Audio/Deathless/Mage/, WAV 44,1 kHz mono 16 bits, 3D, graines 1701 à 1749) :
  boule_lancer_1      « fwoup » : le feu s'ouvre, crépite, part
  boule_explosion_1   coup grave, embrasement, crépitements qui retombent, fumée
  cone_boucle         cône de flammes maintenu (boucle 2 s) : chalumeau et crépitements serrés
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


def lancer(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    fwoup = da.passe_bande(da.bruit_brun(rng, da.idx(0.35)), lambda u: 200.0 + 1200.0 * min(1.0, u * 6), 0.8)
    da.ajouter(buf, env(fwoup, 0.02, 0.06, 0.27), 0.0, 1.0)
    da.ajouter(buf, env(da.feu(rng, 0.4, 30, 0.5), 0.01, 0.1, 0.29), 0.0, 0.5)
    da.ajouter(buf, da.souffle(rng, 0.3, 1500.0, 600.0, 1.0, 0.02, 0.28, 1.5), 0.05, 0.3)
    return buf


def explosion(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.2)
    da.peau(rng, buf, 50.0, 1.0, 0.0, 0.5, 1.8, 1.2)
    da.sub(buf, 90.0, 40.0, 0.5, 0.5)
    da.ajouter(buf, da.souffle(rng, 0.6, 300.0, 1500.0, 0.8, 0.02, 0.55, 1.2), 0.0, 0.6)
    f = da.feu(rng, 1.1, 60, 0.8)
    da.ajouter(buf, env(f, 0.005, 0.1, 0.99, 1.8), 0.0, 0.7)
    fumee = da.passe_bas(da.bruit_brun(rng, da.idx(0.9)), 300.0)
    da.ajouter(buf, env(fumee, 0.1, 0.2, 0.6), 0.25, 0.4)
    return buf


def cone(graine):
    rng = random.Random(graine)
    duree, fondu = 2.0, 0.25
    tot = duree + fondu
    chalumeau = da.passe_bande(da.bruit_brun(rng, da.idx(tot)), 450.0, 0.7)
    lisse = da._bruit_lisse(rng, da.idx(tot), 6.0)
    chalumeau = [v * (0.75 + 0.25 * l) for v, l in zip(chalumeau, lisse)]
    f = da.feu(rng, tot, 45, 0.7)
    mix = [0.6 * a / (max(abs(x) for x in chalumeau) or 1) + b for a, b in zip(chalumeau, f)]
    return da.fondre_boucle(mix, duree, fondu)


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("boule_lancer_1", E, -15.0, None, lambda: lancer(1701)),
    ("boule_explosion_1", E, -12.5, None, lambda: explosion(1702)),
    ("cone_boucle", E, -17.0, da.BOUCLE, lambda: cone(1703)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
