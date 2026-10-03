using System;
using System.Collections.Generic;
using System.Text;

// Générateur de PLAN du donjon « terrasses étagées » (02/10/2026), d'après le croquis de Quentin (Docs/da/brief-donjon.md,
// Version 2) et les dix plans de Docs/outils/donjon_plans.py (variantes v1 à v7, ici corrigées : escaliers à pente tenue,
// paliers, pièces cachées où la hauteur le permet). C# pur, sans UnityEngine : même graine = même plan sur toutes les
// machines (générateur entier maison, aucune décision prise sur un flottant calculé), donc utilisable en réseau (NGO :
// seule la graine circule). La géométrie est construite à part (ConstructeurTerrasses).
//
// Repère : mètres ; x vers l'est, z vers le nord, y vers le haut. Origine au coin sud-ouest intérieur de l'enceinte ;
// le volume intérieur va de (0, 0) à (L, P). Le portail de retour est au milieu du mur sud (z = 0), l'arrivée devant lui.
// Les terrasses sont adossées au fond (nord) ou aux côtés.
//
// Le plan est d'abord une GRILLE de colonnes de 1 m (marge de 10 m autour du volume pour l'enceinte et les pièces
// cachées derrière elle). Chaque colonne est pleine du fond jusqu'à `sol`, vide au-dessus ; une colonne « cavité »
// (pièce cachée, passage d'arche) a en plus un plein haut de `plafond` à `sommet` (la dalle de la terrasse, le linteau).
// Jamais de surplomb : un sol est toujours plein jusqu'en bas, sauf au-dessus d'une pièce cachée fermée sur ses côtés.
// Faces de murs, parapets, torches, déclencheurs et contrôles se déduisent de cette grille.
namespace Deathless.Donjon.Terrasses
{
    public enum Dir : byte { Nord = 0, Est = 1, Sud = 2, Ouest = 3 }
    public enum GenreCellule : byte { Hors, Sol, Massif, Escalier, Cavite, Passage, Ponton }
    public enum GenreFace : byte { Enceinte, Soutenement, Massif, Piece, Linteau }
    public enum Emplacement : byte { SousTerrasse, DerriereEnceinte, DansMassif }
    public enum GenrePiece : byte { Libre, Verrouillee, Secrete }
    /// Serrure d'une pièce verrouillée : clé de bronze, d'argent ou d'or achetée au village (mécano), ou serrure simple
    /// seulement crochetable (kit de crochetage). Toutes se crochètent, de plus en plus difficilement {à confirmer}.
    public enum Serrure : byte { Aucune, Bronze, Argent, Or, Crochetable }
    public enum GenreDeclencheur : byte { Aucun, PlaqueSol, BoutonMural }
    public enum TypeCoffre : byte { GrandCoffre, Coffre }
    public enum TypeMonstre : byte { Sbire, Guerrier, Voleur, Mage }

    [Serializable]
    public class ParametresTerrasses
    {
        /// 0 : tirée par la graine ; 1 à 8 : variante imposée (voir PlanTerrasses.NomsVariantes ; 8 : ponton suspendu).
        public int variante = 0;
        /// 0 : tiré par la graine (2 ou 3, rez compris) ; 2 ou 3 : imposé (une variante incompatible l'emporte).
        public int niveaux = 0;
        public float probaDeuxNiveaux = 0.3f;
        /// Échelle du plan en pour cent (03/10/2026 : 135, soit 1,8 fois la surface d'avant) : cotes des terrasses, du volume,
        /// largeur des escaliers et du ponton, dégagement devant la salle. Entier : aucun flottant dans les décisions.
        public int echellePct = 135;
        public int nbApparitions = 20;
        public int coffresParTerrasse = 2;
        public float probaTroisiemePiece = 0.35f;
        public float probaPieceVerrouillee = 0.5f;
        public float probaPieceSecrete = 0.4f;
        public float hauteurLibreMin = 4.5f;
        /// Naissance de la voûte au-dessus du plus haut sol.
        public float hauteurSousVoute = 5f;
        /// Flèche de la voûte (clé au-dessus de la naissance) : 10 % de la largeur, bornée à [min, max].
        public float flecheVouteMin = 3f, flecheVouteMax = 4.5f;
        public float espacementPiliersMin = 10f;
        public float espacementApparitionsMin = 5f;
        public float distanceArriveeApparitions = 16f;
        public float espacementTorches = 7f;
        public int maxTorchesAllumees = 28;
        public int maxEssais = 40;

        public ParametresTerrasses Copie() { return (ParametresTerrasses)MemberwiseClone(); }
    }

    public struct Pose
    {
        public float x, y, z, rotY;
        public int niveau;
        public Pose(float x, float y, float z, float rotY, int niveau) { this.x = x; this.y = y; this.z = z; this.rotY = rotY; this.niveau = niveau; }
        public override string ToString() { return "(" + x.ToString("0.0") + " ; " + y.ToString("0.0") + " ; " + z.ToString("0.0") + ")"; }
    }

    /// Rectangle de cellules entières, en mètres : [x0, x1[ × [z0, z1[.
    public struct RectM
    {
        public int x0, z0, x1, z1;
        public RectM(int x0, int z0, int x1, int z1) { this.x0 = Math.Min(x0, x1); this.z0 = Math.Min(z0, z1); this.x1 = Math.Max(x0, x1); this.z1 = Math.Max(z0, z1); }
        public int Largeur => x1 - x0;
        public int Profondeur => z1 - z0;
        public float CentreX => (x0 + x1) * 0.5f;
        public float CentreZ => (z0 + z1) * 0.5f;
        public bool Contient(float x, float z) { return x >= x0 && x < x1 && z >= z0 && z < z1; }
        public override string ToString() { return "[" + x0 + "," + z0 + " → " + x1 + "," + z1 + "]"; }
    }

    public struct Cellule
    {
        public GenreCellule genre;
        public float sol, plafond, sommet;
        public short terrasse, escalier, piece;
        public bool obstacle;
        public bool AUnHaut => plafond < PlanTerrasses.Infini;
    }

    public sealed class Terrasse
    {
        public int index, niveau;
        public RectM r;
        public float Hauteur => niveau * PlanTerrasses.HauteurNiveau;
        public string role;
    }

    /// Escalier droit plein (massif dessous), 12 marches de 0,30 × 0,50 m par niveau (volée de 6 m), palier de 2 m entre
    /// deux volées.
    public sealed class Escalier
    {
        public int index;
        public RectM r;
        public Dir dir;                 // sens de la montée
        public int niveauBas, niveauHaut;
        public float Base => niveauBas * PlanTerrasses.HauteurNiveau;
        public float Sommet => niveauHaut * PlanTerrasses.HauteurNiveau;
        public int Volees => niveauHaut - niveauBas;
        public float Longueur => Volees * PlanTerrasses.Volee + (Volees - 1) * PlanTerrasses.Palier;
        public float Largeur => dir == Dir.Nord || dir == Dir.Sud ? r.Largeur : r.Profondeur;

        /// Distance parcourue depuis le pied de l'escalier jusqu'au point (x, z).
        public float Abscisse(float x, float z)
        {
            switch (dir)
            {
                case Dir.Nord: return z - r.z0;
                case Dir.Sud: return r.z1 - z;
                case Dir.Est: return x - r.x0;
                default: return r.x1 - x;
            }
        }

        /// Hauteur de la surface de marche (pente moyenne) à l'abscisse s.
        public float HauteurA(float s)
        {
            float h = Base;
            for (int v = 0; v < Volees; v++)
            {
                float d = s - v * (PlanTerrasses.Volee + PlanTerrasses.Palier);
                if (d <= 0f) return h;
                h += Math.Min(d, PlanTerrasses.Volee) * (PlanTerrasses.HauteurNiveau / PlanTerrasses.Volee);
                if (d <= PlanTerrasses.Volee) return h;
            }
            return Sommet;
        }
    }

    /// Arche (ouverture sans vantail, sauf pièce verrouillée ou secrète) : centre au nu du mur, côté salle ; dir pointe hors
    /// de la pièce, vers le sol de devant.
    public struct Arche
    {
        public float x, z, sol;
        public Dir dir;
        public float largeur, hauteur;
    }

    /// Ponton suspendu (variante 8) : passerelle de bois de `r` (4 m de large à l'échelle 135 %) entre deux terrasses de même niveau, au-dessus
    /// du rez ; garde-corps sur toute la longueur, chaînes pendues à la voûte. Dans la grille, ses cellules sont du genre
    /// Ponton : sol du rez dessous (on passe sous le tablier), plein haut de `Dessous` à `Hauteur` (le tablier).
    public sealed class Ponton
    {
        public RectM r;
        public Dir axe;                 // Est : il franchit d'ouest en est ; Nord : du sud au nord
        public int niveau;
        public float Hauteur => niveau * PlanTerrasses.HauteurNiveau;
        public float Dessous => Hauteur - PlanTerrasses.EpaisseurPonton;
        public float Longueur => axe == Dir.Est || axe == Dir.Ouest ? r.Largeur : r.Profondeur;
    }

    public sealed class PieceCachee
    {
        public int index;
        public RectM r;                 // intérieur
        public RectM passage;           // les 3 cellules de l'arche, à travers le mur de 1 m
        public float sol, plafond;
        public Arche arche;
        public Emplacement emplacement;
        public GenrePiece genre;
        public Serrure serrure;
        public int declencheur = -1;    // index dans PlanTerrasses.declencheurs (pièce secrète)
        public string nom;
        public int Niveau => (int)Math.Round(sol / PlanTerrasses.HauteurNiveau);
    }

    /// Mécanisme qui ouvre une pièce secrète : plaque de pression au sol, bouton mural discret. Toujours posé sur un lieu
    /// atteignable sans la pièce (vérifié).
    public sealed class Declencheur
    {
        public int index;
        public GenreDeclencheur genre;
        public Pose pose;               // plaque : centre au sol ; bouton : au nu du mur, rotY vers la salle
        public int cible;               // index de la pièce
    }

    public sealed class Pilier
    {
        public float x, z, rayon, bas, haut;
        public bool adosse;             // pilastre engagé dans un mur (nervure de la voûte, mur de soutènement)
        public Dir dir;                 // pilastre : direction vers la salle
    }

    /// Face verticale de mur, sur une ligne de la grille ; dir : normale, vers le vide.
    public struct Face
    {
        public float x0, z0, x1, z1;
        public Dir dir;
        public float y0, y1, solDevant;
        public GenreFace genre;
        public bool devantEscalier;
        public int piece;               // pièce dont c'est un mur intérieur (-1 sinon)
        public float Longueur => Math.Abs(x1 - x0) + Math.Abs(z1 - z0);
    }

    /// Parapet de 0,9 m posé sur le bord d'un sol qui domine un sol plus bas d'au moins 1,5 m (terrasse, escalier).
    public struct Parapet
    {
        public float x0, z0, x1, z1;
        public Dir dir;                 // vers le vide
        public float y;                 // pied du parapet
        public bool bois;               // garde-corps de bois d'un ponton (pas un parapet de pierre)
    }

    public sealed class Coffre
    {
        public Pose pose;
        public TypeCoffre type;
        public int piece = -1;
    }

    public sealed class Torche
    {
        public Pose pose;               // au nu du mur ; rotY vers la salle
        public bool allumee;            // porte une lumière (les autres : flamme seule)
        public int piece = -1;
    }

    public sealed class Apparition
    {
        public Pose pose;
        public TypeMonstre type;
        public bool gardien;
    }

