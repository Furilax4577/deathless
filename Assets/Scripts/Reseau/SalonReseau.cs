using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Un joueur du salon, tel que l'hôte le synchronise (NetworkList).
    public struct JoueurSalon : INetworkSerializable, IEquatable<JoueurSalon>
    {
        public ulong clientId;
        public FixedString64Bytes pseudo;
        public FixedString32Bytes classeId;
        public bool pret;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref clientId);
            s.SerializeValue(ref pseudo);
            s.SerializeValue(ref classeId);
            s.SerializeValue(ref pret);
        }

        public bool Equals(JoueurSalon o) => clientId == o.clientId && pseudo.Equals(o.pseudo) && classeId.Equals(o.classeId) && pret == o.pret;
    }

    /// État du salon, possédé par l'hôte (autorité) et répliqué à tous : joueurs (pseudo, classe unique, prêt), compte à
    /// rebours, lancement. Les clients ne font que des demandes (RPC) que l'hôte valide : classe unique au premier arrivé,
    /// prêt seulement avec une classe. Objet réseau persistant (DontDestroyOnLoad) : il survit au chargement du village.
    public class SalonReseau : NetworkBehaviour
    {
        public static SalonReseau Instance { get; private set; }

        public NetworkList<JoueurSalon> Joueurs;
        public NetworkVariable<float> CompteARebours = new NetworkVariable<float>(-1f);
        public NetworkVariable<bool> Lance = new NetworkVariable<bool>(false);

        public const float DureeCompte = 3f;

        void Awake()
        {
            Joueurs = new NetworkList<JoueurSalon>();
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsClient) PresenterRpc(LobbyReseau.PseudoLocal, LobbyReseau.ClassePreferee);
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        // ----------------------------------------------------------------- Demandes des joueurs (vers l'hôte)

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void PresenterRpc(FixedString64Bytes pseudo, FixedString32Bytes classePreferee, RpcParams p = default)
        {
            ulong id = p.Receive.SenderClientId;
            if (Index(id) >= 0) return;
            var j = new JoueurSalon { clientId = id, pseudo = pseudo, pret = false };
            string pref = classePreferee.ToString();
            j.classeId = new FixedString32Bytes(ClasseLibre(pref));
            Joueurs.Add(j);
            ReseauJeu.Journal("Salon : " + pseudo + " arrive (client " + id + ", classe " + j.classeId + ")");
        }

        public void DemanderClasse(string classeId) => ClasseRpc(new FixedString32Bytes(classeId ?? ""));

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void ClasseRpc(FixedString32Bytes classeId, RpcParams p = default)
        {
            int i = Index(p.Receive.SenderClientId);
            if (i < 0 || Lance.Value) return;
            string c = classeId.ToString();
            // Classe unique dans le salon : premier arrivé, premier servi (l'hôte arbitre).
            if (PriseParAutre(c, p.Receive.SenderClientId)) return;
            var j = Joueurs[i];
            j.classeId = classeId;
            j.pret = false;
            Joueurs[i] = j;
            ReseauJeu.Journal("Salon : " + j.pseudo + " choisit " + c);
        }

        public void DemanderPret() => PretRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void PretRpc(RpcParams p = default)
        {
            int i = Index(p.Receive.SenderClientId);
            if (i < 0 || Lance.Value) return;
            var j = Joueurs[i];
            if (j.classeId.Length == 0) return;
            j.pret = !j.pret;
            Joueurs[i] = j;
            ReseauJeu.Journal("Salon : " + j.pseudo + (j.pret ? " est prêt" : " n'est plus prêt"));
        }

        public void DemanderLancement() => LancerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void LancerRpc(RpcParams p = default)
        {
            if (p.Receive.SenderClientId != NetworkManager.ServerClientId) return;   // l'hôte seul
            if (TousPrets()) CompteARebours.Value = 0.01f;
        }

        // ----------------------------------------------------------------- Hôte

        /// Un client est parti : sa place et sa classe sont libérées.
        public void Retirer(ulong clientId)
        {
            if (!IsServer) return;
            int i = Index(clientId);
            if (i >= 0)
            {
                ReseauJeu.Journal("Salon : " + Joueurs[i].pseudo + " quitte le salon");
                Joueurs.RemoveAt(i);
            }
        }

        public bool TousPrets()
        {
            if (Joueurs.Count == 0) return false;
            foreach (var j in Joueurs) if (!j.pret || j.classeId.Length == 0) return false;
            return true;
        }

        void Update()
        {
            if (!IsServer || Lance.Value) return;
            if (TousPrets())
            {
                if (CompteARebours.Value < 0f) CompteARebours.Value = DureeCompte;
                else
                {
                    CompteARebours.Value = Mathf.Max(0f, CompteARebours.Value - Time.deltaTime);
                    if (CompteARebours.Value <= 0f)
                    {
                        Lance.Value = true;
                        ReseauJeu.Instance.LancerPartie();
                    }
                }
            }
            else if (CompteARebours.Value >= 0f) CompteARebours.Value = -1f;
        }

        int Index(ulong id)
        {
            for (int i = 0; i < Joueurs.Count; i++) if (Joueurs[i].clientId == id) return i;
            return -1;
        }

        bool PriseParAutre(string c, ulong sauf)
        {
            foreach (var j in Joueurs) if (j.clientId != sauf && j.classeId.ToString() == c) return true;
            return false;
        }

        string ClasseLibre(string preferee)
        {
            if (!string.IsNullOrEmpty(preferee) && Deathless.UI.Donnees.ClassesJouables.Jouable(preferee) && !PriseParAutre(preferee, ulong.MaxValue)) return preferee;
            foreach (var c in Deathless.UI.Donnees.ClassesJouables.Catalogue)
                if (!c.Verrouillee && !PriseParAutre(c.Id, ulong.MaxValue)) return c.Id;
            return "";
        }
    }
}
