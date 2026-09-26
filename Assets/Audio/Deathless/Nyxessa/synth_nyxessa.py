"""Sons de Nyxessa, la relique (Deathless, lots 1 et 2, regénérés sous la direction sombre le 26/09/2026 au soir).

Nyxessa est une **âme captive** qui ressuscite les héros, pas une boîte à musique. Chaque son porte une **vocalise**
(voix fantôme par formants : impulsions glottiques, formants de Klatt, souffle ; voyelles sombres « ou », « o » pour
la plainte, « a » ouvert pour l'appel et le cri), posée sur un **bourdon** grave ou un **chœur sourd**. Le tintement
de gemme reste la signature de la relique, mais une octave plus bas (mi4 à si5), assourdi, et jamais seul. Référence
de caractère : l'ancien missile magique de Relic (plaintes des limbes pendant le vol, cri grave à l'éclat).
Détail : Docs/son-cahier-des-charges.md (§ 1, § 2, § 3.2, § 8).

Sons écrits (Assets/Audio/Deathless/Nyxessa/, WAV 44,1 kHz mono 16 bits, graines 1101 à 1199) :
  nyxessa_tir_1..3             tir d'un missile : appel bref qui monte, gemme sombre, poussée grave (3D)
  nyxessa_frappee_1..3         un ennemi frappe la relique : coup sourd de peau, plainte courte qui retombe (3D)
  nyxessa_alerte               relique attaquée, rappel au donjon : deux cris d'appel sur un battement de peau (2D)
  nyxessa_palier               palier acheté : un chœur qui s'ouvre (« ou » → « a ») sur le bourdon (3D)
  nyxessa_charge_portail       à l'aube : glissando de voix qui monte, chœur à l'octave, bourdon, souffle (3D)
  nyxessa_retour_energie       au crépuscule : glissando qui redescend, la relique absorbe (peau, gemme grave) (3D)
  nyxessa_onde                 onde de la ceinture : souffle de chœur bref, course de gemmes sombres (3D)
  nyxessa_destruction          défaite : fêlure, long cri qui se déchire et s'éteint dans le bourdon (3D)
  nyxessa_missile_vol_boucle   vol du missile crâne (boucle 2 s) : chœur de plaintes des limbes, os creux qui siffle
  nyxessa_missile_eclat_1..3   éclat du missile : cri bref et déchirant, gemmes sombres, souffle (3D)
  nyxessa_rappel               joueur ramené de force : un appel qui s'éloigne, puis une voix qui revient (2D)
  nyxessa_reapparition         un joueur renaît : des voix lointaines se rapprochent et se posent, pas sur la pierre (3D)

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
SOMBRES = da.penta(4, 5)        # mi4 à ré6 : les gemmes de la relique, assombries


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


def appel(rng, duree, f0, f1, voyelle=("o", "a"), souffle=0.35, rauque=0.0, sous_harm=0.0, montee=0.25):
    """Vocalise d'appel : la voix monte de f0 à f1 sur `montee` de la durée, tient, puis retombe d'un ton."""
    def f(u):
        if u < montee:
            return f0 + (f1 - f0) * (u / montee) ** 0.7
        return f1 * (1 - 0.1 * ((u - montee) / (1 - montee)) ** 1.5)
    a, b = voyelle
    return da.voix(rng, duree, f, lambda u: (a, b, min(1.0, u * 3)), souffle=souffle, rauque=rauque,
                   vibrato=(5.5, 0.018), sous_harm=sous_harm)


def cri(rng, duree, pic, octaves=((1.0, 1.0, 0.45, 0.55), (0.5, 0.8, 0.65, 0.75)), saturation=2.8, tenue=0.35):
    """Cri déchirant (modèle du cri grave de Relic) : la voix jaillit de 120 Hz au pic en 10 % de la durée, tremble
    en poussant, puis s'effondre de 65 % ; voyelle « o » → « a » → « o », raucité et sous-harmonique (la voix se
    casse), deux octaves superposées, saturation douce."""
    def contour(u):
        if u < 0.1:
            return 120 + (pic - 120) * (u / 0.1) ** 0.7
        if u < tenue:
            return pic * (1 + 0.04 * math.sin(u * 60))
        return pic * (1 - 0.65 * ((u - tenue) / (1 - tenue)) ** 1.1)

    n = da.idx(duree)
    res = [0.0] * n
    for octave, gain, sous, rauque in octaves:
        v = da.voix(rng, duree, lambda u, o=octave: contour(u) * o,
                    lambda u: ("o", "a", min(1.0, u * 5)) if u < 0.55 else ("a", "o", min(1.0, (u - 0.55) * 2.2)),
                    souffle=0.45, rauque=rauque, gigue=0.035, vibrato=(6.5, 0.02), sous_harm=sous)
        v = da.saturer(v, saturation)
        v = env(v, 0.004, duree * 0.3, duree * 0.7, 0.9)
        for i in range(n):
            res[i] += gain * v[i]
    return res


def tir(graine, f_voix, accord):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    da.ajouter(buf, env(appel(rng, 0.5, f_voix, f_voix * 1.5, montee=0.3), 0.006, 0.12, 0.37), 0.0, 0.55)
    da.sub(buf, 90.0, 45.0, 0.45, 0.25)                                            # la relique pulse
    da.peau(rng, buf, 62.0, 0.35, 0.0, 0.25, 1.3, 0.4)
    da.gemme(rng, buf, N(accord[0]), 0.22, 0.5, 0.004, eclat=0.35, durete=0.3)      # gemme sombre, sous la voix
    da.gemme(rng, buf, N(accord[1]), 0.16, 0.45, 0.015, eclat=0.35, durete=0.0)
    da.ajouter(buf, da.souffle(rng, 0.3, 400.0, 2500.0, 1.2, 0.02, 0.26, 1.8), 0.0, 0.3)
    return buf


def frappee(graine, f_voix):
    rng = random.Random(graine)
    buf = da.tampon(0.75)
    da.peau(rng, buf, 78.0 * rng.uniform(0.9, 1.1), 0.7, 0.0, 0.2, 1.5, 1.0)        # coup sourd
    da.os_creux(rng, buf, 380.0 * rng.uniform(0.9, 1.1), 0.25, 0.0, 0.05)
    plainte = da.voix(rng, 0.55, lambda u: f_voix * (1 - 0.25 * u), lambda u: ("o", "ou", u),
                      souffle=0.5, rauque=0.25, vibrato=(6.0, 0.025))
    da.ajouter(buf, env(plainte, 0.015, 0.1, 0.43), 0.02, 0.5)
    da.gemme(rng, buf, N("E4"), 0.14, 0.4, 0.004, eclat=0.3, durete=0.0)
    da.gemme(rng, buf, N("F4"), 0.1, 0.35, 0.004, eclat=0.3, durete=0.0)            # un demi-ton faux : elle a mal
    return buf


def alerte(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.05)
    for t0, (f0, f1), force in ((0.0, (294.0, 392.0), 0.85), (0.45, (330.0, 440.0), 1.0)):
        v = appel(rng, 0.45, f0, f1, voyelle=("a", "a"), souffle=0.25, rauque=0.15, montee=0.3)
        da.ajouter(buf, env(v, 0.004, 0.12, 0.33), t0, 0.6 * force)
        da.peau(rng, buf, 82.4, 0.5 * force, t0, 0.3, 1.3, 0.6)                      # battement de cœur grave
    b = da.bourdon(rng, 1.0, [N("E2"), N("B2")], 0.3, 500.0)
    da.ajouter(buf, env(b, 0.01, 0.4, 0.59), 0.0, 0.2)
    return buf


def palier(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.5)
    ch = da.choeur(rng, 2.3, N("E3"), lambda u: ("ou", "a", min(1.0, u * 1.6)), nombre=4, octaves=(1.0, 2.0))
    da.ajouter(buf, env(ch, 0.25, 1.2, 0.85, 1.2), 0.0, 0.6)                       # le chœur s'ouvre
    b = da.bourdon(rng, 2.5, [N("E1"), N("B1")], 0.25, 400.0)
    da.ajouter(buf, env(b, 0.4, 1.3, 0.8), 0.0, 0.35)
    da.peau(rng, buf, 55.0, 0.5, 0.0, 0.5, 1.3, 0.5)
    for k, n in enumerate(("E4", "G4", "B4", "E5")):
        da.gemme(rng, buf, N(n), 0.12, 0.8, 0.3 + 0.12 * k, eclat=0.4, durete=0.2)
    return buf


def glissando(graine, f0, f1, voyelles, absorbe):
    rng = random.Random(graine)
    buf = da.tampon(1.95)
    courbe = (lambda u: f0 * (f1 / f0) ** (u ** 0.8))
    v = da.voix(rng, 1.7, courbe, lambda u: (voyelles[0], voyelles[1], u), souffle=0.4, vibrato=(5.0, 0.02))
    da.ajouter(buf, env(v, 0.01 if not absorbe else 0.02, 0.9, 0.78, 1.2), 0.0, 0.55)
    c = da.choeur(rng, 1.7, lambda u: courbe(u) * 0.5, "ou", nombre=2, souffle=0.55)
    da.ajouter(buf, env(c, 0.2, 0.8, 0.7), 0.0, 0.35)
    b = da.bourdon(rng, 1.95, [N("E1"), N("B1")], 0.3, 350.0)
    da.ajouter(buf, env(b, 0.05, 1.2, 0.7), 0.0, 0.3)
    if absorbe:
        da.ajouter(buf, da.souffle(rng, 1.4, 3000.0, 300.0, 1.2, 0.2, 1.2, 1.3), 0.0, 0.25)
        da.peau(rng, buf, 55.0, 0.6, 1.4, 0.45, 1.3, 0.5)                          # la relique absorbe
        da.gemme(rng, buf, N("E4"), 0.25, 0.5, 1.4, eclat=0.3, durete=0.2)
    else:
        da.ajouter(buf, da.souffle(rng, 1.6, 300.0, 3000.0, 1.2, 1.1, 0.5, 1.3), 0.0, 0.25)
        da.peau(rng, buf, 62.0, 0.45, 0.0, 0.35, 1.4, 0.5)
    return buf


def onde(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.15)
    c = da.choeur(rng, 0.9, N("E3"), lambda u: ("ou", "o", u), nombre=3, souffle=0.6)
    da.ajouter(buf, env(c, 0.12, 0.2, 0.58), 0.0, 0.5)
    notes = [f for f in SOMBRES if f >= N("A4")][:8]
    for k, f in enumerate(notes):
        da.gemme(rng, buf, f, 0.12, 0.35, 0.05 * k, eclat=0.35, jumeau=False, durete=0.3)
    da.ajouter(buf, da.souffle(rng, 0.6, 800.0, 2000.0, 1.3, 0.15, 0.42, 1.5), 0.0, 0.2)
    return buf


def destruction(graine):
    rng = random.Random(graine)
    buf = da.tampon(3.9)
    fele = da.passe_haut(da.bruit(rng, da.idx(0.08)), 700.0)
    da.ajouter(buf, env(fele, 0.0006, 0.006, 0.07, 2.5), 0.0, 0.8)
    da.peau(rng, buf, 48.0, 0.9, 0.0, 0.9, 1.5, 1.0)
    da.ajouter(buf, cri(rng, 2.4, 262.0, octaves=((1.0, 1.0, 0.4, 0.5), (0.5, 0.85, 0.6, 0.7), (0.25, 0.4, 0.5, 0.9)),
                        tenue=0.3), 0.02, 0.55)                                    # le long cri
    eclats = sorted(rng.uniform(300.0, 2000.0) for _ in range(30))
    da.scintillement(rng, buf, 0.0, 1.4, eclats, 45, 0.1, (0.1, 0.4), densite=lambda u: u ** 2.2,
                     registre=lambda u: 1.0 - 0.7 * u)
    b = da.bourdon(rng, 3.4, [N("E1"), N("E2")], 0.2, 300.0)                         # le cri s'éteint dans le bourdon
    da.ajouter(buf, env(b, 1.2, 0.8, 1.4, 1.2), 0.5, 0.45)
    da.gemme(rng, buf, N("E3"), 0.25, 2.2, 1.6, eclat=0.3, durete=0.0)
    return buf


def missile_vol(graine, f_choeur=(82.4, 98.0, 123.5), duree=2.0):
    """Boucle : chœur grave presque immobile (trois voix « ou/o », souffle 0,5, amplitude qui respire à 0,5 Hz, un cycle
    par boucle), plaintes des limbes (quatre glissandos de 0,6 à 0,9 s qui retombent, repliés sur la boucle), os creux
    qui siffle (bruit en bande étroite vers 1,1 kHz, qui ondule), le tout passé sous 4,5 kHz."""
    rng = random.Random(graine)
    fondu = 0.3
    n = da.idx(duree + fondu)
    fond = [0.0] * n
    for f0 in f_choeur:
        v = da.voix(rng, duree + fondu, f0, lambda u: ("ou", "o", 0.5 + 0.5 * math.sin(u * 4)), souffle=0.5,
                    gigue=0.01, vibrato=(4.5, 0.012))
        for i in range(n):
            fond[i] += 0.3 * v[i] * (0.65 + 0.35 * math.sin(2 * math.pi * 0.5 * i / da.RATE))
    siffle = da.souffle_module(rng, duree + fondu, 1100.0, 7.0, 1.0, 0.6, 0.08)
    fond = [a + 0.25 * b for a, b in zip(fond, siffle)]
    fond = da.fondre_boucle(fond, duree, fondu)
    plaintes = da.tampon(duree + 1.0)
    for k in range(4):
        d = rng.uniform(0.6, 0.9)
        f0 = rng.uniform(180.0, 260.0)
        p = da.voix(rng, d, lambda u, f=f0: f * (1 - 0.3 * u ** 1.3), lambda u: ("o", "ou", u), souffle=0.55,
                    rauque=0.15, vibrato=(5.5, 0.03))
        da.ajouter(plaintes, env(p, 0.08, d * 0.3, d * 0.6), k * duree / 4 + rng.uniform(0, 0.15), 0.35)
    res = [a + b for a, b in zip(fond, da.plier(plaintes, duree))]
    return da.circulaire(lambda x: da.passe_bas(da.passe_bas(x, 4500.0), 5500.0), res)


def missile_eclat(graine, pic):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    crac = da.passe_bande(da.bruit(rng, da.idx(0.06)), lambda u: 2800 - 1500 * u, 1.1)
    da.ajouter(buf, [v * math.exp(-5 * i / len(crac)) for i, v in enumerate(crac)], 0.0, 0.8)   # le crâne éclate
    da.ajouter(buf, cri(rng, 0.7, pic, tenue=0.25), 0.0, 0.6)
    da.peau(rng, buf, 60.0, 0.45, 0.0, 0.3, 1.5, 0.4)
    eclats = sorted(rng.uniform(350.0, 1600.0) for _ in range(12))
    da.scintillement(rng, buf, 0.0, 0.25, eclats, 10, 0.12, (0.08, 0.25))
    da.ajouter(buf, da.souffle(rng, 0.6, 1500.0, 300.0, 1.1, 0.02, 0.55, 1.4), 0.05, 0.3)
    return buf


def rappel(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.8)
    v = appel(rng, 0.9, 294.0, 392.0, voyelle=("a", "a"), souffle=0.3, montee=0.2)
    v = da.passe_bas_variable(env(v, 0.005, 0.2, 0.69), lambda u: 5000.0 * (1 - 0.9 * u))  # l'appel s'éloigne
    da.ajouter(buf, v, 0.0, 0.6)
    da.ajouter(buf, da.souffle(rng, 0.6, 500.0, 4000.0, 1.4, 0.45, 0.15, 1.5), 0.2, 0.4)
    r = da.voix(rng, 0.8, lambda u: 147.0 + 73.0 * u ** 0.6, lambda u: ("ou", "o", u), souffle=0.4)
    da.ajouter(buf, env(r, 0.05, 0.3, 0.45), 0.95, 0.5)                             # une voix revient, au village
    da.peau(rng, buf, 55.0, 0.5, 0.95, 0.4, 1.3, 0.5)
    da.gemme(rng, buf, N("E4"), 0.2, 0.6, 0.95, eclat=0.3, durete=0.2)
    return buf


def reapparition(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    c = da.choeur(rng, 0.95, lambda u: N("E3") * (1.5 - 0.5 * u), lambda u: ("ou", "o", u), nombre=3, souffle=0.55)
    c = da.passe_bas_variable(c, lambda u: 600.0 + 3400.0 * u)                        # des voix lointaines se rapprochent
    c = [v * (0.15 + 0.85 * (i / len(c)) ** 1.5) for i, v in enumerate(c)]
    da.ajouter(buf, c, 0.003, 0.7)
    da.ajouter(buf, da.souffle(rng, 0.8, 3000.0, 600.0, 1.3, 0.3, 0.45, 1.3), 0.0, 0.3)
    da.gemme(rng, buf, N("E4"), 0.26, 0.5, 0.85, eclat=0.35)
    da.gemme(rng, buf, N("B4"), 0.18, 0.45, 0.85, eclat=0.35, durete=0.0)
    da.peau(rng, buf, 70.0, 0.3, 0.85, 0.25, 1.3, 0.3)
    da.pas_pierre(rng, buf, 0.97, 0.35)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("nyxessa_tir_1", 1, -14.0, None, lambda: tir(1101, 196.0, ("E5", "B5"))),
    ("nyxessa_tir_2", 1, -14.0, None, lambda: tir(1102, 220.0, ("G5", "D5"))),
    ("nyxessa_tir_3", 1, -14.0, None, lambda: tir(1103, 247.0, ("A4", "E5"))),
    ("nyxessa_frappee_1", 1, -15.0, None, lambda: frappee(1111, 247.0)),
    ("nyxessa_frappee_2", 1, -15.0, None, lambda: frappee(1112, 220.0)),
    ("nyxessa_frappee_3", 1, -15.0, None, lambda: frappee(1113, 262.0)),
    ("nyxessa_alerte", 1, -13.0, None, lambda: alerte(1121)),
    ("nyxessa_palier", 1, -12.5, None, lambda: palier(1131)),
    ("nyxessa_charge_portail", 1, -14.0, None, lambda: glissando(1141, 147.0, 440.0, ("ou", "a"), False)),
    ("nyxessa_retour_energie", 1, -14.0, None, lambda: glissando(1151, 440.0, 147.0, ("a", "ou"), True)),
    ("nyxessa_onde", 1, -17.0, None, lambda: onde(1161)),
    ("nyxessa_destruction", 1, -12.5, None, lambda: destruction(1171)),
    ("nyxessa_missile_vol_boucle", 2, -17.0, da.BOUCLE, lambda: missile_vol(1181)),
    ("nyxessa_missile_eclat_1", 2, -14.0, None, lambda: missile_eclat(1182, 262.0)),
    ("nyxessa_missile_eclat_2", 2, -14.0, None, lambda: missile_eclat(1183, 230.0)),
    ("nyxessa_missile_eclat_3", 2, -14.0, None, lambda: missile_eclat(1184, 294.0)),
    ("nyxessa_rappel", 2, -13.0, None, lambda: rappel(1185)),
    ("nyxessa_reapparition", 2, -14.0, None, lambda: reapparition(1186)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