    public sealed partial class PlanTerrasses
    {
        // ------------------------------------------------------------------ Constantes
        public const float Infini = 1e6f;
        // Écart entre deux niveaux : 3,6 m depuis le 03/10/2026 (3 m avant ; hausse modeste, à la mesure du plan agrandi),
        // soit 12 marches de 0,30 m et une volée de 6 m (nombre entier de mètres : la grille est au mètre).
        public const int MarchesParNiveau = 12;
        public const float Marche = 0.3f, Giron = 0.5f;
        public const float HauteurNiveau = 3.6f;
        public const float Volee = MarchesParNiveau * Giron;           // 6 m par niveau
        public const float Palier = 2f;
        int LargeurEscalier;            // 4 m à l'échelle (S(4))
        int LargeurPonton;              // 3 m à l'échelle (S(3))
        int LibreAvant;                 // 13 m à l'échelle : dégagement de l'avant de la grande salle
        public const float ArcheLargeur = 3f, ArcheHauteur = 4.5f, ArcheNaissance = 3f;
        public const float EpaisseurDalle = 0.6f, HauteurPiece = 5f, FondSol = -0.6f;
        public const float HauteurParapet = 0.9f, ChuteParapet = 1.5f, MarcheMax = 0.65f, PassageMin = 2.2f;
        public const int Marge = 10;
        public const float RayonPilier = 0.85f, RayonPilastre = 0.7f;
        /// Tablier du ponton (planches et longerons) ; hauteur libre dessous tolérée (le rez passe sous le ponton).
        public const float EpaisseurPonton = 0.45f, HauteurSousPontonMin = 3.1f;
        /// Largeur minimale d'un escalier (contrôle) ; largeur posée : 4 m à l'échelle (5 m à 135 %).
        public const int LargeurEscalierMin = 4;
        public static readonly string[] NomsVariantes =
        {
            "", "Deux terrasses, couloir central", "Chaîne Lvl 1 puis Lvl 2", "Trois terrasses, estrade centrale",
            "Grande terrasse et estrade haute", "Deux niveaux, terrasses inégales", "Terrasses latérales en vis-à-vis",
            "Grande terrasse, estrade centrale haute", "Ponton suspendu entre deux Lvl 1"
        };
        static readonly int[] VariantesTroisNiveaux = { 1, 2, 3, 4, 6, 7, 8 };
        static readonly int[] VariantesDeuxNiveaux = { 1, 4, 5, 6 };
        static readonly int[] DX = { 0, 1, 0, -1 }, DZ = { 1, 0, -1, 0 };
        public static int Dx(Dir d) { return DX[(int)d]; }
        public static int Dz(Dir d) { return DZ[(int)d]; }
        public static Dir Oppose(Dir d) { return (Dir)(((int)d + 2) & 3); }
        public static float Angle(Dir d) { return 90f * (int)d; }

        // ------------------------------------------------------------------ Résultat
        public int Graine { get; private set; }
        public int Essai { get; private set; }
        public int Variante { get; private set; }
        public string NomVariante => NomsVariantes[Variante];
        public int Niveaux { get; private set; }
        public int L { get; private set; }      // largeur intérieure (x)
        public int P { get; private set; }      // profondeur intérieure (z)
        public int B { get; private set; }      // profondeur de la bande du fond
        public float Naissance { get; private set; }   // naissance de la voûte (haut des murs latéraux)
        public float Cle { get; private set; }         // clé de voûte
        public float HautMur { get; private set; }
        public ParametresTerrasses Prm { get; private set; }

        public readonly List<Terrasse> terrasses = new List<Terrasse>();
        public readonly List<RectM> massifs = new List<RectM>();
        public readonly List<Escalier> escaliers = new List<Escalier>();
        public readonly List<Ponton> pontons = new List<Ponton>();
        public readonly List<PieceCachee> pieces = new List<PieceCachee>();
        public readonly List<Declencheur> declencheurs = new List<Declencheur>();
        public readonly List<Pilier> piliers = new List<Pilier>();
        public readonly List<float> nervures = new List<float>();          // z des arcs doubleaux de la voûte
        public readonly List<Face> faces = new List<Face>();
        public readonly List<Parapet> parapets = new List<Parapet>();
        public readonly List<Coffre> coffres = new List<Coffre>();
        public readonly List<Torche> torches = new List<Torche>();
        public readonly List<Apparition> apparitions = new List<Apparition>();
        public readonly Pose[] joueurs = new Pose[4];
        public Pose arrivee, portail;
        public RectM zoneArrivee;
        /// Défauts relevés par Valider() au dernier essai (vide : plan conforme).
        public readonly List<string> defauts = new List<string>();

        // Grille
        public int NX { get; private set; }
        public int NZ { get; private set; }
        public Cellule[] cellules = new Cellule[0];

        Alea m_Alea;

        // ================================================================== Génération
        /// Génère le plan de la graine : essais successifs (déterministes) jusqu'au premier qui passe les contrôles.
        public bool Generer(int graine, ParametresTerrasses prm = null)
        {
            Prm = prm ?? new ParametresTerrasses();
            Graine = graine;
            int max = Math.Max(1, Prm.maxEssais);
            for (int e = 0; e < max; e++)
            {
                Essai = e;
                Construire();
                defauts.Clear();
                Valider(defauts);
                if (defauts.Count == 0) return true;
            }
            return false;
        }

        void Construire()
        {
            m_Alea = new Alea(Graine, Essai);
            terrasses.Clear(); massifs.Clear(); escaliers.Clear(); pontons.Clear(); pieces.Clear(); declencheurs.Clear(); piliers.Clear(); nervures.Clear();
            faces.Clear(); parapets.Clear(); coffres.Clear(); torches.Clear(); apparitions.Clear();

            int niv = Prm.niveaux == 2 || Prm.niveaux == 3 ? Prm.niveaux : (m_Alea.Proba(Prm.probaDeuxNiveaux) ? 2 : 3);
            int v = Prm.variante;
            if (v < 1 || v > 8) v = niv == 2 ? VariantesDeuxNiveaux[m_Alea.Entier(VariantesDeuxNiveaux.Length)] : VariantesTroisNiveaux[m_Alea.Entier(VariantesTroisNiveaux.Length)];
            if (v == 5) niv = 2;
            if (v == 2 || v == 3 || v == 7 || v == 8) niv = 3;
            Variante = v;
            LargeurEscalier = S(4); LargeurPonton = S(3); LibreAvant = S(13);
            L = C(32, 34, 36, 38, 40);
            P = C(30, 32, 34, 36);
            B = C(12, 13, 14, 15);
            bool deux = niv == 2;
            switch (v)
            {
                case 1: V1(deux); break;
                case 2: V2(); break;
                case 3: V3(); break;
                case 4: V4(deux); break;
                case 5: V5(); break;
                case 6: V6(deux); break;
                case 8: V8(); break;
                default: V7(); break;
            }
            int nmax = 0;
            foreach (var t in terrasses) nmax = Math.Max(nmax, t.niveau);
            Niveaux = nmax + 1;
            for (int i = 0; i < terrasses.Count; i++)
            {
                terrasses[i].index = i;
                terrasses[i].role = terrasses[i].niveau == 2 ? "Trésor" : "Armurerie";
            }
            for (int i = 0; i < escaliers.Count; i++) escaliers[i].index = i;
            Naissance = nmax * HauteurNiveau + Prm.hauteurSousVoute;
            Cle = Naissance + Math.Min(Prm.flecheVouteMax, Math.Max(Prm.flecheVouteMin, 0.1f * L));
            HautMur = Cle + 0.6f;

            Rasteriser();
            PlacerArrivee();
            PlacerPieces();
            CalculerFaces();
            CalculerParapets();
            PlacerNervures();
            PlacerPiliers();
            AttribuerPieces();
            PlacerCoffres();
            PlacerTorches();
            PlacerApparitions();
        }

        /// Cote de référence (plans de donjon_plans.py) mise à l'échelle du plan, en entiers.
        int S(int v) { return (v * Math.Max(50, Prm.echellePct) + 50) / 100; }
        /// Tirage d'une cote de référence, mise à l'échelle.
        int C(params int[] v) { return S(m_Alea.Choix(v)); }

        // ------------------------------------------------------------------ Variantes (cotes à la manière de donjon_plans.py :
        // x vers l'est, y depuis le FOND (mur nord) ; converties en z = P - y)
        void AjTerrasse(int x0, int y0, int x1, int y1, int niveau)
        {
            terrasses.Add(new Terrasse { r = new RectM(x0, P - y1, x1, P - y0), niveau = niveau });
        }

        void AjMassif(int x0, int y0, int x1, int y1)
        {
            if (x1 - x0 >= 2 && y1 - y0 >= 2) massifs.Add(new RectM(x0, P - y1, x1, P - y0));
        }

        static int LongueurEscalier(int volees) { return (int)Math.Round(volees * Volee + (volees - 1) * Palier); }

        /// Escalier de face qui monte vers le fond jusqu'au bord avant d'une terrasse (y = yFace). Il s'encastre dans la
        /// terrasse s'il mangerait sinon l'avant de la grande salle (LibreAvant), en laissant au moins 4 m de terrasse au-dessus.
        int AjEscalierFace(int xa, int largeur, int yFace, int yFond, int nb, int nh)
        {
            int len = LongueurEscalier(nh - nb);
            int e = Math.Max(0, yFace + len - (P - LibreAvant));
            int maxE = yFace - yFond - 4;
            if (e > maxE) e = Math.Max(0, maxE);
            if (e > len) e = len;           // le pied reste devant la terrasse
            int haut = yFace - e;
            AjEscalier(xa, haut, largeur, nb, nh);
            return haut;
        }

        /// Escalier de face dont le haut est en y = yHaut (montée vers le fond).
        void AjEscalier(int xa, int yHaut, int largeur, int nb, int nh)
        {
            int len = LongueurEscalier(nh - nb);
            escaliers.Add(new Escalier { r = new RectM(xa, P - (yHaut + len), xa + largeur, P - yHaut), dir = Dir.Nord, niveauBas = nb, niveauHaut = nh });
        }

        /// Escalier latéral contre le mur du fond (y de y0 à y0 + 4), qui monte vers la terrasse dont le bord est en x = xBord ;
        /// versEst : la terrasse haute est à l'est.
        void AjEscalierLateral(int xBord, bool versEst, int y0, int nb, int nh)
        {
            int len = LongueurEscalier(nh - nb);
            var r = versEst ? new RectM(xBord - len, P - (y0 + LargeurEscalier), xBord, P - y0) : new RectM(xBord, P - (y0 + LargeurEscalier), xBord + len, P - y0);
            escaliers.Add(new Escalier { r = r, dir = versEst ? Dir.Est : Dir.Ouest, niveauBas = nb, niveauHaut = nh });
        }

        /// v1 : deux terrasses côte à côte (Lvl 1 et Lvl 2), couloir central fermé par un massif, escaliers le long des murs.
        void V1(bool deux)
        {
            int wl = C(12, 13, 14), wr = C(12, 13, 14);
            int nl, nr;
            if (deux) { nl = nr = 1; }
            else if (m_Alea.Proba(0.5f)) { nl = 2; nr = 1; }
            else { nl = 1; nr = 2; }
            AjTerrasse(0, 0, wl, B, nl);
            AjTerrasse(L - wr, 0, L, B, nr);
            AjEscalierFace(0, LargeurEscalier, B, 0, 0, nl);
            AjEscalierFace(L - LargeurEscalier, LargeurEscalier, B, 0, 0, nr);
            AjMassif(wl, 0, L - wr, B - S(3));
        }

