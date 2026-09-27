using UnityEngine;

namespace Deathless.Jeu
{
    /// Options locales du joueur, sauvegardées par un simple interrupteur dans les PlayerPrefs (comme le jeton du
    /// joueur anonyme du réseau). `RelevageMaintenir` et `AfficherDegats` ont leur case dans l'écran Options (onglet
    /// Jeu, `Assets/Scripts/UI/Ecrans/EcransMenus.cs` : `EcranOptions`).
    public static class OptionsJoueur
    {
        const string ClePrefixe = "Deathless.Option.";

        /// Relevage du Renversé (wiki : statuts.md, 26/09/2026) : faux = marteler Saut, vrai = maintenir Saut enfoncé.
        public static bool RelevageMaintenir
        {
            get => PlayerPrefs.GetInt(ClePrefixe + "RelevageMaintenir", 0) != 0;
            set => PlayerPrefs.SetInt(ClePrefixe + "RelevageMaintenir", value ? 1 : 0);
        }

        /// Chiffres de dégâts flottants (wiki : interface.md, 26/09/2026), activés par défaut. Lu par
        /// Deathless.Jeu.DegatsUI avant de lever le moindre événement (option coupée : les barres de vie restent).
        public static bool AfficherDegats
        {
            get => PlayerPrefs.GetInt(ClePrefixe + "AfficherDegats", 1) != 0;
            set => PlayerPrefs.SetInt(ClePrefixe + "AfficherDegats", value ? 1 : 0);
        }
    }
}
