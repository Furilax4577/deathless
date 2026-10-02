# Maison du druide : sortie Tripo (02/10/2026)

- Fichier : `mossy+cottage+3d+model.fbx` (284 619 triangles, un seul maillage, texture 4K de 5,3 Mo), d'**une seule image** (Grok, style chunky). Boîte Tripo : 0,98 × 0,76 × 0,85 unité (largeur, profondeur, hauteur).
- Contenu : toit très pentu couvert de **mousse olive**, cheminée de pierre, **lierre** sur la façade, **jardinières fleuries** (jaune et violet) sous les fenêtres, arche d'entrée **vide**, soubassement de deux rangées, **auvent de séchage ouvert à droite** avec bouquets d'herbes suspendus aux poutres, pas de plateforme.
- À régler au pipeline : le **lierre, les bouquets d'herbes et les fleurs sont des éléments fins** que la décimation risque de perdre : les garder dans la texture ou les simplifier en gros volumes ; la mousse du toit est plus bruitée que les autres maisons (la palette la ramène à quelques verts). Rendus de contrôle : `rendus/`.

## Traitement (02/10/2026)

`maison_pipeline.py --piece druide` : largeur 14,6 m (maison 10,3 m + auvent 3,7 m), profondeur x 0,88, hauteur gardée : **14,6 x 10,0 x 12,7 m**. **16 323 triangles**, atlas 2048², palette 1 avec **conservation des couleurs hors palette** (fleurs jaunes et violettes, lierre vif) ; le lierre, les jardinières et les bouquets d'herbes sont gardés dans la texture (cuisson depuis la haute définition) et les bouquets d'herbes sont en plus protégés de la décimation : ils restent lisibles. Collision de 170 triangles (corps, toit, marches ; l'auvent n'a pas de volume au sol). Sorties : `Assets/Art/Decor/Maisons/Druide.*`.
