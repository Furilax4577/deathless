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
        readonly NetworkVariable<float> m_Reapparition = new NetworkVariable<float>(0f);
        readonly NetworkVariable<bool> m_Mort = new NetworkVariable<bool>(false);

        /// Héros réseau présents (tous les postes), dans l'ordre d'apparition.
        public static readonly List<HerosReseau> Tous = new List<HerosReseau>();

        public Heros Heros { get; private set; }
        /// Hauteur de l'étiquette du pseudo au-dessus des pieds.
        public float hauteurPseudo = 2.35f;
        bool m_MortVue;

        void Awake() { Heros = GetComponent<Heros>(); }

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
            Tous.Add(this);
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
                if (IsServer) Heros.Sante.relais = RelayerVersProprietaire;
            }
        }

        public override void OnNetworkDespawn()
        {
            Tous.Remove(this);
            if (Partie.Instance != null) Partie.Instance.DetacherHeros(OwnerClientId);
        }

        void Update()
        {
            if (!IsSpawned || Heros == null || Heros.Sante == null) return;
            if (IsOwner)
            {
                if (!Mathf.Approximately(m_Vie.Value, Heros.Sante.Pv)) m_Vie.Value = Heros.Sante.Pv;
                if (!Mathf.Approximately(m_VieMax.Value, Heros.Sante.pvMax)) m_VieMax.Value = Heros.Sante.pvMax;
                bool furtif = Heros.Classe != null && Heros.Classe.Furtif;
                if (m_Furtif.Value != furtif) m_Furtif.Value = furtif;
            }
            else
            {
                // Marionnette : vie, furtivité et mort recopiées (les squelettes de l'hôte la visent ou l'ignorent en conséquence).
                Heros.Sante.Fixer(m_Mort.Value ? 0f : m_Vie.Value, m_VieMax.Value);
                if (Heros.Classe is ClasseAssassin a) a.ForcerFurtifDistant(m_Furtif.Value);
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
            EncaisserRpc(info.montant, info.parable, info.continu, info.point, info.direction, source, info.source != null && no == null);
            return info.montant;
        }

        [Rpc(SendTo.Owner)]
        void EncaisserRpc(float montant, bool parable, bool continu, Vector3 point, Vector3 direction, ulong source, bool sourceNyxessa)
        {
            GameObject go = null;
            if (source != 0 && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(source, out var no)) go = no.gameObject;
            Heros.Sante.Encaisser(new InfoDegats
            {
                montant = montant, equipeSource = Equipe.Ennemis, source = go, parable = parable, continu = continu, point = point, direction = direction
            });
        }

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

        /// Hôte : un squelette a repéré cet assassin (marionnette) ; son propriétaire sort du mode furtif.
        public void SignalerRepere() => RepereRpc();

        [Rpc(SendTo.Owner)]
        void RepereRpc() { if (Heros.Classe is ClasseAssassin a) a.Reperer(); }

        // ----------------------------------------------------------------- IAllie

        public string Pseudo => NomJoueur.Value.ToString();
        public string ClasseId => Classe.Value.ToString();
        public float Vie => m_Vie.Value;
        public float VieMax => m_VieMax.Value;
        public bool EstMort => m_Mort.Value;
        public float TempsAvantReapparition => m_Reapparition.Value;
        public Vector3? PositionTete => transform.position + Vector3.up * hauteurPseudo;
    }
}
