# Planche de contrôle des sons de Deathless, lot 2

Générée par `python -B Assets/Audio/Deathless/controle.py 2` : ne pas modifier à la main, relancer le script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.

Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**) ; **tête** = temps avant le premier échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes < 250 Hz, 250-1k, 1-4k, 4-10k, > 10k. Une **boucle** est vérifiée à sa jointure (pas de saut entre la fin et le début) au lieu du fondu de fin.

**29 fichiers, 29 conformes.**

## Bouclier — `Assets/Audio/Deathless/Bouclier/synth_bouclier.py`

Bouclier de Nyxessa et villageois sorcier (Deathless, lot 2 du cahier des charges son, 26/09/2026).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `bouclier_breche_1.wav` | 0.90 s | -2.4 | -24.7 | -13.0 | -13.0 | 0.0 ms | 0 · 57 · 43 · 0 · 0 | ok |
| `bouclier_breche_2.wav` | 0.90 s | -2.6 | -24.6 | -13.0 | -13.0 | 0.0 ms | 0 · 67 · 32 · 0 · 0 | ok |
| `bouclier_brise.wav` | 2.30 s | -3.0 | -20.2 | -12.5 | -12.5 | 0.0 ms | 80 · 4 · 13 · 2 · 1 | ok |
| `bouclier_etat_critique.wav` | 0.85 s | -7.6 | -20.8 | -15.0 | -15.0 | 0.1 ms | 0 · 86 · 14 · 0 · 0 | ok |
| `bouclier_etat_entame.wav` | 0.85 s | -8.9 | -22.1 | -16.0 | -16.0 | 0.0 ms | 0 · 86 · 14 · 0 · 0 | ok |
| `bouclier_leve.wav` | 1.70 s | -5.3 | -22.0 | -14.0 | -14.0 | 3.0 ms | 0 · 79 · 17 · 2 · 1 | ok |
| `bouclier_palier.wav` | 1.30 s | -3.9 | -24.1 | -14.0 | -14.0 | 3.1 ms | 1 · 94 · 5 · 0 · 0 | ok |
| `bouclier_touche_1.wav` | 0.50 s | -1.4 | -25.1 | -15.7 | -15.0 | 0.0 ms | 0 · 82 · 13 · 4 · 1 | ok |
| `bouclier_touche_2.wav` | 0.50 s | -2.8 | -24.4 | -15.0 | -15.0 | 0.1 ms | 1 · 85 · 10 · 2 · 1 | ok |
| `bouclier_touche_3.wav` | 0.50 s | -1.4 | -26.0 | -16.5 | -15.0 | 0.1 ms | 1 · 76 · 17 · 4 · 2 | ok |
| `bouclier_touche_4.wav` | 0.50 s | -3.2 | -24.5 | -15.0 | -15.0 | 0.1 ms | 0 · 83 · 12 · 3 · 1 | ok |
| `sorcier_canalisation_boucle.wav` | 4.00 s (boucle) | -12.4 | -23.2 | -20.0 | -20.0 | 0.0 ms | 0 · 92 · 8 · 0 · 0 | ok |
| `sorcier_canalisation_eclat_1.wav` | 0.40 s | -5.5 | -25.6 | -17.0 | -17.0 | 0.0 ms | 0 · 2 · 90 · 7 · 1 | ok |
| `sorcier_canalisation_eclat_2.wav` | 0.40 s | -6.1 | -25.6 | -17.0 | -17.0 | 0.0 ms | 0 · 1 · 91 · 7 · 1 | ok |
| `sorcier_incantation_boucle.wav` | 3.00 s (boucle) | -8.1 | -25.7 | -18.0 | -18.0 | 0.1 ms | 0 · 30 · 56 · 9 · 5 | ok |

## Nyxessa — `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py`

Sons de Nyxessa, la relique (Deathless, lots 1 et 2 du cahier des charges son, 26/09/2026).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `nyxessa_missile_eclat_1.wav` | 0.60 s | -5.7 | -22.0 | -15.0 | -15.0 | 0.0 ms | 31 · 6 · 59 · 4 · 0 | ok |
| `nyxessa_missile_eclat_2.wav` | 0.60 s | -6.0 | -22.8 | -15.0 | -15.0 | 0.0 ms | 37 · 1 · 56 · 5 · 1 | ok |
| `nyxessa_missile_eclat_3.wav` | 0.60 s | -5.2 | -23.3 | -15.0 | -15.0 | 0.0 ms | 30 · 14 · 54 · 2 · 0 | ok |
| `nyxessa_missile_vol_boucle.wav` | 1.50 s (boucle) | -8.4 | -19.9 | -17.0 | -17.0 | 0.0 ms | 0 · 58 · 33 · 6 · 2 | ok |
| `nyxessa_rappel.wav` | 1.80 s | -4.6 | -21.2 | -13.0 | -13.0 | 0.0 ms | 43 · 13 · 27 · 12 · 5 | ok |
| `nyxessa_reapparition.wav` | 1.30 s | -4.6 | -21.6 | -14.0 | -14.0 | 3.0 ms | 0 · 23 · 51 · 19 · 7 | ok |

## Portail — `Assets/Audio/Deathless/Portail/synth_portail.py`

Portail et téléportation (Deathless, lot 2 du cahier des charges son, 26/09/2026).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `portail_arrivee.wav` | 1.10 s | -1.8 | -21.3 | -14.0 | -14.0 | 1.0 ms | 0 · 24 · 60 · 12 · 4 | ok |
| `portail_bourdon_boucle.wav` | 6.00 s (boucle) | -11.5 | -23.2 | -20.0 | -20.0 | 0.0 ms | 72 · 26 · 2 · 0 · 0 | ok |
| `portail_chute_ciel.wav` | 1.30 s | -4.3 | -23.1 | -14.0 | -14.0 | 3.1 ms | 55 · 6 · 27 · 8 · 4 | ok |
| `portail_depart.wav` | 1.10 s | -8.6 | -25.2 | -14.0 | -14.0 | 0.1 ms | 33 · 14 · 46 · 5 · 2 | ok |
| `portail_ferme_refus.wav` | 0.40 s | -7.0 | -27.9 | -19.0 | -19.0 | 0.0 ms | 0 · 100 · 0 · 0 · 0 | ok |
| `portail_fermeture.wav` | 1.45 s | -6.3 | -21.4 | -14.0 | -14.0 | 0.1 ms | 34 · 38 · 22 · 4 · 2 | ok |
| `portail_ouverture.wav` | 1.65 s | -4.2 | -17.6 | -14.0 | -14.0 | 0.2 ms | 35 · 44 · 18 · 2 · 1 | ok |
| `portail_sortie_sol.wav` | 1.30 s | -8.1 | -22.4 | -14.0 | -14.0 | 0.6 ms | 71 · 25 · 4 · 0 · 0 | ok |
