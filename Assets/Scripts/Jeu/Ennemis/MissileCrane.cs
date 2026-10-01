using UnityEngine;

namespace Deathless.Jeu
{
    /// Missile en forme de crâne (gemmes vertes) : projectile guidé vers une cible. Tiré par Nyxessa (échelle 1,5, par
    /// Nyxessa.TirerMissile : éclat, réaction du cristal) ou par le Nécromancien (échelle 1, taille normale). À l'arrivée :
    /// dégâts, éclatement du crâne, son.
    public class MissileCrane : MonoBehaviour
    {
        Sante m_Cible;
        Squelette m_CibleSquelette;   // squelette de la cible (hauteur visée), cherché une fois au tir
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
        bool m_ParNyxessa;
        bool m_BloqueParBouclier;
        bool m_TireDehors;   // tiré par un ennemi hors de l'enceinte du bouclier (01/10/2026)
        bool m_Compte;   // dégâts comptés dans s_EnVol (retirés à l'arrivée ou à la destruction)

        /// Dégâts des missiles déjà partis vers chaque cible (30/09/2026) : Nyxessa ne tire plus sur un ennemi que les
        /// missiles en vol suffisent à tuer (DefenseNyxessa.Condamne).
        static readonly System.Collections.Generic.Dictionary<Sante, float> s_EnVol = new System.Collections.Generic.Dictionary<Sante, float>();
        public static float DegatsEnVol(Sante s) => s != null && s_EnVol.TryGetValue(s, out var d) ? d : 0f;

        void Decompter()
        {
            if (!m_Compte) return;
            m_Compte = false;
            if (m_Cible == null || !s_EnVol.TryGetValue(m_Cible, out var d)) return;
            d -= m_Degats;
            if (d <= 0.01f) s_EnVol.Remove(m_Cible); else s_EnVol[m_Cible] = d;
        }

        void OnDestroy() => Decompter();

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
            if (cible != null && degats > 0f)
            {
                s_EnVol[cible] = DegatsEnVol(cible) + degats;
                m.m_Compte = true;
            }
            // Multijoueur : l'hôte fait voir le même missile aux clients.
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.Missile(depart, cible, vitesse, guidage, parNyxessa);
            return m;
        }

        static MissileCrane Creer(Vector3 depart, Sante cible, float degats, float vitesse, float guidage, Equipe equipe, GameObject source, bool parNyxessa)
        {
            var go = new GameObject(parNyxessa ? "MissileNyxessa" : "MissileNecromancien");
            go.transform.position = depart;
            var cibleSq = cible != null ? cible.GetComponent<Squelette>() : null;
            Vector3 vise = Viser(cible, cibleSq, depart + Vector3.forward);
            go.transform.rotation = Quaternion.LookRotation((vise - depart).normalized + Vector3.up * 0.35f);
            var m = go.AddComponent<MissileCrane>();
            m.m_Cible = cible;
            m.m_CibleSquelette = cibleSq;
            m.m_DernierPoint = vise;
            m.m_Degats = degats;
            m.m_Vitesse = vitesse;
            m.m_Guidage = guidage;
            m.m_Equipe = equipe;
            m.m_Source = source;
            m.m_Parable = !parNyxessa;
            m.m_ParNyxessa = parNyxessa;
            // Un mage plaqué contre la paroi tire depuis une main déjà dans le cylindre : on retient d'où vient le tir
            // (le tireur, ou à défaut le point de départ) pour arrêter le crâne même s'il naît à l'intérieur.
            var bo = BouclierNyxessa.Instance;
            // Chez un client (missile visuel, sans tireur connu), un départ à moins de 0,8 m de la paroi compte comme du dehors.
            m.m_TireDehors = !parNyxessa && bo != null
                && (source != null ? !bo.Contient(source.transform.position + Vector3.up, -0.05f) : !bo.Contient(depart, 0.8f));
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

        static Vector3 Viser(Sante s, Squelette sq, Vector3 defaut)
        {
            if (s == null) return defaut;
            float h = sq != null && sq.type == TypeEnnemi.Golem ? 2.2f : 1.1f;
            return s.transform.position + Vector3.up * h;
        }

        void Update()
        {
            if (m_Fini) return;
            float dt = Time.deltaTime;
            m_Vie += dt;
            if (m_Cible != null && !m_Cible.Mort && m_Cible.isActiveAndEnabled) m_DernierPoint = Viser(m_Cible, m_CibleSquelette, m_DernierPoint);
            Vector3 vers = m_DernierPoint - transform.position;
            float dist = vers.magnitude;
            if (dist < 0.5f || m_Vie > 5f) { Arriver(); return; }
            Quaternion voulu = Quaternion.LookRotation(vers / dist);
            // Guidage plus serré en fin de course pour ne pas tourner autour de la cible.
            float g = m_Guidage * (dist < 4f ? 4f : 1f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, voulu, g * dt);
            Vector3 depart = transform.position;
            Vector3 arrivee = depart + transform.forward * Mathf.Min(m_Vitesse * dt, dist);
            // Les missiles ennemis sont guidés par code et n'ont pas de collision physique : le bouclier
            // doit donc intercepter leur trajectoire explicitement, y compris sur les clients réseau.
            var bouclier = BouclierNyxessa.Instance;
            bool bloque = false;
            Vector3 impact = arrivee;
            if (!m_ParNyxessa && bouclier != null)
            {
                if (bouclier.IntercepterProjectile(depart, arrivee, out var croisement)) { bloque = true; impact = croisement; }
                else if (m_TireDehors && bouclier.Contient(arrivee)) bloque = true;   // né dans la paroi, tiré du dehors
            }
            if (bloque)
            {
                transform.position = impact;
                m_BloqueParBouclier = true;
                Arriver();
                return;
            }
            transform.position = arrivee;
        }

        void Arriver()
        {
            m_Fini = true;
            Decompter();
            // Arrêté par le bouclier (01/10/2026) : la paroi encaisse le crâne comme un coup de mêlée (l'hôte seul).
            if (m_BloqueParBouclier && !m_Visuel_Seul && BouclierNyxessa.Instance != null)
                BouclierNyxessa.Instance.Absorber(new InfoDegats
                {
                    montant = m_Degats, equipeSource = m_Equipe, source = m_Source, parable = false, aDistance = true,
                    point = transform.position, direction = transform.forward
                });
            if (!m_BloqueParBouclier && !m_Visuel_Seul && m_Cible != null && !m_Cible.Mort && (m_Cible.transform.position + Vector3.up - transform.position).sqrMagnitude < 4f)
            {
                m_Cible.Encaisser(new InfoDegats
                {
                    montant = m_Degats, equipeSource = m_Equipe, source = m_Source, parable = m_Parable, aDistance = true,
                    point = transform.position, direction = transform.forward
                });
            }
            if (m_Visuel != null) m_Visuel.Shatter();
            if (m_Boucle != null) m_Boucle.Stop();
            AudioBank.Jouer(m_ParNyxessa ? SonsDuJeu.MissileEclat : SonsDuJeu.MissileEclatEnnemi, transform.position, 0.8f);
            Destroy(gameObject, 1.5f);
        }
    }
}
