using System.Collections.Generic;
using Deathless.Audio;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Menu principal (0.1) : Solo (ouvre le choix de classe), Options, Crédits, Quitter. La carte en bas à droite
    /// présente la dernière classe jouée (ClassesJouables.Derniere).
    public class EcranMenuPrincipal : Ecran
    {
        public override bool Opaque => true;
        Button m_Solo;
        VisualElement m_Pastille;
        Label m_Initiale, m_Nom, m_Arme, m_Actions;

        protected override void Construire()
        {
            m_Solo = Racine.Q<Button>("menu-solo");
            m_Solo.clicked += () => Navigateur.Ouvrir(Navigateur.ChoixClasse);
            m_Pastille = Racine.Q("menu-classe-pastille");
            m_Initiale = Racine.Q<Label>("menu-classe-initiale");
            m_Nom = Racine.Q<Label>("menu-classe-nom");
            m_Arme = Racine.Q<Label>("menu-classe-arme");
            m_Actions = Racine.Q<Label>("menu-classe-actions");
            Racine.Q<Button>("menu-options").clicked += () => Navigateur.Ouvrir(Navigateur.Options);
            Racine.Q<Button>("menu-credits").clicked += () => Navigateur.Ouvrir(Navigateur.Credits);
            Racine.Q<Button>("menu-quitter").clicked += () => DonneesUI.Commandes?.QuitterJeu();
        }

        protected override VisualElement PremierFocus => m_Solo;

        /// Retour au menu principal : rien à fermer.
        public override bool Retour() => true;

        public override void AuSommet()
        {
            var c = ClassesJouables.Derniere;
            if (c == null || m_Nom == null) return;
            m_Pastille.style.backgroundColor = c.Teinte;
            m_Initiale.text = c.Nom.Substring(0, 1);
            m_Nom.text = c.Nom;
            m_Arme.text = c.Arme;
            m_Actions.text = EcranChoixClasse.ResumeActions(c);
        }
    }

    /// Choix de classe (Solo) : les cinq classes, leur arme et leurs actions (Wiki : classes.md, commandes.md).
    /// La dernière classe jouée est présélectionnée ; Valider lance la partie, Retour revient au menu.
    public class EcranChoixClasse : Ecran
    {
        public override bool Opaque => true;

        readonly List<Button> m_Boutons = new List<Button>();
        readonly List<IClasseJouable> m_Classes = new List<IClasseJouable>();
        readonly List<(VisualElement ligne, Label nom)> m_LignesActions = new List<(VisualElement, Label)>();
        VisualElement m_Liste, m_Portrait, m_Actions;
        Label m_Initiale, m_Nom, m_Role, m_Arme, m_Description, m_Jauge;
        IReadOnlyList<IClasseJouable> m_Source;
        IClasseJouable m_Affichee;

        static readonly string[] s_Emplacements =
            { "Gameplay/AttackPrimary", "Gameplay/AttackSecondary", "Gameplay/Skill1", "Gameplay/Skill2", "Gameplay/Skill3" };

        protected override void Construire()
        {
            m_Liste = Racine.Q("choix-liste");
            m_Portrait = Racine.Q("fiche-portrait");
            m_Initiale = Racine.Q<Label>("fiche-initiale");
            m_Nom = Racine.Q<Label>("fiche-nom");
            m_Role = Racine.Q<Label>("fiche-role");
            m_Arme = Racine.Q<Label>("fiche-arme");
            m_Description = Racine.Q<Label>("fiche-description");
            m_Jauge = Racine.Q<Label>("fiche-jauge");
            m_Actions = Racine.Q("fiche-actions");
            // Cinq lignes fixes (attaque, attaque secondaire, compétences 1 à 3) : l'invite suit l'appareil.
            foreach (var action in s_Emplacements)
            {
                var ligne = new VisualElement();
                ligne.AddToClassList("choix-action");
                var invite = new InputPrompt(action);
                invite.AddToClassList("choix-action__invite");
                var nom = new Label();
                nom.AddToClassList("choix-action__nom");
                ligne.Add(invite);
                ligne.Add(nom);
                m_Actions.Add(ligne);
                m_LignesActions.Add((ligne, nom));
            }
        }

        /// Reconstruit la liste si la source des classes a changé (le jeu peut fournir ses propres classes).
        void Remplir()
        {
            var source = ClassesJouables.Proposees;
            if (ReferenceEquals(source, m_Source)) { MajDerniere(); return; }
            m_Source = source;
            m_Liste.Clear();
            m_Boutons.Clear();
            m_Classes.Clear();
            foreach (var c in source)
            {
                var classe = c;
                var b = new Button { name = "classe-" + c.Id };
                b.AddToClassList("choix-classe");
                var pastille = new VisualElement();
                pastille.AddToClassList("choix-classe__pastille");
                pastille.style.backgroundColor = c.Teinte;
                var initiale = new Label(c.Nom.Substring(0, 1));
                initiale.AddToClassList("choix-classe__initiale");
                pastille.Add(initiale);
                var textes = new VisualElement();
                textes.AddToClassList("choix-classe__textes");
                var nom = new Label(c.Nom);
                nom.AddToClassList("choix-classe__nom");
                var role = new Label(c.Role);
                role.AddToClassList("choix-classe__role");
                textes.Add(nom);
                textes.Add(role);
                var derniere = new Label("Dernière") { name = "derniere" };
                derniere.AddToClassList("choix-classe__derniere");
                b.Add(pastille);
                b.Add(textes);
                b.Add(derniere);
                b.RegisterCallback<FocusInEvent>(_ => Afficher(classe));
                b.RegisterCallback<PointerEnterEvent>(_ => Afficher(classe));
                b.clicked += () => ClassesJouables.Lancer(classe.Id);
                SonDeClic(b);
                m_Liste.Add(b);
                m_Boutons.Add(b);
                m_Classes.Add(c);
            }
            UINavigation.ChainerVerticalement(new List<VisualElement>(m_Boutons));
            MajDerniere();
        }

        void MajDerniere()
        {
            var id = ClassesJouables.DerniereJouee;
            for (var i = 0; i < m_Boutons.Count; i++)
                m_Boutons[i].Q("derniere").style.display = m_Classes[i].Id == id ? DisplayStyle.Flex : DisplayStyle.None;
        }

        int IndexDerniere()
        {
            var id = ClassesJouables.DerniereJouee;
            for (var i = 0; i < m_Classes.Count; i++) if (m_Classes[i].Id == id) return i;
            return 0;
        }

        public override void AuSommet()
        {
            Remplir();
            if (m_Classes.Count > 0) Afficher(m_Classes[IndexDerniere()]);
        }

        protected override VisualElement PremierFocus => m_Boutons.Count > 0 ? m_Boutons[IndexDerniere()] : null;

        /// Fiche de la classe : nom, rôle, arme, description, jauge, actions (emplacement vide grisé).
        public void Afficher(IClasseJouable c)
        {
            if (c == null) return;
            m_Affichee = c;
            m_Portrait.style.backgroundColor = c.Teinte;
            m_Initiale.text = c.Nom.Substring(0, 1);
            m_Nom.text = c.Nom;
            m_Role.text = c.Role;
            m_Arme.text = c.Arme;
            m_Description.text = c.Description;
            m_Jauge.text = c.Jauge == JaugeClasse.Mana ? "Jauge de mana : les sorts en consomment."
                : c.Jauge == JaugeClasse.Rage ? "Jauge de rage : elle monte quand il frappe." : "";
            m_Jauge.style.display = c.Jauge == JaugeClasse.Aucune ? DisplayStyle.None : DisplayStyle.Flex;
            for (var i = 0; i < m_LignesActions.Count; i++)
            {
                var nom = i < c.Actions.Count ? c.Actions[i].Nom : null;
                var vide = string.IsNullOrEmpty(nom);
                m_LignesActions[i].nom.text = vide ? "Vide pour l’instant" : nom;
                m_LignesActions[i].ligne.EnableInClassList("choix-action--vide", vide);
            }
        }

        /// Classe affichée dans la fiche (tests).
        public IClasseJouable Affichee => m_Affichee;

        /// « Frappe à l’épée, garde et parade, charge bélier, soin sur soi. » (carte du menu principal).
        public static string ResumeActions(IClasseJouable c)
        {
            var noms = new List<string>();
            foreach (var a in c.Actions)
                if (!string.IsNullOrEmpty(a.Nom)) noms.Add(noms.Count == 0 ? a.Nom : char.ToLowerInvariant(a.Nom[0]) + a.Nom.Substring(1));
            return string.Join(", ", noms) + ".";
        }
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

    /// Options (0.1) : onglet Jeu (taille de l'interface), onglet Commandes (table en lecture seule) et onglet Audio
    /// (volumes principal, musique, effets, interface : VolumesAudio).
    public class EcranOptions : Ecran
    {
        static readonly (CanalAudio canal, string libelle)[] s_Volumes =
        {
            (CanalAudio.Principal, "Volume principal"),
            (CanalAudio.Musique, "Musique"),
            (CanalAudio.Effets, "Effets spéciaux"),
            (CanalAudio.Interface, "Interface"),
        };

        /// Pas d'un appui gauche / droite sur un volume (%).
        public const int PasVolume = 5;

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
            ("Gameplay/Sprint", "Sprinter"),
            ("Gameplay/DrinkPotion", "Boire une potion"),
            ("Gameplay/Ready", "Se déclarer prêt"),
            ("Gameplay/Pause", "Pause"),
        };

        readonly List<Button> m_Onglets = new List<Button>();
        readonly List<Button> m_Tailles = new List<Button>();
        readonly List<InputPrompt> m_InvitesManette = new List<InputPrompt>();
        readonly List<Slider> m_Curseurs = new List<Slider>();
        readonly List<Label> m_Pourcentages = new List<Label>();
        VisualElement m_PageJeu, m_PageCommandes, m_PageAudio;
        float m_DernierApercu;
        Label m_EnteteManette;
        int m_Onglet;

        protected override void Construire()
        {
            m_PageJeu = Racine.Q("options-jeu");
            m_PageCommandes = Racine.Q("options-commandes");
            m_PageAudio = Racine.Q("options-audio");
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
            ConstruireVolumes();
            Onglet(0);
        }

        void ConstruireVolumes()
        {
            var conteneur = Racine.Q("options-volumes");
            var lignes = new List<VisualElement>();
            foreach (var (canal, libelle) in s_Volumes)
            {
                var c = canal;
                var curseur = new Slider(libelle, 0f, 100f) { name = "volume-" + canal.ToString().ToLowerInvariant(), pageSize = PasVolume };
                curseur.AddToClassList("dl-field");
                curseur.AddToClassList("options-volume");
                curseur.fill = true;
                var pc = new Label();
                pc.AddToClassList("options-volume__pc");
                curseur.Add(pc);
                curseur.SetValueWithoutNotify(Mathf.Round(VolumesAudio.Volume(canal) * 100f));
                curseur.RegisterValueChangedCallback(e => ChangerVolume(c, e.newValue));
                // Manette et flèches : gauche / droite changent le volume par pas de 5 % (la navigation ne quitte pas la ligne).
                curseur.RegisterCallback<NavigationMoveEvent>(e =>
                {
                    if (e.direction != NavigationMoveEvent.Direction.Left && e.direction != NavigationMoveEvent.Direction.Right) return;
                    var pas = e.direction == NavigationMoveEvent.Direction.Right ? PasVolume : -PasVolume;
                    curseur.value = Mathf.Clamp(Mathf.Round(curseur.value / PasVolume) * PasVolume + pas, 0f, 100f);
                    e.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);
                conteneur.Add(curseur);
                m_Curseurs.Add(curseur);
                m_Pourcentages.Add(pc);
                lignes.Add(curseur);
            }
            UINavigation.ChainerVerticalement(lignes);
            VolumesAudio.Changed += (_, __) => MajVolumes();
            MajVolumes();
        }

        void ChangerVolume(CanalAudio canal, float pourcent)
        {
            VolumesAudio.DefinirVolume(canal, Mathf.Round(pourcent) / 100f);
            MajVolumes();
            // L'effet s'entend tout de suite : un son court de la catégorie (au plus un tous les 0,12 s).
            if ((canal == CanalAudio.Effets || canal == CanalAudio.Interface || canal == CanalAudio.Principal)
                && Time.unscaledTime - m_DernierApercu > 0.12f)
            {
                m_DernierApercu = Time.unscaledTime;
                VolumesAudio.JouerApercu(canal == CanalAudio.Principal ? CanalAudio.Effets : canal);
            }
        }

        void MajVolumes()
        {
            for (var i = 0; i < m_Curseurs.Count; i++)
            {
                var v = Mathf.Round(VolumesAudio.Volume(s_Volumes[i].canal) * 100f);
                if (!Mathf.Approximately(m_Curseurs[i].value, v)) m_Curseurs[i].SetValueWithoutNotify(v);
                m_Pourcentages[i].text = v.ToString("0") + " %";
            }
        }

        public override void Cacher()
        {
            base.Cacher();
            VolumesAudio.Enregistrer();
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
            m_PageAudio.style.display = m_Onglet == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override VisualElement PremierFocus =>
            m_Onglet == 0 ? m_Tailles[UIScale.Level - 1] : m_Onglet == 2 ? (VisualElement)m_Curseurs[0] : m_Onglets[1];

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

        /// Réinitialiser (Y) : l'onglet affiché seulement (Jeu : taille ×2 ; Audio : volumes par défaut).
        public override void Reinitialiser()
        {
            if (m_Onglet == 2) VolumesAudio.Reinitialiser();
            else UIScale.Level = UIScale.DefaultLevel;
        }
    }

    /// Écran de score (deroule.md) : résultat, durée, or ; une ligne par joueur, meilleur de chaque catégorie
    /// mis en avant ; Rejouer (vote prêt) ou Arrêter.
    public class EcranScore : Ecran
    {
        public override bool Opaque => true;

        /// Colonnes, dans l'ordre de l'en-tête de Score.uxml (deroule.md).
        static readonly (System.Func<ILigneScore, int> valeur, bool plusBas)[] s_Categories =
        {
            (j => j.OrRapporte, false),
            (j => j.DegatsInfliges, false),
            (j => j.EnnemisTues, false),
            (j => j.Morts, true),
            (j => j.CoupsCritiques, false),
            (j => j.DegatsEvitesNyxessa, false),
            (j => j.SoinsProdigues, false),
        };

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

            // Meilleur de chaque catégorie (morts : le plus bas, les autres : le plus élevé).
            var joueurs = score.Joueurs;
            var meilleurs = new int[s_Categories.Length];
            for (var k = 0; k < s_Categories.Length; k++)
            {
                meilleurs[k] = s_Categories[k].plusBas ? int.MaxValue : int.MinValue;
                foreach (var j in joueurs)
                {
                    var v = s_Categories[k].valeur(j);
                    meilleurs[k] = s_Categories[k].plusBas ? Mathf.Min(meilleurs[k], v) : Mathf.Max(meilleurs[k], v);
                }
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
                for (var k = 0; k < s_Categories.Length; k++)
                {
                    var v = s_Categories[k].valeur(j);
                    ligne.Add(Cellule(EcranHud.Milliers(v), v == meilleurs[k]));
                }
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
