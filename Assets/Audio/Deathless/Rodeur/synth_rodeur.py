"""Rôdeur, arc (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Les flèches ne sont pas magiques : bois, corde, plume et air, jamais de gemme. La corde est une **corde pincée**
(Karplus-Strong) grave qui claque, le corps de l'arc un mode de bois, la flèche un souffle qui s'éloigne ; à l'impact,
l'os creux et le fût qui vibre. Détail : § 3.10 du cahier.

Sons écrits (Assets/Audio/Deathless/Rodeur/, WAV 44,1 kHz mono 16 bits, 3D, graines 1801 à 1849) :
  arc_tir_1            tir peu chargé : corde qui claque, bois, flèche qui part
  arc_tir_charge_1     tir chargé à fond : corde plus grave et plus sèche, flèche qui file et s'éloigne
  fleche_impact_os_1   flèche dans un squelette : os creux, fût qui vibre
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


def tir(graine, f_corde, force):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    c = da.corde(rng, 0.5, f_corde, 0.3 + 0.1 * force, 0.8)
    da.ajouter(buf, env(c, 0.0005, 0.1, 0.4), 0.0, 0.8)
    da.mode(buf, 185.0, 0.3 * force, 0.08, 0.0, 0.0006)                                  # le bois de l'arc
    da.choc(rng, buf, 0.0, 0.4 * force, 0.001, 1800.0, 0.8)
    fleche = da.souffle(rng, 0.35 + 0.15 * force, 3200.0, 1200.0, 1.4, 0.005, 0.33 + 0.15 * force, 1.8)
    da.ajouter(buf, fleche, 0.01, 0.12 * force)
    return buf


def impact_os(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    da.os_creux(rng, buf, 560.0, 0.6, 0.0, 0.06)
    da.mode(buf, 180.0, 0.3, 0.15, 0.0, 0.0008)                                            # le fût vibre
    da.choc(rng, buf, 0.0, 0.4, 0.001, 3000.0, 0.8)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("arc_tir_1", E, -14.0, None, lambda: tir(1801, 110.0, 0.7)),
    ("arc_tir_charge_1", E, -13.0, None, lambda: tir(1802, 98.0, 1.0)),
    ("fleche_impact_os_1", E, -14.0, None, lambda: impact_os(1803)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
