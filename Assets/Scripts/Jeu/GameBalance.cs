using UnityEngine;

namespace Deathless.Jeu
{
    /// Toutes les valeurs chiffrées de la version 0.1 (défense solo, Paladin). Les valeurs du wiki sont reprises telles
    /// quelles ; les autres sont des valeurs de départ « à équilibrer » (liste dans le rapport de la 0.1). Un seul asset :
    /// Assets/Jeu/Resources/GameBalance.asset (chargé par Resources si la scène ne le fournit pas).
    [CreateAssetMenu(menuName = "Deathless/Équilibrage (GameBalance)", fileName = "GameBalance")]
    public class GameBalance : ScriptableObject
    {
        static GameBalance s_Courant;
        public static GameBalance Courant
        {
            get
            {
                if (s_Courant == null) s_Courant = Resources.Load<GameBalance>("GameBalance");
                if (s_Courant == null) s_Courant = CreateInstance<GameBalance>();
                return s_Courant;
            }
            set { s_Courant = value; }
        }

        [Header("Cycle (wiki : deroule)")]
        public float dureeJour = 120f;
        public float dureeCrepuscule = 5f;
        public float dureeNuit = 120f;
        public float dureeAube = 5f;
        [Tooltip("Alerte avant le crépuscule (s).")]
        public float alerteAvantNuit = 15f;
        [Tooltip("Tous prêts : le jour est ramené à ce temps restant (s).")]
        public float compteAReboursPret = 5f;
        public int nuitsPourGagner = 12;

