"""Sons de Nyxessa, la relique (Deathless, lots 1 et 2 du cahier des charges son, 26/09/2026).

Signature de Nyxessa : le **tintement de gemme** (petite barre de cristal, partiels 1 : 2,756 : 5,404 : 8,933, chacun
doublé d'un jumeau désaccordé de 0,6 à 3 Hz), accordé sur la gamme commune (mi mineur pentatonique), porté par une
poussée grave (l'énergie) et des souffles d'air filtrés (la charge qui voyage). Rien de métallique, rien de militaire :
du cristal, de l'air et une masse douce. Détail et cibles : Docs/son-cahier-des-charges.md.

Sons écrits (Assets/Audio/Deathless/Nyxessa/, WAV 44,1 kHz mono 16 bits, graines 1101 à 1199) :
  nyxessa_tir_1..3          tir d'un missile : le cristal pulse, un accord de deux gemmes, l'air part (3D)
  nyxessa_frappee_1..3      un ennemi frappe la relique : coup sourd, gemme grave qui frissonne faux (3D)
  nyxessa_alerte            relique attaquée, rappel imminent au donjon : deux fois deux notes descendantes (2D)
  nyxessa_palier            palier acheté : arpège montant, accord ouvert, gerbe d'étincelles (3D, événement majeur)
  nyxessa_charge_portail    à l'aube, la charge part vers le portail : souffle et gemmes qui montent (3D)
  nyxessa_retour_energie    au crépuscule, l'énergie revient : souffle qui retombe, gemme grave qui absorbe (3D)
  nyxessa_onde              onde de la ceinture (passage au portail, mort d'un allié) : course de gemmes (3D)
  nyxessa_destruction       défaite : fêlure, bris en cascade, dernier soupir grave (3D, événement majeur)
Lot 2 (26/09/2026) :
  nyxessa_missile_vol_boucle   vol du missile en crâne (boucle 1,5 s) : souffle qui palpite, gemme tenue (3D)
  nyxessa_missile_eclat_1..3   le missile éclate : choc de verre, gerbe de gemmes qui monte (3D)
  nyxessa_rappel               joueur ramené de force du donjon : accord tendu, aspiration, arrivée (2D)
  nyxessa_reapparition         un joueur renaît près de Nyxessa : gemmes qui convergent, accord, pas (3D)

Usage : python -B synth_nyxessa.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_nyxessa_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
AIGU = da.penta(6, 7)          # mi6 à ré7 : les gemmes de la relique
TRES_AIGU = da.penta(7, 8)[:6]  # étincelles


def tir(graine, accord):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    da.sub(buf, 110.0, 52.0, 0.3, 0.2)                                   # le cristal pulse et recule
    da.ajouter(buf, da.souffle(rng, 0.26, 700.0, 5200.0, 1.4, 0.012, 0.22, 2.0), 0.0, 0.55)  # l'éclat part
    da.gemme(rng, buf, N(accord[0]), 0.5, 0.75, 0.002)
    da.gemme(rng, buf, N(accord[1]), 0.34, 0.6, 0.012)
    da.scintillement(rng, buf, 0.02, 0.16, TRES_AIGU, 5, 0.1)
    return buf


def frappee(graine, base):
    rng = random.Random(graine)
    buf = da.tampon(0.75)
    da.mode(buf, 170.0 * rng.uniform(0.95, 1.05), 0.55, 0.09, 0.0, 0.0008)   # coup sourd (os, bois, pierre)
    da.mode(buf, 320.0 * rng.uniform(0.95, 1.05), 0.25, 0.05, 0.0, 0.0008)
    da.ajouter(buf, da.enveloppe(da.passe_bas(da.bruit(rng, da.idx(0.05)), 1800.0), 0.0008, 0.004, 0.045, 2.0),
               0.0, 0.9)
    f = N(base)
    da.gemme(rng, buf, f, 0.42, 0.55, 0.004, eclat=0.8)                       # la gemme résonne…
    da.gemme(rng, buf, f * 1.059, 0.24, 0.42, 0.004, eclat=0.6, durete=0.0)  # … un demi-ton trop haut : elle a mal
    da.scintillement(rng, buf, 0.01, 0.12, AIGU, 4, 0.09, (0.06, 0.16))       # éclats qui sautent
    return buf


def alerte(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.05)
    for t0, force in ((0.0, 0.85), (0.4, 1.0)):
        da.gemme(rng, buf, N("B6"), 0.5 * force, 0.4, t0, eclat=1.1)
        da.gemme(rng, buf, N("E6"), 0.55 * force, 0.5, t0 + 0.13, eclat=1.1)
        da.sub(buf, 82.4, 70.0, 0.28 * force, 0.16, t0)                    # battement grave : le cœur de la relique
    return buf


def palier(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.5)
    da.sub(buf, 55.0, 82.4, 0.2, 0.9)
    for k, n in enumerate(("E6", "G6", "B6", "E7")):
        da.gemme(rng, buf, N(n), 0.34 + 0.04 * k, 0.6, 0.09 * k)
    for n, a in (("E6", 0.42), ("B6", 0.34), ("E7", 0.3)):
        da.gemme(rng, buf, N(n), a, 1.7, 0.45, eclat=1.2)
    da.ajouter(buf, da.souffle(rng, 1.3, 1500.0, 7000.0, 1.6, 0.25, 1.0, 1.8), 0.3, 0.22)
    da.scintillement(rng, buf, 0.42, 1.4, TRES_AIGU + AIGU[-4:], 30, 0.13, (0.15, 0.45), densite=lambda u: u ** 1.7)
    return buf


def charge_portail(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.95)
    da.ajouter(buf, da.souffle(rng, 1.9, 300.0, 4200.0, 1.3, 1.25, 0.62, 1.4), 0.0, 0.75)
    da.sinus_glisse(buf, 58.0, 118.0, 0.22, 1.7, 0.0, 0.3, 0.5, 1.0)
    notes = da.penta(5, 7)
    da.scintillement(rng, buf, 0.0, 1.55, notes, 42, 0.14, (0.1, 0.3),
                     densite=lambda u: u ** 0.55, registre=lambda u: u)
    da.gemme(rng, buf, N("B6"), 0.3, 0.5, 0.004)                         # départ net, pas de silence en tête
    return buf


def retour_energie(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.95)
    da.ajouter(buf, da.souffle(rng, 1.45, 4200.0, 350.0, 1.3, 0.2, 1.2, 1.2), 0.0, 0.7)
    notes = da.penta(5, 7)
    da.scintillement(rng, buf, 0.0, 1.3, notes, 36, 0.13, (0.1, 0.3),
                     densite=lambda u: u ** 1.8, registre=lambda u: 1.0 - u)
    da.gemme(rng, buf, N("B6"), 0.26, 0.4, 0.004)
    da.gemme(rng, buf, N("E5"), 0.5, 0.9, 1.35, eclat=0.7)                 # la relique absorbe
    da.sub(buf, 110.0, 55.0, 0.42, 0.5, 1.35)
    return buf


def onde(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.15)
    notes = [f for f in da.penta(5, 7) if f >= da.note("A5")][:10]
    for k, f in enumerate(notes):
        u = k / (len(notes) - 1)
        arc = 0.55 + 0.45 * (1 - abs(2 * u - 1))                             # l'onde enfle puis passe
        da.gemme(rng, buf, f, 0.22 * arc, 0.45, 0.045 * k, eclat=0.8, jumeau=False, durete=0.5)
    da.ajouter(buf, da.souffle(rng, 0.55, 1800.0, 4200.0, 1.5, 0.15, 0.38, 1.5), 0.0, 0.18)
    return buf


def destruction(graine):
    rng = random.Random(graine)
    buf = da.tampon(3.9)
    fele = da.passe_haut(da.bruit(rng, da.idx(0.08)), 800.0)                # la fêlure
    da.ajouter(buf, da.enveloppe(fele, 0.0006, 0.006, 0.07, 2.5), 0.0, 1.0)
    da.choc(rng, buf, 0.0, 0.9, 0.004, 2500.0, 0.5)
    da.sub(buf, 92.0, 34.0, 0.6, 1.7)                                         # la masse s'effondre
    eclats = [rng.uniform(1100.0, 6500.0) for _ in range(40)]                  # bris : hors gamme, chaotique
    eclats.sort()
    da.scintillement(rng, buf, 0.0, 1.7, eclats, 95, 0.2, (0.08, 0.5),
                     densite=lambda u: u ** 2.2, registre=lambda u: 1.0 - 0.7 * u)
    da.gemme(rng, buf, N("E4"), 0.42, 2.6, 0.9, eclat=0.6)                     # dernier soupir de la relique
    da.gemme(rng, buf, N("B4"), 0.22, 2.2, 0.92, eclat=0.5, durete=0.0)
    for k, n in enumerate(("B5", "G5", "E5")):
        da.gemme(rng, buf, N(n), 0.16, 1.2, 1.8 + 0.4 * k, eclat=0.5, durete=0.2)
    poussiere = da.passe_bas(da.bruit(rng, da.idx(2.2)), 420.0)
    da.ajouter(buf, da.enveloppe(poussiere, 0.05, 0.2, 1.95, 1.6), 0.05, 0.5)
    return buf


# --- Lot 2 ---------------------------------------------------------------------------------------------------------
def missile_vol(graine, base="B5"):
    """Boucle de 1,5 s : souffle serré qui palpite à 8 Hz (12 cycles), frisson de gemme tenu (fondamental et partiel
    2,756 avec leurs jumeaux à 1,33 Hz, 2 battements par boucle). Pas d'attaque : le vol est continu."""
    rng = random.Random(graine)
    duree, fondu = 1.5, 0.2
    n = da.idx(duree + fondu)
    air = da.passe_bande(da.bruit(rng, n), 2200.0, 1.3)
    air = [v * (0.65 + 0.35 * math.sin(2 * math.pi * 8.0 * i / da.RATE)) for i, v in enumerate(air)]
    air = da.fondre_boucle([v * 0.5 for v in air], duree, fondu)
    chant = [0.0] * da.idx(duree)
    f = N(base)
    for r, a in ((1.0, 0.16), (2.756, 0.07)):
        for ecart, ph in ((0.0, 0.0), (1.3333, 1.7)):
            fb = round((f * r + ecart) * duree) / duree         # nombre entier de périodes : jointure exacte
            w = 2 * math.pi * fb / da.RATE
            for i in range(len(chant)):
                chant[i] += a * math.sin(w * i + ph)
    return [x + y for x, y in zip(air, chant)]


