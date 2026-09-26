"""Jour, nuit et partie : annonces globales (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Les grandes charnières du cycle, en 2D : peaux de guerre, bourdons qui changent de couleur, chœurs sourds. Le
crépuscule descend d'une quinte vers la nuit, l'aube s'éclaire de mi mineur vers sol majeur ; la vague est un tambour
de guerre d'os ; la victoire, un chœur ouvert en sol majeur avec les gemmes justes de Nyxessa ; la défaite, un bourdon
qui retombe. Détail : § 3.18 du cahier.

Sons écrits (Assets/Audio/Deathless/JourNuit/, WAV 44,1 kHz mono 16 bits, 2D, graines 2501 à 2549) :
  crepuscule   tombée de la nuit (4 s) : deux peaux graves, bourdon qui descend, chœur sourd, grillons qui arrivent
  aube         retour du jour (4 s) : bourdon qui s'éclaire, chœur qui s'ouvre, premiers oiseaux
  vague        une vague est lancée (2 s) : trois coups de peau de guerre, os, terre, cri de chœur
  victoire     la partie est gagnée (6 s) : chœur ouvert, cordes pincées en arpège, peaux, gemmes justes
  defaite      Nyxessa détruite (4 s) : bourdon grave, chœur qui retombe, trois notes de bois sourd
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


def grillons(rng, buf, debut, duree, nombre, amp):
    for _ in range(nombre):
        t0 = debut + rng.uniform(0, duree - 0.4)
        f = rng.uniform(4200.0, 5000.0)
        for k in range(rng.randint(6, 14)):
            da.mode(buf, f, amp * rng.uniform(0.5, 1.0), 0.012, t0 + k / 32.0, 0.001)


def accord(rng, buf, notes, debut, duree, voyelles, amp, nombre=2):
    for f in notes:
        c = da.choeur(rng, duree, f, lambda u: (voyelles[0], voyelles[1], u), nombre=nombre, souffle=0.5)
        da.ajouter(buf, env(c, duree * 0.3, duree * 0.3, duree * 0.4), debut, amp)


def crepuscule(graine):
    rng = random.Random(graine)
    buf = da.tampon(4.0)
    da.peau(rng, buf, 55.0, 0.9, 0.0, 0.9, 1.4, 1.0)
    da.peau(rng, buf, 49.0, 0.8, 0.8, 1.0, 1.4, 1.0)
    b1 = da.bourdon(rng, 2.0, [N("E2"), N("B2")], 0.3, 500.0)
    b2 = da.bourdon(rng, 2.6, [N("A1"), N("E2")], 0.25, 400.0)
    da.ajouter(buf, env(b1, 0.05, 1.2, 0.75), 0.0, 0.4)
    da.ajouter(buf, env(b2, 0.6, 1.0, 1.0), 1.3, 0.45)                                     # une quinte plus bas
    accord(rng, buf, (N("E3"), N("B3")), 0.3, 2.8, ("o", "ou"), 0.25)
    grillons(rng, buf, 2.6, 1.4, 5, 0.08)
    return buf


def aube(graine):
    rng = random.Random(graine)
    buf = da.tampon(4.0)
    da.peau(rng, buf, 82.0, 0.5, 0.0, 0.5, 1.3, 0.5)
    b1 = da.bourdon(rng, 2.0, [N("E2"), N("B2")], 0.3, 500.0)
    b2 = da.bourdon(rng, 2.8, [N("G2"), N("D3")], 0.25, 900.0)
    da.ajouter(buf, env(b1, 0.05, 1.0, 0.95), 0.0, 0.35)
    da.ajouter(buf, env(b2, 0.8, 1.0, 1.0), 1.2, 0.35)                                     # le bourdon s'éclaire
    accord(rng, buf, (N("G3"), N("D4")), 0.8, 3.0, ("o", "a"), 0.25)
    for _ in range(6):                                                                      # premiers oiseaux
        t = rng.uniform(1.8, 3.6)
        f = rng.uniform(2200.0, 3800.0)
        da.sinus_glisse(buf, f, f * rng.uniform(1.15, 1.4), 0.06, 0.07, t, 0.005, 0.05, 0.7)
    return buf


def vague(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.0)
    for k, t in enumerate((0.0, 0.35, 0.7)):
        da.peau(rng, buf, 72.0 - 4 * k, 0.9, t, 0.5, 1.5, 1.2)
        da.cliquetis(rng, buf, t, 0.12, 5, 0.2)
    terre = da.passe_bas(da.bruit_brun(rng, da.idx(1.5)), 180.0)
    da.ajouter(buf, env(terre, 0.3, 0.6, 0.6), 0.2, 0.5)
    c = da.choeur(rng, 0.8, 147.0, lambda u: ("a", "o", u), nombre=4, souffle=0.4, octaves=(1.0, 0.5))
    da.ajouter(buf, env(da.saturer(c, 2.0), 0.02, 0.3, 0.48), 0.7, 0.4)
    return buf


def victoire(graine):
    rng = random.Random(graine)
    buf = da.tampon(6.0)
    b = da.bourdon(rng, 6.0, [N("G1"), N("D2")], 0.25, 700.0)
    da.ajouter(buf, env(b, 0.3, 3.5, 2.2), 0.0, 0.35)
    for k, t in enumerate((0.0, 0.5, 1.0)):
        da.peau(rng, buf, 62.0 + 8 * k, 0.8, t, 0.5, 1.4, 1.0)
    accord(rng, buf, (N("G3"), N("B3"), N("D4"), N("G4")), 0.4, 5.0, ("o", "a"), 0.22, nombre=3)
    for k, n in enumerate(("G3", "B3", "D4", "G4", "B4", "D5")):
        c = da.corde(rng, 1.5, N(n), 1.4, 0.45)
        da.ajouter(buf, env(c, 0.001, 0.3, 1.2), 0.2 + 0.18 * k, 0.25)
    for k, n in enumerate(("E5", "B5")):
        da.gemme(rng, buf, N(n), 0.12, 1.5, 1.6 + 0.3 * k, eclat=0.5)                        # Nyxessa, juste
    return buf


def defaite(graine):
    rng = random.Random(graine)
    buf = da.tampon(4.0)
    b = da.bourdon(rng, 4.0, [N("E1"), N("B1")], 0.2, 300.0)
    da.ajouter(buf, env(b, 0.05, 1.5, 2.45), 0.0, 0.45)
    c = da.choeur(rng, 2.5, lambda u: N("B3") * (1 - 0.3 * u), lambda u: ("o", "ou", u), nombre=3, souffle=0.55)
    da.ajouter(buf, env(c, 0.2, 0.8, 1.5), 0.0, 0.4)
    for k, n in enumerate(("B3", "G3", "E3")):
        da.bois(rng, buf, N(n), 0.35, 0.5, 1.2 + 0.55 * k, clic=0.4, clarte=0.2)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("crepuscule", E, -13.0, 0.3, lambda: crepuscule(2501)),
    ("aube", E, -13.0, 0.3, lambda: aube(2502)),
    ("vague", E, -12.0, None, lambda: vague(2503)),
    ("victoire", E, -11.0, 0.5, lambda: victoire(2504)),
    ("defaite", E, -13.0, 0.4, lambda: defaite(2505)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
