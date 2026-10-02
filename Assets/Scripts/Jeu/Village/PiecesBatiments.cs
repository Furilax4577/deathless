using System.Collections;
using Deathless.Donjon.Terrasses;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Pièces des bâtiments de la carte v5 (03/10/2026, demande de Quentin : « rends les bâtiments entrables : bruit de porte, fondu
    /// au noir, une pièce basique avec une porte seulement pour ressortir »). Construites À L'EXÉCUTION, à la demande, la première fois
    /// qu'un héros entre (aucune lourdeur de scène, rien à regénérer : un seul script, partagé par les six bâtiments) ; un
    /// objet « PiecesBatiments » est créé dans la scène active et disparaît avec elle.
    ///
    /// - **Pièce** : 6 x 5 m, 4,8 m sous le plafond (la caméra épaule y tient : enceinte jusqu'à 4,45 m), planchers, murs et plafond en
    ///   blocs de bois chunky (MaillageTampon et matériau du donjon en terrasses, DonjonTerrasses_Pierre, couleurs par sommet),
    ///   poutres et poteaux sombres, une lampe suspendue (flamme et lumière chaude douce), UNE porte de bois dans le mur ouest ; ni
    ///   meuble, ni ennemi. Une pièce par bâtiment (Roles), à (1000 + 20 x n, 0, 400), loin de la carte comme le donjon en terrasses
    ///   (1000, 0, 120) ; chacune a sa teinte (murs et lumière : le sorcier bleuté, le druide vert, la forge rougeâtre…).
    /// - **Entrer** (EntreeBatiment, touche Interagir devant la porte du bâtiment) puis **sortir** (SortiePiece, devant la porte de la
    ///   pièce) : bruit d'ouverture, fondu au noir (FonduNoir, 0,35 s), saut de position (le héros est sans contrôle, EnTransit),
    ///   bruit de fermeture, 0,25 s de noir, fondu retour (0,4 s). Le héros réapparaît dans la pièce face à l'intérieur, ou devant la porte du
    ///   bâtiment, au pied des marches, dos au bâtiment, face à l'allée.
    /// - **Caméra** : tant que le héros local est dans une pièce, CameraEpaule.Enceinte est l'intérieur de la pièce (la caméra n'en
    ///   sort pas), comme au donjon en terrasses ; la découpe autour du héros reste (CameraEpaule.DecoupeSansTraverser). Les murs ont des
    ///   colliders (BoxCollider) : le héros les touche, la caméra recule contre eux.
    /// - **Éclairage propre** : dans la pièce, la lumière ambiante, le brouillard et le soleil sont ceux de la pièce (après le cycle
    ///   jour / nuit : F9 n'y change rien) ; hors de la pièce, le cycle reprend à l'image suivante (il réécrit tout chaque image).
    /// - **Réseau** : le héros qui entre est téléporté seul (les autres restent dehors) ; sa position est répliquée comme les
    ///   autres téléportations (NetworkTransform.Teleport, comme DonjonJeu.Transit). La carte v5 n'est jouée qu'en solo pour l'instant
    ///   (Partie.Exploration), donc le cas réseau n'est pas encore éprouvé ; en solo et en aperçu tout marche.
    /// - Pas de piège : une vague ne change rien ; la porte de sortie marche toujours. Les squelettes ne voient pas un héros à 600 m.
    [DefaultExecutionOrder(600)]
    public class PiecesBatiments : MonoBehaviour
    {
        /// Un bâtiment par rôle (même ordre que VillageBuilder.V5MaisonsRoles).
        public static readonly string[] Roles = { "Taverne", "Mecano", "Maison", "Forge", "Sorcier", "Druide" };
        /// Coin de la première pièce (centre du sol) et écart entre deux pièces.
        public static readonly Vector3 Origine = new Vector3(1000f, 0f, 400f);
        public const float Ecart = 20f;
        public const float Largeur = 6f, Profondeur = 5f, Hauteur = 4.8f;
        /// Durées (s, temps réel) : fondu vers le noir, noir tenu après le saut, fondu retour.
        public const float DureeFonduNoir = 0.35f, DureeNoir = 0.25f, DureeFonduRetour = 0.4f;

        public sealed class Piece
        {
            public int index;
            public string role;
            public Transform racine;
            /// Intérieur (monde), au-dessus du sol : enceinte de la caméra.
            public Bounds enceinte;
            /// Emprise de la pièce (murs compris), pour savoir si un héros y est.
            public Bounds emprise;
            public Vector3 arrivee;
            public float lacetArrivee;
            /// Devant la porte de la pièce (monde) : centre de la zone de l'invite « Sortir ».
            public Vector3 devantPorte;
            public Vector3 porte;
            public Color ambiance;
            public EntreeBatiment entree;
        }

        public static PiecesBatiments Instance { get; private set; }

        readonly Piece[] m_Pieces = new Piece[Roles.Length];
        bool m_Occupe;
        Light m_Soleil;
        Piece m_Dedans;

        // ----------------------------------------------------------------- Accès

        /// Gestionnaire de la scène (créé à la première demande).
        public static PiecesBatiments Assurer()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PiecesBatiments");
            return go.AddComponent<PiecesBatiments>();
        }

        void Awake()
        {
            Instance = this;
            var dc = FindAnyObjectByType<DayCycle>();
            if (dc != null) m_Soleil = dc.GetComponent<Light>();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                if (m_Dedans != null) { CameraEpaule.Enceinte = null; }
            }
        }

        /// Le héros peut passer une porte : héros local, libre, vivant, partie en cours, aucun passage en cours.
        public static bool PeutPasser(Heros h)
        {
            if (h == null || h.Distant || !h.PeutAgir) return false;
            var p = Partie.Instance;
            return p != null && p.HerosLocal == h && (Instance == null || !Instance.m_Occupe);
        }

        /// Pièce où se trouve ce point (monde), ou null.
        public Piece PieceDe(Vector3 p)
        {
            for (int i = 0; i < m_Pieces.Length; i++) if (m_Pieces[i] != null && m_Pieces[i].emprise.Contains(p)) return m_Pieces[i];
            return null;
        }

        /// Pièce du héros local (null : il est dehors).
        public static Piece PieceLocale
        {
            get
            {
                var p = Partie.Instance;
                var h = p != null ? p.HerosLocal : null;
                return Instance != null && h != null ? Instance.PieceDe(h.transform.position) : null;
            }
        }

        public Piece Pieces(int index) => index >= 0 && index < m_Pieces.Length ? m_Pieces[index] : null;

        // ----------------------------------------------------------------- Passages

        /// Touche Interagir devant la porte du bâtiment : le héros entre dans la pièce du bâtiment.
        public void Entrer(Heros h, EntreeBatiment e)
        {
            if (m_Occupe || h == null || e == null) return;
            int i = System.Array.IndexOf(Roles, e.role);
            Piece p = Obtenir(i >= 0 ? i : 2);
            if (p == null) return;
            p.entree = e;
            StartCoroutine(Passage(h, p, true));
        }

        /// Touche Interagir devant la porte de la pièce : le héros ressort devant son bâtiment.
        public void Sortir(Heros h, Piece p)
        {
            if (m_Occupe || h == null || p == null) return;
            StartCoroutine(Passage(h, p, false));
        }

        IEnumerator Passage(Heros h, Piece p, bool entrant)
        {
            m_Occupe = true;
            h.EnTransit = true;
            bool fini = false;
            try
            {
                // la porte s'ouvre (là où le héros se tient : au bâtiment en entrant, dans la pièce en sortant)
                Vector3 portePoint = entrant ? (p.entree != null ? p.entree.PointPorte : h.transform.position) : p.porte;
                AudioBank.Jouer(SonsPortes.PorteOuvre, portePoint, 0.9f);
                yield return FonduNoir.Vers(1f, DureeFonduNoir);
                if (h == null) yield break;
                if (h.Vivant)
                {
                    Vector3 dest; float lacet;
                    if (entrant) { dest = p.arrivee; lacet = p.lacetArrivee; }
                    else if (p.entree != null) { dest = p.entree.PointRetour; lacet = p.entree.LacetRetour; }
                    else { dest = Partie.Instance.PointReapparition(h.transform.position); lacet = 0f; }
                    Placer(h, dest, lacet);
                    // la porte se referme derrière lui
                    AudioBank.Jouer(SonsPortes.PorteFerme, dest + Vector3.up, 0.9f);
                }
                yield return new WaitForSecondsRealtime(DureeNoir);
                yield return FonduNoir.Vers(0f, DureeFonduRetour);
                fini = true;
            }
            finally
            {
                if (h != null) h.EnTransit = false;
                m_Occupe = false;
                if (!fini) FonduNoir.Couper();   // passage interrompu (héros ou scène détruits) : jamais d'écran noir resté
            }
        }

        /// Saut de position du héros local (comme DonjonJeu.Transit) : au point, regard de lacet `lacet`, caméra derrière lui ;
        /// en réseau, saut sans interpolation chez les autres postes.
        static void Placer(Heros h, Vector3 dest, float lacet)
        {
            h.Teleporter(dest + Vector3.up * 0.05f);
            h.transform.rotation = Quaternion.Euler(0f, lacet, 0f);
            var p = Partie.Instance;
            if (p != null && p.cameraJeu != null && p.cameraJeu.cible == h.transform) p.cameraJeu.lacet = lacet;
            var nt = h.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt != null && nt.IsSpawned && nt.IsOwner) nt.Teleport(h.transform.position, h.transform.rotation, h.transform.localScale);
        }

        // ----------------------------------------------------------------- Caméra et éclairage (après le cycle jour / nuit et le donjon)

        void LateUpdate()
        {
            var p = Partie.Instance;
            var h = p != null ? p.HerosLocal : null;
            Piece dedans = h != null ? PieceDe(h.transform.position) : null;
            if (dedans == null)
            {
                if (m_Dedans != null) CameraEpaule.Enceinte = null;   // le héros vient de sortir (le donjon la remet à null chaque image, mais pas sans lui)
                m_Dedans = null;
                return;
            }
            m_Dedans = dedans;
            CameraEpaule.Enceinte = dedans.enceinte;
            CameraEpaule.DecoupeSansTraverser = true;
            Color a = dedans.ambiance;
            RenderSettings.ambientSkyColor = a;
            RenderSettings.ambientEquatorColor = a * 0.85f;
            RenderSettings.ambientGroundColor = a * 0.6f;
            RenderSettings.fogColor = a * 0.5f;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 220f;
            if (m_Soleil != null) m_Soleil.intensity = 0f;
        }

        // ----------------------------------------------------------------- Construction

        Piece Obtenir(int index)
        {
            if (m_Pieces[index] != null) return m_Pieces[index];
            var mat = Resources.Load<Material>("DonjonTerrasses/DonjonTerrasses_Pierre");
            if (mat == null) { Debug.LogWarning("Pièces des bâtiments : matériau DonjonTerrasses_Pierre introuvable dans les Resources"); return null; }
            var flamme = Resources.Load<Material>("DonjonTerrasses/DonjonTerrasses_Flamme");
            m_Pieces[index] = Construire(index, mat, flamme != null ? flamme : mat);
            return m_Pieces[index];
        }

        /// Teinte de la pièce du rôle : multiplicateur des couleurs du bois, couleur de la lampe, ambiance.
        static void Teinte(string role, out Color bois, out Color lampe, out Color ambiance)
        {
            switch (role)
            {
                case "Taverne": bois = new Color(1.04f, 0.96f, 0.84f); lampe = new Color(1f, 0.70f, 0.36f); break;
                case "Mecano": bois = new Color(1.05f, 0.88f, 0.80f); lampe = new Color(1f, 0.66f, 0.42f); break;
                case "Forge": bois = new Color(0.86f, 0.80f, 0.78f); lampe = new Color(1f, 0.52f, 0.28f); break;
                case "Sorcier": bois = new Color(0.86f, 0.84f, 1.05f); lampe = new Color(0.82f, 0.68f, 1f); break;
                case "Druide": bois = new Color(0.88f, 1.02f, 0.84f); lampe = new Color(0.86f, 1f, 0.58f); break;
                default: bois = Color.white; lampe = new Color(1f, 0.73f, 0.44f); break;   // maison de base
            }
            ambiance = Color.Lerp(new Color(0.42f, 0.34f, 0.27f), lampe, 0.18f);
        }

        static Color32 Mul(Color32 c, Color k) => new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * k.r), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * k.g), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * k.b), 0, 255), 255);

        Piece Construire(int index, Material mat, Material flamme)
        {
            string role = Roles[index];
            Teinte(role, out Color kBois, out Color cLampe, out Color ambiance);
            Vector3 o = Origine + new Vector3(Ecart * index, 0f, 0f);

            var racine = new GameObject("Piece_" + role).transform;
            racine.SetParent(transform, false);
            racine.position = o;
            // Sol de bois : marque la matière des pas (Bois) pour tous les colliders de la pièce.
            racine.gameObject.AddComponent<MatiereSol>().matiere = Matiere.Bois;

            var m = new MaillageTampon();       // bois, poutres, porte
            var f = new MaillageTampon();       // flamme de la lampe
            Color32 boisPlancher = Mul(new Color32(168, 116, 70, 255), kBois);
            Color32 boisMur = Mul(new Color32(186, 134, 86, 255), kBois);
            Color32 boisPlafond = Mul(new Color32(150, 104, 64, 255), kBois);
            Color32 sombre = Mul(new Color32(98, 64, 40, 255), kBois);
            Color32 porteBois = Mul(new Color32(130, 88, 54, 255), kBois);
            Color32 fer = new Color32(96, 98, 108, 255);
            const float demiX = Largeur * 0.5f, demiZ = Profondeur * 0.5f, ep = 0.3f;
            const float hCourse = 0.6f;
            int courses = Mathf.RoundToInt(Hauteur / hCourse);

            // plancher : lames de 0,5 m dans le sens nord-sud, un joint par lame à une cote différente
            for (int i = 0; i < 12; i++)
            {
                float x = -demiX + 0.25f + 0.5f * i;
                uint h = MaillageTampon.Hache(i, 3, 11);
                float coupe = -1.2f + (h % 240u) / 100f;       // joint entre -1,2 et +1,2 m
                float l1 = coupe + demiZ + 0.3f, l2 = demiZ + 0.3f - coupe;
                m.Bloc(new Vector3(x, -0.09f, (-demiZ - 0.3f + coupe) * 0.5f), new Vector3(0.47f, 0.18f, l1 - 0.02f), Quaternion.identity, 0.025f, MaillageTampon.Teinte(boisPlancher, h, 12));
                m.Bloc(new Vector3(x, -0.09f, (coupe + demiZ + 0.3f) * 0.5f), new Vector3(0.47f, 0.18f, l2 - 0.02f), Quaternion.identity, 0.025f, MaillageTampon.Teinte(boisPlancher, h >> 5, 12));
            }

            // noyau sombre derrière les joints : sous le plancher, dans les murs (côté porte : de part et d'autre de l'ouverture et au-dessus),
            // sous le plafond et dans la porte ; sans lui, les joints entre blocs laissent voir le ciel (lignes claires sur les murs)
            Color32 noyau = Mul(new Color32(58, 38, 24, 255), kBois);
            m.Bloc(new Vector3(0f, -0.2f, 0f), new Vector3(Largeur + 0.6f, 0.12f, Profondeur + 0.6f), Quaternion.identity, 0.01f, noyau);
            m.Bloc(new Vector3(0f, Hauteur - 0.04f, 0f), new Vector3(Largeur + 0.6f, 0.12f, Profondeur + 0.6f), Quaternion.identity, 0.01f, noyau);
            for (int sz = -1; sz <= 1; sz += 2)
                m.Bloc(new Vector3(0f, Hauteur * 0.5f, sz * (demiZ + ep * 0.5f)), new Vector3(Largeur + 0.6f, Hauteur, 0.18f), Quaternion.identity, 0.01f, noyau);
            m.Bloc(new Vector3(demiX + ep * 0.5f, Hauteur * 0.5f, 0f), new Vector3(0.18f, Hauteur, Profondeur), Quaternion.identity, 0.01f, noyau);
            for (int sz = -1; sz <= 1; sz += 2)
                m.Bloc(new Vector3(-demiX - ep * 0.5f, 2.4f, sz * 1.625f), new Vector3(0.18f, 4.8f, 1.75f), Quaternion.identity, 0.01f, noyau);
            m.Bloc(new Vector3(-demiX - ep * 0.5f, 3.6f, 0f), new Vector3(0.18f, 2.4f, 1.5f), Quaternion.identity, 0.01f, noyau);
            m.Bloc(new Vector3(-demiX - ep * 0.5f - 0.05f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 1.5f), Quaternion.identity, 0.01f, noyau);

            // murs nord et sud (selon X, de -3,3 à 3,3) : rondins posés à joints décalés une course sur deux
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < courses; k++)
                {
                    float y = hCourse * (k + 0.5f), z = s * (demiZ + ep * 0.5f);
                    float[] bornes = k % 2 == 0 ? new[] { -demiX - ep, 0f, demiX + ep } : new[] { -demiX - ep, -1.65f, 1.65f, demiX + ep };
                    for (int b = 0; b + 1 < bornes.Length; b++)
                        m.Bloc(new Vector3((bornes[b] + bornes[b + 1]) * 0.5f, y, z), new Vector3(bornes[b + 1] - bornes[b] - 0.02f, hCourse - 0.03f, ep), Quaternion.identity, 0.03f,
                            MaillageTampon.Teinte(boisMur, MaillageTampon.Hache(k, b, s + 5), 12));
                }
            // mur est (plein) et mur ouest (la porte : ouverture de 1,5 m sur 2,4 m, quatre courses)
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < courses; k++)
                {
                    float y = hCourse * (k + 0.5f), x = s * (demiX + ep * 0.5f);
                    float[][] segs;
                    if (s < 0 && k < 4) segs = new[] { new[] { -demiZ, -0.75f }, new[] { 0.75f, demiZ } };
                    else if (k % 2 == 0) segs = new[] { new[] { -demiZ, 0f }, new[] { 0f, demiZ } };
                    else segs = new[] { new[] { -demiZ, -1.2f }, new[] { -1.2f, 1.2f }, new[] { 1.2f, demiZ } };
                    for (int b = 0; b < segs.Length; b++)
                        m.Bloc(new Vector3(x, y, (segs[b][0] + segs[b][1]) * 0.5f), new Vector3(ep, hCourse - 0.03f, segs[b][1] - segs[b][0] - 0.02f), Quaternion.identity, 0.03f,
                            MaillageTampon.Teinte(boisMur, MaillageTampon.Hache(k, b + 3, s + 9), 12));
                }

            // poteaux d'angle, lisses (haut, bas, mi-hauteur), sombres
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    m.Bloc(new Vector3(sx * (demiX - 0.14f), Hauteur * 0.5f, sz * (demiZ - 0.14f)), new Vector3(0.34f, Hauteur, 0.34f), Quaternion.identity, 0.04f, MaillageTampon.Teinte(sombre, MaillageTampon.Hache(sx, sz, 2), 6));
            for (int sz = -1; sz <= 1; sz += 2)
            {
                m.Bloc(new Vector3(0f, 4.64f, sz * (demiZ - 0.1f)), new Vector3(Largeur, 0.28f, 0.24f), Quaternion.identity, 0.03f, sombre);                    // sablière haute
                m.Bloc(new Vector3(0f, 0.11f, sz * (demiZ - 0.06f)), new Vector3(Largeur - 0.4f, 0.22f, 0.14f), Quaternion.identity, 0.03f, sombre);            // plinthe
                m.Bloc(new Vector3(0f, 1.3f, sz * (demiZ - 0.05f)), new Vector3(Largeur - 0.4f, 0.12f, 0.12f), Quaternion.identity, 0.025f, sombre);            // lisse d'appui
            }
            for (int sx = -1; sx <= 1; sx += 2)
            {
                m.Bloc(new Vector3(sx * (demiX - 0.1f), 4.64f, 0f), new Vector3(0.24f, 0.28f, Profondeur), Quaternion.identity, 0.03f, sombre);
                if (sx > 0)
                {
                    m.Bloc(new Vector3(sx * (demiX - 0.06f), 0.11f, 0f), new Vector3(0.14f, 0.22f, Profondeur - 0.4f), Quaternion.identity, 0.03f, sombre);
                    m.Bloc(new Vector3(sx * (demiX - 0.05f), 1.3f, 0f), new Vector3(0.12f, 0.12f, Profondeur - 0.4f), Quaternion.identity, 0.025f, sombre);
                }
                else
                {   // mur de la porte : plinthe et lisse de part et d'autre de l'ouverture
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        m.Bloc(new Vector3(sx * (demiX - 0.06f), 0.11f, sz * 1.75f), new Vector3(0.14f, 0.22f, 1.5f), Quaternion.identity, 0.03f, sombre);
                        m.Bloc(new Vector3(sx * (demiX - 0.05f), 1.3f, sz * 1.75f), new Vector3(0.12f, 0.12f, 1.5f), Quaternion.identity, 0.025f, sombre);
                    }
                }
            }

            // plafond : planches dans le sens nord-sud, trois poutres sombres dessous (la plus basse à 4,5 m : hauteur libre pour la caméra)
            for (int i = 0; i < 13; i++)
            {
                float x = -demiX - 0.3f + 0.5f * i + 0.25f;
                m.Bloc(new Vector3(x, Hauteur + 0.1f, 0f), new Vector3(0.47f, 0.2f, Profondeur + 0.6f), Quaternion.identity, 0.025f, MaillageTampon.Teinte(boisPlafond, MaillageTampon.Hache(i, 8, 3), 10));
            }
            for (int i = -1; i <= 1; i++)
                m.Bloc(new Vector3(i * 2f, 4.64f, 0f), new Vector3(0.28f, 0.28f, Profondeur - 0.4f), Quaternion.identity, 0.04f, sombre);

            // porte de bois (mur ouest) : encadrement, vantail à planches verticales, deux traverses, poignée, seuil
            float xp = -demiX - ep * 0.5f;
            for (int sz = -1; sz <= 1; sz += 2)
                m.Bloc(new Vector3(xp, 1.25f, sz * 0.83f), new Vector3(0.4f, 2.5f, 0.2f), Quaternion.identity, 0.03f, sombre);
            m.Bloc(new Vector3(xp, 2.6f, 0f), new Vector3(0.4f, 0.24f, 1.86f), Quaternion.identity, 0.03f, sombre);
            for (int i = 0; i < 5; i++)
                m.Bloc(new Vector3(xp + 0.02f, 1.15f, -0.6f + 0.3f * i), new Vector3(0.1f, 2.3f, 0.285f), Quaternion.identity, 0.02f, MaillageTampon.Teinte(porteBois, MaillageTampon.Hache(i, 1, 4), 10));
            foreach (float yt in new[] { 0.55f, 1.8f })
                m.Bloc(new Vector3(xp + 0.1f, yt, 0f), new Vector3(0.06f, 0.16f, 1.38f), Quaternion.identity, 0.02f, sombre);
            m.Bloc(new Vector3(xp + 0.14f, 1.1f, 0.5f), new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, 0.03f, fer);
            m.Bloc(new Vector3(xp + 0.12f, 0.04f, 0f), new Vector3(0.42f, 0.08f, 1.6f), Quaternion.identity, 0.02f, sombre);

            // lampe suspendue à la poutre centrale : chaîne, cage sombre, flamme
            m.Bloc(new Vector3(0f, 4.22f, 0f), new Vector3(0.05f, 0.56f, 0.05f), Quaternion.identity, 0.01f, fer);
            m.Bloc(new Vector3(0f, 3.78f, 0f), new Vector3(0.34f, 0.07f, 0.34f), Quaternion.identity, 0.02f, sombre);
            m.Bloc(new Vector3(0f, 3.5f, 0f), new Vector3(0.34f, 0.07f, 0.34f), Quaternion.identity, 0.02f, sombre);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    m.Bloc(new Vector3(sx * 0.15f, 3.64f, sz * 0.15f), new Vector3(0.05f, 0.34f, 0.05f), Quaternion.identity, 0.01f, sombre);
            f.Bloc(new Vector3(0f, 3.64f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Quaternion.identity, 0.05f, Mul(new Color32(255, 196, 96, 255), cLampe));

            var visuel = new GameObject("Visuel").transform;
            visuel.SetParent(racine, false);
            Visuel(visuel, "Bois", m, mat);
            Visuel(visuel, "Lampe", f, flamme);

            // colliders : sol, quatre murs, plafond (pas de NavMesh : aucun ennemi ne vient)
            var col = new GameObject("Collisions").transform;
            col.SetParent(racine, false);
            Boite(col, "Sol", new Vector3(0f, -0.25f, 0f), new Vector3(Largeur + 1.4f, 0.5f, Profondeur + 1.4f));
            Boite(col, "MurNord", new Vector3(0f, Hauteur * 0.5f, demiZ + ep * 0.5f), new Vector3(Largeur + 1.2f, Hauteur, ep));
            Boite(col, "MurSud", new Vector3(0f, Hauteur * 0.5f, -demiZ - ep * 0.5f), new Vector3(Largeur + 1.2f, Hauteur, ep));
            Boite(col, "MurEst", new Vector3(demiX + ep * 0.5f, Hauteur * 0.5f, 0f), new Vector3(ep, Hauteur, Profondeur));
            Boite(col, "MurOuest", new Vector3(-demiX - ep * 0.5f, Hauteur * 0.5f, 0f), new Vector3(ep, Hauteur, Profondeur));
            Boite(col, "Plafond", new Vector3(0f, Hauteur + 0.2f, 0f), new Vector3(Largeur + 1.2f, 0.4f, Profondeur + 1.2f));

            // lumière chaude douce, sans ombre
            var lg = new GameObject("Lumiere");
            lg.transform.SetParent(racine, false);
            lg.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = cLampe; l.intensity = 2.6f; l.range = 12f; l.shadows = LightShadows.None;

            // arrivée : près de la porte (mur ouest), face à l'intérieur (+X) ; zone de l'invite « Sortir » devant la porte
            var piece = new Piece
            {
                index = index, role = role, racine = racine, ambiance = ambiance,
                arrivee = o + new Vector3(-demiX + 1.15f, 0f, 0f), lacetArrivee = 90f,
                devantPorte = o + new Vector3(-demiX + 1.0f, 0f, 0f),
                porte = o + new Vector3(-demiX, 1.2f, 0f),
                // intérieur : 0,2 m de marge sur les murs (poteaux d'angle), de 0,1 m à 4,45 m (sous les poutres, à 4,5 m)
                enceinte = new Bounds(o + new Vector3(0f, (0.1f + 4.45f) * 0.5f, 0f), new Vector3(Largeur - 0.4f, 4.35f, Profondeur - 0.4f)),
                emprise = new Bounds(o + new Vector3(0f, (-0.5f + Hauteur + 0.6f) * 0.5f, 0f), new Vector3(Largeur + 0.6f, Hauteur + 1.1f, Profondeur + 0.6f)),
            };
            var sortie = new GameObject("Sortie");
            sortie.transform.SetParent(racine, false);
            sortie.transform.position = piece.devantPorte;
            var sp = sortie.AddComponent<SortiePiece>();
            sp.piece = piece;
            return piece;
        }

        static void Visuel(Transform parent, string nom, MaillageTampon t, Material mat)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = t.VersMesh(nom);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Boite(Transform parent, string nom, Vector3 centre, Vector3 taille)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.AddComponent<BoxCollider>().size = taille;
        }
    }

    /// Porte de la pièce (touche Interagir devant elle : « Sortir »), posée par PiecesBatiments sur chaque pièce construite.
    public class SortiePiece : PointInteraction
    {
        public PiecesBatiments.Piece piece;
        /// Rayon de la zone devant la porte (m) : le héros arrive à 1,15 m de la porte, centre à 1 m.
        public const float Rayon = 2.2f;

        public override string Invite(Heros h, out float distance)
        {
            distance = float.MaxValue;
            if (piece == null || !PiecesBatiments.PeutPasser(h)) return null;
            Vector3 p = h.transform.position;
            if (!piece.emprise.Contains(p)) return null;
            Vector3 d = p - piece.devantPorte; d.y = 0f;
            distance = d.magnitude;
            return distance <= Rayon ? "Sortir" : null;
        }

        public override void Interagir(Heros h)
        {
            if (piece == null || !PiecesBatiments.PeutPasser(h)) return;
            PiecesBatiments.Instance.Sortir(h, piece);
        }
    }
}
