using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Menu principal (0.1) : Solo, Options, Crédits, Quitter.
    public class EcranMenuPrincipal : Ecran
    {
        public override bool Opaque => true;
        Button m_Solo;

        protected override void Construire()
        {
            m_Solo = Racine.Q<Button>("menu-solo");
            m_Solo.clicked += () => DonneesUI.Commandes?.LancerSolo();
            Racine.Q<Button>("menu-options").clicked += () => Navigateur.Ouvrir(Navigateur.Options);
            Racine.Q<Button>("menu-credits").clicked += () => Navigateur.Ouvrir(Navigateur.Credits);
            Racine.Q<Button>("menu-quitter").clicked += () => DonneesUI.Commandes?.QuitterJeu();
        }

        protected override VisualElement PremierFocus => m_Solo;

        /// Retour au menu principal : rien à fermer.
        public override bool Retour() => true;
    }

    /// Pause : la partie continue. Reprendre, Options, Quitter la partie, Quitter le jeu.
    public class EcranPause : Ecran
    {
        Button m_Reprendre;

        protected override void Construire()
        {
            m_Reprendre = Racine.Q<Button>("pause-reprendre");
            m_Reprendre.clicked += () => Navigateur.Fermer();
            Racine.Q<Button>("pause-options").clicked += () => Navigateur.Ouvrir(Navigateur.Options);
            Racine.Q<Button>("pause-quitter-partie").clicked += () => DonneesUI.Commandes?.QuitterPartie();
            Racine.Q<Button>("pause-quitter-jeu").clicked += () => DonneesUI.Commandes?.QuitterJeu();
        }

        protected override VisualElement PremierFocus => m_Reprendre;
    }

    /// Crédits (Wiki/pages/credits.md).
    public class EcranCredits : Ecran
    {
        Button m_Retour;

        protected override void Construire()
        {
            m_Retour = Racine.Q<Button>("credits-retour");
            m_Retour.clicked += () => Navigateur.Fermer();
        }

        protected override VisualElement PremierFocus => m_Retour;
    }

    /// Options (0.1) : onglet Jeu (taille de l'interface) et onglet Commandes (table en lecture seule).
    public class EcranOptions : Ecran
    {
        static readonly (string action, string libelle)[] s_Commandes =
        {
            ("Gameplay/Move", "Se déplacer"),
            ("Gameplay/Look", "Caméra"),
            ("Gameplay/Jump", "Sauter"),
            ("Gameplay/Dodge", "Esquive, roulade"),
            ("Gameplay/Interact", "Interagir, parler"),
            ("Gameplay/CharacterMenu", "Menu du personnage"),
            ("Gameplay/AttackPrimary", "Attaque principale"),
            ("Gameplay/AttackSecondary", "Garde, parade, visée"),
            ("Gameplay/Skill1", "Compétence 1"),
            ("Gameplay/Skill2", "Compétence 2"),
            ("Gameplay/Skill3", "Compétence 3"),
            ("Gameplay/Ultimate", "Ultime"),
            ("Gameplay/Sprint", "Sprinter"),
            ("Gameplay/Crouch", "S’accroupir"),
            ("Gameplay/DrinkPotion", "Boire une potion"),
            ("Gameplay/Ready", "Se déclarer prêt"),
            ("Gameplay/Pause", "Pause"),
        };

        readonly List<Button> m_Onglets = new List<Button>();
        readonly List<Button> m_Tailles = new List<Button>();
        readonly List<InputPrompt> m_InvitesManette = new List<InputPrompt>();
        VisualElement m_PageJeu, m_PageCommandes;
        Label m_EnteteManette;
        int m_Onglet;

        protected override void Construire()
        {
            m_PageJeu = Racine.Q("options-jeu");
            m_PageCommandes = Racine.Q("options-commandes");
            m_EnteteManette = Racine.Q<Label>("options-entete-manette");
            Racine.Query<Button>(className: "dl-tab").ForEach(b => m_Onglets.Add(b));
            for (var i = 0; i < m_Onglets.Count; i++)
            {
                var index = i;
                m_Onglets[i].clicked += () => Onglet(index);
            }
            for (var n = UIScale.MinLevel; n <= UIScale.MaxLevel; n++)
            {
                var b = Racine.Q<Button>("taille-" + n);
                var niveau = n;
                b.clicked += () => UIScale.Level = niveau;
                m_Tailles.Add(b);
            }
            UIScale.Changed += _ => MajTailles();
            MajTailles();
            Racine.Q<Button>("options-retour").clicked += () => Navigateur.Fermer();

            var table = Racine.Q("options-table");
            var lignes = new List<VisualElement>();
            foreach (var (action, libelle) in s_Commandes)
            {
                // Ligne focusable : la manette peut parcourir (et faire défiler) une table en lecture seule.
                var ligne = new VisualElement { name = "ligne-" + action.Replace('/', '-'), focusable = true, tabIndex = 0 };
                ligne.AddToClassList("options-ligne");
                var l = new Label(libelle);
                l.AddToClassList("options-ligne__libelle");
                ligne.Add(l);
                var clavier = new InputPrompt(action) { fixedFamily = "KeyboardMouse" };
                clavier.AddToClassList("options-ligne__invite");
                ligne.Add(clavier);
                var manette = new InputPrompt(action) { fixedFamily = "Xbox" };
                manette.AddToClassList("options-ligne__invite");
                ligne.Add(manette);
                m_InvitesManette.Add(manette);
                table.Add(ligne);
                lignes.Add(ligne);
            }
            UINavigation.ChainerVerticalement(lignes);
            InputDeviceWatcher.Changed += _ => MajManette();
            MajManette();
            Onglet(0);
        }

        /// La colonne manette suit la dernière manette utilisée (Xbox par défaut au clavier).
        void MajManette()
        {
            var famille = InputDeviceWatcher.Current == InputFamily.PlayStation ? InputFamily.PlayStation : InputFamily.Xbox;
            foreach (var invite in m_InvitesManette) invite.fixedFamily = famille.ToString();
            m_EnteteManette.text = famille == InputFamily.PlayStation ? "MANETTE PLAYSTATION" : "MANETTE XBOX";
        }

        void MajTailles()
        {
            for (var i = 0; i < m_Tailles.Count; i++)
                m_Tailles[i].EnableInClassList("dl-button--selected", i + UIScale.MinLevel == UIScale.Level);
        }

        void Onglet(int index)
        {
            m_Onglet = Mathf.Clamp(index, 0, m_Onglets.Count - 1);
            for (var i = 0; i < m_Onglets.Count; i++) m_Onglets[i].EnableInClassList("dl-tab--selected", i == m_Onglet);
            m_PageJeu.style.display = m_Onglet == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            m_PageCommandes.style.display = m_Onglet == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override VisualElement PremierFocus => m_Onglet == 0 ? (VisualElement)m_Tailles[UIScale.Level - 1] : m_Onglets[1];

        public override void OngletPrecedent()
        {
            Onglet((m_Onglet + m_Onglets.Count - 1) % m_Onglets.Count);
            UINavigation.Focus(PremierFocus);
        }

        public override void OngletSuivant()
        {
            Onglet((m_Onglet + 1) % m_Onglets.Count);
            UINavigation.Focus(PremierFocus);
        }

        public override void Reinitialiser() => UIScale.Level = UIScale.DefaultLevel;
    }

    /// Écran de score (deroule.md) : résultat, durée, or ; une ligne par joueur, meilleur de chaque catégorie
    /// mis en avant ; Rejouer (vote prêt) ou Arrêter.
    public class EcranScore : Ecran
    {
        public override bool Opaque => true;

        Label m_Resultat, m_Titre, m_Duree, m_Or, m_Prets;
        VisualElement m_Lignes;
        Button m_Rejouer;
        IScoreFin m_Affiche;

        protected override void Construire()
        {
            m_Resultat = Racine.Q<Label>("score-resultat");
            m_Titre = Racine.Q<Label>("score-titre");
            m_Duree = Racine.Q<Label>("score-duree");
            m_Or = Racine.Q<Label>("score-or");
            m_Prets = Racine.Q<Label>("score-prets");
            m_Lignes = Racine.Q("score-lignes");
            m_Rejouer = Racine.Q<Button>("score-rejouer");
            m_Rejouer.clicked += () => DonneesUI.Commandes?.BasculerPret();
            Racine.Q<Button>("score-arreter").clicked += () => DonneesUI.Commandes?.QuitterPartie();
        }

        protected override VisualElement PremierFocus => m_Rejouer;

        /// Retour = Arrêter (bouton B / Échap, comme l'invite).
        public override bool Retour()
        {
            DonneesUI.Commandes?.QuitterPartie();
            return true;
        }

        public override void AuSommet() => Remplir(DonneesUI.Score);

        public override void MiseAJour(float dt)
        {
            var score = DonneesUI.Score;
            if (score == null) return;
            if (!ReferenceEquals(score, m_Affiche)) Remplir(score);
            m_Prets.text = "Prêts " + score.JoueursPrets + " / " + score.JoueursTotal;
            m_Rejouer.EnableInClassList("score-bouton--pret", score.EstPretLocal);
        }

        void Remplir(IScoreFin score)
        {
            m_Affiche = score;
            m_Lignes.Clear();
            if (score == null) return;
            var victoire = score.Resultat == ResultatPartie.Victoire;
            m_Resultat.text = victoire ? "VICTOIRE" : "DÉFAITE";
            m_Resultat.EnableInClassList("score-resultat--victoire", victoire);
            m_Titre.text = victoire ? "Nyxessa a survécu aux 12 nuits" : "Nyxessa est tombée à la nuit " + score.NuitAtteinte;
            var d = Mathf.Max(0, Mathf.RoundToInt(score.DureeSecondes));
            m_Duree.text = (d / 60) + ":" + (d % 60).ToString("00");
            m_Or.text = EcranHud.Milliers(score.OrTotal);

            // Meilleur de chaque catégorie (morts : le plus bas).
            var joueurs = score.Joueurs;
            int maxOr = int.MinValue, maxDeg = int.MinValue, maxTues = int.MinValue, minMorts = int.MaxValue;
            foreach (var j in joueurs)
            {
                maxOr = Mathf.Max(maxOr, j.OrRapporte);
                maxDeg = Mathf.Max(maxDeg, j.DegatsInfliges);
                maxTues = Mathf.Max(maxTues, j.EnnemisTues);
                minMorts = Mathf.Min(minMorts, j.Morts);
            }

            foreach (var j in joueurs)
            {
                var ligne = new VisualElement();
                ligne.AddToClassList("score-ligne");
                ligne.EnableInClassList("score-ligne--local", j.EstLocal);
                var qui = new VisualElement();
                qui.AddToClassList("score-joueur");
                var pastille = new Label(string.IsNullOrEmpty(j.Classe) ? "?" : j.Classe.Substring(0, 1));
                pastille.AddToClassList("score-joueur__pastille");
                pastille.style.backgroundColor = j.TeinteClasse;
                var noms = new VisualElement();
                var nom = new Label(j.Nom);
                nom.AddToClassList("score-joueur__nom");
                var classe = new Label(j.Classe);
                classe.AddToClassList("score-joueur__classe");
                noms.Add(nom);
                noms.Add(classe);
                qui.Add(pastille);
                qui.Add(noms);
                ligne.Add(qui);
                ligne.Add(Cellule(EcranHud.Milliers(j.OrRapporte), j.OrRapporte == maxOr));
                ligne.Add(Cellule(EcranHud.Milliers(j.DegatsInfliges), j.DegatsInfliges == maxDeg));
                ligne.Add(Cellule(EcranHud.Milliers(j.EnnemisTues), j.EnnemisTues == maxTues));
                ligne.Add(Cellule(j.Morts.ToString(), j.Morts == minMorts));
                m_Lignes.Add(ligne);
            }
        }

        static VisualElement Cellule(string valeur, bool meilleur)
        {
            var cellule = new VisualElement();
            cellule.AddToClassList("score-cellule");
            var pastille = new VisualElement();
            pastille.AddToClassList("score-valeur");
            pastille.EnableInClassList("score-valeur--meilleur", meilleur);
            var texte = new Label(valeur);
            texte.AddToClassList("score-valeur__texte");
            pastille.Add(texte);
            if (meilleur)
            {
                var couronne = new Couronne { tooltip = "Meilleur" };
                couronne.AddToClassList("score-valeur__couronne");
                pastille.Add(couronne);
            }
            cellule.Add(pastille);
            return cellule;
        }
    }
}
