# Maison de base : sortie Tripo (02/10/2026)

- Fichier : `medieval+cottage+3d+model.fbx` (273 713 triangles, un seul maillage, texture 4K `medieval+cottage+3d+model.fbm/…_basecolor.jpg`). Généré par Tripo à partir d'**une seule image** (vue trois-quarts avant-droit de Grok, voir `Docs/da/brief-maisons.md` § Mode une seule image). Boîte Tripo : 0,98 × 0,90 × 0,93 unité (largeur, profondeur, hauteur).
- Contenu : toit fermé en tuiles individuelles, **une seule cheminée** (gauche), arche en claveaux sur embrasure **vide** (pas de battant), fenêtres **sans volets**, lanterne, hublot au pignon droit, **soubassement de deux rangées de pierres et deux marches, sans plateforme**.
- Défaut connu : petit trou dans les tuiles autour de la cheminée (vu du dos) ; pas de fenêtre sur le mur arrière.
- Rendus de contrôle : `rendus/`. Pipeline : `ArtSources/Decor/Maisons/maison_pipeline.py`.

## Traitement (02/10/2026)

Lancement : `blender.exe -b --python ArtSources/Decor/Maisons/maison_pipeline.py -- --piece base`. Échelle uniforme 8,78 m par unité Tripo (largeur du toit 8,6 m) ; dimensions obtenues **8,6 x 7,9 x 8,2 m** (soubassement 7,5 x 6,5 m, embrasure 1,4 x 2,4 m, égout 3,0 m, faîtage 7,4 m, cheminée 8,2 m). Décimée à **10 985 triangles**, atlas cuit 2048², palette 1 (8 couleurs ; la variante 0 est gardée dans `Maison_Base_Texture_Variante.png`), collision de 166 triangles. **Le « trou » du toit** n'en est pas un (maillage fermé, deux arêtes ouvertes au pied) : encoche de la faîtière côté dos autour de la cheminée, recouverte avant décimation par une faîtière neuve. Sorties : `Assets/Art/Decor/Maisons/Maison_Base.*`, prefab `Maison_Base.prefab`. Détails, palette, captures : `Docs/da/brief-maisons.md` § Réception réalisée.
