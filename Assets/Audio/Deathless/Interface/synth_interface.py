"""Sons de l'interface (Deathless, lot 1, regénéré sous la direction sombre le 26/09/2026 au soir).

L'interface reste **sobre et lisible**, mais sombre : du **bois sombre** (lames graves, amorties, peu d'harmoniques
aigus) et de l'**os** (petits tubes creux), plus de cristal ni de carillon. Les notes restent dans la gamme commune
(mi mineur pentatonique), une octave plus bas qu'au premier essai. Le décompte et « tous prêts » prennent une **peau
tendue** et un **bourdon court**. Détail : Docs/son-cahier-des-charges.md (§ 3.1, § 8).

Sons écrits (Assets/Audio/Deathless/Interface/, WAV 44,1 kHz mono 16 bits, 2D, graines 1201 à 1299) :
  interface_survol_1..2      la sélection passe sur un bouton : petit os sec
  interface_clic_1..2        valider : toc de bois sombre et os
  interface_retour           revenir : deux tocs de bois sombre qui descendent
  interface_refus            action impossible : double coup d'os sourd, un peu faux, peau étouffée
  interface_confirmation     action acceptée : toc, puis deux notes de bois sombre qui montent (quinte)
  interface_decompte         chaque seconde d'un compte à rebours : peau tendue courte et os
  interface_onglet           onglet précédent / suivant : frottement sourd et petit os
  interface_pret             un joueur se déclare prêt : peau et quinte de bois sombre
  interface_pret_annule      vote annulé : deux notes de bois sombre qui retombent
  interface_tous_prets       tout le monde est prêt : deux peaux de guerre et un bourdon court

Usage : python -B synth_interface.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_interface_* dans Wiki/data/sons.json.
"""
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note


def survol(graine, f):
    rng = random.Random(graine)
    buf = da.tampon(0.07)
    da.os_creux(rng, buf, f, 0.5, 0.0, 0.025)
    return buf


def clic(graine, n):
    rng = random.Random(graine)
    buf = da.tampon(0.16)
    da.bois(rng, buf, N(n), 0.55, 0.08, 0.0, clic=0.6, clarte=0.35)
    da.os_creux(rng, buf, 720.0, 0.25, 0.0, 0.03)
    da.mode(buf, 150.0, 0.2, 0.04, 0.0, 0.0006)
    return buf


def retour(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.22)
    da.bois(rng, buf, N("B4"), 0.45, 0.07, 0.0, clic=0.5, clarte=0.35)
    da.bois(rng, buf, N("E4"), 0.5, 0.08, 0.065, clic=0.5, clarte=0.3)
    return buf


def refus(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    for t0 in (0.0, 0.1):
        da.os_creux(rng, buf, 300.0, 0.5, t0, 0.05)
        da.os_creux(rng, buf, 318.0, 0.3, t0, 0.045)                                     # un demi-ton de trop
        da.peau(rng, buf, 95.0, 0.35, t0, 0.08, 1.2, 0.3)
    return buf


def confirmation(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    da.bois(rng, buf, N("E4"), 0.35, 0.05, 0.0, clic=0.8, clarte=0.35)
    da.bois(rng, buf, N("E4"), 0.45, 0.4, 0.004, clic=0.0, clarte=0.3)
    da.bois(rng, buf, N("B4"), 0.45, 0.45, 0.085, clic=0.4, clarte=0.3)
    da.peau(rng, buf, 110.0, 0.2, 0.0, 0.15, 1.2, 0.2)
    return buf


def decompte(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.25)
    da.peau(rng, buf, 140.0, 0.6, 0.0, 0.16, 1.35, 0.9)
    da.os_creux(rng, buf, 900.0, 0.2, 0.0, 0.02)
    return buf


def onglet(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.13)
    da.ajouter(buf, da.souffle(rng, 0.06, 900.0, 2000.0, 1.0, 0.004, 0.05, 1.5), 0.0, 0.3)
    da.os_creux(rng, buf, 1100.0, 0.25, 0.035, 0.02)
    return buf


def pret(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.45)
    da.peau(rng, buf, 110.0, 0.4, 0.0, 0.2, 1.3, 0.6)
    da.bois(rng, buf, N("E4"), 0.4, 0.36, 0.003, clic=0.3, clarte=0.3)
    da.bois(rng, buf, N("B4"), 0.35, 0.34, 0.003, clic=0.0, clarte=0.3)
    return buf


def pret_annule(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.35)
    da.bois(rng, buf, N("B4"), 0.4, 0.22, 0.0, clic=0.4, clarte=0.3)
    da.bois(rng, buf, N("G4"), 0.45, 0.22, 0.09, clic=0.3, clarte=0.25)
    return buf


def tous_prets(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    da.peau(rng, buf, 65.0, 0.8, 0.0, 0.5, 1.5, 1.0)                                      # peaux de guerre
    da.peau(rng, buf, 98.0, 0.6, 0.14, 0.4, 1.4, 0.8)
    b = da.bourdon(rng, 0.85, [N("E2"), N("B2")], 0.4, 600.0)
    da.ajouter(buf, da.enveloppe(b, 0.03, 0.3, 0.52, 1.3), 0.12, 0.35)
    da.bois(rng, buf, N("E4"), 0.25, 0.5, 0.14, clic=0.2, clarte=0.3)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("interface_survol_1", 1, -24.0, 0.012, lambda: survol(1201, 980.0)),
    ("interface_survol_2", 1, -24.0, 0.012, lambda: survol(1202, 1100.0)),
    ("interface_clic_1", 1, -18.0, 0.02, lambda: clic(1211, "E4")),
    ("interface_clic_2", 1, -18.0, 0.02, lambda: clic(1212, "G4")),
    ("interface_retour", 1, -19.0, 0.02, lambda: retour(1221)),
    ("interface_refus", 1, -18.0, 0.03, lambda: refus(1231)),
    ("interface_confirmation", 1, -16.0, 0.05, lambda: confirmation(1241)),
    ("interface_decompte", 1, -17.0, 0.03, lambda: decompte(1251)),
    ("interface_onglet", 1, -21.0, 0.02, lambda: onglet(1261)),
    ("interface_pret", 1, -16.0, 0.04, lambda: pret(1271)),
    ("interface_pret_annule", 1, -18.0, 0.04, lambda: pret_annule(1281)),
    ("interface_tous_prets", 1, -14.0, 0.06, lambda: tous_prets(1291)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
