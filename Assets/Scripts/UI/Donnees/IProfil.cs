using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Profil du joueur local (sens UI → jeu et futur réseau) : son pseudo, demandé au premier lancement et modifiable
    /// dans Options > Jeu. Lu par DonneesUI.Profil (ProfilJoueur.Local).
    public interface IProfilJoueur
    {
        /// Pseudo enregistré (« Joueur » tant qu'aucun n'a été choisi).
        string Pseudo { get; }

        /// Vrai si un pseudo a été choisi (sinon l'écran « Pseudo » s'affiche avant le menu principal).
        bool PseudoDefini { get; }

        /// Le pseudo vient de changer (nouveau pseudo).
        event Action<string> PseudoChange;
    }

    /// Pseudo du joueur local, enregistré dans les PlayerPrefs (clé Deathless.Pseudo). Règle : 3 à 16 caractères après
    /// avoir rogné les espaces du début et de la fin ; lettres (accents compris), chiffres, tirets et espaces.
    public static class ProfilJoueur
    {
        public const string ClePrefs = "Deathless.Pseudo";
        public const string ParDefaut = "Joueur";
        public const int LongueurMin = 3, LongueurMax = 16;

        static readonly Regex s_Autorises = new Regex(@"^[\p{L}\p{Nd} \-]+$");

        sealed class Profil : IProfilJoueur
        {
            public string Pseudo => PlayerPrefs.GetString(ClePrefs, ParDefaut);
            public bool PseudoDefini => !string.IsNullOrEmpty(PlayerPrefs.GetString(ClePrefs, ""));
            public event Action<string> PseudoChange;
            public void Signaler(string p) => PseudoChange?.Invoke(p);
        }

        static readonly Profil s_Local = new Profil();

        public static IProfilJoueur Local => s_Local;

        /// Espaces rognés au début et à la fin, espaces multiples réduits à un seul.
        public static string Normaliser(string saisie) =>
            saisie == null ? "" : Regex.Replace(saisie.Trim(), @"\s+", " ");

        /// Vrai si le pseudo (normalisé) est valable ; sinon `raison` dit pourquoi (texte affiché sous le champ).
        public static bool Valider(string saisie, out string raison)
        {
            var p = Normaliser(saisie);
            if (p.Length < LongueurMin) { raison = "Au moins " + LongueurMin + " caractères."; return false; }
            if (p.Length > LongueurMax) { raison = "Au plus " + LongueurMax + " caractères."; return false; }
            if (!s_Autorises.IsMatch(p)) { raison = "Lettres, chiffres, tirets et espaces seulement."; return false; }
            raison = null;
            return true;
        }

        /// Enregistre le pseudo s'il est valable (normalisé). Renvoie faux sinon.
        public static bool Definir(string saisie)
        {
            if (!Valider(saisie, out _)) return false;
            var p = Normaliser(saisie);
            PlayerPrefs.SetString(ClePrefs, p);
            PlayerPrefs.Save();
            s_Local.Signaler(p);
            return true;
        }
    }
}
