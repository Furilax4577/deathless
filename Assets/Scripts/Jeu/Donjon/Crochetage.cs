using System;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Mini-jeu de crochetage d'une serrure (03/10/2026, première version simple ; wiki : donjon.md, {à confirmer}). Le joueur
    /// maintient Interagir : l'aiguille monte ; relâché, elle retombe. Il la garde dans une zone qui se déplace sur la piste, le
    /// temps de remplir la jauge de réussite (elle se vide deux fois plus lentement hors de la zone). Un essai raté (temps
    /// écoulé) casse un crochet, sauf chance d'agilité ; trois essais au plus, et plus de crochet : la serrure résiste. La zone
    /// est plus grande avec l'agilité (l'assassin, à 6, en gagne 36 % ; le paladin, à 2, en perd 12 %) {à confirmer : la
    /// perception de l'assassin n'est que de 2 dans la répartition actuelle}. La serrure d'or ne se crochète jamais (l'appelant
    /// ne lance pas le mini-jeu). Pendant le mini-jeu le héros ne bouge ni n'agit (Heros) ; Esquive ou Saut l'annule sans casser
    /// de crochet, comme un coup reçu ou un pas de trop (crochetageDistanceMax). Local au joueur : solo et aperçu de la nouvelle
    /// carte (les portes à serrure n'existent que dans le donjon en terrasses, sans réseau pour l'instant).
    public class Crochetage : MonoBehaviour, IEtatCrochetage
    {
        enum Phase { Jeu, Pause, Fin }

        public static Crochetage Courant { get; private set; }
        /// Le héros est occupé par le mini-jeu (pas de déplacement, pas d'action) ; faux pendant le message de fin.
        public static bool Bloque => Courant != null && Courant.m_Phase != Phase.Fin;

        Heros m_Heros;
        EtatJoueur m_Etat;
        string m_Nom;
        int m_Difficulte;
        Vector3 m_Point;
        Action m_SurSucces;
        Phase m_Phase;
        float m_PhaseDepuis;
        int m_Essai;
        float m_Aiguille, m_Vitesse, m_Centre, m_Cible, m_Largeur, m_Progres, m_Temps;
        string m_Message = "";
        bool m_Echec, m_Reussi;
        float m_Pas;

        GameBalance B => GameBalance.Courant;

        /// Lance le mini-jeu (héros local). Faux, avec la raison, sans kit de crochetage ou si un autre mini-jeu est en cours.
        public static bool Commencer(Heros h, int difficulte, string nomSerrure, Vector3 point, Action surSucces, out string refus)
        {
            refus = null;
            if (Courant != null) return false;
            var j = h != null ? h.EtatJoueur : null;
            if (j == null || j.crochets <= 0) { refus = "Il faut un kit de crochetage."; return false; }
            var go = new GameObject("Crochetage");
            var c = go.AddComponent<Crochetage>();
            c.m_Heros = h; c.m_Etat = j; c.m_Nom = nomSerrure; c.m_Difficulte = Mathf.Clamp(difficulte, 1, 2);
            c.m_Point = point; c.m_SurSucces = surSucces;
            Courant = c;
            DonneesUI.Crochetage = c;
            h.Sante.Touche += c.SurTouche;
            c.NouvelEssai();
            AudioBank.Jouer(SonsDuJeu.CoffreCadenas, point, 0.7f);
            if (h.Partie != null) h.Partie.Journal("Crochetage : " + nomSerrure + " (difficulté " + difficulte + ", zone " + c.m_Largeur.ToString("F2") + ", " + j.crochets + " crochets)");
            return true;
        }

        void OnDestroy()
        {
            if (m_Heros != null && m_Heros.Sante != null) m_Heros.Sante.Touche -= SurTouche;
            if (Courant == this) Courant = null;
            if (ReferenceEquals(DonneesUI.Crochetage, this)) DonneesUI.Crochetage = null;
        }

        void SurTouche(InfoDegats info, float reel) { if (reel > 0f) Annuler("Crochetage interrompu."); }

        /// Esquive, saut, coup reçu, mort ou éloignement : le mini-jeu s'arrête, aucun crochet n'est cassé.
        public void Annuler(string message = "Crochetage abandonné.")
        {
            if (m_Phase == Phase.Fin) return;
            Terminer(false, message, false);
        }

        // ----------------------------------------------------------------- Règles

        /// Largeur de la zone pour ce héros : base de la serrure, plus l'agilité gagnée (au-dessus de 3).
        float LargeurZone()
        {
            var b = B;
            float baseZone = m_Difficulte >= 2 ? b.crochetageZoneDifficile : b.crochetageZoneFacile;
            float agi = Attributs.Valeur(m_Etat, Attribut.Agilite);
            return Mathf.Clamp(baseZone * (1f + (agi - 3f) * b.crochetageZoneParAgilite), 0.1f, 0.7f);
        }

        void NouvelEssai()
        {
            m_Phase = Phase.Jeu;
            m_PhaseDepuis = 0f;
            m_Largeur = LargeurZone();
            m_Aiguille = 0.08f;
            m_Vitesse = 0f;
            m_Progres = 0f;
            m_Temps = 0f;
            m_Centre = Mathf.Clamp(UnityEngine.Random.Range(0.35f, 0.75f), m_Largeur * 0.5f, 1f - m_Largeur * 0.5f);
            ChoisirCible();
            m_Message = "";
            m_Echec = false;
        }

        void ChoisirCible() => m_Cible = UnityEngine.Random.Range(m_Largeur * 0.5f, 1f - m_Largeur * 0.5f);

        void Update()
        {
            float dt = Time.deltaTime;
            if (m_Heros == null || !m_Heros.Vivant || !m_Heros.EnJeu || m_Heros.EnTransit) { Annuler("Crochetage interrompu."); }
            else if (m_Phase != Phase.Fin)
            {
                Vector3 d = m_Heros.transform.position - m_Point; d.y = 0f;
                if (d.magnitude > B.crochetageDistanceMax) Annuler("Vous vous êtes éloigné de la serrure.");
            }
            bool tenu = m_Heros != null && m_Heros.Entrees != null && m_Heros.Entrees.InteragirMaintenu;
            Avancer(dt, tenu);
        }

        /// Un pas du mini-jeu (appelé par Update ; les tests le font avancer à la main).
        public void Avancer(float dt, bool tenu)
        {
            m_PhaseDepuis += dt;
            if (m_Phase == Phase.Fin) { if (m_PhaseDepuis > 1.6f) Destroy(gameObject); return; }
            if (m_Phase == Phase.Pause)
            {
                if (m_PhaseDepuis >= 1.0f) NouvelEssai();
                return;
            }
            var b = B;
            // Zone : glisse vers une cible tirée au hasard sur la piste, puis en choisit une autre.
            float derive = m_Difficulte >= 2 ? b.crochetageDeriveDifficile : b.crochetageDeriveFacile;
            m_Centre = Mathf.MoveTowards(m_Centre, m_Cible, derive * dt);
            if (Mathf.Abs(m_Centre - m_Cible) < 0.005f) ChoisirCible();
            // Aiguille : monte tant qu'Interagir est maintenu, retombe sinon ; rebond léger en butée.
            m_Vitesse += (tenu ? b.crochetageMontee : -b.crochetageChute) * dt;
            m_Vitesse = Mathf.Clamp(m_Vitesse, -1.4f, 1.4f);
            m_Aiguille += m_Vitesse * dt;
            if (m_Aiguille <= 0f) { m_Aiguille = 0f; m_Vitesse = Mathf.Max(0f, m_Vitesse) * 0.4f; }
            else if (m_Aiguille >= 1f) { m_Aiguille = 1f; m_Vitesse = Mathf.Min(0f, m_Vitesse) * 0.4f; }
            // Jauge de réussite.
            float duree = Mathf.Max(0.3f, b.crochetageDureeReussite);
            if (Dans)
            {
                m_Progres += dt / duree;
                m_Pas -= dt;
                if (m_Pas <= 0f) { m_Pas = 0.45f; AudioBank.Jouer(SonsDuJeu.CoffreCadenas, m_Point, 0.3f, 0.2f); }
            }
            else m_Progres -= dt / duree * 0.5f;
            m_Progres = Mathf.Clamp01(m_Progres);
            if (m_Progres >= 1f) { Reussir(); return; }
            m_Temps += dt;
            if (m_Temps >= b.crochetageDureeEssai) EchouerEssai();
        }

        void Reussir()
        {
            m_Reussi = true;
            Terminer(true, m_Nom + " crochetée !", false);
        }

        void EchouerEssai()
        {
            var b = B;
            float agi = Attributs.Valeur(m_Etat, Attribut.Agilite);
            bool tient = UnityEngine.Random.value < Mathf.Clamp01((agi - 3f) * b.crochetageEconomieParAgilite);
            if (!tient) Inventaire.CasserCrochet(m_Etat);
            m_Essai++;
            m_Echec = true;
            AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.6f);
            if (m_Heros != null && m_Heros.Partie != null)
                m_Heros.Partie.Journal("Crochetage : essai " + m_Essai + " raté, " + (tient ? "le crochet tient bon" : "un crochet casse") + ", " + m_Etat.crochets + " restants");
            if (m_Essai >= Mathf.Max(1, b.crochetageEssais) || m_Etat.crochets <= 0)
            {
                Terminer(false, m_Etat.crochets <= 0 ? "Plus de crochets : la serrure résiste." : "La serrure résiste.", true);
                return;
            }
            m_Message = tient ? "Raté, mais le crochet tient bon." : "Le crochet casse !";
            m_Phase = Phase.Pause;
            m_PhaseDepuis = 0f;
        }

        void Terminer(bool succes, string message, bool echec)
        {
            m_Phase = Phase.Fin;
            m_PhaseDepuis = 0f;
            m_Message = message;
            m_Echec = echec || !succes;
            if (!succes) return;
            AudioBank.Jouer(SonsDuJeu.CoffreOuvert, m_Point, 0.8f);
            m_SurSucces?.Invoke();
        }

        // ----------------------------------------------------------------- IEtatCrochetage

        public bool Actif => true;
        public string Serrure => m_Nom;
        public float Aiguille => m_Aiguille;
        public float ZoneCentre => m_Centre;
        public float ZoneLargeur => m_Largeur;
        public float Progression => m_Progres;
        public float TempsRestant => Mathf.Clamp01(1f - m_Temps / Mathf.Max(1f, B.crochetageDureeEssai));
        public int Essai => Mathf.Min(m_Essai + 1, Mathf.Max(1, B.crochetageEssais));
        public int EssaisMax => Mathf.Max(1, B.crochetageEssais);
        public int Crochets => m_Etat != null ? m_Etat.crochets : 0;
        public bool Dans => m_Phase == Phase.Jeu && Mathf.Abs(m_Aiguille - m_Centre) <= m_Largeur * 0.5f;
        public string Message => m_Message;
        public bool MessageEchec => m_Echec;
        public bool Reussie => m_Reussi;
    }
}
