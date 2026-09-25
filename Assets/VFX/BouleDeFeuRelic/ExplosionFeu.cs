using UnityEngine;

// Fin de la boule de feu refaite dans le langage low poly (25/09/2026) ; remplace LowPolyBlast.Fire + la fumée
// FireEffect (sprites doux smoke_soft) :
// - Explosion (~0,3 s) : éclat de gemmes Feu, cœur clair au centre, orange puis rouge vers l'extérieur, freinées ;
//   gros flash VfxLumiere (Feu, grande) ; braises en gemmes projetées qui retombent (0,8-1,1 s).
// - Fumée (1,2 à 1,6 s) : bouffées à facettes (gemmes, couleurs par sommet) gris charbon et gris chaud, teinte d'ombre
//   du thème Feu au pied, qui montent en s'écartant, tournent un peu et disparaissent par la taille ; quelques braises
//   clignotent dedans au début. Pas de transparence, rien ne disparaît d'un coup.
// API : ExplosionFeu.Jouer(point, rayon, matériau) (matériau à couleurs par sommet : PortalVoxel).
public static class ExplosionFeu
{
    private static Color F(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Feu, r, d); }

    public static GemmesVolantes Jouer(Vector3 point, float rayon, Material materiau)
    {
        if (materiau == null) return null;
        GemmesVolantes g = GemmesVolantes.Creer("ExplosionFeu", materiau, 260, true);
        float r = Mathf.Max(0.5f, rayon);
        Color coeur = F(VfxRole.Coeur, new Color(1f, 0.9f, 0.4f));
        Color vif = F(VfxRole.Vif, new Color(1f, 0.38f, 0.04f));
        Color baseC = F(VfxRole.Base, new Color(0.8f, 0.12f, 0.03f));
        Color ombre = F(VfxRole.Ombre, new Color(0.29f, 0.07f, 0.02f));
        Color blanc = VfxPalette.Accent(VfxTheme.Feu, "Blanc chaud", new Color(1f, 0.96f, 0.84f));
        Color charbon = VfxPalette.Accent(VfxTheme.Feu, "Charbon", new Color(0.18f, 0.165f, 0.157f));
        Color cendre = VfxPalette.Accent(VfxTheme.Feu, "Cendre", new Color(0.416f, 0.38f, 0.353f));

        // Explosion : gemmes projetées en boule, les plus rapides (extérieur) rouges, les lentes (cœur) claires.
        for (int i = 0; i < 90; i++)
        {
            Vector3 d = Random.onUnitSphere;
            d.y = Mathf.Abs(d.y) * 0.8f + 0.1f;
            float v = Random.Range(0.3f, 1f);
            Color c = v < 0.35f ? Color.Lerp(blanc, coeur, v / 0.35f) * 1.6f : v < 0.7f ? Color.Lerp(coeur, vif, (v - 0.35f) / 0.35f) * 1.3f : Color.Lerp(vif, baseC, (v - 0.7f) / 0.3f);
            g.Emettre(point + d * 0.1f, d * v * r * 7f, Mathf.Lerp(0.2f, 0.09f, v) * Mathf.Clamp(r / 2.5f, 0.6f, 1.4f), Random.Range(0.25f, 0.35f), c,
                0f, 9f, 0.03f, 0.25f);
        }
        // Braises : petites gemmes claires projetées qui retombent.
        for (int i = 0; i < 22; i++)
        {
            Vector3 d = Random.onUnitSphere;
            d.y = Mathf.Abs(d.y) + 0.3f;
            g.Emettre(point, d.normalized * Random.Range(3f, 6f), Random.Range(0.03f, 0.05f), Random.Range(0.8f, 1.1f),
                (Random.value < 0.5f ? coeur : vif) * 1.5f, 9.8f, 0.8f, 0.02f, 0.6f, new Vector3(0.6f, 0.6f, 1.3f));
        }
        // Fumée à facettes : bouffées qui naissent pendant l'explosion, montent en s'écartant et s'éteignent par la taille.
        for (int i = 0; i < 46; i++)
        {
            Vector3 d = Random.insideUnitSphere * r * 0.45f;
            d.y = Mathf.Abs(d.y) * 0.5f;
            Vector3 dehors = new Vector3(d.x, 0f, d.z).normalized;
            float bas = Mathf.Clamp01(1f - d.y / (r * 0.25f));
            float k = Random.value;
            Color c = k < 0.45f ? charbon : k < 0.85f ? cendre : Color.Lerp(charbon, cendre, 0.5f);
            if (bas > 0.6f && Random.value < 0.5f) c = Color.Lerp(c, ombre, 0.6f);   // teinte d'ombre du feu au pied
            g.Emettre(point + d, Vector3.up * Random.Range(0.9f, 1.5f) + dehors * Random.Range(0.4f, 0.8f), Random.Range(0.11f, 0.2f) * Mathf.Clamp(r / 2.5f, 0.6f, 1.4f),
                Random.Range(1.2f, 1.6f), c, -0.15f, 0.8f, 0.25f, 0.35f,
                new Vector3(Random.Range(0.9f, 1.3f), Random.Range(0.7f, 1f), Random.Range(0.9f, 1.3f)), Random.Range(0.05f, 0.2f));
        }
        // Braises qui clignotent dans la fumée au début.
        for (int i = 0; i < 14; i++)
        {
            Vector3 d = Random.insideUnitSphere * r * 0.4f;
            d.y = Mathf.Abs(d.y) * 0.6f + 0.2f;
            g.Emettre(point + d, Vector3.up * Random.Range(0.6f, 1f), Random.Range(0.025f, 0.04f), Random.Range(0.15f, 0.3f),
                coeur * 1.8f, 0f, 1f, 0.02f, 0.4f, default(Vector3), Random.Range(0.15f, 0.6f));
        }
        VfxLumiere.Eclat(point + Vector3.up * 0.3f, VfxTheme.Feu, VfxTailleLumiere.Grande, 0.18f);
        return g;
    }
}
