using UnityEngine;

namespace Deathless.Jeu
{
    /// Onde de choc lente du Fracas (Morgrim massue, décidé le 26/09/2026 ; wiki : ennemis.md, Docs/reseau.md) : un
    /// front qui part du point d'impact et s'étend à vitesse lente et constante (GameBalance.morgrimMassueFracasOnde*),
    /// assez lentement pour qu'on saute par-dessus. Non parable (pas de garde ni de parade), non esquivable (la roulade
    /// et ses frames d'invulnérabilité n'y font rien) : seul un saut au bon instant évite les dégâts.
    ///
    /// Visuel : réutilise le prefab d'onde du Golem (OndeDeChoc + OndeGemmes, gemmes Terre déjà en place), dont les
    /// paramètres publics sont réglés ici en un anneau large et lent au lieu du choc bref habituel.
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
        float m_Vitesse, m_RayonMax, m_LargeurBande, m_Depart, m_Duree;
        Deathless.Reseau.EnnemiReseau m_Reseau;
        bool m_Touche;

        /// Crée (ou réutilise, visuellement, le prefab d'onde du Golem) l'onde à `centre`, réglée en front lent. Appelé
        /// chez l'hôte (MorgrimMassue.FaireFracas) et chez chaque client (MorgrimVariant.RecevoirOndeDistante).
        public static GameObject Creer(GameObject prefabVisuel, Vector3 centre, float vitesse, float rayonMax, float largeurBande,
            float depart, Deathless.Reseau.EnnemiReseau reseau)
        {
            GameObject go;
            if (prefabVisuel != null)
            {
                go = Instantiate(prefabVisuel, centre, Quaternion.identity);
                var onde = go.GetComponentInChildren<OndeDeChoc>();
                if (onde != null)
                {
                    // Front large et lent (au lieu du choc bref habituel) : bien lisible au ras du sol, à hauteur de
                    // chevilles, « ça se lit comme à sauter » (décision de Quentin) — anneau épais qui ne s'amincit
                    // presque pas, un peu de poussière (les éclats existants, à la même vitesse que le front).
                    onde.rayonDepart = 0.5f;
                    onde.rayonMax = rayonMax;
                    onde.duree = rayonMax / Mathf.Max(0.1f, vitesse);
                    onde.angleOuverture = 360f;
                    onde.largeurDepart = Mathf.Max(largeurBande, 1.3f);
                    onde.largeurFin = Mathf.Max(largeurBande * 0.7f, 0.9f);
                    onde.Jouer();
                }
            }
            else go = new GameObject("OndeChocLente");
            go.name = "OndeChocLente_Fracas";
            var o = go.GetComponent<OndeChocLente>();
            if (o == null) o = go.AddComponent<OndeChocLente>();
            o.m_Centre = centre; o.m_Vitesse = vitesse; o.m_RayonMax = rayonMax; o.m_LargeurBande = largeurBande;
            o.m_Depart = depart; o.m_Reseau = reseau;
            o.m_Duree = rayonMax / Mathf.Max(0.1f, vitesse);
            Destroy(go, o.m_Duree + 1.2f);
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
                var b = GameBalance.Courant;
                h.Sante.Encaisser(new InfoDegats
                {
                    montant = b.morgrimMassueFracasDegats, equipeSource = Equipe.Ennemis, parable = false,
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