def missile_eclat(graine, registre_bas):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    da.choc(rng, buf, 0.0, 0.8, 0.0015, 4000.0, 0.7)                        # choc de verre
    notes = da.penta(5, 7)
    da.scintillement(rng, buf, 0.0, 0.18, notes, 15, 0.3, (0.1, 0.25),
                     densite=lambda u: u ** 1.6, registre=lambda u: registre_bas + (1 - registre_bas) * u)
    da.sub(buf, 120.0, 60.0, 0.3, 0.14)
    return buf


def rappel(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.8)
    da.gemme(rng, buf, N("E6"), 0.4, 0.9, 0.0)
    da.gemme(rng, buf, N("F6"), 0.32, 0.8, 0.0, durete=0.0)                  # un demi-ton : c'est une punition
    da.ajouter(buf, da.souffle(rng, 0.6, 500.0, 6000.0, 1.5, 0.45, 0.15, 1.5), 0.2, 0.7)   # aspiré vers la relique
    da.scintillement(rng, buf, 0.25, 0.55, da.penta(6, 7), 18, 0.12, (0.08, 0.2),
                     densite=lambda u: u ** 0.6, registre=lambda u: u)
    da.gemme(rng, buf, N("E5"), 0.5, 0.8, 0.95, eclat=0.7)                   # arrivée au village
    da.sub(buf, 110.0, 55.0, 0.35, 0.45, 0.95)
    da.scintillement(rng, buf, 0.95, 0.35, da.penta(5, 6), 10, 0.1, (0.1, 0.3), registre=lambda u: 1 - u)
    return buf


