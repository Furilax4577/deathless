"""Morgrim, le Roi des os (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Le colosse : les sons les plus lourds du jeu. Cri de gorge d'os (voix fantôme très grave, cassée, saturée, à deux
octaves, sur un cliquetis de tous ses os), impacts de peau énorme et de terre, et l'onde du Fracas qui roule au ras du
sol, assez lente pour qu'on entende quand sauter. Détail : § 3.14 du cahier.

Sons écrits (Assets/Audio/Deathless/Morgrim/, WAV 44,1 kHz mono 16 bits, 3D, graines 2101 à 2149) :
  morgrim_cri        cri qui renforce les squelettes : gorge d'os, cliquetis, onde
  massue_fracas_1    la boule à pointes frappe le sol : peau énorme, poussée, terre, pointes
  massue_onde        l'onde de choc roule (2,4 s) : souffle grave qui avance, gravier qui tremble
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


def cri(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.0)
    for octave, gain in ((1.0, 1.0), (0.5, 0.9)):
        v = da.voix(rng, 1.6, lambda u, o=octave: 72.0 * o * (1 + 0.35 * math.sin(math.pi * min(1, u * 1.2))),
                    lambda u: ("o", "a", min(1.0, u * 2)), souffle=0.55, rauque=0.9, gigue=0.05,
                    vibrato=(5.5, 0.035), sous_harm=0.7)
        da.ajouter(buf, env(da.saturer(v, 3.5), 0.03, 0.8, 0.77, 1.2), 0.0, 0.5 * gain)
    da.cliquetis(rng, buf, 0.1, 1.4, 40, 0.25)
    da.ajouter(buf, da.souffle(rng, 1.2, 200.0, 900.0, 1.0, 0.3, 0.9), 0.5, 0.3)
    return buf


def fracas(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.2)
    da.peau(rng, buf, 38.0, 1.0, 0.0, 0.9, 1.8, 1.4)
    da.sub(buf, 60.0, 25.0, 0.8, 0.9)
    terre = da.passe_bas(da.bruit_brun(rng, da.idx(0.8)), 400.0)
    da.ajouter(buf, env(terre, 0.003, 0.1, 0.69, 1.6), 0.0, 0.8)
    da.gravier(rng, buf, 0.0, 0.8, 60, 0.35, densite=lambda u: u ** 1.8)
    for k in range(5):                                                                      # les pointes mordent
        da.choc(rng, buf, 0.002 * k, 0.5, 0.001, rng.uniform(1500.0, 3000.0), 1.0)
    return buf


def onde(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.4)
    roule = da.passe_bande(da.bruit_brun(rng, da.idx(2.4)), lambda u: 150.0 + 350.0 * (1 - u), 1.2)
    roule = [v * (0.7 + 0.3 * math.sin(2 * math.pi * 7 * i / da.RATE)) for i, v in enumerate(roule)]
    da.ajouter(buf, env(roule, 0.02, 1.3, 1.08), 0.0, 1.0)
    da.gravier(rng, buf, 0.0, 2.2, 90, 0.2, bande=lambda u: 1500.0 - 700.0 * u)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("morgrim_cri", E, -11.0, None, lambda: cri(2101)),
    ("massue_fracas_1", E, -11.0, None, lambda: fracas(2102)),
    ("massue_onde", E, -13.0, None, lambda: onde(2103)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
