using UnityEngine;

namespace Deathless.Jeu
{
    /// Onde de choc lente du Fracas (Morgrim massue, décidé le 26/09/2026 ; wiki : ennemis.md, Docs/reseau.md) : un
    /// front qui part du point d'impact et s'étend à vitesse lente et constante (GameBalance.morgrimMassueFracasOnde*),
    /// assez lentement pour qu'on saute par-dessus. Non parable (pas de garde ni de parade), non esquivable (la roulade
    /// et ses frames d'invulnérabilité n'y font rien) : seul un saut au bon instant évite les dégâts.
    ///
    /// Sert aussi au Coup écrasé commun aux deux versions (30/09/2026, onde plus courte autour de lui ; dégâts passés à
    /// la création).
    ///
    /// Visuel propre (30/09/2026, au lieu du prefab d'onde du Golem réglé en onde lente) : anneau épais de gemmes au ras
    /// du sol, palette Terre, dans le langage commun de Morgrim (AnneauGemmesComp de MorgrimEffets, front linéaire qui
    /// suit exactement la distance jugée ici, éclats de poussière projetés) ; démarré au même instant réseau sur chaque
    /// poste (JouerDepuis).
    ///
    /// Réseau : avec la latence, l'hôte verrait les sauts des clients en retard. Le jugement (au sol ou en l'air) se
    /// fait donc côté client propriétaire, sur l'onde qu'il voit (départ et vitesse répliqués par
    /// EnnemiReseau.DiffuserOndeMorgrim, avec l'heure de départ réseau) : chaque poste calcule le même front que
    /// l'hôte malgré la latence (NetworkManager.ServerTime). En solo, ou pour l'hôte lui-même : dégâts appliqués tout
    /// de suite (l'hôte fait déjà foi). Pour un client : signalé à l'hôte (EnnemiReseau.SignalerOndeTouchee), qui
    /// vérifie la vraisemblance avant d'appliquer. Chaque héros n'est touché qu'une fois par onde (marqué ici pour son
    /// propre héros local ; une fois par joueur côté hôte pour la vérification).
    public class OndeChocLente : MonoBehaviour
    {
        Vector3 m_Centre;
        float m_Vitesse, m_RayonMax, m_LargeurBande, m_Depart, m_Duree, m_Degats;
        Deathless.Reseau.EnnemiReseau m_Reseau;
        bool m_Touche;

        /// Crée l'onde à `centre` : front lent, visuel en gemmes Terre, jugement du héros local. Appelé chez l'hôte
        /// (MorgrimMassue.FaireFracas, MorgrimVariant.FaireEcrase) et chez chaque client (MorgrimVariant.RecevoirOndeDistante),
        /// avec la même heure de départ réseau `depart`.
        public static GameObject Creer(Vector3 centre, float vitesse, float rayonMax, float largeurBande, float degats,
            float depart, Deathless.Reseau.EnnemiReseau reseau)
        {
            var go = new GameObject("OndeChocLente");
            go.transform.position = centre + Vector3.up * 0.02f;
            float duree = rayonMax / Mathf.Max(0.1f, vitesse);
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat != null)
            {
                // Anneau large et lent, à hauteur de chevilles : « ça se lit comme à sauter » (décision de Quentin).
                MorgrimEffets.MateriauGemmes = mat;
                var anneau = go.AddComponent<AnneauGemmesComp>();
                anneau.theme = VfxTheme.Terre;
                anneau.lineaire = true;
                anneau.rayonDepart = 0f;
                anneau.rayonMax = rayonMax;
                anneau.duree = duree;
                anneau.largeurDepart = Mathf.Max(largeurBande, 1f);
                anneau.largeurFin = Mathf.Max(largeurBande * 0.8f, 0.8f);
                anneau.capaciteAnneau = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * rayonMax * 10f), 200, 1000);
                anneau.eclats = 40;
                anneau.vieEclats = 1.4f;
                anneau.tailleGemmes = 0.17f;
                anneau.Construire();
                anneau.JouerDepuis(Deathless.Reseau.EnnemiReseau.TempsReseau() - depart);
            }
            var o = go.AddComponent<OndeChocLente>();
            o.m_Centre = centre; o.m_Vitesse = vitesse; o.m_RayonMax = rayonMax; o.m_LargeurBande = largeurBande;
            o.m_Depart = depart; o.m_Reseau = reseau; o.m_Degats = degats;
            o.m_Duree = duree;
            Destroy(go, duree + 1.6f);
            return go;
        }

        void Update()
        {
            if (m_Touche) return;
            float ecoule = Deathless.Reseau.EnnemiReseau.TempsReseau() - m_Depart;
            if (ecoule < 0f) return;
            if (ecoule > m_Duree + 0.3f) { enabled = false; return; }
            var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
            if (h == null || h.Distant || !h.Vivant) return;
            float rFront = Mathf.Min(m_Vitesse * ecoule, m_RayonMax);
            Vector3 d = h.transform.position - m_Centre; d.y = 0f;
            if (Mathf.Abs(d.magnitude - rFront) > m_LargeurBande * 0.5f) return;   // le front n'est pas encore (ou plus) sous les pieds
            if (!h.AuSol) return;   // en l'air au passage du front : rien, c'est tout le principe du saut
            m_Touche = true;
            Appliquer(h, d);
        }

        void Appliquer(Heros h, Vector3 d)
        {
            if (m_Reseau == null || !m_Reseau.IsSpawned || m_Reseau.IsServer)
            {
                // Solo, ou l'hôte pour son propre héros : l'hôte fait déjà foi, appliqué tout de suite.
                h.Sante.Encaisser(new InfoDegats
                {
                    montant = m_Degats, equipeSource = Equipe.Ennemis, parable = false,
                    point = h.transform.position + Vector3.up, direction = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward
                });
                h.Renverser();
                return;
            }
            // Client : signalé à l'hôte, qui vérifie la vraisemblance avant d'appliquer (Docs/reseau.md).
            m_Reseau.SignalerOndeTouchee();
        }
    }
}
