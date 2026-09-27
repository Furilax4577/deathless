# Planche de contrôle des sons de Deathless, lot 3

Générée par `python -B Assets/Audio/Deathless/controle.py` : ne pas modifier à la main, relancer le script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.

Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**, sauf pour les ambiances et les musiques, réglées sur le RMS moyen) ; **tête** = temps avant le premier échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes < 250 Hz, 250-1k, 1-4k, 4-10k, > 10k. Une **boucle** est vérifiée à sa jointure (pas de saut entre la fin et le début) au lieu du fondu de fin.

**41 fichiers, 41 conformes.**

## Squelettes — `Assets/Audio/Deathless/Squelettes/synth_squelettes.py`

Squelettes (Deathless, lot 3 du cahier des charges son, direction sombre, 27/09/2026 ; les cinq premiers fichiers

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `elite_aura_boucle.wav` | 3.00 s (boucle) | -13.1 | -24.5 | -22.0 | -22.0 | 0.2 ms | 99 · 1 · 0 · 0 · 0 | ok |
| `guerrier_coup_1.wav` | 0.40 s | -6.8 | -21.4 | -15.0 | -15.0 | 0.0 ms | 90 · 6 · 3 · 1 · 0 | ok |
| `guerrier_coup_2.wav` | 0.40 s | -8.6 | -21.4 | -15.0 | -15.0 | 0.0 ms | 91 · 6 · 3 · 1 · 0 | ok |
| `guerrier_coup_3.wav` | 0.40 s | -6.7 | -21.4 | -15.0 | -15.0 | 0.0 ms | 88 · 8 · 3 · 1 · 0 | ok |
| `mage_squelette_incantation_1.wav` | 0.80 s | -2.4 | -23.0 | -17.0 | -17.0 | 0.1 ms | 1 · 66 · 29 · 4 · 2 | ok |
| `mage_squelette_incantation_2.wav` | 0.80 s | -2.2 | -22.1 | -17.0 | -17.0 | 0.0 ms | 1 · 63 · 31 · 4 · 2 | ok |
| `mage_squelette_missile_eclat_1.wav` | 0.85 s | -8.6 | -22.0 | -15.0 | -15.0 | 0.0 ms | 6 · 84 · 9 · 0 · 0 | ok |
| `mage_squelette_missile_eclat_2.wav` | 0.85 s | -10.1 | -22.8 | -15.0 | -15.0 | 0.0 ms | 4 · 89 · 7 · 0 · 0 | ok |
| `mage_squelette_missile_vol_boucle.wav` | 2.00 s (boucle) | -9.4 | -21.6 | -18.0 | -18.0 | 0.0 ms | 10 · 79 · 11 · 0 · 0 | ok |
| `mage_squelette_tir_1.wav` | 0.50 s | -1.4 | -22.9 | -16.1 | -15.0 | 0.0 ms | 0 · 33 · 53 · 9 · 4 | ok |
| `mage_squelette_tir_2.wav` | 0.50 s | -1.4 | -22.3 | -15.6 | -15.0 | 0.0 ms | 1 · 44 · 44 · 8 · 4 | ok |
| `squelette_aube_1.wav` | 1.50 s | -1.4 | -20.5 | -15.8 | -15.0 | 11.0 ms | 1 · 31 · 54 · 10 · 4 | ok |
| `squelette_aube_2.wav` | 1.50 s | -1.4 | -23.7 | -18.1 | -15.0 | 9.5 ms | 1 · 34 · 50 · 10 · 5 | ok |
| `squelette_coup_1.wav` | 0.30 s | -4.8 | -22.5 | -16.0 | -16.0 | 0.6 ms | 0 · 32 · 54 · 9 · 4 | ok |
| `squelette_coup_2.wav` | 0.30 s | -3.3 | -22.5 | -16.0 | -16.0 | 1.0 ms | 1 · 30 · 55 · 10 · 4 | ok |
| `squelette_coup_3.wav` | 0.30 s | -4.7 | -22.5 | -16.0 | -16.0 | 0.8 ms | 1 · 35 · 52 · 8 · 4 | ok |
| `squelette_danse_boucle.wav` | 2.00 s (boucle) | -2.3 | -29.6 | -20.0 | -20.0 | 0.0 ms | 0 · 67 · 28 · 4 · 0 | ok |
| `squelette_etourdi_1.wav` | 0.60 s | -1.4 | -27.5 | -19.6 | -17.0 | 0.0 ms | 0 · 52 · 43 · 5 · 0 | ok |
| `squelette_etourdi_2.wav` | 0.60 s | -1.4 | -26.8 | -19.0 | -17.0 | 0.0 ms | 0 · 56 · 38 · 5 · 0 | ok |
| `squelette_mort_1.wav` | 1.00 s | -2.5 | -21.8 | -15.0 | -15.0 | 11.0 ms | 40 · 36 · 22 · 2 · 0 | ok |
| `squelette_mort_2.wav` | 1.00 s | -1.4 | -20.9 | -15.5 | -15.0 | 2.1 ms | 36 · 44 · 19 · 1 · 0 | ok |
| `squelette_mort_3.wav` | 1.00 s | -1.4 | -23.5 | -18.1 | -15.0 | 0.7 ms | 32 · 40 · 26 · 1 · 0 | ok |
| `squelette_pas_1.wav` | 0.12 s | -9.4 | -31.8 | -28.0 | -28.0 | 0.1 ms | 59 · 18 · 9 · 11 · 4 | ok |
| `squelette_pas_2.wav` | 0.12 s | -9.2 | -31.8 | -28.0 | -28.0 | 0.1 ms | 44 · 13 · 23 · 15 · 4 | ok |
| `squelette_pas_3.wav` | 0.12 s | -11.2 | -31.8 | -28.0 | -28.0 | 0.1 ms | 42 · 7 · 28 · 19 · 5 | ok |
| `squelette_pas_4.wav` | 0.12 s | -10.1 | -31.8 | -28.0 | -28.0 | 0.0 ms | 40 · 7 · 36 · 12 · 6 | ok |
| `squelette_preparation_1.wav` | 0.70 s | -1.4 | -21.7 | -17.2 | -16.0 | 0.0 ms | 0 · 14 · 78 · 7 · 1 | ok |
| `squelette_preparation_2.wav` | 0.70 s | -1.4 | -21.2 | -16.5 | -16.0 | 0.1 ms | 0 · 15 · 76 · 8 · 1 | ok |
| `squelette_preparation_3.wav` | 0.70 s | -1.4 | -21.3 | -16.8 | -16.0 | 0.0 ms | 0 · 13 · 78 · 8 · 1 | ok |
| `squelette_repousse_1.wav` | 0.40 s | -7.9 | -21.8 | -17.0 | -17.0 | 1.5 ms | 43 · 53 · 3 · 0 · 0 | ok |
| `squelette_repousse_2.wav` | 0.40 s | -6.7 | -21.3 | -17.0 | -17.0 | 0.5 ms | 33 · 59 · 7 · 1 · 0 | ok |
| `squelette_repousse_3.wav` | 0.40 s | -6.0 | -21.9 | -17.0 | -17.0 | 0.7 ms | 43 · 54 · 3 · 0 · 0 | ok |
| `squelette_sortie_1.wav` | 1.20 s | -5.9 | -25.3 | -14.0 | -14.0 | 0.0 ms | 87 · 6 · 6 · 1 · 0 | ok |
| `squelette_sortie_2.wav` | 1.20 s | -7.0 | -25.4 | -14.0 | -14.0 | 0.0 ms | 88 · 5 · 6 · 1 · 0 | ok |
| `squelette_sortie_3.wav` | 1.20 s | -6.2 | -25.8 | -14.0 | -14.0 | 0.0 ms | 88 · 5 · 6 · 1 · 0 | ok |
| `squelette_touche_1.wav` | 0.25 s | -1.4 | -23.2 | -16.2 | -16.0 | 0.0 ms | 0 · 10 · 78 · 11 · 1 | ok |
| `squelette_touche_2.wav` | 0.25 s | -1.4 | -24.9 | -18.0 | -16.0 | 0.0 ms | 0 · 8 · 82 · 9 · 1 | ok |
| `squelette_touche_3.wav` | 0.25 s | -1.4 | -25.4 | -18.4 | -16.0 | 0.1 ms | 0 · 15 · 75 · 10 · 1 | ok |
| `squelette_touche_4.wav` | 0.25 s | -1.4 | -24.4 | -17.6 | -16.0 | 0.0 ms | 0 · 7 · 80 · 13 · 0 | ok |
| `voleur_elan_1.wav` | 0.50 s | -4.7 | -22.0 | -16.0 | -16.0 | 0.0 ms | 0 · 6 · 59 · 25 · 10 | ok |
| `voleur_elan_2.wav` | 0.50 s | -4.6 | -22.1 | -16.0 | -16.0 | 0.0 ms | 0 · 10 · 57 · 22 · 10 | ok |
