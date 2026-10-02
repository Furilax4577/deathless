using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Bruits de pas par matière (02/10/2026 ; wiki : sons). Un raycast vers le bas par pas (jamais par image), sans
    /// allocation (RaycastNonAlloc, cache collider → matière) ; la matière vient du marqueur MatiereSol du collider
    /// touché, ou, pour le sol de la carte (maillage à palette), de la teinte du triangle touché. L'eau du donjon et des
    /// gués (ZoneEau) l'emporte sur le sol. Les variantes du catalogue (SonsDuJeu.PasParMatiere) sont tirées au hasard
    /// sans répéter la dernière, avec une légère variation de hauteur (± 6 %) et de volume.
    ///
    /// Table matière → sons (ids du catalogue ; fichiers sous Assets/Audio/Deathless/Pas/ sauf indication) :
    ///   Herbe  pas_herbe  herbe_1..5            Terre  pas_terre  terre_1..5 + terre_6 (Kenney footstep06)
    ///   Pierre pas_pierre pierre_1..3 + pierre_4..6 (Kenney footstep00, 07, 08)
    ///   Bois   pas_bois   bois_1..5             Metal  pas_metal  metal_1..5
    ///   Sable  pas_sable  sable_1..5            Eau    water_step (Relic, 3 variantes)
    /// Palette du sol de la carte (index de VillageBuilder.GroundPalette) : 0-5 herbes, 6-7 terres, 8 galets du lit
    /// (pierre ; sous l'eau, eau), 9-10 sable de la lande, 11 berge mouillée (terre) ; un point plus bas que le niveau
    /// de l'eau (y &lt; NiveauEau) est de l'eau.
    public static class PasMatiere
    {
        /// Matière par index de teinte de la palette du sol de la carte (voir ci-dessus).
        static readonly Matiere[] PaletteMatieres =
        {
            Matiere.Herbe, Matiere.Herbe, Matiere.Herbe, Matiere.Herbe, Matiere.Herbe, Matiere.Herbe,
            Matiere.Terre, Matiere.Terre, Matiere.Pierre, Matiere.Sable, Matiere.Sable, Matiere.Terre,
        };
        /// Hauteur de la surface de l'eau de la rivière (m) : en dessous, le lit est de l'eau.
        public const float NiveauEau = -0.1f;

        /// Gain de base par matière (les échantillons sont normalisés à environ -12 dBFS ; l'ancien pas Kenney, à -21 dB,
        /// jouait à 0,35) : la pierre et le métal, courts, ont un niveau perçu plus faible à crête égale.
        static readonly float[] Gains = { 0.20f, 0.24f, 0.28f, 0.26f, 0.24f, 0.22f, 0.30f };

        struct Info { public Matiere matiere; public bool palette; public bool ignorer; public int cases; }

        static readonly Dictionary<int, Info> s_Cache = new Dictionary<int, Info>(256);
        static readonly RaycastHit[] s_Touches = new RaycastHit[10];
        static readonly int[] s_Dernier = { -1, -1, -1, -1, -1, -1, -1 };
        static int s_DernierSquelette = -1;
        static AudioListener s_Ecouteur;

        /// Observateur des pas joués (banc de test, journal) : matière, point, nom du clip.
        public static System.Action<Matiere, Vector3, string> Joue;
        /// Dernière détection (matière et collider touché) : lecture pour les tests.
        public static Matiere Derniere { get; private set; }
        public static Collider DernierCollider { get; private set; }

        // ----------------------------------------------------------------- Détection

        static Info Lire(Collider c)
        {
            int id = c.GetInstanceID();
            if (s_Cache.TryGetValue(id, out Info info)) return info;
            info = new Info { matiere = Matiere.Pierre };
            // Les corps vivants (héros, squelettes, villageois) ne sont pas un sol.
            if (c is CharacterController || c.GetComponentInParent<Sante>() != null) info.ignorer = true;
            else
            {
                var m = c.GetComponentInParent<MatiereSol>();
                if (m != null)
                {
                    info.matiere = m.matiere;
                    info.palette = m.parPalette;
                    if (m.parPalette) info.cases = Mathf.Max(1, m.casesPalette);   // la texture importée peut être élargie (puissance de deux) : le nombre de teintes est noté sur le marqueur
                }
            }
            s_Cache[id] = info;
            return info;
        }

        /// Rayon vers le bas (de 0,5 m au-dessus des pieds à 0,6 m en dessous) : indice, dans s_Touches, du sol le plus
        /// proche (les corps vivants et `ignorer` sont écartés), ou -1.
        static int Frapper(Vector3 pieds, Transform ignorer, out Info ik)
        {
            int n = Physics.RaycastNonAlloc(pieds + Vector3.up * 0.5f, Vector3.down, s_Touches, 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int k = -1;
            float meilleur = float.MaxValue;
            ik = default;
            for (int i = 0; i < n; i++)
            {
                if (s_Touches[i].distance >= meilleur) continue;
                var c = s_Touches[i].collider;
                if (ignorer != null && c.transform.IsChildOf(ignorer)) continue;
                var info = Lire(c);
                if (info.ignorer) continue;
                meilleur = s_Touches[i].distance; k = i; ik = info;
            }
            return k;
        }

        /// Hauteur du sol juste sous `pieds` (m) ; faux s'il n'y a rien à moins de 0,6 m dessous (en l'air).
        public static bool SolSous(Vector3 pieds, Transform ignorer, out float y)
        {
            int k = Frapper(pieds, ignorer, out _);
            y = k >= 0 ? s_Touches[k].point.y : pieds.y;
            return k >= 0;
        }

        /// Matière sous les pieds en `pieds` : eau (ZoneEau) sinon le sol touché par le rayon de Frapper. `ignorer` : racine
        /// du personnage (ses colliders sont écartés).
        public static Matiere Sous(Vector3 pieds, Transform ignorer = null)
        {
            if (Deathless.Donjon.ZoneEau.FacteurEn(pieds + Vector3.up * 0.2f) < 0.99f) { DernierCollider = null; return Derniere = Matiere.Eau; }
            int k = Frapper(pieds, ignorer, out Info ik);
            if (k < 0) { DernierCollider = null; return Derniere = Matiere.Herbe; }
            DernierCollider = s_Touches[k].collider;
            Matiere m = ik.palette ? Palette(s_Touches[k], ik) : ik.matiere;
            return Derniere = m;
        }

        /// Matière du sol de la carte au point touché : teinte (coordonnée u du triangle) → matière ; sous l'eau, eau.
        static Matiere Palette(RaycastHit h, Info info)
        {
            if (h.point.y < NiveauEau) return Matiere.Eau;
            int i = Mathf.Clamp(Mathf.FloorToInt(h.textureCoord.x * info.cases), 0, info.cases - 1);
            return i < PaletteMatieres.Length ? PaletteMatieres[i] : info.matiere;
        }

        // ----------------------------------------------------------------- Lecture

        /// Pas d'un héros (ou d'une marionnette) : détection puis lecture. `intensite` : 1 en marche, plus en course ;
        /// `discret` : marche furtive.
        public static void Pas(Vector3 pieds, Transform proprietaire, float intensite = 1f, bool discret = false)
        {
            Matiere m = Sous(pieds, proprietaire);
            Jouer(m, pieds, intensite * (discret ? 0.35f : 1f), intensite > 1.05f ? 1.04f : 1f, 25f);
        }

        /// Réception d'un saut ou d'une chute : même matière, impact plus lourd (plus grave, plus fort).
        public static void Reception(Vector3 pieds, Transform proprietaire, float vitesseChute)
        {
            Matiere m = Sous(pieds, proprietaire);
            float k = Mathf.Clamp01((vitesseChute - 5f) / 10f);
            Jouer(m, pieds, 1.6f + 0.8f * k, 0.86f - 0.06f * k, 35f);
        }

        /// Pas d'un squelette : le pas d'os léger des squelettes (dl_squelette_pas) sur la terre, l'herbe et le sable ; sur
        /// un sol dur ou résonnant (pierre, bois, métal) ou dans l'eau, le pas de la matière, plus sourd et plus discret.
        public static void PasSquelette(Vector3 pieds, Transform proprietaire)
        {
            Matiere m = Sous(pieds, proprietaire);
            if (m == Matiere.Pierre || m == Matiere.Bois || m == Matiere.Metal || m == Matiere.Eau) { Jouer(m, pieds, 0.55f, 0.82f, 20f); return; }
            var bank = AudioBank.Instance;
            var e = bank != null && bank.catalogue != null ? bank.catalogue.Premiere(SonsDuJeu.SqueletteMarche) : null;
            if (e == null) { Jouer(m, pieds, 0.55f, 0.82f, 20f); return; }
            int j = Choisir(e.clips.Length, ref s_DernierSquelette);
            AudioBank.JouerClip(e.clips[j], pieds, 0.9f, Random.Range(0.94f, 1.06f), e.portee > 0f ? e.portee : 20f);
            Joue?.Invoke(m, pieds, e.clips[j].name);
        }

        static void Jouer(Matiere m, Vector3 point, float volume, float hauteur, float portee)
        {
            var bank = AudioBank.Instance;
            if (bank == null || bank.catalogue == null) return;
            var e = bank.catalogue.Premiere(SonsDuJeu.PasParMatiere[(int)m]) ?? bank.catalogue.Premiere(SonsDuJeu.Pas);
            if (e == null) return;
            int j = Choisir(e.clips.Length, ref s_Dernier[(int)m]);
            float gain = Gains[(int)m] * volume * Random.Range(0.85f, 1f);
            AudioBank.JouerClip(e.clips[j], point, gain, hauteur * Random.Range(0.94f, 1.06f), e.portee > 0f ? e.portee : portee);
            Joue?.Invoke(m, point, e.clips[j].name);
        }

        /// Indice tiré au hasard, jamais le même que le précédent (s'il y a le choix).
        static int Choisir(int n, ref int dernier)
        {
            if (n <= 1) { dernier = 0; return 0; }
            int j = Random.Range(0, n - 1);
            if (dernier >= 0 && j >= dernier) j++;
            dernier = j;
            return j;
        }

        /// Vrai si le point est à portée d'oreille (rayon `rayon`) : les squelettes lointains ne font ni raycast ni son.
        public static bool Proche(Vector3 p, float rayon)
        {
            if (s_Ecouteur == null) s_Ecouteur = Object.FindAnyObjectByType<AudioListener>();
            if (s_Ecouteur == null) return false;
            return (s_Ecouteur.transform.position - p).sqrMagnitude <= rayon * rayon;
        }

        /// Vide le cache des colliders (changement de scène : les identifiants ne sont plus valables).
        public static void Reinitialiser() { s_Cache.Clear(); s_Ecouteur = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReinitialiserAuDemarrage()
        {
            Reinitialiser(); Joue = null; s_DernierSquelette = -1;
            for (int i = 0; i < s_Dernier.Length; i++) s_Dernier[i] = -1;
        }
    }
}
