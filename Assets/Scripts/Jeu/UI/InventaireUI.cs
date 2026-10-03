using Deathless.UI.Donnees;
using UnityEngine;

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

        // Menu du personnage : 0-2 potions, 3-5 clés, 6 kit de crochetage.
        static readonly string[] s_Icones = { "commun_potion_soin", "commun_potion_mana", "commun_potion_endurance", "commun_cle_bronze", "commun_cle_argent", "commun_cle_or", "commun_crochets" };
        static ArticleBoutique Article(int i) => i < 3 ? Inventaire.ArticleDe((Potion)i) : i < 6 ? Inventaire.ArticleDe((Cle)(i - 3)) : ArticleBoutique.KitCrochetage;

        public int NbObjets => InventaireTaille.Objets;
        public string ObjetNom(int i) => Inventaire.Nom(Article(i));
        public string ObjetDescription(int i) => Inventaire.Description(Article(i));
        public string ObjetIcone(int i) => s_Icones[Mathf.Clamp(i, 0, s_Icones.Length - 1)];
        public int ObjetQuantite(int i) => Inventaire.Quantite(J, Article(i));
        public int ObjetMax(int i) => Inventaire.Max(Article(i));
        public bool ObjetUtilisable(int i) => i != 1 || Inventaire.PotionUtilisable(Potion.Mana, H);
    }
}
