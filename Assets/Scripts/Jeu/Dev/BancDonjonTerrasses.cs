using Deathless.Donjon.Terrasses;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deathless.Jeu.Dev
{
    /// Banc du générateur de donjon « terrasses » (scène Assets/Scenes/Dev/DonjonBanc.unity) : régénère le donjon d'une graine
    /// donnée, place un héros à l'échelle sur la dalle d'arrivée et la caméra (vue du joueur à l'épaule : 5,5 m derrière,
    /// 3,7 m de haut, tangage 22°, champ 60° ; vue de dessus sans voûte ; vue isométrique en coupe ; caméra libre en jeu).
    /// En jeu : panneau en haut à gauche (graine, Générer, ←/→, vue, voûte, repères), touches N (graine suivante),
    /// V (vue suivante) ; en vue libre, ZQSD/WASD + A/E pour monter/descendre, clic droit pour tourner la tête.
    /// En édition : bouton dans l'inspecteur du constructeur et menu Deathless > Donjon > Terrasses.
    [ExecuteAlways]
    public class BancDonjonTerrasses : MonoBehaviour
    {
        public enum Vue { Joueur, Dessus, Iso, Libre }

        public ConstructeurTerrasses constructeur;
        public Camera cam;
        [Tooltip("Modèle de héros (2,3 m) posé sur la dalle d'arrivée, pour l'échelle.")]
        public Transform heros;
        public Vue vue = Vue.Joueur;

        string m_Saisie = "1";
        float m_Lacet, m_Tangage = 20f;

        void OnEnable()
        {
            if (constructeur != null && constructeur.Racine == null && constructeur.materiauPierre != null) Regenerer(constructeur.graine);
        }

        public void Regenerer(int g)
        {
            if (constructeur == null) return;
            constructeur.Generer(g);
            m_Saisie = g.ToString();
            PlacerHeros();
            PlacerCamera();
        }

        public void PlacerHeros()
        {
            if (heros == null || constructeur == null || constructeur.Plan == null) return;
            var j = constructeur.Plan.joueurs[1];
            heros.position = constructeur.Monde(j.x + 0.75f, j.y, j.z + 1.6f);
            heros.rotation = constructeur.transform.rotation;
        }

        public void PlacerCamera()
        {
            if (cam == null || constructeur == null || constructeur.Plan == null) return;
            var P = constructeur.Plan;
            constructeur.AfficherVoute(vue == Vue.Joueur || vue == Vue.Libre);
            constructeur.AfficherReperes(vue == Vue.Dessus || vue == Vue.Iso);
            constructeur.AfficherCoupe(vue == Vue.Iso);
            var tr = constructeur.transform;
            switch (vue)
            {
                case Vue.Joueur:
                {
                    // caméra de jeu : pivot à 1,6 m au-dessus du héros, tangage 22°, épaule 0,6 m à droite, recul 5,5 m
                    Vector3 hp = heros != null ? heros.position : constructeur.Monde(P.arrivee.x, 0f, P.arrivee.z + 1.6f);
                    Quaternion rot = tr.rotation * Quaternion.Euler(22f, 0f, 0f);
                    Vector3 pivot = hp + Vector3.up * 1.6f, epaule = pivot + rot * Vector3.right * 0.6f;
                    cam.orthographic = false; cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
                    cam.transform.SetPositionAndRotation(epaule - rot * Vector3.forward * 5.5f, rot);
                    m_Lacet = 0f; m_Tangage = 22f;
                    break;
                }
                case Vue.Dessus:
                {
                    // cadre : l'enceinte et les pièces cachées derrière elle
                    float x0 = -2f, z0 = -2f, x1 = P.L + 2f, z1 = P.P + 2f;
                    foreach (var pc in P.pieces) { x0 = Mathf.Min(x0, pc.r.x0 - 2f); z0 = Mathf.Min(z0, pc.r.z0 - 2f); x1 = Mathf.Max(x1, pc.r.x1 + 2f); z1 = Mathf.Max(z1, pc.r.z1 + 2f); }
                    float asp = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
                    cam.orthographic = true;
                    cam.orthographicSize = Mathf.Max((z1 - z0) * 0.5f + 1.5f, ((x1 - x0) * 0.5f + 1.5f) / asp);
                    cam.nearClipPlane = 1f; cam.farClipPlane = 200f;
                    cam.transform.SetPositionAndRotation(constructeur.Monde((x0 + x1) * 0.5f, P.Cle + 40f, (z0 + z1) * 0.5f), tr.rotation * Quaternion.LookRotation(Vector3.down, Vector3.forward));
                    break;
                }
                case Vue.Iso:
                {
                    Vector3 c = constructeur.Monde(P.L * 0.5f, 2f, P.P * 0.55f);
                    float d = Mathf.Max(P.L, P.P) * 1.55f;
                    cam.orthographic = false; cam.fieldOfView = 40f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300f;
                    Vector3 pos = c + tr.rotation * (new Vector3(-0.55f, 0.85f, -0.75f).normalized * d);
                    cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(c - pos, Vector3.up));
                    break;
                }
            }
        }

        void Update()
        {
            if (!Application.isPlaying || cam == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.nKey.wasPressedThisFrame) Regenerer(constructeur.graine + 1);
                if (kb.vKey.wasPressedThisFrame) { vue = (Vue)(((int)vue + 1) % 4); PlacerCamera(); }
            }
            if (vue != Vue.Libre) return;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 dm = mouse.delta.ReadValue() * 0.15f;
                m_Lacet += dm.x; m_Tangage = Mathf.Clamp(m_Tangage - dm.y, -80f, 80f);
            }
            cam.transform.rotation = Quaternion.Euler(m_Tangage, m_Lacet, 0f);
            if (kb == null) return;
            Vector3 dep = Vector3.zero;
            if (kb.wKey.isPressed || kb.zKey.isPressed) dep += Vector3.forward;
            if (kb.sKey.isPressed) dep += Vector3.back;
            if (kb.dKey.isPressed) dep += Vector3.right;
            if (kb.aKey.isPressed || kb.qKey.isPressed) dep += Vector3.left;
            if (kb.eKey.isPressed) dep += Vector3.up;
            if (kb.cKey.isPressed) dep += Vector3.down;
            float v = kb.leftShiftKey.isPressed ? 18f : 7f;
            cam.transform.position += cam.transform.rotation * dep * v * Time.deltaTime;
        }

        void OnGUI()
        {
            if (!Application.isPlaying || constructeur == null) return;
            GUILayout.BeginArea(new Rect(10, 10, 560, 230), GUI.skin.box);
            GUILayout.Label("Donjon « terrasses » : générateur procédural (N : graine suivante, V : vue)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Graine", GUILayout.Width(50));
            m_Saisie = GUILayout.TextField(m_Saisie, GUILayout.Width(90));
            int g;
            if (GUILayout.Button("Générer", GUILayout.Width(80)) && int.TryParse(m_Saisie, out g)) Regenerer(g);
            if (GUILayout.Button("←", GUILayout.Width(30))) Regenerer(constructeur.graine - 1);
            if (GUILayout.Button("→", GUILayout.Width(30))) Regenerer(constructeur.graine + 1);
            if (GUILayout.Button("Au hasard", GUILayout.Width(90))) Regenerer(Random.Range(1, 1000000));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (Vue v in System.Enum.GetValues(typeof(Vue)))
                if (GUILayout.Toggle(vue == v, v.ToString(), GUI.skin.button, GUILayout.Width(80)) && vue != v) { vue = v; PlacerCamera(); }
            GUILayout.EndHorizontal();
            var P = constructeur.Plan;
            if (P != null)
            {
                GUILayout.Label(P.Resume());
                GUILayout.Label("Plan " + constructeur.TempsPlanMs.ToString("0.0") + " ms, géométrie " + constructeur.TempsConstructionMs.ToString("0") + " ms, NavMesh "
                    + constructeur.TempsNavMeshMs.ToString("0") + " ms ; " + constructeur.NbSommets + " sommets, " + constructeur.NbLumieres + " lumières, " + constructeur.NbCollisions + " collisions");
            }
            GUILayout.EndArea();
        }
    }
}
