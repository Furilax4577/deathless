# Planche de contrôle des sons de Deathless, lots 1 et 2, direction sombre

Générée par `python -B Assets/Audio/Deathless/controle.py` : ne pas modifier à la main, relancer le script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.

Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**, sauf pour les ambiances et les musiques, réglées sur le RMS moyen) ; **tête** = temps avant le premier échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes < 250 Hz, 250-1k, 1-4k, 4-10k, > 10k. Une **boucle** est vérifiée à sa jointure (pas de saut entre la fin et le début) au lieu du fondu de fin.

**53 fichiers, 53 conformes.**

## Bouclier — `Assets/Audio/Deathless/Bouclier/synth_bouclier.py`

Bouclier de Nyxessa et villageois sorcier (Deathless, lot 2, regénéré sous la direction sombre le 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `bouclier_breche_1.wav` | 0.90 s | -3.0 | -23.4 | -13.0 | -13.0 | 0.0 ms | 23 · 64 · 13 · 0 · 0 | ok |
| `bouclier_breche_2.wav` | 0.90 s | -3.5 | -23.6 | -13.0 | -13.0 | 0.0 ms | 20 · 69 · 11 · 0 · 0 | ok |
| `bouclier_brise.wav` | 2.30 s | -2.6 | -21.6 | -12.5 | -12.5 | 0.0 ms | 47 · 44 · 8 · 0 · 1 | ok |
| `bouclier_etat_critique.wav` | 0.85 s | -6.9 | -19.5 | -15.0 | -15.0 | 0.0 ms | 19 · 81 · 0 · 0 · 0 | ok |
| `bouclier_etat_entame.wav` | 0.85 s | -8.0 | -21.2 | -16.0 | -16.0 | 0.1 ms | 20 · 79 · 0 · 0 · 0 | ok |
| `bouclier_leve.wav` | 1.70 s | -5.9 | -17.1 | -14.0 | -14.0 | 0.0 ms | 72 · 28 · 0 · 0 · 0 | ok |
| `bouclier_palier.wav` | 1.30 s | -6.7 | -17.9 | -14.0 | -14.0 | 0.0 ms | 57 · 43 · 0 · 0 · 0 | ok |
| `bouclier_touche_1.wav` | 0.50 s | -3.3 | -24.9 | -15.0 | -15.0 | 0.0 ms | 91 · 5 · 4 · 0 · 0 | ok |
| `bouclier_touche_2.wav` | 0.50 s | -3.7 | -24.9 | -15.0 | -15.0 | 0.0 ms | 94 · 3 · 3 · 0 · 0 | ok |
| `bouclier_touche_3.wav` | 0.50 s | -2.2 | -24.9 | -15.0 | -15.0 | 0.0 ms | 91 · 5 · 3 · 0 · 0 | ok |
| `bouclier_touche_4.wav` | 0.50 s | -2.8 | -24.9 | -15.0 | -15.0 | 0.0 ms | 92 · 4 · 4 · 0 · 0 | ok |
| `sorcier_canalisation_boucle.wav` | 4.00 s (boucle) | -13.5 | -22.6 | -20.0 | -20.0 | 0.0 ms | 97 · 3 · 0 · 0 · 0 | ok |
| `sorcier_canalisation_eclat_1.wav` | 0.40 s | -10.9 | -23.3 | -17.0 | -17.0 | 0.0 ms | 4 · 95 · 1 · 0 · 0 | ok |
| `sorcier_canalisation_eclat_2.wav` | 0.40 s | -10.1 | -22.7 | -17.0 | -17.0 | 0.0 ms | 4 · 95 · 1 · 0 · 0 | ok |
| `sorcier_incantation_boucle.wav` | 3.00 s (boucle) | -10.2 | -20.7 | -18.0 | -18.0 | 0.0 ms | 32 · 67 · 0 · 0 · 0 | ok |

## Interface — `Assets/Audio/Deathless/Interface/synth_interface.py`

Sons de l'interface (Deathless, lot 1, regénéré sous la direction sombre le 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `interface_clic_1.wav` | 0.16 s | -5.2 | -23.1 | -18.0 | -18.0 | 0.0 ms | 1 · 99 · 0 · 0 · 0 | ok |
| `interface_clic_2.wav` | 0.16 s | -3.9 | -23.0 | -18.0 | -18.0 | 0.0 ms | 1 · 99 · 0 · 0 · 0 | ok |
| `interface_confirmation.wav` | 0.60 s | -7.6 | -23.3 | -16.0 | -16.0 | 0.1 ms | 1 · 99 · 0 · 0 · 0 | ok |
| `interface_decompte.wav` | 0.25 s | -4.8 | -23.9 | -17.0 | -17.0 | 0.0 ms | 100 · 0 · 0 · 0 · 0 | ok |
| `interface_onglet.wav` | 0.13 s | -3.4 | -25.1 | -21.0 | -21.0 | 0.4 ms | 0 · 9 · 83 · 5 · 2 | ok |
| `interface_pret.wav` | 0.45 s | -6.4 | -24.9 | -16.0 | -16.0 | 0.0 ms | 13 · 87 · 0 · 0 · 0 | ok |
| `interface_pret_annule.wav` | 0.35 s | -9.9 | -23.8 | -18.0 | -18.0 | 0.0 ms | 0 · 99 · 0 · 0 · 0 | ok |
| `interface_refus.wav` | 0.30 s | -3.0 | -23.1 | -18.0 | -18.0 | 0.0 ms | 35 · 63 · 2 · 0 · 0 | ok |
| `interface_retour.wav` | 0.22 s | -7.0 | -23.1 | -19.0 | -19.0 | 0.0 ms | 3 · 97 · 0 · 0 · 0 | ok |
| `interface_survol_1.wav` | 0.07 s | -6.6 | -25.5 | -24.0 | -24.0 | 0.0 ms | 1 · 86 · 13 · 0 · 0 | ok |
| `interface_survol_2.wav` | 0.07 s | -4.9 | -25.5 | -24.0 | -24.0 | 0.1 ms | 0 · 0 · 100 · 0 · 0 | ok |
| `interface_tous_prets.wav` | 1.00 s | -5.5 | -21.7 | -14.0 | -14.0 | 0.0 ms | 93 · 7 · 0 · 0 · 0 | ok |

## Nyxessa — `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py`

Sons de Nyxessa, la relique (Deathless, lots 1 et 2, regénérés sous la direction sombre le 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `nyxessa_alerte.wav` | 1.05 s | -6.3 | -19.2 | -13.0 | -13.0 | 0.0 ms | 29 · 68 · 4 · 0 · 0 | ok |
| `nyxessa_charge_portail.wav` | 1.95 s | -5.2 | -18.3 | -14.0 | -14.0 | 0.0 ms | 26 · 71 · 3 · 0 · 0 | ok |
| `nyxessa_destruction.wav` | 3.90 s | -4.1 | -21.3 | -12.5 | -12.5 | 0.0 ms | 29 · 59 · 11 · 0 · 0 | ok |
| `nyxessa_frappee_1.wav` | 0.75 s | -6.3 | -22.7 | -15.0 | -15.0 | 0.0 ms | 20 · 80 · 0 · 0 · 0 | ok |
| `nyxessa_frappee_2.wav` | 0.75 s | -3.9 | -21.5 | -15.0 | -15.0 | 0.0 ms | 10 · 90 · 0 · 0 · 0 | ok |
| `nyxessa_frappee_3.wav` | 0.75 s | -4.0 | -23.6 | -15.0 | -15.0 | 0.0 ms | 23 · 77 · 0 · 0 · 0 | ok |
| `nyxessa_missile_eclat_1.wav` | 0.85 s | -6.0 | -19.0 | -14.0 | -14.0 | 0.0 ms | 12 · 76 · 12 · 0 · 0 | ok |
| `nyxessa_missile_eclat_2.wav` | 0.85 s | -6.1 | -19.4 | -14.0 | -14.0 | 0.0 ms | 15 · 75 · 10 · 0 · 0 | ok |
| `nyxessa_missile_eclat_3.wav` | 0.85 s | -6.4 | -19.2 | -14.0 | -14.0 | 0.0 ms | 8 · 79 · 13 · 0 · 0 | ok |
| `nyxessa_missile_vol_boucle.wav` | 2.00 s (boucle) | -7.3 | -19.4 | -17.0 | -17.0 | 0.0 ms | 23 · 63 · 14 · 0 · 0 | ok |
| `nyxessa_onde.wav` | 1.15 s | -11.1 | -25.1 | -17.0 | -17.0 | 0.2 ms | 3 · 95 · 2 · 0 · 0 | ok |
| `nyxessa_palier.wav` | 2.50 s | -4.0 | -16.1 | -12.5 | -12.5 | 0.0 ms | 63 · 35 · 2 · 0 · 0 | ok |
| `nyxessa_rappel.wav` | 1.80 s | -6.4 | -18.8 | -13.0 | -13.0 | 0.9 ms | 10 · 82 · 6 · 1 · 0 | ok |
| `nyxessa_reapparition.wav` | 1.30 s | -4.7 | -21.4 | -14.0 | -14.0 | 4.0 ms | 15 · 72 · 9 · 2 · 1 | ok |
| `nyxessa_retour_energie.wav` | 1.95 s | -6.2 | -20.1 | -14.0 | -14.0 | 1.4 ms | 33 · 63 · 4 · 0 · 0 | ok |
| `nyxessa_tir_1.wav` | 0.85 s | -7.2 | -22.8 | -14.0 | -14.0 | 0.1 ms | 55 · 38 · 7 · 0 · 0 | ok |
| `nyxessa_tir_2.wav` | 0.85 s | -6.8 | -22.3 | -14.0 | -14.0 | 0.0 ms | 48 · 52 · 1 · 0 · 0 | ok |
| `nyxessa_tir_3.wav` | 0.85 s | -7.7 | -22.4 | -14.0 | -14.0 | 0.0 ms | 55 · 42 · 3 · 0 · 0 | ok |

## Portail — `Assets/Audio/Deathless/Portail/synth_portail.py`

Portail et téléportation (Deathless, lot 2, regénéré sous la direction sombre le 26/09/2026 au soir).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `portail_arrivee.wav` | 1.10 s | -5.3 | -20.6 | -14.0 | -14.0 | 0.7 ms | 7 · 84 · 7 · 1 · 0 | ok |
| `portail_bourdon_boucle.wav` | 6.00 s (boucle) | -11.7 | -22.2 | -20.0 | -20.0 | 0.0 ms | 97 · 2 · 0 · 0 · 0 | ok |
| `portail_chute_ciel.wav` | 1.30 s | -5.1 | -26.5 | -14.0 | -14.0 | 17.5 ms | 84 · 3 · 10 · 3 · 1 | ok |
| `portail_depart.wav` | 1.10 s | -6.9 | -22.0 | -14.0 | -14.0 | 0.0 ms | 12 · 84 · 3 · 0 · 0 | ok |
| `portail_ferme_refus.wav` | 0.45 s | -10.6 | -26.8 | -19.0 | -19.0 | 0.0 ms | 18 · 82 · 0 · 0 · 0 | ok |
| `portail_fermeture.wav` | 1.45 s | -4.9 | -20.0 | -14.0 | -14.0 | 0.8 ms | 78 · 15 · 5 · 1 · 1 | ok |
| `portail_ouverture.wav` | 1.65 s | -7.5 | -18.4 | -14.0 | -14.0 | 0.0 ms | 85 · 12 · 2 · 0 · 0 | ok |
| `portail_sortie_sol.wav` | 1.30 s | -5.7 | -22.8 | -14.0 | -14.0 | 0.0 ms | 80 · 20 · 0 · 0 · 0 | ok |
