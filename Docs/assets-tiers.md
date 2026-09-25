# Assets tiers

Assets externes utilisés dans Deathless, avec leur licence et leur emplacement.

| Asset | Source | Licence | Emplacement | Ajouté le |
|---|---|---|---|---|
| KayKit Adventurers 2.0, Character Animations 1.1, Skeletons 1.1, Forest Nature Pack 1.0 (extraits) | https://kaylousberg.itch.io (copiés depuis Relic) | CC0 | `Assets/Art/KayKit/`, `Assets/VFX/*/` | 24-25/09/2026 |
| Kenney Input Prompts 1.5 (Xbox Series, PlayStation Series, Keyboard & Mouse, PNG « Double ») | https://kenney.nl/assets/input-prompts | CC0 (`Assets/Art/UI/KenneyInputPrompts/License.txt`) | `Assets/Art/UI/KenneyInputPrompts/` | 25/09/2026 |
| Fredoka (Regular, Medium, SemiBold, Bold, statiques) | https://fonts.google.com/specimen/Fredoka | SIL OFL 1.1 (`Assets/Art/Fonts/Fredoka/OFL.txt`) | `Assets/Art/Fonts/Fredoka/` | 25/09/2026 |
| Kenney RPG Audio (51 sons `.ogg` : pas, pièces, portes, livres, cuir, lames, métal) | https://kenney.nl/assets/rpg-audio (copié depuis Relic) | CC0 (`Assets/Audio/Kenney/RPGAudio/License.txt`) | `Assets/Audio/Kenney/RPGAudio/` | 25/09/2026 |
| Kenney Interface Sounds 1.0 (100 sons `.ogg` : clics, tics, confirmations, erreurs, gong…) | https://kenney.nl/assets/interface-sounds (copié depuis Relic) | CC0 (`Assets/Audio/Kenney/InterfaceSounds/License.txt`) | `Assets/Audio/Kenney/InterfaceSounds/` | 25/09/2026 |
| Kevin MacLeod : « Village Consort », « Crunk Knight », « Myst on the Moor » (`.mp3`) | https://incompetech.com (copiés depuis Relic) | Creative Commons BY 4.0 : **crédit obligatoire** dès qu'un morceau est joué (texte exact dans `Wiki/pages/credits.md`) | `Assets/Audio/Incompetech/` | 25/09/2026 |

## Sons créés pour le projet

`Assets/Audio/Relic/` : 90 effets sonores et 3 musiques (`Musique/`) générés en Python pur pour Relic, sans échantillon tiers ni licence, validés à l'écoute par Quentin ; scripts de synthèse dans `Assets/Audio/Relic/Sources/`, usage de chaque fichier dans `Assets/Audio/Relic/LISEZMOI.md`. Copiés depuis Relic le 25/09/2026 avec leurs `.meta` (mêmes GUID). Catalogue de tous les sons (noms, usage dans Deathless, statut) : `Wiki/data/sons.json`, page Sons du wiki ; organisation et ajout d'un son : `Docs/sons.md`.

Les musiques de Kevin MacLeod ne sont plus jouées depuis Relic version 59 (remplacées par les musiques générées) : elles restent dans le projet. Leur crédit est déjà dans la page Crédits du wiki ; il devra aussi figurer dans le jeu (menu Crédits, LISEZMOI du build) dès qu'un morceau y est joué.
