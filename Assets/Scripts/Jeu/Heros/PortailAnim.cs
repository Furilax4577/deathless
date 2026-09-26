namespace Deathless.Jeu
{
    /// Paramètres de l'Animator pour l'arrivée par un portail (DonjonJeu.Transit, Heros.DeclencherPortail) : sous-machine
    /// « Portail » commune ajoutée aux contrôleurs des 5 classes par les builders (JeuBuilder.ControleurPaladin,
    /// ClassesBuilder, EmotesBuilder.AjouterPortailArrivee — même modèle que la sous-machine « Emotes »).
    ///
    /// - **Déclencheur** « PortailArrivee » et **booléen** « PortailAir » : vrai joue `Spawn_Air` (chute du ciel, arrivée
    ///   au donjon), faux joue `Spawn_Ground` (sortie du sol, retour au village et rappel de Nyxessa). Choix réversible
    ///   (Wiki : portail.md, {décidé} 26/09/2026).
    /// - Passe par Heros.Declencher (NetworkAnimator, comme les emotes) : les autres postes voient le même clip sur la
    ///   marionnette, sans appel supplémentaire (DonjonJeu.TransitDistant se contente de synchroniser le corps visible).
    /// - Les deux clips durent 1,30 s (Wiki : animations.md) ; le héros reste sans contrôle (Heros.EnTransit) jusqu'à la
    ///   fin, DonjonJeu.Transit attend `DureeClip` avant de le relâcher.
    public static class PortailAnim
    {
        public const string ParamDeclencheur = "PortailArrivee", ParamAir = "PortailAir";
        /// Étiquette commune des deux états (utile pour les tests, comme EmotesHeros.TagEmote).
        public const string TagEtat = "PortailArrivee";
        public const string EtatAir = "PortailSpawnAir", EtatSol = "PortailSpawnGround";
        public const float DureeClip = 1.30f;
    }
}
