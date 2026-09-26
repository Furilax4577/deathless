"""Viking, hache à deux mains (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

La masse avant tout : souffles larges et graves, impacts dans la terre et dans les os, et un **rugissement** de
créature (voix fantôme très grave, rauque et cassée, doublée à l'octave basse, saturée), qui part puis revient comme
pour dire « venez ». Détail : § 3.8 du cahier.

Sons écrits (Assets/Audio/Deathless/Viking/, WAV 44,1 kHz mono 16 bits, 3D, graines 1601 à 1649) :
  hache_elan_1               souffle large et grave, la masse qui passe
  rugissement_1              rugissement : voix grave rauque (« a » → « o »), gorge, onde qui part et revient
  saut_percutant_impact_1    frappe au sol : grosse peau, poussée grave, terre, onde qui s'éloigne
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


def elan(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    da.ajouter(buf, da.souffle(rng, 0.22, 400.0, 1800.0, 0.9, 0.04, 0.18, 1.5), 0.0, 0.8)
    da.sub(buf, 70.0, 50.0, 0.25, 0.2, 0.03)
    return buf


def rugissement(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.6)
    for octave, gain in ((1.0, 1.0), (0.5, 0.8)):
        v = da.voix(rng, 1.2, lambda u, o=octave: 95.0 * o * (1 + 0.25 * math.sin(math.pi * min(1, u * 1.4))),
                    lambda u: ("a", "o", u), souffle=0.5, rauque=0.85, gigue=0.04, vibrato=(7.0, 0.03),
                    sous_harm=0.65)
        da.ajouter(buf, env(da.saturer(v, 3.0), 0.02, 0.6, 0.58, 1.2), 0.0, 0.45 * gain)
    gorge = da.passe_bande(da.bruit(rng, da.idx(1.1)), 700.0, 2.0)
    da.ajouter(buf, env(gorge, 0.05, 0.5, 0.55), 0.0, 0.25)
    da.ajouter(buf, da.souffle(rng, 0.5, 300.0, 1200.0, 1.0, 0.2, 0.3), 0.5, 0.25)          # l'onde part...
    da.ajouter(buf, da.souffle(rng, 0.5, 1200.0, 300.0, 1.0, 0.1, 0.4), 1.05, 0.25)         # ... et revient
    return buf


def saut_impact(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    da.peau(rng, buf, 45.0, 1.0, 0.0, 0.6, 1.7, 1.2)
    da.sub(buf, 80.0, 35.0, 0.6, 0.5)
    da.gravier(rng, buf, 0.0, 0.5, 40, 0.3, densite=lambda u: u ** 2)
    da.ajouter(buf, da.souffle(rng, 0.8, 600.0, 200.0, 1.0, 0.02, 0.75, 1.3), 0.02, 0.4)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("hache_elan_1", E, -14.0, None, lambda: elan(1601)),
    ("rugissement_1", E, -12.5, None, lambda: rugissement(1602)),
    ("saut_percutant_impact_1", E, -13.0, None, lambda: saut_impact(1603)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
