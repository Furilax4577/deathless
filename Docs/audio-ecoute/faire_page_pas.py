"""Page d'écoute des pas v2 (bois, sable, métal) : Docs/audio-ecoute/pas-v2.html, un seul fichier autonome (WAV en base64).

Contenu : pour chaque matière, les 5 variantes v2 (lecteur + mesures), la v1 actuellement jouée par le jeu (pour comparer), des repères de niveau (herbe_1, terre_1, pierre_4 : mêmes lecteurs) et un bouton « Marcher » qui
rejoue la marche comme le jeu (variantes tirées sans répéter la dernière, hauteur +-6 %, un pas toutes les 0,38 s).
Statut : à écouter. Les mesures viennent de synth_pas_v2.mesures (numpy) : l'interpréteur de Blender, comme synth_pas_v2.py :
  C:/Users/Furilax/Tools/blender-5.2.2-windows-x64/blender.exe -b --factory-startup --python faire_page_pas.py --
"""
import base64
import html
import os
import sys

ICI = os.path.dirname(os.path.abspath(__file__))
PAS = os.path.normpath(os.path.join(ICI, "..", "..", "Assets", "Audio", "Deathless", "Pas"))
sys.path.insert(0, PAS)
import synth_pas_v2 as sp  # noqa: E402

MATIERES = (("bois", "Bois", "Ponts, planchers, pièces entrables : un toc boisé sourd, bref, un grincement très discret sur 2 variantes."),
            ("sable", "Sable", "La lande : un froissement granuleux doux et court, sans sifflement."),
            ("metal", "Métal", "Enclume, seau, barres de fer : un tchank sourd de plaque ou de botte ferrée, résonance courte et douce, pas de cloche."))
REPERES = (("herbe_1", "herbe (repère de niveau)"), ("terre_1", "terre (repère)"), ("pierre_4", "pierre, Kenney (repère)"))


def b64(chemin):
    with open(chemin, "rb") as f:
        return base64.b64encode(f.read()).decode("ascii")


def ligne(titre, data_b64, m, cle):
    return ('<tr><td class="n">%s</td><td><audio controls preload="auto" src="data:audio/wav;base64,%s" data-cle="%s"></audio></td>'
            '<td>%.0f ms</td><td>%.1f dB</td><td>%.0f Hz</td><td>%.1f ms</td></tr>\n'
            % (html.escape(titre), data_b64, cle, m["duree_ms"], m["rms50_db"], m["centroide_hz"], m["attaque_ms"]))


