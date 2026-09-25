using System.Collections;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Media;
#endif

namespace Deathless.Dev
{
    /// Outil d'enregistrement (éditeur, en Play) : vidéo en boucle d'une caméra, sans interface (rendu de la caméra seule
    /// dans une RenderTexture), H.264 par UnityEditor.Media.MediaEncoder, pas de temps fixe (Time.captureFramerate).
    ///
    /// Boucle : on capture `duree + fondu` secondes. Les images [fondu, duree[ sont encodées telles quelles ; les
    /// `fondu` dernières secondes capturées sont fondues, pixel par pixel, dans les `fondu` premières (gardées en
    /// mémoire) et encodées à la fin. La dernière image encodée enchaîne donc sur la première sans saut. Ce qui tourne sur
    /// un cycle doit faire un nombre entier de tours sur `duree` (à régler avant, pour l'enregistrement seulement).
    /// Utilisé pour le fond animé du launcher (Assets/Screenshots/menu_nuit_boucle.mp4, Docs/ui-v01.md). Écrire la vidéo
    /// hors de Assets (ex. « Temp/… ») puis la copier : Unity importerait sinon un fichier encore incomplet.
    public class EnregistreurBoucle : MonoBehaviour
    {
        public Camera cam;
        public string chemin = "Assets/Screenshots/boucle.mp4";
        public string cheminRaccord;
        public int largeur = 1920, hauteur = 1080, imagesParSeconde = 30;
        public float duree = 12f, fondu = 1f;
        public uint debitKbps = 8000;
        [Tooltip("Images ignorées avant la capture (stabilisation).")]
        public int chauffe = 15;

        public bool Termine { get; private set; }
        public string Resultat { get; private set; } = "";
        public int ImagesEncodees { get; private set; }

        IEnumerator Start()
        {
#if UNITY_EDITOR
            var avant = Time.captureFramerate;
            Time.captureFramerate = imagesParSeconde;
            for (var i = 0; i < chauffe; i++) yield return null;

            int nBoucle = Mathf.RoundToInt(duree * imagesParSeconde);
            int nFondu = Mathf.RoundToInt(fondu * imagesParSeconde);
            int nTotal = nBoucle + nFondu;
            var rt = new RenderTexture(largeur, hauteur, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var tex = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
            var debut = new Color32[nFondu][];
            Color32[] premiere = null, derniere = null, seconde = null;

            var h264 = new H264EncoderAttributes { gopSize = (uint)imagesParSeconde, numConsecutiveBFrames = 2, profile = VideoEncodingProfile.H264High };
            var attrs = new VideoTrackEncoderAttributes(h264)
            {
                frameRate = new MediaRational(imagesParSeconde),
                width = (uint)largeur,
                height = (uint)hauteur,
                targetBitRate = debitKbps * 1000,
                includeAlpha = false,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(chemin)));
            using (var encodeur = new MediaEncoder(chemin, attrs))
            {
                for (var f = 0; f < nTotal; f++)
                {
                    yield return new WaitForEndOfFrame();
                    var ancienne = cam.targetTexture;
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = ancienne;
                    var actif = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0, false);
                    RenderTexture.active = actif;

                    if (f < nFondu)
                    {
                        debut[f] = tex.GetPixels32();
                        continue;
                    }
                    if (f >= nBoucle)
                    {
                        // Fondu : image capturée f (suite de la dernière image de la boucle) → image capturée f - nBoucle.
                        var j = f - nBoucle;
                        var w = j / (float)nFondu;
                        var cur = tex.GetPixels32();
                        var dst = debut[j];
                        for (var k = 0; k < cur.Length; k++)
                            cur[k] = Color32.Lerp(cur[k], dst[k], w);
                        tex.SetPixels32(cur);
                        if (f == nTotal - 1) derniere = cur;
                    }
                    else if (f == nFondu) premiere = tex.GetPixels32();
                    else if (f == nFondu + 1) seconde = tex.GetPixels32();
                    encodeur.AddFrame(tex);
                    ImagesEncodees++;
                }
            }

            // Raccord : écart moyen (0-255, par canal) entre la dernière et la première image encodées ; en référence,
            // l'écart entre deux images consécutives du milieu de la vidéo.
            var raccord = EcartMoyen(derniere, premiere);
            var reference = EcartMoyen(premiere, seconde);
            Resultat = "images=" + ImagesEncodees + " raccord(derniere->premiere)=" + raccord.ToString("F3")
                       + " reference(1->2)=" + reference.ToString("F3");
            if (!string.IsNullOrEmpty(cheminRaccord)) EcrireRaccord(derniere, premiere);

            Destroy(tex);
            rt.Release();
            Destroy(rt);
            Time.captureFramerate = avant;
#else
            Resultat = "éditeur seulement";
            yield break;
#endif
            Termine = true;
        }

        static float EcartMoyen(Color32[] a, Color32[] b)
        {
            if (a == null || b == null) return -1f;
            double somme = 0;
            for (var i = 0; i < a.Length; i++)
                somme += Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
            return (float)(somme / (a.Length * 3.0));
        }

        /// Dernière image (à gauche) et première (à droite), côte à côte, à mi-résolution.
        void EcrireRaccord(Color32[] gauche, Color32[] droite)
        {
            if (gauche == null || droite == null) return;
            int w = largeur / 2, h = hauteur / 2;
            var img = new Texture2D(w * 2 + 8, h, TextureFormat.RGBA32, false);
            var px = new Color32[(w * 2 + 8) * h];
            for (var i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    px[y * (w * 2 + 8) + x] = gauche[(y * 2) * largeur + x * 2];
                    px[y * (w * 2 + 8) + w + 8 + x] = droite[(y * 2) * largeur + x * 2];
                }
            img.SetPixels32(px);
            img.Apply();
            File.WriteAllBytes(cheminRaccord, img.EncodeToPNG());
            Destroy(img);
        }
    }
}
