using UnityEngine;

namespace Deathless.Jeu
{
    /// Bouclier de Nyxessa (wiki : nyxessa, Bouclier) : levé par le sorcier au début de la nuit (incantation), jamais le
    /// jour. Il encaisse les coups portés à Nyxessa (et au sorcier) à sa place et renvoie des dégâts à chaque attaquant ;
    /// sa couleur suit sa solidité (bleu, orange, rouge : effet validé `BouclierRelique`, RelicShieldEtat + Visual). Il
    /// tient jusqu'à être brisé ; brisé, le sorcier meurt. À l'aube, il redescend. Encaissement et renvoi : GameBalance,
    /// par palier (1 à 5). Autorité : l'hôte (solo : ce poste). Chaque coup encaissé compte comme une frappe sur Nyxessa
    /// (EtatNyxessa.dernierCoup, pour DefenseNyxessa : réserve et salves) et déclenche l'événement `Touche` (réaction du
    /// sorcier, Sorcier.cs) ; à partir du palier de canalisation (bouclierCanalisationPalier, palier 4, wiki), une part
    /// de ces dégâts avance aussi la recharge du prochain missile de Nyxessa (bouclierDegatsParSecondeRecharge).
    public class BouclierNyxessa : MonoBehaviour
    {
        public static BouclierNyxessa Instance { get; private set; }

        [Tooltip("Instance du prefab Assets/VFX/BouclierRelique (RelicShieldEtat + RelicShieldVisual), centrée sur Nyxessa.")]
        public RelicShieldEtat effet;

        public bool Leve => effet != null && effet.IsUp;
        public bool Incantation => effet != null && effet.IsCasting;
        public float Vie => Leve ? effet.Health : 0f;
        public float VieMax => effet != null ? effet.MaxHealth : 0f;
        public Vector3 Centre => transform.position;   // base du cylindre, au centre de Nyxessa
        public float Rayon => effet != null ? effet.Radius : GameBalance.Courant.bouclierRayon;

        /// Le bouclier vient d'être brisé (le sorcier meurt).
        public event System.Action Brise;

        /// Le bouclier vient d'encaisser un coup (dégâts > 0, qu'il protège Nyxessa ou le sorcier) : réaction du sorcier.
        public event System.Action<Vector3> Touche;

        GameBalance B => GameBalance.Courant;
        /// Palier du bouclier acheté à la relique (1 à 5).
        int PalierActuel => Partie.Instance != null ? Partie.Instance.Etat.nyxessa.palierBouclier : 1;
        float m_DernierSon;

        void Awake()
        {
            Instance = this;
            if (effet == null) effet = GetComponentInChildren<RelicShieldEtat>(true);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            var p = Partie.Instance;
            if (p != null && p.nyxessa != null) p.nyxessa.absorbeur = Absorber;
            if (effet != null)
            {
                effet.radius = B.bouclierRayon;
                effet.height = B.bouclierHauteur;
                effet.castSeconds = B.bouclierIncantation;
            }
        }

        /// Le sorcier lève le bouclier (incantation, puis pleine solidité du palier).
        public void Lever()
        {
            if (effet == null || effet.IsUp || effet.IsCasting) return;
            effet.maxHealth = B.Palier(B.bouclierEncaissement, PalierActuel);
            effet.Lever();
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.BouclierLeve(effet.maxHealth);
            AudioBank.Jouer(SonsDuJeu.BouclierLeve, transform.position + Vector3.up * 2f, 0.9f);
            Partie.Instance?.Journal("Bouclier levé par le sorcier (palier " + PalierActuel + ", " + effet.maxHealth + " d'encaissement)");
        }

        /// L'aube, ou la fin de partie : le bouclier redescend dans le sol.
        public void Baisser()
        {
            if (effet == null) return;
            effet.Baisser();
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.BouclierBaisse();
        }

