using System;
using System.Collections.Generic;

// Contrôles du plan « terrasses » : appelés à chaque essai par Generer (un essai refusé est refait avec l'essai suivant de
// la même graine, toujours déterministe), et par les outils de vérification (menu de l'éditeur, banc, harnais hors Unity).
namespace Deathless.Donjon.Terrasses
{
    public sealed partial class PlanTerrasses
    {
        /// Ajoute à `sortie` un texte par règle enfreinte (rien si le plan est conforme).
        public void Valider(List<string> sortie)
        {
            // --- niveaux
            if (Niveaux < 2 || Niveaux > 3) sortie.Add("niveaux : " + Niveaux + " (2 ou 3 attendus)");
            for (int n = 1; n < Niveaux; n++)
                if (!terrasses.Exists(t => t.niveau == n)) sortie.Add("aucune terrasse au Lvl " + n);

            // --- zone d'arrivée : au moins 6 × 5 m de sol libre au rez
            if (zoneArrivee.Largeur < 6 || zoneArrivee.Profondeur < 5) sortie.Add("zone d'arrivée trop petite");
            for (int z = zoneArrivee.z0; z < zoneArrivee.z1; z++)
                for (int x = zoneArrivee.x0; x < zoneArrivee.x1; x++)
                {
                    var c = cellules[Index(x + Marge, z + Marge)];
                    if (c.genre != GenreCellule.Sol || c.sol > 0.01f || c.obstacle) { sortie.Add("zone d'arrivée encombrée en (" + x + ", " + z + ")"); x = int.MaxValue - 1; z = int.MaxValue - 1; }
                }
            foreach (var a in apparitions) if (zoneArrivee.Contient(a.pose.x, a.pose.z)) sortie.Add("apparition dans la zone d'arrivée");
            foreach (var c in coffres) if (zoneArrivee.Contient(c.pose.x, c.pose.z)) sortie.Add("coffre dans la zone d'arrivée");
            foreach (var d in declencheurs) if (zoneArrivee.Contient(d.pose.x, d.pose.z)) sortie.Add("déclencheur dans la zone d'arrivée");
            foreach (var j in joueurs) if (!zoneArrivee.Contient(j.x, j.z)) sortie.Add("point d'arrivée de joueur hors zone");

            // --- connexité : tout sol praticable est atteint depuis l'arrivée, portes ouvertes ; portes fermées, tout sauf
            // l'intérieur des pièces verrouillées ou secrètes
            var ouvert = Atteignables(false);
            var ferme = Atteignables(true);
            int ilots = 0, bas = 0;
            float hMin = Prm.hauteurLibreMin;
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                    for (int s = 0; s < 2; s++)
                    {
                        float h, haut;
                        if (!Surface(i, j, s, out h, out haut)) continue;
                        int nd = Index(i, j) * 2 + s;
                        if (!ouvert[nd]) { ilots++; continue; }
                        if (haut - h < hMin - 0.01f) bas++;
                        var c = cellules[Index(i, j)];
                        if (!ferme[nd] && !(s == 0 && Fermee(c))) ilots++;
                    }
            if (ilots > 0) sortie.Add("connexité : " + ilots + " cases de sol inaccessibles depuis l'arrivée");
            if (bas > 0) sortie.Add("hauteur libre : " + bas + " cases sous " + hMin.ToString("0.0") + " m");
            foreach (var t in terrasses)
            {
                bool ok = false;
                for (int z = t.r.z0; z < t.r.z1 && !ok; z++)
                    for (int x = t.r.x0; x < t.r.x1 && !ok; x++)
                    {
                        int i = x + Marge, j = z + Marge;
                        for (int s = 0; s < 2; s++)
                        {
                            float h, haut;
                            if (Surface(i, j, s, out h, out haut) && Math.Abs(h - t.Hauteur) < 0.01f && ferme[Index(i, j) * 2 + s]) { ok = true; break; }
                        }
                    }
                if (!ok) sortie.Add("terrasse " + t.index + " (Lvl " + t.niveau + ") inaccessible");
            }

            // --- escaliers : pente, largeur, pied et palier d'arrivée praticables
            float pente = Marche;
            pente /= Giron;
            foreach (var e in escaliers)
            {
                if (pente > 0.62f) sortie.Add("escalier " + e.index + " trop raide");
                if (e.Largeur < LargeurEscalier) sortie.Add("escalier " + e.index + " trop étroit");
                float len = e.dir == Dir.Nord || e.dir == Dir.Sud ? e.r.Profondeur : e.r.Largeur;
                if (Math.Abs(len - e.Longueur) > 0.01f) sortie.Add("escalier " + e.index + " : longueur " + len + " au lieu de " + e.Longueur);
                if (!BoutPraticable(e, true)) sortie.Add("escalier " + e.index + " : pas de sol au pied");
                if (!BoutPraticable(e, false)) sortie.Add("escalier " + e.index + " : pas de palier en haut");
            }

            // --- jamais de surplomb : tout plein haut est le plafond d'une pièce ou d'un passage fermés sur leurs côtés
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    var c = cellules[Index(i, j)];
                    if (!c.AUnHaut) continue;
                    if ((c.genre != GenreCellule.Cavite && c.genre != GenreCellule.Passage) || c.piece < 0) { sortie.Add("surplomb en (" + (i - Marge) + ", " + (j - Marge) + ")"); continue; }
                    for (int d = 0; d < 4; d++)
                    {
                        int ni = i + DX[d], nj = j + DZ[d];
                        var b = DansGrille(ni, nj) ? cellules[Index(ni, nj)] : HorsGrille();
                        if ((b.genre == GenreCellule.Cavite || b.genre == GenreCellule.Passage) && b.piece == c.piece) continue;
                        bool plein = b.genre == GenreCellule.Hors || b.genre == GenreCellule.Massif || b.sol >= c.plafond - 0.01f;
                        bool devantArche = c.genre == GenreCellule.Passage && b.genre != GenreCellule.Hors && b.genre != GenreCellule.Massif && Math.Abs(b.sol - c.sol) < 0.01f;
                        if (!plein && !devantArche) { sortie.Add("pièce " + c.piece + " ouverte sur un côté en (" + (i - Marge) + ", " + (j - Marge) + ")"); break; }
                    }
                }

