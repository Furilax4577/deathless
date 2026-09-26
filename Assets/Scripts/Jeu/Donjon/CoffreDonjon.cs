using Deathless.Donjon;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Coffre du donjon (grand coffre ou coffre) : touche Interagir à côté ; le cadenas s'ouvre, le couvercle bascule et
    /// l'or passe au joueur (porté jusqu'au retour par le portail). Posé par DonjonJeu sur le repère du butin ;
    /// l'autorité décide (DonjonJeu.Accorder).
    public class CoffreDonjon : PointInteraction
    {
        DonjonRepere m_Repere;

        DonjonRepere Repere => m_Repere != null ? m_Repere : (m_Repere = GetComponent<DonjonRepere>());

        public override string Invite(Heros h, out float distance)
        {
            distance = float.MaxValue;
            var dj = DonjonJeu.Instance;
            var r = Repere;
            if (dj == null || r == null || h == null || h.Distant || h.EnTransit || dj.ButinPris(r.index)) return null;
            Vector3 d = h.transform.position - transform.position;
            if (Mathf.Abs(d.y) > 1.5f) return null;
            d.y = 0f;
            distance = d.magnitude;
            if (distance > GameBalance.Courant.distanceCoffre) return null;
            // Ouverture gratuite (Quentin, 26/09/2026 : jamais d'or pour ouvrir ; plus tard, peut-être une clé). Pas de
            // montant dans l'invite : « (120 or) » se lisait comme un prix.
            return r.butin == TypeButin.GrandCoffre ? "Ouvrir le grand coffre" : "Ouvrir le coffre";
        }

        public override void Interagir(Heros h)
        {
            var dj = DonjonJeu.Instance;
            if (dj != null && Repere != null) dj.DemanderButin(Repere.index);
        }
    }
}
