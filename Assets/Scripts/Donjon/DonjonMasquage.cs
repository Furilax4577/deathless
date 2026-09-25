using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Donjon
{
    /// Masquage des étages, local à chaque joueur (rien n'est synchronisé) : quand le héros local est dans une zone
    /// couverte (bloc dont au moins 5 cellules sur 9 ont un plancher au-dessus de lui), tout ce qui est au-dessus de
    /// son niveau dans ce bloc disparaît, ainsi que dans le bloc où se trouve la caméra et, en diagonale, dans les deux
    /// blocs entre eux. Dès qu'il ressort à découvert, tout revient.
    ///
    /// Zones : volumes déclencheurs par bloc et par niveau (DonjonZone), lus par des capteurs (DonjonCapteur) posés
    /// sur le héros local et sur sa caméra. Aucun lancer de rayon, aucun calcul par image quand rien ne change :
    /// ce composant ne tourne (Update) que pendant une transition.
    ///
    /// Effet : chaque pièce rétrécit jusqu'à disparaître (0,18 s), en vague depuis le héros, puis elle est
    /// désactivée ; elle regrandit au retour. C'est le langage visuel du projet (les effets apparaissent et
    /// disparaissent par la taille, sans alpha) : net, lisible, sans transparence floue ni tri, et les pièces du
    /// kit ayant leur pivot au sol ou au centre, un mur « rentre » dans son plancher et une dalle se resserre sur
    /// son centre. duree = 0 : masquage net immédiat. Les collisions sont des objets séparés, jamais mis à
    /// l'échelle : les autres joueurs et les squelettes marchent toujours sur l'étage masqué.
    [DisallowMultipleComponent]
    public class DonjonMasquage : MonoBehaviour
    {
        [Tooltip("Durée du rétrécissement d'une pièce (s). 0 : masquage net, sans animation.")]
        public float duree = 0.18f;
        [Tooltip("Vitesse de la vague depuis le héros (m/s) : les pièces proches partent d'abord.")]
        public float vitesseVague = 70f;
        [Tooltip("Décochez pour tout garder visible (comparaison).")]
        public bool actif = true;

        const int NbGroupes = DonjonPlan.NbBlocs * DonjonPlan.NbNiveaux;

        [System.NonSerialized] DonjonPlan m_Plan;
        readonly List<Transform>[] m_Items = new List<Transform>[NbGroupes];
        readonly List<Vector3>[] m_Echelles = new List<Vector3>[NbGroupes];
        readonly List<float>[] m_Retards = new List<float>[NbGroupes];
        readonly bool[] m_Cible = new bool[NbGroupes];      // masqué voulu
        readonly bool[] m_Etat = new bool[NbGroupes];       // masqué appliqué (ou en cours)
        readonly bool[] m_EnCours = new bool[NbGroupes];
        readonly float[] m_Debut = new float[NbGroupes];
        readonly float[] m_Fin = new float[NbGroupes];
        [System.NonSerialized] int m_HerosBloc = -1, m_HerosNiveau = -1, m_CameraBloc = -1;
        [System.NonSerialized] Transform m_Heros;

        public int HerosBloc { get { return m_HerosBloc; } }
        public int HerosNiveau { get { return m_HerosNiveau; } }
        public int CameraBloc { get { return m_CameraBloc; } }
        /// Nombre de groupes (bloc, niveau) actuellement masqués.
        public int NbMasques { get { int n = 0; for (int i = 0; i < NbGroupes; i++) if (m_Cible[i]) n++; return n; } }

        void Awake() { enabled = false; }

        /// Appelé par le générateur avant de reposer les pièces : tout redevient visible, les listes sont vidées.
        public void Preparer(DonjonPlan plan)
        {
            m_Plan = plan;
            for (int g = 0; g < NbGroupes; g++)
            {
                if (m_Items[g] == null) { m_Items[g] = new List<Transform>(128); m_Echelles[g] = new List<Vector3>(128); m_Retards[g] = new List<float>(128); }
                m_Items[g].Clear(); m_Echelles[g].Clear(); m_Retards[g].Clear();
                m_Cible[g] = m_Etat[g] = m_EnCours[g] = false;
            }
            enabled = false;
        }

        public void Ajouter(int groupe, Transform t)
        {
            m_Items[groupe].Add(t);
            m_Echelles[groupe].Add(t.localScale);
            m_Retards[groupe].Add(0f);
        }

        /// Fin de construction : réapplique l'état courant du héros (le donjon a pu être régénéré sous ses pieds).
        public void Terminer() { Recalculer(true); }

        public void Heros(int bloc, int niveau, Transform heros)
        {
            m_Heros = heros;
            if (bloc == m_HerosBloc && niveau == m_HerosNiveau) return;
            m_HerosBloc = bloc; m_HerosNiveau = niveau;
            Recalculer(false);
        }

        public void Camera(int bloc)
        {
            if (bloc == m_CameraBloc) return;
            m_CameraBloc = bloc;
            Recalculer(false);
        }

        public bool EstMasque(int groupe) { return m_Cible[groupe]; }

        /// Recalcule après un changement de réglage (actif, duree).
        public void Rafraichir() { Recalculer(false); }

        /// La caméra doit-elle ignorer ce collider (il appartient à un étage masqué) ? À appeler seulement sur les
        /// collisions trouvées par la caméra, pas à chaque image pour chaque objet.
        public static bool ColliderMasque(Collider c)
        {
            Transform p = c.transform.parent;
            if (p == null) return false;
            DonjonGroupe g;
            if (!p.TryGetComponent(out g) || g.masquage == null) return false;
            return g.masquage.m_Cible[g.index];
        }

        void Recalculer(bool immediat)
        {
            if (m_Plan == null) return;
            for (int g = 0; g < NbGroupes; g++) m_Voulu[g] = false;
            if (actif && m_HerosBloc >= 0 && m_HerosNiveau >= 0 && m_Plan.Couvert(m_HerosBloc, m_HerosNiveau))
            {
                MasquerAuDessus(m_HerosBloc);
                int cb = m_CameraBloc;
                if (cb >= 0 && cb != m_HerosBloc)
                {
                    int hx = m_HerosBloc % DonjonPlan.BlocsX, hy = m_HerosBloc / DonjonPlan.BlocsX;
                    int cx = cb % DonjonPlan.BlocsX, cy = cb / DonjonPlan.BlocsX;
                    if (Mathf.Abs(cx - hx) <= 1 && Mathf.Abs(cy - hy) <= 1)
                    {
                        MasquerAuDessus(cb);
                        if (cx != hx && cy != hy) { MasquerAuDessus(cx + hy * DonjonPlan.BlocsX); MasquerAuDessus(hx + cy * DonjonPlan.BlocsX); }
                    }
                }
            }
            bool anime = false;
            float t0 = Time.unscaledTime;
            Vector3 origine = m_Heros != null ? m_Heros.position : Vector3.zero;
            for (int g = 0; g < NbGroupes; g++)
            {
                if (m_Voulu[g] == m_Cible[g] && !immediat) continue;
                m_Cible[g] = m_Voulu[g];
                if (m_Etat[g] == m_Cible[g] && !m_EnCours[g]) continue;
                List<Transform> items = m_Items[g];
                if (immediat || duree <= 0f || !Application.isPlaying)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        Transform t = items[i];
                        if (t == null) continue;
                        t.localScale = m_Echelles[g][i];
                        if (t.gameObject.activeSelf == m_Cible[g]) t.gameObject.SetActive(!m_Cible[g]);
                    }
                    m_Etat[g] = m_Cible[g]; m_EnCours[g] = false;
                    continue;
                }
                // Transition animée : retards en vague depuis le héros.
                float maxRetard = 0f;
                List<float> retards = m_Retards[g];
                for (int i = 0; i < items.Count; i++)
                {
                    Transform t = items[i];
                    if (t == null) continue;
                    float r = vitesseVague > 0f ? Vector3.Distance(t.position, origine) / vitesseVague : 0f;
                    if (!m_Cible[g]) r *= 0.5f;
                    retards[i] = r;
                    if (r > maxRetard) maxRetard = r;
                    if (!m_Cible[g] && !t.gameObject.activeSelf) { t.localScale = Vector3.zero; t.gameObject.SetActive(true); }
                }
                m_Debut[g] = t0; m_Fin[g] = t0 + maxRetard + duree;
                m_Etat[g] = m_Cible[g]; m_EnCours[g] = true;
                anime = true;
            }
            if (anime) enabled = true;
        }

        readonly bool[] m_Voulu = new bool[NbGroupes];

        void MasquerAuDessus(int bloc)
        {
            for (int k = m_HerosNiveau + 1; k < DonjonPlan.NbNiveaux; k++) m_Voulu[bloc * DonjonPlan.NbNiveaux + k] = true;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            bool reste = false;
            for (int g = 0; g < NbGroupes; g++)
            {
                if (!m_EnCours[g]) continue;
                List<Transform> items = m_Items[g];
                bool masquer = m_Etat[g];
                bool fini = now >= m_Fin[g];
                for (int i = 0; i < items.Count; i++)
                {
                    Transform t = items[i];
                    if (t == null) continue;
                    float s = fini ? 1f : Mathf.Clamp01((now - m_Debut[g] - m_Retards[g][i]) / duree);
                    s = s * s * (3f - 2f * s);
                    float f = masquer ? 1f - s : s;
                    t.localScale = m_Echelles[g][i] * f;
                    if (fini)
                    {
                        t.localScale = m_Echelles[g][i];
                        if (masquer && t.gameObject.activeSelf) t.gameObject.SetActive(false);
                    }
                }
                if (fini) m_EnCours[g] = false; else reste = true;
            }
            if (!reste) enabled = false;
        }
    }
}
