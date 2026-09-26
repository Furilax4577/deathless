using UnityEngine;

namespace Deathless.Jeu
{
    /// Passage d'un portail par la touche Interagir (E, X, Carré) : « Entrer dans le donjon » au portail du village (le
    /// jour, portail ouvert), « Revenir au village » au portail de retour du donjon (Quentin, 26/09/2026 : on n'entre
    /// plus en marchant dedans). Posé par DonjonJeu sur les deux portails ; les conditions, le passage et le dépôt de
    /// l'or sont dans DonjonJeu (InvitePortail, Passer).
    public class PassagePortail : PointInteraction
    {
        [Tooltip("Faux : portail du village (vers le donjon). Vrai : portail de retour du donjon (vers le village).")]
        public bool retour;

        public override string Invite(Heros h, out float distance)
        {
            distance = float.MaxValue;
            var dj = DonjonJeu.Instance;
            return dj != null ? dj.InvitePortail(retour, h, out distance) : null;
        }

        public override void Interagir(Heros h)
        {
            var dj = DonjonJeu.Instance;
            if (dj != null) dj.Passer(retour, h);
        }
    }
}
