# Sons du projet (générés)

Sons créés par programme pour Relic (synthèse en Python pur, aucun échantillon tiers, aucune licence à respecter),
validés un par un par Quentin (les sons du combat, étape 71, sont branchés pour être jugés en jeu). Les scripts de génération sont dans `Sources/` (hors build : Unity ignore les `.py`).

| Fichier | Usage | Généré par |
|---|---|---|
| `fireball_cast.wav` | Lancer de la boule de feu du mage | `synth_sounds4.py`, `fire_cast` (graine 31) |
| `fireball_flight_loop.wav` | Vol de la boule de feu (boucle, son 3D sur le projectile) | `synth_sounds4.py`, `fire_flight` |
| `fireball_explosion.wav` | Explosion de la boule de feu | `synth_sounds4.py`, `fire_explosion` |
| `skull_flight_loop.wav` | Vol du missile du nécromancien : chœur grave presque seul, quelques souffles discrets (boucle, son 3D sur le crâne) | `synth_b_fix2.py`, second fichier (graine 48) |
| `skull_explosion.wav` | Eclatement du crâne : cri grave | `synth_sounds6.py`, `deep_scream(404, 185)` |
| `bow_shot_1..3.wav` | Tir de l'arc du rôdeur (3D, départ du projectile) | `synth_sounds7.py`, `bow_shot` (graine 701) |
| `crossbow_shot_1..3.wav` | Tir de l'arbalète de l'assassin | `synth_sounds7.py`, `crossbow_shot` (702) |
| `skeleton_bow_shot_1..3.wav` | Tir d'un archer squelette | `synth_sounds7.py`, `skeleton_bow_shot` (703) |
| `tower_shot_1..2.wav` | Tir d'une tour | `synth_sounds7.py`, `tower_shot` (704) |
| `arrow_impact_1..3.wav` | Flèche ou carreau qui se fiche (joueurs, tours, archers) | `synth_sounds7.py`, `arrow_impact` (705) |
| `shield_block_1..3.wav` | Coup bloqué au bouclier | `synth_sounds7.py`, `shield_block` (709) |
| `parry.wav` | Parade parfaite (par-dessus le blocage) | `synth_sounds7.py`, `parry` (710) |
| `skeleton_hit_1..3.wav` | Squelette touché | `synth_sounds7.py`, `skeleton_hit` (706) |
| `skeleton_death_1..3.wav` | Squelette tué (effondrement) | `synth_sounds7.py`, `skeleton_death` (707) |
| `skeleton_spawn_1..2.wav` | Sortie du sol | `synth_sounds7.py`, `skeleton_spawn` (708) |
| `dawn_vaporize_1..2.wav` | Squelette vaporisé à l'aube | `synth_sounds7.py`, `dawn_vaporize` (757) |
| `necro_summon.wav` | Incantation d'invocation du nécromancien (3 s) | `synth_sounds7.py`, `necro_summon` (740) |
| `necro_heal_loop.wav` | Soin en canal du nécromancien (boucle) | `synth_sounds7.py`, `necro_heal_loop` (741) |
| `player_hurt_1..3.wav` | Joueur touché | `synth_sounds7.py`, `player_hurt` (711) |
| `player_death.wav` | Mort d'un joueur | `synth_sounds7.py`, `player_death` (712) |
| `respawn.wav` | Réapparition | `synth_sounds7.py`, `respawn` (713) |
| `dodge_1..2.wav` | Esquive | `synth_sounds7.py`, `dodge` (714) |
| `land_1..3.wav` | Réception d'un saut | `synth_sounds7.py`, `land` (715) |
| `water_step_1..3.wav` | Pas dans une salle inondée (les pas au sol sont les `footstep00`–`09` de Kenney RPG Audio) | `synth_sounds7.py`, `water_step` (716) |
| `knight_bash.wav` | Chevalier : poussée au bouclier | `synth_sounds7.py`, `knight_bash` (720) |
| `knight_charge.wav` | Chevalier : charge bélier | `synth_sounds7.py`, `knight_charge` (721) |
| `knight_heal.wav` | Chevalier : soin sur soi | `synth_sounds7.py`, `knight_heal` (722) |
| `mage_ignite.wav` | Mage : embrasement | `synth_sounds7.py`, `mage_ignite` (723) |
| `mage_flame_cone_loop.wav` | Mage : cône de flammes (boucle) | `synth_sounds7.py`, `mage_flame_cone_loop` (724) |
| `mage_avatar.wav` | Mage : avatar du feu | `synth_sounds7.py`, `mage_avatar` (725) |
| `ranger_focus.wav` | Rôdeur : concentration | `synth_sounds7.py`, `ranger_focus` (726) |
| `arrow_rain.wav` | Rôdeur : pluie de flèches | `synth_sounds7.py`, `arrow_rain` (727) |
| `smoke_bomb.wav` | Assassin : bombe fumigène (impact) | `synth_sounds7.py`, `smoke_bomb` (728) |
| `viking_roar_1..2.wav` | Viking : rugissement | `synth_sounds7.py`, `viking_roar` (729) |
| `viking_leap_land.wav` | Viking : atterrissage du saut percutant | `synth_sounds7.py`, `viking_leap_land` (730) |
| `whirlwind_loop.wav` | Viking : tourbillon (boucle) | `synth_sounds7.py`, `whirlwind_loop` (731) |
| `portal_open.wav` | Ouverture du portail (aube) | `synth_sounds7.py`, `portal_open` (750) |
| `portal_close.wav` | Fermeture du portail (nuit) | `synth_sounds7.py`, `portal_close` (751) |
| `portal_hum_loop.wav` | Portail ouvert, audible de près (boucle) | `synth_sounds7.py`, `portal_hum_loop` (752) |
| `relic_hit_1..2.wav` | Relique frappée (installé, pas encore branché) | `synth_sounds7.py`, `relic_hit` (753) |
| `relic_pulse.wav` | Impulsion de l'anneau de la relique | `synth_sounds7.py`, `relic_pulse` (754) |
| `nightfall.wav` | Tombée de la nuit (2D) | `synth_sounds7.py`, `nightfall` (755) |
| `dawn.wav` | L'aube (2D) | `synth_sounds7.py`, `dawn` (756) |
| `crystal_light_1..2.wav` | Cristal de passage qui s'allume | `synth_sounds7.py`, `crystal_light` (758) |
| `trap_spikes.wav` | Piège à piques | `synth_sounds7.py`, `trap_spikes` (759) |
| `trap_flames.wav` | Grille à flammes | `synth_sounds7.py`, `trap_flames` (760) |
| `structure_place.wav` | Tour ou mur posé | `synth_sounds7.py`, `structure_place` (761) |
| `structure_hit_1..2.wav` | Tour ou mur frappé | `synth_sounds7.py`, `structure_hit` (762) |
| `structure_destroyed.wav` | Tour ou mur détruit | `synth_sounds7.py`, `structure_destroyed` (763) |
| `chest_open.wav` | Couvercle d'un coffre du donjon | `synth_sounds7.py`, `chest_open` (764) |
| `portal_pass.wav` | Passage d'un joueur par le portail (départ, 1,1 s) | `synth_sounds7.py`, `portal_pass` (765) |
| `bow_shot_v2_1..3.wav` | Arc du rôdeur, deuxième version (remplace `bow_shot_1..3`, gardés) | `synth_sounds7.py`, `bow_shot_v2` (766) |
| `bow_shot_charged_1..2.wav` | Tir chargé du rôdeur | `synth_sounds7.py`, `bow_shot_charged` (767) |
| `shield_raise.wav` | Bouclier de la relique qui se lève | `synth_sounds7.py`, `shield_raise` (768) |
| `shield_hit_1..3.wav` | Coup sur le dôme | `synth_sounds7.py`, `shield_hit` (769) |
| `shield_break.wav` | Rupture du dôme | `synth_sounds7.py`, `shield_break` (770) |
| `turret_turn_loop.wav` | Tête de tourelle qui pivote (boucle) | `synth_sounds7.py`, `turret_turn_loop` (771) |

