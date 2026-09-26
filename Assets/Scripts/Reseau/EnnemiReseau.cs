using System.Collections;
using System.Collections.Generic;
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
        /// Statuts du squelette, tenus par l'hôte (Docs/reseau.md, « Statuts ») : envoyés seulement quand ils changent.
        NetworkList<StatutReseau> m_Statuts;
        Statuts m_StatutsJeu;
        /// Marionnette (client) : l'IA tourne chez l'hôte.
        public bool Distant => IsSpawned && !IsServer;

        void Awake() { Squelette = GetComponent<Squelette>(); m_Statuts = new NetworkList<StatutReseau>(); }

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
                BrancherStatuts();
                return;
            }
            var sq = Squelette;
            sq.type = (TypeEnnemi)m_Type.Value;
            sq.elite = m_Elite.Value;
            sq.MarquerElite();
            sq.enabled = false;                       // IA et animation pilotées chez l'hôte
            if (sq.Agent != null) sq.Agent.enabled = false;
            sq.Sante.relais = Relayer;
            sq.Sante.Fixer(m_Pv.Value, m_PvMax.Value);
            BrancherStatuts();
            DirecteurVagues.Instance?.AjouterDistant(sq);
            StartCoroutine(SortieVisuelle());
        }

        public override void OnNetworkDespawn()
        {
            DebrancherStatuts();
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

        // ----------------------------------------------------------------- Statuts (l'hôte fait foi)

        void BrancherStatuts()
        {
            m_StatutsJeu = Statuts.De(Squelette);
            if (IsServer)
            {
                m_StatutsJeu.Change += EcrireStatuts;
                EcrireStatuts();
                return;
            }
            m_StatutsJeu.Autorite = false;
            m_StatutsJeu.relais = DemanderStatut;
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

        /// Client : un héros de ce poste pose un statut (brûlure du mage…) ; l'hôte le vérifie et le pose.
        void DemanderStatut(Statut s) => StatutRpc((byte)s.type, s.duree, s.intensite, (byte)s.origine);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void StatutRpc(byte type, float duree, float intensite, byte origine, RpcParams p = default)
        {
            if (Squelette == null || Squelette.Sante.Mort) return;
            var s = StatutsReseau.Valider((TypeStatut)type, duree, intensite, (OrigineStatut)origine, Partie.IdJoueur(p.Receive.SenderClientId), false);
            if (s.type != TypeStatut.Aucun) Statuts.De(Squelette).AjouterDemande(s);
        }

        // ----------------------------------------------------------------- Morgrim : effets au sol (léger, 26/09/2026)

        /// Hôte : diffuse une télégraphie ou un impact au sol de Morgrim aux autres postes (Docs/vfx.md, Docs/reseau.md).
        /// Léger : un octet de thème, une forme (rayon/angle/durée), pas un message par gemme.
        public void DiffuserEffetMorgrim(bool telegraphie, byte theme, float rayon, float duree, float angleDeg)
        {
            if (!IsServer || !IsSpawned) return;
            EffetMorgrimRpc(telegraphie, theme, rayon, duree, angleDeg);
        }

        [Rpc(SendTo.NotServer)]
        void EffetMorgrimRpc(bool telegraphie, byte theme, float rayon, float duree, float angleDeg)
            => (Squelette as MorgrimVariant)?.RejouerEffetDistant(telegraphie, (VfxTheme)theme, rayon, duree, angleDeg);

        // ----------------------------------------------------------------- Morgrim : onde de choc lente du Fracas (Massue, 26/09/2026)

        /// Temps réseau (NetworkManager.ServerTime), sinon Time.time hors réseau (solo) : même horloge que StatutsReseau,
        /// pour que chaque poste calcule le même front d'onde malgré la latence (Docs/reseau.md).
        public static float TempsReseau()
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening ? (float)nm.ServerTime.TimeAsFloat : Time.time;
        }

        // Hôte : onde de Fracas en cours (une seule à la fois par Morgrim) — centre, vitesse, portée et largeur de bande
        // de détection, heure de départ réseau ; joueurs déjà crédités (une touche par onde).
        Vector3 m_OndeCentre;
        float m_OndeDepart = -999f, m_OndeVitesse, m_OndeRayonMax, m_OndeLargeurBande;
        readonly HashSet<int> m_OndeTouches = new HashSet<int>();

        /// Hôte : lance l'onde de Fracas et la diffuse aux autres postes avec l'heure de départ réseau, pour qu'ils
        /// calculent le même front que l'hôte malgré la latence (Docs/reseau.md). Le jugement (au sol ou en l'air) se
        /// fait ensuite chez chaque client, sur l'onde qu'il voit (OndeChocLente) : voir SignalerOndeTouchee.
        public void DiffuserOndeMorgrim(Vector3 centre, float vitesse, float rayonMax, float largeurBande)
        {
            if (!IsServer || !IsSpawned) return;
            m_OndeCentre = centre; m_OndeVitesse = vitesse; m_OndeRayonMax = rayonMax; m_OndeLargeurBande = largeurBande;
            m_OndeDepart = TempsReseau();
            m_OndeTouches.Clear();
            OndeMorgrimRpc(centre, vitesse, rayonMax, largeurBande, m_OndeDepart);
        }

        [Rpc(SendTo.NotServer)]
        void OndeMorgrimRpc(Vector3 centre, float vitesse, float rayonMax, float largeurBande, float depart)
            => (Squelette as MorgrimVariant)?.RecevoirOndeDistante(centre, vitesse, rayonMax, largeurBande, depart);

        /// Client (propriétaire touché) : signale à l'hôte avoir été touché par l'onde en cours, jugé chez lui (au sol
        /// au passage du front — la latence ferait sinon voir à l'hôte les sauts en retard, Docs/reseau.md). L'hôte
        /// vérifie la vraisemblance (front au bon endroit au bon instant, une fois par joueur) avant d'appliquer.
        public void SignalerOndeTouchee() => OndeToucheeRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void OndeToucheeRpc(RpcParams p = default)
        {
            if (m_OndeDepart < 0f || Squelette == null || !Squelette.Vivant) return;
            int joueurId = Partie.IdJoueur(p.Receive.SenderClientId);
            if (m_OndeTouches.Contains(joueurId)) return;
            var h = Partie.Instance != null ? Partie.Instance.HerosDe(joueurId) : null;
            if (h == null || !h.Vivant) return;
            float ecoule = TempsReseau() - m_OndeDepart;
            float duree = m_OndeRayonMax / Mathf.Max(0.1f, m_OndeVitesse);
            if (ecoule < 0f || ecoule > duree + 0.6f) return;   // fenêtre dépassée : trop tard pour être crédible
            float rFront = Mathf.Min(m_OndeVitesse * ecoule, m_OndeRayonMax);
            Vector3 d = h.transform.position - m_OndeCentre; d.y = 0f;
            if (Mathf.Abs(d.magnitude - rFront) > m_OndeLargeurBande + 1.5f) return;   // vraisemblance : latence tolérée
            m_OndeTouches.Add(joueurId);
            var b = GameBalance.Courant;
            h.Sante.Encaisser(new InfoDegats
            {
                montant = b.morgrimMassueFracasDegats, equipeSource = Equipe.Ennemis, source = Squelette.gameObject, parable = false,
                point = h.transform.position + Vector3.up, direction = d.sqrMagnitude > 0.0001f ? d.normalized : Squelette.transform.forward
            });
            h.Renverser();
        }
    }
}
