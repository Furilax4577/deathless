# Génère le wiki statique de Deathless : pages/*.md -> site/*.html (Python 3, bibliothèque standard seulement).
# Lancer : python Wiki/build.py (ou double-clic sur Wiki/ouvrir-wiki.cmd, qui génère puis ouvre le site).
#
# Markdown pris en charge (volontairement réduit) : titres # ## ###, paragraphes, listes "- ", tableaux "| a | b |",
# encadrés "> ", gras **x**, code `x`, liens [texte](page.md), pastilles {couleur #rrggbb}, et trois étiquettes :
# {décidé}, {à confirmer} et {effet validé} (l'apparence est validée, les règles de jeu restent à fixer).
import html, io, json, os, re, datetime

ICI = os.path.dirname(os.path.abspath(__file__))
PAGES = os.path.join(ICI, "pages")
SITE = os.path.join(ICI, "site")

# Ordre du menu : (fichier sans extension, libellé court).
MENU = [
    ("index", "Accueil"),
    ("principes", "Principes"),
    ("village", "Le village"),
    ("nyxessa", "Nyxessa, la relique"),
    ("portail", "Le portail"),
    ("classes", "Classes"),
    ("commandes", "Commandes"),
    ("interface", "Interface"),
    ("effets", "Effets et couleurs"),
    ("a-decider", "À décider"),
    ("credits", "Crédits"),
]

BADGES = {
    "décidé": '<span class="badge ok">décidé</span>',
    "à confirmer": '<span class="badge wait">à confirmer</span>',
    "effet validé": '<span class="badge fx">effet validé</span>',
}


def inline(txt):
    t = html.escape(txt, quote=False)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", t)
    t = re.sub(r"\[([^\]]+)\]\(([^)#]+)\.md(#[^)]*)?\)", lambda m: '<a href="%s.html%s">%s</a>' % (m.group(2), m.group(3) or "", m.group(1)), t)
    t = re.sub(r"\[([^\]]+)\]\((https?://[^)]+)\)", r'<a href="\2">\1</a>', t)
    t = re.sub(r"\{(décidé|à confirmer|effet validé)\}", lambda m: BADGES[m.group(1)], t)
    t = re.sub(r"\{couleur (#[0-9a-fA-F]{6})\}", r'<span class="swatch" style="background:\1"></span><code>\1</code>', t)
    return t


def slug(txt):
    s = re.sub(r"\{[^}]*\}", "", txt).strip().lower()
    s = re.sub(r"[^a-z0-9àâäçéèêëîïôöùûüÿœ]+", "-", s).strip("-")
    return s or "section"


def convertir(md):
    lignes = md.replace("\r\n", "\n").split("\n")
    out, titres, i, titre_page = [], [], 0, None
    while i < len(lignes):
        l = lignes[i]
        if not l.strip():
            i += 1
            continue
        m = re.match(r"^(#{1,3}) (.+)$", l)
        if m:
            n = len(m.group(1)); txt = m.group(2).strip()
            if n == 1 and titre_page is None:
                titre_page = re.sub(r"\{[^}]*\}", "", txt).strip()
            ident = slug(txt)
            if n > 1:
                titres.append((n, re.sub(r"\{[^}]*\}", "", txt).strip(), ident))
            out.append('<h%d id="%s">%s</h%d>' % (n, ident, inline(txt), n))
            i += 1
            continue
        if l.startswith("- "):
            items = []
            while i < len(lignes) and lignes[i].startswith("- "):
                items.append("<li>%s</li>" % inline(lignes[i][2:].strip()))
                i += 1
            out.append("<ul>%s</ul>" % "".join(items))
            continue
        if l.startswith("|"):
            rows = []
            while i < len(lignes) and lignes[i].startswith("|"):
                rows.append([c.strip() for c in lignes[i].strip().strip("|").split("|")])
                i += 1
            head, body = rows[0], [r for r in rows[1:] if not re.match(r"^:?-{2,}:?$", r[0] or "--")]
            h = "".join("<th>%s</th>" % inline(c) for c in head)
            b = "".join("<tr>%s</tr>" % "".join("<td>%s</td>" % inline(c) for c in r) for r in body)
            out.append('<div class="table"><table><thead><tr>%s</tr></thead><tbody>%s</tbody></table></div>' % (h, b))
            continue
        if l.startswith("> "):
            bloc = []
            while i < len(lignes) and lignes[i].startswith("> "):
                bloc.append(lignes[i][2:].strip())
                i += 1
            out.append('<aside class="note">%s</aside>' % inline(" ".join(bloc)))
            continue
        para = []
        while i < len(lignes) and lignes[i].strip() and not re.match(r"^(#{1,3} |- |\||> )", lignes[i]):
            para.append(lignes[i].strip())
            i += 1
        out.append("<p>%s</p>" % inline(" ".join(para)))
    return titre_page or "Sans titre", titres, "\n".join(out)


