using System.Collections.Generic;
using Deathless.Jeu;
using Deathless.UI.Donnees;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Côté réseau d'un héros (préfab de classe). Identité (pseudo, classe) posée par l'hôte à l'apparition ; vie et mode
    /// furtif écrits par le propriétaire, qui simule son héros comme en solo (position : NetworkTransform ; animations :
    /// NetworkAnimator) ; mort et délai de réapparition écrits par l'hôte, qui fait foi (étape 2). Chez les autres postes,
    /// le héros est une marionnette (Heros.Distant). Chez l'hôte, les coups portés à une marionnette (squelettes, missiles)
    /// partent vers son propriétaire, qui les applique (garde, parade, esquive comprises). Les tirs du propriétaire
    /// (flèches, carreaux, boules de feu), sa grenade fumigène et les effets de ses compétences sont rejoués chez les autres. Implémente IAllie (HUD).
    [DisallowMultipleComponent]
    public class HerosReseau : NetworkBehaviour, IAllie
    {
        public readonly NetworkVariable<FixedString64Bytes> NomJoueur = new NetworkVariable<FixedString64Bytes>();
        public readonly NetworkVariable<FixedString32Bytes> Classe = new NetworkVariable<FixedString32Bytes>();
        readonly NetworkVariable<float> m_Vie = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> m_VieMax = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<bool> m_Furtif = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        // Furie du viking (03/10/2026) : état écrit par le propriétaire (qui simule ses bonus), lu par les marionnettes qui
        // grossissent et rougeoient (ClasseViking.ForcerFurieDistante), comme le mode furtif de l'assassin.
        readonly NetworkVariable<bool> m_Furie = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> m_Reapparition = new NetworkVariable<float>(0f);
        readonly NetworkVariable<bool> m_Mort = new NetworkVariable<bool>(false);
        // Soins reçus cumulés, écrits par le propriétaire (le seul à voir Sante.Soigne) : l'hôte crédite la différence
        // au score du joueur (Partie.CompterSoins), sinon les soins d'un client restaient à 0 sur l'écran de score.
        readonly NetworkVariable<float> m_Soins = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        // Attributs (01/10/2026) : points d'attribut gagnés (Attributs.Tasser, 4 bits par attribut), écrits par le
        // propriétaire qui les dépense comme ses rangs de compétence ; recopiés dans l'EtatJoueur de la marionnette, où
        // l'hôte les lit pour ce qu'il décide (or ramassé, recul de la parade parfaite).
        readonly NetworkVariable<int> m_Attributs = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Inventaire (03/10/2026, Inventaire.Tasser : potions, clés, crochets, 4 bits chacun) : écrit par le propriétaire, qui
        // boit ses potions et tourne ses clés ; l'hôte recopie la valeur dans l'EtatJoueur de la marionnette quand elle change
        // (il y juge les achats au mécano et au druide) et l'ajoute lui-même à l'achat, sans attendre le propriétaire.
        readonly NetworkVariable<int> m_Inventaire = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// Héros réseau présents (tous les postes), dans l'ordre d'apparition.
        public static readonly List<HerosReseau> Tous = new List<HerosReseau>();

        public Heros Heros { get; private set; }
        /// Statuts du héros, tenus par l'hôte (Docs/reseau.md, « Statuts ») : envoyés seulement quand ils changent.
        NetworkList<StatutReseau> m_Statuts;
        Statuts m_StatutsJeu;
        /// Hauteur de l'étiquette du pseudo au-dessus des pieds.
        public float hauteurPseudo = 2.35f;
        /// Écart de vie (PV) en deçà duquel le propriétaire ne renvoie pas m_Vie (hors mort, retour à la vie et maximum).
        public const float PasVie = 0.5f;
        bool m_MortVue;

        void Awake() { Heros = GetComponent<Heros>(); m_Statuts = new NetworkList<StatutReseau>(); }

        /// Le héros local de ce poste, s'il est en partie réseau (sinon null).
        public static HerosReseau Local(Heros h)
        {
            if (h == null) return null;
            var r = h.GetComponent<HerosReseau>();
            return r != null && r.IsSpawned && r.IsOwner ? r : null;
        }

        string m_PseudoPrepare, m_ClassePreparee;

        /// Hôte, avant l'apparition : pseudo et classe (écrits à l'apparition, ils partent avec son message).
        public void Preparer(string pseudo, string classeId) { m_PseudoPrepare = pseudo; m_ClassePreparee = classeId; }

        public override void OnNetworkSpawn()
        {
            if (IsServer && m_PseudoPrepare != null)
            {
                NomJoueur.Value = new FixedString64Bytes(m_PseudoPrepare);
                Classe.Value = new FixedString32Bytes(m_ClassePreparee);
            }
            m_Pseudo = m_ClasseId = null;
            NomJoueur.OnValueChanged += NomChange;
            Classe.OnValueChanged += ClasseChange;
            Tous.Add(this);
            m_Inventaire.OnValueChanged += InventaireChange;
            BrancherStatuts();
            if (IsOwner && !IsServer) Heros.Sante.Soigne += OnSoigne;
            else if (IsServer && !IsOwner) m_Soins.OnValueChanged += OnSoinsChange;
            name = "Heros_" + Classe.Value + "_" + NomJoueur.Value + (IsOwner ? " (local)" : "");
            var p = Partie.Instance;
            if (p == null) { ReseauJeu.Journal("héros réseau sans Partie : " + name); return; }
            if (IsOwner)
            {
                // Position d'apparition donnée par l'hôte : le CharacterController est recalé dessus avant le premier pas.
                ReseauJeu.Journal("mon héros apparaît à " + transform.position.ToString("F1"));
                Heros.Teleporter(transform.position + Vector3.up * 0.05f);
                p.LancerReseau(Heros, Classe.Value.ToString(), NomJoueur.Value.ToString(), OwnerClientId);
            }
            else
            {
                p.AttacherHerosDistant(Heros, Classe.Value.ToString(), NomJoueur.Value.ToString(), OwnerClientId);
                InventaireChange(0, m_Inventaire.Value);   // valeur déjà répliquée à l'apparition (OnValueChanged ne la signale pas)
                if (IsServer) Heros.Sante.relais = RelayerVersProprietaire;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Heros != null && Heros.Sante != null) Heros.Sante.Soigne -= OnSoigne;
            m_Soins.OnValueChanged -= OnSoinsChange;
            m_Inventaire.OnValueChanged -= InventaireChange;
            NomJoueur.OnValueChanged -= NomChange;
            Classe.OnValueChanged -= ClasseChange;
            m_Pseudo = m_ClasseId = null;
            DebrancherStatuts();
            Tous.Remove(this);
            if (Partie.Instance != null) Partie.Instance.DetacherHeros(OwnerClientId);
        }

        /// Soin reçu d'un autre joueur (aura du paladin) : l'hôte l'a déjà crédité au soigneur, il ne compte pas ici.
        bool m_SoinExterne;

        void OnSoigne(float reel) { if (reel > 0f && !m_SoinExterne) m_Soins.Value += reel; }

        // ----------------------------------------------------------------- Soin d'aura du paladin (27/09/2026)

        /// Hôte : le paladin `soigneurId` soigne cette marionnette de `montant` ; son propriétaire l'applique (RPC hôte →
        /// propriétaire, même chemin que Renverser). Le soigneur est crédité ici, sur la vie répliquée (estimation).
        public void Soigner(float montant, int soigneurId)
        {
            if (!IsSpawned || !IsServer || Heros == null || Heros.Sante == null || Heros.Sante.Mort) return;
            float estime = Mathf.Clamp(montant, 0f, Mathf.Max(0f, Heros.Sante.pvMax - Heros.Sante.Pv));
            if (estime > 0f && Partie.Instance != null) Partie.Instance.CompterSoins(soigneurId, estime);
            SoignerRpc(montant, soigneurId);
        }

        [Rpc(SendTo.Owner)]
        void SoignerRpc(float montant, int soigneurId)
        {
            if (Heros == null || Heros.Sante == null) return;
            m_SoinExterne = true;
            Heros.Sante.Soigner(montant, soigneurId);
            m_SoinExterne = false;
        }

        /// Propriétaire (client paladin) : demande à l'hôte de soigner les alliés autour de sa marionnette
        /// (ClassePaladin.SoignerAllies chez l'hôte, montant borné à ce que la classe peut donner).
        public void DemanderSoinAura(float montant) => SoinAuraRpc(montant);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void SoinAuraRpc(float montant)
        {
            if (Heros == null || !(Heros.Classe is ClassePaladin) || !Heros.Vivant) return;
            var b = GameBalance.Courant;
            montant = Mathf.Clamp(montant, 0f, Heros.Sante.pvMax * b.soinPart * 1.6f + 1f);   // rang 3 de « Soin fervent » au plus
            ClassePaladin.SoignerAllies(Heros, montant);
        }

        /// Marionnette : l'inventaire du propriétaire a changé (potion bue, achat, clé tournée) ; recopié dans son EtatJoueur.
        void InventaireChange(int avant, int apres)
        {
            if (IsOwner || Heros == null || Heros.EtatJoueur == null) return;
            Inventaire.Detasser(apres, Heros.EtatJoueur);
        }

        void OnSoinsChange(float avant, float apres)
        {
            if (apres > avant && Partie.Instance != null) Partie.Instance.CompterSoins(Partie.IdJoueur(OwnerClientId), apres - avant);
        }

        void Update()
        {
            if (!IsSpawned || Heros == null || Heros.Sante == null) return;
            if (IsOwner)
            {
                // Vie envoyée par pas d'au moins PasVie (régénération, brûlure : plus un envoi par image), mais toujours
                // exacte à 0 (mort de la marionnette : Sante.Mort), en quittant 0 et au maximum.
                float pv = Heros.Sante.Pv, vu = m_Vie.Value;
                if (!Mathf.Approximately(vu, pv) && (Mathf.Abs(vu - pv) >= PasVie || pv <= 0f || vu <= 0f || pv >= Heros.Sante.pvMax))
                    m_Vie.Value = pv;
                if (!Mathf.Approximately(m_VieMax.Value, Heros.Sante.pvMax)) m_VieMax.Value = Heros.Sante.pvMax;
                bool furtif = Heros.Classe != null && Heros.Classe.Furtif;
                if (m_Furtif.Value != furtif) m_Furtif.Value = furtif;
                bool furie = Heros.Classe != null && Heros.Classe.UltimeActif;
                if (m_Furie.Value != furie) m_Furie.Value = furie;
                int attributs = Attributs.Tasser(Heros.EtatJoueur != null ? Heros.EtatJoueur.attributs : null);
                if (m_Attributs.Value != attributs) m_Attributs.Value = attributs;
                int inventaire = Inventaire.Tasser(Heros.EtatJoueur);
                if (m_Inventaire.Value != inventaire) m_Inventaire.Value = inventaire;
            }
            else
            {
                // Marionnette : vie, furtivité et mort recopiées (les squelettes de l'hôte la visent ou l'ignorent en conséquence).
                Heros.Sante.Fixer(m_Mort.Value ? 0f : m_Vie.Value, m_VieMax.Value);
                if (Heros.Classe is ClasseAssassin a) a.ForcerFurtifDistant(m_Furtif.Value);
                if (Heros.Classe is ClasseViking v) v.ForcerFurieDistante(m_Furie.Value);
                var ej = Heros.EtatJoueur;
                if (ej != null)
                {
                    if (ej.attributs == null || ej.attributs.Length < Attributs.Nombre) System.Array.Resize(ref ej.attributs, Attributs.Nombre);
                    Attributs.Detasser(m_Attributs.Value, ej.attributs);
                }
                if (m_Mort.Value != m_MortVue)
                {
                    m_MortVue = m_Mort.Value;
                    Heros.MortDistante(m_MortVue);
                }
            }
            if (IsServer)
            {
                // L'hôte tient la mort et le délai de réapparition de chacun.
                var j = Partie.Instance != null ? Partie.Instance.Joueur(Partie.IdJoueur(OwnerClientId)) : null;
                if (j != null)
                {
                    if (m_Mort.Value != j.mort) m_Mort.Value = j.mort;
                    float r = Mathf.Ceil(j.reapparitionRestante);
                    if (m_Reapparition.Value != r) m_Reapparition.Value = r;
                }
            }
        }

        // ----------------------------------------------------------------- Coups (hôte → propriétaire)

        float RelayerVersProprietaire(InfoDegats info)
        {
            ulong source = 0;
            var no = info.source != null ? info.source.GetComponentInParent<NetworkObject>() : null;
            if (no != null && no.IsSpawned) source = no.NetworkObjectId;
            EncaisserRpc(info.montant, info.parable, info.aDistance, info.continu, info.point, info.direction, source, info.source != null && no == null);
            return info.montant;
        }

        [Rpc(SendTo.Owner)]
        void EncaisserRpc(float montant, bool parable, bool aDistance, bool continu, Vector3 point, Vector3 direction, ulong source, bool sourceNyxessa)
        {
            GameObject go = null;
            if (source != 0 && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(source, out var no)) go = no.gameObject;
            Heros.Sante.Encaisser(new InfoDegats
            {
                montant = montant, equipeSource = Equipe.Ennemis, source = go, parable = parable, aDistance = aDistance, continu = continu, point = point, direction = direction
            });
        }

        // ----------------------------------------------------------------- Parade parfaite (Docs/reseau.md)

        /// Hôte : un coup parable de l'ennemi `ennemi` (NetworkObjectId) vise ce héros et porte dans `duree` s ; son
        /// propriétaire l'annonce localement (jauge de parade, jugement de la parade parfaite).
        public void AnnoncerCoup(ulong ennemi, float duree) { if (IsSpawned && IsServer && !IsOwner) CoupAnnonceRpc(ennemi, duree); }

        [Rpc(SendTo.Owner)]
        void CoupAnnonceRpc(ulong ennemi, float duree)
        {
            if (Heros == null || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ennemi, out var no)) return;
            var sq = no.GetComponent<Squelette>();
            if (sq != null) TelegraphieCoups.Annoncer(sq, Heros, duree);
        }

        /// Propriétaire (client) : parade parfaite jugée ici contre le coup de `attaquant` ; l'hôte la valide (tolérance)
        /// et applique le coup de bouclier (repousse, étourdissement). `avance` : appui avant l'impact prévu (s, journal).
        public void DemanderParadeParfaite(Squelette attaquant, Vector3 direction, float avance)
        {
            var no = attaquant != null ? attaquant.GetComponent<NetworkObject>() : null;
            ParadeParfaiteRpc(no != null && no.IsSpawned ? no.NetworkObjectId : 0UL, direction, avance);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void ParadeParfaiteRpc(ulong ennemi, Vector3 direction, float avance)
        {
            Squelette sq = null;
            if (ennemi != 0 && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ennemi, out var no)) sq = no.GetComponent<Squelette>();
            float rtt = 0f;
            try { rtt = NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) / 1000f; } catch { }
            ParadeParfaite.Demande(Heros, sq, direction, rtt, avance);
        }

        // ----------------------------------------------------------------- Renversé (hôte → propriétaire, 26/09/2026)

        /// Hôte : ce joueur vient d'être renversé (charge écrasante de Morgrim massue, onde de choc non sautée, grosse
        /// chute côté hôte s'il joue lui-même) ; son propriétaire déroule vraiment la séquence (Heros.RenverserLocal).
        public void Renverser() => RenverserRpc();

        [Rpc(SendTo.Owner)]
        void RenverserRpc() => Heros.RenverserLocal();

        /// Hôte : poussée amortie (recul du Tourbillon) ; son propriétaire l'applique à son héros (Heros.PousserLocal).
        public void Pousser(Vector3 vitesse) => PousserRpc(vitesse);

        [Rpc(SendTo.Owner)]
        void PousserRpc(Vector3 vitesse) => Heros.PousserLocal(vitesse);

        // ----------------------------------------------------------------- Mort et réapparition (l'hôte fait foi)

        /// Propriétaire (client) : son héros vient de mourir ; l'hôte compte la mort et fixe le délai.
        public void SignalerMort() => MortRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void MortRpc() { if (Partie.Instance != null) Partie.Instance.SignalerMort(Partie.IdJoueur(OwnerClientId)); }

        /// Hôte : le joueur réapparaît au point donné (délai écoulé, ou aube).
        public void Reapparaitre(Vector3 point) => ReapparaitreRpc(point);

        [Rpc(SendTo.Owner)]
        void ReapparaitreRpc(Vector3 point)
        {
            if (Partie.Instance != null) Partie.Instance.ReapparaitreLocal(point);
        }

        public bool Mort => m_Mort.Value;
        public float Reapparition => m_Reapparition.Value;

        // ----------------------------------------------------------------- Tirs et fumée (propriétaire → autres)

        /// Propriétaire : un projectile vient de partir ; les autres postes le voient voler (sans dégâts : le tireur les compte).
        public void Tir(ProjectileJeu.Genre genre, Vector3 depart, Vector3 cible, float vitesse) => TirRpc((byte)genre, depart, cible, vitesse);

        [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Owner)]
        void TirRpc(byte genre, Vector3 depart, Vector3 cible, float vitesse) => ProjectileJeu.TirerVisuel((ProjectileJeu.Genre)genre, depart, cible, vitesse, transform);

        public void Fumee(Vector3 depart, Vector3 cible, float duree) => FumeeRpc(depart, cible, duree);

        [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Owner)]
        void FumeeRpc(Vector3 depart, Vector3 cible, float duree) { if (Heros.Classe is ClasseAssassin a) a.LancerFumeeDistante(depart, cible, duree); }

        /// Propriétaire : effet de compétence (soin, charge, nuée, cône, tournante, rugissement, saut, critique…) rejoué chez
        /// les autres postes, avec son son, sur la marionnette (ClasseHeros.Diffuser / EffetDistant).
        public void Effet(byte effet, Vector3 a, Vector3 b, float v) => EffetRpc(effet, a, b, v);

        [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Owner)]
        void EffetRpc(byte effet, Vector3 a, Vector3 b, float v)
        {
            EffetsRecus++;
            if (!EffetsVus.Contains(effet)) EffetsVus.Add(effet);
            if (Heros != null && Heros.Classe != null) Heros.Classe.EffetDistant(effet, a, b, v);
        }

        /// Tests : effets reçus de ce héros (marionnette) et numéros déjà vus.
        public int EffetsRecus { get; private set; }
        public readonly List<byte> EffetsVus = new List<byte>();

        /// Hôte : Nyxessa rappelle ce joueur du donjon (crépuscule) ; son propriétaire le ramène au village.
        public void Rappeler(int garde, int perdu) => RappelRpc(garde, perdu);

        [Rpc(SendTo.Owner)]
        void RappelRpc(int garde, int perdu) => DonjonJeu.Instance?.RappelLocal(garde, perdu);

        /// Hôte : un squelette a repéré cet assassin (marionnette) ; son propriétaire sort du mode furtif. Chaque squelette
        /// qui le voit appelle ceci à chaque image jusqu'au retour de m_Furtif (un aller-retour) : un envoi au plus toutes
        /// les 0,5 s, au lieu d'un RPC fiable par image et par squelette.
        public void SignalerRepere()
        {
            if (Time.unscaledTime - m_DernierRepere < 0.5f) return;
            m_DernierRepere = Time.unscaledTime;
            RepereRpc();
        }

        float m_DernierRepere = -99f;

        [Rpc(SendTo.Owner)]
        void RepereRpc() { if (Heros.Classe is ClasseAssassin a) a.Reperer(); }

        // ----------------------------------------------------------------- Statuts (l'hôte fait foi, le propriétaire prédit)

        void BrancherStatuts()
        {
            if (Heros == null) return;
            m_StatutsJeu = Statuts.De(Heros);
            if (IsServer)
            {
                m_StatutsJeu.Change += EcrireStatuts;
                EcrireStatuts();
                return;
            }
            m_StatutsJeu.Autorite = false;
            if (IsOwner) { m_StatutsJeu.relais = DemanderStatut; m_StatutsJeu.predire = true; }
            m_Statuts.OnListChanged += StatutsRecus;
            StatutsReseau.Lire(m_Statuts, m_StatutsJeu, NetworkManager);
        }

        void DebrancherStatuts()
        {
            if (m_StatutsJeu == null) return;
            m_StatutsJeu.Change -= EcrireStatuts;
            m_StatutsJeu.relais = null;
            if (m_Statuts != null) m_Statuts.OnListChanged -= StatutsRecus;
        }

        void EcrireStatuts() { if (IsServer && IsSpawned) StatutsReseau.Ecrire(m_Statuts, m_StatutsJeu, NetworkManager); }

        void StatutsRecus(NetworkListEvent<StatutReseau> e) => StatutsReseau.Lire(m_Statuts, m_StatutsJeu, NetworkManager);

        /// Propriétaire (client) : statut de son héros (chute, garde brisée, ivresse) ; déjà appliqué ici, l'hôte le pose
        /// et le diffuse.
        void DemanderStatut(Statut s) => StatutRpc((byte)s.type, s.duree, s.intensite, (byte)s.origine);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void StatutRpc(byte type, float duree, float intensite, byte origine)
        {
            if (Heros == null || Heros.Sante.Mort) return;
            var s = StatutsReseau.Valider((TypeStatut)type, duree, intensite, (OrigineStatut)origine, Partie.IdJoueur(OwnerClientId), true);
            if (s.type != TypeStatut.Aucun) Statuts.De(Heros).AjouterDemande(s);
        }

        // ----------------------------------------------------------------- IAllie

        /// Pseudo et classe en chaînes, gardés entre deux changements (le HUD les lit à chaque image) : plus de
        /// ToString() de FixedString par lecture.
        string m_Pseudo, m_ClasseId;

        void NomChange(FixedString64Bytes avant, FixedString64Bytes apres) => m_Pseudo = null;
        void ClasseChange(FixedString32Bytes avant, FixedString32Bytes apres) => m_ClasseId = null;

        public string Pseudo => m_Pseudo ??= NomJoueur.Value.ToString();
        public string ClasseId => m_ClasseId ??= Classe.Value.ToString();
        public float Vie => m_Vie.Value;
        public float VieMax => m_VieMax.Value;
        public bool EstMort => m_Mort.Value;
        public float TempsAvantReapparition => m_Reapparition.Value;
        public Vector3? PositionTete => transform.position + Vector3.up * hauteurPseudo;
        /// Vote « prêt » de ce joueur (01/10/2026) : lu dans PartieReseau.Scores (ScoreReseau.pret, écrit par l'hôte à
        /// chaque changement, déjà répliqué pour l'écran de score) : rien de plus à envoyer. Au plus 4 entrées à parcourir.
        public bool EstPret => PartieReseau.Instance != null && PartieReseau.Instance.ScoreDe(OwnerClientId, out var s) && s.pret;
    }
}
