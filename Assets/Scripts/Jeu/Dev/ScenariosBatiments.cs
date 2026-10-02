using System.Collections;
using System.Text;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Vérification en Play de l'entrée et de la sortie des bâtiments de la carte v5 (03/10/2026, sur le modèle de ScenariosRetours),
    /// lancée par execute_code en mode exploration : `ScenariosBatiments.Lancer("tous", "pieces")` (les six bâtiments),
    /// `Lancer("Forge", "pieces")` (un rôle). Pour chaque bâtiment : le héros devant la porte (invite « Entrer », capture), Interagir,
    /// bruit d'ouverture, fondu à mi-chemin (capture), pièce (position, enceinte de la caméra, ambiance, capture de la porte et de
    /// la pièce), invite « Sortir », Interagir, retour devant la porte du bâtiment (position, lacet, invite « Entrer »), fondu retiré.
    /// Résultat dans la console (préfixe [Bâtiments]) et ScenariosBatiments.Dernier ; captures Assets/Screenshots/<nom>_<rôle>_*.png.
    public class ScenariosBatiments : MonoBehaviour
    {
        static ScenariosBatiments() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosBatiments"); }

        static ScenariosBatiments s_I;
        public static string Dernier = "";
        public static readonly StringBuilder Rapport = new StringBuilder();

        public static void Lancer(string quoi = "tous", string capture = null)
        {
            if (s_I == null) s_I = new GameObject("ScenariosBatiments").AddComponent<ScenariosBatiments>();
            s_I.StopAllCoroutines();
            Rapport.Length = 0;
            s_I.StartCoroutine(s_I.Essai(quoi, capture));
        }

        static void Log(string t) { Rapport.AppendLine(t); Dernier = t; Debug.Log("[Bâtiments] " + t); }
        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;

        static EntreeBatiment Entree(string role)
        {
            foreach (var e in FindObjectsByType<EntreeBatiment>(FindObjectsSortMode.None)) if (e.role == role) return e;
            return null;
        }

        /// Impose la place, le regard et la caméra chaque image pendant `duree` s (la souris du bureau fait dériver le lacet).
        IEnumerator Tenir(Heros h, float lacetCamera, float duree)
        {
            var q = h.transform.rotation;
            float t = 0f;
            while (t < duree)
            {
                t += Time.unscaledDeltaTime;
                if (!h.EnTransit) { h.transform.rotation = q; if (P.cameraJeu != null) P.cameraJeu.lacet = lacetCamera; }
                yield return null;
            }
        }

        /// Capture au bout de l'image courante (ScreenCapture.CaptureScreenshot attend le prochain rendu de la vue Game, qui peut venir bien plus
        /// tard quand l'éditeur n'a pas le focus : les fichiers se décalaient d'une étape à l'autre). Le HUD et le voile sont dans l'image.
        IEnumerator Photo(string capture, string nom)
        {
            if (capture == null) yield break;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Assets/Screenshots");
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, capture + "_" + nom + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        /// Ambiance telle qu'elle est rendue (lue au bout de l'image, après tous les LateUpdate).
        IEnumerator LireAmbiance(string role, string etiquette)
        {
            yield return new WaitForEndOfFrame();
            Log(role + " : " + etiquette + " : ambiance " + RenderSettings.ambientSkyColor + " ; soleil " + FindAnyObjectByType<DayCycle>().GetComponent<Light>().intensity.ToString("0.00") + " ; brouillard " + RenderSettings.fogColor);
        }

        static string Invite()
        {
            PointInteraction.Courant(H, out string inv);
            return inv ?? "(aucune)";
        }

        static string SonsDePorte()
        {
            var sb = new StringBuilder();
            var ab = AudioBank.Instance;
            if (ab == null) return "pas d'AudioBank";
            foreach (var s in ab.GetComponentsInChildren<AudioSource>())
                if (s.isPlaying && s.clip != null && s.clip.name.ToLowerInvariant().Contains("door")) sb.Append(s.clip.name).Append(' ');
            return sb.Length > 0 ? sb.ToString().Trim() : "aucun son de porte en cours";
        }

        IEnumerator Essai(string quoi, string capture)
        {
            if (P == null || H == null) { Log("pas de héros local (lancer le mode exploration)"); yield break; }
            if (!Partie.Exploration) Log("ATTENTION : pas en mode exploration");
            var h = H;
            bool inv = h.Sante.invulnerable;
            h.Sante.invulnerable = true;
            var roles = quoi == "tous" ? PiecesBatiments.Roles : new[] { quoi };
            foreach (string role in roles) yield return Un(role, capture);
            h.Sante.invulnerable = inv;
            Log("terminé");
        }

        IEnumerator Un(string role, string capture)
        {
            var h = H;
            var e = Entree(role);
            if (e == null) { Log(role + " : pas de composant EntreeBatiment sur la scène"); yield break; }
            // devant la porte : à 0,3 m du pied, face au bâtiment, caméra derrière lui
            Vector3 pied = e.PointRetour;
            Vector3 dos = e.transform.forward; dos.y = 0f;
            Vector3 depart = pied + dos * 0.3f;
            float lacetFace = e.LacetRetour + 180f;
            DevPartie.PlacerHeros(depart, depart - dos);
            yield return Tenir(h, lacetFace, 0.6f);
            Log(role + " : devant la porte en " + h.transform.position.ToString("F2") + " ; invite = " + Invite());
            yield return Photo(capture, role + "_1_devant");
            yield return null;
            string invDehors = Invite();
            bool ok = PointInteraction.InteragirIci(h);
            Log(role + " : Interagir = " + ok + ", EnTransit = " + h.EnTransit);
            // à 0,15 s : la porte s'ouvre (son), le fondu commence
            yield return new WaitForSecondsRealtime(0.15f);
            Log(role + " : sons de porte = " + SonsDePorte() + " ; voile " + FonduNoir.Opacite.ToString("0.00"));
            yield return new WaitForSecondsRealtime(0.17f);
            yield return Photo(capture, role + "_2_fondu");
            Log(role + " : voile à mi-fondu " + FonduNoir.Opacite.ToString("0.00"));
            float t0 = Time.realtimeSinceStartup;
            while (h.EnTransit && Time.realtimeSinceStartup - t0 < 5f)
            {
                yield return null;
            }
            Log(role + " : passage fini en " + (Time.realtimeSinceStartup - t0).ToString("0.00") + " s après la capture ; héros " + h.transform.position.ToString("F2") + " lacet " + h.transform.eulerAngles.y.ToString("F0") + " ; voile " + FonduNoir.Opacite.ToString("0.00"));
            var piece = PiecesBatiments.PieceLocale;
            Log(role + " : pièce locale = " + (piece != null ? piece.role : "AUCUNE") + " ; invite = " + Invite());
            yield return Tenir(h, 90f, 0.7f);
            if (piece != null)
            {
                Log(role + " : enceinte caméra = " + (CameraEpaule.Enceinte.HasValue ? CameraEpaule.Enceinte.Value.ToString() : "null") + " ; caméra en " + P.cameraJeu.transform.position.ToString("F2")
                    + (CameraEpaule.Enceinte.HasValue && CameraEpaule.Enceinte.Value.Contains(P.cameraJeu.transform.position) ? " (dans l'enceinte)" : " (HORS enceinte)"));
                yield return LireAmbiance(role, "jour");
                yield return Photo(capture, role + "_3_dedans");
                yield return Tenir(h, 90f, 0.3f);
                // vers le centre de la pièce : vue d'ensemble (caméra de dos, comme en jeu)
                DevPartie.PlacerHeros(piece.racine.position + new Vector3(0.6f, 0.05f, 0f), piece.racine.position + new Vector3(5f, 0f, 0f));
                yield return Tenir(h, 90f, 0.8f);
                yield return Photo(capture, role + "_4_centre");
                // regard vers la porte de sortie (caméra tournée vers l'ouest)
                DevPartie.PlacerHeros(piece.arrivee, piece.arrivee + Vector3.left * 3f);
                yield return Tenir(h, 270f, 0.8f);
                Log(role + " : devant la porte de la pièce, invite = " + Invite());
                yield return Photo(capture, role + "_5_porte");
                // F9 : nuit forcée (n'agit pas sur la pièce) ; aperçu seulement
                Partie.DefinirNuitApercu(true);
                yield return Tenir(h, 270f, 1.2f);
                yield return LireAmbiance(role, "nuit forcée (F9)");
                yield return Photo(capture, role + "_6_porte_nuit");
                Partie.DefinirNuitApercu(false);
                yield return Tenir(h, 270f, 0.5f);
                // sortie
                DevPartie.PlacerHeros(piece.arrivee, piece.arrivee + Vector3.right * 3f);
                yield return Tenir(h, 90f, 0.3f);
                ok = PointInteraction.InteragirIci(h);
                Log(role + " : Interagir pour sortir = " + ok);
                yield return new WaitForSecondsRealtime(0.15f);
                Log(role + " : sons de porte (sortie) = " + SonsDePorte());
                yield return new WaitForSecondsRealtime(0.17f);
                yield return Photo(capture, role + "_7_fondu_sortie");
                t0 = Time.realtimeSinceStartup;
                while (h.EnTransit && Time.realtimeSinceStartup - t0 < 5f) yield return null;
                yield return null;
                Vector3 attendu = e.PointRetour;
                float ecart = Vector2.Distance(new Vector2(h.transform.position.x, h.transform.position.z), new Vector2(attendu.x, attendu.z));
                float dLacet = Mathf.Abs(Mathf.DeltaAngle(h.transform.eulerAngles.y, e.LacetRetour));
                Log(role + " : dehors en " + h.transform.position.ToString("F2") + " (écart au pied de la porte " + ecart.ToString("0.00") + " m, lacet " + h.transform.eulerAngles.y.ToString("F0") + ", écart " + dLacet.ToString("0") + "°) ; pièce locale = " + (PiecesBatiments.PieceLocale != null ? "OUI" : "non")
                    + " ; voile " + FonduNoir.Opacite.ToString("0.00") + " ; enceinte " + (CameraEpaule.Enceinte.HasValue ? "ENCORE POSÉE" : "null") + " ; invite = " + Invite());
                yield return Tenir(h, e.LacetRetour, 0.7f);
                yield return Photo(capture, role + "_8_dehors");
            }
        }
    }
}
