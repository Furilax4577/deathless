using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Vagues de la nuit (wiki : deroule) : au crépuscule, tirage et annonce des clairières actives (zones d'apparition) ;
    /// la nuit, vagues à 0 / 40 / 80 s (0 / 30 / 60 / 90 dès la nuit 9), sorties de terre étalées et réparties entre les
    /// clairières actives, plafond de 60 squelettes (au-delà, PV répartis sur les vivants) ; à l'aube, extinction des
    /// zones et désintégration des squelettes debout. Version 0.1 : sbires et guerriers ; voleurs, mages (dont le mage
    /// lanceur de crâne de la nuit 6) et élites sont des guerriers ; Golem nuit 10, Nécromancien nuit 12.
    public class DirecteurVagues : MonoBehaviour
    {
        public static DirecteurVagues Instance { get; private set; }

        [Tooltip("Centres des trois clairières : Nord, Sud-Est, Sud-Ouest (SpawnPoint du village).")]
        public Transform[] clairieres;
        [Tooltip("Zones d'apparition (même ordre que les clairières).")]
        public ZoneApparition[] zones;
        public GameObject prefabSbire, prefabGuerrier, prefabGolem, prefabNecromancien;
        public Transform conteneur;

        readonly List<Squelette> m_Vivants = new List<Squelette>();
        readonly List<Sortie> m_AFaire = new List<Sortie>();
        public IReadOnlyList<Squelette> Vivants => m_Vivants;

        struct Sortie { public float instant; public TypeEnnemi type; public int clairiere; public bool elite; }

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;
        int m_VagueLancee;
        float[] m_Departs;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (P != null) P.PhaseChangee += OnPhase;
        }

        void OnPhase(Phase avant, Phase apres)
        {
            switch (apres)
            {
                case Phase.Crepuscule: Preparer(P.Etat.nuit); break;
                case Phase.Nuit:
                    foreach (int c in P.Etat.vagues.clairieresActives) Zone(c)?.Activer();
                    break;
                case Phase.Aube: Aube(); break;
                case Phase.Terminee:
                    m_AFaire.Clear();
                    foreach (var z in zones) if (z != null) z.Eteindre();
                    break;
            }
        }

        ZoneApparition Zone(int i) => zones != null && i >= 0 && i < zones.Length ? zones[i] : null;

        /// Crépuscule : plan de la nuit et annonce des clairières actives.
        void Preparer(int nuit)
        {
            var b = B;
            var v = P.Etat.vagues;
            v.clairieresActives.Clear();
            int nbClairieres = Mathf.Clamp(GameBalance.ParNuit(b.clairieresParNuit, nuit, 3), 1, clairieres.Length);
            var toutes = new List<int> { 0, 1, 2 };
            for (int i = 0; i < nbClairieres; i++)
            {
                int k = Random.Range(0, toutes.Count);
                v.clairieresActives.Add(toutes[k]);
                toutes.RemoveAt(k);
            }
            foreach (int c in v.clairieresActives) Zone(c)?.Annoncer();

            bool quatre = nuit >= b.nuitQuatreVagues;
            m_Departs = quatre ? b.departsQuatreVagues : b.departsTroisVagues;
            float[] parts = quatre ? b.partsQuatreVagues : b.partsTroisVagues;
            int joueurs = Mathf.Max(1, P.Etat.joueurs.Count);
            int total = Mathf.RoundToInt(GameBalance.ParNuit(b.ennemisParNuit, nuit, 48) * (1f + b.ennemisParJoueurEnPlus * (joueurs - 1)));
            float partGuerriers = GameBalance.ParNuit(b.partGuerriers, nuit, 0.5f);
            int elites = nuit >= 7 ? 2 : nuit >= 5 ? 1 : 0;
            m_AFaire.Clear();
            int pose = 0;
            float vit = Mathf.Max(0.01f, b.vitesseCycle);
            for (int w = 0; w < m_Departs.Length; w++)
            {
                int n = w == m_Departs.Length - 1 ? total - pose : Mathf.RoundToInt(total * parts[Mathf.Min(w, parts.Length - 1)]);
                pose += n;
                for (int i = 0; i < n; i++)
                {
                    var s = new Sortie
                    {
                        instant = (m_Departs[w] + b.etalementVague * i / Mathf.Max(1, n)) / vit,
                        type = Random.value < partGuerriers ? TypeEnnemi.Guerrier : TypeEnnemi.Sbire,
                        clairiere = v.clairieresActives[i % v.clairieresActives.Count]
                    };
                    m_AFaire.Add(s);
                }
            }
            // Élites (guerriers renforcés) dans les vagues du milieu.
            for (int e = 0; e < elites && e < m_AFaire.Count; e++)
            {
                int idx = Mathf.Clamp(m_AFaire.Count / 2 + e * 3, 0, m_AFaire.Count - 1);
                var s = m_AFaire[idx]; s.type = TypeEnnemi.Guerrier; s.elite = true; m_AFaire[idx] = s;
            }
            // Boss dans la dernière vague.
            if (b.bossActives)
            {
                float dernier = m_Departs[m_Departs.Length - 1] / vit + 1f;
                int cl = v.clairieresActives[Random.Range(0, v.clairieresActives.Count)];
                if (nuit == b.nuitGolem && prefabGolem != null) m_AFaire.Add(new Sortie { instant = dernier, type = TypeEnnemi.Golem, clairiere = cl });
                if (nuit == b.nuitNecromancien && prefabNecromancien != null) m_AFaire.Add(new Sortie { instant = dernier, type = TypeEnnemi.Necromancien, clairiere = cl });
            }
            m_AFaire.Sort((a, c) => a.instant.CompareTo(c.instant));
            v.total = m_Departs.Length;
            v.vague = 0;
            v.restantsASortir = m_AFaire.Count;
            m_VagueLancee = 0;
            P.Journal("Nuit " + nuit + " préparée : " + total + " ennemis (+" + (m_AFaire.Count - total) + " boss), " + m_Departs.Length + " vagues, clairières " + string.Join(",", v.clairieresActives));
        }

        void Update()
        {
            if (P == null || P.Etat.phase != Phase.Nuit || !P.EnCours) return;
            float t = P.Etat.tempsPhase;
            var v = P.Etat.vagues;
            float vit = Mathf.Max(0.01f, B.vitesseCycle);
            while (m_Departs != null && m_VagueLancee < m_Departs.Length && t >= m_Departs[m_VagueLancee] / vit)
            {
                m_VagueLancee++;
                v.vague = m_VagueLancee;
                foreach (int c in v.clairieresActives) Zone(c)?.Pulse();
                AudioBank.Jouer2D(SonsDuJeu.Vague, 0.8f);
                P.Journal("Vague " + m_VagueLancee + " / " + m_Departs.Length);
            }
            while (m_AFaire.Count > 0 && m_AFaire[0].instant <= t)
            {
                var s = m_AFaire[0];
                m_AFaire.RemoveAt(0);
                Poser(s.type, PointDans(s.clairiere), s.elite, true);
            }
            v.restantsASortir = m_AFaire.Count;
            v.ennemisVivants = m_Vivants.Count;
        }

        Vector3 PointDans(int clairiere)
        {
            var z = Zone(clairiere);
            Vector3 p = z != null ? z.PointAleatoire() : clairieres[clairiere].position + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) p = hit.position;
            return p;
        }

        /// Pose un squelette (sortie de terre). Au-delà du plafond, ses PV sont répartis sur les vivants.
        public Squelette Poser(TypeEnnemi type, Vector3 point, bool elite, bool compterPlafond)
        {
            var b = B;
            float mult = GameBalance.ParNuit(b.multiplicateurPV, P != null ? P.Etat.nuit : 1, 1f);
            var stats = type == TypeEnnemi.Guerrier ? b.guerrier : b.sbire;
            if (compterPlafond && m_Vivants.Count >= b.plafondSquelettes && type != TypeEnnemi.Golem && type != TypeEnnemi.Necromancien)
            {
                float bonus = stats.pv * mult * (elite ? 3f : 1f);
                if (m_Vivants.Count > 0) foreach (var s in m_Vivants) s.Sante.Renforcer(bonus / m_Vivants.Count);
                return null;
            }
            GameObject prefab = type == TypeEnnemi.Guerrier ? prefabGuerrier : type == TypeEnnemi.Golem ? prefabGolem : type == TypeEnnemi.Necromancien ? prefabNecromancien : prefabSbire;
            if (prefab == null) return null;
            Vector3 vers = (P != null && P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero) - point; vers.y = 0f;
            var go = Instantiate(prefab, point, vers.sqrMagnitude > 0.01f ? Quaternion.LookRotation(vers) : Quaternion.identity, conteneur);
            var sq = go.GetComponent<Squelette>();
            sq.type = type;
            sq.elite = elite;
            if (elite)
            {
                stats = new StatsSquelette { pv = stats.pv * 3f, vitesse = stats.vitesse, degatsJoueur = stats.degatsJoueur * 1.5f, degatsNyxessa = stats.degatsNyxessa * 1.5f, intervalle = stats.intervalle, preparation = stats.preparation, portee = stats.portee + 0.3f };
                go.transform.localScale *= 1.25f;
            }
            sq.Agent.Warp(point);
            sq.Initialiser(stats, mult);
            sq.Retire += OnRetire;
            m_Vivants.Add(sq);
            if (type == TypeEnnemi.Golem || type == TypeEnnemi.Necromancien) P?.Journal("Boss : " + type + " sort de terre");
            return sq;
        }

        /// Invocation du Nécromancien : sbires qui sortent de terre autour de lui (comptés dans le plafond).
        public int Invoquer(Necromancien necro, int n)
        {
            int poses = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = necro.transform.position + Quaternion.Euler(0f, i * 360f / n + Random.Range(-20f, 20f), 0f) * Vector3.forward * Random.Range(2.5f, 4f);
                if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) continue;
                var s = Poser(TypeEnnemi.Sbire, hit.position, false, true);
                if (s == null) continue;
                poses++;
                s.Retire += _ => { if (necro != null) necro.Invoques--; };
            }
            return poses;
        }

        void OnRetire(Squelette s)
        {
            m_Vivants.Remove(s);
            if (P != null) P.Etat.vagues.ennemisVivants = m_Vivants.Count;
        }

        void Aube()
        {
            m_AFaire.Clear();
            foreach (var z in zones) if (z != null && z.Etat != EtatZone.Eteinte) z.Eteindre();
            var restants = new List<Squelette>(m_Vivants);
            float etal = Mathf.Min(B.etalementAube, B.Duree(Phase.Aube) * 0.8f);
            foreach (var s in restants)
            {
                if (s == null) continue;
                float d = Random.Range(0f, etal);
                StartCoroutine(DesintegrerApres(s, d));
            }
            if (restants.Count > 0) P?.Journal("Aube : " + restants.Count + " squelettes se désintègrent");
        }

        System.Collections.IEnumerator DesintegrerApres(Squelette s, float d)
        {
            yield return new WaitForSeconds(d);
            if (s != null) s.Desintegrer(true);
        }
    }
}
