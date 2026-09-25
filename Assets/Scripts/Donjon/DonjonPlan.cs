using System;

namespace Deathless.Donjon
{
    /// Profil d'un bloc de 3 x 3 cellules (12 x 12 m).
    public enum TypeBloc : byte
    {
        Entree = 0,     // salle d'arrivée à double hauteur, sans balcon : arrivée et portail de retour
        Hall = 1,       // salle à double hauteur ; sur le pourtour du donjon, le balcon court le long des murs
        Mezzanine = 2,  // plancher complet au niveau 1 (salle couverte en dessous), relié au balcon
        Bassin = 3,     // demi-niveau en contrebas (2 m) rempli d'eau jusqu'aux genoux (0,5 m)
        Tour = 4        // mezzanine surmontée du 2e étage (grande salle ouverte, balustrade, grand coffre)
    }

    public enum TypeButin : byte { GrandCoffre = 0, Coffre = 1, TasOr = 2 }
    public enum TypeApparition : byte { Sbire = 0, Guerrier = 1, Voleur = 2, Mage = 3 }

    /// Bord entre deux cellules d'un même niveau.
    public enum Bord : byte
    {
        Rien = 0,
        Mur = 1,            // mur plein intérieur (4 m)
        MurExterieur = 2,   // mur d'enceinte (4 m par niveau)
        GardeCorps = 3,     // balustrade au bord d'un plancher
        MurBas = 4,         // mur à demi-hauteur (2 m) entre deux salles : on voit par-dessus
        Bassin = 5,         // muret de fondation qui borde le bassin (2 m, du fond du bassin au rez)
        Arcade = 6          // passage ouvert sous le bord d'un plancher : poteaux et linteau de bois
    }

    /// Élément posé par le plan : niveau, cellule, position locale (m) dans le repère du donjon, rotation Y (degrés),
    /// type (TypeButin, TypeApparition ou catégorie de décor) et variante (tirage libre pour le visuel).
    public struct Pose
    {
        public int niveau, cellule;
        public float x, y, z, rotY;
        public byte type, variante;
        public int Noeud { get { return niveau * DonjonPlan.NbCellules + cellule; } }
        public int Bloc { get { return DonjonPlan.BlocDe(cellule); } }
    }

    /// Escalier. Montée (bassin = false) : KayKit « stairs_long » (8 m, 4 m de haut, 2 m praticables) du niveau
    /// `niveau` au suivant, contre un mur : depart (bas, niveau), bas et haut (cellules occupées, niveau), arrivee
    /// (palier, niveau + 1). Bassin (bassin = true) : KayKit « stairs » réduit à 2 m de haut sur 4 m, tout au rez :
    /// depart = cellule du bassin au pied, bas = haut = cellule de l'escalier, arrivee = cellule du rez en haut.
    /// dir : direction de montée (0 N, 1 E, 2 S, 3 O).
    public struct Escalier
    {
        public int niveau, dir, depart, bas, haut, arrivee;
        public bool bassin;
    }

    /// Plan d'un donjon ouvert, sur une grille de cellules de 4 m (dalles et murs KayKit Dungeon), groupées en blocs
    /// de 3 x 3 cellules, inspiré des planches du pack : grandes salles à double hauteur, balcon à balustrade qui
    /// court le long des murs d'enceinte, escaliers droits contre les murs, mezzanines, bassin d'eau en contrebas.
    /// Entièrement déterministe : même graine (et même essai), même plan sur toutes les machines (générateur entier
    /// maison, distances entières, ni UnityEngine.Random ni System.Random). Aucune allocation après la construction.
    ///
    /// Taille unique : 5 x 4 blocs (60 x 48 m), rez + balcons (4 m) + 2e étage (8 m) + bassin (-2 m) ; toujours
    /// 1 entrée, 2 mezzanines, 1 tour (mezzanine + 2e étage de 15 cellules), 1 bassin, 15 halls ; 3 escaliers (rez →
    /// balcon et balcon → 2e étage, contre les murs ; descente au bassin) ; 7 butins ; 24 apparitions.
    /// Consigne des 90 s : le chemin critique (arrivée → butin le plus lointain → portail de retour, plus court chemin
    /// praticable, escaliers et eau comptés) est compris entre CheminMinDm et CheminMaxDm ; sinon le plan est refait
    /// (essai suivant, toujours déterministe).
    public sealed class DonjonPlan
    {
        // ------------------------------------------------------------------ Taille unique
        public const float Cellule = 4f, HauteurNiveau = 4f, ProfondeurBassin = 2f, ProfondeurEau = 0.5f;
        public const int NbNiveaux = 3;
        public const int BlocTaille = 3, BlocsX = 5, BlocsY = 4, NbBlocs = BlocsX * BlocsY;
        public const int Largeur = BlocsX * BlocTaille, Profondeur = BlocsY * BlocTaille, NbCellules = Largeur * Profondeur;
        public const int NbNoeuds = NbNiveaux * NbCellules;
        public const int NbMezzanines = 3, NbMontees = 2, NbEscaliers = NbMontees + 1;   // une montée par étage
        public const int MaxMursBas = 3;
        public const int NbButins = 7;             // 1 grand coffre, 2 coffres, 4 tas d'or
        public const int NbApparitions = 24;
        public const int MaxTorches = 48, MaxDecors = 160;
        /// L'eau ralentit : vitesse x 0,6 (ZoneEau), coût NavMesh 1/0,6 (zone « Eau »), coût du plan x 5/3.
        public const float FacteurVitesseEau = 0.6f;

        // ------------------------------------------------------------------ Consigne des 90 s (décimètres)
        /// Plafond du chemin critique : 250 m. À 5 m/s (marche) : 50 s de trajet, il reste 40 s pour combattre,
        /// ouvrir les coffres et passer les portails. En course gérée par l'endurance (~6,2 m/s) : 40 s ; 8 m/s : 31 s.
        public const int CheminMaxDm = 2500;
        /// Plancher : même longueur de parcours pour tous les donjons (taille unique).
        public const int CheminMinDm = 1800;
        public const int MaxEssais = 400;
        /// Coûts en décimètres : pas droit 4 m, diagonale 4 m x racine de 2 ; escalier de 8 m = 2 m + 8,94 m x 1,2
        /// (montée plus lente) + 2 m ; escalier du bassin = 2 m dans l'eau (x 5/3) + 4,47 m x 1,2 + 2 m.
        public const int PasOrtho = 40, PasDiag = 57, PasEscalier = 147, PasEscalierBassin = 107;

        // Directions : 0 = nord (+y grille, +z monde), 1 = est (+x), 2 = sud, 3 = ouest.
        static readonly int[] Dx = { 0, 1, 0, -1 };
        static readonly int[] Dy = { 1, 0, -1, 0 };

        // ------------------------------------------------------------------ Résultat
        public readonly TypeBloc[] typeBloc = new TypeBloc[NbBlocs];
        public readonly bool[] plein = new bool[NbNoeuds];            // dalle au niveau (sous un escalier aussi)
        public readonly byte[] materiau = new byte[NbNoeuds];         // 0 pierre, 1 bois
        public readonly sbyte[] escalierDe = new sbyte[NbNoeuds];     // escalier posé sur la cellule, -1 sinon
        public readonly byte[] ouvert = new byte[NbNoeuds];           // bits 1 << direction : passage ouvert
        public readonly bool[] eau = new bool[NbCellules];            // cellule du bassin (rez, 2 m plus bas)
        public readonly Bord[] bordH = new Bord[NbNiveaux * Largeur * (Profondeur + 1)];   // bords le long de x
        public readonly Bord[] bordV = new Bord[NbNiveaux * (Largeur + 1) * Profondeur];   // bords le long de z
        public readonly Escalier[] escaliers = new Escalier[NbEscaliers];
        public int nbEscaliers;
        public int blocBassin = -1, blocTour = -1;
        public readonly int[] distArrivee = new int[NbNoeuds];
        public readonly int[] distRetour = new int[NbNoeuds];
        public readonly Pose[] butins = new Pose[NbButins];
        public readonly Pose[] apparitions = new Pose[NbApparitions];
        public readonly Pose[] torches = new Pose[MaxTorches];       // type : TorcheMurale ou Torchere
        public readonly Pose[] decors = new Pose[MaxDecors];         // type : DecorCoin, DecorBanniere...
        public int nbTorches, nbDecors;
        public Pose arrivee, portailRetour;
        public int graine, essais, cheminCritiqueDm, butinCritique;
        /// Essais rejetés à la dernière génération : plan impossible (escaliers, connexité) ou hors consigne.
        public int rejetsStructure, rejetsTropCourt, rejetsTropLong;
        public uint empreinte;

