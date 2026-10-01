using UnityEngine;

namespace Deathless.Jeu
{
    /// Les six attributs (wiki : classes.md, Attributs ; décidé le 01/10/2026). L'ordre sert d'index dans
    /// EtatJoueur.attributs et dans les répartitions de GameBalance : ne pas le changer.
    public enum Attribut { Force, Endurance, Agilite, Perception, Esprit, Chance }

    /// Attributs du héros : 18 points de départ répartis par la classe (GameBalance.attributs<Classe>), +1 point
    /// d'attribut à chaque aube survécue (en plus du point de compétence), plafond 10, pas de réattribution.
    /// Règle d'équilibre (à confirmer) : les stats actuelles correspondent à la répartition de départ ; seuls les points
    /// GAGNÉS (EtatJoueur.attributs) ajoutent les bonus de GameBalance (attribut*). Avec 0 point gagné, tous les facteurs
    /// valent 1 et toutes les chances 0 : rien ne change.
    /// Les effets sont lus là où la stat est calculée : Heros (vie, endurance, vitesse, esquive, dégâts et critiques dans
    /// Frapper), ClasseHeros (cadence de l'attaque, recharges, jauge), ParadeParfaite et ClassePaladin (recul), DonjonJeu (or).
    public static class Attributs
    {
        public const int Nombre = 6;
        public const int Plafond = 10;
        public const int PointsDepart = 18;
        public const int CoutParPoint = 1;

        public static readonly string[] Noms = { "Force", "Endurance", "Agilité", "Perception", "Esprit", "Chance" };

        static GameBalance B => GameBalance.Courant;
        static readonly int[] s_Defaut = { 3, 3, 3, 3, 3, 3 };

        /// Répartition de départ de la classe (6 valeurs, chacune au moins 1).
        public static int[] Depart(string classeId)
        {
            var b = B;
            int[] r = null;
            if (b != null)
                switch (classeId)
                {
                    case "paladin": r = b.attributsPaladin; break;
                    case "viking": r = b.attributsViking; break;
                    case "mage": r = b.attributsMage; break;
                    case "rodeur": r = b.attributsRodeur; break;
                    case "assassin": r = b.attributsAssassin; break;
                }
            return r != null && r.Length >= Nombre ? r : s_Defaut;
        }

        public static int Depart(string classeId, Attribut a) => Mathf.Max(1, Depart(classeId)[(int)a]);

        /// Points gagnés en partie dans cet attribut (0 sans état).
        public static int Gagnes(EtatJoueur j, Attribut a)
        {
            if (j == null || j.attributs == null) return 0;
            int i = (int)a;
            return i < j.attributs.Length ? Mathf.Max(0, j.attributs[i]) : 0;
        }

        /// Valeur affichée : départ de la classe + points gagnés (au plus Plafond).
        public static int Valeur(EtatJoueur j, Attribut a) => j == null ? 0 : Mathf.Min(Plafond, Depart(j.classeId, a) + Gagnes(j, a));

        /// Un point peut-il encore aller dans cet attribut (plafond non atteint) ?
        public static bool SousPlafond(EtatJoueur j, Attribut a) => j != null && Depart(j.classeId, a) + Gagnes(j, a) < Plafond;

        // ----------------------------------------------------------------- Effets (1 / 0 avec 0 point gagné)

        public static float FacteurDegatsMelee(EtatJoueur j) => 1f + Gagnes(j, Attribut.Force) * B.attributForceDegats;
        public static float FacteurRecul(EtatJoueur j) => 1f + Gagnes(j, Attribut.Force) * B.attributForceRecul;
        public static float BonusPv(EtatJoueur j) => Gagnes(j, Attribut.Endurance) * B.attributEndurancePv;
        public static float BonusEndurance(EtatJoueur j) => Gagnes(j, Attribut.Endurance) * B.attributEnduranceEndurance;
        /// Vitesse de déplacement et cadence de l'attaque (RT).
        public static float FacteurVitesse(EtatJoueur j) => 1f + Gagnes(j, Attribut.Agilite) * B.attributAgiliteVitesse;
        public static float FacteurRechargeEsquive(EtatJoueur j) => Mathf.Max(0.1f, 1f - Gagnes(j, Attribut.Agilite) * B.attributAgiliteEsquive);
        public static float FacteurDegatsDistance(EtatJoueur j) => 1f + Gagnes(j, Attribut.Perception) * B.attributPerceptionDegats;
        public static float FacteurJauge(EtatJoueur j) => 1f + Gagnes(j, Attribut.Esprit) * B.attributEspritJauge;
        public static float FacteurRechargeCompetences(EtatJoueur j) => Mathf.Max(0.1f, 1f - Gagnes(j, Attribut.Esprit) * B.attributEspritRecharge);
        public static float FacteurOr(EtatJoueur j) => 1f + Gagnes(j, Attribut.Chance) * B.attributChanceOr;

