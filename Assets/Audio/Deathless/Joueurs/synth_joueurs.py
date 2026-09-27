"""Joueurs, actions communes (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Corps, cuir, équipement, souffle : pas de cri, pas de voix de héros. Un coup reçu est une peau mate (le corps) et un
cliquetis d'équipement ; la mort coupe le son une demi-seconde puis l'énergie file vers Nyxessa dans un chœur lointain.
Détail : Docs/son-cahier-des-charges.md (§ 3.5).

Sons écrits (Assets/Audio/Deathless/Joueurs/, WAV 44,1 kHz mono 16 bits, 3D, graines 1401 à 1449) :
  joueur_touche_1..2   coup reçu : peau mate, cuir, cliquetis, souffle coupé
  esquive_1            roulade : souffle, roulement sur le sol, équipement
  joueur_mort          mort : coup sourd, silence, gemmes et chœur lointain qui partent vers Nyxessa
  potion_boire         bouchon, trois gorgées, souffle d'aise
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


def equipement(rng, buf, debut, nombre, amp):
    for _ in range(nombre):
        da.plaque(rng, buf, rng.uniform(1800.0, 3200.0), amp * rng.uniform(0.3, 1.0), 0.04, debut + rng.uniform(0, 0.08),
                  durete=0.3)


def touche(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.35)
    da.peau(rng, buf, rng.uniform(110.0, 140.0), 0.7, 0.0, 0.09, 1.3, 1.0)
    cuir = da.passe_bas(da.bruit(rng, da.idx(0.04)), 1200.0)
    da.ajouter(buf, env(cuir, 0.001, 0.005, 0.034, 2.0), 0.0, 0.6)
    equipement(rng, buf, 0.005, 3, 0.12)
    da.ajouter(buf, da.souffle(rng, 0.1, 900.0, 600.0, 1.0, 0.005, 0.09, 2.0), 0.02, 0.3)   # souffle coupé
    return buf


def esquive(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    da.ajouter(buf, da.souffle(rng, 0.16, 700.0, 2000.0, 1.1, 0.01, 0.15, 1.5), 0.0, 0.5)
    roule = da.passe_bas(da.bruit_brun(rng, da.idx(0.3)), 350.0)
    roule = [v * (0.6 + 0.4 * math.sin(2 * math.pi * 12 * i / da.RATE)) for i, v in enumerate(roule)]
    da.ajouter(buf, env(roule, 0.02, 0.1, 0.18), 0.12, 0.8)
    equipement(rng, buf, 0.14, 4, 0.08)
    return buf


def mort(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.6)
    da.peau(rng, buf, 60.0, 0.9, 0.0, 0.3, 1.5, 1.0)
    equipement(rng, buf, 0.0, 4, 0.12)
    c = da.choeur(rng, 0.9, lambda u: N("E3") * (1 + 0.5 * u), "ou", nombre=3, souffle=0.6)
    c = da.passe_bas_variable(env(c, 0.2, 0.3, 0.4), lambda u: 3000.0 * (1 - 0.8 * u))   # l'énergie s'éloigne
    da.ajouter(buf, c, 0.6, 0.45)
    da.scintillement(rng, buf, 0.55, 0.8, da.penta(4, 5), 16, 0.08, (0.1, 0.3), registre=lambda u: u)
    return buf


def potion(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.9)
    da.sinus_glisse(buf, 900.0, 300.0, 0.4, 0.02, 0.0, 0.0005, 0.015, 0.6)             # le bouchon
    for k in range(3):
        t = 0.18 + 0.17 * k
        da.sinus_glisse(buf, 200.0, 480.0, 0.35, 0.05, t, 0.003, 0.04, 0.8)           # glou
        da.ajouter(buf, env(da.passe_bas(da.bruit(rng, da.idx(0.06)), 700.0), 0.005, 0.02, 0.035), t, 0.3)
    da.ajouter(buf, da.souffle(rng, 0.25, 900.0, 500.0, 1.0, 0.05, 0.2, 1.5), 0.65, 0.2)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("joueur_touche_1", E, -15.0, None, lambda: touche(1401)),
    ("joueur_touche_2", E, -15.0, None, lambda: touche(1402)),
    ("esquive_1", E, -16.0, None, lambda: esquive(1403)),
    ("joueur_mort", E, -13.0, None, lambda: mort(1404)),
    ("potion_boire", E, -16.0, None, lambda: potion(1405)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