        public const byte DecorCoin = 0, DecorBanniere = 1, DecorOs = 2, DecorDalleArrivee = 3, DecorTable = 4, DecorFlottant = 5;
        public const byte TorcheMurale = 0, Torchere = 1;

        // ------------------------------------------------------------------ Travail
        uint m_Etat;
        readonly int[] m_Pile = new int[NbNoeuds];
        readonly int[] m_Comp = new int[NbNoeuds];
        readonly bool[] m_Utilise = new bool[NbNoeuds];
        readonly byte[] m_Occupe = new byte[NbNoeuds];               // 16 = centre pris, 1/2/4/8 = quarts NE/SE/SO/NO
        readonly int[] m_Cand = new int[1024];
        readonly int[] m_Ordre = new int[NbNoeuds];
        readonly int[] m_TasHeap = new int[NbNoeuds * 8];
        readonly int[] m_TasCle = new int[NbNoeuds * 8];
        readonly int[] m_DistTournee = new int[NbButins * NbNoeuds];
        readonly int[] m_Dist = new int[NbNoeuds];
        readonly bool[] m_Pris = new bool[NbButins];
        readonly bool[] m_BlocPris = new bool[NbBlocs];
        readonly int[] m_MurBas = new int[MaxMursBas];                // cellule * 4 + direction
        int m_NbMursBas;
        readonly int[] m_Prof = new int[3];
        readonly int[] m_Voisins = new int[16];
        readonly int[] m_CoutVoisins = new int[16];
        int m_NbVoisins;

        // ================================================================== Génération
        /// Génère le plan d'une graine, à partir de l'essai `premierEssai` (0 d'ordinaire ; le générateur passe plus
        /// loin si le NavMesh construit refuse un plan). Renvoie false si aucun essai ne respecte la consigne.
        public bool Generer(int graineDonjon, int premierEssai = 0)
        {
            graine = graineDonjon;
            rejetsStructure = rejetsTropCourt = rejetsTropLong = 0;
            for (int essai = premierEssai; essai < MaxEssais; essai++)
            {
                Amorcer(graineDonjon, essai);
                essais = essai + 1;
                if (!Construire()) { rejetsStructure++; continue; }
                if (cheminCritiqueDm < CheminMinDm) { rejetsTropCourt++; continue; }
                if (cheminCritiqueDm > CheminMaxDm) { rejetsTropLong++; continue; }
                Peupler();
                CalculerEmpreinte();
                return true;
            }
            return false;
        }

        bool Construire()
        {
            Array.Clear(plein, 0, NbNoeuds);
            Array.Clear(materiau, 0, NbNoeuds);
            Array.Clear(ouvert, 0, NbNoeuds);
            Array.Clear(eau, 0, NbCellules);
            Array.Clear(m_Occupe, 0, NbNoeuds);
            Array.Clear(m_Utilise, 0, NbNoeuds);
            for (int i = 0; i < NbNoeuds; i++) escalierDe[i] = -1;
            nbEscaliers = 0; nbTorches = 0; nbDecors = 0; m_NbMursBas = 0;

            ChoisirTypes();
            Planchers();
            ChoisirMursBas();
            OuvrirTout();
            PlacerArriveeEtPortail();
            if (!PlacerMontees()) return false;
            if (!PlacerEscalierBassin()) return false;
            if (!Connexe()) return false;
            FinaliserBords();
            Dijkstra(arrivee.Noeud, distArrivee);
            Dijkstra(portailRetour.Noeud, distRetour);
            return PlacerButins();
        }

        // ------------------------------------------------------------------ Blocs, planchers, bassin
        void ChoisirTypes()
        {
            int entree = 1 + Entier(BlocsX - 2);                 // rangée du bas, jamais dans un coin
            for (int b = 0; b < NbBlocs; b++) typeBloc[b] = TypeBloc.Hall;
            typeBloc[entree] = TypeBloc.Entree;
            // Mezzanines : blocs du pourtour (reliées au balcon), hors entrée et hors voisins directs de l'entrée.
            int nc = 0;
            for (int b = 0; b < NbBlocs; b++)
                if (Pourtour(b) && b != entree && b != entree - 1 && b != entree + 1) m_Cand[nc++] = b;
            for (int m = 0; m < NbMezzanines; m++)
            {
                int i = Entier(nc), b = m_Cand[i];
                typeBloc[b] = TypeBloc.Mezzanine;
                m_Cand[i] = m_Cand[--nc];
            }
            // La tour : une des mezzanines, qui porte le 2e étage.
            nc = 0;
            for (int b = 0; b < NbBlocs; b++) if (typeBloc[b] == TypeBloc.Mezzanine) m_Cand[nc++] = b;
            blocTour = m_Cand[Entier(nc)];
            typeBloc[blocTour] = TypeBloc.Tour;
            // Bassin : un bloc intérieur, pas juste au nord de l'entrée.
            nc = 0;
            for (int b = 0; b < NbBlocs; b++) if (!Pourtour(b) && b != entree + BlocsX) m_Cand[nc++] = b;
            blocBassin = m_Cand[Entier(nc)];
            typeBloc[blocBassin] = TypeBloc.Bassin;
        }

        static bool Pourtour(int b)
        {
            int bx = b % BlocsX, by = b / BlocsX;
            return bx == 0 || by == 0 || bx == BlocsX - 1 || by == BlocsY - 1;
        }

        static bool CellulePourtour(int c)
        {
            int x = c % Largeur, y = c / Largeur;
            return x == 0 || y == 0 || x == Largeur - 1 || y == Profondeur - 1;
        }

        void Planchers()
        {
            for (int c = 0; c < NbCellules; c++)
            {
                int b = BlocDe(c);
                TypeBloc t = typeBloc[b];
                plein[c] = true;
                if (t == TypeBloc.Bassin) eau[c] = true;
                // Balcon le long des murs d'enceinte (sauf dans l'entrée), plancher complet des mezzanines.
                if ((CellulePourtour(c) && t != TypeBloc.Entree) || t == TypeBloc.Mezzanine || t == TypeBloc.Tour)
                {
                    plein[NbCellules + c] = true;
                    materiau[NbCellules + c] = (byte)(t == TypeBloc.Mezzanine || t == TypeBloc.Tour ? 0 : (Hache(b + graine * 31) & 1));
                }
                if (t == TypeBloc.Tour) plein[2 * NbCellules + c] = true;
            }
            // Le 2e étage se prolonge le long du mur d'enceinte sur la rangée murale d'un bloc voisin : une grande salle
            // en L de 15 cellules, ouverte sur le vide des halls par sa balustrade.
            int nc = 0;
            for (int d = 0; d < 4; d++)
            {
                int bx = blocTour % BlocsX + Dx[d], by = blocTour / BlocsX + Dy[d];
                if (bx < 0 || by < 0 || bx >= BlocsX || by >= BlocsY) continue;
                int v = bx + by * BlocsX;
                if (!Pourtour(v) || typeBloc[v] == TypeBloc.Entree) continue;
                m_Cand[nc++] = v;
            }
            if (nc > 0)
            {
                int v = m_Cand[Entier(nc)];
                for (int j = 0; j < BlocTaille; j++)
                    for (int i = 0; i < BlocTaille; i++)
                    {
                        int c = CelluleBloc(v, i, j);
                        if (CellulePourtour(c) && plein[NbCellules + c]) plein[2 * NbCellules + c] = true;
                    }
            }
        }

