using UnityEngine;

namespace Deathless.Jeu
{
    /// Légère aura de gemmes rouges d'un élite (wiki : ennemis, Élites : yeux rouges et légère aura rouge ; le vert reste à
    /// Nyxar). Quelques petites gemmes du thème Rage naissent autour de ses pieds et montent en tournant doucement ; un seul
    /// maillage (GemmesVolantes),
    /// sans lumière. Purement visuel et local (chaque poste la joue pour ses squelettes). Posée par Squelette.MarquerElite.
    public class AuraElite : MonoBehaviour
    {
        public float parSeconde = 7f;
        public float rayon = 0.55f;

        GemmesVolantes m_Gemmes;
        float m_Reste;
        Squelette m_Squelette;

        void Start()
        {
            m_Squelette = GetComponent<Squelette>();
            var mat = EffetsJeu.Gemmes;
            if (mat == null) { enabled = false; return; }
            m_Gemmes = GemmesVolantes.Creer("AuraElite_Gemmes", mat, 64);
        }

        void OnDestroy() { if (m_Gemmes != null) Destroy(m_Gemmes.gameObject); }

        void Update()
        {
            if (m_Gemmes == null) return;
            if (m_Squelette != null && !m_Squelette.Vivant) return;
            m_Reste += Time.deltaTime * parSeconde;
            float echelle = transform.lossyScale.y;
            while (m_Reste >= 1f)
            {
                m_Reste -= 1f;
                float a = Random.value * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * rayon * echelle;
                Vector3 v = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.35f + Vector3.up * Random.Range(0.6f, 1.1f);
                Color c = Random.value < 0.6f
                    ? VfxPalette.Couleur(VfxTheme.Rage, VfxRole.Vif, new Color(0.70f, 0.15f, 0.12f))
                    : VfxPalette.Couleur(VfxTheme.Rage, VfxRole.Base, new Color(0.43f, 0.08f, 0.06f));
                m_Gemmes.Emettre(p, v, Random.Range(0.035f, 0.06f), Random.Range(1.0f, 1.6f), c, 0f, 0.6f, 0.1f, 0.4f);
            }
        }
    }
}
