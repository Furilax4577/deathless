# Forge : sortie Tripo (02/10/2026)

- Fichier : `medieval+cottage+3d+model.fbx` (283 888 triangles, un seul maillage, texture 4K `…_basecolor.jpg`), généré à partir d'**une seule image** (Grok, forge à vide en style « chunky » : voir `Docs/da/brief-forge.md`). Boîte Tripo : 0,98 × 0,74 × 0,79 unité (largeur, profondeur, hauteur).
- Contenu : maison à gauche (porte à arche **vide** sur le pignon avant, fenêtre à barreaux, soubassement de deux rangées de pierres), **atelier ouvert** à droite sous un toit à une pente sur poteaux, **foyer de pierre contre le mur** avec bouche vide et sombre (le feu est un effet moteur), **haute cheminée**, sol de dalles plates ; atelier **vide** (enclume et bac de trempe sont des pièces séparées).
- Écart au plan à régler au pipeline : le modèle est plus profond (rapport profondeur/largeur 0,75 contre 0,55 au plan 17,4 × 9,6 m) et la cheminée est haute ; deux poteaux seulement visibles côté ouvert. Voir `Docs/da/brief-forge.md` pour les cotes et la bande libre de 2,8 m du forgeron.
- Rendus de contrôle : `rendus/`.

## Traitement (02/10/2026)

`maison_pipeline.py --piece forge` : largeur 17,4 m, **profondeur x 0,87**, hauteur gardée (17,4 x 11,4 x 14,0 m) ; **14 192 triangles**, atlas 2048², palette 1, collision de 346 triangles (aucun volume sur le sol de l'atelier, qui est enfoncé de 0,34 m pour être au niveau du terrain). Le foyer du modèle (3,75 x 4,9 m, bouche au sud) est deux fois plus gros que celui du plan : la bande libre de 2,8 m du brief n'existe que devant le foyer (y < 4 m) ; les postes ont été recalés (`Docs/da/brief-forge.md` § Réception réalisée). Rendus des trois options d'échelle : `rendus/option_{A,B,C}_*.png` (A uniforme 17,4 m, **B retenue**, C uniforme 14,6 m). Sorties : `Assets/Art/Decor/Maisons/Forge.*`.
