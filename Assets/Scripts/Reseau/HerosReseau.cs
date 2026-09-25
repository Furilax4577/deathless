using System.Collections.Generic;
using Deathless.Jeu;
using Deathless.UI.Donnees;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Côté réseau d'un héros (préfab de classe) : identité (pseudo, classe, posées par l'hôte à l'apparition) et état
    /// affiché aux autres (vie, mort, réapparition), écrit par le propriétaire. Le propriétaire simule son héros comme en
    /// solo ; sa position (NetworkTransform) et ses animations (NetworkAnimator) partent de lui. Chez les autres, le héros
    /// est une marionnette (Heros.Distant). Implémente IAllie pour la colonne « vie des autres joueurs » du HUD.
    [DisallowMultipleComponent]
    public class HerosReseau : NetworkBehaviour, IAllie
    {
        public readonly NetworkVariable<FixedString64Bytes> NomJoueur = new NetworkVariable<FixedString64Bytes>();
        public readonly NetworkVariable<FixedString32Bytes> Classe = new NetworkVariable<FixedString32Bytes>();
        readonly NetworkVariable<float> m_Vie = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> m_VieMax = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> m_Reapparition = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<bool> m_Mort = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// Héros réseau présents (tous les postes), dans l'ordre d'apparition.
        public static readonly List<HerosReseau> Tous = new List<HerosReseau>();

        public Heros Heros { get; private set; }
        /// Hauteur de l'étiquette du pseudo au-dessus des pieds.
        public float hauteurPseudo = 2.35f;

        void Awake() { Heros = GetComponent<Heros>(); }

        public override void OnNetworkSpawn()
        {
            Tous.Add(this);
            name = "Heros_" + Classe.Value + "_" + NomJoueur.Value + (IsOwner ? " (local)" : "");
            var p = Partie.Instance;
            if (p == null) { ReseauJeu.Journal("héros réseau sans Partie : " + name); return; }
            if (IsOwner)
            {
                // Position d'apparition donnée par l'hôte : le CharacterController est recalé dessus avant le premier pas.
                ReseauJeu.Journal("mon héros apparaît à " + transform.position.ToString("F1"));
                Heros.Teleporter(transform.position + Vector3.up * 0.05f);
            }
            if (IsOwner) p.LancerReseau(Heros, Classe.Value.ToString(), NomJoueur.Value.ToString(), OwnerClientId);
            else p.AttacherHerosDistant(Heros, Classe.Value.ToString(), NomJoueur.Value.ToString(), OwnerClientId);
        }

        public override void OnNetworkDespawn()
        {
            Tous.Remove(this);
            if (Partie.Instance != null) Partie.Instance.DetacherHeros(OwnerClientId);
        }

        void Update()
        {
            if (!IsSpawned || !IsOwner || Heros == null || Heros.Sante == null) return;
            var j = Partie.Instance != null ? Partie.Instance.JoueurLocal : null;
            if (!Mathf.Approximately(m_Vie.Value, Heros.Sante.Pv)) m_Vie.Value = Heros.Sante.Pv;
            if (!Mathf.Approximately(m_VieMax.Value, Heros.Sante.pvMax)) m_VieMax.Value = Heros.Sante.pvMax;
            bool mort = j != null && j.mort;
            if (m_Mort.Value != mort) m_Mort.Value = mort;
            float r = j != null ? Mathf.Ceil(j.reapparitionRestante) : 0f;
            if (m_Reapparition.Value != r) m_Reapparition.Value = r;
        }

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
