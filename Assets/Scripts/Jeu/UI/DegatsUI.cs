using System;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Source des chiffres de dégâts flottants pour l'interface (IDegatsSource, DonneesUI.Degats, posée et actualisée
    /// image par image par HudPresenter). S'abonne une seule fois aux événements globaux de Sante (AnyTouche,
    /// AnyIntercepte, AnyImmunise, AnySoigne : Assets/Scripts/Jeu/Sante.cs), qui couvrent héros et squelettes quel que
    /// soit leur nombre ou leur instant d'apparition. La vie de Nyxessa n'est en revanche pas un vrai Sante.Encaisser
    /// côté client (répliquée par le réseau, Docs/reseau.md) : elle est surveillée image par image dans Actualiser().
    /// Filtre (interface.md, « Chiffres de dégâts ») : chacun ne voit que ses propres coups portés, ce qu'il reçoit
    /// lui-même, et les dégâts subis par Nyxessa (partagés). Option OptionsJoueur.AfficherDegats coupée : plus aucun
    /// événement n'est levé (les barres de vie, elles, restent).
    public sealed class DegatsUI : IDegatsSource
    {
        /// Identifiant réservé (aucun héros ni squelette ne le porte) pour cumuler les chiffres de dégâts à Nyxessa.
        public const int CleNyxessa = -1;

        public event Action<EvenementDegat> Degat;

        static Heros HerosLocal => Partie.Instance != null ? Partie.Instance.HerosLocal : null;
        static int IdLocal { get { var h = HerosLocal; return h != null ? h.Id : 1; } }

        float m_VieNyxessaVue = -1f;

        public DegatsUI()
        {
            Sante.AnyTouche += OnAnyTouche;
            Sante.AnyIntercepte += OnAnyIntercepte;
            Sante.AnyImmunise += OnAnyImmunise;
            Sante.AnySoigne += OnAnySoigne;
        }

        /// À détacher quand la source cesse d'être utilisée (HudPresenter.OnDisable), pour ne pas garder d'abonnement
        /// à ces événements globaux au-delà de la partie.
        public void Detacher()
        {
            Sante.AnyTouche -= OnAnyTouche;
            Sante.AnyIntercepte -= OnAnyIntercepte;
            Sante.AnyImmunise -= OnAnyImmunise;
            Sante.AnySoigne -= OnAnySoigne;
        }

        /// Appelé une fois par image par HudPresenter.
        public void Actualiser()
        {
            var p = Partie.Instance;
            if (!OptionsJoueur.AfficherDegats || p == null || p.nyxessa == null)
            {
                m_VieNyxessaVue = -1f;
                return;
            }
            float pv = p.Etat.nyxessa.pv;
            if (m_VieNyxessaVue >= 0f && pv < m_VieNyxessaVue - 0.01f)
                Emettre(new EvenementDegat
                {
                    Point = p.nyxessa.transform.position + Vector3.up * 2.2f,
                    Montant = m_VieNyxessaVue - pv,
                    Type = TypeChiffreDegat.Nyxessa,
                    Continu = false,
                    CleCible = CleNyxessa,
                });
            m_VieNyxessaVue = pv;
        }

        void OnAnyTouche(Sante s, InfoDegats info, float reel)
        {
            if (!OptionsJoueur.AfficherDegats || reel <= 0f) return;
            var local = HerosLocal;
            if (local != null && s == local.Sante)
            {
                // Dégâts reçus par le héros local (rouge).
                Emettre(new EvenementDegat
                {
                    Point = info.point, Montant = reel, Type = TypeChiffreDegat.Recu, Continu = info.continu, CleCible = local.Id
                });
                return;
            }
            if (s.equipe == Equipe.Relique) return;   // Nyxessa (et le sorcier) : dégâts partagés, traités dans Actualiser()
            if (info.sourceId != IdLocal || info.equipeSource != Equipe.Heros) return;   // pas mon coup
            var type = info.critique ? TypeChiffreDegat.Critique : info.continu ? TypeChiffreDegat.Brulure : TypeChiffreDegat.Normal;
            Emettre(new EvenementDegat { Point = info.point, Montant = reel, Type = type, Continu = info.continu, CleCible = s.GetInstanceID() });
            // Exécution de l'assassin (27/09/2026) : le mot « Exécuté », même style que « Paré », au-dessus du chiffre.
            if (info.execution)
                Emettre(new EvenementDegat { Point = info.point + Vector3.up * 0.45f, Mot = "Exécuté", Type = TypeChiffreDegat.Mot, CleCible = s.GetInstanceID() });
        }

        void OnAnyIntercepte(Sante s, InfoDegats info, Interception r)
        {
            if (!OptionsJoueur.AfficherDegats) return;
            var local = HerosLocal;
            if (local == null || s != local.Sante) return;   // seul le joueur local voit ses parades et blocages
            string mot = r == Interception.Pare ? "Paré" : local.EstInvulnerable ? "Esquivé" : "Bloqué";
            Emettre(new EvenementDegat { Point = info.point, Mot = mot, Type = TypeChiffreDegat.Mot, CleCible = local.Id });
        }

        void OnAnyImmunise(Sante s, InfoDegats info)
        {
            if (!OptionsJoueur.AfficherDegats) return;
            var local = HerosLocal;
            if (local == null || s != local.Sante) return;
            Emettre(new EvenementDegat { Point = info.point, Mot = "Immunisé", Type = TypeChiffreDegat.Mot, CleCible = local.Id });
        }

        void OnAnySoigne(Sante s, float reel)
        {
            if (!OptionsJoueur.AfficherDegats || reel <= 0f) return;
            var local = HerosLocal;
            if (local == null || s != local.Sante) return;
            Emettre(new EvenementDegat
            {
                Point = local.transform.position + Vector3.up * 1.8f, Montant = reel, Type = TypeChiffreDegat.Soin, CleCible = local.Id
            });
        }

        void Emettre(EvenementDegat e) => Degat?.Invoke(e);
    }
}
