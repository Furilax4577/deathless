using System;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Fiche d'un type de statut : nom, icône (ArtSources/Icones/generer_statuts.py), règle de cumul, effet lisible.
    public sealed class DefinitionStatut
    {
        public TypeStatut type;
        public string id;
        public string nom;
        public string icone;
        public RegleCumul regle;
        /// Affliction (liseré rouge) ou bienfait (liseré or).
        public bool nefaste = true;
        /// Effet en clair, avec les valeurs du statut (infobulle du menu du personnage).
        public Func<Statut, string> effet;
    }

    /// Catalogue des statuts (wiki : statuts.md). Un nouveau statut : une valeur dans TypeStatut, une fiche ici, une
    /// icône « statut_<id> » dans generer_statuts.py, puis son effet là où il agit.
    public static class CatalogueStatuts
    {
        public static readonly DefinitionStatut[] Tous =
        {
            new DefinitionStatut
            {
                type = TypeStatut.Brulure, id = "brulure", nom = "Brûlure", icone = "statut_brulure", regle = RegleCumul.Rafraichir,
                effet = s => Mathf.RoundToInt(s.intensite) + " dégâts par seconde. Chaque nouveau coup de feu relance la durée.",
            },
            new DefinitionStatut
            {
                type = TypeStatut.Ralenti, id = "ralenti", nom = "Ralenti", icone = "statut_ralenti", regle = RegleCumul.Prolonger,
                effet = s => "Déplacements ralentis de " + Mathf.RoundToInt(s.intensite * 100f) + " %."
                    + (s.Permanent ? " Tant que vous êtes dans l’eau." : ""),
            },
            new DefinitionStatut
            {
                type = TypeStatut.Etourdi, id = "etourdi", nom = "Étourdi", icone = "statut_etourdi", regle = RegleCumul.Prolonger,
                effet = s => "Ne peut ni se déplacer, ni attaquer.",
            },
            new DefinitionStatut
            {
                type = TypeStatut.Ivresse, id = "ivresse", nom = "Ivresse", icone = "statut_ivresse", regle = RegleCumul.Prolonger,
                nefaste = false,
                effet = s => "La tête tourne : la vue tangue et la démarche hésite. Attaques et visée inchangées.",
            },
            new DefinitionStatut
            {
                type = TypeStatut.Provoque, id = "provoque", nom = "Provoqué", icone = "statut_provoque", regle = RegleCumul.Remplacer,
                effet = s => "S’acharne sur le héros qui l’a provoqué, avant Nyxessa.",
            },
            new DefinitionStatut
            {
                type = TypeStatut.Renverse, id = "renverse", nom = "Renversé", icone = "statut_renverse", regle = RegleCumul.Remplacer,
                effet = s => "Tombe à la renverse puis se relève, sans contrôle. Marteler Saut accélère le relevé (jusqu’à moitié moins).",
            },
        };

        public static DefinitionStatut De(TypeStatut type)
        {
            for (int i = 0; i < Tous.Length; i++) if (Tous[i].type == type) return Tous[i];
            return null;
        }

        public static string Nom(TypeStatut type) => De(type)?.nom ?? type.ToString();
        public static string Icone(TypeStatut type) => De(type)?.icone;
        public static string Effet(Statut s) { var d = De(s.type); return d != null && d.effet != null ? d.effet(s) : ""; }
        public static bool Nefaste(TypeStatut type) => De(type)?.nefaste ?? true;

        /// Source en clair : pseudo et classe du joueur, « Chute », « Taverne », « Eau du donjon »…
        public static string Source(Statut s)
        {
            switch (s.origine)
            {
                case OrigineStatut.Joueur:
                {
                    var j = Partie.Instance != null && s.sourceId > 0 ? Partie.Instance.Joueur(s.sourceId) : null;
                    if (j == null) return s.sourceId > 0 ? "Joueur " + s.sourceId : "Un joueur";
                    return string.IsNullOrEmpty(j.classe) ? j.nom : j.nom + " (" + j.classe + ")";
                }
                case OrigineStatut.Ennemi: return "Un ennemi";
                case OrigineStatut.Chute: return "Chute";
                case OrigineStatut.Taverne: return "Taverne";
                case OrigineStatut.Eau: return "Eau du donjon";
                default: return "Inconnue";
            }
        }
    }
}
