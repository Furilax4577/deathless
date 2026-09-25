using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Deathless.Jeu.Dev
{
    /// Tests en Play sans le focus de l'éditeur : une manette XInput virtuelle dont l'état est écrit à chaque image
    /// (StateEvent), comme dans sandbox-ui. Les entrées passent donc par le vrai chemin du jeu (DeathlessControls →
    /// InputChordResolver → HerosEntrees). API statique appelée par execute_code :
    ///   EntreesSimulees.Stick(gauche, droite, durée), Appui("buttonSouth", durée), Maintenir("leftTrigger", vrai)…
    public class EntreesSimulees : MonoBehaviour
    {
        static EntreesSimulees s_Instance;
        static XInputController s_Pad;

        Vector2 m_Gauche, m_Droite;
        readonly Dictionary<string, float> m_Boutons = new Dictionary<string, float>();   // nom → fin (Time.time), ∞ si maintenu

        static EntreesSimulees I
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("EntreesSimulees");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<EntreesSimulees>();
                }
                if (s_Pad == null || !s_Pad.added)
                {
                    // Les manettes virtuelles d'une session précédente survivent au rechargement du domaine : une
                    // manette restée « appuyée » bloquerait les actions. On les retire avant d'en créer une.
                    RetirerAnciennes();
                    s_Pad = InputSystem.AddDevice<XInputController>("ManetteSimulee");
                }
                return s_Instance;
            }
        }

        public static string Etat() => s_Pad == null ? "pas de manette" : "manette " + s_Pad.name + " active=" + s_Pad.enabled + " courante=" + (Gamepad.current == s_Pad);

        public static void Stick(Vector2 gauche, Vector2 droite, float duree)
        {
            var i = I;
            i.m_Gauche = gauche; i.m_Droite = droite;
            if (duree > 0f) i.StartCoroutine(i.RelacherSticks(duree));
        }

        IEnumerator RelacherSticks(float d)
        {
            yield return new WaitForSeconds(d);
            m_Gauche = Vector2.zero; m_Droite = Vector2.zero;
        }

        /// Appui bref (ou maintenu `duree` s) sur un contrôle : buttonSouth (A), buttonEast (B), leftShoulder (LB),
        /// rightShoulder (RB), leftTrigger (LT), rightTrigger (RT), leftStickPress (L3), select (Vue), start…
        public static void Appui(string controle, float duree = 0.08f) => I.m_Boutons[controle] = Time.time + duree;

        public static void Maintenir(string controle, bool maintenu)
        {
            if (maintenu) I.m_Boutons[controle] = float.PositiveInfinity; else I.m_Boutons.Remove(controle);
        }

        public static void ToutRelacher()
        {
            if (s_Instance == null) return;
            s_Instance.m_Boutons.Clear();
            s_Instance.m_Gauche = s_Instance.m_Droite = Vector2.zero;
        }

        public static void RetirerAnciennes()
        {
            var anciennes = new List<InputDevice>();
            foreach (var d in InputSystem.devices) if (d.name.StartsWith("ManetteSimulee")) anciennes.Add(d);
            foreach (var d in anciennes) InputSystem.RemoveDevice(d);
        }

        public static void Retirer()
        {
            ToutRelacher();
            RetirerAnciennes();
            s_Pad = null;
        }

        void Update()
        {
            if (s_Pad == null || !s_Pad.added) return;
            var fin = new List<string>();
            using (StateEvent.From(s_Pad, out var ptr))
            {
                s_Pad.leftStick.WriteValueIntoEvent(m_Gauche, ptr);
                s_Pad.rightStick.WriteValueIntoEvent(m_Droite, ptr);
                foreach (var c in s_Pad.allControls)
                {
                    if (!(c is UnityEngine.InputSystem.Controls.ButtonControl bc) || c.parent != s_Pad) continue;
                    bool appuye = m_Boutons.TryGetValue(c.name, out float t) && Time.time < t;
                    bc.WriteValueIntoEvent(appuye ? 1f : 0f, ptr);
                }
                InputSystem.QueueEvent(ptr);
            }
            foreach (var kv in m_Boutons) if (Time.time >= kv.Value) fin.Add(kv.Key);
            // Garder une image de relâchement : on retire après l'écriture.
            foreach (var k in fin) m_Boutons.Remove(k);
        }

        void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }
    }
}