            // --- pièces cachées
            foreach (var p in pieces)
            {
                if (p.plafond - p.sol < hMin - 0.01f) sortie.Add("pièce " + p.index + " trop basse");
                if (p.arche.hauteur < hMin - 0.01f || p.arche.largeur < 3f) sortie.Add("arche de la pièce " + p.index + " trop petite");
                int i = CelluleI(p.r.CentreX), j = CelluleJ(p.r.CentreZ);
                if (!ouvert[Index(i, j) * 2]) sortie.Add("pièce " + p.index + " inaccessible");
                if (p.genre != GenrePiece.Libre && ferme[Index(i, j) * 2]) sortie.Add("pièce " + p.index + " fermée mais atteignable sans l'ouvrir");
                if (p.genre == GenrePiece.Verrouillee && p.serrure == Serrure.Aucune) sortie.Add("pièce " + p.index + " verrouillée sans serrure");
                if (p.genre == GenrePiece.Secrete)
                {
                    if (p.declencheur < 0) { sortie.Add("pièce secrète " + p.index + " sans déclencheur"); continue; }
                    var de = declencheurs[p.declencheur];
                    if (de.cible != p.index) sortie.Add("déclencheur " + de.index + " : mauvaise cible");
                    float x = de.pose.x, z = de.pose.z;
                    if (de.genre == GenreDeclencheur.BoutonMural) { x += Dx(Dir4(de.pose.rotY)) * 0.5f; z += Dz(Dir4(de.pose.rotY)) * 0.5f; }
                    int di = CelluleI(x), dj = CelluleJ(z);
                    var dc = cellules[Index(di, dj)];
                    if (!ferme[Index(di, dj) * 2] || dc.piece >= 0) sortie.Add("déclencheur de la pièce " + p.index + " inatteignable sans la pièce");
                }
            }

