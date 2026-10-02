# -*- coding: utf-8 -*-
"""
Nom de la taverne sur son enseigne (02/10/2026) : « Le Tonneau Percé », écrit dans le moteur et non dans le modèle Tripo
(la planche du modèle est sans texte). Génère Assets/Art/Decor/Maisons/Taverne_Enseigne_Nom.png : lettres crème de la police
Fredoka Bold du projet (Assets/Art/Fonts/Fredoka/) cernées d'un liseré brun foncé, fond transparent (RGB constant partout,
seul l'alpha varie : pas de frange au découpage alpha). Côté Unity, MaisonTripo.cs pose deux quads (avant et dos) sur
l'ancre « Enseigne » avec un matériau URP Lit à découpe alpha (Taverne_Enseigne.mat).

Lancement : python ArtSources/Decor/Maisons/taverne_enseigne.py   (Pillow)
Trois lignes, centrées : « Le » (petit), « Tonneau », « Percé » ; texture carrée 2048 x 2048 (le quad fait environ 2 m de côté,
soit ~1000 px par mètre : lisible de 5 à 8 m, caméra d'épaule).
"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", ".."))
POLICE = os.path.join(RACINE, "Assets", "Art", "Fonts", "Fredoka", "Fredoka-Bold.ttf")
SORTIE = os.path.join(RACINE, "Assets", "Art", "Decor", "Maisons", "Taverne_Enseigne_Nom.png")
T = 2048
CREME = (246, 226, 176)    # lettres
BRUN = (44, 27, 18)        # liseré (brun d'embrasure de la palette)
LIGNES = [("Le", 0.50), ("Tonneau", 1.0), ("Percé", 1.0)]   # texte, taille relative
LARGEUR_MAX = 0.96 * T     # la ligne la plus large occupe 96 % de la texture
INTERLIGNE = 0.10          # espace entre lignes, en fraction de la taille de la plus grande ligne


def police(px):
    return ImageFont.truetype(POLICE, int(px))


def mesurer(taille):
    boites = []
    for txt, k in LIGNES:
        f = police(taille * k)
        b = f.getbbox(txt, stroke_width=int(taille * 0.035))
        boites.append((b[2] - b[0], b[3] - b[1], b))
    return boites


def main():
    # taille de base : la plus grande qui fait tenir « Tonneau » (la plus large) dans LARGEUR_MAX et le bloc dans 0,88 T de haut
    taille = 100.0
    while True:
        bx = mesurer(taille)
        larg = max(b[0] for b in bx)
        haut = sum(b[1] for b in bx) + INTERLIGNE * taille * (len(bx) - 1)
        if larg > LARGEUR_MAX or haut > 0.88 * T:
            taille -= 4
            break
        taille += 4
    bx = mesurer(taille)
    haut = sum(b[1] for b in bx) + INTERLIGNE * taille * (len(bx) - 1)
    trait = int(taille * 0.035)
    # deux calques d'alpha : liseré (lettres élargies) et lettres ; couleur unie par calque
    alpha_lettres = Image.new("L", (T, T), 0)
    alpha_total = Image.new("L", (T, T), 0)
    dl, dt = ImageDraw.Draw(alpha_lettres), ImageDraw.Draw(alpha_total)
    y = (T - haut) / 2
    for (txt, k), (w, h, b) in zip(LIGNES, bx):
        f = police(taille * k)
        x = (T - w) / 2 - b[0]
        dl.text((x, y - b[1]), txt, font=f, fill=255)
        dt.text((x, y - b[1]), txt, font=f, fill=255, stroke_width=trait, stroke_fill=255)
        y += h + INTERLIGNE * taille
    # RVB : brun sous le liseré, crème sous les lettres ; hors du dessin, couleur du liseré (alpha 0)
    img = Image.new("RGBA", (T, T), BRUN + (0,))
    img.paste(Image.new("RGBA", (T, T), CREME + (0,)), (0, 0), alpha_lettres.filter(ImageFilter.GaussianBlur(1.2)))
    # alpha : un léger flou pour un bord propre au découpage (seuil 0,4 côté Unity)
    img.putalpha(alpha_total.filter(ImageFilter.GaussianBlur(1.2)))
    os.makedirs(os.path.dirname(SORTIE), exist_ok=True)
    img.save(SORTIE)
    print("écrit", os.path.relpath(SORTIE, RACINE), "taille de base %d px, bloc %d x %d px" % (taille, larg, haut))


if __name__ == "__main__":
    main()
