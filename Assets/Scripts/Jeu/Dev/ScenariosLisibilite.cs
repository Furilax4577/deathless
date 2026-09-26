using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Scène de combat du chantier « lisibilité caméra » (retour de Quentin, 0.5.4 : « les ennemis sont durs à lire »)
    /// : héros au village, trois squelettes autour (un qui prépare son coup au premier plan, deux qui approchent),
    /// capturée sous la caméra actuelle puis trois variantes (A, B, C), de jour puis de nuit. Lancé depuis execute_code
    /// en Play (ScenariosLisibilite.Lancer()). Ne modifie la caméra que le temps des captures (PoserCamera /
    /// RestaurerCamera) ; ne touche pas à GameBalance (les variantes sont posées directement sur la caméra et le
    /// composant CameraEpaule est désactivé pendant la manœuvre pour qu'il ne recalcule pas par-dessus).
    public class ScenariosLisibilite : MonoBehaviour
    {
        static ScenariosLisibilite s_I;

        public struct Variante { public string nom; public float tangage, distance, epaule, champ; }

        public static readonly Variante Actuelle = new Variante { nom = "actuelle", tangage = 12f, distance = 4.5f, epaule = 0.6f, champ = 60f };
        // A : plus haute et plus reculée (voir un plus grand rayon autour du héros).
        public static readonly Variante A = new Variante { nom = "A", tangage = 22f, distance = 5.5f, epaule = 0.6f, champ = 60f };
        // B : champ plus large (+10°), sans bouger.
        public static readonly Variante B = new Variante { nom = "B", tangage = 12f, distance = 4.5f, epaule = 0.6f, champ = 70f };
        // C : épaule moins décalée, héros plus bas dans l'image, ennemis plus centrés.
        public static readonly Variante C = new Variante { nom = "C", tangage = 18f, distance = 4.5f, epaule = 0.25f, champ = 60f };

        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;
        static CameraEpaule Cam => Partie.Instance != null ? Partie.Instance.cameraJeu : null;

        public static void Lancer()
        {
            if (s_I == null) s_I = new GameObject("ScenariosLisibilite").AddComponent<ScenariosLisibilite>();
            s_I.StopAllCoroutines();
            s_I.StartCoroutine(s_I.Sequence());
        }

        static void Log(string t) => Debug.Log("[Lisibilite] " + t);

        IEnumerator Sequence()
        {
            var h = H; var cam = Cam;
            if (h == null || cam == null) { Log("pas de héros/caméra"); yield break; }
            float tangageOrigine = cam.tangage;

            // Place ouverte du village, dégagée (pas de mur proche pour le recul de la caméra).
            Vector3 pos = new Vector3(0f, 0f, -9.5f), regard = new Vector3(0f, 0f, 0f);   // point de départ naturel, déjà face à Nyxessa (contre-jour de nuit)
            DevPartie.PlacerHeros(pos, regard);
            yield return null;

            foreach (bool nuit in new[] { false, true })
            {
                if (Partie.Instance != null) Partie.Instance.ForcerPhase(nuit ? Phase.Nuit : Phase.Jour, Mathf.Max(1, Partie.Instance.Etat.nuit));
                yield return new WaitForSeconds(nuit ? 2.6f : 0.3f);   // laisse l'ambiance (et la bannière de phase) se stabiliser

                var attaquant = DevPartie.PoserDevant(TypeEnnemi.Sbire, 2.4f, 0f);
                var g1 = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 6f, -2.5f);
                var g2 = DevPartie.PoserDevant(TypeEnnemi.Sbire, 6.5f, 2.2f);
                var groupe = new List<Squelette> { attaquant, g1, g2 };
                yield return SortirTous(groupe);

                // Attend que l'attaquant (à portée de contact dès la sortie de terre) entre en préparation, puis vise le
                // pic de lisibilité (juste avant l'impact : PreparationProgress proche de 1).
                float t = 0f;
                while (attaquant != null && attaquant.EtatCourant != Squelette.Etat.Preparation && t < 3f) { t += Time.deltaTime; yield return null; }
                while (attaquant != null && attaquant.EtatCourant == Squelette.Etat.Preparation && attaquant.PreparationProgress < 0.6f) yield return null;

                float ts = Time.timeScale; Time.timeScale = 0f;   // fige la pose exacte pendant les 4 captures caméra
                cam.enabled = false;   // capture manuelle des variantes : CameraEpaule ne doit pas recalculer par-dessus
                foreach (var v in new[] { Actuelle, A, B, C })
                {
                    PoserCamera(cam, h, v);
                    yield return null;   // laisse le rendu se mettre à jour avec la nouvelle position/FOV
                    DevPartie.Capturer("lisibilite_cam_" + v.nom + "_" + (nuit ? "nuit" : "jour"));
                    yield return new WaitForSecondsRealtime(0.05f);   // temps réel : Time.timeScale est à 0 pendant la manœuvre
                }
                cam.enabled = true;
                cam.tangage = tangageOrigine;
                Time.timeScale = ts;

                Log((nuit ? "nuit" : "jour") + " : capturé, attaquant " + (attaquant != null ? attaquant.EtatCourant.ToString() + " " + attaquant.PreparationProgress.ToString("F2") : "absent"));
                if (attaquant != null) Destroy(attaquant.gameObject);
                if (g1 != null) Destroy(g1.gameObject);
                if (g2 != null) Destroy(g2.gameObject);
                yield return new WaitForSeconds(0.3f);
            }
            Log("terminé");
        }

        static IEnumerator SortirTous(List<Squelette> liste)
        {
            float t = 0f; bool enCours = true;
            while (enCours && t < 4f)
            {
                enCours = false;
                foreach (var s in liste) if (s != null && s.EtatCourant == Squelette.Etat.SortieDeTerre) enCours = true;
                t += Time.deltaTime; yield return null;
            }
        }

        static void PoserCamera(CameraEpaule cam, Heros h, Variante v)
        {
            var camU = cam.GetComponent<Camera>();
            Quaternion rot = Quaternion.Euler(v.tangage, cam.lacet, 0f);
            Vector3 pivot = h.transform.position + Vector3.up * GameBalance.Courant.cameraHauteur;
            Vector3 pos = pivot + rot * Vector3.right * v.epaule + rot * Vector3.back * v.distance;
            cam.transform.SetPositionAndRotation(pos, rot);
            if (camU != null) camU.fieldOfView = v.champ;
        }
    }
}
