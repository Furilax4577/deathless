using UnityEngine;

namespace Deathless.Jeu
{
    /// Missiles de Nyxessa, palier 1 fixe dans la 0.1 (wiki : nyxessa) : stock 2, un missile régénéré toutes les 12 s,
    /// 1,5 s au moins entre deux tirs, 40 dégâts, portée 30 m.
    /// Cible : d'abord un ennemi qui frappe Nyxessa ; ensuite le Nécromancien ou un élite ; sinon l'ennemi le plus proche.
    /// Salves : un missile par cible en gardant un missile en réserve ; contre un groupe de trois ennemis ou plus, ou un
    /// élite (boss compris), tirs jusqu'à vider le stock sauf un ; si Nyxessa vient d'être frappée, elle vide tout.
    [RequireComponent(typeof(Sante))]
    public class DefenseNyxessa : MonoBehaviour
    {
        Sante m_Sante;
        Nyxessa m_Visuel;
        EtatNyxessa E => Partie.Instance != null ? Partie.Instance.Etat.nyxessa : null;
        GameBalance B => GameBalance.Courant;
        float m_DerniereAlerte = -99f;
        Squelette m_CibleSalve;
        int m_SalveRestante;

        void Awake()
        {
            m_Sante = GetComponent<Sante>();
            m_Visuel = GetComponent<Nyxessa>();
        }

        void Start()
        {
            var e = E;
            if (e != null) e.stock = B.missilesStock;
            m_Sante.Touche += OnTouche;
            m_Sante.Tue += _ => Detruire();
            m_Sante.invulnerable = B.nyxessaInvincible;
        }

        void OnTouche(InfoDegats info, float reel)
        {
            AudioBank.Jouer(SonsDuJeu.NyxessaFrappee, info.point, 0.9f, 0.25f);
            if (Time.time - m_DerniereAlerte > 5f)
            {
                m_DerniereAlerte = Time.time;
                AudioBank.Jouer2D(SonsDuJeu.NyxessaAlerte, 0.6f);
            }
        }

        Vector3 Centre => m_Visuel != null ? m_Visuel.CentreCristal : transform.position + Vector3.up * 5f;

        void Update()
        {
            var p = Partie.Instance;
            var e = E;
            if (p == null || e == null || !p.EnCours || !Deathless.Reseau.ReseauJeu.Autorite) return;   // multijoueur : l'hôte seul tire
            var b = B;
            float dt = Time.deltaTime;
            e.pv = m_Sante.Pv;
            e.pvMax = m_Sante.pvMax;
            if (e.stock < b.missilesStock)
            {
                e.regeneration += dt;
                if (e.regeneration >= b.missileRegeneration) { e.regeneration = 0f; e.stock++; }
            }
            else e.regeneration = 0f;
            e.depuisDernierTir += dt;
            if (e.depuisDernierTir < b.missileIntervalle || e.stock <= 0) return;
            var dv = DirecteurVagues.Instance;
            if (dv == null || dv.Vivants.Count == 0) return;

            bool frappee = Time.time - e.dernierCoup < b.frappeeRecente;
            int reserve = frappee ? 0 : 1;

            // Salve en cours : on continue sur la même cible tant qu'elle vit.
            Squelette cible = null;
            if (m_SalveRestante > 0 && m_CibleSalve != null && m_CibleSalve.Vivant && Portee(m_CibleSalve)) cible = m_CibleSalve;
            else m_SalveRestante = 0;
            if (cible == null) cible = Choisir(dv, out _);
            if (cible == null) return;

            bool groupe = Groupe(dv, cible) >= b.groupeTaille;
            bool lourd = cible.elite || cible.type == TypeEnnemi.Golem || cible.type == TypeEnnemi.Necromancien;
            if (m_SalveRestante == 0)
            {
                if (frappee) m_SalveRestante = e.stock;                         // vide tout son stock
                else if (groupe || lourd) m_SalveRestante = e.stock - 1;       // salve sauf un
                else m_SalveRestante = e.stock - 1 >= 1 ? 1 : 0;               // un missile, un en réserve
                m_CibleSalve = cible;
            }
            if (m_SalveRestante <= 0 || e.stock <= reserve) return;
            // Groupe : on répartit la salve sur les membres du groupe.
            Squelette tir = cible;
            if (groupe && !lourd) tir = MembreGroupe(dv, cible, m_SalveRestante);
            Tirer(tir, e, p);
            m_SalveRestante--;
        }

        bool Portee(Squelette s)
        {
            Vector3 d = s.transform.position - transform.position; d.y = 0f;
            return d.magnitude <= B.missilePortee && s.EtatCourant != Squelette.Etat.SortieDeTerre;
        }

        Squelette Choisir(DirecteurVagues dv, out int priorite)
        {
            Squelette sur = null, lourd = null, proche = null;
            float dSur = float.MaxValue, dLourd = float.MaxValue, dProche = float.MaxValue;
            foreach (var s in dv.Vivants)
            {
                if (s == null || !s.Vivant || !Portee(s)) continue;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (s.SurNyxessa && d < dSur) { dSur = d; sur = s; }
                if ((s.elite || s.type == TypeEnnemi.Necromancien) && d < dLourd) { dLourd = d; lourd = s; }
                if (d < dProche) { dProche = d; proche = s; }
            }
            priorite = sur != null ? 1 : lourd != null ? 2 : 3;
            return sur ?? lourd ?? proche;
        }

        int Groupe(DirecteurVagues dv, Squelette c)
        {
            int n = 0;
            float r2 = B.groupeRayon * B.groupeRayon;
            foreach (var s in dv.Vivants) if (s != null && s.Vivant && (s.transform.position - c.transform.position).sqrMagnitude <= r2) n++;
            return n;
        }

        Squelette MembreGroupe(DirecteurVagues dv, Squelette c, int rang)
        {
            float r2 = B.groupeRayon * B.groupeRayon;
            int i = 0;
            foreach (var s in dv.Vivants)
            {
                if (s == null || !s.Vivant || (s.transform.position - c.transform.position).sqrMagnitude > r2) continue;
                if (i++ == rang % Mathf.Max(1, Groupe(dv, c))) return s;
            }
            return c;
        }

        void Tirer(Squelette s, EtatNyxessa e, Partie p)
        {
            e.stock--;
            e.depuisDernierTir = 0f;
            var b = B;
            MissileCrane.Tirer(Centre, s.Sante, b.missileDegats, b.missileVitesse, b.missileGuidage, Equipe.Relique, gameObject, true);
            p.Journal("Nyxessa tire sur " + s.type + (s.elite ? " (élite)" : "") + (s.SurNyxessa ? " qui la frappe" : "") + ", stock " + e.stock);
        }

        /// Client d'une partie réseau : Nyxessa détruite chez l'hôte, même effet ici.
        public void DetruireVisuel() => Detruire();

        void Detruire()
        {
            AudioBank.Jouer2D(SonsDuJeu.NyxessaDestruction, 1f);
            var g = EffetsJeu.Gemmes;
            if (g != null)
            {
                GemBurst.Explode(Centre, 4f, g);
                GemBurst.Explode(Centre + Vector3.down * 2f, 3f, g);
            }
            if (m_Visuel != null)
            {
                var cristal = m_Visuel.transform.Find("Crystal");
                if (cristal != null) cristal.gameObject.SetActive(false);
                var ceinture = m_Visuel.GetComponent<RelicBelt>();
                if (ceinture != null) ceinture.enabled = false;
                foreach (var r in m_Visuel.GetComponents<Renderer>()) r.enabled = false;
            }
        }
    }
}