        static uint Hache(int n)
        {
            uint h = (uint)n * 2654435761u;
            h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12;
            return h;
        }

        /// Quelques murs à demi-hauteur (2 m) entre deux halls voisins, sur une seule cellule de large : ils
        /// découpent l'espace sans fermer les salles, on voit par-dessus.
        void ChoisirMursBas()
        {
            for (int essai = 0; essai < 12 && m_NbMursBas < MaxMursBas; essai++)
            {
                int b = Entier(NbBlocs), d = Entier(2), t = Entier(2) == 0 ? 0 : 2;   // jamais au milieu
                int bx = b % BlocsX, by = b / BlocsX;
                if (d == 0 && by == BlocsY - 1 || d == 1 && bx == BlocsX - 1) continue;
                int v = d == 0 ? b + BlocsX : b + 1;
                if (typeBloc[b] != TypeBloc.Hall || typeBloc[v] != TypeBloc.Hall) continue;
                int c = d == 0 ? CelluleBloc(b, t, BlocTaille - 1) : CelluleBloc(b, BlocTaille - 1, t);
                if (CellulePourtour(c) || CellulePourtour(Voisine(c, d))) continue;
                bool deja = false;
                for (int i = 0; i < m_NbMursBas; i++) if (m_MurBas[i] / 4 == c) deja = true;
                if (deja) continue;
                m_MurBas[m_NbMursBas++] = c * 4 + d;
            }
        }

        bool EstMurBas(int c, int d)
        {
            int n = Voisine(c, d);
            for (int i = 0; i < m_NbMursBas; i++)
            {
                int mc = m_MurBas[i] / 4, md = m_MurBas[i] % 4;
                if ((mc == c && md == d) || (mc == n && ((md + 2) & 3) == d)) return true;
            }
            return false;
        }

        /// Ouvre tous les passages entre cellules praticables voisines d'un même niveau, sauf entre le bassin et le
        /// rez (muret de 2 m) et là où un mur bas est prévu. Rappelée après chaque escalier (trémies, paliers).
        void OuvrirTout()
        {
            Array.Clear(ouvert, 0, NbNoeuds);
            for (int k = 0; k < NbNiveaux; k++)
                for (int c = 0; c < NbCellules; c++)
                {
                    int no = k * NbCellules + c;
                    if (!Praticable(no)) continue;
                    for (int d = 0; d < 2; d++)
                    {
                        int n = Voisine(c, d);
                        if (n < 0 || !Praticable(k * NbCellules + n)) continue;
                        if (k == 0 && (eau[c] != eau[n] || EstMurBas(c, d))) continue;
                        ouvert[no] |= (byte)(1 << d);
                        ouvert[k * NbCellules + n] |= (byte)(1 << ((d + 2) & 3));
                    }
                }
        }

        // ------------------------------------------------------------------ Arrivée, portail de retour
        void PlacerArriveeEtPortail()
        {
            int e = 0;
            for (int b = 0; b < NbBlocs; b++) if (typeBloc[b] == TypeBloc.Entree) e = b;
            int ac = CelluleBloc(e, 1, 0);
            arrivee = PoseCellule(0, ac, 0f, 0f, 0f);
            m_Occupe[ac] = 31; m_Utilise[ac] = true;
            // Portail : cellule du bas à gauche ou à droite, adossé au mur d'enceinte sud, tourné vers le nord.
            int pc = CelluleBloc(e, Entier(2) == 0 ? 0 : 2, 0);
            portailRetour = PoseCellule(0, pc, 0f, -0.9f, 0f);
            m_Occupe[pc] = 31; m_Utilise[pc] = true;
        }

        // ------------------------------------------------------------------ Escaliers
        /// Deux escaliers droits contre un mur d'enceinte, du rez au balcon : d'abord de quoi desservir chaque
        /// morceau de balcon, puis au hasard, loin l'un de l'autre (deux montées possibles).
        bool PlacerMontees()
        {
            for (int k = 0; k + 1 < NbNiveaux; k++)
            {
                int nbComp = Composantes(k + 1);
                if (nbComp != 1) return false;              // une seule montée par étage : l'étage doit être d'un seul tenant
                int nc = Candidats(k, 0);
                if (nc == 0) return false;
                // On monte vers la tour : parmi les emplacements dont le palier est le plus près du bloc de la tour
                // (à un bloc près pour le rez), un au hasard. Le trajet vers le 2e étage reste dans la consigne.
                int dmin = int.MaxValue;
                for (int i = 0; i < nc; i++) dmin = Math.Min(dmin, DistanceTour(k, m_Cand[i]));
                int nn = 0;
                for (int i = 0; i < nc; i++) if (DistanceTour(k, m_Cand[i]) <= dmin + (k == 0 ? 1 : 0)) m_Cand[nn++] = m_Cand[i];
                PoserMontee(k, m_Cand[Entier(nn)]);
                if (Composantes(k + 1) != 1) return false;
            }
            return nbEscaliers == NbMontees;
        }

        int DistanceTour(int k, int code)
        {
            Escalier e;
            Decrire(k, code, out e);
            int b = BlocDe(e.arrivee);
            return Math.Abs(b % BlocsX - blocTour % BlocsX) + Math.Abs(b / BlocsX - blocTour / BlocsX);
        }

        bool Servie(int comp)
        {
            for (int i = 0; i < nbEscaliers; i++)
                if (!escaliers[i].bassin && m_Comp[(escaliers[i].niveau + 1) * NbCellules + escaliers[i].arrivee] == comp) return true;
            return false;
        }

        /// Numérote les composantes connexes du niveau k (m_Comp), renvoie leur nombre.
        int Composantes(int k)
        {
            for (int c = 0; c < NbCellules; c++) m_Comp[k * NbCellules + c] = -1;
            int n = 0;
            for (int c = 0; c < NbCellules; c++)
            {
                int noeud = k * NbCellules + c;
                if (!Praticable(noeud) || m_Comp[noeud] >= 0) continue;
                int sp = 0;
                m_Pile[sp++] = noeud; m_Comp[noeud] = n;
                while (sp > 0)
                {
                    int x = m_Pile[--sp], xc = x % NbCellules;
                    for (int d = 0; d < 4; d++)
                    {
                        if ((ouvert[x] & (1 << d)) == 0) continue;
                        int y = k * NbCellules + Voisine(xc, d);
                        if (!Praticable(y) || m_Comp[y] >= 0) continue;
                        m_Comp[y] = n; m_Pile[sp++] = y;
                    }
                }
                n++;
            }
            return n;
        }

        /// Emplacements de montée valides (codés bloc * 12 + dir * 3 + ligne) : la ligne longe un mur d'enceinte.
        /// comp >= 0 : palier dans cette composante du balcon. Deux montées sont à au moins deux blocs l'une de l'autre.
        int Candidats(int k, int comp)
        {
            int nc = 0;
            for (int b = 0; b < NbBlocs; b++)
            {
                if (typeBloc[b] == TypeBloc.Entree || typeBloc[b] == TypeBloc.Bassin || !Pourtour(b)) continue;
                for (int d = 0; d < 4; d++)
                    for (int ligne = 0; ligne < BlocTaille; ligne++)
                    {
                        int code = b * 12 + d * 3 + ligne;
                        Escalier e;
                        if (!Decrire(k, code, out e) || !Valide(e) || !ContreMur(e)) continue;
                        if (comp >= 0 && m_Comp[(k + 1) * NbCellules + e.arrivee] != comp) continue;
                        if (nc < m_Cand.Length) m_Cand[nc++] = code;
                    }
            }
            return nc;
        }

