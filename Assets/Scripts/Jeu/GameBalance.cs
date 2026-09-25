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

        [Header("Nyxessa (wiki : nyxessa ; missiles par palier, achetés à la relique)")]
        public float nyxessaPV = 2000f;
        [Tooltip("Part des PV rendue à l'aube (0 : aucune, le wiki n'en parle pas).")]
        public float nyxessaRegenAube = 0f;
        [Tooltip("Stock de missiles par palier (1 à 5).")]
        public int[] missilesStockPaliers = { 2, 3, 4, 6, 8 };
        [Tooltip("Régénération d'un missile par palier (s).")]
        public float[] missileRegenerationPaliers = { 12f, 10f, 8f, 6.5f, 5f };
        [Tooltip("Intervalle minimal entre deux tirs par palier (s).")]
        public float[] missileIntervallePaliers = { 1.5f, 1.2f, 1f, 0.8f, 0.6f };
        [Tooltip("Dégâts d'un missile par palier.")]
        public float[] missileDegatsPaliers = { 40f, 55f, 75f, 100f, 130f };
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

        [Header("Bouclier du sorcier (wiki : nyxessa, Bouclier ; 5 paliers, achetés à la relique)")]
        [Tooltip("Dégâts absorbés par palier (1 à 5).")]
        public float[] bouclierEncaissement = { 150f, 260f, 370f, 480f, 600f };
        [Tooltip("Dégâts renvoyés à l'attaquant, à chaque coup, par palier (1 à 5).")]
        public float[] bouclierRenvoi = { 5f, 9f, 12f, 16f, 20f };
        [Tooltip("Durée de l'incantation qui lève le bouclier (s).")]
        public float bouclierIncantation = 3f;
        [Tooltip("Rayon du cylindre (m) : il repose sur le giron de la marche du bas du plateau de Nyxessa (apothème 5,36 m).")]
        public float bouclierRayon = 5.3f;
        [Tooltip("Hauteur de la base du bouclier : dessus de la marche du bas du plateau (m).")]
        public float bouclierBase = 0.25f;
        public float bouclierHauteur = 6f;

        [Header("Sorcier (villageois ; wiki : village)")]
        [Tooltip("Nom de sa maison (objet sous Maisons) : il y passe le jour.")]
        public string sorcierMaison = "Maison_5_A";
        public float sorcierVitesse = 3.2f;
        public float sorcierPV = 60f;
        [Tooltip("Distance à Nyxessa de sa place d'incantation, du côté de sa maison (m) : sur le sommet du plateau, à l'intérieur du bouclier près de son bord, avec la place de tomber en arrière sans toucher le rocher.")]
        public float sorcierDistanceNyxessa = 3.6f;
        [Tooltip("Il fait face à l'extérieur, dos à Nyxessa ; il peut se tourner vers un ennemi devant lui de ce nombre de degrés au plus.")]
        public float sorcierPivotMax = 20f;

        [Header("Or des vagues (règle provisoire sans donjon ; wiki : ennemis, deroule)")]
        public int orSbire = 5;
        public int orGuerrier = 8;
        public int orVoleur = 8;
        public int orMage = 10;
        public int orElite = 25;
        public int orMorgrim = 150;
        public int orNyxar = 300;

        [Header("Achats à la relique (wiki : nyxessa, Paliers ; 26/09/2026)")]
        [Tooltip("Prix des paliers 2 à 5 (or de la caisse commune), pour les missiles comme pour le bouclier.")]
        public int[] prixPaliers = { 100, 200, 350, 550 };
        [Tooltip("Distance horizontale au centre de Nyxessa pour ouvrir le menu d'achat (m) : le plateau et ses abords.")]
        public float achatDistance = 9f;

        public const int PalierMax = 5;

        /// Valeur d'un tableau par palier (1 à 5 ; bornée aux extrémités).
        public static T AuPalier<T>(T[] valeurs, int palier) => valeurs == null || valeurs.Length == 0 ? default : valeurs[Mathf.Clamp(palier, 1, valeurs.Length) - 1];
        public float Palier(float[] valeurs, int palier) => AuPalier(valeurs, palier);
        /// Prix du palier suivant (depuis `palier`), ou -1 au palier maximal.
        public int PrixPalierSuivant(int palier) => palier >= 1 && palier < PalierMax && palier - 1 < prixPaliers.Length ? prixPaliers[palier - 1] : -1;

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
        [Tooltip("Portée de l'épée (m). 2,6 : un peu plus que la hache à deux mains du Viking (26/09/2026).")]
        public float epeePortee = 2.6f;
        [Tooltip("Demi-angle du coup vers l'avant (°) : touche plusieurs ennemis en face, moins large que la hache du Viking (70°).")]
        public float epeeDemiAngle = 40f;
        [Tooltip("Instant du coup dans l'attaque (s, clip accéléré).")]
        public float epeeInstant = 0.38f;
        public float epeeVitesseClip = 1.4f;
        [Tooltip("Ennemis touchés au plus par coup (les plus proches, dans l'angle vers l'avant).")]
        public int epeeCiblesParCoup = 3;
        [Tooltip("Pas en avant au début de l'attaque (m) ; aucun s'il y a déjà un ennemi au contact devant lui.")]
        public float epeePas = 0.6f;
        [Tooltip("Durée du pas en avant (s).")]
        public float epeePasDuree = 0.18f;
        [Tooltip("Vitesse de déplacement pendant le reste de l'attaque (facteur).")]
        public float epeeVitesse = 0.4f;
        [Header("Garde et parade")]
        public float gardeDemiAngle = 70f;
        [Tooltip("Vitesse en garde (facteur). 0,7 : paladin plus mobile (26/09/2026).")]
        public float gardeVitesse = 0.7f;
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
        [Header("Mage, style feu (valeurs de départ, à équilibrer)")]
        public float magePV = 100f;
        public float mageVitesse = 5f;
        public float bouleDegats = 25f;
        public float bouleDegatsZone = 15f;
        public float bouleRayon = 2f;
        public float bouleIntervalle = 0.9f;
        public float bouleVitesse = 18f;
        public float boulePortee = 30f;
        [Tooltip("Instant où la boule quitte le bâton dans le geste (s, clip Ranged_Magic_Shoot accéléré).")]
        public float bouleInstant = 0.28f;
        public float manaMax = 100f;
        [Tooltip("Mana rendu par seconde (wiki : environ 1).")]
        public float manaRegen = 1f;
        [Tooltip("Mana rendu par ennemi touché par la boule de feu (wiki : bonus).")]
        public float manaParTouche = 4f;
        public float coneMana = 14f;
        public float coneDegats = 22f;
        public float conePortee = 6f;
        public float coneDemiAngle = 20f;
        public float coneVitesse = 0.4f;
        public float brulureDegats = 5f;
        public float brulureDuree = 3f;

        [Header("Projectiles (wiki classes.md : vitesse et pesanteur)")]
        [Tooltip("Pesanteur des flèches et carreaux (m/s², réelle : 9,81).")]
        public float projectileGravite = 9.81f;
        [Tooltip("Aide à la visée : relèvement maximal (degrés) ajouté pour compenser la chute ; au-delà, le joueur vise au-dessus.")]
        public float aideChuteMax = 3f;
        [Tooltip("Longueur de vol maximale d'une flèche ou d'un carreau (m) avant de disparaître.")]
        public float projectileVolMax = 200f;

        [Header("Rôdeur (wiki : charge 1,2 s, 10 à 40 dégâts, tête ×2)")]
        public float rodeurPV = 110f;
        public float rodeurVitesse = 5f;
        public float arcCharge = 1.2f;
        public float arcDegatsMin = 10f;
        public float arcDegatsMax = 40f;
        public float arcTete = 2f;
        [Tooltip("Vitesse de départ de la flèche selon la charge (m/s) : tir rapide → minimum, charge complète → maximum (wiki : projectiles).")]
        public float arcVitesseMin = 18f;
        public float arcVitesseMax = 55f;
        [Tooltip("Distance du point visé par le réticule (m).")]
        public float arcPortee = 60f;
        public float arcIntervalle = 0.3f;
        public float arcVitesseBander = 0.5f;
        public float viseeVitesse = 0.6f;
        public float nueeRayon = 3f;
        public int nueeSalves = 5;
        public float nueeDegatsSalve = 10f;
        public float nueePortee = 25f;
        public float nueeRecharge = 12f;
        public float rouladeDistance = 4f;
        public float rouladeCout = 20f;
        public int salveFleches = 5;
        public float salveEcart = 20f;
        public float salveDegats = 15f;
        [Tooltip("Vitesse des flèches de la salve de la roulade (m/s).")]
        public float salveVitesse = 35f;
        public float rouladeRecharge = 8f;

        [Header("Assassin (wiki : ×2 furtif, ×3 dos, ×5 les deux ; détection 6 m / 1,5 m ; arbalète 6 s ; grenade 20 s, nuage 5 s)")]
        public float assassinPV = 100f;
        public float assassinVitesse = 5f;
        public float marcheDiscrete = 3.2f;
        public float dagueDegats = 20f;
        public float dagueIntervalle = 0.55f;
        public float daguePortee = 1.8f;
        public float dagueDemiAngle = 45f;
        public float dagueInstant = 0.25f;
        public float critiqueFurtif = 2f;
        public float critiqueDos = 3f;
        public float critiqueFurtifDos = 5f;
        [Tooltip("Angle (degrés) au-delà duquel un coup est « dans le dos ».")]
        public float angleDos = 120f;
        public float assassinDetectionVue = 6f;
        public float assassinDetectionAngle = 60f;
        public float assassinDetectionDos = 1.5f;
        [Tooltip("Hors combat : aucun coup donné, reçu ni repérage depuis (s).")]
        public float horsCombat = 4f;
        public float arbaleteDegats = 45f;
        public float arbaleteTete = 2f;
        [Tooltip("Vitesse fixe du carreau (m/s).")]
        public float arbaleteVitesse = 60f;
        public float arbaletePortee = 40f;
        public float arbaleteRecharge = 6f;
        public float grenadeRecharge = 20f;
        public float grenadeNuage = 5f;
        public float grenadePortee = 8f;

        [Header("Viking (valeurs de départ, à équilibrer)")]
        public float vikingPV = 140f;
        public float vikingVitesse = 5f;
        public float hacheDegats = 38f;
        public float hacheIntervalle = 1.1f;
        public float hachePortee = 2.4f;
        public float hacheDemiAngle = 70f;
        public float hacheInstant = 0.55f;
        public float rageMax = 100f;
        public float rageParTouche = 8f;
        public float rageBaisse = 6f;
        public float rageDelaiBaisse = 4f;
        public float tournanteRage = 20f;
        public float tournanteRageMin = 15f;
        public float tournanteIntervalle = 0.3f;
        public float tournanteRayon = 2.3f;
        public float tournanteDegats = 12f;
        public float tournanteVitesse = 0.6f;
        public float rugissementRage = 25f;
        public float rugissementRecharge = 12f;
        public float rugissementRayon = 10f;
        public float rugissementProvocation = 5f;
        public float sautRage = 35f;
        public float sautRecharge = 8f;
        public float sautDistance = 5f;
        public float sautRayon = 3.5f;
        public float sautDegats = 45f;
        public float sautEtourdi = 1f;

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
        [Tooltip("Classe lancée par « lancerDirectement » (paladin, mage, rodeur, assassin, viking).")]
        public string classeDeTest = "paladin";
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
