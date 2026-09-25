using UnityEngine;

namespace Deathless.Jeu
{
    /// Amélioration des compétences avec les points de compétence (1 point par jour survécu, crédité à l'aube ; menu du
    /// personnage, touche Tab / Y). Premier arbre simple par classe, **proposition à valider** (wiki : classes.md,
    /// Points de compétence) : une amélioration par action de la classe, 3 rangs, 1 point par rang. Chaque rang change
    /// d'un pas fixe une valeur de GameBalance (dégâts, recharge, coût, soin, flèches) ; les classes lisent le facteur
    /// par ClasseHeros.Facteur(index).
    public static class ArbreCompetences
    {
        public const int RangMax = 3;
        public const int CoutParRang = 1;

        public enum Sens { Plus, Moins, Ajout }

        public sealed class Amelioration
        {
            public readonly string nom, icone, texte;
            public readonly Sens sens;
            public readonly float parRang;
            public Amelioration(string nom, string icone, Sens sens, float parRang, string texte)
            { this.nom = nom; this.icone = icone; this.sens = sens; this.parRang = parRang; this.texte = texte; }
        }

        static readonly Amelioration[] Paladin =
        {
            new Amelioration("Épée affûtée", "paladin_epee", Sens.Plus, 0.10f, "+10 % de dégâts à l’épée"),
            new Amelioration("Garde solide", "paladin_garde", Sens.Moins, 0.15f, "−15 % d’endurance par coup bloqué"),
            new Amelioration("Bélier infatigable", "paladin_charge_belier", Sens.Moins, 0.12f, "−12 % de recharge de la charge bélier"),
            new Amelioration("Soin fervent", "paladin_soin", Sens.Plus, 0.20f, "+20 % de vie rendue par le soin"),
        };
        static readonly Amelioration[] Viking =
        {
            new Amelioration("Hache lourde", "viking_hache", Sens.Plus, 0.10f, "+10 % de dégâts à la hache"),
            new Amelioration("Tourbillon", "viking_attaque_tournante", Sens.Moins, 0.15f, "−15 % de rage consommée par l’attaque tournante"),
            new Amelioration("Cri de guerre", "viking_rugissement", Sens.Moins, 0.12f, "−12 % de recharge du rugissement"),
            new Amelioration("Chute brutale", "viking_saut_percutant", Sens.Plus, 0.15f, "+15 % de dégâts du saut percutant"),
        };
        static readonly Amelioration[] Mage =
        {
            new Amelioration("Brasier", "mage_boule_de_feu", Sens.Plus, 0.10f, "+10 % de dégâts de la boule de feu"),
            new Amelioration("Souffle économe", "mage_cone_de_flammes", Sens.Moins, 0.12f, "−12 % de mana consommé par le cône de flammes"),
            new Amelioration("Source de mana", "classe_mage_feu", Sens.Plus, 0.20f, "+20 % de régénération du mana"),
        };
        static readonly Amelioration[] Rodeur =
        {
            new Amelioration("Pointes d’acier", "rodeur_tir", Sens.Plus, 0.10f, "+10 % de dégâts des flèches"),
            new Amelioration("Main sûre", "rodeur_visee", Sens.Moins, 0.10f, "−10 % de temps pour bander l’arc à fond"),
            new Amelioration("Nuée drue", "rodeur_nuee_de_fleches", Sens.Moins, 0.12f, "−12 % de recharge de la nuée de flèches"),
            new Amelioration("Salve fournie", "rodeur_roulade_salve", Sens.Ajout, 1f, "+1 flèche dans la salve de la roulade"),
        };
        static readonly Amelioration[] Assassin =
        {
            new Amelioration("Lame empoisonnée", "assassin_dague", Sens.Plus, 0.10f, "+10 % de dégâts à la dague"),
            new Amelioration("Rechargement vif", "assassin_arbalete", Sens.Moins, 0.12f, "−12 % de recharge de l’arbalète"),
            new Amelioration("Fumée épaisse", "assassin_fumigene", Sens.Moins, 0.12f, "−12 % de recharge de la grenade fumigène"),
        };
        static readonly Amelioration[] Aucune = new Amelioration[0];

        public static Amelioration[] De(string classeId)
        {
            switch (classeId)
            {
                case "paladin": return Paladin;
                case "viking": return Viking;
                case "mage": return Mage;
                case "rodeur": return Rodeur;
                case "assassin": return Assassin;
                default: return Aucune;
            }
        }

        /// Facteur de l'amélioration `index` au rang donné (1 sans effet) ; pour Sens.Ajout, le nombre à ajouter (0 sans effet).
        public static float Facteur(string classeId, int index, int rang)
        {
            var a = De(classeId);
            if (index < 0 || index >= a.Length) return 1f;
            var d = a[index];
            switch (d.sens)
            {
                case Sens.Plus: return 1f + rang * d.parRang;
                case Sens.Moins: return Mathf.Max(0.1f, 1f - rang * d.parRang);
                default: return rang * d.parRang;
            }
        }
    }
}
