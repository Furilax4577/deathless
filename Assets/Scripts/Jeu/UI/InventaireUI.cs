using Deathless.UI.Donnees;

namespace Deathless.Jeu
{
    /// Source de l'inventaire pour l'interface (IInventaireJoueur, DonneesUI.Inventaire, posée par HudPresenter) : lit l'EtatJoueur
    /// du joueur local (potions, clés, crochets ; Inventaire) et son héros (la potion de mana n'est utilisable que par le Mage).
    public class InventaireUI : IInventaireJoueur
    {
        static EtatJoueur J => Partie.Instance != null ? Partie.Instance.JoueurLocal : null;
        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;

        public int Potions(int sorte) => Inventaire.Potions(J, (Potion)sorte);
        public int PotionsMax => GameBalance.Courant.potionsMaxParSorte;
        public bool PotionUtilisable(int sorte) => Inventaire.PotionUtilisable((Potion)sorte, H);
        public int Cles(int sorte) => Inventaire.Cles(J, (Cle)sorte);
        public int Crochets => J != null ? J.crochets : 0;
    }
}