        /// v2 : chaîne. Lvl 1 d'un côté (escalier depuis le rez), Lvl 2 de l'autre, atteint depuis le Lvl 1 par un escalier
        /// latéral posé sur une bande Lvl 1 le long du mur du fond ; couloir du rez entre les deux, sous la bande.
        void V2()
        {
            int wa = C(14, 15, 16), wb = C(10, 11, 12);
            bool est = m_Alea.Proba(0.5f);
            int sd = S(6);
            int db = B - C(0, 1, 2);
            bool escMur = m_Alea.Proba(0.5f);
            if (est)
            {
                AjTerrasse(L - wa, 0, L, B, 1);
                AjTerrasse(0, 0, wb, db, 2);
                if (L - wa > wb) AjTerrasse(wb, 0, L - wa, sd, 1);
                AjEscalierFace(escMur ? L - LargeurEscalier : L - wa, LargeurEscalier, B, 0, 0, 1);
                AjEscalierLateral(wb, false, 0, 1, 2);
            }
            else
            {
                AjTerrasse(0, 0, wa, B, 1);
                AjTerrasse(L - wb, 0, L, db, 2);
                if (L - wb > wa) AjTerrasse(wa, 0, L - wb, sd, 1);
                AjEscalierFace(escMur ? 0 : wa - LargeurEscalier, LargeurEscalier, B, 0, 0, 1);
                AjEscalierLateral(L - wb, true, 0, 1, 2);
            }
        }

        /// v3 : deux Lvl 1 aux extrémités, un Lvl 2 au centre atteint depuis les deux Lvl 1 (boucle).
        void V3()
        {
            int e = C(10, 11, 12);
            while (L - 2 * e < S(10)) e--;
            int b2 = B - C(0, 2);
            AjTerrasse(0, 0, e, B, 1);
            AjTerrasse(e, 0, L - e, b2, 2);
            AjTerrasse(L - e, 0, L, B, 1);
            AjEscalierFace(0, LargeurEscalier, B, 0, 0, 1);
            AjEscalierFace(L - LargeurEscalier, LargeurEscalier, B, 0, 0, 1);
            AjEscalierLateral(e, true, 0, 1, 2);
            AjEscalierLateral(L - e, false, 0, 1, 2);
        }

        /// v4 : grande terrasse Lvl 1 sur tout le fond, estrade Lvl 2 dans un angle ; large escalier central.
        void V4(bool deux)
        {
            bool est = m_Alea.Proba(0.5f);
            int w2 = C(12, 13, 14), d2 = C(7, 8);
            AjTerrasse(0, 0, L, B, 1);
            AjEscalierFace(L / 2 - S(6) / 2, S(6), B, 0, 0, 1);
            if (deux) return;
            if (est) { AjTerrasse(L - w2, 0, L, d2, 2); AjEscalierLateral(L - w2, true, 0, 1, 2); }
            else { AjTerrasse(0, 0, w2, d2, 2); AjEscalierLateral(w2, false, 0, 1, 2); }
        }

        /// v5 : deux niveaux, deux terrasses Lvl 1 inégales, massif entre elles.
        void V5()
        {
            int wl = C(10, 12), wr = C(14, 16), b2 = B + S(2);
            if (m_Alea.Proba(0.5f))
            {
                AjTerrasse(0, 0, wl, B, 1); AjTerrasse(L - wr, 0, L, b2, 1);
                AjEscalierFace(0, LargeurEscalier, B, 0, 0, 1); AjEscalierFace(L - LargeurEscalier, LargeurEscalier, b2, 0, 0, 1);
                AjMassif(wl, 0, L - wr, B - S(2));
            }
            else
            {
                AjTerrasse(0, 0, wr, b2, 1); AjTerrasse(L - wl, 0, L, B, 1);
                AjEscalierFace(0, LargeurEscalier, b2, 0, 0, 1); AjEscalierFace(L - LargeurEscalier, LargeurEscalier, B, 0, 0, 1);
                AjMassif(wr, 0, L - wl, B - S(2));
            }
        }

        /// v6 : terrasses le long des murs est et ouest, en vis-à-vis ; le fond reste au rez.
        void V6(bool deux)
        {
            int d = C(10, 11, 12), h = C(14, 16);
            int n1 = 1, n2 = 1;
            if (!deux) { if (m_Alea.Proba(0.5f)) n1 = 2; else n2 = 2; }
            AjTerrasse(0, 0, d, h, n1);
            AjTerrasse(L - d, 0, L, h, n2);
            AjEscalierFace(0, LargeurEscalier, h, 0, 0, n1);
            AjEscalierFace(L - LargeurEscalier, LargeurEscalier, h, 0, 0, n2);
        }

        /// v7 : grande terrasse Lvl 1, estrade centrale Lvl 2 ; grand escalier central en deux volées séparées d'un palier.
        void V7()
        {
            int c = C(12, 14);
            AjTerrasse(0, 0, L, B, 1);
            int yt1 = AjEscalierFace(L / 2 - S(6) / 2, S(6), B, 0, 0, 1);
            int bas2 = yt1 - 2, haut2 = bas2 - LongueurEscalier(1);
            int d2 = haut2 + 2;
            AjTerrasse((L - c) / 2, 0, (L + c) / 2, d2, 2);
            AjEscalier(L / 2 - LargeurEscalier / 2, haut2, LargeurEscalier, 1, 2);
        }

        /// v8 (croquis de Quentin du 03/10/2026) : à l'ouest, un grand Lvl 1 en L (bande au-dessus de la grande salle et
        /// couloir central qui monte vers le fond) qui mène au Lvl 2 (angle nord-ouest) ; au nord-est, un petit Lvl 1 ;
        /// entre les deux, un ponton suspendu au-dessus du rez. Escaliers : rez → grand Lvl 1 le long du mur ouest, grand
        /// Lvl 1 → Lvl 2 contre le couloir (encastré dans le Lvl 2), rez → petit Lvl 1 le long du mur est. Boucle : on
        /// monte d'un côté, on traverse le ponton, on redescend de l'autre. Une fois sur deux en miroir (grand côté à l'est).
        void V8()
        {
            L = C(36, 38, 40);
            P = C(32, 34, 36);
            bool miroir = m_Alea.Proba(0.5f);
            int w2 = C(11, 12, 13), d2 = C(10, 11, 12);
            int wc = C(4, 5), bl = C(8, 9, 10);
            int lp = C(10, 12, 14);
            int ws = Math.Max(S(9), Math.Min(S(12), L - w2 - wc - lp)), ds = C(12, 13, 14);
            int yp = C(3, 4, 5);
            // abscisse ouest de l'intervalle [x0, x1[ du plan de base (miroir est-ouest)
            Func<int, int, int> X = (x0, x1) => miroir ? L - x1 : x0;
            // grand Lvl 1 (couloir et bande) et Lvl 2 dans l'angle
            AjTerrasse(X(0, w2), 0, X(0, w2) + w2, d2, 2);
            AjTerrasse(X(w2, w2 + wc), 0, X(w2, w2 + wc) + wc, d2, 1);
            AjTerrasse(X(0, w2 + wc), d2, X(0, w2 + wc) + w2 + wc, d2 + bl, 1);
            // petit Lvl 1
            AjTerrasse(X(L - ws, L), 0, X(L - ws, L) + ws, ds, 1);
            // escaliers
            AjEscalierFace(X(0, LargeurEscalier), LargeurEscalier, d2 + bl, d2, 0, 1);
            AjEscalier(X(w2 - LargeurEscalier, w2), Math.Min(d2, d2 + bl - LongueurEscalier(1) - 2), LargeurEscalier, 1, 2);
            AjEscalierFace(X(L - LargeurEscalier, L), LargeurEscalier, ds, 0, 0, 1);
            // ponton : du bord du couloir au bord du petit Lvl 1
            int lg = L - ws - w2 - wc, xa = X(w2 + wc, L - ws);
            pontons.Add(new Ponton { r = new RectM(xa, P - (yp + LargeurPonton), xa + lg, P - yp), axe = Dir.Est, niveau = 1 });
        }

        // ================================================================== Grille
        public int Index(int i, int j) { return j * NX + i; }
        public bool DansGrille(int i, int j) { return i >= 0 && j >= 0 && i < NX && j < NZ; }
        public bool DansVolume(int i, int j) { int x = i - Marge, z = j - Marge; return x >= 0 && z >= 0 && x < L && z < P; }
        public float CentreX(int i) { return i - Marge + 0.5f; }
        public float CentreZ(int j) { return j - Marge + 0.5f; }
        public int CelluleI(float x) { return (int)Math.Floor(x) + Marge; }
        public int CelluleJ(float z) { return (int)Math.Floor(z) + Marge; }
        public Cellule CelluleEn(float x, float z) { int i = CelluleI(x), j = CelluleJ(z); return DansGrille(i, j) ? cellules[Index(i, j)] : HorsGrille(); }
        Cellule HorsGrille() { return new Cellule { genre = GenreCellule.Hors, sol = HautMur, plafond = Infini, sommet = HautMur, terrasse = -1, escalier = -1, piece = -1 }; }

        /// Hauteur de l'intrados de la voûte (arc segmentaire d'est en ouest) à l'abscisse x.
        public float Voute(float x)
        {
            float f = Cle - Naissance, demi = L * 0.5f;
            float R = (demi * demi + f * f) / (2f * f);
            float dx = Math.Min(demi, Math.Abs(x - demi));
            return Cle - R + (float)Math.Sqrt(R * R - dx * dx);
        }

        void Rasteriser()
        {
            NX = L + 2 * Marge; NZ = P + 2 * Marge;
            if (cellules.Length != NX * NZ) cellules = new Cellule[NX * NZ];
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    bool dedans = DansVolume(i, j);
                    cellules[Index(i, j)] = new Cellule
                    {
                        genre = dedans ? GenreCellule.Sol : GenreCellule.Hors,
                        sol = dedans ? 0f : HautMur, plafond = Infini, sommet = 0f,
                        terrasse = -1, escalier = -1, piece = -1
                    };
                }
            var ordre = new List<Terrasse>(terrasses);
            ordre.Sort((a, b) => a.niveau != b.niveau ? a.niveau.CompareTo(b.niveau) : a.index.CompareTo(b.index));
            foreach (var t in ordre)
                Remplir(t.r, (ref Cellule c) => { if (c.genre == GenreCellule.Sol) { c.sol = t.Hauteur; c.terrasse = (short)t.index; } });
            foreach (var m in massifs)
                Remplir(m, (ref Cellule c) => { c.genre = GenreCellule.Massif; c.sol = HautMur; c.terrasse = -1; });
            foreach (var e in escaliers)
            {
                var es = e;
                Remplir(e.r, (ref Cellule c) => { c.genre = GenreCellule.Escalier; c.sol = es.Base; c.escalier = (short)es.index; });
            }
            foreach (var po in pontons)
            {
                var pp = po;
                Remplir(po.r, (ref Cellule c) => { c.genre = GenreCellule.Ponton; c.sol = 0f; c.plafond = pp.Dessous; c.sommet = pp.Hauteur; c.terrasse = -1; });
            }
        }

        delegate void Action2(ref Cellule c);
        void Remplir(RectM r, Action2 a)
        {
            for (int z = r.z0; z < r.z1; z++)
                for (int x = r.x0; x < r.x1; x++)
                {
                    int i = x + Marge, j = z + Marge;
                    if (!DansGrille(i, j)) continue;
                    a(ref cellules[Index(i, j)]);
                }
        }

        /// Hauteur de la surface de marche d'une cellule d'escalier (en son centre).
        public float HauteurEscalier(int i, int j)
        {
            var c = cellules[Index(i, j)];
            if (c.escalier < 0) return c.sol;
            var e = escaliers[c.escalier];
            return e.HauteurA(e.Abscisse(CentreX(i), CentreZ(j)));
        }

        // ================================================================== Arrivée et portail
        void PlacerArrivee()
        {
            float cx = L * 0.5f;
            zoneArrivee = new RectM(L / 2 - 3, 1, L / 2 + 3, 6);
            arrivee = new Pose(cx, 0f, 2.2f, 0f, 0);
            portail = new Pose(cx, 0f, 0f, 0f, 0);
            float[] ox = { -2.25f, -0.75f, 0.75f, 2.25f };
            for (int k = 0; k < 4; k++) joueurs[k] = new Pose(cx + ox[k], 0f, 3.4f, 0f, 0);
        }

