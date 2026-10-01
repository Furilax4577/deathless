using UnityEngine;

namespace Deathless.Jeu
{
    /// Éclat de Nyx de Nyxar (wiki : ennemis.md, Nyxar ; décidé, codé le 30/09/2026) : un de ses deux points faibles, dans
    /// le crâne de sa couronne (index 0) ou dans celui de son grimoire à la ceinture (index 1). C'est une cible à part
    /// entière : sa propre Sante (équipe Ennemis) sur un objet enfant de Nyxar posé à ses pieds (distance et hauteur de
    /// Combat.Ennemis comme pour lui), et une petite sphère de collision qui suit l'os (tête ou bassin) avec le visuel :
    /// les frappes de mêlée (Combat.Ennemis le préfère au corps de Nyxar tant qu'il tient) et les projectiles le
    /// touchent. Un coup sur l'éclat (décidé le 01/10/2026) est un critique garanti sur Nyxar (×2, effet et chiffre de
    /// critique) et abîme l'éclat sans chiffre propre (Sante.renvoi → Necromancien.CoupSurEclat, puis Abimer). Brisé :
    /// Nyxar perd une partie de son kit (Necromancien.EclatBrise).
    ///
    /// Visuel : grappe de gemmes low poly vertes (thème Nyxessa : l'énergie de la relique), un seul maillage, shader
    /// Relic/VertexColorUnlit (EffetsJeu.Gemmes), pulsation lente ; elle rétrécit un peu à mesure qu'il s'use.
    ///
    /// Réseau : l'hôte fait foi sur ses PV (EnnemiReseau, variable m_Eclats) ; chez un client, les coups des héros de ce
    /// poste partent vers l'hôte (EnnemiReseau.RelayerEclat).
    public class EclatNyx : MonoBehaviour
    {
        public int Index { get; private set; }
        public Sante Sante { get; private set; }
        public Necromancien Proprietaire { get; private set; }
        public bool Brise => Sante == null || Sante.Mort;
        /// Position du point faible (visuel et collision).
        public Vector3 Position => m_Point != null ? m_Point.position : transform.position;

        Transform m_Os, m_Point;
        Vector3 m_Decalage;
        SphereCollider m_Collision;
        MeshRenderer m_Rendu;
        Mesh m_Mesh;
        Vector3[] m_Sommets;
        Color[] m_Couleurs;
        float m_Phase;
        const int Gemmes = 7;

        /// Crée l'éclat sous `nyxar`, attaché à l'os `os` au point `pointMonde` (pose de repos).
        public static EclatNyx Creer(Necromancien nyxar, int index, Transform os, Vector3 pointMonde, float rayon)
        {
            var go = new GameObject(index == 0 ? "EclatNyx_Couronne" : "EclatNyx_Grimoire");
            go.transform.SetParent(nyxar.transform, false);
            go.transform.localPosition = Vector3.zero;
            var e = go.AddComponent<EclatNyx>();
            e.Index = index;
            e.Proprietaire = nyxar;
            e.m_Os = os != null ? os : nyxar.transform;
            e.m_Decalage = e.m_Os.InverseTransformPoint(pointMonde);
            e.m_Phase = index * 1.7f;
            e.Sante = go.AddComponent<Sante>();
            e.Sante.equipe = Equipe.Ennemis;
            e.Sante.Initialiser(1f);
            // Tous les postes : le coup part vers Nyxar (critique) au lieu d'être appliqué à l'éclat.
            e.Sante.renvoi = info => nyxar.CoupSurEclat(e, info);

            var point = new GameObject("Point");
            point.transform.SetParent(go.transform, false);
            e.m_Point = point.transform;
            e.m_Collision = point.AddComponent<SphereCollider>();
            e.m_Collision.radius = rayon;
            e.ConstruireVisuel(point, rayon);
            e.LateUpdate();
            return e;
        }

