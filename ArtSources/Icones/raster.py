# -*- coding: utf-8 -*-
"""Rastérisation des icônes en Python pur (bibliothèque standard) : polygones pleins suréchantillonnés, PNG RGBA écrits
avec zlib, conteneur ICO à entrées PNG. Reprend et remplace le rendu de Launcher/make_icon.py (un seul outil).

- rasteriser(formes, taille, ss) : formes = [(points dans le repère 128 × 128, "#rrggbb"), ...] dans l'ordre de dessin
  (peintre), remplissage pair-impair par lignes de balayage sur une grille taille×ss, puis moyenne des sous-pixels
  (anticrénelage). Renvoie les octets RGBA ligne par ligne, alpha non prémultiplié.
- png(largeur, hauteur, rgba) -> octets d'un PNG.
- ico([(taille, octets_png), ...]) -> octets d'un .ico (entrées PNG, lues par Windows Vista et suivants).
Même entrée, même sortie : relancer redonne des fichiers identiques.
"""
import struct
import zlib


def _rgb(hexa):
    return (int(hexa[1:3], 16), int(hexa[3:5], 16), int(hexa[5:7], 16))


def sur_echantillonnage(taille):
    """Sous-pixels par côté : plus fin pour les petites tailles, raisonnable pour les grandes."""
    if taille <= 32:
        return 12
    if taille <= 64:
        return 8
    if taille <= 256:
        return 4
    return 2


def rasteriser(formes, taille, ss=None, repere=128.0):
    ss = ss or sur_echantillonnage(taille)
    n = taille * ss
    k = n / repere
    couleurs = [(0, 0, 0)]
    index = {}
    lignes = [bytearray(n) for _ in range(n)]
    for pts, col in formes:
        if col not in index:
            index[col] = len(couleurs)
            couleurs.append(_rgb(col))
        ci = index[col]
        sp = [(x * k, y * k) for x, y in pts]
        ys = [p[1] for p in sp]
        y0 = max(0, int(min(ys)))
        y1 = min(n - 1, int(max(ys)) + 1)
        aretes = [(sp[i], sp[(i + 1) % len(sp)]) for i in range(len(sp))]
        aretes = [(a, b) for a, b in aretes if a[1] != b[1]]
        for rangee in range(y0, y1 + 1):
            yc = rangee + 0.5
            xs = []
            for (ax, ay), (bx, by) in aretes:
                if (ay <= yc < by) or (by <= yc < ay):
                    xs.append(ax + (yc - ay) * (bx - ax) / (by - ay))
            if len(xs) < 2:
                continue
            xs.sort()
            ligne = lignes[rangee]
            for i in range(0, len(xs) - 1, 2):
                a = max(0, min(n, int(xs[i] + 0.5)))
                b = max(0, min(n, int(xs[i + 1] + 0.5)))
                if b > a:
                    ligne[a:b] = bytes((ci,)) * (b - a)
    total = ss * ss
    sortie = bytearray()
    for py in range(taille):
        r = [0] * taille
        g = [0] * taille
        bl = [0] * taille
        a = [0] * taille
        for sr in range(ss):
            ligne = lignes[py * ss + sr]
            for px in range(taille):
                seg = ligne[px * ss:(px + 1) * ss]
                v0 = seg[0]
                if seg.count(v0) == ss:
                    if v0:
                        c = couleurs[v0]
                        r[px] += c[0] * ss
                        g[px] += c[1] * ss
                        bl[px] += c[2] * ss
                        a[px] += ss
                    continue
                for v in seg:
                    if v:
                        c = couleurs[v]
                        r[px] += c[0]
                        g[px] += c[1]
                        bl[px] += c[2]
                        a[px] += 1
        for px in range(taille):
            if a[px]:
                sortie += bytes((int(r[px] / a[px] + 0.5), int(g[px] / a[px] + 0.5), int(bl[px] / a[px] + 0.5),
                                 int(255 * a[px] / total + 0.5)))
            else:
                sortie += b"\0\0\0\0"
    return bytes(sortie)


def png(largeur, hauteur, rgba):
    def bloc(nature, donnees):
        return (struct.pack(">I", len(donnees)) + nature + donnees
                + struct.pack(">I", zlib.crc32(nature + donnees) & 0xFFFFFFFF))

    brut = bytearray()
    pas = largeur * 4
    for y in range(hauteur):
        brut.append(0)  # filtre « aucun »
        brut += rgba[y * pas:(y + 1) * pas]
    entete = struct.pack(">IIBBBBB", largeur, hauteur, 8, 6, 0, 0, 0)
    return (b"\x89PNG\r\n\x1a\n" + bloc(b"IHDR", entete) + bloc(b"IDAT", zlib.compress(bytes(brut), 9))
            + bloc(b"IEND", b""))


def ico(images):
    """images : [(taille, octets_png)], de la plus grande à la plus petite de préférence."""
    entete = struct.pack("<HHH", 0, 1, len(images))
    decalage = 6 + 16 * len(images)
    entrees = b""
    for taille, donnees in images:
        entrees += struct.pack("<BBBBHHII", taille % 256, taille % 256, 0, 0, 1, 32, len(donnees), decalage)
        decalage += len(donnees)
    return entete + entrees + b"".join(d for _, d in images)
