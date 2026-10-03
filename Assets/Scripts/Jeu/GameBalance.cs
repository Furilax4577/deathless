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
        [Tooltip("Ennemis par nuit pour un joueur (nuits 1 à 12). Nuits 1 à 5 densifiées le 28/09/2026 (Quentin : « les " +
            "premières vagues sont trop molles ») : 8/12/16/20/25 → 12/18/22/26/28 ; nuits 6 à 12 inchangées.")]
        public int[] ennemisParNuit = { 12, 18, 22, 26, 28, 30, 34, 38, 42, 44, 46, 48 };
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
        [Tooltip("Part de guerriers par nuit (nuits 1 à 12). Depuis le 28/09/2026 les voleurs et les mages ont leur propre part " +
            "(partVoleurs, partMages) : la part de guerriers, qui les englobait (0,4 dès la nuit 3, 0,5 dès la nuit 5), est " +
            "ramenée à 0,3 → 0,4 pour que les sbires restent au moins 30 % des sorties. Les élites restent des guerriers renforcés.")]
        public float[] partGuerriers = { 0f, 0.25f, 0.3f, 0.3f, 0.35f, 0.35f, 0.38f, 0.38f, 0.4f, 0.4f, 0.4f, 0.4f };
        [Tooltip("Part de voleurs par nuit (nuits 1 à 12 ; wiki : ennemis.md, décidé 28/09/2026) : aucun les nuits 1 et 2, " +
            "dès la nuit 3 incluse. Tirage d'une sortie (DirecteurVagues.TirerType) : mage, puis voleur, puis guerrier, le reste en sbire.")]
        public float[] partVoleurs = { 0f, 0f, 0.12f, 0.14f, 0.15f, 0.15f, 0.16f, 0.16f, 0.16f, 0.16f, 0.16f, 0.16f };
        [Tooltip("Part de mages (lanceurs de crâne) par nuit (nuits 1 à 12 ; décidé 28/09/2026, wiki : ennemis.md) : dès la nuit 5 incluse (30/09/2026 : les 8 et 10 % des nuits 3 et 4 retirés), plus nombreux nuit 6.")]
        public float[] partMages = { 0f, 0f, 0f, 0f, 0.10f, 0.12f, 0.12f, 0.12f, 0.14f, 0.14f, 0.14f, 0.14f };
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
        [Tooltip("Voleur (wiki : ennemis.md ; 28/09/2026, à équilibrer) : rapide, moins solide qu'un guerrier, coup court à préparer ; " +
            "il chasse les joueurs isolés (Voleur.cs).")]
        public StatsSquelette voleur = new StatsSquelette { pv = 115, vitesse = 4.4f, degatsJoueur = 12, degatsNyxessa = 10, intervalle = 1.4f, preparation = 0.5f, portee = 1.7f };
        [Tooltip("Un joueur est « isolé » si aucun autre joueur vivant n'est à moins de cette distance (m) : le voleur le préfère à tout autre.")]
        public float voleurDistanceIsolement = 10f;
        [Tooltip("Le voleur repère un joueur isolé jusqu'à cette distance (m), plus loin que la détection ordinaire (detectionJoueur) ; " +
            "en dessous de abandonPoursuite pour qu'il ne lâche pas la chasse aussitôt.")]
        public float voleurDistanceChasse = 14f;
        [Tooltip("Mage squelette, ennemi (wiki : ennemis.md, Mage ; 28/09/2026, à équilibrer ; le mage héros est plus bas : magePV…) : " +
            "fragile, tire le missile crâne à taille normale. « intervalle » = temps entre deux tirs, « preparation » = incantation " +
            "avant le départ du crâne, « portee » = portée de tir ; il n'a pas de coup de mêlée.")]
        public StatsSquelette mage = new StatsSquelette { pv = 70, vitesse = 3.2f, degatsJoueur = 12, degatsNyxessa = 12, intervalle = 2.6f, preparation = 0.45f, portee = 7.5f };
        [Tooltip("Le mage garde ses distances (m) : il approche au-delà de y, recule en deçà de x (hors du bouclier de Nyxessa, rayon 5,3 m).")]
        public Vector2 mageDistanceTir = new Vector2(5.5f, 7f);
        public float mageVitesseMissile = 11f;
        [Tooltip("Un joueur vivant plus proche que ça est poursuivi (m).")]
        public float detectionJoueur = 8f;
        [Tooltip("Abandon de la poursuite au-delà (m).")]
        public float abandonPoursuite = 15f;
        [Tooltip("Abandon après ce temps sans pouvoir frapper le joueur (s).")]
        public float abandonApres = 4f;

        [Header("Course et esquive des squelettes (Quentin, 30/09/2026 ; wiki : ennemis.md, à équilibrer)")]
        [Tooltip("Sbire, guerrier, voleur (pas le mage ni les boss) qui poursuivent un héros loin d'eux : vitesse × ce facteur " +
            "(30/09/2026). Jamais en marche vers Nyxessa (les vagues gardent leur rythme).")]
        public float courseFacteur = 1.45f;
        [Tooltip("Plafond de la course (m/s) : pas plus vite qu'un héros qui court sans sprint (vitesse = 5) ; le sprint distance toujours.")]
        public float courseVitesseMax = 5f;
        [Tooltip("Le squelette se met à courir quand sa cible est plus loin que ça (m, horizontal)…")]
        public float courseDistance = 6f;
        [Tooltip("… et repasse au pas de charge en deçà de ça (m) : écart pour ne pas alterner à chaque image.")]
        public float courseDistanceArret = 4f;
        [Tooltip("Esquive (30/09/2026) : un héros à moins de cette distance (m), tourné vers le squelette, qui arme une attaque " +
            "(RT : coup de mêlée, tir, boule) peut être esquivé.")]
        public float esquiveEnnemiDetection = 3.5f;
        [Tooltip("Le héros « fait face » au squelette si l'écart entre son avant et la direction du squelette est sous cet angle (°).")]
        public float esquiveEnnemiAngle = 60f;
        [Tooltip("Délai pendant lequel une attaque armée peut encore déclencher l'esquive (s) : réaction au geste, pas à un vieux coup.")]
        public float esquiveEnnemiFenetre = 0.3f;
        [Tooltip("Chance d'esquiver chaque attaque armée, par type (0 à 1) : sbire.")]
        public float esquiveChanceSbire = 0.10f;
        [Tooltip("Chance d'esquiver : guerrier (lourd, rarement).")]
        public float esquiveChanceGuerrier = 0.05f;
        [Tooltip("Chance d'esquiver : voleur (agile, souvent).")]
        public float esquiveChanceVoleur = 0.35f;
        [Tooltip("Recharge de l'esquive d'un squelette après une esquive (s, tirée entre x et y).")]
        public Vector2 esquiveEnnemiRecharge = new Vector2(4f, 6f);
        [Tooltip("Longueur du bond d'esquive (m ; le héros : 4).")]
        public float esquiveEnnemiDistance = 3f;
        [Tooltip("Durée du bond d'esquive (s ; le héros : 0,35).")]
        public float esquiveEnnemiDuree = 0.4f;
        [Tooltip("Invulnérabilité au début du bond (s ; comme le héros : 0,3).")]
        public float esquiveEnnemiInvulnerable = 0.3f;
        [Tooltip("Part des esquives sur le côté (le reste : en arrière).")]
        public float esquiveEnnemiLaterale = 0.7f;
        [Tooltip("Un squelette plus près que ça du centre de Nyxessa la frappe (m, horizontal).")]
        public float rayonContactNyxessa = 3.2f;
        [Tooltip("Rayon des places autour de Nyxessa (m).")]
        public float rayonPlacesNyxessa = 2.4f;
        [Header("Trajets variés vers Nyxessa (wiki : ennemis.md, Comportement ; décidé 30/09/2026, chiffres à équilibrer)")]
        [Tooltip("01/10/2026 : nombre de couloirs par clairière (sbires, guerriers, voleurs). À sa sortie de terre, chaque squelette en tire un : " +
                 "point de passage à mi-trajet, décalé sur le côté de l'axe clairière → Nyxessa. 1 = tous sur l'axe (comportement d'avant).")]
        public int trajetCouloirs = 3;
        [Tooltip("01/10/2026 : écart latéral (m) entre les couloirs extrêmes, au point de passage (couloirs régulièrement répartis sur cette largeur).")]
        public float trajetLargeur = 22f;
        [Tooltip("01/10/2026 : flou (m) autour du point de passage de chaque squelette (le long de l'axe et sur le côté) : la file se défait.")]
        public float trajetFlou = 3f;
        [Tooltip("01/10/2026 : position du point de passage sur l'axe (part du trajet, tirée entre x et y).")]
        public Vector2 trajetPartPassage = new Vector2(0.4f, 0.6f);
        [Tooltip("01/10/2026 : allongement maximal du chemin par le point de passage (part du chemin direct sur le NavMesh). " +
                 "Au-delà (rivière, forêt), le couloir est resserré, puis abandonné.")]
        public float trajetDetourMax = 0.12f;
        [Tooltip("01/10/2026 : pas de point de passage si la sortie de terre est à moins de ce nombre de m de Nyxessa (invocations du Nécromancien).")]
        public float trajetDistanceMin = 25f;
        [Tooltip("01/10/2026 : écart de vitesse de marche par individu (± part du pas de base, tiré à la sortie de terre ; la course n'est pas touchée).")]
        public float trajetEcartVitesse = 0.08f;
        [Tooltip("01/10/2026 : angle (degrés, ±) des places d'arrivée autour de Nyxessa, de part et d'autre de la direction d'approche " +
                 "(décalé du côté du couloir) : ils n'arrivent pas tous au même point de la paroi du bouclier. Avant : 35.")]
        public float trajetAngleArrivee = 55f;
        [Tooltip("01/10/2026 (garde-fou) : un squelette en marche qui n'avance pas d'1 m vers sa destination pendant ce temps (s) abandonne son point de passage " +
                 "et relance son chemin ; à la deuxième fois de suite, loin de Nyxessa, il est replacé sur le NavMesh 2 m plus près d'elle.")]
        public float marcheBloqueeDelai = 4f;
        [Tooltip("Vitesse de lecture du clip de sortie de terre.")]
        public float vitesseSortieDeTerre = 1.5f;
        [Tooltip("Échelle des personnages Rig_Medium (Knight, squelettes) : 2 m environ.")]
        public float echellePersonnages = 0.8f;
        [Header("Riposte au contact de Nyxessa (wiki : ennemis.md, Comportement ; décidé 27/09/2026)")]
        [Tooltip("Un squelette qui frappe Nyxessa, frappé ce nombre de fois de suite par le même héros à moins de riposteDistance m, se retourne vers lui.")]
        public int riposteCoups = 2;
        public float riposteDistance = 3f;
        [Tooltip("Il le poursuit au plus ce temps (s), ou jusqu'à ce nombre de coups portés, puis revient à Nyxessa.")]
        public float riposteDuree = 3f;
        public int riposteAttaques = 2;

        [Header("Golem (mini-boss, nuit 10)")]
        public float golemPV = 1500f;
        public float golemVitesse = 2.0f;
        public float golemDegatsJoueur = 45f;
        public float golemDegatsNyxessa = 60f;
        public float golemRayonCoup = 3f;
        public float golemPreparation = 1.6f;
        public float golemIntervalle = 4f;
        public float golemEchelle = 0.8f;

        [Header("Morgrim (deux versions du mini-boss, nuit 10 ; wiki : ennemis.md, Morgrim ; décidé 26/09/2026, valeurs à équilibrer)")]
        [Tooltip("Rayon dans lequel un joueur compte comme « proche » pour le choix de compétence.")]
        public float morgrimJoueursProchesRayon = 6f;
        [Tooltip("Fracas (Massue, 26/09/2026) : anneau qui part de l'impact et s'étend lentement (m/s, jusqu'à ce rayon) ; " +
            "non parable, non esquivable (roulade) ; seul un saut au bon instant l'évite. Bande de détection au passage du " +
            "front (largeur, m). Un héros touché au sol : dégâts et statut Renversé. « morgrimMassueFracasRayon » ne sert " +
            "plus qu'au contact fixe de Nyxessa (elle ne saute pas).")]
        public float morgrimMassueFracasRayon = 3.5f;
        public float morgrimMassueFracasDegats = 45f;
        public float morgrimMassueFracasDegatsNyxessa = 60f;
        public float morgrimMassueFracasOndeVitesse = 6f;
        public float morgrimMassueFracasOndeRayonMax = 14f;
        public float morgrimMassueFracasOndeLargeurBande = 1.4f;
        public float morgrimMassueFracasPreparation = 1.35f;
        [Tooltip("Tourbillon (Massue) : dégâts continus à 360° tant qu'un joueur reste dans le rayon, thème Terre.")]
        public float morgrimMassueTourbillonRayon = 3f;
        public float morgrimMassueTourbillonDegatsParSeconde = 18f;
        public float morgrimMassueTourbillonDuree = 1.8f;
        public float morgrimMassueTourbillonPreparation = 0.85f;
        public float morgrimMassueTourbillonRecharge = 6.5f;
        [Tooltip("Tourbillon : léger recul (wiki : ennemis.md), impulsion vers l'extérieur (m/s, amortie) donnée toutes les morgrimMassueTourbillonReculIntervalle s à qui reste dans le rayon.")]
        public float morgrimMassueTourbillonRecul = 3f;
        public float morgrimMassueTourbillonReculIntervalle = 0.3f;
        [Tooltip("Charge écrasante (Massue) : fonce en ligne droite et renverse (statut Renversé, 26/09/2026) le premier joueur touché, thème Terre.")]
        public float morgrimMassueChargeDistance = 8f;
        public float morgrimMassueChargeVitesse = 9f;
        public float morgrimMassueChargeLargeur = 1.6f;
        public float morgrimMassueChargeDegats = 50f;
        public float morgrimMassueChargePreparation = 1.15f;
        public float morgrimMassueChargeRecharge = 7.5f;
        [Tooltip("Fauche (Martache) : coup en cône devant lui, thème Rage.")]
        public float morgrimMartacheFaucheRayon = 3.2f;
        public float morgrimMartacheFaucheAngle = 110f;
        public float morgrimMartacheFaucheDegats = 42f;
        public float morgrimMartacheFaucheDegatsNyxessa = 56f;
        public float morgrimMartacheFauchePreparation = 1.1f;
        [Tooltip("Fend-sol (Martache) : ligne qui ralentit (Ralenti) les joueurs restés dedans, thème Terre (c'est le sol qui casse).")]
        public float morgrimMartacheFendSolLongueur = 5f;
        public float morgrimMartacheFendSolLargeur = 1.4f;
        public float morgrimMartacheFendSolDegats = 28f;
        public float morgrimMartacheFendSolRalentiDuree = 3f;
        [Range(0f, 0.9f)] public float morgrimMartacheFendSolRalentiForce = 0.4f;
        public float morgrimMartacheFendSolPreparation = 1.25f;
        public float morgrimMartacheFendSolRecharge = 6f;
        [Tooltip("Fend-sol : la fissure reste au sol ce temps (s) et ralentit qui s'y tient (Ralenti renouvelé toutes les 0,5 s, force morgrimMartacheFendSolRalentiForce).")]
        public float morgrimMartacheFendSolFissureDuree = 4f;
        [Tooltip("Coup de brèche (Martache) : vise le bouclier de Nyxessa, dégâts renforcés contre lui, thème Rage.")]
        public float morgrimMartacheBrecheDegats = 70f;
        public float morgrimMartacheBrecheMultiplicateurBouclier = 2f;
        public float morgrimMartacheBrechePreparation = 1f;
        public float morgrimMartacheBrecheRecharge = 5f;

        [Header("Nécromancien (boss final, nuit 12)")]
        [Tooltip("PV du corps de Nyxar (× multiplicateur de la nuit). Depuis le 01/10/2026, il prend des dégâts dès le début (×nyxarEclatMultiplicateur par un éclat).")]
        public float necroPV = 1800f;
        public float necroVitesse = 2.6f;
        public float necroDegats = 18f;
        [Tooltip("Vitesse des crânes de Nyxar (m/s ; 12 avant le 01/10/2026). Le mage squelette a la sienne (mageVitesseMissile).")]
        public float necroVitesseMissile = 9f;
        public float necroPorteeTir = 22f;
        public float necroIntervalleTir = 3f;
        public Vector2 necroDistance = new Vector2(12f, 18f);
        public float necroIntervalleInvocation = 15f;
        public int necroSbiresParInvocation = 3;
        public int necroInvoquesMax = 12;
        public float necroEchelle = 0.92f;

        [Header("Nyxar, kit complet (wiki : ennemis.md, Nyxar ; 30/09/2026, valeurs à équilibrer)")]
        [Tooltip("Salve de crânes (grimoire intact) : nombre de crânes par salve et écart entre deux (s). Grimoire brisé : un seul crâne.")]
        public int nyxarSalveCranes = 3;
        public float nyxarSalveEcart = 0.6f;
        [Tooltip("Crânes de Nyxar esquivables (01/10/2026) : guidage (°/s, sans resserrement final ; 90 et ×4 sous 4 m avant), coupé pour de bon " +
            "à moins de nyxarCraneCoupureDistance m de la cible ou si elle sort de ±nyxarCraneCoupureAngle° devant le crâne ; ensuite il file tout droit " +
            "(un pas de côté ou une roulade au bon moment le fait rater). Ne touche pas aux crânes de Nyxessa ni du mage squelette.")]
        public float nyxarCraneGuidage = 45f;
        public float nyxarCraneCoupureDistance = 7f;
        public float nyxarCraneCoupureAngle = 55f;
        [Tooltip("Téléportation (couronne intacte) : un joueur à moins de nyxarTeleportDeclencheur m la déclenche ; il réapparaît à une distance tirée entre x et y m ; recharge (s).")]
        public float nyxarTeleportDeclencheur = 5f;
        public Vector2 nyxarTeleportDistance = new Vector2(11f, 16f);
        public float nyxarTeleportRecharge = 9f;
        [Tooltip("Il ne s'éloigne pas à plus de ce rayon (m) de Nyxessa en se téléportant.")]
        public float nyxarTeleportRayonNyxessa = 26f;
        [Tooltip("Faux au corps à corps (phases 1 et 2) : portée (m), dégâts, préparation et intervalle (s). Parable.")]
        public float nyxarFauxPortee = 2.6f;
        public float nyxarFauxDegats = 30f;
        public float nyxarFauxPreparation = 0.75f;
        public float nyxarFauxIntervalle = 2.4f;
        [Tooltip("Éclats de Nyx (couronne et grimoire) : PV de chacun (× multiplicateur de PV de la nuit). Un coup sur un éclat (01/10/2026) est un " +
            "critique garanti : ×nyxarEclatMultiplicateur sur Nyxar, et l'éclat s'use du coup de base. Un éclat cède aussi de lui-même à 2/3 puis 1/3 " +
            "des PV de Nyxar. Brisé : part du kit perdue (pas de PV en moins).")]
        public float nyxarEclatPV = 200f;
        public float nyxarEclatMultiplicateur = 2f;
        [Tooltip("Phase 3 (les deux éclats brisés) : enragé, au corps à corps à la faux. Vitesse (m/s), dégâts aux joueurs et à Nyxessa, préparation, intervalle (s), portée (m).")]
        public float nyxarEnrageVitesse = 4.2f;
        public float nyxarEnrageDegats = 38f;
        public float nyxarEnrageDegatsNyxessa = 40f;
        public float nyxarEnragePreparation = 0.6f;
        public float nyxarEnrageIntervalle = 1.7f;
        public float nyxarEnragePortee = 2.6f;

        [Header("Morgrim, kit commun aux deux versions (wiki : ennemis.md, Morgrim ; 30/09/2026, valeurs à équilibrer)")]
        [Tooltip("Chance (0-1) qu'une compétence commune prête (Balayage, Coup écrasé) passe avant la compétence de la version.")]
        [Range(0f, 1f)] public float morgrimCommunChance = 0.45f;
        [Tooltip("Balayage : arc de hache devant lui (rayon m, angle °), dégâts, recul (m/s), préparation et recharge (s). Parable.")]
        public float morgrimBalayageRayon = 3.8f;
        public float morgrimBalayageAngle = 200f;
        public float morgrimBalayageDegats = 36f;
        public float morgrimBalayageRecul = 5f;
        public float morgrimBalayagePreparation = 1.25f;
        public float morgrimBalayageRecharge = 5.5f;
        [Tooltip("Coup écrasé : frappe par-dessus au sol, onde de choc autour de lui (vitesse m/s, rayon m, bande m) ; à sauter comme le Fracas ; dégâts et Renversé au sol.")]
        public float morgrimEcraseDegats = 34f;
        public float morgrimEcraseDegatsNyxessa = 45f;
        public float morgrimEcraseOndeVitesse = 7f;
        public float morgrimEcraseOndeRayonMax = 8f;
        public float morgrimEcraseOndeLargeurBande = 1.3f;
        public float morgrimEcrasePreparation = 1.5f;
        public float morgrimEcraseRecharge = 9f;
        [Tooltip("Cri : quand au moins morgrimCriSquelettesMin squelettes sont à moins de morgrimCriRayon m, il crie (préparation s) et les galvanise (statut Galvanisé) : dégâts +bonus, vitesse +bonus, pendant la durée (s). Recharge (s).")]
        public float morgrimCriRayon = 12f;
        public int morgrimCriSquelettesMin = 3;
        public float morgrimCriPreparation = 1f;
        public float morgrimCriRecharge = 15f;
        public float morgrimCriDuree = 8f;
        public float morgrimCriBonusDegats = 0.3f;
        public float morgrimCriBonusVitesse = 0.25f;
        [Header("Morgrim offensif (retour de Quentin, 01/10/2026 ; valeurs à équilibrer)")]
        [Tooltip("Il chasse les joueurs : le plus proche vu à moins de morgrimDetection m (ou celui qui l'a frappé depuis moins de " +
            "morgrimAgressionDuree s, jusqu'à morgrimDetection × 1,5 m) ; Nyxessa seulement quand aucun joueur n'est à portée.")]
        public float morgrimDetection = 22f;
        public float morgrimAgressionDuree = 4f;
        [Tooltip("Cadence de base (s entre deux coups ; 4 pour le Golem d'origine), pas de marche et vitesse de course (m/s) quand sa proie " +
            "est à plus de morgrimCourseDistance m.")]
        public float morgrimIntervalle = 2.8f;
        public float morgrimVitesse = 2.4f;
        public float morgrimVitesseCourse = 3.4f;
        public float morgrimCourseDistance = 6f;
        [Tooltip("Enchaînement : après un coup simple (Fracas, Fauche), si une compétence est prête, elle part après ce délai (s) au lieu de la récupération ordinaire.")]
        public float morgrimEnchainementDelai = 0.45f;

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
        [Tooltip("Palier du bouclier à partir duquel le sorcier canalise l'énergie de Nyxessa (wiki : palier 4, décidé).")]
        public int bouclierCanalisationPalier = 4;
        [Tooltip("Canalisation (palier 4+) : dégâts encaissés par le bouclier pour 1 s de recharge du prochain missile de "
            + "Nyxessa {à équilibrer}.")]
        public float bouclierDegatsParSecondeRecharge = 20f;

        [Header("Sorcier (villageois ; wiki : village)")]
        [Tooltip("Nom de sa maison (objet sous Maisons) : il y passe le jour.")]
        public string sorcierMaison = "Maison_5_A";
        public float sorcierVitesse = 3.2f;
        public float sorcierPV = 60f;
        [Tooltip("Distance à Nyxessa de sa place d'incantation, du côté de sa maison (m) : sur le sommet du plateau, à l'intérieur du bouclier près de son bord, avec la place de tomber en arrière sans toucher le rocher.")]
        public float sorcierDistanceNyxessa = 3.6f;
        [Tooltip("Il fait face à l'extérieur, dos à Nyxessa ; il peut se tourner vers un ennemi devant lui de ce nombre de degrés au plus.")]
        public float sorcierPivotMax = 20f;
        [Tooltip("Intervalle minimal entre deux réactions de coup du sorcier quand le bouclier est frappé (s), pour qu'il ne tremble pas en continu sous une pluie de coups.")]
        public float sorcierReactionCoupIntervalle = 0.4f;

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
        [Tooltip("Distance horizontale au centre de Nyxessa pour ouvrir le menu d'achat (m) : le dessus du plateau seulement "
            + "(VillageBuilder.PlateauTopRadius ; Quentin, 26/09/2026).")]
        public float achatDistance = 5.2f;

        public const int PalierMax = 5;

        [Header("Donjon (wiki : deroule.md, Le donjon ; 26/09/2026, à équilibrer)")]
        [Tooltip("Or du grand coffre du 2e étage. 30/09/2026 : 120 → 160, il reprend la moitié de l'or des tas retirés.")]
        public int orGrandCoffre = 160;
        [Tooltip("Or d'un coffre (deux par donjon). 30/09/2026 : 50 → 70, ils reprennent l'autre moitié de l'or des tas retirés.")]
        public int orCoffre = 70;
        [Tooltip("Tas d'or au sol du donjon (quatre par plan, ramassés en passant dessus). Retirés le 30/09/2026 (Quentin : « les " +
            "tas de pièces dans le donjon c'est ciao ») : le plan les place toujours (même tirage, mêmes gardiens), mais ils ne sont " +
            "ni montrés ni ramassables et les gardiens ne les gardent plus ; leur or est passé aux coffres.")]
        public bool tasOrDonjon = false;
        [Tooltip("Or d'un tas d'or (quatre par donjon), si tasOrDonjon.")]
        public int orTasOr = 20;
        [Tooltip("Hausse de l'or du donjon par nuit déjà passée (0,1 : +10 % par nuit).")]
        public float orDonjonParNuit = 0.1f;

        [Header("Butin de clés et de crochets (03/10/2026, {à équilibrer}) : on en trouve dans les coffres du donjon")]
        [Tooltip("Coffre (deux par donjon) : chance (0 à 1) de contenir une clé. 03/10/2026, Quentin : « les clés et le kit devraient pouvoir se looter ».")]
        public float cleCoffreChance = 0.30f;
        [Tooltip("Coffre : chance de contenir un kit de crochetage.")]
        public float kitCoffreChance = 0.15f;
        [Tooltip("Grand coffre (2e étage) : chance de contenir une clé (1 : toujours).")]
        public float cleGrandCoffreChance = 1f;
        [Tooltip("Grand coffre : chance de contenir un kit de crochetage.")]
        public float kitGrandCoffreChance = 0.35f;
        [Tooltip("Clés trouvées quand la chance réussit (même sorte), pour un coffre puis pour le grand coffre ; un kit trouvé donne GameBalance.crochetsParKit crochets.")]
        public int cleCoffreQuantite = 1;
        public int cleGrandCoffreQuantite = 1;
        [Tooltip("Sorte de la clé trouvée dans un coffre : poids relatifs (bronze, argent, or).")]
        public Vector3 cleCoffrePoids = new Vector3(70f, 30f, 0f);
        [Tooltip("Sorte de la clé trouvée dans le grand coffre : poids relatifs (bronze, argent, or).")]
        public Vector3 cleGrandCoffrePoids = new Vector3(20f, 55f, 25f);
        [Tooltip("Part du butin porté gardée quand Nyxessa rappelle le joueur (ou s'il meurt au donjon), par palier de Nyxessa (1 à 5).")]
        public float[] partGardeeRappel = { 0f, 0.2f, 0.4f, 0.6f, 0.75f };
        [Tooltip("Squelettes qui gardent le butin (sbires et guerriers), posés chaque jour sur les points d'apparition du donjon.")]
        public int gardiensDonjon = 6;
        [Tooltip("Part de guerriers parmi les gardiens.")]
        public float partGuerriersDonjon = 0.35f;
        [Tooltip("Gardiens plus agressifs (Quentin, 30/09/2026) : un héros vu (ligne de vue, murs compris) à moins de cette distance (m) " +
            "est poursuivi ; pas d'abandon au bout de abandonApres s sans frapper.")]
        public float gardienDetection = 12f;
        [Tooltip("Laisse d'un gardien (m) : il poursuit tant que sa cible reste à moins de ça de son poste, puis y retourne.")]
        public float gardienLaisse = 20f;
        [Tooltip("Alerte : un gardien qui repère un héros ou est frappé lance aussi les gardiens à moins de cette distance (m).")]
        public float gardienAlerte = 8f;
        [Tooltip("Distance horizontale au centre d'un portail pour le passer avec la touche Interagir (m ; Quentin, 26/09/2026 : " +
                 "on n'entre plus en marchant dedans).")]
        public float distancePortail = 3f;
        [Tooltip("Distance pour ramasser un tas d'or en passant dessus (m).")]
        public float rayonTasOr = 1.4f;
        [Tooltip("Distance pour ouvrir un coffre (touche Interagir, m).")]
        public float distanceCoffre = 2.4f;

        [Header("Taverne (Maison_1_A ; décision du 26/09/2026) : de jour seulement, payée par la caisse commune")]
        public int tavernePrixRepas = 15;
        [Tooltip("Vie rendue par le repas.")]
        public float taverneSoinRepas = 40f;
        public int tavernePrixBiere = 5;
        public int tavernePrixTournee = 30;
        [Tooltip("Distance horizontale au comptoir pour parler au tavernier (m).")]
        public float taverneDistance = 2.4f;
        [Tooltip("Ivresse d'une bière (s).")]
        public float ivresseBiere = 8f;
        [Tooltip("Ivresse d'une tournée, pour tous les joueurs (s).")]
        public float ivresseTournee = 15f;
        [Tooltip("Roulis de la caméra à pleine ivresse (degrés) : un tangage doux.")]
        public float ivresseRoulis = 4f;
        [Tooltip("Ondulation de la direction de marche à pleine ivresse (degrés) : une démarche hésitante.")]
        public float ivresseDeviation = 14f;

        [Header("Boutiques du village : mécano (clés, crochets) et druide (potions) ; 03/10/2026, {à équilibrer}")]
        [Tooltip("Distance horizontale à l'ancre d'échange du mécano ou du druide pour leur parler (m ; comme la taverne).")]
        public float boutiqueDistance = 2.4f;
        [Tooltip("Mécano : prix d'une clé de bronze (or, pris dans la caisse commune).")]
        public int mecanoPrixCleBronze = 40;
        public int mecanoPrixCleArgent = 120;
        public int mecanoPrixCleOr = 320;
        [Tooltip("Mécano : prix du kit de crochetage (or).")]
        public int mecanoPrixKit = 60;
        [Tooltip("Clés d'une même sorte qu'un joueur peut porter (à usage unique).")]
        public int clesMaxParSorte = 2;
        [Tooltip("Crochets d'un kit. Un crochet casse à chaque échec de crochetage.")]
        public int crochetsParKit = 5;
        [Tooltip("Crochets qu'un joueur peut porter (deux kits).")]
        public int crochetsMax = 10;
        [Tooltip("Druide : prix d'une potion de santé (or).")]
        public int druidePrixSante = 20;
        [Tooltip("Druide : prix d'une potion de mana (or ; réservée au Mage, seul à avoir une jauge de mana).")]
        public int druidePrixMana = 20;
        [Tooltip("Druide : prix d'une potion d'endurance (or).")]
        public int druidePrixEndurance = 15;
        [Tooltip("Potions d'une même sorte qu'un joueur peut porter.")]
        public int potionsMaxParSorte = 3;
        [Tooltip("Potion de santé : part des points de vie maximum rendue (0,4 = 40 %).")]
        public float potionSantePart = 0.4f;
        [Tooltip("Potion de mana : part de la jauge de mana rendue (0,5 = 50 %).")]
        public float potionManaPart = 0.5f;
        [Tooltip("Potion d'endurance : rend toute l'endurance, puis la récupération est multipliée pendant cette durée (s).")]
        public float potionEnduranceDuree = 10f;
        [Tooltip("Potion d'endurance : facteur de la récupération pendant la durée ci-dessus (2 : doublée).")]
        public float potionEnduranceFacteur = 2f;
        [Tooltip("Temps minimal entre deux potions bues (s), toutes sortes confondues.")]
        public float potionRecharge = 1.5f;

        [Header("Crochetage des serrures (mini-jeu, 03/10/2026) : maintenir Interagir pour garder l'aiguille dans la zone, {à confirmer}")]
        [Tooltip("Essais par serrure : un crochet casse à chaque essai raté ; à 0 crochet, le mini-jeu s'arrête.")]
        public int crochetageEssais = 3;
        [Tooltip("Largeur de la zone (part de la piste) pour une serrure simple ou de bronze.")]
        public float crochetageZoneFacile = 0.30f;
        [Tooltip("Largeur de la zone pour une serrure d'argent.")]
        public float crochetageZoneDifficile = 0.20f;
        [Tooltip("Agilité du joueur : par point au-dessus de 3, part de la zone ajoutée (l'assassin, à 6, gagne 36 % de zone ; le paladin, à 2, en perd 12 %).")]
        public float crochetageZoneParAgilite = 0.12f;
        [Tooltip("Agilité du joueur : par point au-dessus de 3, chance qu'un crochet ne casse pas sur un échec.")]
        public float crochetageEconomieParAgilite = 0.10f;
        [Tooltip("Vitesse de déplacement de la zone (pistes par seconde), serrure simple ou de bronze, puis d'argent.")]
        public float crochetageDeriveFacile = 0.28f;
        public float crochetageDeriveDifficile = 0.45f;
        [Tooltip("Temps à passer dans la zone pour réussir un essai (s) ; la jauge redescend deux fois plus lentement hors de la zone.")]
        public float crochetageDureeReussite = 2.4f;
        [Tooltip("Temps accordé à un essai avant qu'il échoue (s).")]
        public float crochetageDureeEssai = 12f;
        [Tooltip("Aiguille : accélération vers le haut quand Interagir est maintenu, et gravité quand il est relâché (pistes par seconde au carré).")]
        public float crochetageMontee = 2.6f;
        public float crochetageChute = 2.2f;
        [Tooltip("Distance à la serrure au-delà de laquelle le crochetage s'arrête (m).")]
        public float crochetageDistanceMax = 3.4f;

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
        [Tooltip("30 → 27 : lissage du 27/09/2026 (wiki : classe-paladin.md).")]
        public float epeeDegats = 27f;
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
        [Tooltip("Endurance payée par point de dégâts bloqué (1 → 0,8 : lissage du 27/09/2026).")]
        public float gardeCoutParDegat = 0.8f;
        public float gardeBriseeEtourdi = 0.8f;
        public float paradeFenetre = 0.25f;
        public float paradeEtourdi = 1f;
        [Header("Parade parfaite (jauge de parade, 26/09/2026, à équilibrer)")]
        [Tooltip("Fenêtre de la parade parfaite (s avant l'impact prévu) : la garde levée dans ces derniers instants donne un coup de bouclier.")]
        public float paradeParfaiteFenetre = 0.1f;
        [Tooltip("Tolérance après l'impact prévu (s) : un appui juste après compte encore (écart d'affichage, gigue du réseau).")]
        public float paradeParfaiteGrace = 0.05f;
        [Tooltip("Durée montrée par la jauge de parade (s) : le curseur part de la gauche quand il reste cette durée avant l'impact.")]
        public float paradeJaugeDuree = 0.8f;
        [Tooltip("Coup de bouclier : bond en avant (m) et sa durée (s).")]
        public float paradeParfaiteBond = 0.7f;
        public float paradeParfaiteBondDuree = 0.14f;
        [Tooltip("Instant du coup de bouclier après l'appui (s) : repousse et étourdissement partent à cet instant.")]
        public float paradeParfaiteInstant = 0.12f;
        [Tooltip("Durée totale du coup de bouclier (s) : le paladin ne fait rien d'autre pendant ce temps.")]
        public float paradeParfaiteDuree = 0.45f;
        [Tooltip("Cône du coup de bouclier : portée (m) et demi-angle (°) devant le paladin.")]
        public float paradeParfaitePortee = 2.5f;
        public float paradeParfaiteDemiAngle = 60f;
        [Tooltip("Repousse des ennemis touchés par le coup de bouclier (m) et leur étourdissement (s).")]
        public float paradeParfaiteRepousse = 2f;
        public float paradeParfaiteEtourdi = 0.8f;
        [Tooltip("Réseau : l'hôte accepte une parade parfaite d'un client jusqu'à l'impact + le temps d'aller-retour + cette marge (s).")]
        public float paradeParfaiteToleranceReseau = 0.3f;
        [Tooltip("Tremblement de la caméra au coup de bouclier : amplitude (m) et durée (s) ; 0 : aucun.")]
        public float paradeParfaiteSecousse = 0.06f;
        public float paradeParfaiteSecousseDuree = 0.18f;
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
        [Tooltip("Penché du corps vers l'avant pendant la ruée (degrés, pivot aux pieds ; ni caméra ni capsule). Banc : 14.")]
        public float chargePenche = 14f;
        [Tooltip("Cadence des jambes (Running_A) pendant la ruée : vitesse réelle ÷ vitesse des pieds du clip, bornée ici.")]
        public float chargeCadenceMin = 0.8f;
        public float chargeCadenceMax = 8f;   // jambes très rapides, effet cartoon (Quentin, 26/09/2026 : plutôt que des pieds qui glissent)
        [Header("Soin d'aura (lissage du 27/09/2026 : le paladin et les alliés à moins de soinRayonAura m reçoivent le même montant)")]
        public float soinPart = 0.25f;
        public float soinIncantation = 0.6f;
        [Tooltip("30 → 20 s (27/09/2026).")]
        public float soinRecharge = 20f;
        [Tooltip("Rayon de l'aura de soin (m) : les alliés vivants à cette distance sont soignés du même montant que le paladin.")]
        public float soinRayonAura = 4f;
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
        [Tooltip("Mana rendu par seconde (01/10/2026, à équilibrer : 1 → 3, refonte du kit du mage).")]
        public float manaRegen = 3f;
        [Tooltip("Mana rendu par ennemi touché par la boule de feu. 0 depuis le 01/10/2026 (décision de Quentin : plus de regain sur l'attaque primaire, seule la régénération passive manaRegen reste) ; le champ est gardé comme réglage.")]
        public float manaParTouche = 0f;
        [Tooltip("Mana consommé par seconde de cône (01/10/2026, à équilibrer : 14 → 10).")]
        public float coneMana = 10f;
        [Tooltip("Dégâts par seconde du cône de flammes (01/10/2026, à équilibrer : 22 → 30), en 4 tics par seconde.")]
        public float coneDegats = 30f;
        public float conePortee = 6f;
        public float coneDemiAngle = 20f;
        public float coneVitesse = 0.4f;
        [Tooltip("Cône de flammes (décidé le 01/10/2026, à équilibrer) : part de vitesse retirée aux ennemis dedans (statut Ralenti, 0,4 = −40 %).")]
        public float coneRalenti = 0.4f;
        [Tooltip("Cône de flammes (01/10/2026) : durée du Ralenti posé à chaque tic (s), renouvelé tant que l'ennemi reste dans le cône.")]
        public float coneRalentiDuree = 0.5f;

        [Header("Mage : grande boule de feu (LB, décidé le 01/10/2026, chiffres à équilibrer)")]
        [Tooltip("Grande boule de feu (01/10/2026, à équilibrer) : dégâts à la cible touchée (centre de l'explosion).")]
        public float grandeBouleDegats = 60f;
        [Tooltip("Grande boule de feu (01/10/2026, à équilibrer) : dégâts aux autres ennemis dans le rayon de l'explosion.")]
        public float grandeBouleDegatsZone = 35f;
        [Tooltip("Grande boule de feu (01/10/2026, à équilibrer) : rayon de l'explosion (m).")]
        public float grandeBouleRayon = 5f;
        [Tooltip("Grande boule de feu (01/10/2026, à équilibrer) : coût en mana.")]
        public float grandeBouleMana = 35f;
        [Tooltip("Grande boule de feu (01/10/2026, à équilibrer) : recharge (s).")]
        public float grandeBouleRecharge = 10f;
        [Tooltip("Grande boule de feu (01/10/2026) : temps de lancer (s) — la boule quitte le bâton à cet instant (Ranged_Magic_Raise puis Ranged_Magic_Shoot).")]
        public float grandeBouleInstant = 0.8f;
        [Tooltip("Grande boule de feu (01/10/2026) : durée totale du geste (s) ; le mage avance au ralenti pendant ce temps.")]
        public float grandeBouleDuree = 1.1f;
        [Tooltip("Grande boule de feu (01/10/2026) : vitesse de vol (m/s), plus lente que la boule (18).")]
        public float grandeBouleVitesse = 12f;
        [Tooltip("Grande boule de feu (01/10/2026) : taille du visuel en vol (× la boule de feu).")]
        public float grandeBouleTaille = 1.9f;
        [Tooltip("Grande boule de feu (02/10/2026, à confirmer) : portée de la visée au sol (m) ; le cercle de visée se pose au plus loin à cette distance du mage.")]
        public float grandeBoulePortee = 20f;
        [Tooltip("Grande boule de feu (02/10/2026, à confirmer) : rayon du « cœur » de l'explosion (m) ; l'ennemi le plus proche du point visé, s'il est dans ce rayon, prend les gros dégâts (grandeBouleDegats), les autres ceux de la zone.")]
        public float grandeBouleCoeur = 1.5f;
        [Tooltip("Coup critique du Mage (décidé le 01/10/2026, à équilibrer) : chance (0,05 = 5 %) qu'une boule de feu ou une grande boule de feu " +
                 "fasse un critique, tirée une fois par boule à l'explosion (coup direct et zone) ; la brûlure ne critique pas.")]
        public float mageCritiqueChance = 0.05f;
        [Tooltip("Coup critique du Mage (décidé le 01/10/2026, à équilibrer) : multiplicateur des dégâts de la boule ou de la grande boule critique.")]
        public float mageCritiqueMultiplicateur = 2f;

        [Header("Mage : mur de flammes (RB, décidé le 01/10/2026, chiffres à équilibrer)")]
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : longueur de la ligne de feu (m), perpendiculaire à la visée.")]
        public float murLongueur = 8f;
        [Tooltip("Mur de flammes (01/10/2026) : distance du milieu du mur devant le mage (m).")]
        public float murDistance = 4f;
        [Tooltip("Mur de flammes (02/10/2026, à confirmer) : portée de la visée au sol (m) ; le mur se pose au plus loin à cette distance du mage, en travers de la ligne qui va de lui au point visé.")]
        public float murPortee = 14f;
        [Tooltip("Mur de flammes (01/10/2026) : épaisseur de la zone qui brûle (m), de part et d'autre de la ligne.")]
        public float murEpaisseur = 1.4f;
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : durée (s).")]
        public float murDuree = 5f;
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : coût en mana.")]
        public float murMana = 30f;
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : recharge (s).")]
        public float murRecharge = 14f;
        [Tooltip("Mur de flammes (01/10/2026) : instant où le mur prend dans le geste (s, Ranged_Magic_Summon accéléré).")]
        public float murInstant = 0.5f;
        [Tooltip("Mur de flammes (01/10/2026) : durée totale du geste (s).")]
        public float murGeste = 0.85f;
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : part de vitesse retirée aux ennemis qui le traversent ou s'y tiennent (Ralenti).")]
        public float murRalenti = 0.4f;
        [Tooltip("Mur de flammes (01/10/2026) : durée du Ralenti posé à chaque tic (s), renouvelé tant que l'ennemi est dans le mur.")]
        public float murRalentiDuree = 0.6f;
        [Tooltip("Mur de flammes (01/10/2026, à équilibrer) : un ennemi qui entre dans le mur monte d'un palier de brûlure ; s'il y reste, encore un palier toutes les x s.")]
        public float murIntervallePalier = 1.5f;
        [Tooltip("Brûlure : dégâts par seconde si brulureDegatsPaliers est vide (sinon le palier 1 de la liste fait foi, 01/10/2026).")]
        public float brulureDegats = 5f;
        [Tooltip("Brûlure : durée minimale (s) après le dernier coup de feu ; plus longue aux paliers hauts, le temps de redescendre (01/10/2026).")]
        public float brulureDuree = 3f;
        [Tooltip("Brûlure en paliers (décidé le 30/09/2026, chiffres du 01/10/2026 à équilibrer) : dégâts par seconde de chaque palier, du 1 au plafond (le nombre d'entrées est le plafond ; 3 paliers au plus, décidé par Quentin le 01/10/2026).")]
        public float[] brulureDegatsPaliers = { 5f, 8f, 12f };
        [Tooltip("Brûlure en paliers (01/10/2026, à équilibrer) : part de jauge remplie par chaque tic du cône de flammes (4 tics par seconde ; 1 = un palier).")]
        public float brulureRemplissageCone = 0.15f;
        [Tooltip("Brûlure en paliers (01/10/2026, à équilibrer) : part de jauge remplie par la boule de feu (coup direct ou explosion), par ennemi touché.")]
        public float brulureRemplissageBoule = 0.4f;
        [Tooltip("Brûlure en paliers (01/10/2026, à équilibrer) : délai sans feu reçu (s) avant que la jauge ne baisse.")]
        public float brulureDelaiDescente = 1f;
        [Tooltip("Brûlure en paliers (01/10/2026, à équilibrer) : vitesse de redescente (jauge par seconde ; 0,75 : un palier en 1,3 s).")]
        public float brulureVitesseDescente = 0.75f;

        [Header("Projectiles (wiki classes.md : vitesse et pesanteur)")]
        [Tooltip("Pesanteur des flèches et carreaux (m/s², réelle : 9,81).")]
        public float projectileGravite = 9.81f;
        [Tooltip("Aide à la visée : relèvement maximal (degrés) ajouté pour compenser la chute ; au-delà, le joueur vise au-dessus.")]
        public float aideChuteMax = 3f;
        [Tooltip("Longueur de vol maximale d'une flèche ou d'un carreau (m) avant de disparaître.")]
        public float projectileVolMax = 200f;

        [Header("Rôdeur (wiki : charge 1,2 s, 10 à 50 dégâts, tête ×2 ; lissage du 27/09/2026)")]
        [Tooltip("110 → 120 (27/09/2026).")]
        public float rodeurPV = 120f;
        public float rodeurVitesse = 5f;
        public float arcCharge = 1.2f;
        public float arcDegatsMin = 10f;
        [Tooltip("40 → 50 (27/09/2026) → 48 (01/10/2026, nerf variante F du simulateur de vagues).")]
        public float arcDegatsMax = 48f;
        [Tooltip("Tir à la tête de l'arc : ×2 → ×1,8 (01/10/2026, variante F : un sbire ne meurt plus d'une flèche à la tête).")]
        public float arcTete = 1.8f;
        [Tooltip("Une flèche à pleine charge (100 %) étourdit l'ennemi touché ce temps (s ; moitié sur Morgrim). 0 : aucun.")]
        public float arcEtourdiPleineCharge = 1f;
        [Tooltip("Vitesse de départ de la flèche selon la charge (m/s) : tir rapide → minimum, charge complète → maximum (wiki : projectiles).")]
        public float arcVitesseMin = 18f;
        public float arcVitesseMax = 55f;
        [Tooltip("Distance du point visé par le réticule (m).")]
        public float arcPortee = 60f;
        [Tooltip("Intervalle du tir rapide (s) : 0,3 → 0,15 (27/09/2026).")]
        public float arcIntervalle = 0.15f;
        public float arcVitesseBander = 0.5f;
        public float viseeVitesse = 0.6f;
        [Header("Visée d'une zone au sol (mage : grande boule et mur ; rôdeur : nuée ; 02/10/2026, à confirmer)")]
        [Tooltip("Facteur de vitesse de marche pendant la visée d'une zone (comme l'arc bandé : 0,5).")]
        public float viseeZoneVitesse = 0.5f;
        [Tooltip("Distance minimale entre le héros et le centre d'une zone visée (m) : pas de zone sous ses propres pieds.")]
        public float viseeZoneDistanceMin = 1.5f;
        public float nueeRayon = 3f;
        public int nueeSalves = 5;
        [Tooltip("10 → 14 par salve (27/09/2026).")]
        public float nueeDegatsSalve = 14f;
        public float nueePortee = 25f;
        public float nueeRecharge = 12f;
        [Tooltip("La zone de la nuée ralentit qui y reste (statut Ralenti, renouvelé une salve sur deux, soit toutes les 0,48 s, comme la fissure du Fend-sol) : part de vitesse retirée et durée de chaque relance (s).")]
        [Range(0f, 0.9f)] public float nueeRalentiForce = 0.4f;
        public float nueeRalentiDuree = 1f;
        public float rouladeDistance = 4f;
        public float rouladeCout = 20f;
        public int salveFleches = 5;
        public float salveEcart = 20f;
        [Tooltip("15 → 18 (27/09/2026).")]
        public float salveDegats = 18f;
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
        [Header("Pas de l'ombre et Exécution (assassin, RB ; wiki : classe-assassin.md, décidé 27/09/2026)")]
        [Tooltip("Bond dans la direction visée (m) et sa durée (s) ; invulnérable pendant, traverse les ennemis, pas les murs.")]
        public float pasOmbreDistance = 7f;
        public float pasOmbreDuree = 0.15f;
        [Tooltip("Réticule sur un ennemi : le bond s'arrête à cette distance derrière lui (m), face à son dos.")]
        public float pasOmbreArret = 1f;
        [Tooltip("Portée du réticule pour choisir la cible du bond (m) : un ennemi visé plus loin que le bond n'est pas ciblé.")]
        public float pasOmbrePorteeCible = 9f;
        public float pasOmbreRecharge = 6f;
        [Tooltip("Exécution (passif de la dague) : un ennemi commun sous cette part de vie est achevé net ; un élite ou un boss prend le coup ×executionElite. Chaque exécution remet la recharge du bond à zéro.")]
        [Range(0f, 1f)] public float executionSeuil = 0.3f;
        public float executionElite = 3f;

        [Header("Viking (valeurs de départ, à équilibrer)")]
        public float vikingPV = 140f;
        public float vikingVitesse = 5f;
        public float hacheDegats = 38f;
        public float hacheIntervalle = 1.1f;
        public float hachePortee = 2.4f;
        public float hacheDemiAngle = 70f;
        public float hacheInstant = 0.55f;
        [Header("Rage et Furie (viking ; wiki : classe-viking.md ; décidé par Quentin le 03/10/2026 : la rage monte au combat, les compétences sont gratuites avec recharge, ultime à la rage pleine ; valeurs à équilibrer)")]
        public float rageMax = 100f;
        [Tooltip("Plancher de rage {à confirmer}. Les compétences ne coûtent plus de rage (03/10/2026) : le plancher de 30 (27/09/2026) n'a plus lieu d'être, 0 = la rage repart de zéro hors combat.")]
        public float rageMin = 0f;
        [Tooltip("Rage gagnée par ennemi touché avec la hache.")]
        public float rageParTouche = 8f;
        [Tooltip("Rage gagnée en encaissant des dégâts (03/10/2026) : par point de dégât subi (après Peau de fer) ; chute et brûlure ne comptent pas. {à équilibrer}")]
        public float rageParDegatRecu = 0.5f;
        [Tooltip("Plafond de la rage gagnée par un seul coup reçu. {à équilibrer}")]
        public float rageRecuMax = 12f;
        [Tooltip("Baisse de la rage hors combat (par seconde), après rageDelaiBaisse sans toucher ni être touché.")]
        public float rageBaisse = 6f;
        public float rageDelaiBaisse = 4f;
        [Tooltip("Rage rendue par un tic de la tournante, par ennemi touché (2 → 1, 03/10/2026 : la tournante est gratuite, elle ne doit pas remplir la jauge seule) {à équilibrer}.")]
        public float tournanteRageParTic = 1f;
        [Tooltip("Attaque tournante (03/10/2026) : maintien maximal (s) puis recharge (s) comptée depuis la fin du tourbillon, relâché ou non. {à équilibrer}")]
        public float tournanteDureeMax = 3f;
        public float tournanteRecharge = 10f;
        public float tournanteIntervalle = 0.3f;
        public float tournanteRayon = 2.3f;
        [Tooltip("12 → 10 (27/09/2026).")]
        public float tournanteDegats = 10f;
        public float tournanteVitesse = 0.6f;
        public float rugissementRecharge = 12f;
        public float rugissementRayon = 10f;
        public float rugissementProvocation = 5f;
        [Tooltip("Peau de fer (27/09/2026) : le rugissement pose ce bienfait sur le viking au moment du cri : part des dégâts subis retirée, durée (s).")]
        [Range(0f, 0.9f)] public float peauDeFerReduction = 0.35f;
        public float peauDeFerDuree = 6f;
        public float sautRecharge = 8f;
        public float sautDistance = 5f;
        public float sautRayon = 3.5f;
        public float sautDegats = 45f;
        public float sautEtourdi = 1f;
        [Tooltip("Recul de la hache (03/10/2026) : déplacement (m) infligé à chaque ennemi touché par un coup de hache (hors boss), multiplié par la Force gagnée et par furieRecul. {à équilibrer}")]
        public float hacheRecul = 0.4f;

        [Header("Furie, ultime du viking (03/10/2026 ; entrée automatique à la rage pleine {à confirmer} ; valeurs à équilibrer)")]
        [Tooltip("Durée de la Furie (s) : la jauge se vide à vitesse fixe, de pleine à vide, et ne monte plus.")]
        public float furieDuree = 10f;
        [Tooltip("Échelle du modèle du viking en Furie (lissée, modèle visuel seul).")]
        public float furieEchelle = 1.15f;
        [Tooltip("Vitesse de déplacement en Furie (×).")]
        public float furieVitesse = 1.2f;
        [Tooltip("Cadence de la hache en Furie (×) : coup plus tôt et animation plus rapide.")]
        public float furieCadence = 1.15f;
        [Tooltip("Recul de la hache en Furie (×).")]
        public float furieRecul = 1.5f;
        [Tooltip("Dégâts du viking en Furie (×), tous ses coups.")]
        public float furieDegats = 1.1f;
        [Tooltip("Vitesse de recharge des compétences en Furie (×) : le temps de recharge s'écoule plus vite.")]
        public float furieRecharge = 1.5f;
        [Tooltip("Rage à partir de laquelle le HUD avertit que la Furie approche (part de la jauge).")]
        [Range(0.5f, 0.99f)] public float furieSignal = 0.85f;

        [Header("Chute (wiki : statuts.md ; valeurs à équilibrer)")]
        [Tooltip("Hauteur de chute sans dégâts (m) : un saut sur place monte à 1,2 m ; un niveau du donjon fait 4 m.")]
        public float chuteSeuil = 3.5f;
        [Tooltip("Dégâts par mètre de chute au-delà du seuil.")]
        public float chuteDegatsParMetre = 12f;
        [Tooltip("Dégâts maximaux d'une chute.")]
        public float chuteDegatsMax = 80f;
        [Tooltip("Durée du statut Ralenti après une chute au-delà du seuil (s).")]
        public float chuteRalentiDuree = 3f;
        [Tooltip("Part de vitesse retirée par ce Ralenti (0,4 = −40 %).")]
        [Range(0f, 0.9f)] public float chuteRalentiForce = 0.4f;
        [Tooltip("Deuxième seuil (m, 26/09/2026) : au-delà, le héros est Renversé (Heros.Renverser) avant le Ralenti de chute.")]
        public float chuteRenverseSeuil = 7f;

        [Header("Renversé (knockdown, 26/09/2026 ; wiki : statuts.md ; valeurs à équilibrer)")]
        [Tooltip("Chute à la renverse (Death_A, rig Medium General) : durée du clip (s).")]
        public float renverseChuteDuree = 0.8f;
        [Tooltip("Temps tenu au sol, pose finale de la chute (s), avant de se relever.")]
        public float renverseAuSolDuree = 0.4f;
        [Tooltip("Relevé (Lie_StandUp, rig Medium General) : durée du clip à vitesse 1 (s).")]
        public float renverseReleveDuree = 2.33f;
        [Tooltip("Accélération du relevé (facteur de vitesse de l'Animator) : 1,5 ramène Lie_StandUp à environ 1,55 s.")]
        public float renverseReleveVitesse = 1.5f;
        [Tooltip("Marteler Saut (accessibilité : maintenir) pendant le Renversé : secondes de temps au sol/relevé retirées par appui.")]
        public float renverseMartelementReduction = 0.08f;
        [Tooltip("Intervalle minimal entre deux réductions en mode « maintenir » (s) : même rythme maximal que marteler.")]
        public float renverseMartelementIntervalleMaintenir = 0.15f;
        [Tooltip("Plafond de réduction, en part de la durée totale du Renversé (0,5 = jamais plus de moitié moins).")]
        [Range(0f, 0.9f)] public float renverseMartelementPlafond = 0.5f;

        [Header("Attributs (wiki : classes.md, Attributs ; décidé 01/10/2026, chiffres à équilibrer)")]
        [Tooltip("01/10/2026 : répartition de départ des 18 points d'attribut du Paladin (Force, Endurance, Agilité, Perception, Esprit, Chance). " +
                 "Règle d'équilibre (à confirmer) : les stats actuelles correspondent à cette répartition ; seuls les points gagnés ajoutent les bonus ci-dessous.")]
        public int[] attributsPaladin = { 4, 6, 2, 1, 3, 2 };
        [Tooltip("01/10/2026 : répartition de départ du Viking (Force, Endurance, Agilité, Perception, Esprit, Chance).")]
        public int[] attributsViking = { 6, 5, 3, 1, 2, 1 };
        [Tooltip("01/10/2026 : répartition de départ du Mage (Force, Endurance, Agilité, Perception, Esprit, Chance).")]
        public int[] attributsMage = { 1, 2, 3, 3, 6, 3 };
        [Tooltip("01/10/2026 : répartition de départ du Rôdeur (Force, Endurance, Agilité, Perception, Esprit, Chance).")]
        public int[] attributsRodeur = { 1, 3, 5, 6, 2, 1 };
        [Tooltip("01/10/2026 : répartition de départ de l'Assassin (Force, Endurance, Agilité, Perception, Esprit, Chance).")]
        public int[] attributsAssassin = { 3, 2, 6, 2, 1, 4 };
        [Tooltip("01/10/2026, Force, par point gagné : part de dégâts au corps à corps ajoutée (0,03 = +3 %).")]
        public float attributForceDegats = 0.03f;
        [Tooltip("01/10/2026, Force, par point gagné : part de recul infligé ajoutée (charge bélier, coup de bouclier ; 0,05 = +5 %).")]
        public float attributForceRecul = 0.05f;
        [Tooltip("01/10/2026, Endurance, par point gagné : points de vie maximum ajoutés.")]
        public float attributEndurancePv = 8f;
        [Tooltip("01/10/2026, Endurance, par point gagné : endurance maximum ajoutée (garde, esquive, course).")]
        public float attributEnduranceEndurance = 5f;
        [Tooltip("01/10/2026, Agilité, par point gagné : part de vitesse de déplacement et d'attaque (cadence de l'attaque RT) ajoutée (0,02 = +2 %).")]
        public float attributAgiliteVitesse = 0.02f;
        [Tooltip("01/10/2026, Agilité, par point gagné : part de recharge de l'esquive retirée (0,03 = −3 %).")]
        public float attributAgiliteEsquive = 0.03f;
        [Tooltip("01/10/2026, Perception, par point gagné : part de dégâts à distance ajoutée (flèches, carreaux, sorts du Mage ; 0,03 = +3 %).")]
        public float attributPerceptionDegats = 0.03f;
        [Tooltip("01/10/2026, Perception, par point gagné : chance de critique à distance ajoutée (0,01 = +1 %).")]
        public float attributPerceptionCritique = 0.01f;
        [Tooltip("01/10/2026, Esprit, par point gagné : part de la jauge de classe ajoutée à son maximum (mana, rage ; 0,05 = +5 %).")]
        public float attributEspritJauge = 0.05f;
        [Tooltip("01/10/2026, Esprit, par point gagné : part de recharge des compétences retirée (LB, RB, arbalète ; 0,02 = −2 %).")]
        public float attributEspritRecharge = 0.02f;
        [Tooltip("01/10/2026, Chance, par point gagné : chance de critique ajoutée à tous les coups (0,01 = +1 %).")]
        public float attributChanceCritique = 0.01f;
        [Tooltip("01/10/2026, Chance, par point gagné : part d'or ramassé ajoutée (butins du donjon ; 0,03 = +3 %).")]
        public float attributChanceOr = 0.03f;
        [Tooltip("01/10/2026 : multiplicateur des dégâts d'un critique tiré grâce aux attributs (coup qui n'était pas déjà critique ; " +
                 "le Mage garde mageCritiqueMultiplicateur).")]
        public float attributCritiqueMultiplicateur = 2f;

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
        [Tooltip("Recul derrière l'épaule (m). 5,5 : caméra plus haute et reculée pour la lisibilité des coups ennemis " +
            "(décision de Quentin, 26/09/2026, planche lisibilite_cam_planche.png). Le recul contre les murs (CameraEpaule.Recul) " +
            "borne déjà la distance réelle dans les petites pièces (SphereCast jusqu'au premier mur) : pas de valeur séparée pour les intérieurs.")]
        public float cameraDistance = 5.5f;
        public float cameraEpaule = 0.6f;
        public float cameraHauteur = 1.6f;
        [Tooltip("Donjon (30/09/2026) : rayon de la découpe circulaire autour du héros, en fraction de la hauteur de l'écran " +
            "(shader Deathless/DonjonDecoupe) ; 0 = pas de découpe (la caméra se rapproche contre les murs comme ailleurs).")]
        public float cameraDecoupeRayon = 0.24f;
        [Tooltip("Tangage appliqué au début de la partie (CameraEpaule.Suivre), plus robuste que la valeur de scène. " +
            "22° : caméra plus haute (décision de Quentin, 26/09/2026, au lieu de 12° par défaut).")]
        public float cameraTangageDefaut = 22f;
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
        [Tooltip("Journal détaillé (vagues, tirs de Nyxessa). Éditeur seulement : retiré des builds (Partie.Journal est Conditional).")]
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
