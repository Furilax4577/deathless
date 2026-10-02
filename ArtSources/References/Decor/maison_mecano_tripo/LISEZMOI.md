# Boutique du mécano : sortie Tripo (02/10/2026)

- Fichier : `medieval+cottage+3d+model.fbx` (262 847 triangles, un seul maillage, texture 4K), d'**une seule image** (Grok, style chunky). Boîte Tripo : 0,82 × 0,76 × 0,98 unité (largeur, profondeur, hauteur).
- Contenu : haut pignon avant à **deux petites fenêtres**, **grande vitrine à petits carreaux** (à droite de l'arche), arche d'entrée **vide**, toit de tuiles rouges, **tuyau de poêle gris coudé** sur le toit, **girouette en engrenage** au faîte, soubassement de deux rangées, pas de plateforme.
- Écarts à régler au pipeline : la maison est **plus haute que large** (rapport largeur/hauteur 0,84), alors que le plan est 12,6 × 9,6 m : choisir l'échelle ; la **façade latérale** (côté droit) est pleine, avec des panneaux de colombage sans fenêtre ; le mur arrière est plein aussi. Rendus de contrôle : `rendus/`.

## Traitement (02/10/2026)

`maison_pipeline.py --piece mecano` : échelle uniforme, largeur 12,6 m (celle du plan, soubassement 11,0 m), profondeur 11,8 m hors tout (soubassement 10,3 m), hauteur 15,1 m (plan : faîtage 9,5 m ; le modèle est haut et on ne l'écrase pas). **11 943 triangles**, atlas 2048², palette 1, collision de 170 triangles, pas d'émission (vitres sans couleur propre). Sorties : `Assets/Art/Decor/Maisons/Mecano.*`.
