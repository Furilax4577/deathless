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
        /// Fin du compte à rebours en temps serveur (NetworkManager.ServerTime), -1 sans compte : écrite une fois au
        /// départ, chaque poste en déduit le restant (Restant) sans autre message.
        public NetworkVariable<double> FinCompte = new NetworkVariable<double>(-1d);
        public NetworkVariable<bool> Lance = new NetworkVariable<bool>(false);

        public const float DureeCompte = 3f;

        /// Un compte à rebours est en cours.
        public bool EnCompte => FinCompte.Value >= 0d;

        /// Secondes restantes avant le lancement (0 sans compte).
        public float Restant => EnCompte && NetworkManager != null ? Mathf.Max(0f, (float)(FinCompte.Value - NetworkManager.ServerTime.Time)) : 0f;

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
            // Partie lancée : retour d'un joueur parti ou arrivée en cours de partie (ReseauJeu décide de sa classe).
            if (Lance.Value) { ReseauJeu.Instance?.PresentationEnCours(id, pseudo.ToString(), classePreferee.ToString()); return; }
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
            if (TousPrets()) FinCompte.Value = NetworkManager.ServerTime.Time + 0.01;
        }

        // ----------------------------------------------------------------- Hôte

        /// Un client est parti : sa place et sa classe sont libérées (en cours de partie, ReseauJeu les garde pour son
        /// retour). Renvoie sa ligne, s'il était là.
        public bool Retirer(ulong clientId, out JoueurSalon joueur)
        {
            joueur = default;
            if (!IsServer) return false;
            int i = Index(clientId);
            if (i < 0) return false;
            joueur = Joueurs[i];
            ReseauJeu.Journal("Salon : " + joueur.pseudo + " quitte le salon");
            Joueurs.RemoveAt(i);
            return true;
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
                if (!EnCompte) FinCompte.Value = NetworkManager.ServerTime.Time + DureeCompte;
                else if (NetworkManager.ServerTime.Time >= FinCompte.Value)
                {
                    Lance.Value = true;
                    ReseauJeu.Instance.LancerPartie();
                }
            }
            else if (EnCompte) FinCompte.Value = -1d;
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