def main():
    out = []
    for mat, nom, desc in MATIERES:
        out.append('<section><h2>%s <span class="badge">à écouter</span></h2><p>%s</p>' % (nom, html.escape(desc)))
        out.append('<p><button class="marche" data-mat="%s">Marcher (v2, 12 pas)</button> '
                   '<button class="marche" data-mat="%s" data-v1="1">Marcher (v1, jouée par le jeu)</button> '
                   '<button class="marche" data-mat="herbe" data-rep="1">Marcher sur l\'herbe (repère)</button></p>' % (mat, mat))
        out.append('<table><tr><th></th><th>Écoute</th><th>Durée</th><th>Niveau perçu</th><th>Centroïde</th><th>Attaque</th></tr>')
        for k in range(1, 6):
            c = os.path.join(PAS, "%s_v2_%d.wav" % (mat, k))
            out.append(ligne("v2 en attente : %s_v2_%d" % (mat, k), b64(c), sp.mesures(sp.lire(c)), "%s:v2:%d" % (mat, k)))
        for k in range(1, 6):
            c = os.path.join(PAS, "%s_%d.wav" % (mat, k))
            out.append(ligne("v1 utilisée : %s_%d" % (mat, k), b64(c), sp.mesures(sp.lire(c)), "%s:v1:%d" % (mat, k)))
        out.append("</table></section>")
    out.append('<section><h2>Repères de niveau</h2><table><tr><th></th><th>Écoute</th><th>Durée</th><th>Niveau perçu</th><th>Centroïde</th><th>Attaque</th></tr>')
    for fich, titre in REPERES:
        c = os.path.join(PAS, fich + ".wav")
        out.append(ligne(titre, b64(c), sp.mesures(sp.lire(c)), "%s:rep:1" % fich.split("_")[0]))
    out.append("</table></section>")
    # herbe_2..5 : pour la marche de repère (herbe_1 est dans le tableau ci-dessus)
    for k in range(2, 6):
        out.append('<audio hidden preload="auto" src="data:audio/wav;base64,%s" data-cle="herbe:rep:%d"></audio>' % (b64(os.path.join(PAS, "herbe_%d.wav" % k)), k))
    page = GABARIT.replace("{{CONTENU}}", "\n".join(out))
    chemin = os.path.join(ICI, "pas-v2.html")
    with open(chemin, "w", encoding="utf-8") as f:
        f.write(page)
    print("écrit", chemin, "%d Ko" % (len(page) // 1024))


GABARIT = """<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Pas v2 : bois, sable, métal</title>
<style>
:root{--fond:#f6f3ec;--texte:#2a2620;--carte:#fff;--trait:#d8d0c0;--accent:#8a5a1f}
@media (prefers-color-scheme:dark){:root{--fond:#1d1b18;--texte:#ece6da;--carte:#26231f;--trait:#3d3830;--accent:#e0a860}}
body{margin:0 auto;background:var(--fond);color:var(--texte);font:16px/1.5 system-ui,sans-serif;padding:16px;max-width:980px}
h1{font-size:1.5rem;margin:.2em 0}h2{font-size:1.2rem;margin:1em 0 .3em}
section{background:var(--carte);border:1px solid var(--trait);border-radius:10px;padding:8px 16px 14px;margin:14px 0}
table{border-collapse:collapse;width:100%}td,th{padding:4px 8px;border-bottom:1px solid var(--trait);text-align:left;font-size:.9rem}
td.n{white-space:nowrap}audio{height:32px;max-width:100%}
.badge{font-size:.75rem;background:var(--accent);color:var(--fond);border-radius:99px;padding:2px 10px;vertical-align:middle}
button{font:inherit;padding:6px 12px;border-radius:8px;border:1px solid var(--accent);background:transparent;color:var(--texte);cursor:pointer;margin:2px 4px 2px 0}
button:hover{background:var(--accent);color:var(--fond)}
.note{font-size:.9rem;opacity:.85}
</style></head><body>
<h1>Pas v2 : bois, sable, métal <span class="badge">à écouter</span></h1>
<p class="note">03/10/2026. Les trois matières ont été refaites à partir de vrais échantillons (Kenney RPG Audio, CC0) retravaillés. Le niveau est réglé à -14 dB perçu ; les repères d'herbe, de terre et de pierre sont en bas. « Marcher » rejoue 12 pas comme en jeu (variante tirée sans répéter la dernière, hauteur ± 6 %, volume de la matière). Les mesures sont celles du fichier ; elles ne disent pas si le son est bon : c'est à l'oreille.</p>
{{CONTENU}}
<script>
const GAINS={bois:.26,sable:.28,metal:.24,herbe:.20};
let ctx=null;const tampons={};
async function charge(cle){
  if(tampons[cle])return tampons[cle];
  const el=document.querySelector('audio[data-cle="'+cle+'"]');
  if(!el)return null;
  ctx=ctx||new (window.AudioContext||window.webkitAudioContext)();
  const r=await fetch(el.src);const buf=await ctx.decodeAudioData(await r.arrayBuffer());
  return tampons[cle]=buf;
}
document.querySelectorAll('button.marche').forEach(b=>b.addEventListener('click',async()=>{
  const mat=b.dataset.mat,ver=b.dataset.rep?'rep':(b.dataset.v1?'v1':'v2');
  const bufs=[];
  for(let k=1;k<=5;k++){const t=await charge(mat+':'+ver+':'+k);if(t)bufs.push(t);}
  if(!bufs.length)return;
  if(ctx.state==='suspended')await ctx.resume();
  let dernier=-1,t0=ctx.currentTime+0.05;
  for(let i=0;i<12;i++){
    let j;do{j=Math.floor(Math.random()*bufs.length);}while(bufs.length>1&&j===dernier);dernier=j;
    const s=ctx.createBufferSource();s.buffer=bufs[j];s.playbackRate.value=1+(Math.random()*.12-.06);
    const g=ctx.createGain();g.gain.value=(GAINS[mat]||.25)*(0.85+Math.random()*.15)*3.2;
    s.connect(g).connect(ctx.destination);s.start(t0+i*.38);
  }
}));
</script>
</body></html>
"""

if __name__ == "__main__":
    main()
