using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Potions du druide ; la valeur sert d'index dans EtatJoueur.potions et dans le HUD (ne pas réordonner).
    public enum Potion : byte { Sante, Mana, Endurance }

    /// Clés du mécano ; la valeur sert d'index dans EtatJoueur.cles.
    public enum Cle : byte { Bronze, Argent, Or }

    /// Articles des deux boutiques du village (octet sur le réseau : ne pas réordonner).
    public enum ArticleBoutique : byte { CleBronze, CleArgent, CleOr, KitCrochetage, PotionSante, PotionMana, PotionEndurance }

    /// Inventaire d'un joueur (03/10/2026, boutiques du mécano et du druide ; wiki : village.md, donjon.md) : potions (3 sortes,
    /// 3 au plus de chaque), clés à usage unique (bronze, argent, or ; 2 au plus de chaque), crochets du kit. Les valeurs sont
    /// dans EtatJoueur (potions, cles, crochets) ; ici, les noms, les prix, les maximums, l'ajout et l'emballage en un entier
    /// pour le réseau (4 bits par quantité : propriétaire écrit, HerosReseau réplique ; l'hôte valide les achats).
    public static class Inventaire
    {
        public const int NbPotions = 3, NbCles = 3;

        static GameBalance B => GameBalance.Courant;

        public static bool EstPotion(ArticleBoutique a) => a >= ArticleBoutique.PotionSante;
        public static bool EstCle(ArticleBoutique a) => a <= ArticleBoutique.CleOr;
        public static Potion DePotion(ArticleBoutique a) => (Potion)((int)a - (int)ArticleBoutique.PotionSante);
        public static ArticleBoutique ArticleDe(Potion p) => (ArticleBoutique)((int)ArticleBoutique.PotionSante + (int)p);
        public static ArticleBoutique ArticleDe(Cle k) => (ArticleBoutique)((int)ArticleBoutique.CleBronze + (int)k);

        public static string Nom(ArticleBoutique a)
        {
            switch (a)
            {
                case ArticleBoutique.CleBronze: return "Clé de bronze";
                case ArticleBoutique.CleArgent: return "Clé d’argent";
                case ArticleBoutique.CleOr: return "Clé d’or";
                case ArticleBoutique.KitCrochetage: return "Kit de crochetage";
                case ArticleBoutique.PotionSante: return "Potion de santé";
                case ArticleBoutique.PotionMana: return "Potion de mana";
                default: return "Potion d’endurance";
            }
        }

        public static string NomPotion(Potion p) => Nom(ArticleDe(p));

        public static int Prix(ArticleBoutique a)
        {
            var b = B;
            switch (a)
            {
                case ArticleBoutique.CleBronze: return b.mecanoPrixCleBronze;
                case ArticleBoutique.CleArgent: return b.mecanoPrixCleArgent;
                case ArticleBoutique.CleOr: return b.mecanoPrixCleOr;
                case ArticleBoutique.KitCrochetage: return b.mecanoPrixKit;
                case ArticleBoutique.PotionSante: return b.druidePrixSante;
                case ArticleBoutique.PotionMana: return b.druidePrixMana;
                default: return b.druidePrixEndurance;
            }
        }

        /// Quantité maximale portée de cet article (crochets : GameBalance.crochetsMax).
        public static int Max(ArticleBoutique a)
        {
            var b = B;
            if (EstPotion(a)) return Mathf.Max(1, b.potionsMaxParSorte);
            if (EstCle(a)) return Mathf.Max(1, b.clesMaxParSorte);
            return Mathf.Max(b.crochetsParKit, b.crochetsMax);
        }

        /// Quantité portée (0 sans état) ; pour le kit, le nombre de crochets.
        public static int Quantite(EtatJoueur j, ArticleBoutique a)
        {
            if (j == null) return 0;
            if (EstPotion(a)) return j.potions != null && (int)DePotion(a) < j.potions.Length ? j.potions[(int)DePotion(a)] : 0;
            if (EstCle(a)) return j.cles != null && (int)a < j.cles.Length ? j.cles[(int)a] : 0;
            return j.crochets;
        }

        public static int Potions(EtatJoueur j, Potion p) => Quantite(j, ArticleDe(p));
        public static int Cles(EtatJoueur j, Cle k) => Quantite(j, ArticleDe(k));

        /// Un joueur de cette classe peut-il porter et boire cette potion ? La potion de mana est réservée au Mage (seule
        /// classe à jauge de mana) ; le héros peut être une marionnette (même prefab de classe).
        public static bool PotionUtilisable(Potion p, Heros h) => p != Potion.Mana || (h != null && h.Classe != null && h.Classe.Jauge == JaugeClasse.Mana);

        /// Ajoute l'article acheté (le kit donne GameBalance.crochetsParKit crochets), dans la limite du maximum.
        public static void Ajouter(EtatJoueur j, ArticleBoutique a)
        {
            if (j == null) return;
            if (j.potions == null || j.potions.Length < NbPotions) System.Array.Resize(ref j.potions, NbPotions);
            if (j.cles == null || j.cles.Length < NbCles) System.Array.Resize(ref j.cles, NbCles);
            if (EstPotion(a)) { int i = (int)DePotion(a); j.potions[i] = Mathf.Min(Max(a), j.potions[i] + 1); }
            else if (EstCle(a)) { int i = (int)a; j.cles[i] = Mathf.Min(Max(a), j.cles[i] + 1); }
            else j.crochets = Mathf.Min(Max(a), j.crochets + Mathf.Max(1, B.crochetsParKit));
        }

        /// Retire un exemplaire (potion bue, clé tournée) ; faux s'il n'y en avait pas.
        public static bool Retirer(EtatJoueur j, Potion p)
        {
            if (j == null || j.potions == null || (int)p >= j.potions.Length || j.potions[(int)p] <= 0) return false;
            j.potions[(int)p]--;
            return true;
        }

        public static bool Retirer(EtatJoueur j, Cle k)
        {
            if (j == null || j.cles == null || (int)k >= j.cles.Length || j.cles[(int)k] <= 0) return false;
            j.cles[(int)k]--;
            return true;
        }

        /// Casse un crochet (échec de crochetage) ; faux sans crochet.
        public static bool CasserCrochet(EtatJoueur j)
        {
            if (j == null || j.crochets <= 0) return false;
            j.crochets--;
            return true;
        }

        // ----------------------------------------------------------------- Emballage réseau (4 bits par quantité)

        /// Les sept quantités dans un entier : potions (santé, mana, endurance), clés (bronze, argent, or), crochets. Chacune
        /// est bornée à 15 (le maximum de crochets est de 10).
        public static int Tasser(EtatJoueur j)
        {
            if (j == null) return 0;
            int v = 0;
            for (int i = 0; i < NbPotions; i++) v |= Mathf.Clamp(j.potions != null && i < j.potions.Length ? j.potions[i] : 0, 0, 15) << (4 * i);
            for (int i = 0; i < NbCles; i++) v |= Mathf.Clamp(j.cles != null && i < j.cles.Length ? j.cles[i] : 0, 0, 15) << (4 * (NbPotions + i));
            v |= Mathf.Clamp(j.crochets, 0, 15) << (4 * (NbPotions + NbCles));
            return v;
        }

        public static void Detasser(int v, EtatJoueur j)
        {
            if (j == null) return;
            if (j.potions == null || j.potions.Length < NbPotions) j.potions = new int[NbPotions];
            if (j.cles == null || j.cles.Length < NbCles) j.cles = new int[NbCles];
            for (int i = 0; i < NbPotions; i++) j.potions[i] = (v >> (4 * i)) & 0xF;
            for (int i = 0; i < NbCles; i++) j.cles[i] = (v >> (4 * (NbPotions + i))) & 0xF;
            j.crochets = (v >> (4 * (NbPotions + NbCles))) & 0xF;
        }

        /// Texte d'un résumé (journal, tests) : « potions 1/0/2, clés 1/0/0, crochets 5 ».
        public static string Resume(EtatJoueur j)
        {
            if (j == null) return "(aucun joueur)";
            return "potions " + Quantite(j, ArticleBoutique.PotionSante) + "/" + Quantite(j, ArticleBoutique.PotionMana) + "/" + Quantite(j, ArticleBoutique.PotionEndurance)
                + ", clés " + Quantite(j, ArticleBoutique.CleBronze) + "/" + Quantite(j, ArticleBoutique.CleArgent) + "/" + Quantite(j, ArticleBoutique.CleOr)
                + ", crochets " + j.crochets;
        }
    }
}