        /// Les deux cellules de l'escalier longent le mur d'enceinte.
        static bool ContreMur(Escalier e)
        {
            int dg = (e.dir + 1) & 3, dd = (e.dir + 3) & 3;
            return (Voisine(e.bas, dg) < 0 && Voisine(e.haut, dg) < 0) || (Voisine(e.bas, dd) < 0 && Voisine(e.haut, dd) < 0);
        }

        bool Decrire(int k, int code, out Escalier e)
        {
            int b = code / 12, d = code % 12 / 3, ligne = code % 3;
            int bx = b % BlocsX * BlocTaille, by = b / BlocsX * BlocTaille;
            e = new Escalier { niveau = k, dir = d };
            for (int t = 0; t < 3; t++)
            {
                int x, y;
                switch (d)
                {
                    case 0: x = bx + ligne; y = by + t; break;
                    case 2: x = bx + ligne; y = by + 2 - t; break;
                    case 1: x = bx + t; y = by + ligne; break;
                    default: x = bx + 2 - t; y = by + ligne; break;
                }
                m_Prof[t] = x + y * Largeur;
            }
            int arr = Voisine(m_Prof[2], d);
            if (arr < 0) return false;
            e.depart = m_Prof[0]; e.bas = m_Prof[1]; e.haut = m_Prof[2]; e.arrivee = arr;
            return true;
        }

        bool Valide(Escalier e)
        {
            int N = NbCellules, a = e.niveau * N, h = (e.niveau + 1) * N;
            if (!Praticable(a + e.depart) || !Praticable(a + e.bas) || !Praticable(a + e.haut)) return false;
            if (e.niveau == 0 && (eau[e.depart] || eau[e.bas] || eau[e.haut])) return false;
            if (!Praticable(h + e.arrivee)) return false;
            if (m_Utilise[a + e.depart] || m_Utilise[a + e.bas] || m_Utilise[a + e.haut]) return false;
            if (m_Utilise[h + e.bas] || m_Utilise[h + e.haut] || m_Utilise[h + e.arrivee]) return false;
            if (m_Occupe[a + e.depart] != 0 || m_Occupe[h + e.arrivee] != 0) return false;
            if (ouvert[a + e.depart] == 0) return false;
            // Au-dessus du balcon, pas de trémie dans le 2e étage : l'escalier monte à côté de la grande salle.
            if (e.niveau >= 1 && (plein[h + e.bas] || plein[h + e.haut])) return false;
            // Rien ne doit passer sous un escalier du balcon (pas d'autre escalier dessous).
            if (e.niveau >= 1 && (escalierDe[e.bas] >= 0 || escalierDe[e.haut] >= 0 || escalierDe[e.depart] >= 0)) return false;
            return true;
        }

        void PoserMontee(int k, int code)
        {
            Escalier e;
            Decrire(k, code, out e);
            int N = NbCellules, a = k * N, h = (k + 1) * N, idx = nbEscaliers++;
            escaliers[idx] = e;
            escalierDe[a + e.bas] = (sbyte)idx; escalierDe[a + e.haut] = (sbyte)idx;
            // Trémie au-dessus de l'escalier ; le balcon (niveau 1) est élargi d'une cellule côté salle tout le long,
            // pour rester continu autour de la trémie (rez → balcon) ou autour de l'escalier posé dessus (balcon → étage).
            plein[h + e.bas] = false; plein[h + e.haut] = false;
            int dIn = Voisine(e.bas, (e.dir + 1) & 3) < 0 ? (e.dir + 3) & 3 : (e.dir + 1) & 3;
            for (int t = 0; t < 4; t++)
            {
                int c = t == 0 ? e.depart : t == 1 ? e.bas : t == 2 ? e.haut : e.arrivee;
                int ci = Voisine(c, dIn);
                if (ci < 0 || eau[ci] || escalierDe[ci] >= 0) continue;
                if (!plein[N + ci]) { plein[N + ci] = true; materiau[N + ci] = 1; }
                m_Utilise[N + ci] = true;
            }
            m_Utilise[a + e.depart] = true; m_Utilise[a + e.bas] = true; m_Utilise[a + e.haut] = true;
            m_Utilise[h + e.bas] = true; m_Utilise[h + e.haut] = true; m_Utilise[h + e.arrivee] = true;
            // Sous un escalier du balcon, pas de montée du rez ni de décor au rez.
            if (k >= 1) { m_Utilise[e.bas] = true; m_Utilise[e.haut] = true; }
            OuvrirTout();
        }

        /// Descente au bassin : escalier de pierre d'une cellule au milieu d'un côté du bassin.
        bool PlacerEscalierBassin()
        {
            int d0 = Entier(4);
            for (int i = 0; i < 4; i++)
            {
                int d = (d0 + i) & 3;                                     // côté du bassin par où l'on sort
                int cs = CelluleBloc(blocBassin, 1 + Dx[d], 1 + Dy[d]);    // cellule de l'escalier (bord du bassin)
                int dehors = Voisine(cs, d), pied = CelluleBloc(blocBassin, 1, 1);
                if (dehors < 0 || !Praticable(dehors) || eau[dehors] || m_Utilise[dehors] || m_Occupe[dehors] != 0) continue;
                int idx = nbEscaliers++;
                escaliers[idx] = new Escalier { niveau = 0, dir = d, depart = pied, bas = cs, haut = cs, arrivee = dehors, bassin = true };
                escalierDe[cs] = (sbyte)idx;
                m_Utilise[cs] = true; m_Utilise[dehors] = true; m_Utilise[pied] = true;
                OuvrirTout();
                return true;
            }
            return false;
        }

        public bool Praticable(int noeud) { return plein[noeud] && escalierDe[noeud] < 0; }

        /// Hauteur du sol d'un nœud (m) : niveau x 4 m, fond du bassin à -2 m.
        public float HauteurSol(int noeud)
        {
            int k = noeud / NbCellules;
            return k == 0 && eau[noeud % NbCellules] ? -ProfondeurBassin : k * HauteurNiveau;
        }

        // ------------------------------------------------------------------ Connexité et distances
        bool Connexe()
        {
            for (int i = 0; i < NbNoeuds; i++) m_Comp[i] = 0;
            int sp = 0, depart = arrivee.Noeud;
            m_Pile[sp++] = depart; m_Comp[depart] = 1;
            while (sp > 0)
            {
                int x = m_Pile[--sp];
                ParcourirVoisins(x);
                for (int v = 0; v < m_NbVoisins; v++)
                {
                    int y = m_Voisins[v];
                    if (m_Comp[y] != 0) continue;
                    m_Comp[y] = 1; m_Pile[sp++] = y;
                }
            }
            for (int i = 0; i < NbNoeuds; i++) if (Praticable(i) && m_Comp[i] == 0) return false;
            return true;
        }

