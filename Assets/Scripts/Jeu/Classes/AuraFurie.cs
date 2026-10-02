using UnityEngine;

namespace Deathless.Jeu
{
    /// Aura de la Furie du viking (03/10/2026, Docs/vfx.md) : des gemmes low poly du thème Rage (rouge sombre, rouge vif,
    /// rouge pâle ; jamais de vert) naissent autour du corps et montent en tournant, une gerbe éclate à l'entrée, une petite
    /// poussée de braises retombe à la sortie, une lumière rouge petite reste allumée entre les deux. Un seul maillage
    /// (GemmesVolantes), sans alpha ; purement visuel : chaque poste la joue pour le viking, local ou marionnette.
    public class AuraFurie : MonoBehaviour
    {
        public float parSeconde = 48f;
        public float rayon = 0.5f;
        public float hauteur = 1.9f;

        GemmesVolantes m_Gemmes;
        VfxLumiere m_Lumiere;
        float m_Reste;
        bool m_Actif;
        float m_Echelle = 1f;

        public bool Actif => m_Actif;

        bool Assurer()
        {
            if (m_Gemmes != null) return true;
            var mat = EffetsJeu.Gemmes;
            if (mat == null) return false;
            m_Gemmes = GemmesVolantes.Creer("AuraFurie_Gemmes", mat, 160);
            return true;
        }

        void OnDestroy() { if (m_Gemmes != null) Destroy(m_Gemmes.gameObject); }

        static Color Teinte(float t)
        {
            // Rouge vif la plupart du temps, cœur pâle (orangé) pour les étincelles, base sombre pour la profondeur.
            if (t < 0.5f) return VfxPalette.Couleur(VfxTheme.Rage, VfxRole.Vif, new Color(0.70f, 0.15f, 0.12f));
            if (t < 0.8f) return VfxPalette.Couleur(VfxTheme.Rage, VfxRole.Coeur, new Color(1.00f, 0.45f, 0.35f));
            return VfxPalette.Couleur(VfxTheme.Rage, VfxRole.Base, new Color(0.43f, 0.08f, 0.06f));
        }

        /// Échelle du modèle (1 normal, 1,15 en Furie) : la gerbe et le halo suivent la taille du corps.
        public void Echelle(float e) { m_Echelle = Mathf.Max(0.5f, e); }

        /// Entrée en Furie : gerbe en couronne à hauteur de poitrine, lumière allumée, émission continue.
        public void Commencer()
        {
            if (m_Actif || !Assurer()) return;
            m_Actif = true;
            m_Reste = 0f;
            float e = Mathf.Max(1f, m_Echelle);
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f + Random.value * 0.15f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = transform.position + dir * 0.35f * e + Vector3.up * Random.Range(0.3f, 1.5f) * e;
                Vector3 v = dir * Random.Range(2.2f, 4.0f) + Vector3.up * Random.Range(0.2f, 1.4f);
                m_Gemmes.Emettre(p, v, Random.Range(0.08f, 0.14f), Random.Range(0.5f, 0.85f), Teinte(Random.value), -1.5f, 2.2f, 0.05f, 0.35f);
            }
            if (m_Lumiere == null) m_Lumiere = VfxLumiere.Creer(transform, Vector3.up * 1.1f, VfxTheme.Rage, VfxTailleLumiere.Petite, -1f);
            else m_Lumiere.Allumer(-1f);
        }

        /// Fin de la Furie : lumière éteinte, quelques braises qui retombent (retour au calme).
        public void Arreter(bool braises = true)
        {
            if (!m_Actif) return;
            m_Actif = false;
            if (m_Lumiere != null) m_Lumiere.Eteindre();
            if (!braises || m_Gemmes == null) return;
            float e = Mathf.Max(1f, m_Echelle);
            for (int i = 0; i < 14; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(0.1f, 0.4f) * e + Vector3.up * Random.Range(0.2f, 1.6f) * e;
                Vector3 v = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.4f + Vector3.up * Random.Range(0.1f, 0.5f);
                m_Gemmes.Emettre(p, v, Random.Range(0.035f, 0.07f), Random.Range(0.7f, 1.1f), Teinte(Random.value * 0.5f), 1.0f, 1.5f, 0.05f, 0.2f);
            }
        }

        void Update()
        {
            if (!m_Actif || m_Gemmes == null) return;
            m_Reste += Time.deltaTime * parSeconde;
            float e = Mathf.Max(1f, m_Echelle);
            while (m_Reste >= 1f)
            {
                m_Reste -= 1f;
                float a = Random.value * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rayon * e * Random.Range(0.7f, 1.1f) + Vector3.up * Random.Range(0.05f, hauteur) * e;
                Vector3 v = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.6f + Vector3.up * Random.Range(0.9f, 1.8f);
                m_Gemmes.Emettre(p, v, Random.Range(0.06f, 0.12f), Random.Range(0.7f, 1.2f), Teinte(Random.value), 0f, 0.5f, 0.1f, 0.4f);
            }
        }
    }
}