def reapparition(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    da.ajouter(buf, da.souffle(rng, 0.8, 5000.0, 800.0, 1.4, 0.35, 0.45, 1.3), 0.0, 0.5)   # l'énergie converge
    da.gemme(rng, buf, N("E7"), 0.18, 0.3, 0.003, eclat=0.6, jumeau=False, durete=0.4)
    da.scintillement(rng, buf, 0.0, 0.8, da.penta(6, 7), 28, 0.13, (0.08, 0.22),
                     densite=lambda u: u ** 0.7, registre=lambda u: 1.0 - 0.6 * u)
    da.gemme(rng, buf, N("E5"), 0.42, 0.6, 0.8, eclat=0.8)                   # le corps est là
    da.gemme(rng, buf, N("B5"), 0.3, 0.55, 0.8, eclat=0.8, durete=0.0)
    da.pas_pierre(rng, buf, 0.95, 0.35)                                      # un pas sur la pierre
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s ou None, fabrique)
SONS = [
    ("nyxessa_tir_1", 1, -14.0, None, lambda: tir(1101, ("E6", "B6"))),
    ("nyxessa_tir_2", 1, -14.0, None, lambda: tir(1102, ("G6", "D7"))),
    ("nyxessa_tir_3", 1, -14.0, None, lambda: tir(1103, ("A6", "E7"))),
    ("nyxessa_frappee_1", 1, -15.0, None, lambda: frappee(1111, "E5")),
    ("nyxessa_frappee_2", 1, -15.0, None, lambda: frappee(1112, "D5")),
    ("nyxessa_frappee_3", 1, -15.0, None, lambda: frappee(1113, "G5")),
    ("nyxessa_alerte", 1, -13.0, None, lambda: alerte(1121)),
    ("nyxessa_palier", 1, -12.5, None, lambda: palier(1131)),
    ("nyxessa_charge_portail", 1, -14.0, None, lambda: charge_portail(1141)),
    ("nyxessa_retour_energie", 1, -14.0, None, lambda: retour_energie(1151)),
    ("nyxessa_onde", 1, -17.0, None, lambda: onde(1161)),
    ("nyxessa_destruction", 1, -12.5, None, lambda: destruction(1171)),
    ("nyxessa_missile_vol_boucle", 2, -17.0, da.BOUCLE, lambda: missile_vol(1181)),
    ("nyxessa_missile_eclat_1", 2, -15.0, None, lambda: missile_eclat(1182, 0.2)),
    ("nyxessa_missile_eclat_2", 2, -15.0, None, lambda: missile_eclat(1183, 0.3)),
    ("nyxessa_missile_eclat_3", 2, -15.0, None, lambda: missile_eclat(1184, 0.1)),
    ("nyxessa_rappel", 2, -13.0, None, lambda: rappel(1185)),
    ("nyxessa_reapparition", 2, -14.0, None, lambda: reapparition(1186)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