Régénérer : depuis `Sources/` (les scripts s'importent entre eux) : `python synth_sounds4.py <dossier>`, `python synth_sounds6.py <dossier>`, `python synth_b_fix2.py <murmures.wav> <choeur.wav>`.

`synth_sounds7.py` (plan des sons, `Docs/plan-sons.md`) : `python synth_sounds7.py <dossier> [nom ...]`, variantes `_1`, `_2`... Les essais non branchés sont dans `SonsEssais/` à la racine du dépôt (page `ecoute.html`). Branchement : copier le fichier ici puis menu **Relic > Sons > Installer** (`Assets/Editor/SoundSetup.cs`).

## Musiques (`Musique/`)

| Fichier | Ambiance (GameMusic) | Généré par |
|---|---|---|
| `musique_dehors_jour.wav` | Village de jour, menu, salle d'attente | `synth_music.py jour` (graine 9001) |
| `musique_dehors_nuit.wav` | Village de nuit (vague) | `synth_music.py nuit` (graine 9002) |
| `musique_dedans_donjon.wav` | Donjon | `synth_music.py donjon` (graine 9003) |

Boucles sans raccord (nappes fondues fin/début, queues de réverbération rabattues au début), stéréo. Importées en flux (Streaming, Vorbis 0,7) par `Relic > Sons > Installer`, qui les branche aussi sur GameMusic.
