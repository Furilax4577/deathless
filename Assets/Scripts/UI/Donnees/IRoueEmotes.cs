using System.Collections.Generic;

namespace Deathless.UI.Donnees
{
    /// Roue à emotes du joueur local (calque du HUD). Posée par le jeu dans DonneesUI.RoueEmotes quand le héros local
    /// existe ; lue à chaque image. Le jeu tient tout : ouverture (touche maintenue), secteur pointé (stick droit ou
    /// souris), lancement au relâchement. La roue ne fait qu'afficher.
    public interface IRoueEmotes
    {
        /// Touche maintenue : la roue est affichée (la caméra ne tourne plus).
        bool Ouverte { get; }
        /// Les emotes, dans l'ordre des secteurs : le premier en haut, puis dans le sens des aiguilles d'une montre.
        IReadOnlyList<IEmoteRoue> Emotes { get; }
        /// Secteur pointé (index dans Emotes), ou -1 au centre : relâcher n'y lance rien.
        int Pointee { get; }
    }

    /// Une emote de la roue.
    public interface IEmoteRoue
    {
        /// Nom affiché (« Salut », « Boire un coup »…).
        string Nom { get; }
        /// Identifiant de l'icône (IconesUI, ex. « emote_salut ») ; absente : le nom seul.
        string Icone { get; }
    }
}
