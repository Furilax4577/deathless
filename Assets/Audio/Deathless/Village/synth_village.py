"""Village, or, coffres, taverne (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Matières du décor : l'or est un éclat de métal fin en grappe (pièces) dans le cuir ; un coffre grince (frottement
colle-glisse du bois sur ses charnières) puis son couvercle tombe ; la bière gargouille dans une chope de bois.
Détail : § 3.17 du cahier.

Sons écrits (Assets/Audio/Deathless/Village/, WAV 44,1 kHz mono 16 bits, graines 2301 à 2349) :
  or_caisse_1     de l'or entre dans la caisse commune : pièces qui tombent dans une bourse de cuir (2D)
  coffre_ouvre_1  un coffre s'ouvre : charnière qui grince, couvercle qui tombe, reflet d'or (3D)
  taverne_biere   boire une bière : robinet, glouglou, gorgées, chope reposée (2D)
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


def grincement(rng, duree, f_debut, f_fin, resonances=(620.0, 1450.0)):
    """Frottement colle-glisse : impulsions dont la cadence passe de f_debut à f_fin Hz, dans deux résonances de bois."""
    n = da.idx(duree)
    x = [0.0] * n
    t = 0.0
    while t < duree:
        u = t / duree
        x[da.idx(t)] = rng.uniform(0.6, 1.0)
        t += 1.0 / (f_debut + (f_fin - f_debut) * u) * rng.uniform(0.85, 1.15)
    res = [0.0] * n
    for fc in resonances:
        y = da.passe_bande(x, fc, 8.0)
        res = [a + b for a, b in zip(res, y)]
    return res


def or_caisse(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    for _ in range(10):
        da.plaque(rng, buf, rng.uniform(2600.0, 4600.0), rng.uniform(0.1, 0.3), rng.uniform(0.06, 0.14),
                  rng.random() ** 1.5 * 0.3, durete=0.5)
    da.peau(rng, buf, 140.0, 0.35, 0.05, 0.06, 1.2, 0.5)                                    # la bourse de cuir
    return buf


def coffre(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    da.choc(rng, buf, 0.0, 0.3, 0.001, 1800.0, 0.8)                                          # le loquet
    g = grincement(rng, 0.55, 45.0, 18.0)
    da.ajouter(buf, env(g, 0.03, 0.35, 0.17), 0.0, 0.6)
    da.peau(rng, buf, 90.0, 0.7, 0.6, 0.2, 1.3, 1.0)                                        # le couvercle tombe
    da.mode(buf, 210.0, 0.35, 0.1, 0.6, 0.0006)
    da.plaque(rng, buf, 3300.0, 0.08, 0.3, 0.65, durete=0.2)                                 # reflet d'or
    return buf


def biere(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.4)
    jet = da.passe_bande(da.bruit(rng, da.idx(0.45)), lambda u: 500.0 + 700.0 * u, 3.0)
    da.ajouter(buf, env(jet, 0.02, 0.35, 0.08), 0.0, 0.4)                                    # la chope se remplit
    for k in range(3):
        t = 0.55 + 0.17 * k
        da.sinus_glisse(buf, 160.0, 420.0, 0.35, 0.06, t, 0.004, 0.05, 0.8)
        da.ajouter(buf, env(da.passe_bas(da.bruit(rng, da.idx(0.07)), 600.0), 0.005, 0.02, 0.045), t, 0.3)
    da.bois(rng, buf, N("E3"), 0.45, 0.08, 1.2, clic=0.6, clarte=0.3)                        # chope reposée
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("or_caisse_1", E, -15.0, None, lambda: or_caisse(2301)),
    ("coffre_ouvre_1", E, -14.0, None, lambda: coffre(2302)),
    ("taverne_biere", E, -16.0, None, lambda: biere(2303)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
