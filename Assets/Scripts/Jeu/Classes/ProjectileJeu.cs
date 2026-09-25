using System;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Projectile des classes : flèches et carreaux (modèle KayKit `arrow_bow`, non magiques, traînée d'air TraineeAir),
    /// boule de feu (FireballVisual en vol, légère cloche). Flèches et carreaux ont une vitesse et subissent la pesanteur
    /// (GameBalance.projectileGravite) : ils volent en cloche, orientés le long de leur trajectoire ; l'aide à la visée ne
    /// relève le tir que d'un petit angle (GameBalance.aideChuteMax) pour compenser la chute. Balayage par SphereCast à chaque image ; au contact, le rappel
    /// `impact(point, direction, sante)` reçoit l'ennemi touché (ou null pour le décor). Les flèches restent plantées
    /// dans ce qu'elles touchent (suivent le squelette) puis disparaissent.
    public class ProjectileJeu : MonoBehaviour
    {
        public enum Genre { Fleche, Carreau, BouleDeFeu }

        /// Tests (ScenariosClasses « balistique ») : arrivée d'un projectile (genre, point, ennemi touché ou null, hauteur
        /// maximale atteinte au-dessus du départ).
        public static event Action<Genre, Vector3, Sante, float> Arrivee;
        Vector3 m_Depart;
        float m_Sommet;

        Genre m_Genre;
        Vector3 m_Vitesse;
        float m_Gravite;
        float m_Reste;
        Transform m_Tireur;
        Action<Vector3, Vector3, Sante> m_Impact;
        TraineeAir m_Trainee;
        AudioSource m_Boucle;
        bool m_Fini;
        float m_DemiLongueur;
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        public static ProjectileJeu Tirer(Genre genre, Vector3 depart, Vector3 cible, float vitesse, float portee, Transform tireur,
            Action<Vector3, Vector3, Sante> impact)
        {
            var fx = EffetsJeu.Instance;
            GameObject go;
            if (genre != Genre.BouleDeFeu && fx != null && fx.modeleFleche != null)
            {
                go = Instantiate(fx.modeleFleche);
                if (genre == Genre.Carreau) go.transform.localScale *= 0.85f;
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            else go = new GameObject(genre.ToString());
            go.name = genre.ToString();
            Vector3 dir = (cible - depart).sqrMagnitude > 0.0001f ? (cible - depart).normalized : tireur.forward;
            var bal = GameBalance.Courant;
            if (genre != Genre.BouleDeFeu) dir = DirectionBalistique(depart, cible, vitesse, bal.projectileGravite, bal.aideChuteMax, dir);
            go.transform.SetPositionAndRotation(depart, Quaternion.LookRotation(dir));
            var p = go.AddComponent<ProjectileJeu>();
            p.m_Genre = genre;
            p.m_Tireur = tireur;
            p.m_Impact = impact;
            p.m_Reste = portee;
            p.m_Depart = depart;
            if (genre == Genre.BouleDeFeu)
            {
                // Légère cloche (Relic : +1,5 m/s vers le haut, chute 5), visée corrigée pour tomber sur la cible.
                p.m_Gravite = 5f;
                float t = Vector3.Distance(depart, cible) / Mathf.Max(1f, vitesse);
                p.m_Vitesse = dir * vitesse + Vector3.up * (0.5f * p.m_Gravite * t);
                if (fx != null) FireballVisual.Attach(go.transform, fx.terre, 1f, fx.gemmes);
                p.m_Boucle = AudioBank.Boucle(SonsDuJeu.BouleVol, go.transform, 0.6f);
            }
            else
            {
                p.m_Vitesse = dir * vitesse;
                p.m_Gravite = bal.projectileGravite;
                p.m_Reste = Mathf.Max(portee, bal.projectileVolMax);
                var mf = go.GetComponentInChildren<MeshFilter>();
                p.m_DemiLongueur = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents.z * go.transform.lossyScale.z : 0.35f;
                if (fx != null && fx.gemmes != null)
                    p.m_Trainee = TraineeAir.Attacher(go.transform, new Vector3(0f, 0f, -p.m_DemiLongueur / Mathf.Max(0.001f, go.transform.lossyScale.z)),
                        fx.gemmes, genre == Genre.Carreau ? 1.2f : 2.4f, genre == Genre.Carreau ? 0.009f : 0.012f, genre == Genre.Carreau ? 0.16f : 0.22f);
            }
            return p;
        }

        /// Direction de tir d'un projectile lent (vitesse `v`, pesanteur `g`) vers `cible` : la visée droite, relevée au plus de
        /// `aideMaxDeg` degrés vers l'angle qui touche la cible (tir tendu, la solution basse). Au-delà (cible trop loin pour
        /// cette vitesse, ou chute trop forte), le relèvement reste plafonné : le joueur vise au-dessus.
        public static Vector3 DirectionBalistique(Vector3 depart, Vector3 cible, float v, float g, float aideMaxDeg, Vector3 repli)
        {
            Vector3 d = cible - depart;
            Vector3 plat = new Vector3(d.x, 0f, d.z);
            float x = plat.magnitude, y = d.y;
            if (x < 0.05f || g <= 0f || v <= 0.1f) return d.sqrMagnitude > 0.0001f ? d.normalized : repli;
            float droit = Mathf.Atan2(y, x);
            float v2 = v * v;
            float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
            float ideal = disc >= 0f ? Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (g * x)) : Mathf.PI * 0.25f;
            float a = droit + Mathf.Clamp(ideal - droit, 0f, aideMaxDeg * Mathf.Deg2Rad);
            return (plat / x * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)).normalized;
        }

        void Update()
        {
            if (m_Fini) return;
            float dt = Time.deltaTime;
            m_Vitesse += Vector3.down * m_Gravite * dt;
            Vector3 pas = m_Vitesse * dt;
            float d = pas.magnitude;
            if (d < 1e-5f) return;
            Vector3 dir = pas / d;
            Vector3 avant = transform.position + dir * m_DemiLongueur;   // la pointe mène
            float r = m_Genre == Genre.BouleDeFeu ? 0.25f : 0.08f;
            int n = Physics.SphereCastNonAlloc(avant, r, dir, s_Hits, d, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; int k = -1;
            for (int i = 0; i < n; i++)
            {
                var h = s_Hits[i];
                if (m_Tireur != null && h.collider.transform.IsChildOf(m_Tireur)) continue;
                var s = h.collider.GetComponentInParent<Sante>();
                if (s != null && s.equipe != Equipe.Ennemis) continue;   // alliés et Nyxessa traversés
                if (h.distance < best) { best = h.distance; k = i; }
            }
            if (k >= 0)
            {
                var h = s_Hits[k];
                Vector3 point = h.point == Vector3.zero ? avant + dir * h.distance : h.point;
                Arriver(point, dir, h.collider);
                return;
            }
            transform.position += pas;
            m_Sommet = Mathf.Max(m_Sommet, transform.position.y - m_Depart.y);
            transform.rotation = Quaternion.LookRotation(dir);
            m_Reste -= d;
            if (m_Reste <= 0f) Arriver(transform.position, dir, null);
        }

        void Arriver(Vector3 point, Vector3 dir, Collider touche)
        {
            m_Fini = true;
            var s = touche != null ? touche.GetComponentInParent<Sante>() : null;
            Arrivee?.Invoke(m_Genre, point, s, m_Sommet);
            if (m_Trainee != null) m_Trainee.Detacher();
            if (m_Boucle != null) m_Boucle.Stop();
            if (m_Genre == Genre.BouleDeFeu)
            {
                transform.position = point;
                m_Impact?.Invoke(point, dir, s);
                foreach (Transform c in transform) c.gameObject.SetActive(false);
                Destroy(gameObject, 0.1f);
                return;
            }
            // Flèche plantée : la pointe au point d'impact, enfoncée de 10 cm ; elle suit ce qu'elle a touché.
            transform.position = point - dir * (m_DemiLongueur - 0.1f);
            transform.rotation = Quaternion.LookRotation(dir);
            if (touche != null) transform.SetParent(touche.transform, true);
            m_Impact?.Invoke(point, dir, s);
            Destroy(gameObject, s != null ? 1.2f : 2f);
        }
    }
}