        /// Voisins praticables d'un nœud et coûts (dm) : 4 directions ouvertes, diagonales dans un carré 2 x 2
        /// entièrement ouvert, escaliers ; dans l'eau, coût x 5/3 (vitesse x 0,6).
        void ParcourirVoisins(int x)
        {
            m_NbVoisins = 0;
            int k = x / NbCellules, c = x % NbCellules, cx = c % Largeur, cy = c / Largeur, baseK = k * NbCellules;
            bool dansEau = k == 0 && eau[c];
            byte o = ouvert[x];
            for (int d = 0; d < 4; d++)
            {
                if ((o & (1 << d)) == 0) continue;
                int n = baseK + Voisine(c, d);
                if (!Praticable(n)) continue;
                m_Voisins[m_NbVoisins] = n; m_CoutVoisins[m_NbVoisins++] = dansEau ? PasOrtho * 5 / 3 : PasOrtho;
            }
            for (int d = 0; d < 4; d++)
            {
                int d2 = (d + 1) & 3;
                if ((o & (1 << d)) == 0 || (o & (1 << d2)) == 0) continue;
                int a = baseK + cx + Dx[d] + (cy + Dy[d]) * Largeur;
                int b = baseK + cx + Dx[d2] + (cy + Dy[d2]) * Largeur;
                if ((ouvert[a] & (1 << d2)) == 0 || (ouvert[b] & (1 << d)) == 0) continue;
                int n = baseK + cx + Dx[d] + Dx[d2] + (cy + Dy[d] + Dy[d2]) * Largeur;
                if (!Praticable(n) || !Praticable(a) || !Praticable(b)) continue;
                m_Voisins[m_NbVoisins] = n; m_CoutVoisins[m_NbVoisins++] = dansEau ? PasDiag * 5 / 3 : PasDiag;
            }
            for (int i = 0; i < nbEscaliers; i++)
            {
                Escalier e = escaliers[i];
                int dep = e.niveau * NbCellules + e.depart, arr = (e.bassin ? 0 : e.niveau + 1) * NbCellules + e.arrivee;
                int cout = e.bassin ? PasEscalierBassin : PasEscalier;
                if (x == dep) { m_Voisins[m_NbVoisins] = arr; m_CoutVoisins[m_NbVoisins++] = cout; }
                else if (x == arr) { m_Voisins[m_NbVoisins] = dep; m_CoutVoisins[m_NbVoisins++] = cout; }
            }
        }

        /// Plus courts chemins (dm) depuis un nœud, tas binaire préalloué.
        public void Dijkstra(int depart, int[] dist)
        {
            for (int i = 0; i < NbNoeuds; i++) dist[i] = int.MaxValue;
            dist[depart] = 0;
            int taille = 0;
            Empiler(ref taille, depart, 0);
            while (taille > 0)
            {
                int cle = m_TasCle[0], x = m_TasHeap[0];
                Depiler(ref taille);
                if (cle > dist[x]) continue;
                ParcourirVoisins(x);
                for (int v = 0; v < m_NbVoisins; v++)
                {
                    int y = m_Voisins[v], nd = cle + m_CoutVoisins[v];
                    if (nd >= dist[y]) continue;
                    dist[y] = nd;
                    Empiler(ref taille, y, nd);
                }
            }
        }

        void Empiler(ref int taille, int x, int cle)
        {
            if (taille >= m_TasHeap.Length) return;
            int i = taille++;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (m_TasCle[p] <= cle) break;
                m_TasCle[i] = m_TasCle[p]; m_TasHeap[i] = m_TasHeap[p]; i = p;
            }
            m_TasCle[i] = cle; m_TasHeap[i] = x;
        }

        void Depiler(ref int taille)
        {
            taille--;
            if (taille == 0) return;
            int cle = m_TasCle[taille], x = m_TasHeap[taille], i = 0;
            while (true)
            {
                int g = 2 * i + 1;
                if (g >= taille) break;
                int dd = g + 1 < taille && m_TasCle[g + 1] < m_TasCle[g] ? g + 1 : g;
                if (m_TasCle[dd] >= cle) break;
                m_TasCle[i] = m_TasCle[dd]; m_TasHeap[i] = m_TasHeap[dd]; i = dd;
            }
            m_TasCle[i] = cle; m_TasHeap[i] = x;
        }

        /// Tournée complète d'un joueur seul : arrivée → tous les butins (plus proche voisin) → portail, en dm.
        public int CalculerTourneeDm()
        {
            for (int i = 0; i < NbButins; i++)
            {
                Dijkstra(butins[i].Noeud, m_Dist);
                Array.Copy(m_Dist, 0, m_DistTournee, i * NbNoeuds, NbNoeuds);
                m_Pris[i] = false;
            }
            int total = 0, pos = -1;
            for (int t = 0; t < NbButins; t++)
            {
                int best = -1, dmin = int.MaxValue;
                for (int i = 0; i < NbButins; i++)
                {
                    if (m_Pris[i]) continue;
                    int dd = pos < 0 ? distArrivee[butins[i].Noeud] : m_DistTournee[pos * NbNoeuds + butins[i].Noeud];
                    if (dd < dmin) { dmin = dd; best = i; }
                }
                m_Pris[best] = true; total += dmin; pos = best;
            }
            return total + distRetour[butins[pos].Noeud];
        }

        // ------------------------------------------------------------------ Bords
        void FinaliserBords()
        {
            for (int k = 0; k < NbNiveaux; k++)
            {
                for (int vy = 0; vy <= Profondeur; vy++)
                    for (int x = 0; x < Largeur; x++)
                    {
                        int a = vy > 0 ? x + (vy - 1) * Largeur : -1, b = vy < Profondeur ? x + vy * Largeur : -1;
                        bordH[(k * (Profondeur + 1) + vy) * Largeur + x] = BordEntre(k, a, b, 0);
                    }
                for (int y = 0; y < Profondeur; y++)
                    for (int vx = 0; vx <= Largeur; vx++)
                    {
                        int a = vx > 0 ? vx - 1 + y * Largeur : -1, b = vx < Largeur ? vx + y * Largeur : -1;
                        bordV[(k * Profondeur + y) * (Largeur + 1) + vx] = BordEntre(k, a, b, 1);
                    }
            }
        }

        Bord BordEntre(int k, int a, int b, int d)
        {
            if (a < 0 || b < 0) return Bord.MurExterieur;
            int na = k * NbCellules + a, nb = k * NbCellules + b;
            if (k == 0 && eau[a] != eau[b])
            {
                // Muret du bassin, sauf là où l'escalier du bassin débouche.
                for (int i = 0; i < nbEscaliers; i++)
                    if (escaliers[i].bassin && ((escaliers[i].bas == a && escaliers[i].arrivee == b) || (escaliers[i].bas == b && escaliers[i].arrivee == a))) return Bord.Rien;
                return Bord.Bassin;
            }
            if (escalierDe[na] >= 0 || escalierDe[nb] >= 0) return Bord.Rien;   // les flancs de l'escalier font mur
            bool pa = plein[na], pb = plein[nb];
            if (!pa && !pb) return Bord.Rien;
            if (pa && pb)
            {
                if ((ouvert[na] & (1 << d)) == 0) return k == 0 && EstMurBas(a, d) ? Bord.MurBas : Bord.Mur;
                // Passage sous le bord d'un plancher : arcade de bois (rez seulement).
                if (k + 1 < NbNiveaux && !(k == 0 && eau[a]) && plein[(k + 1) * NbCellules + a] != plein[(k + 1) * NbCellules + b]) return Bord.Arcade;
                return Bord.Rien;
            }
            if (k == 0) return Bord.Mur;
            int w = pa ? a : b, v = pa ? b : a;
            for (int i = 0; i < nbEscaliers; i++)
                if (!escaliers[i].bassin && escaliers[i].niveau == k - 1 && escaliers[i].haut == v && escaliers[i].arrivee == w) return Bord.Rien;
            // Bord d'un plancher au-dessus d'un escalier du niveau inférieur posé sur un plancher (balcon → étage) :
            // les flancs de l'escalier dépassent déjà, balustrade quand même pour ne pas tomber.
            return Bord.GardeCorps;
        }

        /// Bord d'une cellule au niveau k dans la direction d.
        public Bord BordCellule(int k, int c, int d)
        {
            int x = c % Largeur, y = c / Largeur;
            switch (d)
            {
                case 0: return bordH[(k * (Profondeur + 1) + y + 1) * Largeur + x];
                case 2: return bordH[(k * (Profondeur + 1) + y) * Largeur + x];
                case 1: return bordV[(k * Profondeur + y) * (Largeur + 1) + x + 1];
                default: return bordV[(k * Profondeur + y) * (Largeur + 1) + x];
            }
        }