        bool DansZoneArrivee(float x, float z, float marge)
        {
            return x > zoneArrivee.x0 - marge && x < zoneArrivee.x1 + marge && z > zoneArrivee.z0 - marge - 1f && z < zoneArrivee.z1 + marge;
        }

        // ================================================================== Pièces cachées
        struct Candidat { public int i0, j0, w, dd; public Dir d; public Emplacement emp; public float f, hote; }

        void Locale(Dir d, int i0, int j0, int u, int v, out int i, out int j)
        {
            switch (d)
            {
                case Dir.Sud: i = i0 + u; j = j0 + v; break;          // devant au sud, la pièce s'étend vers le nord
                case Dir.Nord: i = i0 + u; j = j0 - v; break;
                case Dir.Ouest: i = i0 + v; j = j0 + u; break;
                default: i = i0 - v; j = j0 + u; break;               // Est
            }
        }

        bool EstHote(Cellule c, GenreCellule genre, float sol)
        {
            return c.genre == genre && !c.obstacle && c.piece < 0 && Math.Abs(c.sol - sol) < 0.01f;
        }

        bool TesterPiece(Dir d, int i0, int j0, int w, int dd, out Candidat cand)
        {
            cand = default(Candidat);
            int a = (w - 3) / 2;
            int fi, fj;
            Locale(d, i0, j0, a + 1, -1, out fi, out fj);
            if (!DansGrille(fi, fj) || !DansVolume(fi, fj)) return false;
            var fc = cellules[Index(fi, fj)];
            if (fc.genre != GenreCellule.Sol || fc.obstacle) return false;
            float f = fc.sol;
            // sol de devant : 3 × 2 cellules praticables au même niveau, hors de la zone d'arrivée
            for (int u = a; u < a + 3; u++)
                for (int v = -2; v <= -1; v++)
                {
                    int i, j; Locale(d, i0, j0, u, v, out i, out j);
                    if (!DansGrille(i, j) || !DansVolume(i, j)) return false;
                    var c = cellules[Index(i, j)];
                    if (c.genre != GenreCellule.Sol || c.obstacle || Math.Abs(c.sol - f) > 0.01f) return false;
                    if (DansZoneArrivee(CentreX(i), CentreZ(j), 3f)) return false;
                }
            int pi, pj; Locale(d, i0, j0, a + 1, 0, out pi, out pj);
            if (!DansGrille(pi, pj)) return false;
            var hc = cellules[Index(pi, pj)];
            GenreCellule genre = hc.genre;
            float hote = hc.sol;
            Emplacement emp;
            if (genre == GenreCellule.Sol) { if (hote - f < Prm.hauteurLibreMin + EpaisseurDalle + 0.01f) return false; emp = Emplacement.SousTerrasse; }
            else if (genre == GenreCellule.Hors)
            {
                // derrière l'enceinte : jamais au sud (arrivée), et loin de l'avant de la salle
                if (d == Dir.Nord || CentreZ(fj) < zoneArrivee.z1 + 8) return false;
                emp = Emplacement.DerriereEnceinte;
            }
            else if (genre == GenreCellule.Massif) emp = Emplacement.DansMassif;
            else return false;
            // hôte plein : passage, intérieur et anneau de 1 m (sauf l'avant, qui est le mur percé de l'arche)
            for (int u = -1; u <= w; u++)
                for (int v = 0; v <= dd + 1; v++)
                {
                    int i, j; Locale(d, i0, j0, u, v, out i, out j);
                    if (!DansGrille(i, j)) return false;
                    if (!EstHote(cellules[Index(i, j)], genre, hote)) return false;
                    if (genre == GenreCellule.Hors && (i < 1 || j < 1 || i >= NX - 1 || j >= NZ - 1)) return false;
                }
            cand = new Candidat { i0 = i0, j0 = j0, w = w, dd = dd, d = d, emp = emp, f = f, hote = hote };
            return true;
        }

        void PlacerPieces()
        {
            int nb = 2 + (m_Alea.Proba(Prm.probaTroisiemePiece) ? 1 : 0);
            var cands = new List<Candidat>(256);
            var usages = new int[3];
            for (int k = 0; k < nb; k++)
            {
                int w = m_Alea.Choix(6, 7, 8), dd = m_Alea.Choix(5, 6);
                cands.Clear();
                for (int d = 0; d < 4; d++)
                    for (int j0 = 0; j0 < NZ; j0++)
                        for (int i0 = 0; i0 < NX; i0++)
                        {
                            Candidat c;
                            if (!TesterPiece((Dir)d, i0, j0, w, dd, out c)) continue;
                            if (!LoinDesPieces(c)) continue;
                            cands.Add(c);
                        }
                if (cands.Count == 0) continue;
                // priorité : une arche dans un mur de soutènement (croquis), puis des emplacements variés
                int meilleur = int.MaxValue;
                foreach (var c in cands) meilleur = Math.Min(meilleur, Cout(c, usages));
                var retenus = cands.FindAll(c => Cout(c, usages) == meilleur);
                var choix = retenus[m_Alea.Entier(retenus.Count)];
                usages[(int)choix.emp]++;
                AjouterPiece(choix);
            }
        }

        static int Cout(Candidat c, int[] usages)
        {
            int prio = c.emp == Emplacement.SousTerrasse ? 0 : c.emp == Emplacement.DansMassif ? 1 : 2;
            return usages[(int)c.emp] * 3 + prio;
        }

        void CentreCandidat(Candidat c, out float x, out float z)
        {
            int i, j; Locale(c.d, c.i0, c.j0, c.w / 2, c.dd / 2 + 1, out i, out j);
            x = CentreX(i); z = CentreZ(j);
        }

        bool LoinDesPieces(Candidat c)
        {
            float x, z; CentreCandidat(c, out x, out z);
            foreach (var p in pieces)
            {
                float dx = p.r.CentreX - x, dz = p.r.CentreZ - z;
                if (dx * dx + dz * dz < S(12) * S(12)) return false;
            }
            return true;
        }

        void AjouterPiece(Candidat c)
        {
            var p = new PieceCachee { index = pieces.Count, emplacement = c.emp, sol = c.f };
            // sous une terrasse haute (8,4 m), la pièce garde 6 m sous plafond : le reste est une dalle épaisse
            p.plafond = c.emp == Emplacement.SousTerrasse ? Math.Min(c.hote - EpaisseurDalle, c.f + HauteurPiece + 1f) : c.f + HauteurPiece;
            int a = (c.w - 3) / 2;
            int ia, ja, ib, jb;
            Locale(c.d, c.i0, c.j0, 0, 1, out ia, out ja); Locale(c.d, c.i0, c.j0, c.w - 1, c.dd, out ib, out jb);
            p.r = new RectM(Math.Min(ia, ib) - Marge, Math.Min(ja, jb) - Marge, Math.Max(ia, ib) - Marge + 1, Math.Max(ja, jb) - Marge + 1);
            Locale(c.d, c.i0, c.j0, a, 0, out ia, out ja); Locale(c.d, c.i0, c.j0, a + 2, 0, out ib, out jb);
            p.passage = new RectM(Math.Min(ia, ib) - Marge, Math.Min(ja, jb) - Marge, Math.Max(ia, ib) - Marge + 1, Math.Max(ja, jb) - Marge + 1);
            int pi, pj, fi, fj;
            Locale(c.d, c.i0, c.j0, a + 1, 0, out pi, out pj); Locale(c.d, c.i0, c.j0, a + 1, -1, out fi, out fj);
            Dir dehors = c.d;
            // nu du mur côté salle : frontière entre la cellule du passage et celle de devant
            p.arche = new Arche
            {
                x = (CentreX(pi) + CentreX(fi)) * 0.5f, z = (CentreZ(pj) + CentreZ(fj)) * 0.5f, sol = c.f,
                dir = dehors, largeur = ArcheLargeur, hauteur = ArcheHauteur
            };
            p.nom = c.emp == Emplacement.SousTerrasse ? "Crypte" : c.emp == Emplacement.DansMassif ? "Réserve" : "Passage";
            pieces.Add(p);
            for (int u = 0; u < c.w; u++)
                for (int v = 1; v <= c.dd; v++)
                {
                    int i, j; Locale(c.d, c.i0, c.j0, u, v, out i, out j);
                    Creuser(i, j, GenreCellule.Cavite, c.f, p.plafond, p.index);
                }
            for (int u = a; u < a + 3; u++)
            {
                int i, j; Locale(c.d, c.i0, c.j0, u, 0, out i, out j);
                Creuser(i, j, GenreCellule.Passage, c.f, c.f + ArcheHauteur, p.index);
            }
        }

        void Creuser(int i, int j, GenreCellule g, float sol, float plafond, int piece)
        {
            int k = Index(i, j);
            var c = cellules[k];
            c.sommet = c.sol;
            c.sol = sol; c.plafond = plafond; c.genre = g; c.piece = (short)piece;
            cellules[k] = c;
        }

        // ================================================================== Surfaces de marche et connexité
        /// Surface praticable s (0 : sol bas ; 1 : dessus d'une cavité) de la cellule k : hauteur et plafond au-dessus.
        public bool Surface(int i, int j, int s, out float h, out float haut)
        {
            h = 0f; haut = 0f;
            var c = cellules[Index(i, j)];
            if (s == 0)
            {
                if (c.obstacle) return false;
                switch (c.genre)
                {
                    case GenreCellule.Sol:
                    case GenreCellule.Cavite:
                    case GenreCellule.Passage:
                    case GenreCellule.Ponton:
                        h = c.sol; break;
                    case GenreCellule.Escalier:
                        h = HauteurEscalier(i, j); break;
                    default: return false;
                }
                haut = c.AUnHaut ? c.plafond : Voute(CentreX(i));
                return true;
            }
            if (!c.AUnHaut || c.sommet > HautMur - 0.5f) return false;
            h = c.sommet; haut = Voute(CentreX(i));
            return true;
        }

        /// Noeuds atteignables depuis l'arrivée (2 par cellule). portesFermees : les pièces verrouillées ou secrètes ne
        /// s'ouvrent pas.
        public bool[] Atteignables(bool portesFermees)
        {
            int n = NX * NZ;
            var vu = new bool[n * 2];
            var file = new Queue<int>();
            int i0 = CelluleI(arrivee.x), j0 = CelluleJ(arrivee.z);
            int dep = Index(i0, j0) * 2;
            vu[dep] = true; file.Enqueue(dep);
            while (file.Count > 0)
            {
                int nd = file.Dequeue();
                int k = nd >> 1, s = nd & 1, i = k % NX, j = k / NX;
                float ha, ta;
                if (!Surface(i, j, s, out ha, out ta)) continue;
                for (int d = 0; d < 4; d++)
                {
                    int ni = i + DX[d], nj = j + DZ[d];
                    if (!DansGrille(ni, nj)) continue;
                    for (int sb = 0; sb < 2; sb++)
                    {
                        int nn = Index(ni, nj) * 2 + sb;
                        if (vu[nn]) continue;
                        float hb, tb;
                        if (!Surface(ni, nj, sb, out hb, out tb)) continue;
                        if (portesFermees && sb == 0 && Fermee(cellules[Index(ni, nj)])) continue;
                        if (Math.Abs(ha - hb) > MarcheMax) continue;
                        if (Math.Min(ta, tb) - Math.Max(ha, hb) < PassageMin) continue;
                        vu[nn] = true; file.Enqueue(nn);
                    }
                }
            }
            return vu;
        }

        bool Fermee(Cellule c)
        {
            return c.piece >= 0 && (c.genre == GenreCellule.Cavite || c.genre == GenreCellule.Passage) && pieces[c.piece].genre != GenrePiece.Libre;
        }

