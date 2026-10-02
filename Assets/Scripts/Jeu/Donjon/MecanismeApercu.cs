using Deathless.Donjon.Terrasses;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Aperçu de la nouvelle carte seulement (Partie.Exploration, donjon en terrasses) : touche Interagir sur une porte
    /// à serrure (elle s'ouvre, sans clé ni crochetage) ou sur le bouton mural d'une pièce secrète (sa paroi s'enfonce).
    /// Posé par DonjonJeu après la construction ; le vrai gameplay (clés au mécano, crochetage, réplication réseau des
    /// ouvertures) reste à faire : PorteDonjon.Ouvrir() et DeclencheurDonjon.Activer() sont les seules entrées.
    public class MecanismeApercu : PointInteraction
    {
        public PorteDonjon porte;
        public DeclencheurDonjon bouton;

        /// Point d'interaction : milieu de la porte (sa charnière est sur un montant) ou le bouton lui-même.
        Vector3 Centre
        {
            get
            {
                if (porte != null) return porte.transform.TransformPoint(new Vector3(PlanTerrasses.ArcheLargeur * 0.5f, 1.2f, 0f));
                return bouton != null ? bouton.transform.position : transform.position;
            }
        }

        bool Disponible => porte != null ? !porte.Ouverte : bouton != null && !bouton.Actif;

        public override string Invite(Heros h, out float distance)
        {
            distance = float.MaxValue;
            if (h == null || h.Distant || h.EnTransit || !Disponible) return null;
            Vector3 c = Centre;
            Vector3 d = h.transform.position - c;
            if (d.y < -2.5f || d.y > 1f) return null;
            d.y = 0f;
            distance = d.magnitude;
            if (distance > 3.0f) return null;
            if (porte != null) return "Ouvrir la porte (aperçu, sans clé)";
            return "Appuyer sur la pierre";
        }

        public override void Interagir(Heros h)
        {
            if (porte != null)
            {
                if (porte.Ouverte) return;
                porte.Ouvrir();
                AudioBank.Jouer(SonsDuJeu.CoffreOuvert, Centre, 0.8f);
            }
            else if (bouton != null && !bouton.Actif)
            {
                bouton.Activer();
                AudioBank.Jouer(SonsDuJeu.CoffreCadenas, Centre, 0.8f);
            }
        }
    }
}
