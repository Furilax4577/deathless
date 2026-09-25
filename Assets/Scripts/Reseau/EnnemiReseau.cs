using System.Collections;
using Deathless.Jeu;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Côté réseau d'un squelette (Docs/reseau.md, étape 2). L'hôte fait foi : il le fait sortir de terre, le pilote (IA,
    /// NavMesh), lui applique les coups et le désintègre ; position (NetworkTransform) et animations (NetworkAnimator)
    /// partent de lui. Chez un client, le squelette est une marionnette : pas d'IA ni d'agent ; ses PV suivent ceux de
    /// l'hôte ; les coups, étourdissements, poussées et provocations des héros de ce poste partent vers l'hôte (RPC).
    [DisallowMultipleComponent]
    public class EnnemiReseau : NetworkBehaviour
    {
        readonly NetworkVariable<float> m_Pv = new NetworkVariable<float>(1f);
        readonly NetworkVariable<float> m_PvMax = new NetworkVariable<float>(1f);
        readonly NetworkVariable<bool> m_Elite = new NetworkVariable<bool>(false);
        readonly NetworkVariable<byte> m_Type = new NetworkVariable<byte>(0);

        public Squelette Squelette { get; private set; }
        /// Marionnette (client) : l'IA tourne chez l'hôte.
        public bool Distant => IsSpawned && !IsServer;

        void Awake() { Squelette = GetComponent<Squelette>(); }

        /// Hôte, avant l'apparition : type et élite (l'échelle de l'élite suit par le NetworkTransform).
        public void Preparer(TypeEnnemi type, bool elite)
        {
            m_TypePrepare = type;
            m_ElitePrepare = elite;
        }

        TypeEnnemi m_TypePrepare;
        bool m_ElitePrepare;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Valeurs écrites à l'apparition (elles partent avec le message d'apparition).
                m_Type.Value = (byte)m_TypePrepare;
                m_Elite.Value = m_ElitePrepare;
                m_Pv.Value = Squelette.Sante.Pv;
                m_PvMax.Value = Squelette.Sante.pvMax;
                return;
            }
            var sq = Squelette;
            sq.type = (TypeEnnemi)m_Type.Value;
            sq.elite = m_Elite.Value;
            sq.enabled = false;                       // IA et animation pilotées chez l'hôte
            if (sq.Agent != null) sq.Agent.enabled = false;
            sq.Sante.relais = Relayer;
            sq.Sante.Fixer(m_Pv.Value, m_PvMax.Value);
            DirecteurVagues.Instance?.AjouterDistant(sq);
            StartCoroutine(SortieVisuelle());
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer || Squelette == null) return;
            DirecteurVagues.Instance?.RetirerDistant(Squelette);
            // Désintégration (mort ou aube) : mêmes gemmes que chez l'hôte.
            var gemmes = EffetsJeu.Gemmes;
            if (gemmes != null && Squelette.modele != null) GemBurst.Rise(EffetsJeu.Volume(gameObject), gemmes);
        }

        /// Sortie de terre chez le client (la montée du modèle est locale à l'hôte, hors NetworkTransform).
        IEnumerator SortieVisuelle()
        {
            var sq = Squelette;
            if (sq.modele == null) yield break;
            if (EffetsJeu.Terre != null) DirtBurst.Spawn(transform.position, EffetsJeu.Terre, 1f);
            AudioBank.Jouer(SonsDuJeu.SqueletteSortie, transform.position, 0.8f, 0.15f);
            float t = 0f, d = sq.dureeSortie;
            bool terre = false;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.1f, d * 0.75f));
                sq.modele.localPosition = Vector3.down * 1.9f * (1f - (1f - (1f - k) * (1f - k)));
                if (!terre && t >= d * 0.35f) { terre = true; if (EffetsJeu.Terre != null) DirtBurst.Spawn(transform.position, EffetsJeu.Terre, 0.55f); }
                yield return null;
            }
            sq.modele.localPosition = Vector3.zero;
        }

        void Update()
        {
            if (!IsSpawned) return;
            var s = Squelette.Sante;
            if (IsServer)
            {
                if (m_Pv.Value != s.Pv) m_Pv.Value = s.Pv;
                if (m_PvMax.Value != s.pvMax) m_PvMax.Value = s.pvMax;
            }
            else if (s.Pv != m_Pv.Value || s.pvMax != m_PvMax.Value)
            {
                s.Fixer(m_Pv.Value, m_PvMax.Value);
                if (s.Mort) foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            }
        }

        // ----------------------------------------------------------------- Client → hôte

        float Relayer(InfoDegats info)
        {
            var s = Squelette.Sante;
            float estime = Mathf.Min(info.montant, s.Pv);
            if (!info.continu) AudioBank.Jouer(SonsDuJeu.SqueletteTouche, transform.position + Vector3.up, 0.8f, 0.05f);
            FrapperRpc(info.montant, info.critique, info.continu, info.point, info.direction);
            return estime;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void FrapperRpc(float montant, bool critique, bool continu, Vector3 point, Vector3 direction, RpcParams p = default)
        {
            var s = Squelette.Sante;
            if (s == null || s.Mort) return;
            ulong client = p.Receive.SenderClientId;
            var h = Partie.Instance != null ? Partie.Instance.HerosDe(Partie.IdJoueur(client)) : null;
            float reel = s.Encaisser(new InfoDegats
            {
                montant = montant, sourceId = Partie.IdJoueur(client), equipeSource = Equipe.Heros, source = h != null ? h.gameObject : null,
                critique = critique, continu = continu, point = point, direction = direction
            });
            if (reel > 0f && Partie.Instance != null) Partie.Instance.CompterDegats(Partie.IdJoueur(client), reel, critique);
        }

        public void DemanderEtourdir(float duree, int sourceId) => EtourdirRpc(duree);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void EtourdirRpc(float duree, RpcParams p = default) => Squelette.Etourdir(duree, Partie.IdJoueur(p.Receive.SenderClientId));

        public void DemanderRepousser(Vector3 deplacement, float etourdi) => RepousserRpc(deplacement, etourdi);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void RepousserRpc(Vector3 deplacement, float etourdi, RpcParams p = default) => Squelette.Repousser(deplacement, etourdi, Partie.IdJoueur(p.Receive.SenderClientId));

        public void DemanderProvoquer(float duree) => ProvoquerRpc(duree);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void ProvoquerRpc(float duree, RpcParams p = default)
        {
            var h = Partie.Instance != null ? Partie.Instance.HerosDe(Partie.IdJoueur(p.Receive.SenderClientId)) : null;
            if (h != null) Squelette.Provoquer(h, duree);
        }
    }
}
