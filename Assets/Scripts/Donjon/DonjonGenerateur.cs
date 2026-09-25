using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Donjon
{
    /// Construit le donjon d'une graine : plan (DonjonPlan), pièces KayKit, collisions, repères, torches, zones de
    /// masquage, puis NavMesh (NavMeshSurface, paquet AI Navigation). Utilisable en jeu : un donjon neuf chaque nuit,
    /// même graine = même donjon pour tous les joueurs (seule la graine passe par le réseau).
    ///
    /// Les objets sont gardés en réserve et réutilisés d'une génération à l'autre : après la première, une
    /// régénération n'instancie plus rien (déplacements, activations et NavMesh seulement). Les collisions sont
    /// des objets à part (boîtes simples), jamais mises à l'échelle par le masquage des étages.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DonjonMasquage))]
    public class DonjonGenerateur : MonoBehaviour
    {
        public DonjonKit kit;
        [Tooltip("Graine du donjon (en jeu : tirée chaque nuit par le serveur et envoyée à tous).")]
        public int graine = 1;
        [NonSerialized] int m_AireEau = -1;
        public bool genererAuDemarrage = true;
        public bool construireNavMesh = true;

        // ------------------------------------------------------------------ Résultat
        public DonjonPlan Plan { get { return m_Plan; } }
        public DonjonRepere Arrivee { get; private set; }
        public DonjonRepere PortailRetour { get; private set; }
        public readonly DonjonRepere[] Butins = new DonjonRepere[DonjonPlan.NbButins];
        public readonly DonjonRepere[] Apparitions = new DonjonRepere[DonjonPlan.NbApparitions];
        public float DernierTempsPlanMs { get; private set; }
        public float DernierTempsConstructionMs { get; private set; }
        public float DernierTempsNavMeshMs { get; private set; }
        public long DerniereAllocationOctets { get; private set; }
        public int NbObjets { get; private set; }
        public int NbLumieres { get; private set; }
        public event Action<DonjonGenerateur> Genere;

        const int NbGroupes = DonjonPlan.NbBlocs * DonjonPlan.NbNiveaux;
        const float C = DonjonPlan.Cellule, H = DonjonPlan.HauteurNiveau;
        static readonly float PenteEscalier = Mathf.Atan2(H, 2f * C) * Mathf.Rad2Deg; // 26,57°

        // État d'exécution : jamais sérialisé (sinon un rechargement de domaine garderait les conteneurs mais
        // perdrait les réserves, et la génération suivante dupliquerait tout).
        [NonSerialized] readonly DonjonPlan m_Plan = new DonjonPlan();
        [NonSerialized] DonjonMasquage m_Masquage;
        [NonSerialized] NavMeshSurface m_Surface;
        [NonSerialized] Transform m_Racine, m_Visuels, m_Collisions, m_Reperes, m_Zones, m_Lumieres, m_Reserve;
        readonly Transform[] m_GroupesVisuels = new Transform[NbGroupes];
        readonly Transform[] m_GroupesCollisions = new Transform[NbGroupes];

        sealed class Reserve
        {
            public GameObject modele;
            public readonly List<GameObject> objets = new List<GameObject>(64);
            public int utilises;
            public bool bornesOk;
            public Bounds bornes;
        }
        readonly Dictionary<GameObject, Reserve> m_Reserves = new Dictionary<GameObject, Reserve>();
        readonly List<Reserve> m_ListeReserves = new List<Reserve>();
        readonly List<BoxCollider> m_Boites = new List<BoxCollider>(1024);
        [NonSerialized] int m_BoitesUtilisees;
        readonly List<Light> m_Lampes = new List<Light>(64);
        [NonSerialized] int m_LampesUtilisees;
        [NonSerialized] GameObject m_Anneau;
        [NonSerialized] Transform m_DernierVisuel;
        static Mesh s_MeshAnneau;

        static string[] s_NomsApparitions;
        readonly int[] m_TypeNomme = new int[DonjonPlan.NbApparitions];

        void Awake()
        {
            if (genererAuDemarrage && Application.isPlaying) Generer(graine);
        }

        // ================================================================== Génération
        /// Génère et construit le donjon de la graine. Renvoie false si le plan n'a pas pu respecter la consigne.
        public bool Generer(int graineDonjon)
        {
            graine = graineDonjon;
            long alloc0 = GC.GetTotalMemory(false);
            var chrono = System.Diagnostics.Stopwatch.StartNew();
            bool ok = false;
            float plan = 0f, pieces = 0f, nav = 0f;
            int premier = 0;
            // Le plan garantit la consigne sur sa grille ; le NavMesh construit la vérifie en vrai (escaliers dans les
            // deux sens, chaque butin atteignable depuis l'arrivée). S'il refuse, on passe à l'essai suivant de la même
            // graine : tout reste déterministe, et le serveur peut transmettre (graine, essai) pour trancher.
            for (int relance = 0; relance < 6; relance++)
            {
                chrono.Restart();
                ok = m_Plan.Generer(graineDonjon, premier);
                plan += (float)chrono.Elapsed.TotalMilliseconds;
                if (!ok) Debug.LogWarning("Donjon : graine " + graineDonjon + " sans plan valide après " + DonjonPlan.MaxEssais + " essais (dernier essai conservé).");
                chrono.Restart();
                Construire();
                pieces += (float)chrono.Elapsed.TotalMilliseconds;
                if (!construireNavMesh) break;
                chrono.Restart();
                ConstruireNavMesh();
                nav += (float)chrono.Elapsed.TotalMilliseconds;
                bool escaliersOk = VerifierEscaliers(), butinsOk = VerifierButins();
                if (escaliersOk && butinsOk) break;
                ok = false;
                if (relance == 5) Debug.LogError("Donjon graine " + graineDonjon + " : " + DefautsEscaliers + DefautsButins);
                premier = m_Plan.essais;
            }
            Relances = premier == 0 ? 0 : 1;
            DernierTempsPlanMs = plan; DernierTempsConstructionMs = pieces; DernierTempsNavMeshMs = nav;
            DerniereAllocationOctets = GC.GetTotalMemory(false) - alloc0;
            if (Genere != null) Genere(this);
            return ok;
        }

        /// 1 si le NavMesh a fait refaire le plan à la dernière génération.
        public int Relances { get; private set; }
        public string DefautsButins { get; private set; }

        /// Chaque butin est atteignable par le NavMesh depuis l'arrivée, et le portail de retour aussi.
        public bool VerifierButins()
        {
            DefautsButins = "";
            NavMeshHit ha;
            if (!NavMesh.SamplePosition(Arrivee.transform.position, out ha, 1f, NavMesh.AllAreas)) { DefautsButins = "arrivée hors NavMesh. "; return false; }
            for (int i = 0; i < DonjonPlan.NbButins; i++)
            {
                NavMeshHit hb;
                Vector3 p = Butins[i].transform.position;
                if (!NavMesh.SamplePosition(p, out hb, 2.5f, NavMesh.AllAreas) || LongueurChemin(ha.position, hb.position) < 0f)
                    DefautsButins += "butin " + i + " inaccessible. ";
            }
            NavMeshHit hp;
            if (!NavMesh.SamplePosition(PortailRetour.transform.position + PortailRetour.transform.forward, out hp, 1.5f, NavMesh.AllAreas) || LongueurChemin(ha.position, hp.position) < 0f)
                DefautsButins += "portail de retour inaccessible. ";
            return DefautsButins.Length == 0;
        }

        void ConstruireNavMesh()
        {
            if (m_Surface == null) m_Surface = GetComponent<NavMeshSurface>();
            if (m_Surface == null) m_Surface = gameObject.AddComponent<NavMeshSurface>();
            m_Surface.collectObjects = CollectObjects.Children;
            m_Surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            m_Surface.layerMask = ~(1 << 2);          // pas les zones de masquage (Ignore Raycast)
            m_Surface.agentTypeID = 0;                // Humanoid : rayon 0,5 m, hauteur 2 m, pente 45°, marche 0,75 m
            // Eau : coût 1 / 0,6 (déjà réglé dans les zones du projet ; rappelé ici au cas où).
            if (m_AireEau >= 0) NavMesh.SetAreaCost(m_AireEau, 1f / DonjonPlan.FacteurVitesseEau);
            Physics.SyncTransforms();
            m_Surface.BuildNavMesh();
        }

        /// Position monde d'une pose du plan.
        public Vector3 PositionMonde(Pose p) { return transform.TransformPoint(new Vector3(p.x, p.y, p.z)); }

        // ================================================================== Construction
        void Construire()
        {
            if (kit == null) { Debug.LogError("Donjon : aucun kit de pièces (DonjonKit) assigné."); return; }
            Preparer();
            m_Masquage.Preparer(m_Plan);
            foreach (var r in m_ListeReserves) r.utilises = 0;
            m_BoitesUtilisees = 0; m_LampesUtilisees = 0;

            PoserSols();
            PoserBords();
            PoserSommets();
            PoserEscaliers();
            PoserButins();
            PoserApparitions();
            PoserDecor();
            PoserTorches();
            PoserPortail();
            PoserBassin();

            // Rendre à la réserve ce qui n'a pas servi cette fois.
            int n = 0;
            foreach (var r in m_ListeReserves)
            {
                for (int i = r.utilises; i < r.objets.Count; i++)
                    if (r.objets[i].activeSelf) { r.objets[i].SetActive(false); r.objets[i].transform.SetParent(m_Reserve, false); }
                n += r.utilises;
            }
            for (int i = m_BoitesUtilisees; i < m_Boites.Count; i++)
                if (m_Boites[i].gameObject.activeSelf) { m_Boites[i].gameObject.SetActive(false); m_Boites[i].transform.SetParent(m_Reserve, false); }
            for (int i = m_LampesUtilisees; i < m_Lampes.Count; i++) if (m_Lampes[i].gameObject.activeSelf) m_Lampes[i].gameObject.SetActive(false);
            NbObjets = n + m_BoitesUtilisees;
            NbLumieres = m_LampesUtilisees;
            m_Masquage.Terminer();
        }

        /// Crée une fois pour toutes la hiérarchie (conteneurs, groupes, zones). Après un rechargement de domaine
        /// (éditeur), les réserves sont perdues : l'ancienne construction est alors détruite et refaite.
        void Preparer()
        {
            if (m_Masquage == null) m_Masquage = GetComponent<DonjonMasquage>();
            if (m_Racine != null) return;
            Transform ancien = transform.Find("Genere");
            if (ancien != null) Detruire(ancien.gameObject);
            m_Reserves.Clear(); m_ListeReserves.Clear(); m_Boites.Clear(); m_Lampes.Clear(); m_Anneau = null;
            m_Racine = Enfant(transform, "Genere");
            m_Visuels = Enfant(m_Racine, "Visuels");
            m_Collisions = Enfant(m_Racine, "Collisions");
            m_Reperes = Enfant(m_Racine, "Reperes");
            m_Lumieres = Enfant(m_Racine, "Lumieres");
            m_Zones = Enfant(m_Racine, "Zones");
            m_Reserve = Enfant(m_Racine, "Reserve");
            for (int b = 0; b < DonjonPlan.NbBlocs; b++)
                for (int k = 0; k < DonjonPlan.NbNiveaux; k++)
                {
                    int g = b * DonjonPlan.NbNiveaux + k;
                    string nom = "B" + b.ToString("00") + "_N" + k;
                    m_GroupesVisuels[g] = Enfant(m_Visuels, nom);
                    m_GroupesCollisions[g] = Enfant(m_Collisions, nom);
                    var dg = m_GroupesCollisions[g].gameObject.AddComponent<DonjonGroupe>();
                    dg.index = g; dg.masquage = m_Masquage;
                }
            // Zones : un volume par bloc et par niveau (4 m), plus un au-dessus du dernier niveau pour la caméra.
            for (int b = 0; b < DonjonPlan.NbBlocs; b++)
                for (int k = 0; k <= DonjonPlan.NbNiveaux; k++)
                {
                    var go = new GameObject("Zone_B" + b.ToString("00") + "_N" + k);
                    go.layer = 2;
                    go.transform.SetParent(m_Zones, false);
                    float bx = (b % DonjonPlan.BlocsX + 0.5f) * DonjonPlan.BlocTaille * C, bz = (b / DonjonPlan.BlocsX + 0.5f) * DonjonPlan.BlocTaille * C;
                    float y0 = k == 0 ? -DonjonPlan.ProfondeurBassin - 0.5f : k * H - 0.5f, y1 = k < DonjonPlan.NbNiveaux ? k * H + 3.5f : k * H + 30f;
                    go.transform.localPosition = new Vector3(bx, (y0 + y1) * 0.5f, bz);
                    var bc = go.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.size = new Vector3(DonjonPlan.BlocTaille * C, y1 - y0, DonjonPlan.BlocTaille * C);
                    var z = go.AddComponent<DonjonZone>();
                    z.bloc = b; z.niveau = Mathf.Min(k, DonjonPlan.NbNiveaux - 1); z.masquage = m_Masquage;
                }
            if (s_NomsApparitions == null)
            {
                s_NomsApparitions = new string[DonjonPlan.NbApparitions * 4];
                for (int i = 0; i < DonjonPlan.NbApparitions; i++)
                    for (int t = 0; t < 4; t++) s_NomsApparitions[i * 4 + t] = "Apparition_" + ((TypeApparition)t) + "_" + (i + 1).ToString("00");
            }
            // Ordre fixe du plan : 0 grand coffre, 1-2 coffres, 3-6 tas d'or.
            for (int i = 0; i < DonjonPlan.NbButins; i++)
                Butins[i] = NouveauRepere("Butin_" + (i + 1) + "_" + (i == 0 ? "GrandCoffre" : i < 3 ? "Coffre" : "TasOr"), DonjonRepere.Genre.Butin, i);
            for (int i = 0; i < DonjonPlan.NbApparitions; i++) m_TypeNomme[i] = -1;
            for (int i = 0; i < DonjonPlan.NbApparitions; i++) Apparitions[i] = NouveauRepere("Apparition", DonjonRepere.Genre.Apparition, i);
            Arrivee = NouveauRepere("Arrivee", DonjonRepere.Genre.Arrivee, 0);
            PortailRetour = NouveauRepere("PortailRetour", DonjonRepere.Genre.PortailRetour, 0);
        }

        DonjonRepere NouveauRepere(string nom, DonjonRepere.Genre genre, int index)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(m_Reperes, false);
            var r = go.AddComponent<DonjonRepere>();
            r.genre = genre; r.index = index;
            return r;
        }

        static Transform Enfant(Transform parent, string nom)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Detruire(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ Réserves
        GameObject Prendre(GameObject modele, int groupe, Vector3 pos, float rotY, Vector3 echelle)
        {
            if (modele == null) return null;
            Reserve r;
            if (!m_Reserves.TryGetValue(modele, out r))
            {
                r = new Reserve { modele = modele };
                m_Reserves.Add(modele, r); m_ListeReserves.Add(r);
            }
            GameObject go;
            if (r.utilises < r.objets.Count) go = r.objets[r.utilises];
            else
            {
                go = Instantiate(modele);
                go.name = modele.name;
                foreach (var col in go.GetComponentsInChildren<Collider>()) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
                r.objets.Add(go);
            }
            r.utilises++;
            Transform t = go.transform;
            Transform parent = m_GroupesVisuels[groupe];
            if (t.parent != parent) t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, rotY, 0f);
            t.localScale = echelle;
            if (!go.activeSelf) go.SetActive(true);
            m_Masquage.Ajouter(groupe, t);
            m_DernierVisuel = t;
            return go;
        }

        GameObject Prendre(GameObject modele, int groupe, Vector3 pos, float rotY)
        {
            return Prendre(modele, groupe, pos, rotY, Vector3.one);
        }

        /// Bornes locales d'un modèle (toutes ses mailles), calculées une fois.
        Bounds Bornes(GameObject modele)
        {
            Reserve r;
            if (!m_Reserves.TryGetValue(modele, out r) || r.objets.Count == 0) return new Bounds(Vector3.up * 0.5f, Vector3.one);
            if (r.bornesOk) return r.bornes;
            Transform racine = r.objets[0].transform;
            bool premier = true;
            Bounds b = new Bounds();
            foreach (var mf in racine.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 coin = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = racine.InverseTransformPoint(mf.transform.TransformPoint(coin));
                    p = Vector3.Scale(p, racine.localScale);
                    if (premier) { b = new Bounds(p, Vector3.zero); premier = false; } else b.Encapsulate(p);
                }
            }
            r.bornes = b; r.bornesOk = true;
            return b;
        }

        void Boite(int groupe, Vector3 pos, Quaternion rot, Vector3 centre, Vector3 taille)
        {
            BoxCollider bc;
            if (m_BoitesUtilisees < m_Boites.Count) bc = m_Boites[m_BoitesUtilisees];
            else
            {
                var go = new GameObject("Boite");
                bc = go.AddComponent<BoxCollider>();
                m_Boites.Add(bc);
            }
            m_BoitesUtilisees++;
            Transform t = bc.transform;
            Transform parent = m_GroupesCollisions[groupe];
            if (t.parent != parent) t.SetParent(parent, false);
            t.localPosition = pos; t.localRotation = rot; t.localScale = Vector3.one;
            bc.center = centre; bc.size = taille;
            if (!bc.gameObject.activeSelf) bc.gameObject.SetActive(true);
        }

        void BoiteModele(GameObject modele, int groupe, Vector3 pos, float rotY)
        {
            Bounds b = Bornes(modele);
            Boite(groupe, pos, Quaternion.Euler(0f, rotY, 0f), b.center, b.size);
        }

        void Lampe(Vector3 pos, Color couleur, float intensite, float portee)
        {
            Light l;
            if (m_LampesUtilisees < m_Lampes.Count) l = m_Lampes[m_LampesUtilisees];
            else
            {
                var go = new GameObject("Lumiere");
                go.transform.SetParent(m_Lumieres, false);
                l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;           // pas d'ombres temps réel : ~40 lumières ponctuelles
                l.renderMode = LightRenderMode.Auto;
                m_Lampes.Add(l);
            }
            m_LampesUtilisees++;
            l.transform.localPosition = pos;
            l.color = couleur; l.intensity = intensite; l.range = portee;
            if (!l.gameObject.activeSelf) l.gameObject.SetActive(true);
        }

        static int Groupe(int bloc, int niveau) { return bloc * DonjonPlan.NbNiveaux + niveau; }

        static uint Hache(int n)
        {
            uint h = (uint)n * 2654435761u;
            h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12;
            return h;
        }

        static GameObject Choisir(GameObject[] liste, uint h)
        {
            if (liste == null || liste.Length == 0) return null;
            return liste[(int)(h % (uint)liste.Length)];
        }

        // ------------------------------------------------------------------ Sols
        void PoserSols()
        {
            for (int noeud = 0; noeud < DonjonPlan.NbNoeuds; noeud++)
            {
                if (!m_Plan.plein[noeud]) continue;
                int k = noeud / DonjonPlan.NbCellules, c = noeud % DonjonPlan.NbCellules;
                int g = Groupe(DonjonPlan.BlocDe(c), k);
                uint h = Hache(noeud + m_Plan.graine * 7919);
                GameObject m = k == 0 ? Choisir(kit.solsRez, h >> 3) : m_Plan.materiau[noeud] == 1 ? Choisir(kit.solsBois, h >> 3) : Choisir(kit.solsPierre, h >> 3);
                Vector3 p = new Vector3((c % DonjonPlan.Largeur + 0.5f) * C, m_Plan.HauteurSol(noeud), (c / DonjonPlan.Largeur + 0.5f) * C);
                Prendre(m, g, p, (h & 3) * 90f);
                if (k > 0 && kit.plafond != null) Prendre(kit.plafond, g, p + Vector3.down * 0.18f, 0f, new Vector3(1f, -1f, 1f));
                Boite(g, p, Quaternion.identity, new Vector3(0f, -0.1f, 0f), new Vector3(C, 0.3f, C));
            }
        }

        // ------------------------------------------------------------------ Murs et garde-corps
        void PoserBords()
        {
            int L = DonjonPlan.Largeur, P = DonjonPlan.Profondeur;
            for (int k = 0; k < DonjonPlan.NbNiveaux; k++)
            {
                for (int vy = 0; vy <= P; vy++)
                    for (int x = 0; x < L; x++)
                    {
                        Bord b = m_Plan.BordH(k, x, vy);
                        if (b == Bord.Rien) continue;
                        int ca = vy > 0 ? x + (vy - 1) * L : -1, cb = vy < P ? x + vy * L : -1;
                        PoserBord(k, b, ChoisirCellule(k, ca, cb), new Vector3((x + 0.5f) * C, k * H, vy * C), 0f, x * 131 + vy * 17 + k * 7, cb >= 0 && m_Plan.eau[cb] ? 0f : 180f);
                    }
                for (int y = 0; y < P; y++)
                    for (int vx = 0; vx <= L; vx++)
                    {
                        Bord b = m_Plan.BordV(k, vx, y);
                        if (b == Bord.Rien) continue;
                        int ca = vx > 0 ? vx - 1 + y * L : -1, cb = vx < L ? vx + y * L : -1;
                        PoserBord(k, b, ChoisirCellule(k, ca, cb), new Vector3(vx * C, k * H, (y + 0.5f) * C), 90f, vx * 71 + y * 29 + k * 13 + 5, cb >= 0 && m_Plan.eau[cb] ? 90f : 270f);
                    }
            }
        }

        /// Cellule qui porte un bord : celle qui est praticable au niveau k, sinon celle qui existe.
        int ChoisirCellule(int k, int a, int b)
        {
            if (a >= 0 && m_Plan.plein[k * DonjonPlan.NbCellules + a]) return a;
            if (b >= 0 && m_Plan.plein[k * DonjonPlan.NbCellules + b]) return b;
            return a >= 0 ? a : b;
        }

        /// versBassin : direction (degrés) vers le côté bassin d'un muret de bassin.
        void PoserBord(int k, Bord b, int cellule, Vector3 p, float rot, int graineLocale, float versBassin)
        {
            int g = Groupe(DonjonPlan.BlocDe(cellule), k);
            uint h = Hache(graineLocale + m_Plan.graine * 104729);
            Quaternion r = Quaternion.Euler(0f, rot, 0f);
            switch (b)
            {
                case Bord.GardeCorps:
                    Prendre(kit.gardeCorps, g, p, rot);
                    Boite(g, p, r, new Vector3(0f, 0.6f, 0f), new Vector3(C, 1.2f, 0.5f));
                    return;
                case Bord.Arcade:
                    // Poteaux et linteau de bois sous le bord du balcon : on passe dessous, seuls les poteaux arrêtent.
                    Prendre(kit.arcade, g, p, rot);
                    Boite(g, p, r, new Vector3(1.8f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 0.4f));
                    Boite(g, p, r, new Vector3(-1.8f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 0.4f));
                    return;
                case Bord.MurBas:
                    Prendre(Choisir(kit.murs, h >> 4), g, p, rot + ((h >> 2 & 1) == 0 ? 0f : 180f), new Vector3(1f, 0.5f, 1f));
                    Boite(g, p, r, new Vector3(0f, H * 0.25f, 0f), new Vector3(C, H * 0.5f, 1f));
                    return;
                case Bord.Bassin:
                {
                    // Bloc de fondation de 2 m, face tournée vers l'eau, adossé sous la dalle du rez.
                    Quaternion vb = Quaternion.Euler(0f, versBassin, 0f);
                    Vector3 f = p - vb * Vector3.forward * 1.0f + Vector3.down * DonjonPlan.ProfondeurBassin;
                    Prendre(kit.fondationBassin, g, f, versBassin, new Vector3(2f, 1f, 1f));
                    Boite(g, p - vb * Vector3.forward * 0.25f, vb, new Vector3(0f, -DonjonPlan.ProfondeurBassin * 0.5f, 0f), new Vector3(C, DonjonPlan.ProfondeurBassin, 0.5f));
                    return;
                }
            }
            GameObject m = b == Bord.MurExterieur && k > 0 && (h & 3) == 0 ? Choisir(kit.mursHauts, h >> 4) : Choisir(kit.murs, h >> 4);
            Prendre(m, g, p, rot + ((h >> 2 & 1) == 0 ? 0f : 180f));
            Boite(g, p, r, new Vector3(0f, H * 0.5f, 0f), new Vector3(C, H, 1f));
        }

        void PoserSommets()
        {
            int L = DonjonPlan.Largeur, P = DonjonPlan.Profondeur;
            for (int k = 0; k < DonjonPlan.NbNiveaux; k++)
                for (int vy = 0; vy <= P; vy++)
                    for (int vx = 0; vx <= L; vx++)
                    {
                        byte s = m_Plan.Sommet(k, vx, vy);
                        if (s == 0) continue;
                        int cx = Mathf.Min(vx, L - 1), cy = Mathf.Min(vy, P - 1);
                        int cellule = cx + cy * L;
                        for (int q = 0; q < 4; q++)
                        {
                            int x = vx - 1 + (q & 1), y = vy - 1 + (q >> 1);
                            if (x < 0 || y < 0 || x >= L || y >= P) continue;
                            if (m_Plan.plein[k * DonjonPlan.NbCellules + x + y * L]) { cellule = x + y * L; break; }
                        }
                        int g = Groupe(DonjonPlan.BlocDe(cellule), k);
                        Vector3 p = new Vector3(vx * C, k * H, vy * C);
                        if (s == 1)
                        {
                            Prendre(kit.pilier, g, p, 0f);
                            Boite(g, p, Quaternion.identity, new Vector3(0f, H * 0.5f, 0f), new Vector3(1.5f, H, 1.5f));
                        }
                        else
                        {
                            Prendre(kit.poteau, g, p, 0f);
                            Boite(g, p, Quaternion.identity, new Vector3(0f, 0.7f, 0f), new Vector3(0.7f, 1.4f, 0.7f));
                        }
                    }
        }

        // ------------------------------------------------------------------ Escaliers
        /// Le modèle KayKit « stairs_long » a son pivot EN HAUT : il fait 4 m à z = 0 et descend vers +z jusqu'à
        /// 0,4 m à z = 8 (mesuré par lancers de rayons sur le maillage). On le pose donc pivot au bord du palier
        /// (entre la cellule haute et la cellule d'arrivée), tourné vers le bas de la montée.
        void PoserEscaliers()
        {
            for (int i = 0; i < m_Plan.nbEscaliers; i++)
            {
                Escalier e = m_Plan.escaliers[i];
                int g = Groupe(DonjonPlan.BlocDe(e.bas), e.niveau);
                Vector3 dir = new Vector3(DonjonPlan.DirX(e.dir), 0f, DonjonPlan.DirY(e.dir));
                float rot = DonjonPlan.Angle(dir.x, dir.z);
                Vector3 pied; float course, montee;
                Geometrie(e, out pied, out course, out montee);
                // Modèle posé pivot au sommet (bord du palier), tourné vers le bas de la montée.
                Vector3 sommet = pied + dir * course + Vector3.up * montee;
                if (e.bassin) Prendre(kit.escalierBassin, g, sommet + Vector3.down * montee, rot + 180f, new Vector3(1f, montee / H, 1f));
                else Prendre(kit.escalier, g, sommet + Vector3.down * montee, rot + 180f);
                // Collisions dans le repère de la montée (origine au pied, +z vers le haut) : rampe calée sur le nez
                // des marches, bande plate au sommet, flancs pleins (garde-corps de l'escalier), massif dessous.
                Quaternion ry = Quaternion.Euler(0f, rot, 0f), pente = Quaternion.Euler(-PenteEscalier, 0f, 0f), rampe = ry * pente;
                Vector3 n = pente * Vector3.up;
                float decal = 0.25f * montee / H;
                Vector3 a = new Vector3(0f, 0f, -decal * course / montee), bb = new Vector3(0f, montee + 0.05f, (montee + 0.05f - decal) * (course / montee));
                Vector3 milieu = (a + bb) * 0.5f;
                float longueur = (bb - a).magnitude;
                Boite(g, pied + ry * (milieu - n * 0.15f), rampe, Vector3.zero, new Vector3(2f, 0.3f, longueur));
                Boite(g, pied + ry * new Vector3(0f, montee - 0.1f, course - 0.2f), ry, Vector3.zero, new Vector3(2f, 0.3f, 0.5f));
                Boite(g, pied + ry * (milieu + n * 0.45f + Vector3.right * 1.75f), rampe, Vector3.zero, new Vector3(1.5f, 1.3f, longueur));
                Boite(g, pied + ry * (milieu + n * 0.45f + Vector3.left * 1.75f), rampe, Vector3.zero, new Vector3(1.5f, 1.3f, longueur));
                Boite(g, pied + ry * new Vector3(0f, montee * 0.275f, course * 0.75f), ry, Vector3.zero, new Vector3(5f, montee * 0.55f, course * 0.5f));
                Boite(g, pied + ry * new Vector3(0f, montee * 0.125f, course * 0.375f), ry, Vector3.zero, new Vector3(5f, montee * 0.25f, course * 0.25f));
            }
        }

        /// Pied de la montée (bord bas), longueur horizontale et hauteur d'un escalier.
        void Geometrie(Escalier e, out Vector3 pied, out float course, out float montee)
        {
            Vector3 dir = new Vector3(DonjonPlan.DirX(e.dir), 0f, DonjonPlan.DirY(e.dir));
            if (e.bassin)
            {
                course = C; montee = DonjonPlan.ProfondeurBassin;
                pied = CentreCellule(e.bas, 0) - dir * (C * 0.5f) + Vector3.down * montee;
            }
            else
            {
                course = 2f * C; montee = H;
                pied = CentreCellule(e.bas, e.niveau) - dir * (C * 0.5f);
            }
        }

        static Vector3 CentreCellule(int c, int k)
        {
            return new Vector3((c % DonjonPlan.Largeur + 0.5f) * C, k * H, (c / DonjonPlan.Largeur + 0.5f) * C);
        }

        // ------------------------------------------------------------------ Vérification des escaliers
        /// Résultat de la dernière vérification : nombre d'escaliers valides et détail des défauts.
        public int EscaliersValides { get; private set; }
        public string DefautsEscaliers { get; private set; }

        /// Pour chaque escalier : le NavMesh existe au pied (niveau bas) et au palier (niveau haut), un chemin NavMesh
        /// les relie dans les deux sens en passant par CET escalier (moins de 20 m, contre ~13 m attendus), et la
        /// rampe de collision est à la bonne hauteur au pied, au milieu et en haut.
        public bool VerifierEscaliers()
        {
            EscaliersValides = 0;
            System.Text.StringBuilder sb = null;
            for (int i = 0; i < m_Plan.nbEscaliers; i++)
            {
                Escalier e = m_Plan.escaliers[i];
                Vector3 pied; float course, montee;
                Geometrie(e, out pied, out course, out montee);
                Vector3 dir = new Vector3(DonjonPlan.DirX(e.dir), 0f, DonjonPlan.DirY(e.dir));
                Vector3 bas = transform.TransformPoint(pied - dir * (C * 0.5f));
                Vector3 haut = transform.TransformPoint(pied + dir * (course + C * 0.5f) + Vector3.up * montee);
                float attenduChemin = course + C + 3f;
                string defaut = null;
                NavMeshHit hb, hh;
                if (!NavMesh.SamplePosition(bas + Vector3.up * 0.3f, out hb, 0.8f, NavMesh.AllAreas) || Mathf.Abs(hb.position.y - bas.y) > 0.3f) defaut = "pas de NavMesh au pied";
                else if (!NavMesh.SamplePosition(haut + Vector3.up * 0.3f, out hh, 0.8f, NavMesh.AllAreas) || Mathf.Abs(hh.position.y - haut.y) > 0.3f) defaut = "pas de NavMesh au palier";
                else
                {
                    float l1 = LongueurChemin(hb.position, hh.position), l2 = LongueurChemin(hh.position, hb.position);
                    if (l1 < 0f || l1 > attenduChemin) defaut = "montée impossible par cet escalier (" + l1.ToString("F1") + " m)";
                    else if (l2 < 0f || l2 > attenduChemin) defaut = "descente impossible par cet escalier (" + l2.ToString("F1") + " m)";
                }
                for (int t = 0; t < 3 && defaut == null; t++)
                {
                    float u = course * (t == 0 ? 0.125f : t == 1 ? 0.5f : 0.875f);
                    float attendu = pied.y + 0.25f * montee / H + u * montee / course;
                    Vector3 o = transform.TransformPoint(pied + dir * u + Vector3.up * (montee + 2f));
                    RaycastHit rh;
                    if (!Physics.Raycast(o, Vector3.down, out rh, montee + 3f, ~(1 << 2), QueryTriggerInteraction.Ignore) || Mathf.Abs(rh.point.y - transform.TransformPoint(Vector3.up * attendu).y) > 0.2f)
                        defaut = "rampe à la mauvaise hauteur à " + u + " m";
                }
                if (defaut == null) { EscaliersValides++; continue; }
                if (sb == null) sb = new System.Text.StringBuilder();
                sb.Append("escalier ").Append(i).Append(e.bassin ? " (bassin)" : " (montée)").Append(" : ").Append(defaut).Append(". ");
            }
            DefautsEscaliers = sb == null ? "" : sb.ToString();
            return sb == null;
        }

        [NonSerialized] NavMeshPath m_Chemin;
        [NonSerialized] readonly Vector3[] m_Coins = new Vector3[64];

        /// Longueur (m) du chemin NavMesh complet entre deux points, -1 s'il n'existe pas.
        public float LongueurChemin(Vector3 a, Vector3 b)
        {
            if (m_Chemin == null) m_Chemin = new NavMeshPath();
            if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, m_Chemin) || m_Chemin.status != NavMeshPathStatus.PathComplete) return -1f;
            int n = m_Chemin.GetCornersNonAlloc(m_Coins);
            float l = 0f;
            for (int i = 1; i < n; i++) l += Vector3.Distance(m_Coins[i - 1], m_Coins[i]);
            return l;
        }

        // ------------------------------------------------------------------ Butins, apparitions, décor
        void PoserButins()
        {
            for (int i = 0; i < DonjonPlan.NbButins; i++)
            {
                Pose b = m_Plan.butins[i];
                TypeButin t = (TypeButin)b.type;
                GameObject m = t == TypeButin.GrandCoffre ? kit.grandCoffre : t == TypeButin.Coffre ? kit.coffre : Choisir(kit.tasOr, b.variante);
                int g = Groupe(b.Bloc, b.niveau);
                Vector3 p = new Vector3(b.x, b.y, b.z);
                GameObject v = Prendre(m, g, p, b.rotY);
                BoiteModele(m, g, p, b.rotY);
                DonjonRepere r = Butins[i];
                r.transform.localPosition = p; r.transform.localRotation = Quaternion.Euler(0f, b.rotY, 0f);
                r.niveau = b.niveau; r.butin = t; r.visuel = v;
            }
        }

        void PoserApparitions()
        {
            for (int i = 0; i < DonjonPlan.NbApparitions; i++)
            {
                Pose a = m_Plan.apparitions[i];
                DonjonRepere r = Apparitions[i];
                r.transform.localPosition = new Vector3(a.x, a.y, a.z);
                r.transform.localRotation = Quaternion.Euler(0f, a.rotY, 0f);
                r.niveau = a.niveau; r.apparition = (TypeApparition)a.type; r.visuel = null;
                if (m_TypeNomme[i] != a.type) { r.gameObject.name = s_NomsApparitions[i * 4 + a.type]; m_TypeNomme[i] = a.type; }
            }
            Pose ar = m_Plan.arrivee, po = m_Plan.portailRetour;
            Arrivee.transform.localPosition = new Vector3(ar.x, ar.y, ar.z);
            Arrivee.transform.localRotation = Quaternion.Euler(0f, ar.rotY, 0f);
            PortailRetour.transform.localPosition = new Vector3(po.x, po.y, po.z);
            PortailRetour.transform.localRotation = Quaternion.Euler(0f, po.rotY, 0f);
        }

        void PoserDecor()
        {
            for (int i = 0; i < m_Plan.nbDecors; i++)
            {
                Pose d = m_Plan.decors[i];
                int g = Groupe(d.Bloc, d.niveau);
                Vector3 p = new Vector3(d.x, d.y, d.z);
                switch (d.type)
                {
                    case DonjonPlan.DecorCoin:
                    {
                        // Collé dans le coin : le plan donne le côté, la taille du modèle donne le recul.
                        GameObject m = Choisir(kit.decorsCoin, d.variante);
                        Prendre(m, g, p, d.rotY);
                        Bounds bo = Bornes(m);
                        float e = Mathf.Max(bo.extents.x, bo.extents.z) + 0.05f;
                        float cx = (d.cellule % DonjonPlan.Largeur + 0.5f) * C, cz = (d.cellule / DonjonPlan.Largeur + 0.5f) * C;
                        p = new Vector3(cx + Mathf.Sign(d.x - cx) * (C * 0.5f - 0.5f - e) - bo.center.x * 0f, d.y, cz + Mathf.Sign(d.z - cz) * (C * 0.5f - 0.5f - e));
                        m_DernierVisuel.localPosition = p - Quaternion.Euler(0f, d.rotY, 0f) * new Vector3(bo.center.x, 0f, bo.center.z);
                        BoiteModele(m, g, m_DernierVisuel.localPosition, d.rotY);
                        break;
                    }
                    case DonjonPlan.DecorBanniere:
                    {
                        // La bannière est modélisée 0,38 m devant son pivot : pivot reculé dans le mur.
                        Vector3 versMur = Quaternion.Euler(0f, d.rotY, 0f) * Vector3.back;
                        Prendre(Choisir(kit.bannieres, d.variante), g, p + versMur * 0.38f, d.rotY);
                        break;
                    }
                    case DonjonPlan.DecorOs:
                        Prendre(Choisir(kit.os, d.variante), g, p + Vector3.up * 0.04f, d.rotY);
                        break;
                    case DonjonPlan.DecorTable:
                        Prendre(kit.tableLongue, g, p, d.rotY);
                        BoiteModele(kit.tableLongue, g, p, d.rotY);
                        break;
                    case DonjonPlan.DecorFlottant:
                    {
                        // Tonneau couché, à moitié dans l'eau (visuel seul : il ne gêne pas la marche).
                        GameObject t = Prendre(kit.tonneauFlottant, g, new Vector3(p.x, -DonjonPlan.ProfondeurBassin + DonjonPlan.ProfondeurEau - 0.55f, p.z), d.rotY);
                        if (t != null) t.transform.localRotation = Quaternion.Euler(0f, d.rotY, 90f);
                        break;
                    }
                    case DonjonPlan.DecorDalleArrivee:
                        Prendre(kit.dalleArrivee, g, p + Vector3.up * 0.015f, 0f);
                        break;
                }
            }
        }

        void PoserTorches()
        {
            for (int i = 0; i < m_Plan.nbTorches; i++)
            {
                Pose t = m_Plan.torches[i];
                int g = Groupe(t.Bloc, t.niveau);
                Vector3 p = new Vector3(t.x, t.y, t.z);
                Quaternion r = Quaternion.Euler(0f, t.rotY, 0f);
                if (t.type == DonjonPlan.TorcheMurale)
                {
                    Prendre(kit.torcheMurale, g, p + Vector3.up * 2.3f, t.rotY);
                    Lampe(p + Vector3.up * 3.0f + r * Vector3.forward * 0.6f, kit.couleurTorche, kit.intensiteTorche, kit.porteeTorche);
                }
                else
                {
                    Prendre(kit.colonneTorchere, g, p, t.rotY);
                    Prendre(kit.torcheSurPied, g, p + Vector3.up * 1.75f, t.rotY);
                    Boite(g, p, Quaternion.identity, new Vector3(0f, 0.7f, 0f), new Vector3(0.7f, 1.4f, 0.7f));
                    Lampe(p + Vector3.up * 2.6f, kit.couleurTorche, kit.intensiteTorche, kit.porteeTorche);
                }
            }
        }

        // ------------------------------------------------------------------ Portail de retour (repère visuel)
        void PoserPortail()
        {
            Pose po = m_Plan.portailRetour;
            int g = Groupe(po.Bloc, 0);
            Vector3 p = new Vector3(po.x, po.y, po.z);
            GameObject socle = Prendre(kit.socle, g, p, po.rotY, new Vector3(1.45f, 0.1f, 0.55f));
            if (m_Anneau == null)
            {
                m_Anneau = new GameObject("AnneauPortail");
                m_Anneau.AddComponent<MeshFilter>().sharedMesh = MeshAnneau();
                m_Anneau.AddComponent<MeshRenderer>();
            }
            m_Anneau.GetComponent<MeshRenderer>().sharedMaterial = kit.anneau;
            Transform ta = m_Anneau.transform;
            if (ta.parent != m_GroupesVisuels[g]) ta.SetParent(m_GroupesVisuels[g], false);
            ta.localPosition = p + Vector3.up * 1.85f;
            ta.localRotation = Quaternion.Euler(0f, po.rotY, 0f);
            ta.localScale = Vector3.one;
            if (!m_Anneau.activeSelf) m_Anneau.SetActive(true);
            m_Masquage.Ajouter(g, ta);
            Lampe(p + Vector3.up * 1.85f + Quaternion.Euler(0f, po.rotY, 0f) * Vector3.forward * 1.2f, kit.couleurPortail, kit.intensitePortail, 7f);
            PortailRetour.visuel = m_Anneau;
            Arrivee.visuel = socle;
        }

        [NonSerialized] GameObject m_Eau;
        static Mesh s_MeshEau;

        /// Surface d'eau du bassin (facettes, shader EauLowPoly), volume ZoneEau (vitesse x 0,6) et volume NavMesh
        /// « Eau » (coût 1 / 0,6). Même bloc pour tous les objets : ils suivent le masquage du rez (jamais masqué).
        void PoserBassin()
        {
            int b = m_Plan.blocBassin;
            int g = Groupe(b, 0);
            float taille = DonjonPlan.BlocTaille * C;
            Vector3 centre = new Vector3((b % DonjonPlan.BlocsX + 0.5f) * taille, -DonjonPlan.ProfondeurBassin, (b / DonjonPlan.BlocsX + 0.5f) * taille);
            if (m_Eau == null)
            {
                m_Eau = new GameObject("Eau");
                m_Eau.AddComponent<MeshFilter>().sharedMesh = MeshEau(taille);
                m_Eau.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var zone = new GameObject("ZoneEau");
                zone.layer = 2;
                zone.transform.SetParent(m_Eau.transform, false);
                var bc = zone.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.center = new Vector3(0f, -DonjonPlan.ProfondeurEau * 0.5f + 0.1f, 0f);
                bc.size = new Vector3(taille - 0.2f, DonjonPlan.ProfondeurEau + 1.2f, taille - 0.2f);
                zone.AddComponent<ZoneEau>();
                // Volume NavMesh sur la couche par défaut (la surface filtre aussi les volumes par couche).
                var volGo = new GameObject("VolumeNavMeshEau");
                volGo.transform.SetParent(m_Eau.transform, false);
                var vol = volGo.AddComponent<NavMeshModifierVolume>();
                vol.center = new Vector3(0f, -DonjonPlan.ProfondeurEau + 0.3f, 0f);
                vol.size = new Vector3(taille, 1.6f, taille);
                m_AireEau = NavMesh.GetAreaFromName("Eau");
                vol.area = m_AireEau >= 0 ? m_AireEau : 3;
            }
            m_Eau.GetComponent<MeshRenderer>().sharedMaterial = kit.eau;
            Transform te = m_Eau.transform;
            if (te.parent != m_GroupesVisuels[g]) te.SetParent(m_GroupesVisuels[g], false);
            te.localPosition = centre + Vector3.up * DonjonPlan.ProfondeurEau;
            te.localRotation = Quaternion.identity;
            if (!m_Eau.activeSelf) m_Eau.SetActive(true);
            if (m_AireEau < 0) m_AireEau = NavMesh.GetAreaFromName("Eau");
        }

        /// Grille de 1 m, triangles indépendants (le shader donne des facettes plates), faces vers le haut.
        static Mesh MeshEau(float taille)
        {
            if (s_MeshEau != null) return s_MeshEau;
            int n = Mathf.RoundToInt(taille);
            var v = new Vector3[(n + 1) * (n + 1)];
            var tri = new int[n * n * 6];
            float h = taille * 0.5f;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++) v[i + j * (n + 1)] = new Vector3(i - h, 0f, j - h);
            int t = 0;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = i + j * (n + 1), b = a + 1, c = a + n + 1, d = c + 1;
                    bool alt = ((i + j) & 1) == 0;
                    if (alt) { tri[t++] = a; tri[t++] = c; tri[t++] = d; tri[t++] = a; tri[t++] = d; tri[t++] = b; }
                    else { tri[t++] = a; tri[t++] = c; tri[t++] = b; tri[t++] = b; tri[t++] = c; tri[t++] = d; }
                }
            s_MeshEau = new Mesh { name = "EauBassin", vertices = v, triangles = tri };
            s_MeshEau.RecalculateNormals();
            s_MeshEau.RecalculateBounds();
            s_MeshEau.bounds = new Bounds(Vector3.zero, new Vector3(taille, 1f, taille));
            return s_MeshEau;
        }

        /// Anneau low poly (16 x 6 faces plates), 3 m de diamètre, dans le plan XY (on le traverse selon Z).
        static Mesh MeshAnneau()
        {
            if (s_MeshAnneau != null) return s_MeshAnneau;
            const int nu = 16, nv = 6;
            const float R = 1.5f, r = 0.16f;
            var v = new Vector3[nu * nv * 4];
            var n = new Vector3[v.Length];
            var tri = new int[nu * nv * 6];
            int iv = 0, it = 0;
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    Vector3 a = Tore(i, j, nu, nv, R, r), b = Tore(i + 1, j, nu, nv, R, r), c = Tore(i + 1, j + 1, nu, nv, R, r), d = Tore(i, j + 1, nu, nv, R, r);
                    Vector3 nf = Vector3.Cross(b - a, d - a).normalized;
                    Vector3 centre = (a + b + c + d) * 0.25f, axe = new Vector3(centre.x, centre.y, 0f).normalized * R;
                    if (Vector3.Dot(nf, centre - axe) < 0f) nf = -nf;
                    v[iv] = a; v[iv + 1] = b; v[iv + 2] = c; v[iv + 3] = d;
                    n[iv] = n[iv + 1] = n[iv + 2] = n[iv + 3] = nf;
                    bool sens = Vector3.Dot(Vector3.Cross(b - a, c - a), nf) > 0f;
                    // Face avant de Unity : normale = (b - a) x (c - a).
                    if (sens) { tri[it++] = iv; tri[it++] = iv + 1; tri[it++] = iv + 2; tri[it++] = iv; tri[it++] = iv + 2; tri[it++] = iv + 3; }
                    else { tri[it++] = iv; tri[it++] = iv + 2; tri[it++] = iv + 1; tri[it++] = iv; tri[it++] = iv + 3; tri[it++] = iv + 2; }
                    iv += 4;
                }
            s_MeshAnneau = new Mesh { name = "AnneauPortailRetour", vertices = v, normals = n, triangles = tri };
            s_MeshAnneau.RecalculateBounds();
            return s_MeshAnneau;
        }

        static Vector3 Tore(int i, int j, int nu, int nv, float R, float r)
        {
            float u = i * Mathf.PI * 2f / nu, w = j * Mathf.PI * 2f / nv;
            float rr = R + r * Mathf.Cos(w);
            return new Vector3(rr * Mathf.Cos(u), rr * Mathf.Sin(u), r * Mathf.Sin(w));
        }
    }
}