        // ================================================================== Faces de murs
        void CalculerFaces()
        {
            var brut = new Dictionary<long, List<int>>();
            var cles = new List<FaceCle>();
            var index = new Dictionary<FaceCle, int>();
            float[] sa = new float[4], sv = new float[4];
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    var a = cellules[Index(i, j)];
                    for (int d = 0; d < 4; d++)
                    {
                        int ni = i + DX[d], nj = j + DZ[d];
                        if (!DansGrille(ni, nj)) continue;
                        var b = cellules[Index(ni, nj)];
                        if (b.genre == GenreCellule.Hors && !b.AUnHaut) continue;
                        if (b.genre == GenreCellule.Massif) continue;
                        // pleins de a : [Fond, sol] et [plafond, sommet] ; vides de b : [sol, plafond|haut] et [sommet, haut]
                        float hautB = DansVolume(ni, nj) ? HautMur : (b.AUnHaut ? b.plafond : b.sol);
                        int na = 0;
                        sa[na++] = FondSol; sa[na++] = a.sol;
                        if (a.AUnHaut) { sa[na++] = a.plafond; sa[na++] = a.sommet; }
                        int nv = 0;
                        sv[nv++] = b.sol; sv[nv++] = b.AUnHaut ? b.plafond : hautB;
                        if (b.AUnHaut && b.sommet < HautMur - 0.01f) { sv[nv++] = b.sommet; sv[nv++] = HautMur; }
                        for (int p = 0; p < na; p += 2)
                            for (int q = 0; q < nv; q += 2)
                            {
                                float y0 = Math.Max(sa[p], sv[q]), y1 = Math.Min(sa[p + 1], sv[q + 1]);
                                if (y1 - y0 < 0.05f) continue;
                                GenreFace g;
                                int piece = -1;
                                bool dansPiece = q == 0 && (b.genre == GenreCellule.Cavite || b.genre == GenreCellule.Passage);
                                if (dansPiece) { g = GenreFace.Piece; piece = b.piece; }
                                else if (p == 2) g = GenreFace.Linteau;
                                else if (a.genre == GenreCellule.Hors || (a.genre == GenreCellule.Passage && a.sommet >= HautMur - 0.01f)) g = GenreFace.Enceinte;
                                else if (a.genre == GenreCellule.Massif) g = GenreFace.Massif;
                                else g = GenreFace.Soutenement;
                                if (a.genre == GenreCellule.Cavite || a.genre == GenreCellule.Passage) { if (p == 0) continue; }
                                if (a.genre == GenreCellule.Ponton) continue;   // tablier de bois : construit à part
                                var cle = new FaceCle
                                {
                                    dir = (Dir)d, ligne = LigneFace(i, j, (Dir)d), y0 = Q(y0), y1 = Q(y1),
                                    sd = Q(q == 0 ? b.sol : b.sommet), genre = g, esc = q == 0 && b.genre == GenreCellule.Escalier, piece = piece
                                };
                                int id;
                                if (!index.TryGetValue(cle, out id)) { id = cles.Count; cles.Add(cle); index[cle] = id; brut[id] = new List<int>(); }
                                brut[id].Add(d == 0 || d == 2 ? i : j);
                            }
                    }
                }
            for (int id = 0; id < cles.Count; id++)
            {
                var cle = cles[id];
                var pos = brut[id];
                pos.Sort();
                int deb = pos[0], prec = pos[0];
                for (int n = 1; n <= pos.Count; n++)
                {
                    if (n < pos.Count && pos[n] == prec + 1) { prec = pos[n]; continue; }
                    AjouterFace(cle, deb, prec + 1);
                    if (n < pos.Count) { deb = prec = pos[n]; }
                }
            }
        }

        struct FaceCle : IEquatable<FaceCle>
        {
            public Dir dir; public int ligne, y0, y1, sd, piece; public GenreFace genre; public bool esc;
            public bool Equals(FaceCle o) { return dir == o.dir && ligne == o.ligne && y0 == o.y0 && y1 == o.y1 && sd == o.sd && genre == o.genre && esc == o.esc && piece == o.piece; }
            public override bool Equals(object o) { return o is FaceCle && Equals((FaceCle)o); }
            public override int GetHashCode() { unchecked { return ((((((int)dir * 397 ^ ligne) * 397 ^ y0) * 397 ^ y1) * 397 ^ sd) * 397 ^ (int)genre) * 31 + (esc ? 1 : 0) + piece * 7919; } }
        }

        static int Q(float y) { return (int)Math.Round(y * 100f); }

        /// Coordonnée (en cellules de grille) de la ligne qui sépare la cellule (i, j) de sa voisine en d.
        static int LigneFace(int i, int j, Dir d)
        {
            switch (d)
            {
                case Dir.Nord: return j + 1;
                case Dir.Sud: return j;
                case Dir.Est: return i + 1;
                default: return i;
            }
        }

        void AjouterFace(FaceCle c, int deb, int fin)
        {
            var f = new Face { dir = c.dir, y0 = c.y0 / 100f, y1 = c.y1 / 100f, solDevant = c.sd / 100f, genre = c.genre, devantEscalier = c.esc, piece = c.piece };
            float ligne = c.ligne - Marge;
            if (c.dir == Dir.Nord || c.dir == Dir.Sud) { f.z0 = f.z1 = ligne; f.x0 = deb - Marge; f.x1 = fin - Marge; }
            else { f.x0 = f.x1 = ligne; f.z0 = deb - Marge; f.z1 = fin - Marge; }
            faces.Add(f);
        }

        // ================================================================== Parapets
        /// Surface de marche la plus haute d'une cellule (pour les bords) ; false si c'est un mur.
        bool Dessus(int i, int j, out float h)
        {
            float t;
            if (Surface(i, j, 1, out h, out t)) return true;
            var c = cellules[Index(i, j)];
            if (c.genre == GenreCellule.Hors || c.genre == GenreCellule.Massif) { h = c.sol; return false; }
            if (c.obstacle) { h = c.sol; return true; }
            return Surface(i, j, 0, out h, out t);
        }

        void CalculerParapets()
        {
            var cles = new Dictionary<long, List<int>>();
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    if (!DansVolume(i, j)) continue;
                    float ha;
                    if (!Dessus(i, j, out ha)) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        int ni = i + DX[d], nj = j + DZ[d];
                        if (!DansGrille(ni, nj) || !DansVolume(ni, nj)) continue;
                        float hb;
                        if (!Dessus(ni, nj, out hb)) continue;
                        if (ha - hb < ChuteParapet) continue;
                        bool bois = cellules[Index(i, j)].genre == GenreCellule.Ponton;
                        long cle = ((long)d << 48) | (bois ? 1L << 47 : 0L) | ((long)LigneFace(i, j, (Dir)d) << 32) | (uint)Q(ha);
                        List<int> l;
                        if (!cles.TryGetValue(cle, out l)) cles[cle] = l = new List<int>();
                        l.Add(d == 0 || d == 2 ? i : j);
                    }
                }
            var tri = new List<long>(cles.Keys);
            tri.Sort();
            foreach (var cle in tri)
            {
                var pos = cles[cle];
                pos.Sort();
                Dir d = (Dir)(cle >> 48);
                bool bois = ((cle >> 47) & 1L) != 0;
                int ligne = (int)((cle >> 32) & 0xFFFF);
                float y = (int)(cle & 0xFFFFFFFF) / 100f;
                int deb = pos[0], prec = pos[0];
                for (int n = 1; n <= pos.Count; n++)
                {
                    if (n < pos.Count && pos[n] == prec + 1) { prec = pos[n]; continue; }
                    var pa = new Parapet { dir = d, y = y, bois = bois };
                    float l = ligne - Marge;
                    if (d == Dir.Nord || d == Dir.Sud) { pa.z0 = pa.z1 = l; pa.x0 = deb - Marge; pa.x1 = prec + 1 - Marge; }
                    else { pa.x0 = pa.x1 = l; pa.z0 = deb - Marge; pa.z1 = prec + 1 - Marge; }
                    parapets.Add(pa);
                    if (n < pos.Count) deb = prec = pos[n];
                }
            }
        }

        bool ParapetPres(float x, float z, float r)
        {
            foreach (var p in parapets)
            {
                float cx = Math.Max(p.x0, Math.Min(x, p.x1)), cz = Math.Max(p.z0, Math.Min(z, p.z1));
                if ((cx - x) * (cx - x) + (cz - z) * (cz - z) <= r * r) return true;
            }
            return false;
        }

        // ================================================================== Voûte, pilastres, piliers
        void PlacerNervures()
        {
            int nr = Math.Max(3, (P + 4) / 8);
            for (int k = 1; k < nr; k++)
            {
                int z = (P * k + nr / 2) / nr;
                nervures.Add(z);
                // pilastres des murs latéraux, sous la naissance de chaque arc doubleau
                Pilastre(0, z, Dir.Est);
                Pilastre(L - 1, z, Dir.Ouest);
            }
            // pilastres le long des murs de soutènement qui donnent sur la grande salle
            foreach (var f in faces)
            {
                if (f.genre != GenreFace.Soutenement || f.devantEscalier || f.solDevant > 0.01f || f.y0 > 0.01f) continue;
                float lg = f.Longueur;
                if (lg < 10f) continue;
                int n = (int)((lg - 2f) / 8f);
                for (int m = 0; m < n; m++)
                {
                    float s = (float)Math.Round(lg * (m + 0.5f) / n);
                    float x = f.x0 == f.x1 ? f.x0 : f.x0 + s, z = f.z0 == f.z1 ? f.z0 : f.z0 + s;
                    if (PresArche(x, z, 3.5f) || PresEscalier(x, z, 2.5f)) continue;
                    PoserPilastreFace(x, z, f.dir, 0f);
                }
            }
        }

        void Pilastre(int xc, int z, Dir d)
        {
            // les deux cellules de devant (de part et d'autre de z) doivent être un sol plein, pas un escalier
            int i = xc + Marge, j1 = z - 1 + Marge, j2 = z + Marge;
            if (!DansGrille(i, j2) || !DansGrille(i, j1)) return;
            var a = cellules[Index(i, j1)]; var b = cellules[Index(i, j2)];
            bool ok = a.genre == GenreCellule.Sol && b.genre == GenreCellule.Sol && !a.obstacle && !b.obstacle && Math.Abs(a.sol - b.sol) < 0.01f;
            float x = d == Dir.Est ? 0f : L;
            if (ok && PresArche(x, z, 3f)) ok = false;
            piliers.Add(new Pilier { x = x + Dx(d) * 0.3f, z = z, rayon = RayonPilastre, bas = ok ? a.sol : Naissance - 1.2f, haut = Naissance, adosse = true, dir = d });
            if (ok) { cellules[Index(i, j1)].obstacle = true; cellules[Index(i, j2)].obstacle = true; }
        }

        void PoserPilastreFace(float x, float z, Dir d, float bas)
        {
            // cellules de devant
            int i1, j1, i2, j2;
            if (d == Dir.Nord || d == Dir.Sud)
            {
                int j = CelluleJ(z + (d == Dir.Nord ? 0.5f : -0.5f));
                i1 = CelluleI(x - 0.5f); i2 = CelluleI(x + 0.5f); j1 = j2 = j;
            }
            else
            {
                int i = CelluleI(x + (d == Dir.Est ? 0.5f : -0.5f));
                j1 = CelluleJ(z - 0.5f); j2 = CelluleJ(z + 0.5f); i1 = i2 = i;
            }
            if (!DansGrille(i1, j1) || !DansGrille(i2, j2)) return;
            var a = cellules[Index(i1, j1)]; var b = cellules[Index(i2, j2)];
            if (a.genre != GenreCellule.Sol || b.genre != GenreCellule.Sol || a.obstacle || b.obstacle || a.sol > bas + 0.01f || b.sol > bas + 0.01f) return;
            if (DansZoneArrivee(x, z, 2f)) return;
            piliers.Add(new Pilier { x = x + Dx(d) * 0.3f, z = z + Dz(d) * 0.3f, rayon = RayonPilastre, bas = bas, haut = Voute(x), adosse = true, dir = d });
            cellules[Index(i1, j1)].obstacle = true; cellules[Index(i2, j2)].obstacle = true;
        }

        bool PresArche(float x, float z, float r)
        {
            foreach (var p in pieces)
            {
                float dx = p.arche.x - x, dz = p.arche.z - z;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        bool PresEscalier(float x, float z, float r)
        {
            foreach (var e in escaliers)
            {
                float cx = Math.Max(e.r.x0, Math.Min(x, e.r.x1)), cz = Math.Max(e.r.z0, Math.Min(z, e.r.z1));
                if ((cx - x) * (cx - x) + (cz - z) * (cz - z) < r * r) return true;
            }
            return false;
        }

        /// Piliers libres de la grande salle : grille régulière dans la zone dégagée du rez, espacés d'au moins 8 m.
        void PlacerPiliers()
        {
            int zA = zoneArrivee.z1 + 5;     // la première rangée ne bouche pas la vue de l'arrivée
            // profondeur dégagée du rez : la plus grande des trois sondes (axe, quarts) ; les terrasses latérales ou un
            // escalier central ne la réduisent donc pas à rien
            int zB = 0;
            foreach (int xs in new[] { L / 4, L / 2, 3 * L / 4 })
            {
                int zs = P;
                for (int x = xs - 2; x < xs + 2; x++)
                    for (int z = zA; z < P; z++)
                        if (!LibreRez(x, z)) { zs = Math.Min(zs, z); break; }
                zB = Math.Max(zB, zs);
            }
            zB -= 4;
            if (zB < zA) return;
            // une rangée, ou deux si la salle est profonde ; la seconde en quinconce si elles sont à moins de 8 m
            var rangs = new List<int>();
            if (zB - zA >= 6) { rangs.Add(zA + 1); rangs.Add(zB); }
            else rangs.Add((zA + zB + 1) / 2);
            bool quinconce = rangs.Count == 2 && rangs[1] - rangs[0] < Prm.espacementPiliersMin;
            var premiers = new List<float>();
            for (int b = 0; b < rangs.Count; b++)
            {
                int z = rangs[b];
                var xs = new List<float>();
                if (b == 1 && quinconce && premiers.Count >= 2)
                {
                    for (int k = 0; k + 1 < premiers.Count; k++) xs.Add((premiers[k] + premiers[k + 1]) * 0.5f);
                }
                else
                {
                    // chaque tronçon de rez dégagé de la rangée (de part et d'autre d'un escalier central, par exemple)
                    for (int xa = 0; xa < L; )
                    {
                        if (!LibreRez(xa, z)) { xa++; continue; }
                        int xb = xa;
                        while (xb < L && LibreRez(xb, z)) xb++;
                        float u0 = xa + 3.5f, u1 = xb - 3.5f;
                        if (u1 >= u0)
                        {
                            int n = Math.Max(1, Math.Min(L >= 48 ? 4 : L >= 36 ? 3 : 2, 1 + (int)((u1 - u0) / (Prm.espacementPiliersMin + 1.5f))));
                            if (n == 1) xs.Add((u0 + u1) * 0.5f);
                            else for (int k = 0; k < n; k++) xs.Add(u0 + (u1 - u0) * k / (n - 1));
                        }
                        xa = xb;
                    }
                }
                foreach (float xf in xs)
                {
                    int x = (int)Math.Round(xf);
                    // décalages essayés du plus près au plus loin de la position voulue
                    for (int t = 0; t < OrdreDecalage.Length; t++)
                    {
                        int q = OrdreDecalage[t], ox = (q % 5) - 2, oz = (q / 5) - 2;
                        if (PilierPossible(x + ox, z + oz)) { PoserPilier(x + ox, z + oz); if (b == 0) premiers.Add(x + ox); break; }
                    }
                }
            }
            // repli : aucun pilier sur la grille voulue ; la place libre la plus centrale, puis sa symétrique
            bool aucun = true;
            foreach (var pl in piliers) if (!pl.adosse) aucun = false;
            if (!aucun) return;
            int bx = -1, bz = -1; float bd = float.MaxValue;
            for (int z = zA; z < P - 3; z++)
                for (int x = 3; x <= L / 2; x++)
                {
                    if (!PilierPossible(x, z)) continue;
                    float d = (x - L * 0.3f) * (x - L * 0.3f) + (z - P * 0.4f) * (z - P * 0.4f);
                    if (d < bd) { bd = d; bx = x; bz = z; }
                }
            if (bx < 0) return;
            PoserPilier(bx, bz);
            if (PilierPossible(L - bx, bz)) PoserPilier(L - bx, bz);
        }

        static float DistanceSegment(float x, float z, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az, l2 = dx * dx + dz * dz;
            float t = l2 < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, ((x - ax) * dx + (z - az) * dz) / l2));
            float cx = ax + t * dx - x, cz = az + t * dz - z;
            return (float)Math.Sqrt(cx * cx + cz * cz);
        }

        bool LibreRez(int x, int z)
        {
            if (x < 0 || x >= L || z < 0 || z >= P) return false;
            var c = cellules[Index(x + Marge, z + Marge)];
            return c.genre == GenreCellule.Sol && c.sol < 0.01f;
        }

        static readonly int[] OrdreDecalage = { 12, 7, 11, 13, 17, 6, 8, 16, 18, 2, 10, 14, 22, 1, 3, 5, 9, 15, 19, 21, 23, 0, 4, 20, 24 };

        bool PilierPossible(int x, int z)
        {
            for (int dz = -3; dz < 3; dz++)
                for (int dx = -3; dx < 3; dx++)
                {
                    int i = x + dx + Marge, j = z + dz + Marge;
                    if (!DansVolume(i, j)) return false;
                    var c = cellules[Index(i, j)];
                    if (c.genre != GenreCellule.Sol || c.sol > 0.01f || c.obstacle) return false;
                }
            if (DansZoneArrivee(x, z, 2.5f)) return false;
            if (PresArche(x, z, 6f)) return false;
            // allées dégagées : de l'arrivée au pied de chaque escalier du rez
            foreach (var e in escaliers)
            {
                if (e.niveauBas != 0) continue;
                float px, pz;
                switch (e.dir)
                {
                    case Dir.Nord: px = e.r.CentreX; pz = e.r.z0 - 1f; break;
                    case Dir.Sud: px = e.r.CentreX; pz = e.r.z1 + 1f; break;
                    case Dir.Est: px = e.r.x0 - 1f; pz = e.r.CentreZ; break;
                    default: px = e.r.x1 + 1f; pz = e.r.CentreZ; break;
                }
                if (DistanceSegment(x, z, arrivee.x, zoneArrivee.z1, px, pz) < 2.2f) return false;
            }
            float e2 = Prm.espacementPiliersMin * Prm.espacementPiliersMin;
            foreach (var p in piliers)
            {
                float dx = p.x - x, dz = p.z - z;
                if (!p.adosse && dx * dx + dz * dz < e2) return false;
                if (p.adosse && dx * dx + dz * dz < 16f) return false;
            }
            return true;
        }

        void PoserPilier(int x, int z)
        {
            piliers.Add(new Pilier { x = x, z = z, rayon = RayonPilier, bas = 0f, haut = Voute(x), adosse = false });
            for (int dz = -1; dz < 1; dz++)
                for (int dx = -1; dx < 1; dx++)
                    cellules[Index(x + dx + Marge, z + dz + Marge)].obstacle = true;
        }

        // ================================================================== Pièces verrouillées, secrètes, déclencheurs
        void AttribuerPieces()
        {
            if (pieces.Count == 0) return;
            var libres = new List<int>();
            for (int k = 0; k < pieces.Count; k++) libres.Add(k);
            if (m_Alea.Proba(Prm.probaPieceVerrouillee))
            {
                int k = libres[m_Alea.Entier(libres.Count)];
                libres.Remove(k);
                var p = pieces[k];
                p.genre = GenrePiece.Verrouillee;
                int t = m_Alea.Entier(100);
                p.serrure = t < 40 ? Serrure.Bronze : t < 65 ? Serrure.Argent : t < 82 ? Serrure.Or : Serrure.Crochetable;
                p.nom = p.serrure == Serrure.Or ? "Salle du trésor" : p.serrure == Serrure.Argent ? "Chambre forte" : "Réserve fermée";
            }
            if (libres.Count > 0 && m_Alea.Proba(Prm.probaPieceSecrete))
            {
                // de préférence dans un mur de soutènement ou un massif (paroi masquée)
                var pref = libres.FindAll(k => pieces[k].emplacement != Emplacement.DerriereEnceinte);
                var l = pref.Count > 0 ? pref : libres;
                int kk = l[m_Alea.Entier(l.Count)];
                libres.Remove(kk);
                var p = pieces[kk];
                p.genre = GenrePiece.Secrete;
                p.nom = "Pièce secrète";
                var dec = PoserDeclencheur(p);
                if (dec == null) { p.genre = GenrePiece.Libre; p.nom = "Crypte"; }
                else { dec.index = declencheurs.Count; declencheurs.Add(dec); p.declencheur = dec.index; }
            }
        }

        Declencheur PoserDeclencheur(PieceCachee p)
        {
            var vu = Atteignables(true);
            bool plaque = m_Alea.Proba(0.55f);
            for (int essai = 0; essai < 2; essai++, plaque = !plaque)
            {
                if (plaque)
                {
                    var cands = new List<int>();
                    for (int j = 0; j < NZ; j++)
                        for (int i = 0; i < NX; i++)
                        {
                            int k = Index(i, j);
                            var c = cellules[k];
                            if (c.genre != GenreCellule.Sol || c.obstacle || !vu[k * 2] || !DansVolume(i, j)) continue;
                            float x = CentreX(i), z = CentreZ(j);
                            if (DansZoneArrivee(x, z, 2f) || PresArche(x, z, 3f) || PresEscalier(x, z, 2f) || ParapetPres(x, z, 1.6f)) continue;
                            float dx = x - p.arche.x, dz = z - p.arche.z, d2 = dx * dx + dz * dz;
                            if (d2 < 8f * 8f || d2 > 26f * 26f) continue;
                            if (!EntoureDeSol(i, j, c.sol)) continue;
                            cands.Add(k);
                        }
                    if (cands.Count == 0) continue;
                    int kc = cands[m_Alea.Entier(cands.Count)];
                    int ic = kc % NX, jc = kc / NX;
                    var cc = cellules[kc];
                    cellules[kc].obstacle = false;
                    return new Declencheur { genre = GenreDeclencheur.PlaqueSol, pose = new Pose(CentreX(ic), cc.sol, CentreZ(jc), 0f, Niv(cc.sol)), cible = p.index };
                }
                else
                {
                    var cands = new List<Pose>();
                    foreach (var f in faces)
                    {
                        if (f.devantEscalier || f.genre == GenreFace.Linteau || f.genre == GenreFace.Piece) continue;
                        if (f.y0 > f.solDevant + 0.01f || f.y1 < f.solDevant + 2f || f.Longueur < 3f) continue;
                        for (float s = 1.5f; s < f.Longueur - 1f; s += 1f)
                        {
                            // au milieu d'une cellule le long du mur ; cellule de devant juste en face
                            float x = f.x0 == f.x1 ? f.x0 : f.x0 + s, z = f.z0 == f.z1 ? f.z0 : f.z0 + s;
                            int i = CelluleI(x + Dx(f.dir) * 0.5f), j = CelluleJ(z + Dz(f.dir) * 0.5f);
                            if (!DansGrille(i, j)) continue;
                            int k = Index(i, j);
                            var c = cellules[k];
                            if (c.genre != GenreCellule.Sol || c.obstacle || !vu[k * 2] || Math.Abs(c.sol - f.solDevant) > 0.01f) continue;
                            if (DansZoneArrivee(x, z, 2f) || PresArche(x, z, 4f) || PresEscalier(x, z, 2f) || PresPilier(x, z, 2f)) continue;
                            float dx = x - p.arche.x, dz = z - p.arche.z, d2 = dx * dx + dz * dz;
                            if (d2 < 6f * 6f || d2 > 26f * 26f) continue;
                            cands.Add(new Pose(x, f.solDevant + 1.1f, z, Angle(f.dir), Niv(f.solDevant)));
                        }
                    }
                    if (cands.Count == 0) continue;
                    var po = cands[m_Alea.Entier(cands.Count)];
                    return new Declencheur { genre = GenreDeclencheur.BoutonMural, pose = po, cible = p.index };
                }
            }
            return null;
        }

        bool EntoureDeSol(int i, int j, float sol)
        {
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!DansGrille(i + dx, j + dz)) return false;
                    var c = cellules[Index(i + dx, j + dz)];
                    if (c.genre != GenreCellule.Sol || c.obstacle || Math.Abs(c.sol - sol) > 0.01f) return false;
                }
            return true;
        }

        bool PresPilier(float x, float z, float r)
        {
            foreach (var p in piliers)
            {
                float dx = p.x - x, dz = p.z - z, rr = r + p.rayon;
                if (dx * dx + dz * dz < rr * rr) return true;
            }
            return false;
        }

        static int Niv(float h) { return (int)Math.Round(h / HauteurNiveau); }

        // ================================================================== Coffres
        /// Direction d'un mur plein contre la cellule (i, j) au niveau du sol h ; -1 si aucune.
        /// La cellule b est-elle un mur plein entre h et h + 2 m (enceinte, massif, terrasse plus haute, paroi d'une pièce) ?
        bool EstMur(int ni, int nj, float h)
        {
            if (!DansGrille(ni, nj)) return true;
            var b = cellules[Index(ni, nj)];
            if (b.genre == GenreCellule.Hors || b.genre == GenreCellule.Massif) return true;
            if (b.genre == GenreCellule.Sol || b.genre == GenreCellule.Escalier) return b.sol >= h + 2f;
            return false;   // cavité, passage : ouvert
        }

        int MurContre(int i, int j, float h)
        {
            int best = -1;
            for (int d = 0; d < 4; d++)
            {
                if (!EstMur(i + DX[d], j + DZ[d], h)) continue;
                if (best < 0 || d == 0) best = d;       // le mur du fond (nord) d'abord
            }
            return best;
        }

        bool CoffrePossible(int i, int j, int dmur, float h, int piece)
        {
            // la cellule et ses deux voisines le long du mur : même sol, libres, contre le même mur
            int ux = DZ[dmur] != 0 ? 1 : 0, uz = DX[dmur] != 0 ? 1 : 0;
            for (int s = -1; s <= 1; s++)
            {
                int ci = i + ux * s, cj = j + uz * s;
                if (!DansGrille(ci, cj)) return false;
                var c = cellules[Index(ci, cj)];
                bool okGenre = piece < 0 ? c.genre == GenreCellule.Sol : (c.genre == GenreCellule.Cavite && c.piece == piece);
                if (!okGenre || c.obstacle || Math.Abs(c.sol - h) > 0.01f) return false;
                if (!EstMur(ci + DX[dmur], cj + DZ[dmur], h)) return false;
            }
            float x = CentreX(i), z = CentreZ(j);
            if (PresArche(x, z, 3.2f) || PresEscalier(x, z, 2.2f) || ParapetPres(x, z, 1.2f) || PresPilier(x, z, 1.4f) || DansZoneArrivee(x, z, 2f)) return false;
            foreach (var co in coffres)
            {
                float dx = co.pose.x - x, dz = co.pose.z - z;
                if (dx * dx + dz * dz < 3.2f * 3.2f) return false;
            }
            foreach (var de in declencheurs)
            {
                float dx = de.pose.x - x, dz = de.pose.z - z;
                if (dx * dx + dz * dz < 2.5f * 2.5f) return false;
            }
            return true;
        }

        bool PoserCoffre(TypeCoffre type, Func<int, int, bool> zone, float h, int piece, float viseX, float viseZ, bool auHasard)
        {
            int bi = -1, bj = -1, bd = -1;
            float bestD = float.MaxValue;
            var cands = new List<int>();
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    if (!zone(i, j)) continue;
                    var c = cellules[Index(i, j)];
                    if (Math.Abs(c.sol - h) > 0.01f) continue;
                    int dm = MurContre(i, j, h);
                    if (dm < 0 || !CoffrePossible(i, j, dm, h, piece)) continue;
                    if (auHasard) { cands.Add(Index(i, j) * 4 + dm); continue; }
                    float dx = CentreX(i) - viseX, dz = CentreZ(j) - viseZ, d2 = dx * dx + dz * dz;
                    if (d2 < bestD - 1e-4f) { bestD = d2; bi = i; bj = j; bd = dm; }
                }
            if (auHasard)
            {
                if (cands.Count == 0) return false;
                int q = cands[m_Alea.Entier(cands.Count)];
                bd = q & 3; bi = (q >> 2) % NX; bj = (q >> 2) / NX;
            }
            if (bi < 0) return false;
            // le coffre tourne le dos au mur, à 0,75 m de son nu
            float x = CentreX(bi) - DX[bd] * 0.25f, z = CentreZ(bj) - DZ[bd] * 0.25f;
            coffres.Add(new Coffre { pose = new Pose(x, h, z, Angle(Oppose((Dir)bd)), Niv(h)), type = type, piece = piece });
            return true;
        }

        void PlacerCoffres()
        {
            int nmax = Niveaux - 1;
            bool grand = false;
            var ordre = new List<Terrasse>(terrasses);
            ordre.Sort((a, b) => b.niveau != a.niveau ? b.niveau.CompareTo(a.niveau) : (b.r.Largeur * b.r.Profondeur).CompareTo(a.r.Largeur * a.r.Profondeur));
            foreach (var t in ordre)
            {
                int ti = t.index;
                Func<int, int, bool> zone = (i, j) => { var c = cellules[Index(i, j)]; return c.genre == GenreCellule.Sol && c.terrasse == ti; };
                int n = Prm.coffresParTerrasse;
                if (t.niveau == nmax && !grand)
                {
                    if (PoserCoffre(TypeCoffre.GrandCoffre, zone, t.Hauteur, -1, t.r.CentreX, t.r.z1, false)) { grand = true; n = Math.Max(1, n - 1); }
                }
                if (t.r.Largeur * t.r.Profondeur < 70) n = Math.Min(n, 1);
                for (int k = 0; k < n; k++) PoserCoffre(TypeCoffre.Coffre, zone, t.Hauteur, -1, 0f, 0f, true);
            }
            // repli : grand coffre sur la plus haute terrasse qui a un mur libre
            for (int k = 0; k < ordre.Count && !grand; k++)
            {
                var t = ordre[k];
                int ti = t.index;
                grand = PoserCoffre(TypeCoffre.GrandCoffre, (i, j) => { var c = cellules[Index(i, j)]; return c.genre == GenreCellule.Sol && c.terrasse == ti; }, t.Hauteur, -1, t.r.CentreX, t.r.CentreZ, true);
            }
            foreach (var p in pieces)
            {
                int pi = p.index;
                Func<int, int, bool> zone = (i, j) => { var c = cellules[Index(i, j)]; return c.genre == GenreCellule.Cavite && c.piece == pi; };
                // le fond de la pièce : à l'opposé de l'arche
                float vx = p.r.CentreX - Dx(p.arche.dir) * p.r.Largeur, vz = p.r.CentreZ - Dz(p.arche.dir) * p.r.Profondeur;
                int nc = 1;
                bool gc = false;
                if (p.genre == GenrePiece.Verrouillee)
                {
                    if (p.serrure == Serrure.Argent) nc = 2;
                    if (p.serrure == Serrure.Or) { gc = true; nc = 1; }
                }
                else if (p.genre == GenrePiece.Secrete) nc = 2;
                if (gc) PoserCoffre(TypeCoffre.GrandCoffre, zone, p.sol, pi, vx, vz, false);
                for (int k = 0; k < nc; k++) PoserCoffre(TypeCoffre.Coffre, zone, p.sol, pi, vx, vz, k > 0 || gc);
            }
        }

        // ================================================================== Torches
        void PlacerTorches()
        {
            float esp = Math.Max(3f, Prm.espacementTorches);
            // portail : deux torches qui l'encadrent
            AjTorche(new Pose(L * 0.5f - 3.2f, 2.6f, 0f, 0f, 0), -1);
            AjTorche(new Pose(L * 0.5f + 3.2f, 2.6f, 0f, 0f, 0), -1);
            foreach (var f in faces)
            {
                if (f.devantEscalier || f.genre == GenreFace.Linteau || f.genre == GenreFace.Piece) continue;
                if (f.y0 > f.solDevant + 0.5f || f.y1 < f.solDevant + 2.9f) continue;
                float lg = f.Longueur;
                if (lg < 3f) continue;
                int n = Math.Max(1, (int)Math.Round(lg / esp));
                float hy = f.solDevant + (f.y1 - f.solDevant >= 4f ? 2.6f : 2.2f);
                for (int m = 0; m < n; m++)
                {
                    float s = lg * (m + 0.5f) / n;
                    float x = f.x0 == f.x1 ? f.x0 : f.x0 + s, z = f.z0 == f.z1 ? f.z0 : f.z0 + s;
                    if (!DevantPraticable(x, z, f.dir, f.solDevant)) continue;
                    if (PresArche(x, z, 3f) || PresEscalier(x, z, 1.2f) || PresPilier(x, z, 1.2f)) continue;
                    if (Math.Abs(z) < 0.01f && Math.Abs(x - L * 0.5f) < 5f) continue;
                    AjTorche(new Pose(x, hy, z, Angle(f.dir), Niv(f.solDevant)), -1);
                }
            }
            // une torche au fond de chaque pièce cachée
            foreach (var p in pieces)
            {
                Dir fond = Oppose(p.arche.dir);
                float x = p.r.CentreX, z = p.r.CentreZ;
                if (fond == Dir.Nord) z = p.r.z1; else if (fond == Dir.Sud) z = p.r.z0; else if (fond == Dir.Est) x = p.r.x1; else x = p.r.x0;
                AjTorche(new Pose(x, p.sol + 2.5f, z, Angle(p.arche.dir), p.Niveau), p.index);
            }
            // lumières : toutes celles des pièces, puis les plus éloignées les unes des autres (échantillonnage le plus lointain)
            int budget = Prm.maxTorchesAllumees;
            var allumees = new List<Torche>();
            foreach (var t in torches) if (t.piece >= 0 && budget > 0) { t.allumee = true; allumees.Add(t); budget--; }
            while (budget > 0)
            {
                Torche best = null; float bd = -1f;
                foreach (var t in torches)
                {
                    if (t.allumee) continue;
                    float dmin = float.MaxValue;
                    foreach (var a in allumees)
                    {
                        float dx = a.pose.x - t.pose.x, dy = a.pose.y - t.pose.y, dz = a.pose.z - t.pose.z;
                        dmin = Math.Min(dmin, dx * dx + dy * dy + dz * dz);
                    }
                    if (dmin > bd + 1e-3f) { bd = dmin; best = t; }
                }
                if (best == null) break;
                best.allumee = true; allumees.Add(best); budget--;
            }
        }

        void AjTorche(Pose p, int piece)
        {
            foreach (var t in torches)
            {
                float dx = t.pose.x - p.x, dy = t.pose.y - p.y, dz = t.pose.z - p.z;
                if (dx * dx + dy * dy + dz * dz < 3f * 3f) return;
            }
            foreach (var d in declencheurs)
            {
                float dx = d.pose.x - p.x, dz = d.pose.z - p.z;
                if (d.genre == GenreDeclencheur.BoutonMural && dx * dx + dz * dz < 2f * 2f) return;
            }
            torches.Add(new Torche { pose = p, piece = piece });
        }

        bool DevantPraticable(float x, float z, Dir d, float sol)
        {
            float px = x + Dx(d) * 0.5f, pz = z + Dz(d) * 0.5f;
            var c = CelluleEn(px, pz);
            if (c.genre != GenreCellule.Sol && c.genre != GenreCellule.Cavite) return false;
            return Math.Abs(c.sol - sol) < 0.01f;
        }

        // ================================================================== Apparitions
        void PlacerApparitions()
        {
            // distance (en cellules) au bord du sol plein de même hauteur
            var dist = new int[NX * NZ];
            var file = new Queue<int>();
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    int k = Index(i, j);
                    var c = cellules[k];
                    if (c.genre != GenreCellule.Sol || c.obstacle || !DansVolume(i, j)) { dist[k] = 0; continue; }
                    bool bord = false;
                    for (int dz = -1; dz <= 1 && !bord; dz++)
                        for (int dx = -1; dx <= 1 && !bord; dx++)
                        {
                            if (!DansGrille(i + dx, j + dz)) { bord = true; break; }
                            var b = cellules[Index(i + dx, j + dz)];
                            if (b.genre != GenreCellule.Sol || b.obstacle || Math.Abs(b.sol - c.sol) > 0.01f) bord = true;
                        }
                    dist[k] = bord ? 1 : int.MaxValue;
                    if (bord) file.Enqueue(k);
                }
            while (file.Count > 0)
            {
                int k = file.Dequeue(), i = k % NX, j = k / NX;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int ni = i + dx, nj = j + dz;
                        if (!DansGrille(ni, nj)) continue;
                        int nk = Index(ni, nj);
                        if (dist[nk] > dist[k] + 1) { dist[nk] = dist[k] + 1; file.Enqueue(nk); }
                    }
            }
            var vu = Atteignables(true);
            int nmax = Niveaux - 1;
            int total = Math.Max(0, Prm.nbApparitions);
            // répartition : terrasse la plus haute (gardiens principaux), autres terrasses, grande salle
            var parTerrasse = new int[terrasses.Count];
            int restant = total;
            int aireTot = 0;
            foreach (var t in terrasses) aireTot += t.r.Largeur * t.r.Profondeur;
            foreach (var t in terrasses)
            {
                // 35 % des apparitions au prorata de l'aire, arrondi en entiers (03/10/2026 : l'arrondi flottant de « total × 0,35 × aire / aire
                // totale » tombait parfois pile sur ,5 et Mono, qui calcule en double, ne tranchait pas comme .NET : graines 5, 24, 25… différentes)
                long num = 35L * total * t.r.Largeur * t.r.Profondeur, den = 100L * Math.Max(1, aireTot);
                int n = t.niveau == nmax ? 3 : Math.Max(2, (int)((2 * num + den) / (2 * den)));
                n = Math.Min(n, restant);
                parTerrasse[t.index] = n; restant -= n;
            }
            for (int ti = 0; ti < terrasses.Count; ti++)
            {
                var t = terrasses[ti];
                for (int k = 0; k < parTerrasse[ti]; k++)
                {
                    TypeMonstre type;
                    if (t.niveau == nmax) type = k == 2 ? TypeMonstre.Mage : TypeMonstre.Guerrier;
                    else type = k == 0 ? TypeMonstre.Mage : (k % 2 == 1 ? TypeMonstre.Sbire : TypeMonstre.Voleur);
                    int tt = ti;
                    PoserApparition(type, true, (i, j) => cellules[Index(i, j)].terrasse == tt, type == TypeMonstre.Mage, dist, vu);
                }
            }
            for (int k = 0; k < restant; k++)
            {
                int r = m_Alea.Entier(4);
                var type = r < 2 ? TypeMonstre.Sbire : r == 2 ? TypeMonstre.Guerrier : TypeMonstre.Voleur;
                PoserApparition(type, false, (i, j) => { var c = cellules[Index(i, j)]; return c.terrasse < 0 && c.sol < 0.01f; }, false, dist, vu);
            }
        }

        void PoserApparition(TypeMonstre type, bool gardien, Func<int, int, bool> zone, bool bord, int[] dist, bool[] vu, bool repli = false)
        {
            var cands = new List<int>();
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    int k = Index(i, j);
                    var c = cellules[k];
                    if (c.genre != GenreCellule.Sol || !vu[k * 2] || !zone(i, j)) continue;
                    if (dist[k] < (bord ? 1 : 2)) continue;
                    if (bord && !ParapetPres(CentreX(i), CentreZ(j), 2.6f)) continue;
                    cands.Add(k);
                }
            // Seuils diminués d'1 mm² (03/10/2026) : les cotes sont au décimètre, les carrés de distance tombent pile sur le seuil
            // (3² + 4² = 5²) et Mono, qui calcule en double, ne tranchait pas comme .NET (graines 378, 394) ; la marge rend le test
            // identique partout sans rien changer d'autre (aucune distance réelle n'est entre le seuil et le seuil moins 0,01 m²).
            const float marge = 0.001f;
            float e2 = Prm.espacementApparitionsMin * Prm.espacementApparitionsMin - marge;
            float da2 = Prm.distanceArriveeApparitions * Prm.distanceArriveeApparitions - marge;
            // tous les candidats, dans un ordre tiré au sort (mélange de Fisher-Yates déterministe)
            for (int t = 0; t < cands.Count; t++)
            {
                int r = t + m_Alea.Entier(cands.Count - t);
                int k = cands[r]; cands[r] = cands[t]; cands[t] = k;
                int i = k % NX, j = k / NX;
                float x = CentreX(i) + (m_Alea.Entier(7) - 3) * 0.1f, z = CentreZ(j) + (m_Alea.Entier(7) - 3) * 0.1f;
                float h = cellules[k].sol;
                bool ok = true;
                float ax = x - arrivee.x, az = z - arrivee.z;
                if (ax * ax + az * az < da2) ok = false;
                if (ok) foreach (var a in apparitions) { float dx = a.pose.x - x, dz = a.pose.z - z; if (dx * dx + dz * dz < e2) { ok = false; break; } }
                if (ok) foreach (var c in coffres) { float dx = c.pose.x - x, dz = c.pose.z - z; if (dx * dx + dz * dz < 2.5f * 2.5f - marge) { ok = false; break; } }
                if (ok) foreach (var d in declencheurs) { float dx = d.pose.x - x, dz = d.pose.z - z; if (dx * dx + dz * dz < 2.5f * 2.5f - marge) { ok = false; break; } }
                if (ok && (PresArche(x, z, 3f) || PresEscalier(x, z, 1.5f) || PresPilier(x, z, 1.2f))) ok = false;
                if (!ok) continue;
                // regard vers le centre de la grande salle
                float vx = L * 0.5f - x, vz = P * 0.35f - z;
                float rot = (float)(Math.Atan2(vx, vz) * 180.0 / Math.PI);
                apparitions.Add(new Apparition { pose = new Pose(x, h, z, rot, Niv(h)), type = type, gardien = gardien });
                return;
            }
            // repli : au bord sans contrainte de bord, puis n'importe où sur le sol atteignable
            if (bord) PoserApparition(type, gardien, zone, false, dist, vu);
            else if (!repli) PoserApparition(type, false, (i, j) => DansVolume(i, j), false, dist, vu, true);
        }

        // ================================================================== Empreinte
        /// Hachage FNV-1a du plan (grille et éléments, cotes au centimètre) : deux postes qui ont la même empreinte pour une
        /// graine ont le même donjon (contrôle réseau, tests de déterminisme).
        public uint Empreinte()
        {
            uint h = 2166136261u;
            Action<int> m = v => { unchecked { h = (h ^ (uint)v) * 16777619u; } };
            Action<float> f = v => m((int)Math.Round(v * 100f));
            m(Graine); m(Essai); m(Variante); m(L); m(P);
            foreach (var c in cellules) { m((int)c.genre); f(c.sol); f(c.AUnHaut ? c.plafond : -1f); f(c.sommet); m(c.obstacle ? 1 : 0); m(c.piece); }
            foreach (var e in escaliers) { m(e.r.x0); m(e.r.z0); m(e.r.x1); m(e.r.z1); m((int)e.dir); }
            foreach (var po in pontons) { m(po.r.x0); m(po.r.z0); m(po.r.x1); m(po.r.z1); m(po.niveau); }
            foreach (var p in pieces) { m(p.r.x0); m(p.r.z0); m((int)p.genre); m((int)p.serrure); m(p.declencheur); }
            foreach (var d in declencheurs) { m((int)d.genre); f(d.pose.x); f(d.pose.z); }
            foreach (var p in piliers) { f(p.x); f(p.z); f(p.haut); }
            foreach (var c in coffres) { f(c.pose.x); f(c.pose.z); m((int)c.type); }
            foreach (var t in torches) { f(t.pose.x); f(t.pose.y); f(t.pose.z); m(t.allumee ? 1 : 0); }
            foreach (var a in apparitions) { f(a.pose.x); f(a.pose.z); m((int)a.type); }
            return h;
        }

        // ================================================================== Résumé
        public string Resume()
        {
            var sb = new StringBuilder();
            sb.Append("Graine ").Append(Graine).Append(" (essai ").Append(Essai).Append(") : v").Append(Variante).Append(" « ").Append(NomVariante).Append(" », ")
              .Append(Niveaux).Append(" niveaux, ").Append(L).Append(" × ").Append(P).Append(" m, voûte ").Append(Naissance.ToString("0.0")).Append(" → ").Append(Cle.ToString("0.0")).Append(" m ; ")
              .Append(terrasses.Count).Append(" terrasses, ").Append(escaliers.Count).Append(" escaliers, ")
              .Append(pontons.Count > 0 ? pontons.Count + " ponton (" + pontons[0].Longueur + " m), " : "").Append(pieces.Count).Append(" pièces cachées");
            foreach (var p in pieces)
            {
                sb.Append(" [").Append(p.nom).Append(", ").Append(p.emplacement).Append(", sol ").Append(p.sol.ToString("0")).Append(" m");
                if (p.genre == GenrePiece.Verrouillee) sb.Append(", serrure ").Append(p.serrure);
                if (p.genre == GenrePiece.Secrete && p.declencheur >= 0) sb.Append(", secrète : ").Append(declencheurs[p.declencheur].genre);
                sb.Append("]");
            }
            int libres = 0; foreach (var pl in piliers) if (!pl.adosse) libres++;
            sb.Append(", ").Append(libres).Append(" piliers + ").Append(piliers.Count - libres).Append(" pilastres, ").Append(coffres.Count).Append(" coffres, ")
              .Append(torches.Count).Append(" torches, ").Append(apparitions.Count).Append(" apparitions");
            if (defauts.Count > 0) sb.Append(" ; DÉFAUTS : ").Append(string.Join(" ; ", defauts.ToArray()));
            return sb.ToString();
        }

        // ================================================================== Aléa
        /// Générateur entier xorshift32 : mêmes tirages sur toutes les machines.
        sealed class Alea
        {
            uint m_S;
            public Alea(int graine, int essai)
            {
                ulong z = (ulong)(uint)graine * 0x9E3779B97F4A7C15UL + (ulong)(essai + 1) * 0xBF58476D1CE4E5B9UL;
                z ^= z >> 31; z *= 0x94D049BB133111EBUL; z ^= z >> 29;
                m_S = (uint)(z ^ (z >> 32));
                if (m_S == 0) m_S = 0x6D2B79F5u;
                for (int k = 0; k < 4; k++) Suivant();
            }
            public uint Suivant() { m_S ^= m_S << 13; m_S ^= m_S >> 17; m_S ^= m_S << 5; return m_S; }
            public int Entier(int n) { return n <= 1 ? 0 : (int)(Suivant() % (uint)n); }
            public bool Proba(float p) { return Entier(10000) < (int)(p * 10000f); }
            public int Choix(params int[] v) { return v[Entier(v.Length)]; }
        }
    }
}