            // --- piliers
            for (int a = 0; a < piliers.Count; a++)
            {
                var pa = piliers[a];
                if (pa.adosse) continue;
                if (zoneArrivee.Contient(pa.x, pa.z)) sortie.Add("pilier dans la zone d'arrivée");
                for (int b = a + 1; b < piliers.Count; b++)
                {
                    var pb = piliers[b];
                    if (pb.adosse) continue;
                    float dx = pa.x - pb.x, dz = pa.z - pb.z;
                    if (dx * dx + dz * dz < Prm.espacementPiliersMin * Prm.espacementPiliersMin - 0.01f) sortie.Add("piliers à moins de " + Prm.espacementPiliersMin + " m");
                }
            }

            // --- coffres
            int grands = 0;
            foreach (var c in coffres)
            {
                if (c.type == TypeCoffre.GrandCoffre) grands++;
                int nd = Index(CelluleI(c.pose.x), CelluleJ(c.pose.z)) * 2;
                bool dansPieceFermee = c.piece >= 0 && pieces[c.piece].genre != GenrePiece.Libre;
                if (!(dansPieceFermee ? ouvert[nd] : ferme[nd])) sortie.Add("coffre inaccessible en " + c.pose);
            }
            if (grands == 0) sortie.Add("aucun grand coffre");

            // --- apparitions
            if (apparitions.Count < Prm.nbApparitions) sortie.Add("apparitions : " + apparitions.Count + " / " + Prm.nbApparitions);
            float e2 = Prm.espacementApparitionsMin * Prm.espacementApparitionsMin - 0.01f;
            float da2 = Prm.distanceArriveeApparitions * Prm.distanceArriveeApparitions - 0.01f;
            for (int a = 0; a < apparitions.Count; a++)
            {
                var pa = apparitions[a].pose;
                int nd = Index(CelluleI(pa.x), CelluleJ(pa.z)) * 2;
                if (!ferme[nd]) sortie.Add("apparition " + a + " inaccessible");
                float ax = pa.x - arrivee.x, az = pa.z - arrivee.z;
                if (ax * ax + az * az < da2) sortie.Add("apparition " + a + " trop près de l'arrivée");
                for (int b = a + 1; b < apparitions.Count; b++)
                {
                    float dx = pa.x - apparitions[b].pose.x, dz = pa.z - apparitions[b].pose.z;
                    if (dx * dx + dz * dz < e2) sortie.Add("apparitions " + a + " et " + b + " trop proches");
                }
            }
        }

        static Dir Dir4(float rotY)
        {
            int q = (int)Math.Round(rotY / 90f) & 3;
            return (Dir)q;
        }

        /// Le pied (ou le haut) de l'escalier débouche sur un sol praticable à la bonne hauteur, sur toute sa largeur.
        bool BoutPraticable(Escalier e, bool pied)
        {
            Dir d = pied ? Oppose(e.dir) : e.dir;
            float h = pied ? e.Base : e.Sommet;
            int n = 0, ok = 0;
            for (int z = e.r.z0; z < e.r.z1; z++)
                for (int x = e.r.x0; x < e.r.x1; x++)
                {
                    int i = x + Marge + Dx(d), j = z + Marge + Dz(d);
                    // ne garder que la rangée du bout
                    var c0 = cellules[Index(i - Dx(d), j - Dz(d))];
                    if (c0.escalier != e.index) continue;
                    if (DansGrille(i, j) && cellules[Index(i, j)].escalier == e.index) continue;
                    n++;
                    float hh, t;
                    if (DansGrille(i, j) && (Surface(i, j, 0, out hh, out t) && Math.Abs(hh - h) < 0.01f || Surface(i, j, 1, out hh, out t) && Math.Abs(hh - h) < 0.01f)) ok++;
                }
            return n > 0 && ok * 2 >= n;
        }
    }
}
