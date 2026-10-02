using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Bots du banc en jeu (BancClasses) : un par classe, qui joue uniquement par la manette virtuelle (EntreesSimulees :
    /// boutons, stick gauche) et la caméra (lacet, tangage de CameraEpaule, comme le stick droit). Rotations calquées sur
    /// Docs/outils/simulateur_vagues.py (seuils des profils « moyen » et « bon ») ; visée bruitée tirée à chaque tir.
    public abstract class Bot
    {
        /// Poste des classes à distance (m de Nyxessa, du côté le plus pressé) et laisse des classes de mêlée (simulateur).
        public const float Poste = 4.5f, Laisse = 10f;

        protected BancClasses Banc;
        protected Heros H => Banc.Heros;
        protected Partie P => Banc.Partie;
        protected GameBalance B => GameBalance.Courant;
        protected bool Bon;
        protected System.Random R;
        protected string[] NomsEmplacements = { "rt", "lt", "lb", "rb" };

        /// Direction (depuis Nyxessa) du côté le plus pressé : moyenne des squelettes proches, pondérée par leur proximité.
        public Vector3 Pression = Vector3.forward;
        float m_MajPression;
        protected readonly List<Squelette> V = new List<Squelette>();
        readonly float[] m_Restant = new float[4];
        readonly EtatEmplacement[] m_EtatEmp = new EtatEmplacement[4];

        /// Point visé à chaque image (null : on garde la caméra).
        protected System.Func<Vector3> Visee;
        protected float ProchaineAction;
        float m_LibreDepuis;
        bool m_EtaitOccupe;

        // Profil (simulateur : PROFILS).
        protected float Reaction => Bon ? 0.05f : 0.15f;
        protected float BruitDeg => Bon ? 0.8f : 1.6f;          // écart type de la visée (degrés), tiré à chaque tir
        protected float BruitAnticipation => Bon ? 0.1f : 0.35f;   // erreur relative sur l'avance donnée à une cible qui marche

        public static Bot Creer(string classe, BancClasses banc, string profil, int graine)
        {
            Bot b;
            switch (classe)
            {
                case "viking": b = new BotViking(); break;
                case "mage": b = new BotMage(); break;
                case "rodeur": b = new BotRodeur(); break;
                case "assassin": b = new BotAssassin(); break;
                default: b = new BotPaladin(); break;
            }
            b.Banc = banc;
            b.Bon = profil == "bon";
            b.R = new System.Random(graine);
            return b;
        }

        public void Tick(float dt)
        {
            if (H == null || H.Classe == null) return;
            V.Clear();
            V.AddRange(Banc.Vivants());
            if (Time.time >= m_MajPression) { m_MajPression = Time.time + 0.5f; MajPression(); }
            SuivreEmplacements();
            bool occ = H.Classe.Occupe || !H.PeutAgir;
            if (m_EtaitOccupe && !occ) m_LibreDepuis = Time.time;
            m_EtaitOccupe = occ;
            if (!H.Vivant) return;
            Jouer(dt);
            if (Visee != null) Viser(Visee());
            // Sorts de zone à visée au sol (02/10/2026) : le bot ouvre la visée (LB / RB), puis confirme (RT) quand l'accord est passé.
            if (m_Confirmer > 0f && Time.time >= m_Confirmer)
            {
                m_Confirmer = 0f;
                if (H.Classe.EnVisee) EntreesSimulees.Appui("rightTrigger", 0.1f);
            }
        }

        float m_Confirmer;

        protected abstract void Jouer(float dt);

        public virtual void SurCoupDague(Sante cible) { }
        protected float RecuDepuisSoin;
        public void SurCoupRecu(float m) { RecuDepuisSoin += m; }

        // ----------------------------------------------------------------- État de la classe

        void SuivreEmplacements()
        {
            for (int i = 1; i < 4; i++)
            {
                var e = H.Classe.Emplacement(i, out float restant, out float total);
                if (m_Restant[i] <= 0.001f && restant > 0.001f) Banc.Usage(NomsEmplacements[i]);
                else if (m_EtatEmp[i] != EtatEmplacement.Actif && e == EtatEmplacement.Actif && total <= 0f) Banc.Usage(NomsEmplacements[i]);
                m_Restant[i] = restant;
                m_EtatEmp[i] = e;
            }
        }

        protected bool Pret(int i) => H.Classe.Emplacement(i, out _, out _) == EtatEmplacement.Pret;
        protected bool Actif(int i) => H.Classe.Emplacement(i, out _, out _) == EtatEmplacement.Actif;
        protected float Jauge => H.Classe.ValeurJauge;

        /// Le joueur peut lancer une action : classe libre, temps de réaction écoulé depuis la fin de la précédente.
        protected bool Libre => H.PeutAgir && !H.Classe.Occupe && Time.time >= ProchaineAction && Time.time - m_LibreDepuis >= Reaction;

        /// Appui bref ; LB et RB partent après le délai d'accord (0,1 s réelle) : la visée reste tenue jusque-là.
        protected void Appuyer(string controle, string usage)
        {
            EntreesSimulees.Appui(controle, 0.1f);
            Banc.Usage("appui_" + usage);
            bool accord = controle == "leftShoulder" || controle == "rightShoulder";
            if (usage == "mur" || usage == "grande_boule" || usage == "nuee") m_Confirmer = Time.time + 0.2f * Time.timeScale;   // visée au sol : RT confirme
            ProchaineAction = Time.time + Mathf.Max(Reaction, 0.12f) + (accord ? 0.12f * Time.timeScale : 0f);
        }

        // ----------------------------------------------------------------- Géométrie

        protected Vector3 Nyx => P.nyxessa.transform.position;
        protected Vector3 Pos => H.transform.position;
        protected static float D(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

        void MajPression()
        {
            Vector3 somme = Vector3.zero;
            foreach (var s in V)
            {
                Vector3 d = BancClasses.Plat(s.transform.position - Nyx);
                float l = d.magnitude;
                if (l < 0.5f || l > 40f) continue;
                somme += d / l / (1f + l / 8f);
            }
            if (somme.sqrMagnitude > 0.0001f) Pression = somme.normalized;
        }

        protected int Compter(Vector3 c, float r)
        {
            int n = 0;
            foreach (var s in V) if (D(s.transform.position, c) <= r + (s.Agent != null ? s.Agent.radius : 0.4f)) n++;
            return n;
        }

        protected Vector3 Centre(Vector3 c, float r)
        {
            Vector3 som = Vector3.zero; int n = 0;
            foreach (var s in V) if (D(s.transform.position, c) <= r) { som += s.transform.position; n++; }
            return n > 0 ? som / n : c;
        }

        /// Squelette (à `portee` m du héros) qui a le plus de voisins à `rayon` m ; égalité : le plus menaçant.
        protected Squelette PlusDense(float portee, float rayon, out int n)
        {
            Squelette best = null; n = 0; float bs = float.MaxValue;
            foreach (var s in V)
            {
                if (D(s.transform.position, Pos) > portee) continue;
                int k = Compter(s.transform.position, rayon);
                float sc = -k * 100f + Menace(s);
                if (sc < bs) { bs = sc; best = s; n = k; }
            }
            return best;
        }

        /// Menace (plus petit = plus menaçant) : ce qui frappe Nyxessa d'abord, puis le plus proche d'elle.
        protected float Menace(Squelette s) => D(s.transform.position, Nyx) - (s.SurNyxessa ? 50f : 0f);

        protected Squelette CibleDistance(float portee)
        {
            Squelette best = null; float bs = float.MaxValue;
            foreach (var s in V)
            {
                if (D(s.transform.position, Pos) > portee) continue;
                float sc = Menace(s);
                if (sc < bs) { bs = sc; best = s; }
            }
            return best;
        }

        /// Cible de mêlée : dans la laisse de 10 m autour de Nyxessa (ou au contact du héros) ; ce qui frappe Nyxessa
        /// d'abord, puis le plus menaçant, en gardant la cible courante sauf écart net.
        protected Squelette CibleMelee(Squelette actuelle)
        {
            Squelette best = null; float bs = float.MaxValue;
            foreach (var s in V)
            {
                float dN = D(s.transform.position, Nyx), dH = D(s.transform.position, Pos);
                if (dN > Laisse + 1.5f && dH > 2.5f) continue;
                float sc = dN + 0.5f * dH - (s.SurNyxessa ? 20f : 0f) - (s == actuelle ? 3f : 0f) + Bonus(s, dH);
                // Un joueur ne frappe pas à travers le pilier de Nyxessa : cible cachée pénalisée.
                if (!Combat.Degage(Pos + Vector3.up * 1.2f, s.transform.position + Vector3.up)) sc += 12f;
                if (sc < bs) { bs = sc; best = s; }
            }
            return best;
        }

        protected virtual float Bonus(Squelette s, float dH) => 0f;

        /// Point d'approche d'une cible de mêlée : côté extérieur quand elle est près de Nyxessa (on ne se glisse pas
        /// entre elle et le pilier).
        protected Vector3 Approche(Squelette s)
        {
            Vector3 v = BancClasses.Plat(s.transform.position - Nyx);
            if (v.magnitude > 6f || v.sqrMagnitude < 0.01f) return s.transform.position;
            return s.transform.position + v.normalized * 1.3f;
        }

        protected Vector3 Maison => BancClasses.Sol(Nyx + Pression * 3.5f);
        protected Vector3 PosteDistance => BancClasses.Sol(Nyx + Pression * Poste);

        // ----------------------------------------------------------------- Manette et caméra

        /// Stick gauche vers `dest` (relatif à la caméra), arrêt à `arret` m.
        protected void Bouger(Vector3 dest, float arret)
        {
            Vector3 d = BancClasses.Plat(dest - Pos);
            if (d.magnitude <= arret) { Arret(); return; }
            var cam = P.cameraJeu;
            Vector3 f = Quaternion.Euler(0f, cam != null ? cam.lacet : 0f, 0f) * Vector3.forward;
            Vector3 r = Vector3.Cross(Vector3.up, f);
            Vector3 n = d.normalized;
            // Coincé (marche du plateau de Nyxessa, foule) : un joueur saute, puis contourne.
            if (Time.time >= m_Controle)
            {
                bool bloque = D(Pos, m_PosControle) < 0.25f && !H.Classe.Occupe && d.magnitude > 1.5f;
                m_Coince = bloque ? m_Coince + 1 : 0;
                m_PosControle = Pos;
                m_Controle = Time.time + 0.6f;
                if (m_Coince >= 2) { EntreesSimulees.Appui("buttonSouth", 0.1f); Banc.Usage("deblocage"); m_Contourne = Time.time + 0.7f; m_Sens = -m_Sens; }
            }
            if (Time.time < m_Contourne) n = Quaternion.Euler(0f, 70f * m_Sens, 0f) * n;
            EntreesSimulees.Stick(new Vector2(Vector3.Dot(n, r), Vector3.Dot(n, f)), Vector2.zero, 0f);
        }

        float m_Controle, m_Contourne, m_Sens = 1f;
        int m_Coince;
        Vector3 m_PosControle;

        protected void Arret() { EntreesSimulees.Stick(Vector2.zero, Vector2.zero, 0f); m_Coince = 0; m_PosControle = Pos; m_Controle = Time.time + 0.6f; }

        /// Lacet et tangage de la caméra tels que le centre de l'écran passe par `point` (même placement que CameraEpaule :
        /// décalage d'épaule et recul relus sur la caméra, zoom de visée compris).
        /// Mêlée : les coups partent vers l'avant à plat de la caméra ; le joueur tourne la caméra vers sa cible (lacet),
        /// tangage de jeu.
        protected bool Melee;

        protected void Viser(Vector3 point)
        {
            if (Melee)
            {
                var cm = P.cameraJeu;
                Vector3 v = BancClasses.Plat(point - Pos);
                if (cm != null && v.sqrMagnitude > 0.01f) { cm.lacet = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg; cm.tangage = 20f; }
                return;
            }
            var cam = P.cameraJeu;
            if (cam == null) return;
            var b = B;
            Vector3 pivot = Pos + Vector3.up * b.cameraHauteur;
            Quaternion rot0 = Quaternion.Euler(cam.tangage, cam.lacet, 0f);
            Vector3 off = Quaternion.Inverse(rot0) * (cam.transform.position - pivot);
            if (off.magnitude < 0.3f || off.magnitude > 9f || off.z > -0.1f) off = new Vector3(b.cameraEpaule, 0f, -b.cameraDistance);
            float lac = cam.lacet, tan = cam.tangage;
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = pivot + Quaternion.Euler(tan, lac, 0f) * off;
                Vector3 v = point - pos;
                lac = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                tan = -Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg;
            }
            cam.lacet = lac;
            cam.tangage = Mathf.Clamp(tan, -60f, 80f);
        }

        protected float Gauss()
        {
            double u1 = 1.0 - R.NextDouble(), u2 = R.NextDouble();
            return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Sin(2.0 * System.Math.PI * u2));
        }

        protected double Hasard() => R.NextDouble();

        /// Erreur de visée d'un tir (tirée une fois par tir) : angles (degrés) et facteur d'avance.
        protected struct Erreur { public float x, y, avance; }

        protected Erreur Tirer(float facteurBruit = 1f) => new Erreur { x = Gauss() * BruitDeg * facteurBruit, y = Gauss() * BruitDeg * facteurBruit, avance = 1f + Gauss() * BruitAnticipation - (Bon ? 0f : 0.15f) };

        /// Point visé sur `s` (hauteur `h` au-dessus des pieds, ou la tête si h < 0), avec l'avance d'un projectile à
        /// `vitesse` m/s parti dans `delai` s, et l'erreur du tir.
        protected Vector3 PointSur(Squelette s, float h, float vitesse, float delai, Erreur e)
        {
            if (s == null) return Pos + H.transform.forward * 10f;
            Vector3 c = h < 0f ? s.CentreTete : s.transform.position + Vector3.up * h;
            Vector3 v = s.Agent != null && s.Agent.enabled ? s.Agent.velocity : Vector3.zero;
            v.y = 0f;
            float t = delai + (vitesse > 0f ? Vector3.Distance(Pos + Vector3.up * 1.4f, c) / vitesse : 0f);
            Vector3 p = c + v * t * e.avance;
            var cam = P.cameraJeu;
            Vector3 o = cam != null ? cam.transform.position : Pos + Vector3.up * 1.6f;
            Vector3 dir = p - o; float l = dir.magnitude;
            if (l < 0.01f) return p;
            dir /= l;
            Vector3 dr = Vector3.Cross(Vector3.up, dir).normalized, dh = Vector3.Cross(dir, dr);
            return p + dr * (l * Mathf.Tan(e.x * Mathf.Deg2Rad)) + dh * (l * Mathf.Tan(e.y * Mathf.Deg2Rad));
        }
    }

    // ===================================================================== Paladin

    /// Épée (3 cibles au plus), charge bélier sur une cible à 2,5 m (moyen) ou 4 m (bon) au moins, soin d'aura quand il
    /// aurait perdu 25 % de sa vie (héros immortel : coups comptés). Pas de garde ni de parade (simulateur : idem).
    class BotPaladin : Bot
    {
        Squelette m_Cible;
        float m_Chk = -1f; Squelette m_ChkCible;
        public BotPaladin() { Melee = true; NomsEmplacements = new[] { "epee", "garde", "charge", "soin" }; }

        protected override void Jouer(float dt)
        {
            if (m_Chk > 0f && Time.time >= m_Chk)
            {
                m_Chk = -1f;
                int n = Combat.Ennemis(Pos, H.transform.forward, B.epeePortee, B.epeeDemiAngle).Count;
                if (n > 0) Banc.Usage("epee_cibles" + n);
                else if (m_ChkCible == null || !m_ChkCible.Vivant) Banc.Usage("epee_vide_mort");
                else
                {
                    Vector3 v = BancClasses.Plat(m_ChkCible.transform.position - Pos);
                    Banc.Usage(v.magnitude > B.epeePortee + 0.4f ? "epee_vide_loin" : Vector3.Angle(H.transform.forward, v) > B.epeeDemiAngle ? "epee_vide_angle" : "epee_vide_autre");
                }
            }
            m_Cible = CibleMelee(m_Cible);
            var c = m_Cible;
            if (c == null) { Visee = null; Bouger(Maison, 0.8f); return; }
            float d = D(c.transform.position, Pos);
            Visee = () => c != null ? c.transform.position + Vector3.up * 1.1f : Pos + H.transform.forward * 5f;
            if (Libre)
            {
                var b = B;
                if (RecuDepuisSoin >= 0.25f * H.Sante.pvMax && Pret(3)) { RecuDepuisSoin = 0f; Appuyer("rightShoulder", "soin"); return; }
                if (Pret(2) && d >= (Bon ? 4f : 2.5f) && d <= b.chargeDistance + 0.5f) { Appuyer("leftShoulder", "charge"); return; }
                if (d <= b.epeePortee - 0.3f) { Appuyer("rightTrigger", "epee"); m_Chk = Time.time + 0.42f; m_ChkCible = c; }
            }
            if (d <= B.epeePortee - 0.6f) Arret(); else Bouger(Approche(c), 0.4f);
        }
    }

    // ===================================================================== Viking

    /// Hache ; Furie dès que la rage est pleine (R3) ; rugissement s'il y a 4 (moyen) ou 3 (bon) squelettes à 10 m ; saut sur
    /// le groupe le plus dense à portée de bond (3 ou 2 squelettes, ou le seul présent) ; tournante dès 3 squelettes à
    /// 2,3 m, arrêtée sous 2 (ou par ses 3 s de maintien). Compétences gratuites, limitées par leur recharge (03/10/2026).
    class BotViking : Bot
    {
        Squelette m_Cible;
        public BotViking() { Melee = true; NomsEmplacements = new[] { "hache", "tournante", "rugissement", "saut" }; }

        protected override void Jouer(float dt)
        {
            var b = B;
            m_Cible = CibleMelee(m_Cible);
            var c = m_Cible;
            if (Actif(1))
            {
                int n = Compter(Pos, b.tournanteRayon + 0.3f);
                Vector3 centre = Centre(Pos, 4f);
                Visee = () => centre + Vector3.up;
                if (n < 2) { EntreesSimulees.Maintenir("leftTrigger", false); ProchaineAction = Time.time + Reaction; }
                Bouger(c != null ? c.transform.position : centre, 0.8f);
                return;
            }
            if (c == null) { Visee = null; Bouger(Maison, 0.8f); EntreesSimulees.Maintenir("leftTrigger", false); return; }
            float d = D(c.transform.position, Pos);
            Visee = () => c != null ? c.transform.position + Vector3.up * 1.1f : Pos + H.transform.forward * 5f;
            if (Libre)
            {
                // Furie : déclenchée dès que la rage est pleine et qu'un squelette est à portée du combat (R3).
                if (H.Classe.UltimePret) { Appuyer("rightStickPress", "furie"); return; }
                if (Pret(2) && Compter(Pos, b.rugissementRayon) >= (Bon ? 3 : 4)) { Appuyer("leftShoulder", "rugissement"); return; }
                if (Pret(3))
                {
                    Squelette best = null; int bn = 0;
                    foreach (var s in V)
                    {
                        Vector3 v = BancClasses.Plat(s.transform.position - Pos);
                        float l = v.magnitude;
                        if (l < 3f || l > 8f) continue;
                        Vector3 atterrit = Pos + v / l * (b.sautDistance + 1f);
                        if (D(atterrit, Nyx) > Laisse + 1f) continue;
                        int k = Compter(atterrit, b.sautRayon);
                        if (k > bn) { bn = k; best = s; }
                    }
                    if (best != null && (bn >= (Bon ? 2 : 3) || V.Count == 1))
                    {
                        var cible = best;
                        Visee = () => cible != null ? cible.transform.position + Vector3.up : Pos + H.transform.forward * 5f;
                        Appuyer("rightShoulder", "saut");
                        return;
                    }
                }
                if (Pret(1) && Compter(Pos, b.tournanteRayon) >= 3) { EntreesSimulees.Maintenir("leftTrigger", true); Banc.Usage("appui_tournante"); ProchaineAction = Time.time + 0.3f; return; }
                if (d <= b.hachePortee - 0.2f) Appuyer("rightTrigger", "hache");
            }
            if (d <= b.hachePortee - 0.6f) Arret(); else Bouger(Approche(c), 0.4f);
        }
    }

    // ===================================================================== Mage

    /// Poste à 4,5 m de Nyxessa. Mur de flammes en travers d'un groupe qui approche (4 ou 3 squelettes à 4–14 m) ; grande
    /// boule sur le groupe le plus fourni dans ses 5 m (3 ou 2) ; cône dès 3 squelettes dedans et 40 (moyen) ou 20 (bon)
    /// de mana, arrêté sous 2 cibles ou à mana vide ; boule de feu sur le groupe le plus fourni à 30 m.
    class BotMage : Bot
    {
        Erreur m_Err;
        Squelette m_CibleTir;
        public BotMage() { NomsEmplacements = new[] { "boule", "cone", "grande_boule", "mur" }; }

        int DansCone(Vector3 dir)
        {
            var b = B; int n = 0;
            foreach (var s in V)
            {
                Vector3 v = BancClasses.Plat(s.transform.position - Pos);
                if (v.magnitude > b.conePortee + 0.4f) continue;
                if (v.sqrMagnitude > 0.04f && Vector3.Angle(dir, v) > b.coneDemiAngle + 5f) continue;
                n++;
            }
            return n;
        }

        protected override void Jouer(float dt)
        {
            var b = B;
            Bouger(PosteDistance, 0.8f);
            if (Actif(1))
            {
                Vector3 dir = BancClasses.Plat(H.AvantCamera);
                int n = DansCone(dir);
                Vector3 c = Centre(Pos + dir * 3f, 3.5f);
                Visee = () => c + Vector3.up;
                if (n < 2 || Jauge <= 1f) { EntreesSimulees.Maintenir("leftTrigger", false); ProchaineAction = Time.time + Reaction; }
                return;
            }
            if (H.Classe.Occupe)
            {
                // Boule ou grande boule en préparation : la visée suit la cible jusqu'au départ.
                var ct = m_CibleTir;
                if (ct != null && ct.Vivant) { var e = m_Err; float v = m_Grande ? b.grandeBouleVitesse : b.bouleVitesse; float dl = m_Grande ? b.grandeBouleInstant : b.bouleInstant; Visee = () => PointSur(ct, 1f, v, dl * 0.5f, e); }
                return;
            }
            if (!Libre) return;
            float mana = Jauge;
            // Mur de flammes.
            if (Pret(3) && mana >= b.murMana)
            {
                Vector3 som = Vector3.zero; int k = 0;
                foreach (var s in V) { float d = D(s.transform.position, Pos); if (d >= 4f && d <= 14f) { som += s.transform.position; k++; } }
                if (k > 0)
                {
                    Vector3 dir = BancClasses.Plat(som / k - Pos).normalized;
                    int n = 0;
                    foreach (var s in V)
                    {
                        Vector3 v = BancClasses.Plat(s.transform.position - Pos);
                        float le = Vector3.Dot(v, dir);
                        if (le < 3f || le > 14f) continue;
                        if ((v - dir * le).magnitude <= b.murLongueur * 0.5f + 0.5f) n++;
                    }
                    if (n >= (Bon ? 3 : 4))
                    {
                        Vector3 cible = Pos + dir * 8f;
                        Visee = () => cible;
                        Appuyer("rightShoulder", "mur");
                        return;
                    }
                }
            }
            // Grande boule.
            if (Pret(2) && mana >= b.grandeBouleMana)
            {
                var s = PlusDense(Mathf.Min(b.boulePortee, b.grandeBoulePortee), b.grandeBouleRayon, out int n);
                if (s != null && n >= (Bon ? 2 : 3) && D(s.transform.position, Pos) > 3f)
                {
                    m_CibleTir = s; m_Err = Tirer(1.4f); m_Grande = true;
                    var e = m_Err;
                    Visee = () => PointSur(s, 1f, b.grandeBouleVitesse, b.grandeBouleInstant, e);
                    Appuyer("leftShoulder", "grande_boule");
                    return;
                }
            }
            // Cône.
            if (mana >= (Bon ? 20f : 40f))
            {
                Vector3 meilleure = Vector3.zero; int bn = 0;
                foreach (var s in V)
                {
                    Vector3 v = BancClasses.Plat(s.transform.position - Pos);
                    if (v.magnitude > b.conePortee || v.sqrMagnitude < 0.04f) continue;
                    int n = DansCone(v.normalized);
                    if (n > bn) { bn = n; meilleure = v.normalized; }
                }
                if (bn >= 3)
                {
                    Vector3 c = Pos + meilleure * 3f;
                    Visee = () => c + Vector3.up;
                    EntreesSimulees.Maintenir("leftTrigger", true);
                    Banc.Usage("appui_cone");
                    ProchaineAction = Time.time + 0.3f;
                    return;
                }
            }
            // Boule de feu.
            if (Time.time - m_DerniereBoule >= b.bouleIntervalle + 0.02f)
            {
                var s = PlusDense(b.boulePortee, b.bouleRayon, out int n);
                if (s == null) { Visee = null; return; }
                m_CibleTir = s; m_Err = Tirer(1.4f); m_Grande = false;
                var e = m_Err;
                Visee = () => PointSur(s, 1f, b.bouleVitesse, b.bouleInstant, e);
                Appuyer("rightTrigger", "boule");
                m_DerniereBoule = Time.time;
            }
        }

        bool m_Grande;
        float m_DerniereBoule = -99f;
    }

    // ===================================================================== Rôdeur

    /// Poste à 4,5 m de Nyxessa ; cible : ce qui frappe Nyxessa, sinon le plus proche d'elle, à 40 m au plus. Charge
    /// complète (moyen 70 %, bon 95 % des tirs ; sinon 50 %) visée à la tête avec l'avance ; tir rapide si un squelette
    /// est à 3 m ; nuée dès qu'elle couvre 2 squelettes (1 s'ils ne sont qu'un ou deux à 25 m ; moyen : sur la cible,
    /// bon : sur le groupe le plus dense) ; roulade et salve quand un squelette est à 5 m (moyen) ou qu'une cible est à
    /// 20 m (bon).
    class BotRodeur : Bot
    {
        enum Etat { Libre, Bande, Relache }
        Etat m_Etat;
        float m_Debut, m_Tenue, m_Relache;
        Squelette m_Cible;
        Erreur m_Err;
        bool m_Rapide;
        public BotRodeur() { NomsEmplacements = new[] { "arc", "visee", "nuee", "roulade" }; }

        protected override void Jouer(float dt)
        {
            var b = B;
            Bouger(PosteDistance, 0.8f);
            switch (m_Etat)
            {
                case Etat.Bande:
                {
                    if (m_Cible == null || !m_Cible.Vivant) { m_Cible = CibleDistance(40f); m_Err = Tirer(); }
                    var c = m_Cible; var e = m_Err;
                    float charge = Mathf.Clamp01(m_Tenue / b.arcCharge);
                    float v = Mathf.Lerp(b.arcVitesseMin, b.arcVitesseMax, charge);
                    float h = m_Rapide || !Bon ? 1.0f : -1f;   // bon : la tête ; moyen : le torse
                    Visee = c != null ? (System.Func<Vector3>)(() => PointSur(c, h, v, 0.03f, e)) : null;
                    if (Time.time - m_Debut >= m_Tenue)
                    {
                        EntreesSimulees.Maintenir("rightTrigger", false);
                        m_Etat = Etat.Relache; m_Relache = Time.time;
                    }
                    return;
                }
                case Etat.Relache:
                    if (Time.time - m_Relache > 0.06f && !H.Classe.Occupe) { m_Etat = Etat.Libre; ProchaineAction = Time.time + Reaction; }
                    return;
            }
            if (!Libre) return;
            var cible = CibleDistance(40f);
            // Roulade et salve.
            if (Pret(3) && H.Endurance >= b.rouladeCout)
            {
                Squelette s = null;
                if (!Bon) { foreach (var x in V) if (D(x.transform.position, Pos) <= 5f) { s = x; break; } }
                else s = PlusDense(20f, 3f, out _);
                if (s != null)
                {
                    var e = Tirer(2f);
                    Visee = () => PointSur(s, 1f, b.salveVitesse, 0.1f, e);
                    Viser(Visee());
                    Appuyer("rightShoulder", "roulade");
                    Banc.NoterSalve();
                    return;
                }
            }
            // Nuée.
            if (Pret(2))
            {
                int proches = 0; foreach (var x in V) if (D(x.transform.position, Pos) <= b.nueePortee) proches++;
                Squelette s; int n;
                if (Bon) s = PlusDense(b.nueePortee, b.nueeRayon, out n);
                else { s = cible != null && D(cible.transform.position, Pos) <= b.nueePortee ? cible : null; n = s != null ? Compter(s.transform.position, b.nueeRayon) : 0; }
                if (s != null && (n >= 2 || (proches <= 2 && n >= 1)))
                {
                    var e = Tirer(1.5f);
                    float avance = 0.55f + 0.35f + 0.5f;
                    Visee = () => { var p = PointSur(s, 0.05f, 0f, avance, e); return p; };
                    Appuyer("leftShoulder", "nuee");
                    return;
                }
            }
            if (cible == null) { Visee = null; return; }
            // Bander : tir rapide de près, sinon charge complète (ou à moitié).
            m_Cible = cible;
            m_Err = Tirer();
            bool pres = false; foreach (var x in V) if (D(x.transform.position, Pos) <= 3f) { pres = true; break; }
            m_Rapide = pres;
            if (pres) m_Tenue = 0.3f;
            else if (Hasard() < (Bon ? 0.95 : 0.70)) m_Tenue = b.arcCharge + (Bon ? 0.12f : 0.25f);
            else m_Tenue = b.arcCharge * 0.5f + 0.06f;
            EntreesSimulees.Maintenir("rightTrigger", true);
            Banc.Usage("appui_arc");
            m_Debut = Time.time;
            m_Etat = Etat.Bande;
            var c2 = m_Cible; var e2 = m_Err; float h2 = pres || !Bon ? 1f : -1f;
            Visee = () => PointSur(c2, h2, b.arcVitesseMax, 0f, e2);
        }
    }

    // ===================================================================== Assassin

    /// Dague (cible, angle du dos et exécution comme ClasseAssassin.PorterDague) ; furtif hors combat en marchant ;
    /// grenade dans la mêlée (3 squelettes à 5 m) ; Pas de l'ombre derrière une cible à 2–9 m ; arbalète sur ce qui
    /// approche quand rien n'est dans la laisse. Moyen : bond et grenade une fois sur deux, va droit sur sa cible ; bon :
    /// toujours, contourne sa cible pour la prendre de dos, change de cible après un coup sur un squelette qui frappe
    /// Nyxessa, achève d'abord ce qui est sous 30 %.
    class BotAssassin : Bot
    {
        Squelette m_Cible;
        int m_CoupsCible;
        float m_RefusBond, m_RefusGrenade;
        enum Etat { Libre, Arbalete }
        Etat m_Etat;
        float m_DebutArbalete; bool m_Tire;
        Squelette m_CibleTir; Erreur m_Err;
        public BotAssassin() { NomsEmplacements = new[] { "dague", "arbalete", "grenade", "pas_ombre" }; }

        protected override float Bonus(Squelette s, float dH) => Bon && s.Executable && dH < 6f ? -30f : 0f;

        public override void SurCoupDague(Sante cible)
        {
            if (m_Cible != null && cible == m_Cible.Sante) m_CoupsCible++;
        }

        float m_ViseeFine;

        protected override void Jouer(float dt)
        {
            var b = B;
            Melee = m_Etat != Etat.Arbalete && Time.time >= m_ViseeFine;
            if (m_Etat == Etat.Arbalete)
            {
                Arret();
                var ct = m_CibleTir; var e = m_Err;
                if (ct != null && ct.Vivant) Visee = () => PointSur(ct, -1f, b.arbaleteVitesse, 0.03f, e);
                float t = Time.time - m_DebutArbalete;
                if (!m_Tire && t >= 0.35f + Reaction && ct != null && ct.Vivant && Pret(1) == false && H.Classe.Emplacement(1, out float rest, out _) == EtatEmplacement.Actif)
                { EntreesSimulees.Appui("rightTrigger", 0.1f); Banc.Usage("appui_arbalete"); m_Tire = true; m_DebutArbalete = Time.time - 0.35f - Reaction; }
                if ((m_Tire && t >= 0.35f + Reaction + 0.45f) || t > 3f || ct == null || !ct.Vivant)
                { EntreesSimulees.Maintenir("leftTrigger", false); m_Etat = Etat.Libre; ProchaineAction = Time.time + Reaction; }
                return;
            }
            var avant = m_Cible;
            m_Cible = CibleMelee(m_Cible);
            // Bon : après un coup sur un squelette qui frappe Nyxessa, passer à un autre (la riposte vient au deuxième).
            if (Bon && m_Cible != null && m_Cible == avant && m_CoupsCible >= 1 && m_Cible.SurNyxessa && !m_Cible.Executable)
            {
                foreach (var s in V)
                    if (s != m_Cible && s.SurNyxessa && D(s.transform.position, Pos) <= 4f) { m_Cible = s; break; }
            }
            if (m_Cible != avant) m_CoupsCible = 0;
            var c = m_Cible;
            if (c == null)
            {
                Visee = null;
                // Rien dans la laisse : arbalète sur ce qui approche.
                var loin = CibleDistance(b.arbaletePortee);
                if (loin != null && Libre && H.Classe.Emplacement(1, out float restant, out _) != EtatEmplacement.Recharge && restant <= 0f)
                {
                    Melee = false; m_CibleTir = loin; m_Err = Tirer(); m_Etat = Etat.Arbalete; m_DebutArbalete = Time.time; m_Tire = false;
                    EntreesSimulees.Maintenir("leftTrigger", true);
                    Arret();
                    return;
                }
                Bouger(Maison, 0.8f);
                return;
            }
            float d = D(c.transform.position, Pos);
            Visee = () => c != null ? c.transform.position + Vector3.up * 1.1f : Pos + H.transform.forward * 5f;
            if (Libre)
            {
                if (Pret(2) && Time.time >= m_RefusGrenade && Compter(Pos, 5f) >= 3)
                {
                    if (Bon || Hasard() < 0.5)
                    {
                        Vector3 centre = Centre(Pos, 5f);
                        if (D(centre, Pos) < 2.2f) centre = Pos + BancClasses.Plat(c.transform.position - Pos).normalized * 2.5f;
                        Visee = () => centre;
                        Melee = false; m_ViseeFine = Time.time + 0.15f + 0.12f * Time.timeScale;
                        Appuyer("leftShoulder", "grenade");
                        return;
                    }
                    m_RefusGrenade = Time.time + b.grenadeRecharge;
                }
                if (Pret(3) && Time.time >= m_RefusBond && d >= 2f && d <= 9f)
                {
                    if (Bon || Hasard() < 0.5)
                    {
                        Visee = () => c != null ? c.transform.position + Vector3.up * 1f : Pos + H.transform.forward * 5f;
                        Melee = false; m_ViseeFine = Time.time + 0.15f + 0.12f * Time.timeScale;
                        Viser(Visee());
                        Appuyer("rightShoulder", "pas_ombre");
                        return;
                    }
                    m_RefusBond = Time.time + b.pasOmbreRecharge;
                }
                if (d <= b.daguePortee - 0.15f) { Appuyer("rightTrigger", "dague"); }
            }
            // Déplacement : bon, il se place dans le dos de sa cible ; moyen, il va droit sur elle.
            if (Bon)
            {
                Vector3 f = BancClasses.Plat(c.transform.forward).normalized;
                Vector3 dos = c.transform.position - f * 1.2f;
                bool dansLeDos = Combat.DansLeDos(c.transform, Pos, b.angleDos);
                if (!dansLeDos && d < 4f) Bouger(dos, 0.3f);
                else if (d <= b.daguePortee - 0.5f) Arret(); else Bouger(Approche(c), 0.4f);
            }
            else if (d <= b.daguePortee - 0.5f) Arret(); else Bouger(Approche(c), 0.4f);
        }
    }
}
