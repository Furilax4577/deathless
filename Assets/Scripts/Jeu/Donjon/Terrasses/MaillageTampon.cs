using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deathless.Donjon.Terrasses
{
    /// Tampon de maillage à couleurs par sommet (shader Deathless/VertexColorLit ou Relic/VertexColorUnlit) : blocs aux arêtes
    /// abattues et normales lissées (aspect « jouet », facettes denses et douces), tours (fûts de piliers), quadrilatères.
    public sealed class MaillageTampon
    {
        public readonly List<Vector3> v = new List<Vector3>(4096);
        public readonly List<Vector3> n = new List<Vector3>(4096);
        public readonly List<Color32> c = new List<Color32>(4096);
        public readonly List<int> t = new List<int>(8192);

        public int NbSommets => v.Count;
        public void Vider() { v.Clear(); n.Clear(); c.Clear(); t.Clear(); }

        // Faces d'un bloc, dans son repère : 0 +X, 1 -X, 2 +Y, 3 -Y, 4 +Z, 5 -Z.
        public const int SansDessous = 63 & ~(1 << 3);
        public const int SansArriere = 63 & ~(1 << 5);
        public const int Tout = 63;

        static readonly float[] s_U = new float[4], s_V = new float[4];

        /// Bloc de taille `taille` centré en `centre`, tourné de `rot`, arêtes abattues de `rayon` (normales lissées).
        public void Bloc(Vector3 centre, Vector3 taille, Quaternion rot, float rayon, Color32 col, int masque = Tout)
        {
            Vector3 h = taille * 0.5f;
            float rmax = Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f;
            float r = Mathf.Clamp(rayon, 0.001f, Mathf.Max(0.001f, rmax));
            Vector3 hi = new Vector3(h.x - r, h.y - r, h.z - r);
            for (int f = 0; f < 6; f++)
            {
                if ((masque & (1 << f)) == 0) continue;
                int ax = f >> 1;
                float sg = (f & 1) == 0 ? 1f : -1f;
                int au = (ax + 1) % 3, av = (ax + 2) % 3;
                s_U[0] = -h[au]; s_U[1] = -hi[au]; s_U[2] = hi[au]; s_U[3] = h[au];
                s_V[0] = -h[av]; s_V[1] = -hi[av]; s_V[2] = hi[av]; s_V[3] = h[av];
                Vector3 nf = Vector3.zero; nf[ax] = sg;
                int b = v.Count;
                for (int a = 0; a < 4; a++)
                    for (int bb = 0; bb < 4; bb++)
                    {
                        Vector3 p = Vector3.zero;
                        p[ax] = sg * h[ax]; p[au] = s_U[a]; p[av] = s_V[bb];
                        Vector3 inner = new Vector3(Mathf.Clamp(p.x, -hi.x, hi.x), Mathf.Clamp(p.y, -hi.y, hi.y), Mathf.Clamp(p.z, -hi.z, hi.z));
                        Vector3 d = p - inner;
                        Vector3 nn = d.sqrMagnitude > 1e-10f ? d.normalized : nf;
                        v.Add(centre + rot * (inner + nn * r));
                        n.Add(rot * nn);
                        c.Add(col);
                    }
                Vector3 nw = rot * nf;
                for (int a = 0; a < 3; a++)
                    for (int bb = 0; bb < 3; bb++)
                    {
                        int i0 = b + a * 4 + bb;
                        Quad(i0, i0 + 1, i0 + 5, i0 + 4, nw);
                    }
            }
        }

        /// Ajoute le quadrilatère (i0, i1, i2, i3) dans l'ordre qui le tourne vers `normale`.
        void Quad(int i0, int i1, int i2, int i3, Vector3 normale)
        {
            Vector3 cr = Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]);
            if (cr.sqrMagnitude < 1e-12f) cr = Vector3.Cross(v[i2] - v[i0], v[i3] - v[i0]);
            if (Vector3.Dot(cr, normale) >= 0f) { t.Add(i0); t.Add(i1); t.Add(i2); t.Add(i0); t.Add(i2); t.Add(i3); }
            else { t.Add(i0); t.Add(i2); t.Add(i1); t.Add(i0); t.Add(i3); t.Add(i2); }
        }

        /// Quadrilatère plat (a, b, c, d dans l'ordre du contour).
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 normale, Color32 col)
        {
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) { n.Add(normale); c.Add(col); }
            Quad(i0, i0 + 1, i0 + 2, i0 + 3, normale);
        }

        /// Quadrilatère à normales par sommet (surfaces courbes : voûte).
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Color32 col)
        {
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            n.Add(na); n.Add(nb); n.Add(nc); n.Add(nd);
            for (int k = 0; k < 4; k++) c.Add(col);
            Quad(i0, i0 + 1, i0 + 2, i0 + 3, (na + nb + nc + nd).normalized);
        }

        /// Fût de révolution (pilier, tambour) : rayon r, hauteur H depuis `bas`, arêtes abattues de `ch`, `cotes` côtés.
        public void Tour(Vector3 bas, float r, float H, float ch, int cotes, Color32 col, bool dessus, bool dessous, float rDessus = -1f)
        {
            float rt = rDessus > 0f ? rDessus : r;
            ch = Mathf.Min(ch, H * 0.45f);
            // profil (rayon, hauteur, normale radiale, normale verticale)
            Vector4[] prof =
            {
                new Vector4(r - ch, 0f, 0.55f, -0.83f), new Vector4(r, ch, 1f, 0f),
                new Vector4(rt, H - ch, 1f, 0f), new Vector4(rt - ch, H, 0.55f, 0.83f)
            };
            int b = v.Count;
            for (int k = 0; k <= cotes; k++)
            {
                float a = 2f * Mathf.PI * k / cotes;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (int p = 0; p < 4; p++)
                {
                    v.Add(bas + new Vector3(ca * prof[p].x, prof[p].y, sa * prof[p].x));
                    n.Add(new Vector3(ca * prof[p].z, prof[p].w, sa * prof[p].z).normalized);
                    c.Add(col);
                }
            }
            for (int k = 0; k < cotes; k++)
                for (int p = 0; p < 3; p++)
                {
                    int i0 = b + k * 4 + p, i1 = i0 + 4, i2 = i1 + 1, i3 = i0 + 1;
                    Vector3 mid = (v[i0] + v[i2]) * 0.5f - bas; mid.y = 0f;
                    Quad(i0, i1, i2, i3, mid + Vector3.up * (p == 0 ? -0.5f : p == 2 ? 0.5f : 0f));
                }
            if (dessus) Disque(bas + Vector3.up * H, rt - ch, cotes, Vector3.up, col);
            if (dessous) Disque(bas, r - ch, cotes, Vector3.down, col);
        }

        public void Disque(Vector3 centre, float r, int cotes, Vector3 normale, Color32 col, Color32? colCentre = null)
        {
            int b = v.Count;
            v.Add(centre); n.Add(normale); c.Add(colCentre ?? col);
            Vector3 u = Vector3.Cross(normale, Mathf.Abs(normale.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 w = Vector3.Cross(normale, u);
            for (int k = 0; k <= cotes; k++)
            {
                float a = 2f * Mathf.PI * k / cotes;
                v.Add(centre + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r); n.Add(normale); c.Add(col);
            }
            for (int k = 0; k < cotes; k++)
            {
                int i1 = b + 1 + k, i2 = i1 + 1;
                Vector3 cr = Vector3.Cross(v[i1] - v[b], v[i2] - v[b]);
                if (Vector3.Dot(cr, normale) >= 0f) { t.Add(b); t.Add(i1); t.Add(i2); }
                else { t.Add(b); t.Add(i2); t.Add(i1); }
            }
        }

        /// Polygone convexe plat (éventail depuis le centre).
        public void Polygone(Vector3 centre, List<Vector3> contour, Vector3 normale, Color32 col, Color32 colCentre)
        {
            int b = v.Count;
            v.Add(centre); n.Add(normale); c.Add(colCentre);
            foreach (var p in contour) { v.Add(p); n.Add(normale); c.Add(col); }
            for (int k = 0; k < contour.Count; k++)
            {
                int i1 = b + 1 + k, i2 = b + 1 + (k + 1) % contour.Count;
                Vector3 cr = Vector3.Cross(v[i1] - v[b], v[i2] - v[b]);
                if (Vector3.Dot(cr, normale) >= 0f) { t.Add(b); t.Add(i1); t.Add(i2); }
                else { t.Add(b); t.Add(i2); t.Add(i1); }
            }
        }

        public Mesh VersMesh(string nom)
        {
            var m = new Mesh { name = nom };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------ Teintes
        public static uint Hache(int a, int b, int c)
        {
            unchecked
            {
                uint h = (uint)a * 0x8DA6B343u ^ (uint)b * 0xD8163841u ^ (uint)c * 0xCB1AB31Fu;
                h ^= h >> 13; h *= 0x5BD1E995u; h ^= h >> 15;
                return h;
            }
        }

        public static uint Hache(float x, float y, float z) { return Hache(Mathf.RoundToInt(x * 20f), Mathf.RoundToInt(y * 20f), Mathf.RoundToInt(z * 20f)); }

        /// Variation douce d'une teinte (luminosité ± amp, et une pointe de chaleur), sans bruit.
        public static Color32 Teinte(Color32 baseCol, uint h, int amp)
        {
            int dl = (int)(h % (uint)(2 * amp + 1)) - amp;
            int dw = (int)((h >> 8) % 5u) - 2;
            return new Color32((byte)Mathf.Clamp(baseCol.r + dl + dw, 0, 255), (byte)Mathf.Clamp(baseCol.g + dl, 0, 255), (byte)Mathf.Clamp(baseCol.b + dl - dw, 0, 255), 255);
        }
    }
}
