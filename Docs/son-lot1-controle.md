# Planche de contrôle des sons de Deathless, lot 1

Générée par `python -B Assets/Audio/Deathless/controle.py` : ne pas modifier à la main, relancer le script après chaque régénération. Méthode et cibles : [cahier des charges son](son-cahier-des-charges.md), § 5.

Colonnes : **crête** en dBFS (plafond -1,4) ; **RMS** moyen sur tout le fichier ; **niveau** perçu = RMS maximal sur 50 ms, en dBFS (c'est lui qui est réglé sur la **cible**) ; **tête** = temps avant le premier échantillon au-dessus de -40 dBFS (20 ms au plus) ; **spectre** = part de l'énergie en % dans les bandes < 250 Hz, 250-1k, 1-4k, 4-10k, > 10k.

**24 fichiers, 24 conformes.**

## Interface — `Assets/Audio/Deathless/Interface/synth_interface.py`

Sons de l'interface (Deathless, lot 1 du cahier des charges son, 26/09/2026).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `interface_clic_1.wav` | 0.16 s | -3.2 | -23.0 | -18.0 | -18.0 | 0.0 ms | 0 · 100 · 0 · 0 · 0 | ok |
| `interface_clic_2.wav` | 0.16 s | -3.9 | -23.0 | -18.0 | -18.0 | 0.0 ms | 0 · 84 · 16 · 0 · 0 | ok |
| `interface_confirmation.wav` | 0.60 s | -7.5 | -22.9 | -16.0 | -16.0 | 0.0 ms | 0 · 86 · 14 · 0 · 0 | ok |
| `interface_decompte.wav` | 0.25 s | -1.4 | -24.6 | -17.6 | -17.0 | 0.0 ms | 0 · 100 · 0 · 0 · 0 | ok |
| `interface_onglet.wav` | 0.13 s | -8.8 | -25.1 | -21.0 | -21.0 | 0.2 ms | 0 · 0 · 58 · 29 · 13 | ok |
| `interface_pret.wav` | 0.45 s | -7.9 | -24.7 | -16.0 | -16.0 | 0.0 ms | 0 · 96 · 4 · 0 · 0 | ok |
| `interface_pret_annule.wav` | 0.35 s | -9.2 | -23.6 | -18.0 | -18.0 | 0.0 ms | 0 · 97 · 3 · 0 · 0 | ok |
| `interface_refus.wav` | 0.30 s | -4.0 | -22.9 | -18.0 | -18.0 | 0.0 ms | 9 · 90 · 1 · 0 · 0 | ok |
| `interface_retour.wav` | 0.22 s | -5.1 | -23.4 | -19.0 | -19.0 | 0.0 ms | 0 · 96 · 3 · 0 · 0 | ok |
| `interface_survol_1.wav` | 0.07 s | -7.9 | -25.5 | -24.0 | -24.0 | 0.0 ms | 0 · 0 · 100 · 0 · 0 | ok |
| `interface_survol_2.wav` | 0.07 s | -7.6 | -25.5 | -24.0 | -24.0 | 0.0 ms | 0 · 0 · 100 · 0 · 0 | ok |
| `interface_tous_prets.wav` | 1.00 s | -6.7 | -21.5 | -14.0 | -14.0 | 0.1 ms | 45 · 26 · 29 · 1 · 0 | ok |

## Nyxessa — `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py`

Sons de Nyxessa, la relique (Deathless, lot 1 du cahier des charges son, 26/09/2026).

| Fichier | Durée | Crête | RMS | Niveau | Cible | Tête | Spectre (%) | Contrôle |
|---|---|---|---|---|---|---|---|---|
| `nyxessa_alerte.wav` | 1.05 s | -3.6 | -20.7 | -13.0 | -13.0 | 0.1 ms | 34 · 0 · 62 · 3 · 0 | ok |
| `nyxessa_charge_portail.wav` | 1.95 s | -2.4 | -17.6 | -14.0 | -14.0 | 4.0 ms | 57 · 5 · 29 · 6 · 3 | ok |
| `nyxessa_destruction.wav` | 3.90 s | -1.7 | -21.2 | -12.5 | -12.5 | 0.1 ms | 79 · 6 · 5 · 9 · 1 | ok |
| `nyxessa_frappee_1.wav` | 0.75 s | -3.1 | -26.0 | -15.0 | -15.0 | 0.1 ms | 3 · 90 · 6 · 0 · 0 | ok |
| `nyxessa_frappee_2.wav` | 0.75 s | -3.2 | -26.0 | -15.0 | -15.0 | 0.1 ms | 2 · 91 · 7 · 0 · 0 | ok |
| `nyxessa_frappee_3.wav` | 0.75 s | -3.5 | -26.4 | -15.0 | -15.0 | 0.1 ms | 5 · 83 · 11 · 1 · 0 | ok |
| `nyxessa_onde.wav` | 1.15 s | -7.5 | -23.2 | -17.0 | -17.0 | 0.1 ms | 0 · 8 · 84 · 7 · 1 | ok |
| `nyxessa_palier.wav` | 2.50 s | -1.7 | -22.2 | -12.5 | -12.5 | 0.0 ms | 24 · 0 · 65 · 9 · 2 | ok |
| `nyxessa_retour_energie.wav` | 1.95 s | -6.8 | -21.7 | -14.0 | -14.0 | 0.5 ms | 44 · 10 · 27 · 14 · 5 | ok |
| `nyxessa_tir_1.wav` | 0.85 s | -3.1 | -24.3 | -14.0 | -14.0 | 0.6 ms | 47 · 3 · 45 · 4 · 1 | ok |
| `nyxessa_tir_2.wav` | 0.85 s | -4.9 | -24.1 | -14.0 | -14.0 | 0.7 ms | 42 · 2 · 50 · 6 · 1 | ok |
| `nyxessa_tir_3.wav` | 0.85 s | -3.9 | -24.2 | -14.0 | -14.0 | 0.5 ms | 46 · 2 · 43 · 7 · 1 | ok |