        [Header("Vagues (wiki : deroule)")]
        [Tooltip("Ennemis par nuit pour un joueur (nuits 1 à 12).")]
        public int[] ennemisParNuit = { 8, 12, 16, 20, 25, 30, 34, 38, 42, 44, 46, 48 };
        [Tooltip("Clairières actives par nuit (nuits 1 à 12).")]
        public int[] clairieresParNuit = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3 };
        public float[] departsTroisVagues = { 0f, 40f, 80f };
        public float[] departsQuatreVagues = { 0f, 30f, 60f, 90f };
        [Tooltip("Première nuit à quatre vagues.")]
        public int nuitQuatreVagues = 9;
        [Tooltip("Répartition des ennemis entre les vagues (à équilibrer).")]
        public float[] partsTroisVagues = { 0.30f, 0.35f, 0.35f };
        public float[] partsQuatreVagues = { 0.22f, 0.24f, 0.26f, 0.28f };
        [Tooltip("Durée sur laquelle les sorties d'une vague sont étalées (s).")]
        public float etalementVague = 8f;
        [Tooltip("Part de guerriers par nuit (les voleurs, mages et élites de la 0.1 sont des guerriers).")]
        public float[] partGuerriers = { 0f, 0.25f, 0.4f, 0.4f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
        [Tooltip("Multiplicateur de PV par nuit (wiki : +10 % dès la nuit 9, +20 % dès la nuit 11).")]
        public float[] multiplicateurPV = { 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        [Tooltip("Joueurs en plus : +60 % d'ennemis par joueur (wiki).")]
        public float ennemisParJoueurEnPlus = 0.6f;
        [Tooltip("Plafond de squelettes sur le terrain (wiki).")]
        public int plafondSquelettes = 60;
        [Tooltip("Mini-boss (Golem) nuit 10 et boss final (Nécromancien) nuit 12.")]
        public bool bossActives = true;
        public int nuitGolem = 10;
        public int nuitNecromancien = 12;
        [Tooltip("Étalement de la désintégration à l'aube (s).")]
        public float etalementAube = 1.5f;

        [Header("Squelettes ordinaires")]
        public StatsSquelette sbire = new StatsSquelette { pv = 100, vitesse = 3.4f, degatsJoueur = 8, degatsNyxessa = 8, intervalle = 1.8f, preparation = 0.7f, portee = 1.8f };
        public StatsSquelette guerrier = new StatsSquelette { pv = 160, vitesse = 3.0f, degatsJoueur = 14, degatsNyxessa = 14, intervalle = 2.2f, preparation = 0.8f, portee = 2.0f };
        [Tooltip("Un joueur vivant plus proche que ça est poursuivi (m).")]
        public float detectionJoueur = 8f;
        [Tooltip("Abandon de la poursuite au-delà (m).")]
        public float abandonPoursuite = 15f;
        [Tooltip("Abandon après ce temps sans pouvoir frapper le joueur (s).")]
        public float abandonApres = 4f;
        [Tooltip("Un squelette plus près que ça du centre de Nyxessa la frappe (m, horizontal).")]
        public float rayonContactNyxessa = 3.2f;
        [Tooltip("Rayon des places autour de Nyxessa (m).")]
        public float rayonPlacesNyxessa = 2.4f;
        [Tooltip("Vitesse de lecture du clip de sortie de terre.")]
        public float vitesseSortieDeTerre = 1.5f;
        [Tooltip("Échelle des personnages Rig_Medium (Knight, squelettes) : 2 m environ.")]
        public float echellePersonnages = 0.8f;

        [Header("Golem (mini-boss, nuit 10)")]
        public float golemPV = 1500f;
        public float golemVitesse = 2.0f;
        public float golemDegatsJoueur = 45f;
        public float golemDegatsNyxessa = 60f;
        public float golemRayonCoup = 3f;
        public float golemPreparation = 1.6f;
        public float golemIntervalle = 4f;
        public float golemEchelle = 0.8f;

        [Header("Nécromancien (boss final, nuit 12)")]
        public float necroPV = 1200f;
        public float necroVitesse = 2.6f;
        public float necroDegats = 18f;
        public float necroVitesseMissile = 12f;
        public float necroPorteeTir = 22f;
        public float necroIntervalleTir = 3f;
        public Vector2 necroDistance = new Vector2(12f, 18f);
        public float necroIntervalleInvocation = 15f;
        public int necroSbiresParInvocation = 3;
        public int necroInvoquesMax = 12;
        public float necroEchelle = 0.92f;

        [Header("Nyxessa (wiki : nyxessa, palier 1)")]
        public float nyxessaPV = 2000f;
        [Tooltip("Part des PV rendue à l'aube (0 : aucune, le wiki n'en parle pas).")]
        public float nyxessaRegenAube = 0f;
        public int missilesStock = 2;
        public float missileRegeneration = 12f;
        public float missileIntervalle = 1.5f;
        public float missileDegats = 40f;
        public float missilePortee = 30f;
        public float missileVitesse = 18f;
        public float missileGuidage = 180f;
        [Tooltip("Groupe : nombre d'ennemis (wiki : 3) dans ce rayon autour de la cible (m).")]
        public int groupeTaille = 3;
        public float groupeRayon = 4f;
        [Tooltip("Nyxessa « frappée » (vide son stock) si un coup date de moins de (s).")]
        public float frappeeRecente = 2f;
        [Tooltip("Délai entre la destruction de Nyxessa et l'écran de score (s).")]
        public float delaiScoreDefaite = 3f;

        [Header("Paladin")]
        public float herosPV = 150f;
        public float endurance = 100f;
        public float enduranceRegen = 15f;
        public float enduranceDelai = 1f;
        public float vitesse = 5f;
        public float sprintMultiplicateur = 1.6f;
        public float sprintCout = 20f;
        public float hauteurSaut = 1.2f;
        public float sautCout = 15f;
        public float gravite = 20f;
        public float esquiveDistance = 4f;
        public float esquiveDuree = 0.35f;
        public float esquiveCout = 25f;
        public float esquiveInvulnerable = 0.3f;
        public float esquiveRecharge = 1.2f;
        [Header("Épée")]
        public float epeeDegats = 30f;
        public float epeeIntervalle = 0.75f;
        public float epeePortee = 2.2f;
        public float epeeDemiAngle = 50f;
        [Tooltip("Instant du coup dans l'attaque (s, clip accéléré).")]
        public float epeeInstant = 0.38f;
        public float epeeVitesseClip = 1.4f;
        public int epeeCiblesParCoup = 1;
        [Header("Garde et parade")]
        public float gardeDemiAngle = 70f;
        public float gardeVitesse = 0.5f;
        [Tooltip("Endurance payée par point de dégâts bloqué.")]
        public float gardeCoutParDegat = 1f;
        public float gardeBriseeEtourdi = 0.8f;
        public float paradeFenetre = 0.25f;
        public float paradeEtourdi = 1f;
        [Header("Charge bélier (précision utilisateur du 25/09/2026)")]
        public float chargeDistance = 7f;
        public float chargeDuree = 0.5f;
        public float chargeAnticipation = 0.18f;
        public float chargeLargeur = 1.4f;
        public float chargeDegatsMin = 15f;
        public float chargeDegatsMax = 60f;
        public float chargeEtourdiCible = 2.5f;
        public float chargeEtourdiRepousses = 0.6f;
        public float chargeRepoussement = 2.5f;
        public float chargeRecharge = 14f;
        [Header("Soin sur soi")]
        public float soinPart = 0.25f;
        public float soinIncantation = 0.6f;
        public float soinRecharge = 30f;
        [Header("Mort et réapparition (wiki : deroule)")]
        public float reapparitionBase = 8f;
        public float reapparitionParMort = 4f;
        public float reapparitionInvulnerable = 2f;
        [Header("Score")]
        [Tooltip("Dégâts évités à Nyxessa : un squelette tué alors qu'il la frappait compte pour ses coups sur cette durée (s).")]
        public float fenetreDegatsEvites = 10f;
        [Tooltip("Un squelette « frappe Nyxessa » s'il l'a touchée depuis moins de (s).")]
        public float surNyxessaDepuis = 3f;

        [Header("Caméra")]
        public float cameraDistance = 4.5f;
        public float cameraEpaule = 0.6f;
        public float cameraHauteur = 1.6f;
        public Vector2 cameraTangage = new Vector2(-30f, 60f);
        public float sensibiliteSouris = 0.12f;
        public float sensibiliteManette = 180f;

        [Header("Développement (mode de test)")]
        [Tooltip("Divise les durées des phases et les horaires des vagues (10 : nuit de 12 s).")]
        public float vitesseCycle = 1f;
        [Range(1, 12)] public int nuitDeDepart = 1;
        [Tooltip("Commence directement au crépuscule de la nuit de départ.")]
        public bool commencerALaNuit;
        public bool joueurInvincible;
        public bool nyxessaInvincible;
        [Tooltip("Saute le menu principal : la partie démarre au chargement de la scène.")]
        public bool lancerDirectement;
        [Tooltip("Journal détaillé (vagues, tirs de Nyxessa).")]
        public bool journal = true;

        // ----------------------------------------------------------------- Aides

        public float Duree(Phase p)
        {
            float v = Mathf.Max(0.01f, vitesseCycle);
            switch (p)
            {
                case Phase.Jour: return dureeJour / v;
                case Phase.Crepuscule: return dureeCrepuscule / v;
                case Phase.Nuit: return dureeNuit / v;
                case Phase.Aube: return dureeAube / v;
                default: return 0f;
            }
        }

        public static T ParNuit<T>(T[] table, int nuit, T defaut)
        {
            if (table == null || table.Length == 0) return defaut;
            return table[Mathf.Clamp(nuit - 1, 0, table.Length - 1)];
        }

        public float DelaiReapparition(int mortsAvant) => reapparitionBase + reapparitionParMort * Mathf.Max(0, mortsAvant);
    }

    [System.Serializable]
    public class StatsSquelette
    {
        public float pv = 100f;
        public float vitesse = 3.4f;
        public float degatsJoueur = 8f;
        public float degatsNyxessa = 8f;
        public float intervalle = 1.8f;
        public float preparation = 0.7f;
        public float portee = 1.8f;
    }
}
