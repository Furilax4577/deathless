# Bâtiments : prompt direct dans Tripo, sans accessoires (02/10/2026, soir)

Retour de Quentin sur les bâtiments Tripo posés sur la carte v5 : les textures « font enfant qui peint et déborde partout » (peinture qui bave autour des détails), la végétation du druide est « dégueulasse », la mousse et le lierre sont à ajouter après coup, de même que le panneau et le tonneau de la taverne. **Règles nouvelles** :

1. **On prompte directement dans Tripo (texte vers 3D)**, plus de passage par Grok : onglet Texte, un bâtiment à la fois, on regarde ce que ça donne avant de généraliser.
2. **Rien d'accroché ni de peint sur la façade ni sur le toit** : ni lanterne, ni enseigne, ni tonneau, ni lierre, ni mousse, ni jardinière, ni bouquets d'herbes, ni bûches, ni feu. Tout cela est **ajouté après coup, en pièces à part** dans le moteur.
3. **Lanternes : on se sert de ce que l'on a** (le prefab de lanterne existant, posé sur une ancre `Lanterne` à côté de la porte).
4. **Couleurs unies et propres** : une teinte par matière (tuiles, plâtre, bois, pierre, vitre), aucun détail peint fin, aucune salissure.

## Réglages Tripo (texte)

- Onglet **Texte** ; **Modèle HD**, Qualité de maillage **Ultra**, **Triangle**, **200 000 à 300 000 polygones**, **PBR désactivé**, **Supprimer l'éclairage activé**, **texture 4K**, confidentialité privée.
- Remplir **aussi le champ « prompt négatif »** s'il existe (liste ci-dessous).
- Export FBX avec texture dans `ArtSources/References/Decor/<nom>_tripo_texte/`.
- Si la texture de Tripo « bave » encore : générer **sans texture**, me donner le FBX gris, et la chaîne Blender colore par zones (tuiles, plâtre, bois, pierre, vitre) avec une palette plate, sans passer par la peinture de Tripo.

## 1. Maison de base (à essayer en premier)

**Prompt** (anglais, moins de 900 caractères) :

```
Small half-timbered cottage, chunky toy-like low-poly game asset with smooth dense faceting and rounded forms. Steep
gabled roof of big puffy brick-red clay tiles, completely closed solid roof, exactly one short stone chimney on the
left gable. Smooth cream plaster walls with thick dark brown timber beams and diagonal braces. Walls stand directly on
a plinth of exactly two low courses of big rounded light-grey stone blocks, two small stone steps in front of the
door. Empty arched doorway framed by large stone blocks, no door leaf. One window with a plain thick wooden frame and
dark glass, no shutters. Flat clean unlit colors, one clean color per material, smooth plaster, no stains, no grime,
no wood grain, no painted details. Isolated building only, three-quarter front view.
```

**Prompt négatif** :

```
ground, grass, platform, terrace, paving, lantern, hanging sign, barrel, crate, ivy, moss, plants, flowers, flower
box, shutters, door leaf, props, characters, cast shadows, noisy texture, grime, stains, text, letters
```

## Les autres bâtiments (même bloc, descriptions courtes)

Même premier paragraphe de style que ci-dessus (toit fermé, soubassement de deux rangées, embrasure vide, pas de volets), puis :

- **Sorcier** : « House plus an attached round tower at the back right, tall conical roof of big dark blue slate shingles with a crescent-moon finial, cream plaster, one round porthole window in the tower, short stone chimney. »
- **Druide** : « Cottage with a very steep 50-degree roof of plain brown clay tiles (clean, no moss, no plants), short crooked stone chimney, two plain windows. » **Mousse, lierre, jardinières, herbes : en pièces à part dans le moteur.**
- **Mécano** : « Shop with a tall front gable and two storeys at the front, a very large shop window with many small square panes, brick-red tiles, bent grey stove pipe through the roof. »
- **Forge** : « Blacksmith workshop with dark brown tiles, a very massive tall stone chimney, a lean-to open shed on the right with thick wooden posts. Hearth, anvil and tub are separate props. »
- **Taverne** : « Long single-storey great hall with a tall hipped roof of big brick-red tiles, huge stone chimney at the left end, wide empty arched doorway under a small tiled porch roof, two windows. » **Enseigne, potence, tonneaux et fuite de bière : en pièces à part.**

## Pièces à ajouter après coup (moteur, pas dans Tripo)

| Pièce | Où | Comment |
|---|---|---|
| Lanterne | ancre `Lanterne` près de la porte (maison de base, sorcier, autres au besoin) | prefab de lanterne existant (le même que les potences de la grotte) |
| Enseigne de la taverne et sa potence | ancre `Enseigne` | prop à part (modèle simple ou Tripo sur une image d'enseigne seule) ; le nom s'écrit dans le moteur |
| Tonneaux de la taverne | ancre `Tonneau_Fuite` sur l'auvent, un au sol à gauche de la porte | prop tonneau à part ; effet `BiereFuite` déjà prêt |
| Mousse et lierre du druide | sur le toit et le colombage | plaques ou touffes de gemmes low poly, vert mat (jamais d'émission), posées par script sur des points du toit |
| Jardinières, bouquets d'herbes, étendoir | ancres du druide | props à part |
| Foyer, enclume, bac de la forge | ancres `Foyer_Feu`, enclume, bac | props à part (voir `brief-forge.md`) |