        public Bord BordH(int k, int x, int vy) { return bordH[(k * (Profondeur + 1) + vy) * Largeur + x]; }
        public Bord BordV(int k, int vx, int y) { return bordV[(k * Profondeur + y) * (Largeur + 1) + vx]; }

        public static bool EstMur(Bord b) { return b == Bord.Mur || b == Bord.MurExterieur; }
        /// Bord qui arrête la marche (on ne le franchit pas).
        public static bool Bloquant(Bord b) { return b != Bord.Rien && b != Bord.Arcade; }

        /// Sommet (vx, vy) au niveau k : 0 rien, 1 pilier de pierre (4 m), 2 poteau de balustrade.
        public byte Sommet(int k, int vx, int vy)
        {
            Bord g = vx > 0 ? BordH(k, vx - 1, vy) : Bord.Rien;
            Bord dr = vx < Largeur ? BordH(k, vx, vy) : Bord.Rien;
            Bord ba = vy > 0 ? BordV(k, vx, vy - 1) : Bord.Rien;
            Bord ha = vy < Profondeur ? BordV(k, vx, vy) : Bord.Rien;
            int murs = (EstMur(g) ? 1 : 0) + (EstMur(dr) ? 1 : 0) + (EstMur(ba) ? 1 : 0) + (EstMur(ha) ? 1 : 0);
            int gc = (g == Bord.GardeCorps ? 1 : 0) + (dr == Bord.GardeCorps ? 1 : 0) + (ba == Bord.GardeCorps ? 1 : 0) + (ha == Bord.GardeCorps ? 1 : 0);
            bool murDroit = murs == 2 && ((EstMur(g) && EstMur(dr)) || (EstMur(ba) && EstMur(ha)));
            if (murs > 0 && (!murDroit || gc > 0)) return 1;
            bool gcDroit = gc == 2 && ((g == Bord.GardeCorps && dr == Bord.GardeCorps) || (ba == Bord.GardeCorps && ha == Bord.GardeCorps));
            if (gc > 0 && murs == 0 && !gcDroit) return 2;
            return 0;
        }

        // ------------------------------------------------------------------ Butins et chemin critique
        bool PlacerButins()
        {
            Array.Clear(m_BlocPris, 0, NbBlocs);
            int nb = 0;
            // Grand coffre : dans la grande salle du 2e étage, là où l'aller-retour est le plus long.
            int n = MeilleurNoeud(2, -1, 4);
            if (n < 0) return false;
            butins[nb++] = PoserCoffre(n, TypeButin.GrandCoffre);
            // Coffres : un au rez sous un plancher (salle couverte), un en hauteur ; parmi les plus lointains.
            n = MeilleurNoeud(0, 1, 4);
            if (n < 0) return false;
            butins[nb++] = PoserCoffre(n, TypeButin.Coffre);
            n = MeilleurNoeud(1, -1, 4);
            if (n < 0) return false;
            butins[nb++] = PoserCoffre(n, TypeButin.Coffre);
            // Tas d'or : un dans le bassin, un au balcon, un dans un hall du rez, un au rez n'importe où.
            for (int t = 0; t < 4; t++)
            {
                n = NoeudTasOr(t);
                if (n < 0) n = NoeudTasOr(9);
                if (n < 0) return false;
                butins[nb++] = PoserTasOr(n);
            }
            cheminCritiqueDm = 0; butinCritique = 0;
            for (int i = 0; i < NbButins; i++)
            {
                int no = butins[i].Noeud;
                int l = distArrivee[no] + distRetour[no];
                if (l > cheminCritiqueDm) { cheminCritiqueDm = l; butinCritique = i; }
            }
            return true;
        }

        bool LibrePourButin(int noeud)
        {
            if (!Praticable(noeud) || m_Occupe[noeud] != 0 || m_Utilise[noeud]) return false;
            int c = noeud % NbCellules;
            if (typeBloc[BlocDe(c)] == TypeBloc.Entree) return false;
            if (m_BlocPris[BlocDe(c)]) return false;
            if (distArrivee[noeud] == int.MaxValue) return false;
            // Seuls les emplacements dont l'aller-retour tient la consigne des 90 s peuvent porter du butin.
            return distArrivee[noeud] + distRetour[noeud] <= CheminMaxDm;
        }

        /// Nœud libre du niveau k au plus long aller-retour. couvert = 1 : sous un plancher ; hasard > 1 : tirage
        /// parmi les nœuds dont l'aller-retour dépasse 75 % du meilleur. Jamais dans l'eau (les coffres).
        int MeilleurNoeud(int k, int couvert, int hasard)
        {
            int nc = 0;
            for (int c = 0; c < NbCellules; c++)
            {
                int no = k * NbCellules + c;
                if (!LibrePourButin(no) || (k == 0 && eau[c])) continue;
                if (couvert == 1 && (k + 1 >= NbNiveaux || !plein[(k + 1) * NbCellules + c])) continue;
                if (nc < m_Cand.Length) m_Cand[nc++] = no;
            }
            if (nc == 0) return -1;
            int best = -1, bestRt = -1;
            for (int i = 0; i < nc; i++)
            {
                int rt = distArrivee[m_Cand[i]] + distRetour[m_Cand[i]];
                if (rt > bestRt) { bestRt = rt; best = m_Cand[i]; }
            }
            if (hasard <= 1) return best;
            int nn = 0;
            for (int i = 0; i < nc; i++)
            {
                int rt = distArrivee[m_Cand[i]] + distRetour[m_Cand[i]];
                if (rt * 4 >= bestRt * 3) m_Cand[nn++] = m_Cand[i];
            }
            return m_Cand[Entier(nn)];
        }

        int NoeudTasOr(int t)
        {
            int nc = 0;
            for (int no = 0; no < NbNoeuds; no++)
            {
                if (!LibrePourButin(no)) continue;
                int k = no / NbCellules, c = no % NbCellules;
                bool dansEau = k == 0 && eau[c];
                if (t == 0 && !dansEau) continue;
                if (t == 1 && k == 0) continue;
                if (t == 2 && (k != 0 || dansEau || plein[NbCellules + c])) continue;
                if (t == 3 && (k != 0 || dansEau)) continue;
                if (nc < m_Cand.Length) m_Cand[nc++] = no;
            }
            if (nc == 0) return -1;
            return m_Cand[Entier(nc)];
        }

        /// Coffre adossé à un mur ou à une balustrade (sinon au centre), tourné vers la salle : il ne bouche jamais
        /// un balcon d'une cellule de large.
        Pose PoserCoffre(int noeud, TypeButin type)
        {
            int k = noeud / NbCellules, c = noeud % NbCellules;
            int dos = -1;
            for (int d = 0; d < 4 && dos < 0; d++) if (EstMur(BordCellule(k, c, d))) dos = d;
            for (int d = 0; d < 4 && dos < 0; d++) if (Bloquant(BordCellule(k, c, d))) dos = d;
            Pose p;
            if (dos >= 0) p = PoseCellule(k, c, Dx[dos] * 0.75f, Dy[dos] * 0.75f, Angle(-Dx[dos], -Dy[dos]));
            else
            {
                int dmin = int.MaxValue, dir = 2;
                for (int d = 0; d < 4; d++)
                {
                    if ((ouvert[noeud] & (1 << d)) == 0) continue;
                    int v = k * NbCellules + Voisine(c, d);
                    if (Praticable(v) && distArrivee[v] < dmin) { dmin = distArrivee[v]; dir = d; }
                }
                p = PoseCellule(k, c, 0f, 0f, Angle(Dx[dir], Dy[dir]));
            }
            p.type = (byte)type;
            m_Occupe[noeud] = 31;
            m_BlocPris[BlocDe(c)] = true;
            return p;
        }

