# Maison du sorcier : sortie Tripo (02/10/2026)

- Fichier : `fantasy+cottage+3d+model.fbx` (282 374 triangles, un seul maillage, texture 4K), d'**une seule image** (Grok, style chunky ; prompt dans `Docs/da/brief-maisons.md`). Boîte Tripo : 0,76 × 0,72 × 0,98 unité (largeur, profondeur, hauteur).
- Contenu : maison basse à toit d'ardoise bleue, **tour ronde** à toit conique et croissant de lune (hublot à cadre de pierre, fente), arche d'entrée **vide**, deux fenêtres sans volets, lanterne, cheminée en pierre, soubassement de deux rangées, **pas de plateforme**.
- À régler au pipeline : le modèle est haut et étroit par rapport au plan (11,6 × 9,6 m + tour Ø 4,5 m) ; le crénelage sous le toit de la tour est un détail qui plaît, à garder. Rendus de contrôle : `rendus/`.

## Traitement (02/10/2026)

`maison_pipeline.py --piece sorcier` : largeur 12,4 m, profondeur x 0,93, hauteur gardée : **12,4 x 11,0 x 16,0 m** (murs 4,5 m, faîtage 9,5 m, tour Ø 4,2 m, croissant à 16 m ; le plan donne 11,6 x 9,6 m + tour Ø 4,5 m). **14 706 triangles**, atlas 2048², palette 1 (ardoise bleue franche, vitres et lanterne émissives la nuit), collision de 258 triangles (corps, toit, marches, tour en deux enveloppes), prefab avec `Entree`, `Lanterne`, `Porte_Pivot` (pas de volets). Le crénelage sous le toit de la tour est gardé. Sorties : `Assets/Art/Decor/Maisons/Sorcier.*`.
