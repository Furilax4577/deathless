"""Sons de l'interface (Deathless, lot 1 du cahier des charges son, 26/09/2026).

Signature de l'interface : le **bois sec**. Un bouton est une petite lame de bois (marimba accordée 1 : 3,93 : 9,24)
frappée d'un clic de bois (bande 2-5 kHz, 1 ms) : très amortie pour la navigation (bloc de bois), plus longue et
chantante pour ce qui se valide (note de marimba). Notes prises dans la gamme commune (mi mineur pentatonique), comme
les gemmes de Nyxessa et les musiques. Pas de gemme ici : le cristal est réservé à Nyxessa. Pas de bip électronique.
Détail et cibles : Docs/son-cahier-des-charges.md.

Sons écrits (Assets/Audio/Deathless/Interface/, WAV 44,1 kHz mono 16 bits, 2D, graines 1201 à 1299) :
  interface_survol_1..2      la sélection passe sur un bouton : tic de bois aigu, 70 ms
  interface_clic_1..2        valider : toc de bois clair, 160 ms
  interface_retour           revenir : deux tocs qui descendent
  interface_refus            action impossible : double toc grave, étouffé, un peu faux
  interface_confirmation     action acceptée : toc, puis deux notes de marimba qui montent (quinte)
  interface_decompte         chaque seconde d'un compte à rebours : bloc de bois net
  interface_onglet           onglet précédent / suivant : frottement bref et tic
  interface_pret             un joueur se déclare prêt : toc et quinte de marimba ensemble
  interface_pret_annule      vote annulé : deux notes qui retombent, plus sombres
  interface_tous_prets       tout le monde est prêt : tambour de bois et arpège montant

Usage : python -B synth_interface.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script).
Catalogue : ids dl_interface_* dans Wiki/data/sons.json.
"""
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note


def survol(graine, n):
    rng = random.Random(graine)
    buf = da.tampon(0.07)
    da.bois(rng, buf, N(n), 0.5, 0.035, 0.0, clic=0.6, clarte=0.7)
    return da.master(buf, -24.0, fondu=0.012)


def clic(graine, n):
    rng = random.Random(graine)
    buf = da.tampon(0.16)
    da.bois(rng, buf, N(n), 0.55, 0.09, 0.0, clic=1.0)
    da.mode(buf, 240.0, 0.18, 0.035, 0.0, 0.0006)      # la planche sous la lame : un peu de corps
    return da.master(buf, -18.0, fondu=0.02)


def retour(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.22)
    da.bois(rng, buf, N("A5"), 0.42, 0.07, 0.0, clic=0.8)
    da.bois(rng, buf, N("E5"), 0.5, 0.08, 0.065, clic=0.7)
    return da.master(buf, -19.0, fondu=0.02)


def refus(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    for t0 in (0.0, 0.1):
        da.bois(rng, buf, N("E4"), 0.6, 0.07, t0, clic=0.5, clarte=0.4)
        da.bois(rng, buf, N("F4"), 0.3, 0.06, t0, clic=0.0, clarte=0.3)   # demi-ton de trop : « non »
        da.ajouter(buf, da.enveloppe(da.passe_bas(da.bruit(rng, da.idx(0.03)), 900.0), 0.0006, 0.003, 0.026, 2.0),
                   t0, 0.3)
    return da.master(buf, -18.0, fondu=0.03)


def confirmation(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    da.bois(rng, buf, N("E5"), 0.35, 0.05, 0.0, clic=1.0)
    da.bois(rng, buf, N("E5"), 0.45, 0.42, 0.004, clic=0.0)
    da.bois(rng, buf, N("B5"), 0.45, 0.48, 0.085, clic=0.5)
    return da.master(buf, -16.0, fondu=0.05)


def decompte(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.25)
    da.bois(rng, buf, N("A5"), 0.55, 0.06, 0.0, clic=1.2, clarte=0.6)
    da.mode(buf, N("A5") * 2.31, 0.18, 0.03, 0.0, 0.0006)               # second mode du bloc de bois (non accordé)
    return da.master(buf, -17.0, fondu=0.03)


def onglet(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.13)
    da.ajouter(buf, da.souffle(rng, 0.06, 2500.0, 5000.0, 1.2, 0.004, 0.05, 1.5), 0.0, 0.35)
    da.bois(rng, buf, N("D7"), 0.25, 0.025, 0.035, clic=0.6, clarte=0.6)
    return da.master(buf, -21.0, fondu=0.02)


def pret(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.45)
    da.bois(rng, buf, N("E5"), 0.3, 0.05, 0.0, clic=1.0)
    da.bois(rng, buf, N("E5"), 0.4, 0.38, 0.003, clic=0.0)
    da.bois(rng, buf, N("B5"), 0.35, 0.36, 0.003, clic=0.0)
    return da.master(buf, -16.0, fondu=0.04)


def pret_annule(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.35)
    da.bois(rng, buf, N("B5"), 0.4, 0.25, 0.0, clic=0.6, clarte=0.6)
    da.bois(rng, buf, N("G5"), 0.45, 0.25, 0.09, clic=0.4, clarte=0.5)
    return da.master(buf, -18.0, fondu=0.04)


def tous_prets(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    da.tambour_bois(rng, buf, 110.0, 0.6, 0.0, 0.28)
    for k, n in enumerate(("E5", "G5", "B5", "E6")):
        da.bois(rng, buf, N(n), 0.4, 0.5, 0.004 + 0.07 * k, clic=0.5)
    da.bois(rng, buf, N("B5"), 0.3, 0.65, 0.29, clic=0.3)
    da.bois(rng, buf, N("E6"), 0.38, 0.7, 0.29, clic=0.3)
    return da.master(buf, -14.0, fondu=0.06)


SONS = [
    ("interface_survol_1", lambda: survol(1201, "B6")),
    ("interface_survol_2", lambda: survol(1202, "D7")),
    ("interface_clic_1", lambda: clic(1211, "A5")),
    ("interface_clic_2", lambda: clic(1212, "B5")),
    ("interface_retour", lambda: retour(1221)),
    ("interface_refus", lambda: refus(1231)),
    ("interface_confirmation", lambda: confirmation(1241)),
    ("interface_decompte", lambda: decompte(1251)),
    ("interface_onglet", lambda: onglet(1261)),
    ("interface_pret", lambda: pret(1271)),
    ("interface_pret_annule", lambda: pret_annule(1281)),
    ("interface_tous_prets", lambda: tous_prets(1291)),
]


def main():
    dossier = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
    noms = sys.argv[2:]
    for nom, fabrique in SONS:
        if noms and nom not in noms:
            continue
        chemin = os.path.join(dossier, nom + ".wav")
        da.ecrire(chemin, fabrique())
        print("écrit", chemin)


if __name__ == "__main__":
    main()
