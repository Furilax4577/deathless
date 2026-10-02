using Deathless.Donjon.Terrasses;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Aperçu de la nouvelle carte seulement (Partie.Exploration, donjon en terrasses) : touche Interagir sur une porte
    /// à serrure ou sur le bouton mural d'une pièce secrète (sa paroi s'enfonce). Posé par DonjonJeu après la construction.
    /// Serrures (03/10/2026, clés du mécano et crochetage ; wiki : donjon.md) : une serrure de bronze, d'argent ou d'or
    /// s'ouvre en consommant la clé correspondante (à usage unique) ; sans clé, le message « Il faut une clé de bronze » ; si
    /// le joueur a un kit de crochetage et que la serrure est de bronze ou d'argent, le crochetage (mini-jeu, Crochetage)
    /// s'offre à la place ; la serrure simple ne se crochète que (kit requis) ; l'or ne se crochète jamais. Le réseau des
    /// ouvertures reste à faire (l'aperçu est solo) : PorteDonjon.Ouvrir() et DeclencheurDonjon.Activer() sont les seules entrées.
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
            if (h == null || h.Distant || h.EnTransit || !Disponible || Crochetage.Bloque) return null;
            Vector3 c = Centre;
            Vector3 d = h.transform.position - c;
            if (d.y < -2.5f || d.y > 1f) return null;
            d.y = 0f;
            distance = d.magnitude;
            if (distance > 3.0f) return null;
            if (porte == null) return "Appuyer sur la pierre";
            switch (Choix(h))
            {
                case Moyen.Cle: return "Ouvrir la porte (" + NomCle(porte.serrure) + ")";
                case Moyen.Crochets: return "Crocheter la serrure";
                default: return porte.serrure == Serrure.Crochetable ? "Serrure simple : kit de crochetage" : "Porte verrouillée : " + NomCle(porte.serrure);
            }
        }

        public override void Interagir(Heros h)
        {
            if (Crochetage.Bloque) return;
            if (porte != null)
            {
                if (porte.Ouverte) return;
                var j = h != null ? h.EtatJoueur : null;
                switch (Choix(h))
                {
                    case Moyen.Cle:
                        Inventaire.Retirer(j, CleDe(porte.serrure));
                        P(h)?.Journal("Porte : " + NomCle(porte.serrure) + " tournée (reste " + Inventaire.Cles(j, CleDe(porte.serrure)) + ")");
                        Ouvrir(true);
                        break;
                    case Moyen.Crochets:
                        Crochetage.Commencer(h, porte.DifficulteCrochetage, NomSerrure(porte.serrure), Centre, () => Ouvrir(false), out string refus);
                        if (refus != null) Dire(refus);
                        break;
                    default:
                        Dire(porte.serrure == Serrure.Crochetable ? "Il faut un kit de crochetage." : "Il faut une " + NomCle(porte.serrure) + ".");
                        AudioBank.Jouer(SonsDuJeu.CoffreCadenas, Centre, 0.6f);
                        break;
                }
            }
            else if (bouton != null && !bouton.Actif)
            {
                bouton.Activer();
                AudioBank.Jouer(SonsDuJeu.CoffreCadenas, Centre, 0.8f);
            }
        }

        // ----------------------------------------------------------------- Serrures

        enum Moyen { Aucun, Cle, Crochets }

        /// Comment ce héros peut ouvrir la porte : par la clé de sa serrure, à défaut par le crochetage (kit, serrure simple, de
        /// bronze ou d'argent), sinon aucun moyen.
        Moyen Choix(Heros h)
        {
            var j = h != null ? h.EtatJoueur : null;
            var s = porte.serrure;
            if (s == Serrure.Bronze || s == Serrure.Argent || s == Serrure.Or)
                if (Inventaire.Cles(j, CleDe(s)) > 0) return Moyen.Cle;
            if (s != Serrure.Or && j != null && j.crochets > 0) return Moyen.Crochets;
            return Moyen.Aucun;
        }

        static Cle CleDe(Serrure s) => s == Serrure.Or ? Cle.Or : s == Serrure.Argent ? Cle.Argent : Cle.Bronze;

        /// « clé de bronze » (Il faut une clé de bronze).
        static string NomCle(Serrure s) => s == Serrure.Or ? "clé d’or" : s == Serrure.Argent ? "clé d’argent" : "clé de bronze";

        static string NomSerrure(Serrure s) => s == Serrure.Or ? "Serrure d’or" : s == Serrure.Argent ? "Serrure d’argent" : s == Serrure.Bronze ? "Serrure de bronze" : "Serrure simple";

        void Ouvrir(bool sonCoffre)
        {
            if (porte == null || porte.Ouverte) return;
            porte.Ouvrir();
            if (sonCoffre) AudioBank.Jouer(SonsDuJeu.CoffreOuvert, Centre, 0.8f);   // le crochetage joue son propre son de réussite
        }

        static Partie P(Heros h) => h != null ? h.Partie : null;

        static void Dire(string texte) => DonjonJeu.Instance?.Annoncer(texte, 3.5f);
    }
}
