using System.Collections;
using Deathless.UI.Ecrans;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.Jeu
{
    /// Fondu au noir plein cadre (03/10/2026, entrée et sortie des bâtiments de la carte v5 ; il n'y en avait pas avant :
    /// les portails du donjon passent par des gemmes, pas par un voile). Un voile noir dans un UIDocument à lui, créé à la
    /// demande (aucun changement de scène), sur le panneau du jeu (DeathlessPanel, celui du navigateur d'écrans) et au-dessus de
    /// tous les écrans : le HUD, l'invite et les messages passent donc sous le noir. Sans panneau (aucun navigateur d'écrans
    /// dans la scène), le fondu ne montre rien et l'enchaînement continue, sans erreur.
    /// Les durées sont en temps réel (le jeu en pause ne bloque pas un fondu commencé).
    public class FonduNoir : MonoBehaviour
    {
        /// Ordre d'affichage du voile dans le panneau (le navigateur d'écrans est à 0).
        const int OrdreAffichage = 5000;

        static FonduNoir s_I;
        UIDocument m_Doc;
        VisualElement m_Voile;
        float m_Opacite;

        /// Opacité courante du voile (0 : transparent, 1 : écran noir) ; pour les tests.
        public static float Opacite => s_I != null ? s_I.m_Opacite : 0f;

        static FonduNoir Assurer()
        {
            if (s_I != null) return s_I;
            var ecrans = FindAnyObjectByType<NavigateurEcrans>();
            var doc0 = ecrans != null ? ecrans.GetComponent<UIDocument>() : null;
            if (doc0 == null || doc0.panelSettings == null) return null;
            var go = new GameObject("FonduNoir");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = doc0.panelSettings;
            doc.sortingOrder = OrdreAffichage;
            s_I = go.AddComponent<FonduNoir>();
            s_I.m_Doc = doc;
            go.SetActive(true);
            return s_I;
        }

        void OnDestroy() { if (s_I == this) s_I = null; }

        /// Le voile existe dans le panneau (recréé si le panneau a été reconstruit) ; faux tant que la racine n'est pas prête.
        bool Pret()
        {
            if (m_Doc == null) return false;
            VisualElement racine = m_Doc.rootVisualElement;
            if (racine == null) return false;
            if (m_Voile == null || m_Voile.panel == null || m_Voile.parent != racine)
            {
                racine.pickingMode = PickingMode.Ignore;
                m_Voile = new VisualElement { name = "fondu-noir", pickingMode = PickingMode.Ignore };
                IStyle s = m_Voile.style;
                s.position = Position.Absolute;
                s.left = 0; s.top = 0; s.right = 0; s.bottom = 0;
                s.backgroundColor = Color.black;
                s.opacity = m_Opacite;
                s.display = m_Opacite > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
                racine.Add(m_Voile);
            }
            return true;
        }

        void Poser(float o)
        {
            m_Opacite = Mathf.Clamp01(o);
            if (!Pret()) return;
            m_Voile.style.opacity = m_Opacite;
            m_Voile.style.display = m_Opacite > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// Fondu vers l'opacité `cible` (1 : noir, 0 : transparent) en `duree` s (temps réel), à utiliser par `yield return`.
        public static IEnumerator Vers(float cible, float duree)
        {
            var f = Assurer();
            if (f == null) { yield return new WaitForSecondsRealtime(duree); yield break; }
            float depart = f.m_Opacite, t = 0f;
            f.Poser(depart);
            while (t < duree)
            {
                t += Time.unscaledDeltaTime;
                if (f == null) yield break;
                f.Poser(Mathf.Lerp(depart, cible, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duree))));
                yield return null;
            }
            if (f != null) f.Poser(cible);
        }

        /// Retire le voile tout de suite (passage interrompu).
        public static void Couper() { if (s_I != null) s_I.Poser(0f); }
    }
}
