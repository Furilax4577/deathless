using System.IO;
using System.Text;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Raccourcis de test (appelés par execute_code en Play) : poser un squelette, placer le héros, relire l'état, capturer.
    public static class DevPartie
    {
        static Partie P => Partie.Instance;

        public static string Etat()
        {
            var p = P;
            if (p == null) return "pas de partie";
            var e = p.Etat;
            var sb = new StringBuilder();
            sb.Append(e.phase).Append(" nuit ").Append(e.nuit).Append(" reste ").Append(e.TempsRestant.ToString("F1")).Append(" s");
            sb.Append(" | Nyxessa ").Append(e.nyxessa.pv.ToString("F0")).Append("/").Append(e.nyxessa.pvMax.ToString("F0")).Append(" stock ").Append(e.nyxessa.stock);
            sb.Append(" | vague ").Append(e.vagues.vague).Append("/").Append(e.vagues.total).Append(" vivants ").Append(DirecteurVagues.Instance != null ? DirecteurVagues.Instance.Vivants.Count : 0).Append(" à sortir ").Append(e.vagues.restantsASortir);
            var j = p.JoueurLocal;
            if (j != null)
            {
                sb.Append(" | héros PV ").Append(j.pv.ToString("F0")).Append(" end ").Append(j.endurance.ToString("F0")).Append(j.mort ? " MORT (" + j.reapparitionRestante.ToString("F1") + " s)" : "");
                var s = j.score;
                sb.Append(" | score dégâts ").Append(s.degatsInfliges.ToString("F0")).Append(" tués ").Append(s.ennemisTues).Append(" morts ").Append(s.morts).Append(" évités ").Append(s.degatsEvitesNyxessa.ToString("F0")).Append(" soins ").Append(s.soinsProdigues.ToString("F0")).Append(" crit ").Append(s.coupsCritiques);
                var h = p.HerosLocal;
                if (h != null) sb.Append(" | action ").Append(h.ActionCourante).Append(h.EnGarde ? " GARDE" : "").Append(" pos ").Append(h.transform.position.ToString("F1"));
            }
            return sb.ToString();
        }

        /// Pose un squelette à `distance` m devant le héros, face à lui (sortie de terre comprise).
        public static Squelette PoserDevant(TypeEnnemi type, float distance, float lateral = 0f)
        {
            var h = P != null ? P.HerosLocal : null;
            if (h == null || DirecteurVagues.Instance == null) return null;
            Vector3 p = h.transform.position + h.transform.forward * distance + h.transform.right * lateral;
            if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) p = hit.position;
            return DirecteurVagues.Instance.Poser(type, p, false, false);
        }

        /// Place le héros (et la caméra derrière lui) en regardant un point.
        public static void PlacerHeros(Vector3 position, Vector3 regard)
        {
            var h = P != null ? P.HerosLocal : null;
            if (h == null) return;
            var cc = h.GetComponent<CharacterController>();
            cc.enabled = false;
            h.transform.position = position;
            Vector3 d = regard - position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) h.transform.rotation = Quaternion.LookRotation(d);
            cc.enabled = true;
            if (P.cameraJeu != null) { P.cameraJeu.lacet = h.transform.eulerAngles.y; }
        }

        public static void Capturer(string nom)
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/Screenshots");
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, nom + ".png"));
        }

        public static void Blesser(float degats)
        {
            var h = P != null ? P.HerosLocal : null;
            if (h != null) h.Sante.Encaisser(new InfoDegats { montant = degats, equipeSource = Equipe.Ennemis, point = h.transform.position });
        }

        public static void BlesserNyxessa(float degats)
        {
            if (P != null && P.nyxessa != null) P.nyxessa.Encaisser(new InfoDegats { montant = degats, equipeSource = Equipe.Ennemis, point = P.nyxessa.transform.position + Vector3.up * 2f });
        }
    }
}