        void ConstruireVisuel(GameObject point, float rayon)
        {
            var mat = EffetsJeu.Gemmes;
            if (mat == null) return;
            var mf = point.AddComponent<MeshFilter>();
            m_Rendu = point.AddComponent<MeshRenderer>();
            m_Rendu.sharedMaterial = mat;
            m_Rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Sommets = new Vector3[Gemmes * LowPolyGem.VerticesPerGem];
            m_Couleurs = new Color[m_Sommets.Length];
            m_Mesh = new Mesh { name = "EclatNyx" };
            m_Mesh.MarkDynamic();
            m_Mesh.vertices = m_Sommets; m_Mesh.colors = m_Couleurs; m_Mesh.triangles = LowPolyGem.Triangles(Gemmes);
            m_Mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (rayon * 4f));
            mf.sharedMesh = m_Mesh;
            m_Taille = rayon;
        }

        float m_Taille;

        static Color[] Palette => VfxPalette.Cache("EclatNyx.Nyxessa", () => new[]
        {
            VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.07f, 0.38f, 0.05f)),
            VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.7f, 0.08f)),
            VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.55f, 0.95f, 0.2f)),
            VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.76f, 1f, 0.44f)) * VfxPalette.Intensite(VfxTheme.Nyxessa, 2.5f),
        });

        void LateUpdate()
        {
            if (m_Point == null) return;
            if (m_Os != null) m_Point.position = m_Os.TransformPoint(m_Decalage);
            if (m_Mesh == null || m_Sommets == null) return;
            if (Brise) { if (m_Rendu != null) m_Rendu.enabled = false; return; }
            // Grappe : un gros cristal au centre (cœur lumineux), six petits autour ; pulsation lente, rétrécit en s'usant.
            float t = Time.time + m_Phase;
            float usure = 0.65f + 0.35f * (Sante != null ? Sante.Ratio : 1f);
            float pulse = 1f + 0.08f * Mathf.Sin(t * 3.1f);
            var pal = Palette;
            float s = m_Taille * usure * pulse;
            Quaternion tour = Quaternion.Euler(0f, t * 40f, 0f);
            LowPolyGem.Write(m_Sommets, m_Couleurs, 0, Vector3.zero, s * 0.95f, new Vector3(0.75f, 1.35f, 0.75f), tour, pal[3], LowPolyGem.DefaultLight);
            for (int i = 1; i < Gemmes; i++)
            {
                float a = (i - 1) * 60f + t * 25f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(s * 0.62f, (i % 2 == 0 ? 0.15f : -0.2f) * s, 0f);
                Quaternion r = Quaternion.Euler(25f * (i % 3 - 1), a, 30f);
                LowPolyGem.Write(m_Sommets, m_Couleurs, i, p, s * 0.42f, new Vector3(0.7f, 1.2f, 0.7f), r, pal[i % 3], LowPolyGem.DefaultLight);
            }
            m_Mesh.vertices = m_Sommets;
            m_Mesh.colors = m_Couleurs;
        }

        /// Hôte : PV de l'éclat (× multiplicateur de la nuit), plein.
        public void Initialiser(float pv) { if (Sante != null) Sante.Initialiser(Mathf.Max(1f, pv)); }

        /// Client : PV recopiés de l'hôte (EnnemiReseau) ; un éclat qui passe à 0 se brise ici aussi (effet déjà
        /// joué par EffetsBoss, seulement le visuel et la collision coupés).
        public void Fixer(float pv, float pvMax)
        {
            if (Sante == null) return;
            bool avant = Sante.Mort;
            Sante.Fixer(pv, Mathf.Max(1f, pvMax));
            if (!avant && Sante.Mort) Couper();
        }

        /// Hôte : l'éclat s'use de `montant` (sans chiffre ni événement de coup) ; à 0, il se brise (Nyxar.EclatBrise).
        public void Abimer(float montant, InfoDegats info)
        {
            if (Sante == null || Brise || montant <= 0f) return;
            Sante.Fixer(Sante.Pv - montant, Sante.pvMax);
            if (!Sante.Mort) return;
            Couper();
            if (Proprietaire != null) Proprietaire.EclatBrise(this, info);
            Deathless.Succes.ServiceSucces.EclatBrise(Index);   // succès (couronne, grimoire, ordre inverse)
        }

        void Couper()
        {
            if (m_Collision != null) m_Collision.enabled = false;
            if (m_Rendu != null) m_Rendu.enabled = false;
        }

        void OnDestroy() { if (m_Mesh != null) Destroy(m_Mesh); }
    }
}