CSS = """
:root{--fond:#f6f3ec;--surface:#ffffff;--encre:#1f2433;--doux:#5b5f6b;--ligne:#e2dccd;--accent:#8a6a1f;--nyx:#1e7a45;
--ok-fond:#dcf1e3;--ok:#1b6a3a;--wait-fond:#fbe8c8;--wait:#7a4a06;--fx-fond:#dde6f7;--fx:#23457a;--code:#efe9dc;color-scheme:light}
@media (prefers-color-scheme: dark){:root{--fond:#161a24;--surface:#1f2533;--encre:#f4ecd8;--doux:#b9b3a3;--ligne:#333b52;
--accent:#d9b264;--nyx:#6fd08f;--ok-fond:#1d3b2a;--ok:#9fe0b4;--wait-fond:#3d2f16;--wait:#f5c77a;--fx-fond:#1f2d47;--fx:#a9c3f0;--code:#2a3142;color-scheme:dark}}
*{box-sizing:border-box}
body{margin:0;background:var(--fond);color:var(--encre);font:16px/1.6 "Segoe UI",system-ui,sans-serif}
.cadre{display:grid;grid-template-columns:260px minmax(0,1fr);min-height:100vh}
nav{position:sticky;top:0;align-self:start;height:100vh;overflow:auto;padding:28px 20px;border-right:1px solid var(--ligne);background:var(--surface)}
.marque{font-family:Fredoka,"Trebuchet MS",sans-serif;font-weight:700;font-size:26px;letter-spacing:2px;color:var(--encre);text-decoration:none}
.sous{margin:2px 0 18px;color:var(--doux);font-size:13px}
nav input{width:100%;padding:8px 10px;border-radius:8px;border:1px solid var(--ligne);background:var(--fond);color:var(--encre);font:inherit;font-size:14px;margin-bottom:14px}
nav ul{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:2px}
nav a{display:block;padding:6px 10px;border-radius:8px;color:var(--encre);text-decoration:none;font-size:15px}
nav a:hover{background:var(--fond)}nav a.ici{background:var(--fond);font-weight:600;color:var(--accent)}
nav .res a{font-size:13px;color:var(--doux)}
main{padding:40px clamp(16px,5vw,64px) 80px;max-width:900px}
h1,h2,h3{font-family:Fredoka,"Trebuchet MS",sans-serif;line-height:1.25;text-wrap:balance}
h1{font-size:40px;margin:0 0 8px}h2{font-size:26px;margin:40px 0 10px;padding-top:8px;border-top:1px solid var(--ligne)}h3{font-size:19px;margin:26px 0 6px}
p,li{max-width:70ch}ul{padding-left:22px}
a{color:var(--accent)}
code{background:var(--code);padding:1px 6px;border-radius:5px;font-size:.9em}
.table{overflow-x:auto;margin:14px 0}table{border-collapse:collapse;min-width:100%}
th,td{text-align:left;padding:8px 12px;border-bottom:1px solid var(--ligne);vertical-align:top}
th{font-size:13px;letter-spacing:.5px;text-transform:uppercase;color:var(--doux)}
.badge{display:inline-block;padding:0 9px;border-radius:999px;font-size:12px;font-weight:600;vertical-align:2px;margin-left:4px}
.badge.ok{background:var(--ok-fond);color:var(--ok)}.badge.fx{background:var(--fx-fond);color:var(--fx)}.badge.wait{background:var(--wait-fond);color:var(--wait)}
.note{border:1px solid var(--ligne);background:var(--surface);border-radius:10px;padding:12px 16px;margin:16px 0;color:var(--doux)}
.swatch{display:inline-block;width:14px;height:14px;border-radius:4px;vertical-align:-2px;margin-right:6px;border:1px solid var(--ligne)}
.maj{margin-top:48px;color:var(--doux);font-size:13px}
@media (max-width:760px){.cadre{grid-template-columns:minmax(0,1fr)}nav{position:static;height:auto;border-right:0;border-bottom:1px solid var(--ligne)}}
"""

JS = """
(function(){
  var champ=document.getElementById('cherche'), res=document.getElementById('res');
  if(!champ) return;
  champ.addEventListener('input',function(){
    var q=champ.value.trim().toLowerCase(); res.innerHTML='';
    if(q.length<2) return;
    INDEX.filter(function(e){return e.t.toLowerCase().indexOf(q)>=0;}).slice(0,12).forEach(function(e){
      var li=document.createElement('li'); var a=document.createElement('a');
      a.href=e.u; a.textContent=e.p+' › '+e.t; li.appendChild(a); res.appendChild(li);
    });
  });
})();
"""


def main():
    os.makedirs(SITE, exist_ok=True)
    pages, index = [], []
    for nom, court in MENU:
        chemin = os.path.join(PAGES, nom + ".md")
        if not os.path.exists(chemin):
            continue
        titre, titres, corps = convertir(io.open(chemin, encoding="utf-8").read())
        pages.append((nom, court, titre, corps))
        index.append({"p": court, "t": titre, "u": nom + ".html"})
        index.extend({"p": court, "t": t, "u": "%s.html#%s" % (nom, ident)} for _, t, ident in titres)
    maj = datetime.date.today().strftime("%d/%m/%Y")
    for nom, court, titre, corps in pages:
        menu = "".join('<li><a href="%s.html"%s>%s</a></li>' % (n, ' class="ici" aria-current="page"' if n == nom else "", html.escape(c))
                       for n, c, _, _ in pages)
        doc = ('<!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
               '<title>%s · Wiki Deathless</title>'
               '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Fredoka:wght@500;700&amp;display=swap">'
               '<style>%s</style></head><body><div class="cadre"><nav aria-label="Pages du wiki">'
               '<a class="marque" href="index.html">DEATHLESS</a><p class="sous">Wiki des règles</p>'
               '<label for="cherche" class="sous" style="display:block;margin:0 0 6px">Rechercher</label>'
               '<input id="cherche" type="search" placeholder="Classe, portail, touche…" autocomplete="off">'
               '<ul class="res" id="res"></ul><ul>%s</ul></nav>'
               '<main>%s<p class="maj">Généré le %s depuis <code>Wiki/pages/%s.md</code>.</p></main></div>'
               '<script>var INDEX=%s;%s</script></body></html>'
               % (html.escape(titre), CSS, menu, corps, maj, nom, json.dumps(index, ensure_ascii=False), JS))
        io.open(os.path.join(SITE, nom + ".html"), "w", encoding="utf-8", newline="\n").write(doc)
    print("Wiki généré : %d pages dans %s" % (len(pages), SITE))


if __name__ == "__main__":
    main()
