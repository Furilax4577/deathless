using UnityEngine;

namespace Deathless.Jeu
{
    /// Missile en forme de crâne (gemmes vertes) : projectile guidé vers une cible. Tiré par Nyxessa (échelle 1,5, par
    /// Nyxessa.TirerMissile : éclat, réaction du cristal) ou par le Nécromancien (échelle 1, taille normale). À l'arrivée :
    /// dégâts, éclatement du crâne, son.
    public class MissileCrane : MonoBehaviour
    {
        Sante m_Cible;
        Vector3 m_DernierPoint;
        float m_Vitesse, m_Guidage, m_Degats, m_Vie;
        Equipe m_Equipe;
        int m_SourceId;
        bool m_Parable;
        GameObject m_Source;
        SkullMissileVisual m_Visuel;
        AudioSource m_Boucle;
        bool m_Fini;
        bool m_Visuel_Seul;

        /// Multijoueur (client) : le même vol que chez l'hôte, sans dégâts (l'hôte les applique).
        public static MissileCrane TirerVisuel(Vector3 depart, Sante cible, float vitesse, float guidage, bool parNyxessa)
        {
            var m = Creer(depart, cible, 0f, vitesse, guidage, Equipe.Relique, null, parNyxessa);
            m.m_Visuel_Seul = true;
            return m;
        }

        public static MissileCrane Tirer(Vector3 depart, Sante cible, float degats, float vitesse, float guidage, Equipe equipe, GameObject source, bool parNyxessa)
        {
            var m = Creer(depart, cible, degats, vitesse, guidage, equipe, source, parNyxessa);
            // Multijoueur : l'hôte fait voir le même missile aux clients.
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.Missile(depart, cible, vitesse, guidage, parNyxessa);
            return m;
        }

        static MissileCrane Creer(Vector3 depart, Sante cible, float degats, float vitesse, float guidage, Equipe equipe, GameObject source, bool parNyxessa)
        {
            var go = new GameObject(parNyxessa ? "MissileNyxessa" : "MissileNecromancien");
            go.transform.position = depart;
            Vector3 vise = Viser(cible, depart + Vector3.forward);
            go.transform.rotation = Quaternion.LookRotation((vise - depart).normalized + Vector3.up * 0.35f);
            var m = go.AddComponent<MissileCrane>();
            m.m_Cible = cible;
            m.m_DernierPoint = vise;
            m.m_Degats = degats;
            m.m_Vitesse = vitesse;
            m.m_Guidage = guidage;
            m.m_Equipe = equipe;
            m.m_Source = source;
            m.m_Parable = !parNyxessa;
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.formeCrane != null && fx.gemmes != null)
            {
                if (parNyxessa && Nyxessa.Instance != null)
                    m.m_Visuel = Nyxessa.Instance.TirerMissile(go.transform, fx.formeCrane, fx.gemmes, 1.5f);
                else
                {
                    GemBurst.Explode(depart + go.transform.forward * 0.3f, 0.5f, fx.gemmes);
                    m.m_Visuel = SkullMissileVisual.Attach(go.transform, fx.formeCrane, Vector3.zero, 0.55f, fx.gemmes, 1f);
                }
            }
            m.m_Boucle = AudioBank.Boucle(SonsDuJeu.MissileVol, go.transform, 0.5f);
            if (parNyxessa) AudioBank.Jouer(SonsDuJeu.NyxessaTir, depart, 0.7f);
            return m;
        }

        static Vector3 Viser(Sante s, Vector3 defaut)
        {
            if (s == null) return defaut;
            var sq = s.GetComponent<Squelette>();
            float h = sq != null && sq.type == TypeEnnemi.Golem ? 2.2f : 1.1f;
            return s.transform.position + Vector3.up * h;
        }

        void Update()
        {
            if (m_Fini) return;
            float dt = Time.deltaTime;
            m_Vie += dt;
            if (m_Cible != null && !m_Cible.Mort && m_Cible.isActiveAndEnabled) m_DernierPoint = Viser(m_Cible, m_DernierPoint);
            Vector3 vers = m_DernierPoint - transform.position;
            float dist = vers.magnitude;
            if (dist < 0.5f || m_Vie > 5f) { Arriver(); return; }
            Quaternion voulu = Quaternion.LookRotation(vers / dist);
            // Guidage plus serré en fin de course pour ne pas tourner autour de la cible.
            float g = m_Guidage * (dist < 4f ? 4f : 1f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, voulu, g * dt);
            transform.position += transform.forward * Mathf.Min(m_Vitesse * dt, dist);
        }

        void Arriver()
        {
            m_Fini = true;
            if (!m_Visuel_Seul && m_Cible != null && !m_Cible.Mort && (m_Cible.transform.position + Vector3.up - transform.position).sqrMagnitude < 4f)
            {
                m_Cible.Encaisser(new InfoDegats
                {
                    montant = m_Degats, equipeSource = m_Equipe, source = m_Source, parable = m_Parable,
                    point = transform.position, direction = transform.forward
                });
            }
            if (m_Visuel != null) m_Visuel.Shatter();
            if (m_Boucle != null) m_Boucle.Stop();
            AudioBank.Jouer(SonsDuJeu.MissileEclat, transform.position, 0.8f);
            Destroy(gameObject, 1.5f);
        }
    }
}
