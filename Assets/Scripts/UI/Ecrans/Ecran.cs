using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Base d'un écran (menu principal, HUD, pause…). Un écran est un arbre UXML instancié une fois dans le panneau,
    /// montré ou caché par le NavigateurEcrans. Il lit ses données dans DonneesUI et ne connaît pas le jeu.
    public abstract class Ecran
    {
        public VisualElement Racine { get; private set; }
        protected NavigateurEcrans Navigateur { get; private set; }
        VisualElement m_DernierFocus;

        /// Carte d'actions active quand l'écran est au sommet : UI (menus) ou Gameplay (HUD).
        public virtual bool CarteUI => true;

        /// Opaque : cache ce qui est dessous (menu principal, HUD, score). Sinon superposé (pause, options, crédits).
        public virtual bool Opaque => false;

        public void Initialiser(NavigateurEcrans navigateur, VisualElement racine)
        {
            Navigateur = navigateur;
            Racine = racine;
            Racine.AddToClassList("dl-ecran");
            // Plein écran, transparent aux clics (seuls les éléments visibles de l'écran les prennent).
            Racine.pickingMode = PickingMode.Ignore;
            Racine.style.position = Position.Absolute;
            Racine.style.left = Racine.style.top = Racine.style.right = Racine.style.bottom = 0;
            Racine.RegisterCallback<FocusInEvent>(e => m_DernierFocus = e.target as VisualElement);
            Construire();
        }

        /// Recherche des éléments, abonnements aux boutons.
        protected abstract void Construire();

        /// Premier élément à recevoir le focus (manette) quand l'écran s'ouvre.
        protected virtual VisualElement PremierFocus => null;

        /// Élément à qui rendre le focus : le dernier focus de l'écran s'il est encore visible, sinon le premier.
        public VisualElement FocusARendre
        {
            get
            {
                if (m_DernierFocus != null && m_DernierFocus.panel != null && m_DernierFocus.enabledInHierarchy
                    && m_DernierFocus.resolvedStyle.display != DisplayStyle.None)
                    return m_DernierFocus;
                return PremierFocus;
            }
        }

        public virtual void Montrer() => Racine.style.display = DisplayStyle.Flex;
        public virtual void Cacher() => Racine.style.display = DisplayStyle.None;

        /// Appelé quand l'écran arrive au sommet de la pile (ouverture, ou retour depuis un écran superposé).
        public virtual void AuSommet() { }

        /// Chaque image tant que l'écran est visible.
        public virtual void MiseAJour(float dt) { }

        /// UI/Cancel (B, Rond, Échap). Renvoie vrai si l'écran l'a traité ; sinon le navigateur ferme l'écran.
        public virtual bool Retour() => false;

        public virtual void OngletPrecedent() { }
        public virtual void OngletSuivant() { }
        public virtual void Reinitialiser() { }
    }
}
