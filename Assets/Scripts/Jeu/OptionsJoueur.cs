using UnityEngine;

namespace Deathless.Jeu
{
    /// Options locales du joueur qui n'ont pas encore leur case dans l'écran Options (UI Toolkit) : un simple
    /// interrupteur dans les PlayerPrefs, par profil (comme le jeton du joueur anonyme du réseau). À reporter dans
    /// l'écran Options si Quentin le demande.
    public static class OptionsJoueur
    {
        const string ClePrefixe = "Deathless.Option.";

        /// Relevage du Renversé (wiki : statuts.md, 26/09/2026) : faux = marteler Saut, vrai = maintenir Saut enfoncé.
        public static bool RelevageMaintenir
        {
            get => PlayerPrefs.GetInt(ClePrefixe + "RelevageMaintenir", 0) != 0;
            set => PlayerPrefs.SetInt(ClePrefixe + "RelevageMaintenir", value ? 1 : 0);
        }
    }
}
