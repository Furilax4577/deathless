using System;
using System.Collections.Generic;
using Deathless.Audio;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Clavier virtuel (manette, souris) : une grille de touches (Button) ; le haut / bas / gauche / droite de la manette
    /// s'y déplace, A tape la touche. Le clavier physique tape directement dans le champ.
    public class ClavierVirtuel : VisualElement
    {
        public const string Espace = "Espace", Effacer = "Effacer", Valider = "Valider", Maj = "Maj";

        public event Action<string> Touche;
        public event Action ToucheEffacer, ToucheValider;

        readonly List<Button> m_Lettres = new List<Button>();
        readonly List<Button> m_Toutes = new List<Button>();
        bool m_Majuscules = true;

        /// `lignes` : une chaîne par rangée, un caractère par touche. `speciales` : dernière rangée (Maj, Espace, Effacer,
        /// Valider), dans l'ordre voulu.
        public ClavierVirtuel(IEnumerable<string> lignes, IEnumerable<string> speciales)
        {
            AddToClassList("clavier");
            foreach (var ligne in lignes)
            {
                var rangee = new VisualElement();
                rangee.AddToClassList("clavier__rangee");
                foreach (var c in ligne)
                {
                    var car = c.ToString();
                    var b = new Button { text = car };
                    b.AddToClassList("clavier__touche");
                    b.clicked += () => { VolumesAudio.JouerInterface(SonInterface.Clic, 0.6f); Touche?.Invoke(b.text); };
                    rangee.Add(b);
                    m_Toutes.Add(b);
                    if (char.IsLetter(c)) m_Lettres.Add(b);
                }
                Add(rangee);
            }
            var bas = new VisualElement();
            bas.AddToClassList("clavier__rangee");
            foreach (var s in speciales)
            {
                var nom = s;
                var b = new Button { text = nom == Espace ? "Espace" : nom == Effacer ? "Effacer" : nom == Maj ? "Maj" : "Valider" };
                b.AddToClassList("clavier__touche");
                b.AddToClassList("clavier__touche--large");
                if (nom == Valider) b.AddToClassList("clavier__touche--valider");
                b.clicked += () =>
                {
                    VolumesAudio.JouerInterface(SonInterface.Clic, 0.6f);
                    if (nom == Espace) Touche?.Invoke(" ");
                    else if (nom == Effacer) ToucheEffacer?.Invoke();
                    else if (nom == Maj) Majuscules = !Majuscules;
                    else ToucheValider?.Invoke();
                };
                bas.Add(b);
                m_Toutes.Add(b);
            }
            Add(bas);
        }

        public bool Majuscules
        {
            get => m_Majuscules;
            set
            {
                m_Majuscules = value;
                foreach (var b in m_Lettres) b.text = value ? b.text.ToUpperInvariant() : b.text.ToLowerInvariant();
            }
        }

        public Button PremiereTouche => m_Toutes.Count > 0 ? m_Toutes[0] : null;
    }

    /// Saisie d'un texte : pseudo (premier lancement, options) ou code de salon. Champ texte pour le clavier physique,
    /// clavier virtuel pour la manette et la souris. A : touche, B : effacer (saisie obligatoire) ou retour, Y : valider.
    public class EcranSaisie : Ecran
    {
        public override bool Opaque => true;

        static readonly string[] s_LignesPseudo = { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZÉÈÇ-", "0123456789" };
        static readonly string[] s_SpecialesPseudo = { ClavierVirtuel.Maj, ClavierVirtuel.Espace, ClavierVirtuel.Effacer, ClavierVirtuel.Valider };
        static readonly string[] s_LignesCode = { "ABCDEFGHJK", "LMNPQRSTUV", "WXYZ", "0123456789" };
        static readonly string[] s_LignesAdresse = { "123", "456", "789", ".0:" };
        static readonly string[] s_SpecialesCode = { ClavierVirtuel.Effacer, ClavierVirtuel.Valider };
        public const int LongueurCode = 6;

        TextField m_Champ;
        Label m_Titre, m_Aide, m_Erreur, m_Regle;
        VisualElement m_ConteneurClavier;
        ClavierVirtuel m_Clavier;
        InputPrompt m_InviteRetour;
        Func<string, string> m_Verifier;
        Action<string> m_SurValide;
        bool m_Obligatoire, m_AutoMaj;

        /// Vrai pendant la saisie obligatoire du premier lancement (pseudo).
        public bool Obligatoire => m_Obligatoire;

        protected override void Construire()
        {
            m_Champ = Racine.Q<TextField>("saisie-champ");
            m_Titre = Racine.Q<Label>("saisie-titre");
            m_Aide = Racine.Q<Label>("saisie-aide");
            m_Erreur = Racine.Q<Label>("saisie-erreur");
            m_Regle = Racine.Q<Label>("saisie-regle");
            m_ConteneurClavier = Racine.Q("saisie-clavier");
            m_InviteRetour = Racine.Q<InputPrompt>("saisie-invite-retour");
            m_Champ.RegisterValueChangedCallback(_ => m_Erreur.text = "");
            m_Champ.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Valider(); e.StopPropagation(); }
            }, TrickleDown.TrickleDown);
            UINavigation.ReleaseTextFields(Racine);
        }

        /// Pseudo : au premier lancement (obligatoire : pas de retour) ou depuis les options.
        public void ConfigurerPseudo(bool premierLancement, Action surValide)
        {
            Configurer(premierLancement ? "Choisis ton pseudo" : "Changer de pseudo",
                "Il sera affiché aux autres joueurs." + (premierLancement ? " Tu pourras le changer dans les options." : ""),
                ProfilJoueur.LongueurMin + " à " + ProfilJoueur.LongueurMax + " caractères : lettres, chiffres, tirets et espaces.",
                premierLancement ? "" : DonneesUI.Profil.Pseudo, ProfilJoueur.LongueurMax + 4,
                v => ProfilJoueur.Valider(v, out var raison) ? null : raison,
                v => { ProfilJoueur.Definir(v); surValide?.Invoke(); },
                premierLancement, true);
        }

        /// Code court du salon (Unity Lobby, 6 caractères).
        public void ConfigurerCode(string valeur, Action<string> surValide)
        {
            Configurer("Code du salon", "Le code que l’hôte voit dans son salon.",
                LongueurCode + " lettres et chiffres.", valeur, LongueurCode,
                v => v == null || v.Trim().Length != LongueurCode ? "Le code a " + LongueurCode + " caractères." : null,
                v => surValide?.Invoke(v.Trim().ToUpperInvariant()), false, false, s_LignesCode);
        }

        /// Secours : adresse IP de l'hôte (et port facultatif).
        public void ConfigurerAdresse(string valeur, Action<string> surValide)
        {
            Configurer("Rejoindre par adresse IP", "En secours, si le code ne fonctionne pas : l’adresse IP de l’hôte.",
                "Chiffres, points, et « :port » facultatif (192.168.1.20:7777).", valeur, 21,
                v => System.Text.RegularExpressions.Regex.IsMatch((v ?? "").Trim(), @"^\d{1,3}(\.\d{1,3}){3}(:\d{1,5})?$") ? null : "Adresse IP attendue, par exemple 192.168.1.20.",
                v => surValide?.Invoke(v.Trim()), false, false, s_LignesAdresse);
        }

        void Configurer(string titre, string aide, string regle, string valeur, int longueurMax,
            Func<string, string> verifier, Action<string> surValide, bool obligatoire, bool pseudo, string[] lignes = null)
        {
            m_Titre.text = titre;
            m_Aide.text = aide;
            m_Regle.text = regle;
            m_Erreur.text = "";
            m_Champ.maxLength = longueurMax;
            m_Champ.SetValueWithoutNotify(valeur ?? "");
            m_Verifier = verifier;
            m_SurValide = surValide;
            m_Obligatoire = obligatoire;
            m_AutoMaj = pseudo;
            m_InviteRetour.text = obligatoire ? "Effacer" : "Retour";
            m_ConteneurClavier.Clear();
            m_Clavier = pseudo ? new ClavierVirtuel(s_LignesPseudo, s_SpecialesPseudo) : new ClavierVirtuel(lignes ?? s_LignesCode, s_SpecialesCode);
            m_Clavier.Touche += Taper;
            m_Clavier.ToucheEffacer += EffacerDernier;
            m_Clavier.ToucheValider += Valider;
            m_Clavier.Majuscules = !pseudo || string.IsNullOrEmpty(m_Champ.value);
            m_ConteneurClavier.Add(m_Clavier);
        }

        void Taper(string s)
        {
            if (m_Champ.value.Length >= m_Champ.maxLength) return;
            m_Champ.value += m_AutoMaj ? s : s.ToUpperInvariant();
            // Pseudo : majuscule au début, puis minuscules (comme un clavier de téléphone).
            if (m_AutoMaj && char.IsLetter(s, 0) && m_Clavier.Majuscules) m_Clavier.Majuscules = false;
        }

        void EffacerDernier()
        {
            var v = m_Champ.value;
            if (v.Length > 0) m_Champ.value = v.Substring(0, v.Length - 1);
            if (m_AutoMaj && m_Champ.value.Length == 0) m_Clavier.Majuscules = true;
        }

        public void Valider()
        {
            var erreur = m_Verifier != null ? m_Verifier(m_Champ.value) : null;
            if (erreur != null)
            {
                m_Erreur.text = erreur;
                VolumesAudio.JouerInterface(SonInterface.Refus, 0.7f);
                return;
            }
            VolumesAudio.JouerInterface(SonInterface.Clic);
            m_SurValide?.Invoke(m_Champ.value);
        }

        /// Texte saisi (tests).
        public string Texte { get => m_Champ.value; set => m_Champ.value = value; }

        protected override VisualElement PremierFocus =>
            InputDeviceWatcher.Current == InputFamily.KeyboardMouse || m_Clavier == null ? (VisualElement)m_Champ : m_Clavier.PremiereTouche;

        /// B / Échap : efface un caractère pendant la saisie obligatoire, sinon ferme l'écran.
        public override bool Retour()
        {
            if (!m_Obligatoire) return false;
            EffacerDernier();
            return true;
        }

        /// Y : valider.
        public override void Reinitialiser() => Valider();
    }

    /// Lobby multijoueur (Wiki : interface.md) : entrée (créer un salon, rejoindre par code ou adresse IP), puis salon
    /// de quatre emplacements (pseudo, emblème de la classe, prêt / pas prêt), compte à rebours quand tous sont prêts.
    /// Lit DonneesUI.Lobby (ILobby) ; aucune logique réseau ici.
    public class EcranLobby : Ecran
    {
        public override bool Opaque => true;

        sealed class Emplacement
        {
            public VisualElement racine, embleme;
            public Label pseudo, classe, etat, hote;
        }

        readonly List<Emplacement> m_Emplacements = new List<Emplacement>();
        VisualElement m_Entree, m_Salon;
        Label m_Message, m_CodeTexte, m_CodeTitre, m_CodeValeur, m_Compte, m_Copie, m_SousTitre;
        Button m_Creer, m_Code, m_Rejoindre, m_ParIp, m_Afficher, m_Copier, m_Classe, m_Pret, m_Lancer, m_Quitter;
        InputPrompt m_InviteRetour, m_InvitePret;
        string m_CodeSaisi = "";
        bool m_CodeVisible;
        float m_CopieJusqua;
        EtatLobby m_EtatAffiche = (EtatLobby)(-1);

        static ILobby L => DonneesUI.Lobby;
        bool DansSalon => L != null && (L.Etat == EtatLobby.Salon || L.Etat == EtatLobby.CompteARebours || L.Etat == EtatLobby.Connexion);

        protected override void Construire()
        {
            m_Entree = Racine.Q("lobby-entree");
            m_Salon = Racine.Q("lobby-salon");
            m_Message = Racine.Q<Label>("lobby-message");
            m_SousTitre = Racine.Q<Label>("lobby-sous-titre");
            m_CodeTexte = Racine.Q<Label>("lobby-code-texte");
            m_CodeTitre = Racine.Q<Label>("lobby-code-titre");
            m_CodeValeur = Racine.Q<Label>("lobby-code-valeur");
            m_Compte = Racine.Q<Label>("lobby-compte");
            m_Copie = Racine.Q<Label>("lobby-copie");
            m_Creer = Racine.Q<Button>("lobby-creer");
            m_Code = Racine.Q<Button>("lobby-code");
            m_Rejoindre = Racine.Q<Button>("lobby-rejoindre");
            m_ParIp = Racine.Q<Button>("lobby-par-ip");
            m_Afficher = Racine.Q<Button>("lobby-afficher");
            m_Copier = Racine.Q<Button>("lobby-copier");
            m_Classe = Racine.Q<Button>("lobby-classe");
            m_Pret = Racine.Q<Button>("lobby-pret");
            m_Lancer = Racine.Q<Button>("lobby-lancer");
            m_Quitter = Racine.Q<Button>("lobby-quitter");
            m_InviteRetour = Racine.Q<InputPrompt>("lobby-invite-retour");
            m_InvitePret = Racine.Q<InputPrompt>("lobby-invite-pret");

            m_Creer.clicked += () => L?.CreerSalon();
            m_Code.clicked += () =>
            {
                Navigateur.Saisie.ConfigurerCode(m_CodeSaisi, v => { m_CodeSaisi = v; Navigateur.Fermer(); MajCodeSaisi(); UINavigation.Focus(m_Rejoindre); });
                Navigateur.Ouvrir(Navigateur.Saisie);
            };
            m_Rejoindre.clicked += () => L?.Rejoindre(m_CodeSaisi);
            // Secours : adresse IP directe (lien secondaire).
            m_ParIp.clicked += () =>
            {
                Navigateur.Saisie.ConfigurerAdresse("", v => { Navigateur.Fermer(); L?.RejoindreParAdresse(v); });
                Navigateur.Ouvrir(Navigateur.Saisie);
            };
            m_Afficher.clicked += () => m_CodeVisible = !m_CodeVisible;
            m_Copier.clicked += () =>
            {
                if (L == null) return;
                GUIUtility.systemCopyBuffer = L.CodeSalon;
                m_CopieJusqua = Time.unscaledTime + 2f;
            };
            m_Classe.clicked += () =>
            {
                var local = Local();
                Navigateur.ChoixClasse.OuvrirPourLobby(id =>
                {
                    // Classe unique : si un autre joueur l'a prise entre-temps, le choix reste ouvert.
                    if (L != null && L.ChoisirClasse(id)) Navigateur.Fermer();
                    else VolumesAudio.JouerInterface(SonInterface.Refus, 0.55f);
                }, local?.ClasseId, id => LobbyOutils.PrisePar(L, id));
            };
            m_Pret.clicked += () => L?.BasculerPret();
            m_Lancer.clicked += () => L?.LancerMaintenant();
            m_Quitter.clicked += () => L?.Quitter();

            var conteneur = Racine.Q("lobby-emplacements");
            for (var i = 0; i < 4; i++)
            {
                var e = new Emplacement { racine = new VisualElement() };
                e.racine.AddToClassList("lobby-place");
                e.embleme = IconesUI.Creer(IconesUI.RepliClasse, "lobby-place__embleme");
                e.pseudo = new Label { pickingMode = PickingMode.Ignore };
                e.pseudo.AddToClassList("lobby-place__pseudo");
                e.classe = new Label { pickingMode = PickingMode.Ignore };
                e.classe.AddToClassList("lobby-place__classe");
                e.etat = new Label { pickingMode = PickingMode.Ignore };
                e.etat.AddToClassList("lobby-place__etat");
                e.hote = new Label("Hôte") { pickingMode = PickingMode.Ignore };
                e.hote.AddToClassList("lobby-place__hote");
                e.racine.Add(e.hote);
                e.racine.Add(e.embleme);
                e.racine.Add(e.pseudo);
                e.racine.Add(e.classe);
                e.racine.Add(e.etat);
                conteneur.Add(e.racine);
                m_Emplacements.Add(e);
            }
            MajCodeSaisi();
        }

        IJoueurLobby Local()
        {
            if (L == null) return null;
            foreach (var j in L.Joueurs) if (j.EstLocal) return j;
            return null;
        }

        void MajCodeSaisi()
        {
            var vide = string.IsNullOrEmpty(m_CodeSaisi);
            m_CodeTexte.text = vide ? "Saisir le code (6 caractères)" : m_CodeSaisi;
            m_CodeTexte.EnableInClassList("lobby__champ-texte--vide", vide);
        }

        protected override VisualElement PremierFocus => DansSalon ? m_Classe : m_Creer;

        public override void AuSommet()
        {
            m_EtatAffiche = (EtatLobby)(-1);
            MiseAJour(0f);
        }

        public override void MiseAJour(float dt)
        {
            var l = L;
            if (l == null) return;
            var etat = l.Etat;
            var salon = DansSalon;
            if (etat != m_EtatAffiche)
            {
                var avant = m_EtatAffiche;
                m_EtatAffiche = etat;
                m_Entree.style.display = salon ? DisplayStyle.None : DisplayStyle.Flex;
                m_Salon.style.display = salon ? DisplayStyle.Flex : DisplayStyle.None;
                m_InvitePret.style.display = salon ? DisplayStyle.Flex : DisplayStyle.None;
                m_InviteRetour.text = salon ? "Quitter le salon" : "Retour";
                // Entrée → salon ou salon → entrée : le focus suit (manette).
                var etaitSalon = avant == EtatLobby.Salon || avant == EtatLobby.CompteARebours || avant == EtatLobby.Connexion;
                if (Navigateur.Sommet == this && salon != etaitSalon)
                    Racine.schedule.Execute(() => { if (Navigateur.Sommet == this) UINavigation.Focus(PremierFocus); }).StartingIn(30);
            }
            // Place réservée en permanence (une ligne, même vide : texte de remplacement transparent, à la hauteur exacte de
            // la police à toutes les tailles d'interface) : un message qui apparaît ou disparaît (erreur, hôte parti, services
            // indisponibles) ne décale rien ; trop long, il est coupé par des points de suspension.
            var vide = string.IsNullOrEmpty(l.Message);
            m_Message.text = vide ? "Message" : l.Message;
            m_Message.EnableInClassList("lobby__message--vide", vide);
            m_Message.EnableInClassList("lobby__message--erreur", etat == EtatLobby.Erreur);
            m_SousTitre.text = etat == EtatLobby.Connexion ? "Connexion…"
                : salon ? (l.EstHote ? "Tu héberges ce salon. " : "") + "Choisis ta classe, puis déclare-toi prêt. La partie se lance quand tous sont prêts."
                : "Jusqu’à quatre joueurs. Chacun choisit sa classe, puis se déclare prêt.";
            if (!salon) return;

            // Code du salon : l'hôte peut l'afficher (masqué par défaut) et le copier.
            m_CodeTitre.text = l.EstHote ? "CODE DU SALON" : "SALON";
            m_CodeValeur.text = l.EstHote && !m_CodeVisible ? new string('•', Mathf.Max(6, l.CodeSalon.Length)) : l.CodeSalon;
            m_Afficher.style.display = l.EstHote ? DisplayStyle.Flex : DisplayStyle.None;
            m_Afficher.text = m_CodeVisible ? "Masquer" : "Afficher";
            m_Copier.style.display = l.EstHote ? DisplayStyle.Flex : DisplayStyle.None;
            m_Copie.style.visibility = Time.unscaledTime < m_CopieJusqua ? Visibility.Visible : Visibility.Hidden;

            var tousPrets = l.Joueurs.Count > 0;
            for (var i = 0; i < m_Emplacements.Count; i++)
            {
                var e = m_Emplacements[i];
                var j = i < l.Joueurs.Count ? l.Joueurs[i] : null;
                e.racine.EnableInClassList("lobby-place--libre", j == null);
                e.racine.EnableInClassList("lobby-place--local", j != null && j.EstLocal);
                e.racine.EnableInClassList("lobby-place--pret", j != null && j.Pret);
                e.hote.style.display = j != null && j.EstHote ? DisplayStyle.Flex : DisplayStyle.None;
                if (j == null)
                {
                    IconesUI.Poser(e.embleme, IconesUI.RepliClasse);
                    e.pseudo.text = "Emplacement libre";
                    e.classe.text = i < l.JoueursMax ? "En attente d’un joueur" : "";
                    e.etat.text = "";
                    continue;
                }
                if (!j.Pret) tousPrets = false;
                var c = ClassesJouables.Trouver(j.ClasseId);
                IconesUI.Poser(e.embleme, c != null ? c.Embleme : IconesUI.RepliClasse);
                e.pseudo.text = j.Pseudo + (j.EstLocal ? " (toi)" : "");
                e.classe.text = c != null ? c.Nom : "Classe à choisir";
                e.etat.text = j.Pret ? "Prêt" : "Pas prêt";
            }

            var local = Local();
            m_Pret.text = local != null && local.Pret ? "Plus prêt" : "Je suis prêt";
            m_Pret.EnableInClassList("lobby__bouton--actif", local != null && local.Pret);
            m_Lancer.style.display = l.EstHote ? DisplayStyle.Flex : DisplayStyle.None;
            m_Lancer.SetEnabled(tousPrets);
            var compte = etat == EtatLobby.CompteARebours;
            // Décompte : sa ligne est toujours réservée (masquée hors décompte), les boutons en dessous ne bougent jamais.
            m_Compte.style.visibility = compte ? Visibility.Visible : Visibility.Hidden;
            if (compte) m_Compte.text = "Tous prêts : la partie commence dans " + Mathf.CeilToInt(l.CompteARebours);
        }

        /// B : quitter le salon (reste sur le lobby), sinon fermer le lobby.
        public override bool Retour()
        {
            if (!DansSalon) { L?.Quitter(); return false; }
            L.Quitter();
            return true;
        }

        /// Y : prêt / pas prêt.
        public override void Reinitialiser()
        {
            if (DansSalon) L.BasculerPret();
        }
    }
}
