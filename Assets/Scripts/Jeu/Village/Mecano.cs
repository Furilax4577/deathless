namespace Deathless.Jeu
{
    /// Mécano (vendeur traditionnel) : clés de bronze, d'argent et d'or, kit de crochetage.
    public class Mecano : BoutiqueVillage
    {
        static readonly ArticleBoutique[] s_Catalogue = { ArticleBoutique.CleBronze, ArticleBoutique.CleArgent, ArticleBoutique.CleOr, ArticleBoutique.KitCrochetage };
        protected override ArticleBoutique[] Catalogue => s_Catalogue;
        protected override string TitreBoutique => "Mécano";
        protected override string InviteBoutique => "Mécano : acheter";
        protected override string SousTitreBoutique => "Clés à usage unique et kit de crochetage. L’or est pris dans la caisse commune.";
        protected override string RefusNuit => "Le mécano ne vend que de jour.";
    }
}