        Pose PoserTasOr(int noeud)
        {
            int k = noeud / NbCellules, c = noeud % NbCellules;
            float ox = 0f, oz = 0f;
            int d0 = Entier(4);
            for (int i = 0; i < 4; i++)
            {
                int d = (d0 + i) & 3;
                if (!Bloquant(BordCellule(k, c, d))) continue;
                ox = Dx[d] * 1.1f; oz = Dy[d] * 1.1f; break;
            }
            Pose p = PoseCellule(k, c, ox, oz, Entier(4) * 90f);
            p.type = (byte)TypeButin.TasOr;
            p.variante = (byte)Entier(2);
            m_Occupe[noeud] = 31;
            m_BlocPris[BlocDe(c)] = true;
            return p;
        }

        // ------------------------------------------------------------------ Apparitions, torches, décor
        void Peupler()
        {
            PlacerApparitions();
            PlacerTorches();
            PlacerDecor();
        }

        /// Régions (bloc, niveau) triées de la plus lointaine à la plus proche ; chacune reçoit une apparition typée,
        /// puis on recommence, jusqu'à 24. L'entrée n'en a jamais.
        void PlacerApparitions()
        {
            int nr = 0;
            for (int b = 0; b < NbBlocs; b++)
            {
                if (typeBloc[b] == TypeBloc.Entree) continue;
                for (int k = 0; k < NbNiveaux; k++)
                {
                    int dmax = -1, nbc = 0;
                    for (int j = 0; j < BlocTaille; j++)
                        for (int i = 0; i < BlocTaille; i++)
                        {
                            int no = k * NbCellules + CelluleBloc(b, i, j);
                            if (!Praticable(no)) continue;
                            nbc++;
                            if (distArrivee[no] > dmax) dmax = distArrivee[no];
                        }
                    if (dmax < 0 || nbc < 2) continue;
                    m_Ordre[nr] = b * NbNiveaux + k; m_Cand[nr] = dmax; nr++;
                }
            }
            for (int i = 1; i < nr; i++)
            {
                int r = m_Ordre[i], dd = m_Cand[i], j = i - 1;
                while (j >= 0 && m_Cand[j] < dd) { m_Ordre[j + 1] = m_Ordre[j]; m_Cand[j + 1] = m_Cand[j]; j--; }
                m_Ordre[j + 1] = r; m_Cand[j + 1] = dd;
            }
            int na = 0;
            for (int passe = 0; passe < 4 && na < NbApparitions; passe++)
                for (int i = 0; i < nr && na < NbApparitions; i++)
                {
                    int b = m_Ordre[i] / NbNiveaux, k = m_Ordre[i] % NbNiveaux;
                    na = ApparitionDansRegion(b, k, TypeRegion(b, k, passe), na);
                }
        }

        TypeApparition TypeRegion(int b, int k, int passe)
        {
            TypeBloc t = typeBloc[b];
            if (k == 2) return passe == 0 ? TypeApparition.Guerrier : TypeApparition.Mage;
            if (k == 1) return t == TypeBloc.Mezzanine || t == TypeBloc.Tour ? (passe == 0 ? TypeApparition.Mage : TypeApparition.Guerrier) : TypeApparition.Voleur;
            if (t == TypeBloc.Mezzanine || t == TypeBloc.Tour) return TypeApparition.Guerrier;
            return TypeApparition.Sbire;
        }

        int ApparitionDansRegion(int b, int k, TypeApparition t, int na)
        {
            int n = 0;
            for (int j = 0; j < BlocTaille; j++)
                for (int i = 0; i < BlocTaille; i++)
                {
                    int no = k * NbCellules + CelluleBloc(b, i, j);
                    if (!Praticable(no) || (m_Occupe[no] & 16) != 0 || m_Utilise[no]) continue;
                    m_Pile[n++] = no;
                }
            if (n == 0) return na;
            int ch = m_Pile[Entier(n)];
            m_Occupe[ch] |= 16;
            float jx = (Entier(9) - 4) * 0.15f, jz = (Entier(9) - 4) * 0.15f;
            Pose p = PoseCellule(ch / NbCellules, ch % NbCellules, jx, jz, 0f);
            float dx = arrivee.x - p.x, dz = arrivee.z - p.z;
            p.rotY = (float)Math.Round(Angle(dx, dz) / 45f) * 45f;
            p.type = (byte)t;
            p.variante = (byte)Entier(5);
            apparitions[na++] = p;
            return na;
        }

        /// Une torche par région (bloc, niveau) sur un mur plein (deux dans l'entrée) ; sans mur : une torchère
        /// (colonne + torche) dans un coin de cellule du rez. Une quarantaine de lumières, sans ombres.
        void PlacerTorches()
        {
            for (int b = 0; b < NbBlocs; b++)
                for (int k = 0; k < NbNiveaux; k++)
                {
                    int nt = typeBloc[b] == TypeBloc.Entree ? 2 : 1;
                    for (int t = 0; t < nt && nbTorches < MaxTorches; t++)
                    {
                        int bc = -1, bd = 0, bs = int.MinValue, libre = -1;
                        for (int j = 0; j < BlocTaille; j++)
                            for (int i = 0; i < BlocTaille; i++)
                            {
                                int c = CelluleBloc(b, i, j), no = k * NbCellules + c;
                                if (!Praticable(no) || (k == 0 && eau[c])) continue;
                                if (libre < 0 && m_Occupe[no] == 0 && !m_Utilise[no]) libre = no;
                                for (int d = 0; d < 4; d++)
                                {
                                    if (!EstMur(BordCellule(k, c, d)) || no == portailRetour.Noeud) continue;
                                    float tx = (c % Largeur + 0.5f + Dx[d] * 0.4f) * Cellule, tz = (c / Largeur + 0.5f + Dy[d] * 0.4f) * Cellule;
                                    float dmin = 1e9f;
                                    for (int q = 0; q < nbTorches; q++)
                                    {
                                        if (torches[q].niveau != k) continue;
                                        float ex = torches[q].x - tx, ez = torches[q].z - tz, dd = ex * ex + ez * ez;
                                        if (dd < dmin) dmin = dd;
                                    }
                                    int sc = (i == 1 || j == 1 ? 40 : 0) + (int)Math.Min(dmin, 900f) + Entier(8);
                                    if (sc > bs) { bs = sc; bc = c; bd = d; }
                                }
                            }
                        if (bc >= 0)
                        {
                            Pose p = PoseCellule(k, bc, Dx[bd] * 1.5f, Dy[bd] * 1.5f, Angle(-Dx[bd], -Dy[bd]));
                            p.type = TorcheMurale; p.variante = (byte)bd;
                            torches[nbTorches++] = p;
                        }
                        else if (libre >= 0 && t == 0 && k == 0)
                        {
                            m_Occupe[libre] |= 1;
                            Pose p = PoseCellule(0, libre % NbCellules, 1.2f, 1.2f, Entier(4) * 90f);
                            p.type = Torchere;
                            torches[nbTorches++] = p;
                        }
                    }
                }
        }

