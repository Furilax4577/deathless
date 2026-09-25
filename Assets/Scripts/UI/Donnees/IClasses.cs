using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Jauge propre à une classe (bas, à gauche du HUD, sous l'endurance).
    public enum JaugeClasse
    {
        Aucune,
        /// Mage : bleue.
        Mana,
        /// Viking : orange.
        Rage,
    }

    /// Une action d'une classe, pour l'écran de choix (attaque, attaque secondaire, compétences 1 à 3).
    public interface IActionClasse
    {
        /// Action de DeathlessControls, « Carte/Action » (l'invite du bouton en est tirée).
        string Action { get; }

        /// Nom affiché (« Charge bélier »). Null ou vide : emplacement vide (affiché grisé).
        string Nom { get; }
    }

    /// Une classe jouable (écran de choix de classe, carte du menu principal).
    public interface IClasseJouable
    {
        /// Identifiant stable : « paladin », « mage », « rodeur », « assassin », « viking ».
        string Id { get; }
        string Nom { get; }
        /// Rôle (wiki, classes.md) : « Tank, une cible à la fois ».
        string Role { get; }
        /// Style d'arme : « Épée et bouclier ».
        string Arme { get; }
        /// Une ou deux phrases.
        string Description { get; }
        /// Teinte du portrait.
        Color Teinte { get; }
        JaugeClasse Jauge { get; }
        /// Attaque principale, attaque secondaire, compétences 1, 2, 3 (dans cet ordre).
        IReadOnlyList<IActionClasse> Actions { get; }
    }

    /// Classes proposées par le jeu (sens jeu → UI) et lancement d'une partie avec la classe choisie (UI → jeu).
    /// Facultatif : l'objet enregistré comme ICommandesPartie l'implémente s'il connaît les classes. Sinon l'écran de
    /// choix affiche ClassesJouables.Catalogue et appelle ICommandesPartie.LancerSolo().
    public interface IClassesJouables
    {
        IReadOnlyList<IClasseJouable> Classes { get; }

        /// Lance une partie solo avec la classe `classeId` (un Id du catalogue). Un jeu qui ne connaît pas encore
        /// cette classe lance celle qu'il sait jouer.
        void LancerSolo(string classeId);
    }

    /// État propre à la classe du joueur local, lu par le HUD à chaque image. Facultatif : l'objet enregistré comme
    /// IEtatJoueur l'implémente si sa classe a une jauge ou un mode furtif (rétrocompatible : sans lui, rien ne s'affiche).
    public interface IEtatJoueurClasse
    {
        /// Jauge de la classe (Aucune : masquée).
        JaugeClasse Jauge { get; }
        float ValeurJauge { get; }
        float JaugeMax { get; }

        /// Assassin en mode furtif : œil barré près du portrait (thème Ombre).
        bool Furtif { get; }
    }

    /// Catalogue des cinq classes (Wiki/pages/classes.md et commandes.md) et mémoire de la dernière classe jouée.
    public static class ClassesJouables
    {
        public const string ClePrefs = "Deathless.DerniereClasse";
        public const string ParDefaut = "paladin";

        sealed class ActionClasse : IActionClasse
        {
            public ActionClasse(string action, string nom) { Action = action; Nom = nom; }
            public string Action { get; }
            public string Nom { get; }
        }

        sealed class Classe : IClasseJouable
        {
            public string Id { get; set; }
            public string Nom { get; set; }
            public string Role { get; set; }
            public string Arme { get; set; }
            public string Description { get; set; }
            public Color Teinte { get; set; }
            public JaugeClasse Jauge { get; set; }
            public IReadOnlyList<IActionClasse> Actions { get; set; }
        }

        static IActionClasse[] Actions(string rt, string lt, string lb, string rb, string lbrb = null) => new IActionClasse[]
        {
            new ActionClasse("Gameplay/AttackPrimary", rt),
            new ActionClasse("Gameplay/AttackSecondary", lt),
            new ActionClasse("Gameplay/Skill1", lb),
            new ActionClasse("Gameplay/Skill2", rb),
            new ActionClasse("Gameplay/Skill3", lbrb),
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.gray;

        /// Teintes : Paladin or (#d9b264, inchangé) ; les autres tirées des palettes de VFX de leur thème
        /// (Feu vif, Chasse ocre, Ombre lilas, Rage vif). Le Rôdeur prend l'ocre #a8742f et non l'ocre clair #d9b45a,
        /// trop proche de l'or du Paladin.
        public static readonly IReadOnlyList<IClasseJouable> Catalogue = new IClasseJouable[]
        {
            new Classe
            {
                Id = "paladin", Nom = "Paladin", Role = "Tank, une cible à la fois", Arme = "Épée et bouclier",
                Description = "Il tient la ligne : la garde bloque les coups et devient une parade au bon moment.",
                Teinte = Hex("#d9b264"), Jauge = JaugeClasse.Aucune,
                Actions = Actions("Frappe à l’épée", "Garde et parade", "Charge bélier", "Soin sur soi"),
            },
            new Classe
            {
                Id = "mage", Nom = "Mage", Role = "Distance, zone", Arme = "Bâton",
                Description = "Boules de feu et flammes : les ennemis touchés brûlent. Ses sorts coûtent du mana.",
                Teinte = Hex("#ff610a"), Jauge = JaugeClasse.Mana,
                Actions = Actions("Boule de feu", "Cône de flammes (maintenu)", null, null),
            },
            new Classe
            {
                Id = "rodeur", Nom = "Rôdeur", Role = "Distance, précision", Arme = "Arc et carquois",
                Description = "Plus l’arc est bandé, plus le tir fait mal. Une flèche dans la tête est un coup critique.",
                Teinte = Hex("#a8742f"), Jauge = JaugeClasse.Aucune,
                Actions = Actions("Bander et tirer", "Viser", "Nuée de flèches", "Roulade arrière et salve"),
            },
            new Classe
            {
                Id = "assassin", Nom = "Assassin", Role = "Furtif, coups critiques", Arme = "Dague, arbalète dans le dos",
                Description = "En marchant, il devient furtif. Un coup non détecté ou porté dans le dos est un coup critique.",
                Teinte = Hex("#a58ad6"), Jauge = JaugeClasse.Aucune,
                Actions = Actions("Dague", "Arbalète en main, visée", "Grenade fumigène", null),
            },
            new Classe
            {
                Id = "viking", Nom = "Viking", Role = "Mêlée, zone", Arme = "Hache à deux mains",
                Description = "Chaque coup fait monter sa rage, que ses compétences consomment.",
                Teinte = Hex("#b3261e"), Jauge = JaugeClasse.Rage,
                Actions = Actions("Hache", "Attaque tournante (maintenue)", "Rugissement", "Saut percutant"),
            },
        };

        /// Classes proposées : celles du jeu s'il les fournit, sinon le catalogue.
        public static IReadOnlyList<IClasseJouable> Proposees =>
            (DonneesUI.Commandes as IClassesJouables)?.Classes ?? Catalogue;

        public static IClasseJouable Trouver(string id)
        {
            foreach (var c in Proposees) if (c.Id == id) return c;
            foreach (var c in Catalogue) if (c.Id == id) return c;
            return null;
        }

        /// Dernière classe jouée (PlayerPrefs), présélectionnée dans l'écran de choix et présentée au menu principal.
        public static string DerniereJouee
        {
            get => PlayerPrefs.GetString(ClePrefs, ParDefaut);
            set { PlayerPrefs.SetString(ClePrefs, value); PlayerPrefs.Save(); }
        }

        public static IClasseJouable Derniere => Trouver(DerniereJouee) ?? Trouver(ParDefaut);

        /// Valider le choix : retient la classe et lance la partie solo.
        public static void Lancer(string classeId)
        {
            DerniereJouee = classeId;
            if (DonneesUI.Commandes is IClassesJouables classes) classes.LancerSolo(classeId);
            else DonneesUI.Commandes?.LancerSolo();
        }
    }
}
