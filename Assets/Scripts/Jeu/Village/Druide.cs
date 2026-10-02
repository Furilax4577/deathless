namespace Deathless.Jeu
{
    /// Druide : potions de santé, de mana (Mage seulement) et d'endurance.
    public class Druide : BoutiqueVillage
    {
        static readonly ArticleBoutique[] s_Catalogue = { ArticleBoutique.PotionSante, ArticleBoutique.PotionMana, ArticleBoutique.PotionEndurance };
        protected override ArticleBoutique[] Catalogue => s_Catalogue;
        protected override string TitreBoutique => "Druide";
        protected override string InviteBoutique => "Druide : potions";
        protected override string SousTitreBoutique => "Potions à boire à la croix directionnelle ou aux touches 1, 2, 3. L’or est pris dans la caisse commune.";
        protected override string RefusNuit => "Le druide ne vend que de jour.";
    }
}
