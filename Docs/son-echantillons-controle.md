# Planche de contrôle des sons de Deathless, échantillons de la direction sombre

Générée par `python -B Assets/Audio/Deathless/controle.py` : ne pas modifier à la main, relancer le script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.

Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**, sauf pour les ambiances et les musiques, réglées sur le RMS moyen) ; **tête** = temps avant le premier échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes < 250 Hz, 250-1k, 1-4k, 4-10k, > 10k. Une **boucle** est vérifiée à sa jointure (pas de saut entre la fin et le début) au lieu du fondu de fin.

**54 fichiers, 54 conformes.**

## Ambiances — `Assets/Audio/Deathless/Ambiances/synth_ambiances.py`

Ambiances (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `ambiance_donjon_boucle.wav` | 20.00 s (boucle), stéréo | -12.2 | -30.0 | -27.2 | -30.0 (RMS) | 0.0 ms | 67 · 22 · 9 · 2 · 1 | ok |
| `ambiance_village_nuit_boucle.wav` | 20.00 s (boucle), stéréo | -13.5 | -30.0 | -25.7 | -30.0 (RMS) | 0.0 ms | 88 · 9 · 3 · 1 · 0 | ok |

## Assassin — `Assets/Audio/Deathless/Assassin/synth_assassin.py`

Assassin, dague et arbalète (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `dague_elan_1.wav` | 0.20 s | -4.4 | -23.0 | -17.0 | -17.0 | 0.4 ms | 0 · 1 · 50 · 35 · 14 | ok |
| `fumee.wav` | 1.50 s | -4.3 | -20.5 | -15.0 | -15.0 | 0.0 ms | 20 · 2 · 17 · 34 · 27 | ok |
| `furtif_entree.wav` | 0.70 s | -8.0 | -24.0 | -18.0 | -18.0 | 1.7 ms | 15 · 67 · 14 · 3 · 1 | ok |

## Candidats — `Assets/Audio/Deathless/Candidats/synth_candidats.py`

Candidats du mois (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `barde_bwoiing.wav` | 0.80 s | -7.2 | -18.6 | -14.0 | -14.0 | 0.1 ms | 90 · 10 · 0 · 0 · 0 | ok |
| `clochard_pet_defense_1.wav` | 0.70 s | -3.2 | -21.8 | -14.0 | -14.0 | 0.2 ms | 15 · 29 · 43 · 9 · 4 | ok |
| `djbob_scratch_1.wav` | 0.60 s | -3.9 | -16.8 | -14.0 | -14.0 | 2.0 ms | 0 · 56 · 28 · 11 · 5 | ok |

## Critiques — `Assets/Audio/Deathless/Critiques/synth_critiques.py`

Coups critiques (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `critique_1.wav` | 0.50 s | -1.4 | -25.3 | -15.6 | -14.0 | 0.0 ms | 22 · 0 · 69 · 8 · 1 | ok |
| `critique_meilleur.wav` | 0.85 s | -1.4 | -23.6 | -12.5 | -12.5 | 0.0 ms | 36 · 17 · 39 · 6 · 1 | ok |

## Joueurs — `Assets/Audio/Deathless/Joueurs/synth_joueurs.py`

Joueurs, actions communes (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `esquive_1.wav` | 0.50 s | -6.7 | -21.9 | -16.0 | -16.0 | 1.2 ms | 80 · 14 · 5 · 1 · 0 | ok |
| `joueur_mort.wav` | 1.60 s | -6.4 | -25.9 | -13.0 | -13.0 | 0.0 ms | 71 · 28 · 1 · 0 · 0 | ok |
| `joueur_touche_1.wav` | 0.35 s | -2.7 | -23.3 | -15.0 | -15.0 | 0.0 ms | 42 · 12 · 31 · 14 · 2 | ok |
| `joueur_touche_2.wav` | 0.35 s | -1.4 | -23.7 | -15.3 | -15.0 | 0.0 ms | 43 · 10 · 32 · 14 · 2 | ok |
| `potion_boire.wav` | 0.90 s | -8.2 | -23.1 | -16.0 | -16.0 | 0.0 ms | 28 · 70 · 2 · 0 · 0 | ok |

## JourNuit — `Assets/Audio/Deathless/JourNuit/synth_journuit.py`

Jour, nuit et partie : annonces globales (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `aube.wav` | 4.00 s | -5.4 | -18.8 | -13.0 | -13.0 | 0.0 ms | 66 · 34 · 1 · 0 · 0 | ok |
| `crepuscule.wav` | 4.00 s | -4.6 | -22.2 | -13.0 | -13.0 | 0.0 ms | 92 · 8 · 0 · 0 · 0 | ok |
| `defaite.wav` | 4.00 s | -5.6 | -18.1 | -13.0 | -13.0 | 2.3 ms | 90 · 10 · 0 · 0 · 0 | ok |
| `vague.wav` | 2.00 s | -3.4 | -22.0 | -12.0 | -12.0 | 0.0 ms | 82 · 16 · 2 · 0 · 0 | ok |
| `victoire.wav` | 6.00 s | -2.2 | -20.9 | -11.0 | -11.0 | 0.0 ms | 77 · 22 · 1 · 0 · 0 | ok |

## Mage — `Assets/Audio/Deathless/Mage/synth_mage.py`

Mage, feu (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `boule_explosion_1.wav` | 1.20 s | -5.9 | -22.6 | -12.5 | -12.5 | 0.0 ms | 95 · 3 · 1 · 1 · 0 | ok |
| `boule_lancer_1.wav` | 0.50 s | -5.6 | -20.9 | -15.0 | -15.0 | 0.9 ms | 50 · 23 · 22 · 4 · 2 | ok |
| `cone_boucle.wav` | 2.00 s (boucle) | -6.7 | -20.7 | -17.0 | -17.0 | 0.0 ms | 53 · 40 · 3 · 2 · 2 | ok |

## Morgrim — `Assets/Audio/Deathless/Morgrim/synth_morgrim.py`

Morgrim, le Roi des os (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `massue_fracas_1.wav` | 1.20 s | -6.3 | -19.6 | -11.0 | -11.0 | 0.0 ms | 99 · 1 · 0 · 0 · 0 | ok |
| `massue_onde.wav` | 2.40 s | -1.9 | -17.8 | -13.0 | -13.0 | 1.4 ms | 34 · 65 · 2 · 0 · 0 | ok |
| `morgrim_cri.wav` | 2.00 s | -2.4 | -16.5 | -11.0 | -11.0 | 4.0 ms | 35 · 54 · 11 · 0 · 0 | ok |

## Musique — `Assets/Audio/Deathless/Musique/synth_musique.py`

Musiques (Deathless, échantillons de la direction sombre, 26/09/2026 au soir) : les trois morceaux du jeu, pour juger

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `musique_donjon_boucle.wav` | 40.00 s (boucle), stéréo | -3.0 | -21.0 | -10.8 | -21.0 (RMS) | 0.0 ms | 85 · 10 · 4 · 1 · 0 | ok |
| `musique_jour_boucle.wav` | 41.74 s (boucle), stéréo | -1.4 | -20.4 | -13.9 | -20.0 (RMS) | 0.0 ms | 34 · 57 · 7 · 2 · 0 | ok |
| `musique_nuit_boucle.wav` | 34.91 s (boucle), stéréo | -1.4 | -20.1 | -14.4 | -20.0 (RMS) | 0.0 ms | 43 · 55 · 2 · 1 · 0 | ok |

## Nyxar — `Assets/Audio/Deathless/Nyxar/synth_nyxar.py`

Nyxar, le Nécromancien (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `nyxar_arrivee.wav` | 3.50 s | -7.1 | -21.4 | -12.0 | -12.0 | 0.0 ms | 76 · 24 · 0 · 0 · 0 | ok |
| `nyxar_eclat_brise_1.wav` | 2.00 s | -1.4 | -22.4 | -12.1 | -12.0 | 0.0 ms | 4 · 81 · 15 · 0 · 1 | ok |

## Paladin — `Assets/Audio/Deathless/Paladin/synth_paladin.py`

Paladin, épée et bouclier (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `blocage_1.wav` | 0.40 s | -2.4 | -23.0 | -14.0 | -14.0 | 0.0 ms | 91 · 6 · 3 · 0 · 0 | ok |
| `epee_elan_1.wav` | 0.30 s | -3.9 | -21.8 | -15.0 | -15.0 | 1.5 ms | 0 · 22 · 64 · 9 · 5 | ok |
| `epee_impact_1.wav` | 0.30 s | -1.4 | -24.3 | -16.5 | -14.0 | 0.0 ms | 74 · 22 · 3 · 0 · 0 | ok |
| `parade_parfaite.wav` | 0.90 s | -1.8 | -21.8 | -12.5 | -12.5 | 0.0 ms | 75 · 1 · 23 · 1 · 0 | ok |
| `soin.wav` | 1.50 s | -5.7 | -20.3 | -15.0 | -15.0 | 11.5 ms | 3 · 91 · 5 · 0 · 0 | ok |

## Rodeur — `Assets/Audio/Deathless/Rodeur/synth_rodeur.py`

Rôdeur, arc (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `arc_tir_1.wav` | 0.60 s | -2.5 | -24.3 | -14.0 | -14.0 | 0.0 ms | 3 · 15 · 56 · 25 · 2 | ok |
| `arc_tir_charge_1.wav` | 0.60 s | -1.4 | -23.2 | -13.0 | -13.0 | 0.0 ms | 3 · 12 · 47 · 34 · 4 | ok |
| `fleche_impact_os_1.wav` | 0.30 s | -1.4 | -25.5 | -17.7 | -14.0 | 0.0 ms | 88 · 12 · 0 · 0 · 0 | ok |

## Squelettes — `Assets/Audio/Deathless/Squelettes/synth_squelettes.py`

Squelettes (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `squelette_aube_1.wav` | 1.50 s | -1.4 | -20.5 | -15.8 | -15.0 | 11.0 ms | 1 · 31 · 54 · 10 · 4 | ok |
| `squelette_mort_1.wav` | 1.00 s | -2.5 | -21.8 | -15.0 | -15.0 | 11.0 ms | 40 · 36 · 22 · 2 · 0 | ok |
| `squelette_preparation_1.wav` | 0.70 s | -1.4 | -21.7 | -17.2 | -16.0 | 0.0 ms | 0 · 14 · 78 · 7 · 1 | ok |
| `squelette_sortie_1.wav` | 1.20 s | -5.9 | -25.3 | -14.0 | -14.0 | 0.0 ms | 87 · 6 · 6 · 1 · 0 | ok |
| `squelette_touche_1.wav` | 0.25 s | -1.4 | -23.2 | -16.2 | -16.0 | 0.0 ms | 0 · 10 · 78 · 11 · 1 | ok |

## Statuts — `Assets/Audio/Deathless/Statuts/synth_statuts.py`

Statuts (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `brulure_boucle.wav` | 1.00 s (boucle) | -10.0 | -26.6 | -22.0 | -22.0 | 2.9 ms | 83 · 12 · 1 · 2 · 2 | ok |
| `etourdi_boucle.wav` | 1.00 s (boucle) | -6.9 | -27.9 | -22.0 | -22.0 | 10.0 ms | 0 · 0 · 45 · 54 · 1 | ok |
| `ivresse_debut_1.wav` | 0.80 s | -8.0 | -21.7 | -17.0 | -17.0 | 0.2 ms | 0 · 99 · 1 · 0 · 0 | ok |
| `renverse_chute.wav` | 0.80 s | -6.6 | -24.6 | -14.0 | -14.0 | 3.0 ms | 97 · 1 · 2 · 0 · 0 | ok |

## Viking — `Assets/Audio/Deathless/Viking/synth_viking.py`

Viking, hache à deux mains (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `hache_elan_1.wav` | 0.40 s | -6.0 | -20.3 | -14.0 | -14.0 | 3.4 ms | 85 · 7 · 6 · 1 · 1 | ok |
| `rugissement_1.wav` | 1.60 s | -3.8 | -17.7 | -12.5 | -12.5 | 1.9 ms | 30 · 61 · 9 · 0 · 0 | ok |
| `saut_percutant_impact_1.wav` | 1.00 s | -6.3 | -22.8 | -13.0 | -13.0 | 0.0 ms | 99 · 1 · 0 · 0 · 0 | ok |

## Village — `Assets/Audio/Deathless/Village/synth_village.py`

Village, or, coffres, taverne (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `coffre_ouvre_1.wav` | 1.00 s | -5.6 | -26.6 | -14.0 | -14.0 | 0.1 ms | 92 · 1 · 4 · 2 · 0 | ok |
| `or_caisse_1.wav` | 0.60 s | -2.4 | -23.6 | -15.0 | -15.0 | 11.7 ms | 11 · 1 · 27 · 58 · 3 | ok |
| `taverne_biere.wav` | 1.40 s | -7.7 | -23.0 | -16.0 | -16.0 | 1.7 ms | 46 · 36 · 16 · 1 · 1 | ok |
