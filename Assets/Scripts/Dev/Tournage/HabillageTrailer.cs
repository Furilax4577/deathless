#if UNITY_EDITOR
using System.Collections.Generic;
using Deathless.UI.Ecrans;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.Dev.Tournage
{
    /// Habillage de la bande-annonce en UI Toolkit : cartons (textes du storyboard), voile noir, écran final. Un panneau
    /// à part (copie du DeathlessPanel : thème Deathless, jetons --dl-*, police Fredoka) rendu dans la RenderTexture
    /// d'interface de l'encodeur, jamais à l'écran. Le HUD du jeu (UIDocument de NavigateurEcrans) est branché sur le
    /// même panneau pendant le tournage (BrancherHud), montré ou masqué par plan (Hud), et rendu à son panneau d'origine
    /// à la fin (Debrancher). Styles : Assets/Scripts/Dev/Tournage/Tournage.uss.
    public class HabillageTrailer : MonoBehaviour
    {
        public const string CheminUss = "Assets/Scripts/Dev/Tournage/Tournage.uss";

        sealed class Carton
        {
            public Label label;
            public string texte;
            public float debut, fin;
            public bool instantane;
            public string classe;
        }

        PanelSettings m_Panneau;
        UIDocument m_Doc;
        VisualElement m_Racine, m_Voile, m_Final;
        Label m_Centre, m_Bas, m_Titre, m_Accroche, m_Coop, m_Appel;
        readonly List<Carton> m_Cartons = new List<Carton>();
        float m_DebutFinal = -1f;
        System.Func<float, float> m_Voilage;

        UIDocument m_NavDoc;
        PanelSettings m_PanneauHud;
        bool m_HudVisible = true;
        public bool HudVisible => m_HudVisible;
        public NavigateurEcrans Navigateur { get; private set; }
        public string Etat { get; private set; } = "";

        /// Voile noir (0-1) courant : pour l'encodeur (noir sous le texte).
        public float Noir { get; private set; }

        public static HabillageTrailer Creer(Transform parent, RenderTexture cible)
        {
            var go = new GameObject("HabillageTrailer");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HabillageTrailer>();
            h.Construire(cible);
            return h;
        }

        void Construire(RenderTexture cible)
        {
            var modele = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings/DeathlessPanel.asset");
            m_Panneau = modele != null ? Instantiate(modele) : ScriptableObject.CreateInstance<PanelSettings>();
            m_Panneau.name = "PanneauTournage";
            m_Panneau.targetTexture = cible;
            m_Panneau.clearColor = true;
            m_Panneau.colorClearValue = new Color(0f, 0f, 0f, 0f);
            m_Panneau.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            m_Panneau.referenceResolution = new Vector2Int(1920, 1080);
            m_Panneau.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            m_Panneau.match = 0.5f;

            m_Doc = gameObject.AddComponent<UIDocument>();
            m_Doc.panelSettings = m_Panneau;
            m_Doc.sortingOrder = 500;
            var root = m_Doc.rootVisualElement;
            var uss = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(CheminUss);
            if (uss != null) root.styleSheets.Add(uss); else Etat = "Tournage.uss introuvable ; ";
            root.pickingMode = PickingMode.Ignore;

            m_Racine = new VisualElement { name = "tr-racine", pickingMode = PickingMode.Ignore };
            m_Racine.AddToClassList("tr-racine");
            root.Add(m_Racine);
            m_Voile = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Voile.AddToClassList("tr-voile");
            m_Racine.Add(m_Voile);
            m_Centre = NouveauLabel("tr-centre");
            m_Bas = NouveauLabel("tr-bas");
            m_Final = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Final.AddToClassList("tr-final");
            m_Racine.Add(m_Final);
            m_Titre = NouveauLabel("tr-final__titre", m_Final);
            m_Accroche = NouveauLabel("tr-final__accroche", m_Final);
            m_Coop = NouveauLabel("tr-final__coop", m_Final);
            m_Appel = NouveauLabel("tr-final__appel", m_Final);
            m_Titre.text = "DEATHLESS";
            m_Accroche.text = "4 AMIS. 1 RELIQUE. 0 PITIÉ.";
            m_Coop.text = "COOP 4 JOUEURS";
            m_Appel.text = "AJOUTEZ À VOTRE LISTE DE SOUHAITS";
            Effacer();
        }

        Label NouveauLabel(string classe, VisualElement parent = null)
        {
            var l = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
            l.AddToClassList(classe);
            (parent ?? m_Racine).Add(l);
            return l;
        }

        /// Vide les cartons du plan précédent.
        public void Effacer()
        {
            m_Cartons.Clear();
            m_DebutFinal = -1f;
            m_Voilage = null;
            Noir = 0f;
            foreach (var l in new[] { m_Centre, m_Bas })
            {
                l.text = "";
                l.style.opacity = 0f;
                l.ClearClassList();
            }
            m_Centre.AddToClassList("tr-centre");
            m_Bas.AddToClassList("tr-bas");
            m_Final.style.display = DisplayStyle.None;
            m_Voile.style.opacity = 0f;
        }

        /// Grand texte au centre (ouverture « choc »). `instantane` : présent dès sa première image, sans animation.
        public void Centre(string texte, float debut, float fin, bool instantane = false, string variante = null)
            => m_Cartons.Add(new Carton { label = m_Centre, texte = texte, debut = debut, fin = fin, instantane = instantane, classe = variante });

        /// Texte en bas de cadre ; `variante` : classe USS en plus (tr-bas--petit, tr-bas--bandeau).
        public void Bas(string texte, float debut, float fin, string variante = null, bool instantane = false)
            => m_Cartons.Add(new Carton { label = m_Bas, texte = texte, debut = debut, fin = fin, instantane = instantane, classe = variante });

        /// Voile noir plein écran sous les textes (0 transparent, 1 noir), fonction du temps du plan.
        public void Voile(System.Func<float, float> opacite) => m_Voilage = opacite;

        /// Écran final (titre, accroche, coop, liste de souhaits) à partir de `debut`.
        public void Final(float debut) => m_DebutFinal = debut;

        /// Met l'habillage à l'instant `t` du plan (appelé avant le rendu de l'image).
        public void Maj(float t)
        {
            Noir = m_Voilage != null ? Mathf.Clamp01(m_Voilage(t)) : 0f;
            m_Voile.style.opacity = Noir;
            foreach (var label in new[] { m_Centre, m_Bas })
            {
                Carton actif = null;
                foreach (var c in m_Cartons) if (c.label == label && t >= c.debut && t < c.fin) actif = c;
                if (actif == null) { label.style.opacity = 0f; continue; }
                if (label.text != actif.texte) label.text = actif.texte;
                foreach (var v in new[] { "tr-bas--petit", "tr-bas--bandeau", "tr-bas--or", "tr-centre--or", "tr-centre--haut" })
                    label.EnableInClassList(v, actif.classe != null && actif.classe.Contains(v));
                // HUD montré : le carton du bas remonte au-dessus de la barre des compétences.
                label.EnableInClassList("tr-bas--hud", label == m_Bas && m_HudVisible && (actif.classe == null || !actif.classe.Contains("tr-bas--bandeau")));
                float u = t - actif.debut, reste = actif.fin - t;
                float op = actif.instantane ? 1f : Mathf.Clamp01(u / 0.12f);
                if (reste < 0.12f && actif.fin < 900f) op = Mathf.Min(op, Mathf.Clamp01(reste / 0.12f));
                float e = actif.instantane ? 1f : 1f + 0.14f * Mathf.Pow(1f - Mathf.Clamp01(u / 0.22f), 3f);
                label.style.opacity = op;
                label.style.scale = new Scale(new Vector3(e, e, 1f));
            }
            if (m_DebutFinal >= 0f && t >= m_DebutFinal)
            {
                m_Final.style.display = DisplayStyle.Flex;
                float u = t - m_DebutFinal;
                Apparaitre(m_Titre, u, 0f, 1.25f);
                Apparaitre(m_Accroche, u, 0.7f, 1.1f);
                Apparaitre(m_Coop, u, 1.3f, 1.1f);
                Apparaitre(m_Appel, u, 1.6f, 1.1f);
            }
            else m_Final.style.display = DisplayStyle.None;
        }

        static void Apparaitre(VisualElement e, float u, float debut, float echelle)
        {
            float v = u - debut;
            float op = Mathf.Clamp01(v / 0.18f);
            float k = Mathf.Clamp01(v / 0.35f);
            float s = 1f + (echelle - 1f) * Mathf.Pow(1f - k, 3f);
            e.style.opacity = op;
            e.style.scale = new Scale(new Vector3(s, s, 1f));
        }

        // ------------------------------------------------------------------ HUD du jeu

        /// Branche l'UIDocument du navigateur d'écrans (HUD, lobby, roue à emotes) sur le panneau de tournage. À rappeler
        /// après chaque chargement de scène.
        public void BrancherHud()
        {
            Navigateur = FindAnyObjectByType<NavigateurEcrans>();
            m_NavDoc = Navigateur != null ? Navigateur.GetComponent<UIDocument>() : null;
            if (m_NavDoc == null) { Etat += "navigateur introuvable ; "; return; }
            if (m_NavDoc.panelSettings != m_Panneau)
            {
                m_PanneauHud = m_NavDoc.panelSettings;
                m_NavDoc.panelSettings = m_Panneau;
            }
            Hud(m_HudVisible);
        }

        public void Hud(bool visible)
        {
            m_HudVisible = visible;
            if (m_NavDoc != null && m_NavDoc.rootVisualElement != null)
                m_NavDoc.rootVisualElement.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        /// Racine du navigateur (translation / échelle pour un travelling sur l'interface).
        public VisualElement RacineHud => m_NavDoc != null ? m_NavDoc.rootVisualElement : null;

        /// Rend le HUD à son panneau d'origine (fin du tournage).
        public void Debrancher()
        {
            if (m_NavDoc != null)
            {
                if (m_NavDoc.rootVisualElement != null)
                {
                    m_NavDoc.rootVisualElement.style.visibility = StyleKeyword.Null;
                    m_NavDoc.rootVisualElement.style.translate = StyleKeyword.Null;
                    m_NavDoc.rootVisualElement.style.scale = StyleKeyword.Null;
                }
                if (m_PanneauHud != null && m_NavDoc.panelSettings == m_Panneau) m_NavDoc.panelSettings = m_PanneauHud;
            }
            m_NavDoc = null;
        }

        void OnDestroy()
        {
            Debrancher();
            if (m_Panneau != null) Destroy(m_Panneau);
        }
    }
}
#endif
