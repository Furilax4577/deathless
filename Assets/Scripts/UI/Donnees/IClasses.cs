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

        /// Identifiant de l'icône (nom du SVG sans extension, table IconesUI) : « paladin_charge_belier ». Null : aucune.
        string Icone { get; }
    }

    /// Une classe jouable (écran de choix de classe, carte du menu principal).
    public interface IClasseJouable
    {
        /// Identifiant stable : « paladin », « mage », « rodeur », « assassin », « viking ».
        string Id { get; }
        string Nom { get; }
        /// Rôle (wiki, classes.md) : « Tank, mêlée vers l’avant ».
        string Role { get; }
        /// Style d'arme : « Épée et bouclier ».
        string Arme { get; }
        /// Une ou deux phrases.
        string Description { get; }
        /// Teinte du portrait.
        Color Teinte { get; }
        /// Identifiant de l'emblème hexagonal (table IconesUI) : « classe_paladin ».
        string Embleme { get; }
        JaugeClasse Jauge { get; }
        /// Attaque principale, attaque secondaire, compétences 1, 2, 3 (dans cet ordre).
        IReadOnlyList<IActionClasse> Actions { get; }
        /// Classe à venir (Druide, Mécanicien) : visible dans le choix de classe avec l'étiquette « Bientôt », mais on ne
        /// peut pas la jouer (Valider inactif, son de refus).
        bool Verrouillee { get; }
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

    /// Aperçu 3D de la classe dans l'écran de choix (facultatif, fourni par le jeu : DonneesUI.ApercuClasse). Une caméra
    /// dédiée rend le héros de la classe (modèle, arme, pose de repos) sur un socle, dans une RenderTexture que l'écran
    /// affiche. La caméra n'est active que pendant Montrer … Cacher.
    public interface IApercuClasse
    {
        /// Image rendue (RenderTexture, fond transparent).
        Texture Rendu { get; }

        /// Affiche la classe (changement immédiat, apparition par la taille) et active la caméra.
        void Montrer(string classeId);

        /// Désactive la caméra (écran de choix fermé).
        void Cacher();

        /// Rotation manuelle en degrés (souris glissée, stick droit), en plus de la rotation lente continue.
        void Tourner(float degres);
    }

    /// Potions du joueur local (facultatif, rétrocompatible) : l'objet enregistré comme IEtatJoueur l'implémente s'il gère
    /// des potions ; le HUD affiche alors, à droite des jauges, l'icône de la potion, le nombre restant et l'invite de
    /// Gameplay/DrinkPotion (croix haut, 1).
    public interface IEtatJoueurPotions
    {
        int Potions { get; }
        int PotionsMax { get; }
    }

    /// Catalogue des cinq classes (Wiki/pages/classes.md et commandes.md) et mémoire de la dernière classe jouée.
    public static class ClassesJouables
    {
        public const string ClePrefs = "Deathless.DerniereClasse";
        public const string ParDefaut = "paladin";

        sealed class ActionClasse : IActionClasse
        {
            public ActionClasse(string action, (string nom, string icone) a) { Action = action; Nom = a.nom; Icone = a.icone; }
            public string Action { get; }
            public string Nom { get; }
            public string Icone { get; }
        }

        sealed class Classe : IClasseJouable
        {
            public string Id { get; set; }
            public string Nom { get; set; }
            public string Role { get; set; }
            public string Arme { get; set; }
            public string Description { get; set; }
            public Color Teinte { get; set; }
            public string Embleme { get; set; }
            public JaugeClasse Jauge { get; set; }
            public bool Verrouillee { get; set; }
            public IReadOnlyList<IActionClasse> Actions { get; set; }
        }

        static readonly (string, string) Vide = (null, null);

        /// Actions RT, LT, LB, RB, LB + RB : (nom affiché, identifiant de l'icône). Table icône → bouton :
        /// ArtSources/Icones/LISEZMOI.md.
        static IActionClasse[] Actions((string, string) rt, (string, string) lt, (string, string) lb, (string, string) rb) =>
            Actions(rt, lt, lb, rb, Vide);

        static IActionClasse[] Actions((string, string) rt, (string, string) lt, (string, string) lb, (string, string) rb, (string, string) lbrb) => new IActionClasse[]
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
                Id = "paladin", Nom = "Paladin", Role = "Tank, mêlée vers l’avant", Arme = "Épée et bouclier",
                Description = "Il tient la ligne : la garde bloque les coups et devient une parade au bon moment.",
                Teinte = Hex("#d9b264"), Embleme = "classe_paladin", Jauge = JaugeClasse.Aucune,
                Actions = Actions(("Frappe à l’épée", "paladin_epee"), ("Garde et parade", "paladin_garde"),
                    ("Charge bélier", "paladin_charge_belier"), ("Soin sur soi", "paladin_soin")),
            },
            new Classe
            {
                Id = "mage", Nom = "Mage", Role = "Distance, zone", Arme = "Bâton",
                Description = "Boules de feu et flammes : les ennemis touchés brûlent. Ses sorts coûtent du mana.",
                Teinte = Hex("#ff610a"), Embleme = "classe_mage_feu", Jauge = JaugeClasse.Mana,
                Actions = Actions(("Boule de feu", "mage_boule_de_feu"), ("Cône de flammes (maintenu)", "mage_cone_de_flammes"), Vide, Vide),
            },
            new Classe
            {
                Id = "rodeur", Nom = "Rôdeur", Role = "Distance, précision", Arme = "Arc et carquois",
                Description = "Plus l’arc est bandé, plus le tir fait mal. Une flèche dans la tête est un coup critique.",
                Teinte = Hex("#a8742f"), Embleme = "classe_rodeur", Jauge = JaugeClasse.Aucune,
                Actions = Actions(("Bander en visant, relâcher pour tirer", "rodeur_tir"), ("Viser (maintenu, sans zoom)", "rodeur_visee"),
                    ("Nuée de flèches", "rodeur_nuee_de_fleches"), ("Roulade arrière et salve", "rodeur_roulade_salve")),
            },
            new Classe
            {
                Id = "assassin", Nom = "Assassin", Role = "Furtif, coups critiques", Arme = "Dague, arbalète dans le dos",
                Description = "En marchant, il devient furtif. Un coup non détecté ou porté dans le dos est un coup critique.",
                Teinte = Hex("#a58ad6"), Embleme = "classe_assassin", Jauge = JaugeClasse.Aucune,
                Actions = Actions(("Dague", "assassin_dague"), ("Arbalète en main, visée", "assassin_arbalete"),
                    ("Grenade fumigène", "assassin_fumigene"), Vide),
            },
            new Classe
            {
                Id = "viking", Nom = "Viking", Role = "Mêlée, zone", Arme = "Hache à deux mains",
                Description = "Chaque coup fait monter sa rage, que ses compétences consomment.",
                Teinte = Hex("#b3261e"), Embleme = "classe_viking", Jauge = JaugeClasse.Rage,
                Actions = Actions(("Hache", "viking_hache"), ("Attaque tournante (maintenue)", "viking_attaque_tournante"),
                    ("Rugissement", "viking_rugissement"), ("Saut percutant", "viking_saut_percutant")),
            },
            // Classes à venir (Wiki : interface.md, classes.md) : verrouillées, étiquette « Bientôt ».
            new Classe
            {
                Id = "druide", Nom = "Druide", Role = "Bientôt", Arme = "À venir", Verrouillee = true,
                Description = "Arme, rôle et compétences à venir.",
                Teinte = Hex("#8a6a48"), Embleme = "classe_druide", Jauge = JaugeClasse.Aucune,
                Actions = Actions(Vide, Vide, Vide, Vide),
            },
            new Classe
            {
                Id = "mecanicien", Nom = "Mécanicien", Role = "Bientôt", Arme = "À venir", Verrouillee = true,
                Description = "Arme, rôle et compétences à venir.",
                Teinte = Hex("#7a8088"), Embleme = "classe_mecanicien", Jauge = JaugeClasse.Aucune,
                Actions = Actions(Vide, Vide, Vide, Vide),
            },
        };

        /// Classes proposées : celles du jeu s'il les fournit, sinon le catalogue.
        public static IReadOnlyList<IClasseJouable> Proposees =>
            (DonneesUI.Commandes as IClassesJouables)?.Classes ?? Catalogue;

        /// Classe par identifiant ou par nom affiché (IEtatJoueur.Classe donne le nom).
        public static IClasseJouable TrouverParNom(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return null;
            foreach (var c in Proposees) if (c.Nom == nom || c.Id == nom) return c;
            foreach (var c in Catalogue) if (c.Nom == nom || c.Id == nom) return c;
            return null;
        }

        /// Icône de l'action `action` (« Gameplay/Skill1 ») de la classe, ou null.
        public static string IconeAction(IClasseJouable classe, string action)
        {
            if (classe == null || string.IsNullOrEmpty(action)) return null;
            foreach (var a in classe.Actions) if (a.Action == action) return string.IsNullOrEmpty(a.Nom) ? null : a.Icone;
            return null;
        }

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

        public static IClasseJouable Derniere => Jouable(DerniereJouee) ? Trouver(DerniereJouee) : Trouver(ParDefaut);

        /// Valider le choix : retient la classe et lance la partie solo.
        /// Vrai si la classe peut être jouée (connue et non verrouillée).
        public static bool Jouable(string classeId)
        {
            var c = Trouver(classeId);
            return c != null && !c.Verrouillee;
        }

        public static void Lancer(string classeId)
        {
            if (!Jouable(classeId)) return;
            DerniereJouee = classeId;
            if (DonneesUI.Commandes is IClassesJouables classes) classes.LancerSolo(classeId);
            else DonneesUI.Commandes?.LancerSolo();
        }
    }
}
