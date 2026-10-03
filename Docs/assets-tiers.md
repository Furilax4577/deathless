# Assets tiers

Assets externes utilisés dans Deathless, avec leur licence et leur emplacement.

| Asset | Source | Licence | Emplacement | Ajouté le |
|---|---|---|---|---|
| KayKit Adventurers 2.0, Character Animations 1.1, Skeletons 1.1, Forest Nature Pack 1.0 (extraits) | https://kaylousberg.itch.io (copiés depuis Relic) | CC0 | `Assets/Art/KayKit/`, `Assets/VFX/*/` | 24-25/09/2026 |
| Kenney Input Prompts 1.5 (Xbox Series, PlayStation Series, Keyboard & Mouse, PNG « Double ») | https://kenney.nl/assets/input-prompts | CC0 (`Assets/Art/UI/KenneyInputPrompts/License.txt`) | `Assets/Art/UI/KenneyInputPrompts/` | 25/09/2026 |
| Fredoka (Regular, Medium, SemiBold, Bold, statiques) | https://fonts.google.com/specimen/Fredoka | SIL OFL 1.1 (`Assets/Art/Fonts/Fredoka/OFL.txt`) | `Assets/Art/Fonts/Fredoka/` | 25/09/2026 |
| Kenney RPG Audio (51 sons `.ogg` : pas, pièces, portes, livres, cuir, lames, métal) | https://kenney.nl/assets/rpg-audio (copié depuis Relic) | CC0 (`Assets/Audio/Kenney/RPGAudio/License.txt`) | `Assets/Audio/Kenney/RPGAudio/` | 25/09/2026 |
| Kenney Interface Sounds 1.0 (100 sons `.ogg` : clics, tics, confirmations, erreurs, gong…) | https://kenney.nl/assets/interface-sounds (copié depuis Relic) | CC0 (`Assets/Audio/Kenney/InterfaceSounds/License.txt`) | `Assets/Audio/Kenney/InterfaceSounds/` | 25/09/2026 |

## Sons créés pour le projet

`Assets/Audio/Relic/` : 90 effets sonores et 3 musiques (`Musique/`) générés en Python pur pour Relic, sans échantillon tiers ni licence, validés à l'écoute par Quentin ; scripts de synthèse dans `Assets/Audio/Relic/Sources/`, usage de chaque fichier dans `Assets/Audio/Relic/LISEZMOI.md`. Copiés depuis Relic le 25/09/2026 avec leurs `.meta` (mêmes GUID). Catalogue de tous les sons (noms, usage dans Deathless, statut) : `Wiki/data/sons.json`, page Sons du wiki ; organisation et ajout d'un son : `Docs/sons.md`.

## Cap : dégager KayKit et Kenney

Décisions de Quentin : à terme, remplacer tout ce qui vient de KayKit (26/09/2026) et de Kenney (03/10/2026, sons RPG Audio et Interface Sounds, icônes Input Prompts) par des assets propres à Deathless. Les deux jauges (`Wiki/jauge_kaykit.py` et `Wiki/jauge_kenney.py`, données `Wiki/data/kaykit.json` et `Wiki/data/kenney.json`) se recalculent à chaque `python Wiki/build.py` et s'affichent dans la page « À faire » du wiki développeur, avec pour Kenney le tableau « à remplacer » par fréquence d'usage et l'ordre de remplacement proposé.
