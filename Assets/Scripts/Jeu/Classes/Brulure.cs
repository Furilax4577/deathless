using UnityEngine;

namespace Deathless.Jeu
{
    /// Brûlure du Mage, style feu (wiki : les ennemis touchés brûlent pendant un moment) : posée sur l'ennemi par la boule de
    /// feu et le cône ; dégâts par seconde pendant la durée, rafraîchie par chaque nouveau coup (pas de cumul) ;
    /// flammèches `BurnFlammeches` au centre du squelette et crépitement en boucle ; s'arrête à la mort.
    public class Brulure : MonoBehaviour
    {
        Sante m_Sante;
        Heros m_Source;
        float m_Fin;
        float m_Accu;
        GameObject m_Flammes;
        AudioSource m_Son;

        public static void Allumer(Sante cible, Heros source)
        {
            if (cible == null || cible.Mort) return;
            var b = cible.GetComponent<Brulure>();
            if (b == null) b = cible.gameObject.AddComponent<Brulure>();
            b.m_Sante = cible;
            b.m_Source = source;
            b.m_Fin = Time.time + GameBalance.Courant.brulureDuree;
            b.enabled = true;
            if (b.m_Flammes == null && EffetsJeu.Instance != null && EffetsJeu.Instance.prefabBrulure != null)
            {
                var sq = cible.GetComponent<Squelette>();
                float h = sq != null && sq.type == TypeEnnemi.Golem ? 2f : 1f;
                b.m_Flammes = Instantiate(EffetsJeu.Instance.prefabBrulure, cible.transform);
                b.m_Flammes.transform.localPosition = Vector3.up * h;
                b.m_Flammes.transform.localScale = Vector3.one * h;
            }
            if (b.m_Flammes != null && !b.m_Flammes.activeSelf) b.m_Flammes.SetActive(true);
            if (b.m_Son == null) b.m_Son = AudioBank.Boucle(SonsDuJeu.Brulure, cible.transform, 0.45f);
            else if (!b.m_Son.isPlaying) b.m_Son.Play();
        }

        void Update()
        {
            if (m_Sante == null || m_Sante.Mort || !m_Sante.isActiveAndEnabled || Time.time >= m_Fin) { Eteindre(); return; }
            m_Accu += GameBalance.Courant.brulureDegats * Time.deltaTime;
            if (m_Accu >= 1f)
            {
                float d = Mathf.Floor(m_Accu);
                m_Accu -= d;
                int id = m_Source != null ? m_Source.Id : 0;
                float reel = m_Sante.Encaisser(new InfoDegats { montant = d, sourceId = id, equipeSource = Equipe.Heros, source = m_Source != null ? m_Source.gameObject : null, point = m_Sante.transform.position + Vector3.up, continu = true });
                if (reel > 0f && id > 0 && Partie.Instance != null) Partie.Instance.CompterDegats(id, reel, false);
            }
        }

        void Eteindre()
        {
            if (m_Flammes != null) m_Flammes.SetActive(false);
            if (m_Son != null) m_Son.Stop();
            m_Accu = 0f;
            enabled = false;
        }
    }
}
