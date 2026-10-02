namespace Deathless.Jeu
{
    /// Sons des portes des bâtiments de la carte v5 (03/10/2026, PiecesBatiments) : ids du catalogue (Wiki/data/sons.json), le premier
    /// présent joue (même principe que SonsDuJeu). Les deux existaient déjà dans le catalogue (Kenney, RPG Audio, CC0, à l'origine
    /// « disponible » : portes des maisons ou du donjon) : ouverture (2 variantes, au moment où le héros touche Interagir) et
    /// fermeture (4 variantes, à l'arrivée, la porte se referme derrière lui).
    public static class SonsPortes
    {
        public static readonly string[] PorteOuvre = { "kenney_rpg_dooropen" };
        public static readonly string[] PorteFerme = { "kenney_rpg_doorclose" };
    }
}
