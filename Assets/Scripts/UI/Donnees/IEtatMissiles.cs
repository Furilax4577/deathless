namespace Deathless.UI.Donnees
{
    /// Missiles de Nyxessa, pour le compteur du HUD (en haut, à droite de la barre de Nyxessa). Facultative : à implémenter
    /// par l'objet enregistré comme IEtatPartie (trouvé par DonneesUI.Partie as IEtatMissiles) ; sans elle, le compteur
    /// est masqué.
    public interface IEtatMissiles
    {
        /// Missiles prêts à partir (stock).
        int MissilesDisponibles { get; }
        /// Stock maximal du palier acheté à la relique.
        int MissilesMax { get; }
        /// Recharge du prochain missile, de 0 (vient de commencer) à 1 (arrive). Sans objet quand le stock est plein
        /// (1 par convention) : le HUD allume alors l'icône entière.
        float ChargeProchainMissile { get; }
    }
}
