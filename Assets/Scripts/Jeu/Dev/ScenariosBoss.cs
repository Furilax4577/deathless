using System.Collections;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Scénarios de vérification des boss en Play (30/09/2026), lancés par execute_code :
    /// ScenariosBoss.Lancer("nyxar"), ScenariosBoss.Lancer("morgrim_massue"), ScenariosBoss.Lancer("morgrim_martache").
    /// Résultats dans la console (préfixe [Boss]) et dans ScenariosBoss.Dernier ; captures Assets/Screenshots/boss_*.png.
    public class ScenariosBoss : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosBoss() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosBoss"); }

        static ScenariosBoss s_I;
        public static string Dernier = "";
        public static bool Fini;

        public static void Lancer(string nom)
        {
            if (s_I == null) s_I = new GameObject("ScenariosBoss").AddComponent<ScenariosBoss>();
            s_I.StopAllCoroutines();
            Dernier = ""; Fini = false;
            switch (nom)
            {
                case "nyxar": s_I.StartCoroutine(s_I.Nyxar()); break;
                case "morgrim_massue": s_I.StartCoroutine(s_I.Morgrim(true)); break;
                case "morgrim_martache": s_I.StartCoroutine(s_I.Morgrim(false)); break;
                case "nyxar_esquive": s_I.StartCoroutine(s_I.NyxarEsquive()); break;
            }
        }

        static void Log(string t) { Dernier += t + "\n"; Debug.Log("[Boss] " + t); }
        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;

        static void Viser(Transform cible)
        {
            var h = H;
            if (h == null || cible == null) return;
            Vector3 d = cible.position - h.transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            h.transform.rotation = Quaternion.LookRotation(d);
            Partie.Instance.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        static int Compter<T>() where T : Object => FindObjectsByType<T>(FindObjectsSortMode.None).Length;

        /// Photo de près d'un boss par une caméra temporaire (la caméra d'épaule est souvent masquée par le héros) :
        /// placée à `distance` m devant lui, un peu en biais, visant `hauteur` m ; PNG dans Assets/Screenshots.
        public static void Photo(Transform cible, string nom, float distance = 3.2f, float hauteur = 1.3f, float biais = 30f)
        {
            if (cible == null) return;
            var go = new GameObject("PhotoBoss");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 45f; cam.nearClipPlane = 0.05f;
            Vector3 vise = cible.position + Vector3.up * hauteur;
            go.transform.position = vise + Quaternion.Euler(0f, biais, 0f) * cible.forward * distance + Vector3.up * 0.4f;
            go.transform.LookAt(vise);
            var rt = new RenderTexture(900, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            var prec = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(900, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = prec;
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Assets/Screenshots");
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, nom + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            Destroy(rt); Destroy(tex); Destroy(go);
        }

        IEnumerator Nyxar()
        {
            var h = H;
            h.Sante.invulnerable = true;
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -30f));
            yield return null;
            var n = DevPartie.PoserDevant(TypeEnnemi.Necromancien, 13f) as Necromancien;
            if (n == null) { Log("Nyxar non posé"); Fini = true; yield break; }
            yield return new WaitForSeconds(3f);
            Log("posé : PV " + n.Sante.Pv.ToString("F0") + "/" + n.Sante.pvMax.ToString("F0") + ", invulnérable " + n.Sante.invulnerable
                + ", éclats " + n.PvEclats.ToString("F0") + ", phase " + n.PhaseCombat);
            // Salve : combien de crânes à la fois ?
            int maxMissiles = 0; float t = 0f;
            while (t < 4f) { t += Time.deltaTime; maxMissiles = Mathf.Max(maxMissiles, Compter<MissileCrane>()); Viser(n.transform); yield return null; }
            Log("salve : jusqu'à " + maxMissiles + " crânes en vol");
            Viser(n.transform);
            Photo(n.transform, "boss_nyxar_eclats_face", 3.2f, 1.3f, 20f);
            Photo(n.transform, "boss_nyxar_eclats_cote", 2.6f, 0.9f, -80f);
            // Coup sur le corps : dégâts normaux (01/10/2026).
            float pv0 = n.Sante.Pv;
            float r0 = h.Frapper(n.Sante, 100f, false, n.transform.position + Vector3.up, n.transform.forward);
            Log("coup de 100 sur le corps : PV " + pv0.ToString("F0") + " → " + n.Sante.Pv.ToString("F0") + " (rendu " + r0.ToString("F0") + ")");
            // Coup sur l'éclat de la couronne : critique garanti ×2 sur Nyxar, l'éclat s'use de 100.
            bool vuCritique = false; float chiffre = 0f;
            System.Action<Sante, InfoDegats, float> espion = (s, info, reel) => { if (s == n.Sante && info.critique) { vuCritique = true; chiffre = reel; } };
            Sante.AnyTouche += espion;
            pv0 = n.Sante.Pv; float pvc0 = n.Eclat(0).Sante.Pv;
            yield return null;   // autre image : pas le même geste que le coup au corps
            float r1 = h.Frapper(n.Eclat(0).Sante, 100f, false, n.Eclat(0).Position, n.transform.forward);
            Sante.AnyTouche -= espion;
            Log("coup de 100 sur la couronne : Nyxar " + pv0.ToString("F0") + " → " + n.Sante.Pv.ToString("F0") + ", éclat " + pvc0.ToString("F0") + " → "
                + n.Eclat(0).Sante.Pv.ToString("F0") + ", chiffre critique " + vuCritique + " (" + chiffre.ToString("F0") + "), rendu " + r1.ToString("F0"));
            // Coup de zone sur le corps et les deux éclats dans la même image : Nyxar ne le prend qu'une fois.
            yield return null;
            pv0 = n.Sante.Pv; pvc0 = n.Eclat(0).Sante.Pv; float pvg0 = n.Eclat(1).Sante.Pv;
            h.Frapper(n.Eclat(0).Sante, 50f, false, n.Eclat(0).Position, n.transform.forward);
            h.Frapper(n.Eclat(1).Sante, 50f, false, n.Eclat(1).Position, n.transform.forward);
            h.Frapper(n.Sante, 50f, false, n.transform.position + Vector3.up, n.transform.forward);
            Log("zone de 50 (couronne, grimoire, corps) : Nyxar −" + (pv0 - n.Sante.Pv).ToString("F0") + ", couronne −" + (pvc0 - n.Eclat(0).Sante.Pv).ToString("F0")
                + ", grimoire −" + (pvg0 - n.Eclat(1).Sante.Pv).ToString("F0"));
            // Tranche de vie : sous 2/3 des PV max, l'éclat le plus abîmé cède de lui-même.
            yield return null;
            h.Frapper(n.Sante, n.Sante.Pv - (n.Sante.pvMax * 2f / 3f - 1f), false, n.transform.position + Vector3.up, n.transform.forward);
            Log("tranche 2/3 : PV " + n.Sante.Pv.ToString("F0") + "/" + n.Sante.pvMax.ToString("F0") + ", couronne brisée " + n.CouronneBrisee
                + ", grimoire brisé " + n.GrimoireBrise + ", phase " + n.PhaseCombat);
            // Cible de mêlée préférée : l'éclat.
            var cibles = Combat.Ennemis(n.transform.position - n.transform.forward * 1.5f, n.transform.forward, 3f, 180f);
            Log("mêlée à 1,5 m : première cible " + (cibles.Count > 0 ? cibles[0].name : "aucune") + " (" + cibles.Count + " cibles)");
            // Approche : téléportation.
            Vector3 avant = n.transform.position;
            DevPartie.PlacerHeros(n.transform.position - n.transform.forward * 2.5f, n.transform.position);
            t = 0f;
            while (t < 2f && Vector3.Distance(avant, n.transform.position) < 4f) { t += Time.deltaTime; yield return null; }
            Log("approche à 2,5 m : déplacé de " + Vector3.Distance(avant, n.transform.position).ToString("F1") + " m en " + t.ToString("F2") + " s");
            yield return new WaitForSeconds(0.5f);
            DevPartie.Capturer("boss_nyxar_teleport");
            Photo(n.transform, "boss_nyxar_couronne_brisee", 3.2f, 1.3f, 20f);
            // Éclat de la couronne brisé.
            var c = n.Eclat(0);
            n.AbimerEclat(0, new InfoDegats { montant = 9999f, sourceId = 1, equipeSource = Equipe.Heros, point = c.Position, continu = true });
            Log("couronne brisée : PV " + n.Sante.Pv.ToString("F0") + "/" + n.Sante.pvMax.ToString("F0") + ", phase " + n.PhaseCombat + ", invulnérable " + n.Sante.invulnerable);
            // Collé à lui sans couronne : faux.
            DevPartie.PlacerHeros(n.transform.position - n.transform.forward * 2f, n.transform.position);
            t = 0f; bool prep = false; Vector3 p0 = n.transform.position;
            while (t < 4f) { t += Time.deltaTime; h.Sante.invulnerable = true; if (n.EtatCourant == Squelette.Etat.Preparation) prep = true; yield return null; }
            Log("collé 4 s sans couronne : faux préparée " + prep + ", déplacé de " + Vector3.Distance(p0, n.transform.position).ToString("F1") + " m, héros PV " + h.Sante.Pv.ToString("F0"));
            var g = n.Eclat(1);
            n.AbimerEclat(1, new InfoDegats { montant = 9999f, sourceId = 1, equipeSource = Equipe.Heros, point = g.Position, continu = true });
            Log("grimoire brisé : PV " + n.Sante.Pv.ToString("F0") + "/" + n.Sante.pvMax.ToString("F0") + ", phase " + n.PhaseCombat + ", enragé " + n.Enrage + ", invulnérable " + n.Sante.invulnerable);
            DevPartie.PlacerHeros(n.transform.position - n.transform.forward * 7f, n.transform.position);
            t = 0f; float dMin = 99f;
            while (t < 5f) { t += Time.deltaTime; h.Sante.invulnerable = true; dMin = Mathf.Min(dMin, Vector3.Distance(n.transform.position, h.transform.position)); yield return null; }
            Log("enragé : distance min au héros " + dMin.ToString("F1") + " m, état " + n.EtatCourant + ", vitesse " + n.Agent.speed.ToString("F1"));
            Photo(n.transform, "boss_nyxar_enrage", 4f, 1.2f, 25f);
            n.Sante.Encaisser(new InfoDegats { montant = 99999f, sourceId = 1, equipeSource = Equipe.Heros, point = n.transform.position + Vector3.up });
            Log("coup fatal : mort " + n.Sante.Mort);
            h.Sante.invulnerable = false;
            Fini = true;
        }

        /// Crânes de Nyxar (01/10/2026) : écart dans la salve, puis touchés immobile contre touchés avec un pas de côté
        /// (3 m en 0,5 s, lancé quand un crâne arrive à moins de `declenche` m).
        IEnumerator NyxarEsquive()
        {
            var h = H;
            h.Sante.Fixer(99999f, 99999f);
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -30f));
            yield return null;
            var n = DevPartie.PoserDevant(TypeEnnemi.Necromancien, 14f) as Necromancien;
            if (n == null) { Log("Nyxar non posé"); Fini = true; yield break; }
            int touches = 0;
            System.Action<InfoDegats, float> compte = (info, reel) => { if (info.aDistance && info.source == n.gameObject) touches++; };
            h.Sante.Touche += compte;
            var vus = new System.Collections.Generic.HashSet<int>();
            var departs = new System.Collections.Generic.List<float>();
            for (int passe = 0; passe < 2; passe++)
            {
                bool esquive = passe == 1;
                int t0 = touches, v0 = vus.Count;
                float t = 0f, finPas = -1f; Vector3 pas = Vector3.zero;
                while (t < 16f)
                {
                    t += Time.deltaTime;
                    Viser(n.transform);
                    foreach (var m in FindObjectsByType<MissileCrane>(FindObjectsSortMode.None))
                    {
                        if (m.name != "MissileNecromancien") continue;
                        if (vus.Add(m.GetInstanceID())) departs.Add(Time.time);
                        Vector3 d = h.transform.position + Vector3.up - m.transform.position;
                        if (esquive && finPas < 0f && d.magnitude < 4.5f && Vector3.Dot(d, m.transform.forward) > 0f)
                        {
                            pas = Vector3.Cross(Vector3.up, m.transform.forward).normalized * 6f;
                            finPas = 0.5f;
                        }
                    }
                    if (finPas > 0f) { h.CC.Move(pas * Time.deltaTime); finPas -= Time.deltaTime; if (finPas <= 0f) finPas = -0.4f; }
                    else if (finPas < -0.01f) { finPas += Time.deltaTime; if (finPas >= -0.01f) finPas = -1f; }
                    // Reste à distance de Nyxar (pas de faux ni de téléportation) : recentré entre deux passes seulement.
                    yield return null;
                }
                Log((esquive ? "avec pas de côté" : "immobile") + " 16 s : " + (vus.Count - v0) + " crânes, " + (touches - t0) + " touchés");
                DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), n.transform.position);
            }
            float ecartMin = 99f;
            for (int i = 1; i < departs.Count; i++) { float e = departs[i] - departs[i - 1]; if (e > 0.05f) ecartMin = Mathf.Min(ecartMin, e); }
            Log("écart minimal entre deux crânes d'une salve : " + ecartMin.ToString("F2") + " s ; vitesse " + GameBalance.Courant.necroVitesseMissile + " m/s");
            h.Sante.Touche -= compte;
            Fini = true;
        }

        IEnumerator Morgrim(bool massue)
        {
            var h = H;
            h.Sante.invulnerable = true;
            DevPartie.PlacerHeros(new Vector3(-9f, 0f, -9f), new Vector3(-30f, 0f, -30f));
            yield return null;
            DirecteurVagues.Instance.ChoisirMorgrim(massue);
            var m = DevPartie.PoserDevant(TypeEnnemi.Golem, 6f) as MorgrimVariant;
            for (int i = 0; i < 4; i++) DevPartie.PoserDevant(TypeEnnemi.Sbire, 8f, -3f + i * 2f);
            if (m == null) { Log("Morgrim non posé"); Fini = true; yield break; }
            Log("posé : " + m.GetType().Name + ", PV " + m.Sante.Pv.ToString("F0"));
            float t = 0f; int galva = 0, ondes = 0; float pvMin = h.Sante.Pv; bool renverse = false;
            while (t < 30f)
            {
                t += Time.deltaTime;
                Viser(m.transform);
                ondes = Mathf.Max(ondes, Compter<OndeChocLente>());
                var dv = DirecteurVagues.Instance;
                int g = 0;
                foreach (var s in dv.Vivants) if (s != null && s.Statuts != null && s.Statuts.A(TypeStatut.Galvanise)) g++;
                if (g > galva) { galva = g; Photo(m.transform, "boss_morgrim_cri", 8f, 1.5f, 20f); }
                h.Sante.invulnerable = true;
                if (h.Statuts != null && h.Statuts.A(TypeStatut.Renverse)) renverse = true;
                yield return null;
            }
            Log("30 s : squelettes galvanisés (max) " + galva + ", ondes vues " + ondes + ", héros renversé " + renverse);
            h.Sante.invulnerable = false;
            Fini = true;
        }
    }
}
