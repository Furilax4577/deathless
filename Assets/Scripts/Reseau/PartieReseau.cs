using System;
using Deathless.Jeu;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Score et état d'un joueur de la partie réseau, tels que l'hôte les tient (écran de score, vote, HUD des clients).
    public struct ScoreReseau : INetworkSerializable, IEquatable<ScoreReseau>
    {
        public ulong clientId;
        public FixedString64Bytes pseudo;
        public FixedString32Bytes classeId;
        public bool pret, mort;
        public float reapparition;
        public int or, degats, tues, morts, critiques, evites, soins;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref clientId); s.SerializeValue(ref pseudo); s.SerializeValue(ref classeId);
            s.SerializeValue(ref pret); s.SerializeValue(ref mort); s.SerializeValue(ref reapparition);
            s.SerializeValue(ref or); s.SerializeValue(ref degats); s.SerializeValue(ref tues); s.SerializeValue(ref morts);
            s.SerializeValue(ref critiques); s.SerializeValue(ref evites); s.SerializeValue(ref soins);
        }

        public bool Equals(ScoreReseau o) => clientId == o.clientId && pseudo.Equals(o.pseudo) && classeId.Equals(o.classeId) && pret == o.pret
            && mort == o.mort && Mathf.Approximately(reapparition, o.reapparition) && or == o.or && degats == o.degats && tues == o.tues
            && morts == o.morts && critiques == o.critiques && evites == o.evites && soins == o.soins;
    }

    /// Monde de la partie réseau (Docs/reseau.md, étape 2), possédé par l'hôte qui fait foi : horloge (phase, nuit, temps),
    /// Nyxessa, caisse commune, scores et état des joueurs (vote prêt, mort, réapparition), sorcier et bouclier. Les clients
    /// suivent (Partie.SuivreHote, Sorcier.SuivreHote, BouclierNyxessa) ; les événements ponctuels (zones, missiles, pièces
    /// d'or, bouclier) leur sont joués par RPC. Apparu par l'hôte au lancement, détruit avec la scène.
    public class PartieReseau : NetworkBehaviour
    {
        public static PartieReseau Instance { get; private set; }

        public readonly NetworkVariable<byte> Phase = new NetworkVariable<byte>();
        public readonly NetworkVariable<int> Nuit = new NetworkVariable<int>(1);
        public readonly NetworkVariable<float> TempsPhase = new NetworkVariable<float>();
        public readonly NetworkVariable<float> DureePhase = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Duree = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> ComptePret = new NetworkVariable<bool>();
        public readonly NetworkVariable<byte> Resultat = new NetworkVariable<byte>();
        public readonly NetworkVariable<int> NuitAtteinte = new NetworkVariable<int>();
        public readonly NetworkVariable<int> OrEquipe = new NetworkVariable<int>();
        public readonly NetworkVariable<int> PalierMissiles = new NetworkVariable<int>(1);
        public readonly NetworkVariable<int> PalierBouclier = new NetworkVariable<int>(1);
        public readonly NetworkVariable<float> NyxPv = new NetworkVariable<float>(1f);
        public readonly NetworkVariable<float> NyxPvMax = new NetworkVariable<float>(1f);
        public readonly NetworkVariable<bool> NyxDetruite = new NetworkVariable<bool>();
        public readonly NetworkVariable<int> Joueurs = new NetworkVariable<int>(1);
        public readonly NetworkVariable<int> Prets = new NetworkVariable<int>();
        // Sorcier (marionnette chez les clients).
        public readonly NetworkVariable<byte> SorcierEtat = new NetworkVariable<byte>();
        public readonly NetworkVariable<Vector3> SorcierPosition = new NetworkVariable<Vector3>();
        public readonly NetworkVariable<float> SorcierLacet = new NetworkVariable<float>();
        public readonly NetworkVariable<float> SorcierVitesse = new NetworkVariable<float>();
        public NetworkList<ScoreReseau> Scores;

        float m_EnvoiTemps;

        void Awake() { Scores = new NetworkList<ScoreReseau>(); }

        public override void OnNetworkSpawn() { Instance = this; }
        public override void OnNetworkDespawn() { if (Instance == this) Instance = null; }

        public bool ScoreDe(ulong clientId, out ScoreReseau s)
        {
            foreach (var x in Scores) if (x.clientId == clientId) { s = x; return true; }
            s = default;
            return false;
        }

        // ----------------------------------------------------------------- Hôte : écriture de l'état

        void LateUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            var p = Partie.Instance;
            if (p == null) return;
            var e = p.Etat;
            Ecrire(Phase, (byte)e.phase);
            Ecrire(Nuit, e.nuit);
            Ecrire(DureePhase, e.dureePhase);
            Ecrire(ComptePret, e.comptePret);
            Ecrire(Resultat, (byte)e.resultat);
            Ecrire(NuitAtteinte, e.nuitAtteinte);
            Ecrire(OrEquipe, e.orEquipe);
            Ecrire(PalierMissiles, e.nyxessa.palierMissiles);
            Ecrire(PalierBouclier, e.nyxessa.palierBouclier);
            Ecrire(Joueurs, Mathf.Max(1, e.joueurs.Count));
            Ecrire(Prets, p.JoueursPrets);
            if (p.nyxessa != null) { Ecrire(NyxPv, p.nyxessa.Pv); Ecrire(NyxPvMax, p.nyxessa.pvMax); }
            Ecrire(NyxDetruite, e.nyxessa.detruite);
            // Temps : 5 fois par seconde (les clients avancent seuls entre deux envois), et à chaque changement de phase.
            m_EnvoiTemps -= Time.deltaTime;
            if (m_EnvoiTemps <= 0f || e.tempsPhase < TempsPhase.Value) { m_EnvoiTemps = 0.2f; TempsPhase.Value = e.tempsPhase; Duree.Value = e.duree; }
            var so = Sorcier.Instance;
            if (so != null)
            {
                Ecrire(SorcierEtat, (byte)so.EtatCourant);
                if ((SorcierPosition.Value - so.transform.position).sqrMagnitude > 0.0004f) SorcierPosition.Value = so.transform.position;
                if (Mathf.Abs(Mathf.DeltaAngle(SorcierLacet.Value, so.transform.eulerAngles.y)) > 0.5f) SorcierLacet.Value = so.transform.eulerAngles.y;
                float v = so.Vitesse;
                if (Mathf.Abs(SorcierVitesse.Value - v) > 0.1f) SorcierVitesse.Value = v;
            }
            // Scores et état de chaque joueur.
            for (int i = 0; i < e.joueurs.Count; i++)
            {
                var j = e.joueurs[i];
                var s = new ScoreReseau
                {
                    clientId = (ulong)Mathf.Max(0, j.id - 1), pseudo = new FixedString64Bytes(j.nom ?? ""), classeId = new FixedString32Bytes(j.classeId ?? ""),
                    pret = j.pret, mort = j.mort, reapparition = Mathf.Ceil(j.reapparitionRestante),
                    or = j.score.orRapporte, degats = Mathf.RoundToInt(j.score.degatsInfliges), tues = j.score.ennemisTues, morts = j.score.morts,
                    critiques = j.score.coupsCritiques, evites = Mathf.RoundToInt(j.score.degatsEvitesNyxessa), soins = Mathf.RoundToInt(j.score.soinsProdigues)
                };
                if (i < Scores.Count) { if (!Scores[i].Equals(s)) Scores[i] = s; }
                else Scores.Add(s);
            }
            while (Scores.Count > e.joueurs.Count) Scores.RemoveAt(Scores.Count - 1);
        }

        static void Ecrire<T>(NetworkVariable<T> v, T valeur) where T : IEquatable<T>
        {
            if (!v.Value.Equals(valeur)) v.Value = valeur;
        }

        // ----------------------------------------------------------------- Client → hôte

        /// Vote « prêt » (jour) ou Rejouer (écran de score) d'un client.
        public void DemanderPret() => PretRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void PretRpc(RpcParams p = default)
        {
            if (Partie.Instance != null) Partie.Instance.BasculerPret(Partie.IdJoueur(p.Receive.SenderClientId));
        }

        /// Achat d'un palier à la relique par un client : l'hôte décide (caisse commune) et lui répond.
        public void DemanderAchat(byte amelioration) => AchatRpc(amelioration);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void AchatRpc(byte amelioration, RpcParams p = default)
        {
            var partie = Partie.Instance;
            if (partie == null) return;
            ulong client = p.Receive.SenderClientId;
            string msg = partie.Acheter((Partie.Amelioration)amelioration, Partie.IdJoueur(client));
            ReponseAchatRpc(new FixedString128Bytes(msg), RpcTarget.Single(client, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void ReponseAchatRpc(FixedString128Bytes message, RpcParams p = default) => ReponseAchat?.Invoke(message.ToString());

        /// Client : réponse de l'hôte à sa demande d'achat.
        public static event Action<string> ReponseAchat;

        /// Hôte : un palier a été acheté ; les clients jouent le son et l'annonce.
        public void AnnoncerPalier(byte amelioration, int palier, string qui)
        {
            if (IsServer) PalierRpc(amelioration, palier, new FixedString64Bytes(qui ?? ""));
        }

        [Rpc(SendTo.NotServer)]
        void PalierRpc(byte amelioration, int palier, FixedString64Bytes qui)
        {
            var partie = Partie.Instance;
            if (partie == null) return;
            if (amelioration == 0) partie.Etat.nyxessa.palierMissiles = palier; else partie.Etat.nyxessa.palierBouclier = palier;
            partie.SignalerPalier((Partie.Amelioration)amelioration, palier, qui.ToString());
        }

        // ----------------------------------------------------------------- Hôte → clients (événements)

        public void Zone(int index, int action) { if (IsServer) ZoneRpc(index, (byte)action); }

        [Rpc(SendTo.NotServer)]
        void ZoneRpc(int index, byte action) => DirecteurVagues.Instance?.ActionZone(index, action);

        public void PieceOr(Vector3 point) { if (IsServer) PieceOrRpc(point); }

        [Rpc(SendTo.NotServer)]
        void PieceOrRpc(Vector3 point)
        {
            Deathless.Jeu.PieceOr.Jouer(point);
            AudioBank.Jouer(SonsDuJeu.Or, point, 0.3f, 0.1f);
        }

        public void BouclierLeve(float max) { if (IsServer) BouclierLeveRpc(max); }

        [Rpc(SendTo.NotServer)]
        void BouclierLeveRpc(float max) => BouclierNyxessa.Instance?.LeverDistant(max);

        public void BouclierTouche(float montant, Vector3 point) { if (IsServer) BouclierToucheRpc(montant, point); }

        [Rpc(SendTo.NotServer)]
        void BouclierToucheRpc(float montant, Vector3 point) => BouclierNyxessa.Instance?.ToucherDistant(montant, point);

        public void BouclierBaisse() { if (IsServer) BouclierBaisseRpc(); }

        [Rpc(SendTo.NotServer)]
        void BouclierBaisseRpc() => BouclierNyxessa.Instance?.Baisser();

        public void SorcierInvoque() { if (IsServer) SorcierInvoqueRpc(); }

        [Rpc(SendTo.NotServer)]
        void SorcierInvoqueRpc() => Sorcier.Instance?.InvoquerDistant();

        /// Missile en crâne (Nyxessa ou Nécromancien) : les clients voient le même vol, sans dégâts (l'hôte les applique).
        public void Missile(Vector3 depart, Sante cible, float vitesse, float guidage, bool parNyxessa)
        {
            if (!IsServer || cible == null) return;
            ulong id = 0; bool nyx = cible == (Partie.Instance != null ? Partie.Instance.nyxessa : null);
            var no = cible.GetComponentInParent<NetworkObject>();
            if (!nyx && (no == null || !no.IsSpawned)) return;
            if (no != null) id = no.NetworkObjectId;
            MissileRpc(depart, id, nyx, vitesse, guidage, parNyxessa);
        }

        [Rpc(SendTo.NotServer)]
        void MissileRpc(Vector3 depart, ulong cibleId, bool nyx, float vitesse, float guidage, bool parNyxessa)
        {
            Sante cible = null;
            if (nyx) cible = Partie.Instance != null ? Partie.Instance.nyxessa : null;
            else if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(cibleId, out var no)) cible = no.GetComponentInChildren<Sante>();
            if (cible != null) MissileCrane.TirerVisuel(depart, cible, vitesse, guidage, parNyxessa);
        }
    }
}