        /// Chance de critique ajoutée par les attributs : Chance (tous les coups) + Perception (coups à distance).
        public static float ChanceCritique(EtatJoueur j, bool aDistance)
            => Gagnes(j, Attribut.Chance) * B.attributChanceCritique + (aDistance ? Gagnes(j, Attribut.Perception) * B.attributPerceptionCritique : 0f);

        // ----------------------------------------------------------------- Textes (menu du personnage)

        static string Pc(float part) => Mathf.RoundToInt(part * 100f) + " %";

        /// Effet d'un point gagné, en clair.
        public static string EffetParPoint(Attribut a)
        {
            var b = B;
            switch (a)
            {
                case Attribut.Force: return "+" + Pc(b.attributForceDegats) + " de dégâts au corps à corps, +" + Pc(b.attributForceRecul) + " de recul";
                case Attribut.Endurance: return "+" + b.attributEndurancePv.ToString("0") + " PV, +" + b.attributEnduranceEndurance.ToString("0") + " d’endurance";
                case Attribut.Agilite: return "+" + Pc(b.attributAgiliteVitesse) + " de vitesse (déplacement, attaque), −" + Pc(b.attributAgiliteEsquive) + " de recharge de l’esquive";
                case Attribut.Perception: return "+" + Pc(b.attributPerceptionDegats) + " de dégâts à distance, +" + Pc(b.attributPerceptionCritique) + " de critique à distance";
                case Attribut.Esprit: return "+" + Pc(b.attributEspritJauge) + " de jauge de classe, −" + Pc(b.attributEspritRecharge) + " de recharge des compétences";
                default: return "+" + Pc(b.attributChanceCritique) + " de critique, +" + Pc(b.attributChanceOr) + " d’or ramassé";
            }
        }

        /// Total des bonus gagnés, en clair (vide sans point gagné).
        public static string EffetActuel(EtatJoueur j, Attribut a)
        {
            int g = Gagnes(j, a);
            if (g <= 0) return "";
            var b = B;
            switch (a)
            {
                case Attribut.Force: return "+" + Pc(g * b.attributForceDegats) + " dégâts, +" + Pc(g * b.attributForceRecul) + " recul";
                case Attribut.Endurance: return "+" + (g * b.attributEndurancePv).ToString("0") + " PV, +" + (g * b.attributEnduranceEndurance).ToString("0") + " endurance";
                case Attribut.Agilite: return "+" + Pc(g * b.attributAgiliteVitesse) + " vitesse, −" + Pc(1f - FacteurRechargeEsquive(j)) + " esquive";
                case Attribut.Perception: return "+" + Pc(g * b.attributPerceptionDegats) + " dégâts, +" + Pc(g * b.attributPerceptionCritique) + " critique";
                case Attribut.Esprit: return "+" + Pc(g * b.attributEspritJauge) + " jauge, −" + Pc(1f - FacteurRechargeCompetences(j)) + " recharges";
                default: return "+" + Pc(g * b.attributChanceCritique) + " critique, +" + Pc(g * b.attributChanceOr) + " or";
            }
        }

        // ----------------------------------------------------------------- Réseau (HerosReseau)

        /// Points gagnés tassés dans un entier (4 bits par attribut, 0 à 15), pour la variable réseau du héros.
        public static int Tasser(int[] gagnes)
        {
            int v = 0;
            if (gagnes == null) return 0;
            for (int i = 0; i < Nombre && i < gagnes.Length; i++) v |= Mathf.Clamp(gagnes[i], 0, 15) << (4 * i);
            return v;
        }

        public static void Detasser(int v, int[] gagnes)
        {
            if (gagnes == null) return;
            for (int i = 0; i < Nombre && i < gagnes.Length; i++) gagnes[i] = (v >> (4 * i)) & 0xF;
        }
    }
}