        /// Coup porté à Nyxessa ou au sorcier : le bouclier levé l'encaisse et renvoie des dégâts à l'attaquant ; renvoie
        /// la part qui passe (0 tant qu'il tient ; le reste du coup qui le brise).
        public float Absorber(InfoDegats info)
        {
            if (!Leve || info.montant <= 0f) return info.montant;
            float pris = Mathf.Min(effet.Health, info.montant);
            Vector3 point = PointSurParoi(info);
            effet.Frapper(pris, point);
            if (Deathless.Reseau.ReseauJeu.EnPartie && Deathless.Reseau.ReseauJeu.Autorite) Deathless.Reseau.PartieReseau.Instance?.BouclierTouche(pris, point);
            if (Time.time - m_DernierSon > 0.12f)
            {
                m_DernierSon = Time.time;
                AudioBank.Jouer(SonsDuJeu.BouclierTouche, point, 0.7f, 0.1f);
            }
            var p = Partie.Instance;
            if (p != null)
            {
                // Bug corrigé (27/09/2026) : le bouclier absorbe tout le coup avant qu'il n'atteigne la vie de Nyxessa
                // (Sante.Encaisser ne déclenche alors pas Touche), donc « frappée » ne s'allumait jamais tant qu'il tenait
                // et le missile gardé en réserve (wiki : Salves) ne partait plus, même affiché plein dans le HUD. Un coup
                // encaissé par le bouclier compte désormais comme une frappe sur Nyxessa (elle vide son stock si besoin).
                p.Etat.nyxessa.dernierCoup = Time.time;
                // Canalisation (palier 4+, wiki : Bouclier) : une part des dégâts encaissés avance la recharge du prochain
                // missile de Nyxessa, sans jamais toucher à sa vie. Décidé par l'hôte seul (multijoueur : lui seul tire).
                if (Deathless.Reseau.ReseauJeu.Autorite && PalierActuel >= B.bouclierCanalisationPalier) AvancerRechargeMissiles(pris);
            }
            Touche?.Invoke(point);
            // Riposte : dégâts renvoyés à l'attaquant (Nyxessa n'est créditée d'aucun score).
            var attaquant = info.source != null ? info.source.GetComponentInParent<Sante>() : null;
            float renvoi = B.Palier(B.bouclierRenvoi, PalierActuel);
            if (attaquant != null && !attaquant.Mort && renvoi > 0f)
                attaquant.Encaisser(new InfoDegats { montant = renvoi, equipeSource = Equipe.Relique, source = gameObject, point = attaquant.transform.position + Vector3.up, direction = (attaquant.transform.position - transform.position).normalized });
            if (!effet.IsUp)
            {
                AudioBank.Jouer(SonsDuJeu.BouclierBrise, transform.position + Vector3.up * 2f, 1f);
                Partie.Instance?.Journal("Bouclier brisé");
                Brise?.Invoke();
            }
            return info.montant - pris;
        }

        /// Canalisation (palier 4+ du bouclier) : convertit une partie des dégâts encaissés en recharge du prochain
        /// missile de Nyxessa (GameBalance.bouclierDegatsParSecondeRecharge, à équilibrer) ; peut faire gagner plusieurs
        /// missiles d'un coup si le coup est gros, sans dépasser le stock du palier.
        void AvancerRechargeMissiles(float degats)
        {
            var p = Partie.Instance;
            if (p == null || degats <= 0f || B.bouclierDegatsParSecondeRecharge <= 0f) return;
            var n = p.Etat.nyxessa;
            int max = GameBalance.AuPalier(B.missilesStockPaliers, n.palierMissiles);
            if (n.stock >= max) return;
            float duree = GameBalance.AuPalier(B.missileRegenerationPaliers, n.palierMissiles);
            n.regeneration += degats / B.bouclierDegatsParSecondeRecharge;
            while (n.stock < max && n.regeneration >= duree) { n.regeneration -= duree; n.stock++; }
            if (n.stock >= max) n.regeneration = 0f;
        }

        // ----------------------------------------------------------------- Client d'une partie réseau (l'hôte fait foi)

        public void LeverDistant(float max)
        {
            if (effet == null) return;
            effet.maxHealth = max;
            effet.Lever();
            AudioBank.Jouer(SonsDuJeu.BouclierLeve, transform.position + Vector3.up * 2f, 0.9f);
        }

        public void ToucherDistant(float montant, Vector3 point)
        {
            if (!Leve) return;
            effet.Frapper(montant, point);
            AudioBank.Jouer(SonsDuJeu.BouclierTouche, point, 0.7f, 0.1f);
            if (!effet.IsUp) AudioBank.Jouer(SonsDuJeu.BouclierBrise, transform.position + Vector3.up * 2f, 1f);
        }

        Vector3 PointSurParoi(InfoDegats info)
        {
            Vector3 d = info.point - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f && info.source != null) { d = info.source.transform.position - transform.position; d.y = 0f; }
            if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
            return transform.position + d.normalized * Rayon + Vector3.up * Mathf.Clamp(info.point.y - transform.position.y, 0.5f, 3f);
        }
    }
}