        /// Mobilier des planches KayKit : tonneaux, caisses, tables, dans les coins entre deux murs ; longues tables le
        /// long des murs du rez ; bannières ; os sur les points d'apparition ; tonneaux flottants dans le bassin.
        /// Jamais rien qui ressemble à du butin (ni or, ni coffre, ni sac) : seul le vrai butin en a l'allure.
        void PlacerDecor()
        {
            for (int k = 0; k < NbNiveaux; k++)
                for (int c = 0; c < NbCellules; c++)
                {
                    int no = k * NbCellules + c;
                    if (!Praticable(no) || m_Utilise[no] || (k == 0 && eau[c])) continue;
                    if (k == 0 && EscalierAuDessus(c)) continue;
                    for (int coin = 0; coin < 4; coin++)
                    {
                        int dv = (coin == 0 || coin == 3) ? 0 : 2, dh = (coin == 0 || coin == 1) ? 1 : 3;
                        if (!EstMur(BordCellule(k, c, dv)) || !EstMur(BordCellule(k, c, dh))) continue;
                        byte q = (byte)(1 << coin);
                        if ((m_Occupe[no] & q) != 0 || m_Occupe[no] == 31) continue;
                        if (Entier(100) >= 55) continue;
                        m_Occupe[no] |= q;
                        AjouterDecor(DecorCoin, k, c, Dx[dh] * 1.1f, Dy[dv] * 1.1f, Entier(4) * 90f + (Entier(21) - 10));
                    }
                }
            // Longues tables contre un mur du rez, sous le balcon (il reste 1,5 m de passage et la salle à côté).
            int tables = 0;
            for (int essai = 0; essai < 40 && tables < 4; essai++)
            {
                int c = Entier(NbCellules), no = c;
                if (!Praticable(no) || m_Utilise[no] || eau[c] || m_Occupe[no] != 0 || typeBloc[BlocDe(c)] == TypeBloc.Entree) continue;
                int dm = -1;
                for (int d = 0; d < 4 && dm < 0; d++) if (EstMur(BordCellule(0, c, d))) dm = d;
                if (dm < 0) continue;
                m_Occupe[no] = 31;
                AjouterDecor(DecorTable, 0, c, Dx[dm] * 0.45f, Dy[dm] * 0.45f, Angle(Dx[dm], Dy[dm]) + 90f);
                tables++;
            }
            // Bannières : un bloc sur deux, au rez, sur un mur d'enceinte sans torche.
            for (int b = 0; b < NbBlocs; b++)
            {
                int n = 0;
                for (int j = 0; j < BlocTaille; j++)
                    for (int i = 0; i < BlocTaille; i++)
                    {
                        int c = CelluleBloc(b, i, j);
                        if (!Praticable(c) || eau[c]) continue;
                        for (int d = 0; d < 4; d++)
                        {
                            if (BordCellule(0, c, d) != Bord.MurExterieur || TorcheSur(0, c, d) || c == portailRetour.cellule) continue;
                            if (n < m_Cand.Length) m_Cand[n++] = c * 4 + d;
                        }
                    }
                if (n == 0 || Entier(100) >= 50) continue;
                int v = m_Cand[Entier(n)], cc = v / 4, dd = v % 4;
                AjouterDecor(DecorBanniere, 0, cc, Dx[dd] * 1.5f, Dy[dd] * 1.5f, Angle(-Dx[dd], -Dy[dd]));
            }
            // Tonneaux flottants dans le bassin.
            for (int f = 0, essai = 0; f < 2 && essai < 20; essai++)
            {
                int c = CelluleBloc(blocBassin, Entier(3), Entier(3));
                if (m_Utilise[c] || m_Occupe[c] != 0 || escalierDe[c] >= 0) continue;
                m_Occupe[c] |= 1;
                AjouterDecor(DecorFlottant, 0, c, (Entier(9) - 4) * 0.2f, (Entier(9) - 4) * 0.2f, Entier(360));
                f++;
            }
            // Ossements sur chaque point d'apparition (les squelettes sortent de terre là).
            for (int i = 0; i < NbApparitions && nbDecors < MaxDecors; i++)
            {
                Pose p = apparitions[i];
                p.type = DecorOs;
                p.rotY = Entier(360);
                p.variante = (byte)Entier(256);
                decors[nbDecors++] = p;
            }
        }

        bool EscalierAuDessus(int c)
        {
            for (int i = 0; i < nbEscaliers; i++)
                if (!escaliers[i].bassin && escaliers[i].niveau >= 1 && (escaliers[i].bas == c || escaliers[i].haut == c)) return true;
            return false;
        }

        bool TorcheSur(int k, int c, int d)
        {
            for (int i = 0; i < nbTorches; i++)
                if (torches[i].niveau == k && torches[i].cellule == c && torches[i].type == TorcheMurale && torches[i].variante == d) return true;
            return false;
        }

        void AjouterDecor(byte type, int k, int c, float ox, float oz, float rot)
        {
            if (nbDecors >= MaxDecors) return;
            Pose p = PoseCellule(k, c, ox, oz, rot);
            p.type = type;
            p.variante = (byte)Entier(256);
            decors[nbDecors++] = p;
        }

        // ------------------------------------------------------------------ Outils publics
        /// Le bloc est-il couvert au niveau k (au moins 5 cellules sur 9 ont un plancher au-dessus) ? C'est là que
        /// le masquage de l'étage se déclenche.
        public bool Couvert(int b, int k)
        {
            if (k + 1 >= NbNiveaux) return false;
            int n = 0;
            for (int j = 0; j < BlocTaille; j++)
                for (int i = 0; i < BlocTaille; i++)
                    if (plein[(k + 1) * NbCellules + CelluleBloc(b, i, j)]) n++;
            return n >= 5;
        }

        /// Le bloc a-t-il un plancher au-dessus du niveau k ?
        public bool ADuHaut(int b, int k)
        {
            for (int kk = k + 1; kk < NbNiveaux; kk++)
                for (int j = 0; j < BlocTaille; j++)
                    for (int i = 0; i < BlocTaille; i++)
                        if (plein[kk * NbCellules + CelluleBloc(b, i, j)]) return true;
            return false;
        }

        public static int BlocDe(int c) { return c % Largeur / BlocTaille + c / Largeur / BlocTaille * BlocsX; }

        public static int CelluleBloc(int b, int i, int j)
        {
            return b % BlocsX * BlocTaille + i + (b / BlocsX * BlocTaille + j) * Largeur;
        }

        public static int Voisine(int c, int d)
        {
            int x = c % Largeur + Dx[d], y = c / Largeur + Dy[d];
            if (x < 0 || y < 0 || x >= Largeur || y >= Profondeur) return -1;
            return x + y * Largeur;
        }

        public static int DirX(int d) { return Dx[d]; }
        public static int DirY(int d) { return Dy[d]; }

        /// Pose au centre d'une cellule du niveau k (fond du bassin compris), décalée de (ox, oz) mètres.
        public Pose PoseCellule(int k, int c, float ox, float oz, float rot)
        {
            return new Pose
            {
                niveau = k, cellule = c,
                x = (c % Largeur + 0.5f) * Cellule + ox, y = HauteurSol(k * NbCellules + c), z = (c / Largeur + 0.5f) * Cellule + oz,
                rotY = rot
            };
        }

        /// Angle Y (degrés) d'une direction (dx, dz), 0 = +z.
        public static float Angle(float dx, float dz) { return (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI); }

        void Amorcer(int g, int essai)
        {
            uint h = (uint)g * 0x9E3779B9u ^ ((uint)essai + 1u) * 0x85EBCA6Bu;
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            m_Etat = h == 0 ? 0x6D2B79F5u : h;
        }

        uint Suivant()
        {
            uint x = m_Etat;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            m_Etat = x;
            return x;
        }

        int Entier(int max) { return max <= 1 ? 0 : (int)(Suivant() % (uint)max); }

        void CalculerEmpreinte()
        {
            uint h = 2166136261u;
            for (int i = 0; i < NbNoeuds; i++) { h = (h ^ (plein[i] ? 1u : 0u)) * 16777619u; h = (h ^ ouvert[i]) * 16777619u; h = (h ^ (uint)(escalierDe[i] + 1)) * 16777619u; }
            for (int i = 0; i < NbButins; i++) { h = (h ^ (uint)butins[i].Noeud) * 16777619u; h = (h ^ butins[i].type) * 16777619u; }
            for (int i = 0; i < NbApparitions; i++) { h = (h ^ (uint)(int)(apparitions[i].x * 100f)) * 16777619u; h = (h ^ (uint)(int)(apparitions[i].z * 100f)) * 16777619u; }
            for (int i = 0; i < nbDecors; i++) h = (h ^ decors[i].variante) * 16777619u;
            h = (h ^ (uint)cheminCritiqueDm) * 16777619u;
            empreinte = h;
        }
    }
}
