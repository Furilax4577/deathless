"""Génère Ressources/deathless.ico : l'emblème Nyxessa du jeu (gemme à facettes et sa ceinture de gemmes en orbite).

Un seul outil : l'emblème est dessiné et rastérisé par ArtSources/Icones/generer_icones.py (formes) et
ArtSources/Icones/raster.py (rendu suréchantillonné, PNG et ICO en Python pur). Ce script régénère l'emblème
(ArtSources/Icones/Nyxessa/, variante NYXESSA_CHOIX) puis recopie nyxessa.ico ici. ICO à entrées PNG : 256, 128, 64,
48, 32, 24 et 16 px, version simplifiée jusqu'à 32 px. Relancer le script redonne exactement le même fichier.

Usage : python Launcher/make_icon.py
"""
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "Ressources", "deathless.ico")
ICONES = os.path.join(os.path.dirname(HERE), "ArtSources", "Icones")

sys.dont_write_bytecode = True
sys.path.insert(0, ICONES)
import generer_icones  # noqa: E402


def main():
    generer_icones.exporter_nyxessa()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    shutil.copyfile(os.path.join(ICONES, "Nyxessa", "nyxessa.ico"), OUT)
    print("écrit", OUT, os.path.getsize(OUT), "octets")


if __name__ == "__main__":
    main()
