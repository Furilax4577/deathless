# Génère le wiki statique de Deathless en deux versions (Python 3, bibliothèque standard seulement) :
#   site/    version développeur, complète ;
#   public/  version joueur : seulement les règles décidées, sans notes techniques ni pistes.
# Lancer : python Wiki/build.py (ou double-clic sur Wiki/ouvrir-wiki.cmd / Wiki/ouvrir-wiki-joueur.cmd).
#
# Filtrage de la version joueur :
#   - pages marquées "dev" dans MENU : absentes ;
#   - {dev} dans une ligne, une ligne de tableau ou une cellule d'en-tête (colonne entière) : absent ;
#     dans un titre de section : toute la section est absente ; {{dev: texte}} en ligne : absent ;
#   - {public} : ligne présente seulement dans la version joueur ;
#   - tout ce qui porte {à confirmer} (ligne, ligne de tableau, section) : absent ;
#   - étiquettes {décidé} et {effet validé} retirées, {à équilibrer} devient « valeurs provisoires » ;
#   - liens vers une page absente : remplacés par leur texte ; sections et tableaux vidés : retirés.
#
# Markdown pris en charge (volontairement réduit) : titres # ## ###, paragraphes, listes "- " et sous-listes "  - ", tableaux "| a | b |",
# encadrés "> ", gras **x**, code `x`, liens [texte](page.md), pastilles {couleur #rrggbb}, et trois étiquettes :
# {décidé}, {à confirmer} et {effet validé} (l'apparence est validée, les règles de jeu restent à fixer).
# Balises de catalogue, seules sur leur ligne : {catalogue sons} (un tableau par catégorie avec lecteurs audio, filtres),
# {sons à écouter} (encart : liens vers les sons créés qui attendent l'écoute de Quentin) et {sons à créer} (sons
# nécessaires qui n'existent pas), lues dans data/sons.json ; les fichiers audio sont copiés de Assets/Audio/ vers
# site/sons/ à chaque génération (seulement s'ils ont changé).
# Vidéos : une ligne qui commence par {video chemin} (chemin relatif à Wiki/, ex. media/animations/Idle_A.mp4) devient une
# carte vidéo (lecture automatique en boucle, muette, chargée seulement quand elle arrive à l'écran) ; le texte qui suit la
# balise est la légende, en parties séparées par " | " (une ligne chacune). Les lignes {video} consécutives forment une
# grille. Le fichier est copié dans site/videos/ ; la version joueur n'a jamais de vidéo (balise retirée).
import html, io, json, os, re, datetime, shutil, unicodedata, urllib.parse

ICI = os.path.dirname(os.path.abspath(__file__))
PAGES = os.path.join(ICI, "pages")
SITE = os.path.join(ICI, "site")
PUBLIC = os.path.join(ICI, "public")
DATA = os.path.join(ICI, "data")
AUDIO = os.path.normpath(os.path.join(ICI, "..", "Assets", "Audio"))
SITE_SONS = os.path.join(SITE, "sons")
ICONES = os.path.normpath(os.path.join(ICI, "..", "ArtSources", "Icones"))
ICONES_COPIEES = set()  # icônes SVG référencées par {icone nom}, copiées dans <version>/icones/
VIDEOS_COPIEES = set()  # vidéos référencées par {video chemin} (chemins relatifs à media/), copiées dans site/videos/

# Ordre du menu : (fichier sans extension, libellé court[, options]). Options, séparées par des espaces : "dev" si la page
# est réservée à la version développeur, "sous" pour une sous-page (affichée en retrait sous la page qui la précède).
MENU = [
    ("index", "Accueil"),
    ("univers", "L'univers"),
    ("principes", "Principes"),
    ("deroule", "Déroulé d'une partie"),
    ("village", "Le village"),
    ("nyxessa", "Nyxessa, la relique"),
    ("portail", "Le portail"),
    ("classes", "Classes"),
    ("classe-paladin", "Paladin", "sous"),
    ("classe-mage", "Mage", "sous"),
    ("classe-rodeur", "Rôdeur", "sous"),
    ("classe-assassin", "Assassin", "sous"),
    ("classe-viking", "Viking", "sous"),
    ("classe-druide", "Druide (bientôt)", "sous"),
    ("classe-mecanicien", "Mécanicien (bientôt)", "sous"),
    ("classe-barde", "Barde (bientôt)", "sous"),
    ("classe-bavaroise", "Bavaroise (bientôt)", "sous"),
    ("classe-clochard", "Clochard (bientôt)", "sous"),
    ("ennemis", "Ennemis"),
    ("commandes", "Commandes"),
    ("interface", "Interface"),
    ("effets", "Effets et couleurs", "dev"),
    ("animations", "Animations", "dev"),
    ("a-decider", "À décider", "dev"),
    ("a-faire", "À faire", "dev"),
    ("sons", "Sons", "dev"),
    ("credits", "Crédits"),
]

BADGES = {
    "décidé": '<span class="badge ok">décidé</span>',
    "à confirmer": '<span class="badge wait">à confirmer</span>',
    "effet validé": '<span class="badge fx">effet validé</span>',
    "à équilibrer": '<span class="badge tune">à équilibrer</span>',
    "provisoire": '<span class="badge tune">valeurs provisoires</span>',
    "emote possible": '<span class="badge emote">emote possible</span>',
}
PAGES_PRESENTES = None  # pages de la version en cours ; un lien vers une page absente devient du texte


def inline(txt):
    t = html.escape(txt, quote=False)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", t)
    t = re.sub(r"\[([^\]]+)\]\(([^)#]+)\.md(#[^)]*)?\)", lambda m: m.group(1) if PAGES_PRESENTES is not None and m.group(2) not in PAGES_PRESENTES
               else '<a href="%s.html%s">%s</a>' % (m.group(2), m.group(3) or "", m.group(1)), t)
    t = re.sub(r"\[([^\]]+)\]\((https?://[^)]+)\)", r'<a href="\2">\1</a>', t)
    t = re.sub(r"\{(décidé|à confirmer|effet validé|à équilibrer|provisoire|emote possible)\}", lambda m: BADGES[m.group(1)], t)
    t = re.sub(r"\{icone(-grande)? ([a-z0-9_]+)\}", icone, t)
    t = re.sub(r"\{couleur (#[0-9a-fA-F]{6})\}", r'<span class="swatch" style="background:\1"></span><code>\1</code>', t)
    return t


def icone(m):
    """{icone nom} : petite icône en ligne ; {icone-grande nom} : grande icône (en-tête d'une page de classe).
    Le SVG est pris dans ArtSources/Icones/Classes ou ArtSources/Icones/Competences, puis copié dans <version>/icones/."""
    nom = m.group(2)
    for sous in ("Classes", "Competences", "Nyxessa"):
        if os.path.exists(os.path.join(ICONES, sous, nom + ".svg")):
            ICONES_COPIEES.add((sous, nom))
            return '<img class="icone%s" src="icones/%s.svg" alt="">' % (" grande" if m.group(1) else "", nom)
    return '<span class="manque">[icône %s introuvable]</span>' % nom


def copier_icones(dossier):
    cible = os.path.join(dossier, "icones")
    os.makedirs(cible, exist_ok=True)
    for sous, nom in ICONES_COPIEES:
        shutil.copyfile(os.path.join(ICONES, sous, nom + ".svg"), os.path.join(cible, nom + ".svg"))


# ------------------------------------------------------------------ vidéos ({video chemin}, version développeur)

RE_VIDEO = re.compile(r"^\{video ([^}]+)\}\s*(.*)$")


def carte_video(m):
    """{video chemin} légende | ligne 2 | ... : carte avec une vidéo muette en boucle, chargée paresseusement (data-src,
    posée par JS_VIDEOS quand la carte approche de l'écran). Le fichier est copié plus tard dans site/videos/."""
    chemin = m.group(1).strip().replace("\\", "/")
    rel = chemin[len("media/"):] if chemin.startswith("media/") else chemin
    if not os.path.isfile(os.path.join(ICI, chemin)):
        print("Attention : vidéo absente : Wiki/%s" % chemin)
        video = '<span class="manque">vidéo absente : %s</span>' % html.escape(chemin)
    else:
        VIDEOS_COPIEES.add(rel)
        video = ('<video data-src="videos/%s" muted loop playsinline preload="none" disablepictureinpicture></video>'
                 % html.escape(urllib.parse.quote(rel), quote=True))
    parts = [p.strip() for p in m.group(2).split(" | ") if p.strip()]
    legende = "".join("<span>%s</span>" % inline(p) for p in parts)
    return '<figure class="carte-video">%s<figcaption>%s</figcaption></figure>' % (video, legende)


JS_VIDEOS = """
(function(){
  var vs=document.querySelectorAll('video[data-src]');
  if(!vs.length) return;
  function charge(v){ if(!v.src){ v.src=v.dataset.src; } var p=v.play(); if(p&&p.catch) p.catch(function(){}); }
  if(!('IntersectionObserver' in window)){ vs.forEach(charge); return; }
  var io=new IntersectionObserver(function(es){
    es.forEach(function(e){ if(e.isIntersecting) charge(e.target); else if(e.target.src) e.target.pause(); });
  },{rootMargin:'200px 0px'});
  vs.forEach(function(v){ io.observe(v); });
})();
"""


def copier_videos(dossier):
    """Copie dans <dossier>/videos/ les vidéos référencées (seulement celles qui ont changé) et retire les autres."""
    cible = os.path.join(dossier, "videos")
    copies = 0
    for rel in VIDEOS_COPIEES:
        src, dst = os.path.join(ICI, "media", rel), os.path.join(cible, rel)
        a = os.stat(src)
        if os.path.exists(dst):
            b = os.stat(dst)
            if b.st_size == a.st_size and int(b.st_mtime) == int(a.st_mtime):
                continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)
        copies += 1
    gardes = {os.path.normcase(os.path.join(cible, r)) for r in VIDEOS_COPIEES}
    for dp, dn, fn in os.walk(cible, topdown=False):
        for f in fn:
            if os.path.normcase(os.path.join(dp, f)) not in gardes:
                os.remove(os.path.join(dp, f))
        if dp != cible and not os.listdir(dp):
            os.rmdir(dp)
    if VIDEOS_COPIEES:
        print("Vidéos : %d fichiers dans %s (%d copiés)" % (len(VIDEOS_COPIEES), os.path.relpath(cible, ICI), copies))


def options(e):
    return e[2].split() if len(e) > 2 else []


def slug(txt):
    s = re.sub(r"\{[^}]*\}", "", txt).strip().lower()
    s = re.sub(r"[^a-z0-9àâäçéèêëîïôöùûüÿœ]+", "-", s).strip("-")
    return s or "section"


# ------------------------------------------------------------------ catalogue des sons (data/sons.json)

# statut -> (libellé du badge, classe CSS, libellé du filtre) ; a_ecouter : créé, pas encore validé à l'écoute.
STATUTS_SONS = {"a_ecouter": ("à écouter", "ecoute", "À écouter"), "utilise": ("utilisé", "ok", "Utilisés"),
                "disponible": ("disponible", "dispo", "Disponibles"), "a_creer": ("à créer", "wait", "À créer")}
SONS_COPIES = set()  # chemins sous Assets/Audio/ référencés par les pages, copiés dans site/sons/ en fin de génération
_sons = None


def sans_accents(txt):
    return "".join(c for c in unicodedata.normalize("NFD", txt.lower()) if not unicodedata.combining(c))


def charger_sons():
    """Lit data/sons.json (liste d'objets id, nom, categorie, usage, fichier, variantes, source, licence, statut)."""
    global _sons
    if _sons is None:
        chemin = os.path.join(DATA, "sons.json")
        try:
            _sons = json.load(io.open(chemin, encoding="utf-8"))
        except (OSError, ValueError) as e:
            raise SystemExit("Catalogue des sons illisible (%s) : %s" % (chemin, e))
        for s in _sons:
            manque = [k for k in ("id", "nom", "categorie", "statut") if not s.get(k)]
            if manque or s["statut"] not in STATUTS_SONS or (s["statut"] != "a_creer" and not s.get("fichier")):
                raise SystemExit("data/sons.json, entrée incomplète ou statut inconnu : %r" % s)
    return _sons


def lecteur(rel):
    """Lecteur audio d'un fichier de Assets/Audio/ (copié ensuite dans site/sons/)."""
    if not os.path.isfile(os.path.join(AUDIO, rel)):
        print("Attention : fichier audio absent : Assets/Audio/%s" % rel)
        return '<span class="manque">fichier absent</span>'
    SONS_COPIES.add(rel)
    return '<audio controls preload="none" src="sons/%s"></audio>' % html.escape(urllib.parse.quote(rel), quote=True)


def ligne_son(s, colonnes):
    libelle, classe = STATUTS_SONS[s["statut"]][:2]
    q = sans_accents(" ".join(str(s.get(k) or "") for k in ("nom", "categorie", "usage", "source", "id", "fichier")))
    attrs = ' id="son-%s" data-cat="%s" data-statut="%s" data-q="%s"' % (
        html.escape(s["id"], quote=True), html.escape(s["categorie"], quote=True), s["statut"], html.escape(q, quote=True))
    nom = '<strong>%s</strong> <span class="badge %s">%s</span>' % (html.escape(s["nom"]), classe, libelle)
    usage = inline(s.get("usage") or "") or "—"
    if colonnes == "a_creer":
        return "<tr%s><td>%s</td><td>%s</td><td>%s</td></tr>" % (attrs, nom, html.escape(s["categorie"]), usage)
    fichiers = s.get("variantes") or [s["fichier"]]
    if len(fichiers) > 1:
        ecoute = "".join('<div class="var"><span>%d</span>%s</div>' % (n, lecteur(f)) for n, f in enumerate(fichiers, 1))
        fic = "%s <small>(%d variantes)</small>" % (html.escape(fichiers[0]), len(fichiers))
    else:
        ecoute, fic = lecteur(fichiers[0]), html.escape(fichiers[0])
    source = "%s<br><small>%s</small>" % (html.escape(s.get("source") or ""), html.escape(s.get("licence") or ""))
    return '<tr%s><td>%s<br><code class="fic">%s</code></td><td>%s</td><td>%s</td><td>%s</td></tr>' % (
        attrs, nom, fic, ecoute, usage, source)


JS_SONS = """
document.addEventListener('DOMContentLoaded',function(){
  var q=document.getElementById('sons-q'),c=document.getElementById('sons-cat'),s=document.getElementById('sons-statut'),nb=document.getElementById('sons-nb');
  if(!q) return;
  function norm(t){return t.toLowerCase().normalize('NFD').replace(/[\\u0300-\\u036f]/g,'');}
  function filtre(){
    var mots=norm(q.value).split(/\\s+/).filter(Boolean),cat=c.value,st=s.value,n=0;
    document.querySelectorAll('.bloc-sons').forEach(function(b){
      var vis=0;
      b.querySelectorAll('tbody tr').forEach(function(tr){
        var ok=(!cat||tr.dataset.cat===cat)&&(!st||tr.dataset.statut===st)&&mots.every(function(m){return tr.dataset.q.indexOf(m)>=0;});
        tr.hidden=!ok; if(ok) vis++;
      });
      b.hidden=!vis; n+=vis;
    });
    nb.textContent=n+(n>1?' sons affichés':' son affiché');
  }
  [q,c,s].forEach(function(e){e.addEventListener('input',filtre);});
  document.addEventListener('play',function(e){document.querySelectorAll('audio').forEach(function(a){if(a!==e.target)a.pause();});},true);
  filtre();
});
"""


def catalogue_sons():
    """{catalogue sons} : barre de filtres puis un tableau par catégorie (ordre du fichier), sans les sons à créer."""
    sons = [s for s in charger_sons() if s["statut"] != "a_creer"]
    cats = []
    for s in charger_sons():
        if s["categorie"] not in cats:
            cats.append(s["categorie"])
    options = "".join('<option value="%s">%s</option>' % (html.escape(c, quote=True), html.escape(c)) for c in cats)
    out = ['<div class="filtres-sons" role="search">'
           '<input id="sons-q" type="search" placeholder="Rechercher un son, un usage, un fichier…" aria-label="Rechercher un son" autocomplete="off">'
           '<select id="sons-cat" aria-label="Catégorie"><option value="">Toutes les catégories</option>%s</select>'
           '<select id="sons-statut" aria-label="Statut"><option value="">Tous les statuts</option>%s</select>'
           '<span id="sons-nb" aria-live="polite"></span></div>'
           % (options, "".join('<option value="%s">%s</option>' % (k, v[2]) for k, v in STATUTS_SONS.items()))]
    titres = []
    for cat in cats:
        lignes = [ligne_son(s, "catalogue") for s in sons if s["categorie"] == cat]
        if not lignes:
            continue
        ident = slug(cat)
        titres.append((2, cat, ident))
        titres.extend((4, s["nom"], "son-" + s["id"]) for s in sons
                      if s["categorie"] == cat and s["statut"] in ("utilise", "a_ecouter"))
        out.append('<section class="bloc-sons"><h2 id="%s">%s <small>(%d)</small></h2><div class="table"><table class="sons">'
                   '<thead><tr><th>Son</th><th>Écouter</th><th>Usage dans Deathless</th><th>Source</th></tr></thead>'
                   '<tbody>%s</tbody></table></div></section>' % (ident, html.escape(cat), len(lignes), "".join(lignes)))
    out.append("<script>%s</script>" % JS_SONS)
    return "\n".join(out), titres


def sons_a_creer():
    """{sons à créer} : les sons dont Deathless a besoin et qui n'existent pas encore."""
    sons = [s for s in charger_sons() if s["statut"] == "a_creer"]
    titres = [(2, "À créer", "a-creer")] + [(4, s["nom"], "son-" + s["id"]) for s in sons]
    if not sons:
        return "<h2 id=\"a-creer\">À créer <small>(0)</small></h2><p>Aucun son à créer pour l'instant.</p>", titres
    lignes = "".join(ligne_son(s, "a_creer") for s in sons)
    return ('<section class="bloc-sons"><h2 id="a-creer">À créer <small>(%d)</small></h2><div class="table"><table class="sons">'
            '<thead><tr><th>Son</th><th>Catégorie</th><th>Quand il joue</th></tr></thead><tbody>%s</tbody></table></div></section>'
            % (len(sons), lignes)), titres


def sons_a_ecouter():
    """{sons à écouter} : encart qui liste, par catégorie, les sons créés en attente d'écoute, avec un lien vers chacun."""
    sons = [s for s in charger_sons() if s["statut"] == "a_ecouter"]
    if not sons:
        return "<aside class=\"note encart-ecoute\" id=\"a-ecouter\">Aucun son en attente d'écoute.</aside>", []
    cats = []
    for s in sons:
        if s["categorie"] not in cats:
            cats.append(s["categorie"])
    lignes = "".join('<li><strong>%s</strong> : %s</li>' % (html.escape(c), ", ".join(
        '<a href="#son-%s">%s</a>' % (html.escape(s["id"], quote=True), html.escape(s["nom"])) for s in sons if s["categorie"] == c))
        for c in cats)
    return ('<aside class="note encart-ecoute" id="a-ecouter"><p><span class="badge ecoute">à écouter</span> '
            "<strong>%d sons créés attendent d'être écoutés par Quentin</strong> : un clic mène au son dans le catalogue. Une fois validé, "
            'son statut passe à « utilisé » dans <code>Wiki/data/sons.json</code>.</p><ul>%s</ul></aside>'
            % (len(sons), lignes)), [(2, "À écouter", "a-ecouter")]


BALISES = {"{catalogue sons}": catalogue_sons, "{sons à créer}": sons_a_creer, "{sons à écouter}": sons_a_ecouter}


def copier_sons():
    """Copie dans site/sons/ les fichiers audio référencés (seulement ceux qui ont changé) et retire les autres."""
    copies = 0
    for rel in SONS_COPIES:
        src, dst = os.path.join(AUDIO, rel), os.path.join(SITE_SONS, rel)
        a = os.stat(src)
        if os.path.exists(dst):
            b = os.stat(dst)
            if b.st_size == a.st_size and int(b.st_mtime) == int(a.st_mtime):
                continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)
        copies += 1
    gardes = {os.path.normcase(os.path.join(SITE_SONS, r)) for r in SONS_COPIES}
    for dp, dn, fn in os.walk(SITE_SONS, topdown=False):
        for f in fn:
            if os.path.normcase(os.path.join(dp, f)) not in gardes:
                os.remove(os.path.join(dp, f))
        if dp != SITE_SONS and not os.listdir(dp):
            os.rmdir(dp)
    if SONS_COPIES:
        print("Sons : %d fichiers dans site/sons (%d copiés)" % (len(SONS_COPIES), copies))


def filtrer(md, public):
    """Adapte une page à la version développeur (public=False) ou joueur (public=True). Voir l'entête."""
    md = md.replace("\r\n", "\n")
    if public:
        md = re.sub(r"\s*\{\{dev:.*?\}\}", "", md)
    else:
        md = re.sub(r"\{\{dev:\s*(.*?)\}\}", r"\1", md)
    out, saut, table_cols = [], None, None
    for l in md.split("\n"):
        if public and RE_VIDEO.match(l.strip()):
            continue  # pas de vidéos dans la version joueur
        m = re.match(r"^(#{1,3}) ", l)
        if m:
            n = len(m.group(1))
            if saut is not None and n > saut:
                continue
            saut = None
            if public and ("{dev}" in l or "{à confirmer}" in l):
                saut = n
                continue
        elif saut is not None:
            continue
        if l.startswith("|"):
            cells = l.strip().strip("|").split("|")
            if table_cols is None:  # ligne d'en-tête du tableau
                table_cols = [k for k, c in enumerate(cells) if not (public and "{dev}" in c)]
            elif public and ("{dev}" in l or "{à confirmer}" in l):
                continue
            l = "|" + "|".join(cells[k] for k in table_cols if k < len(cells)) + "|"
        else:
            table_cols = None
            if l.startswith("  - ") and out and out[-1] is None:
                continue  # sous-élément d'un élément retiré
            if public and l.strip() and ("{dev}" in l or "{à confirmer}" in l):
                out.append(None)
                continue
            if not public and "{public}" in l:
                continue
        if public:
            l = re.sub(r"\s*\{(décidé|effet validé)\}", "", l).replace("{à équilibrer}", "{provisoire}").replace("{public}", "")
        else:
            l = l.replace("{dev}", "")
        l = l.rstrip()
        if not l.startswith("  - "):
            l = l.lstrip() if not l.startswith(("- ", "|", "#", ">")) else l
        out.append(l)
    lignes = [l for l in out if l is not None]
    propre, i = [], 0
    while i < len(lignes):  # tableaux réduits à leur en-tête : retirés
        if lignes[i].startswith("|"):
            j = i
            while j < len(lignes) and lignes[j].startswith("|"):
                j += 1
            if j - i > 2:
                propre.extend(lignes[i:j])
            i = j
            continue
        propre.append(lignes[i])
        i += 1
    final = []
    for k, l in enumerate(propre):  # sections vides : retirées
        m = re.match(r"^(#{2,3}) ", l)
        if m:
            n, vide = len(m.group(1)), True
            for suite in propre[k + 1:]:
                m2 = re.match(r"^(#{1,3}) ", suite)
                if m2 and len(m2.group(1)) <= n:
                    break
                if suite.strip() and not m2:
                    vide = False
                    break
            if vide:
                continue
        final.append(l)
    return "\n".join(final)


def convertir(md):
    lignes = md.replace("\r\n", "\n").split("\n")
    out, titres, i, titre_page = [], [], 0, None
    while i < len(lignes):
        l = lignes[i]
        if not l.strip():
            i += 1
            continue
        if l.strip() in BALISES:
            bloc, sous = BALISES[l.strip()]()
            out.append(bloc)
            titres.extend(sous)
            i += 1
            continue
        if RE_VIDEO.match(l.strip()):
            cartes = []
            while i < len(lignes) and RE_VIDEO.match(lignes[i].strip()):
                cartes.append(carte_video(RE_VIDEO.match(lignes[i].strip())))
                i += 1
            out.append('<div class="grille-videos">%s</div>' % "".join(cartes))
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
                texte = inline(lignes[i][2:].strip())
                i += 1
                sous = []
                while i < len(lignes) and lignes[i].startswith("  - "):
                    sous.append("<li>%s</li>" % inline(lignes[i][4:].strip()))
                    i += 1
                items.append("<li>%s%s</li>" % (texte, "<ul>%s</ul>" % "".join(sous) if sous else ""))
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
        while i < len(lignes) and lignes[i].strip() and not re.match(r"^(#{1,3} |- |\||> |\{video )", lignes[i]):
            para.append(lignes[i].strip())
            i += 1
        out.append("<p>%s</p>" % inline(" ".join(para)))
    return titre_page or "Sans titre", titres, "\n".join(out)


CSS = """
:root{--fond:#f6f3ec;--surface:#ffffff;--encre:#1f2433;--doux:#5b5f6b;--ligne:#e2dccd;--accent:#8a6a1f;--nyx:#1e7a45;
--ok-fond:#dcf1e3;--ok:#1b6a3a;--wait-fond:#fbe8c8;--wait:#7a4a06;--fx-fond:#dde6f7;--fx:#23457a;--tune-fond:#efe3f5;--tune:#5e2a7a;--code:#efe9dc;color-scheme:light}
@media (prefers-color-scheme: dark){:root{--fond:#161a24;--surface:#1f2533;--encre:#f4ecd8;--doux:#b9b3a3;--ligne:#333b52;
--accent:#d9b264;--nyx:#6fd08f;--ok-fond:#1d3b2a;--ok:#9fe0b4;--wait-fond:#3d2f16;--wait:#f5c77a;--fx-fond:#1f2d47;--fx:#a9c3f0;--tune-fond:#35243f;--tune:#dcb6ef;--code:#2a3142;color-scheme:dark}}
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
.badge.ok{background:var(--ok-fond);color:var(--ok)}.badge.fx{background:var(--fx-fond);color:var(--fx)}.badge.tune{background:var(--tune-fond);color:var(--tune)}.badge.wait{background:var(--wait-fond);color:var(--wait)}
.note{border:1px solid var(--ligne);background:var(--surface);border-radius:10px;padding:12px 16px;margin:16px 0;color:var(--doux)}
.swatch{display:inline-block;width:14px;height:14px;border-radius:4px;vertical-align:-2px;margin-right:6px;border:1px solid var(--ligne)}
nav li.sous-page a{padding:3px 10px 3px 26px;font-size:14px;color:var(--doux)}nav li.sous-page a.ici{color:var(--accent)}
img.icone{width:28px;height:28px;vertical-align:-8px;margin-right:4px}img.icone.grande{width:112px;height:112px;display:block;margin:4px 0 8px}
td img.icone{width:44px;height:44px;vertical-align:middle;background:#1b2130;border-radius:10px;padding:5px}
.maj{margin-top:48px;color:var(--doux);font-size:13px}
.badge.dispo{background:var(--code);color:var(--doux)}.badge.ecoute{background:var(--accent);color:var(--surface)}
.encart-ecoute{color:var(--encre);border-color:var(--accent)}.encart-ecoute p{margin:0 0 6px}.encart-ecoute ul{margin:0;font-size:14px}
table.sons tr:target td{background:var(--wait-fond)}
.filtres-sons{position:sticky;top:0;z-index:2;display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:18px 0 0;padding:10px 0;background:var(--fond);border-bottom:1px solid var(--ligne)}
.filtres-sons input,.filtres-sons select{padding:7px 10px;border-radius:8px;border:1px solid var(--ligne);background:var(--surface);color:var(--encre);font:inherit;font-size:14px}
.filtres-sons input{flex:1 1 220px}#sons-nb{color:var(--doux);font-size:13px}
h2 small{font-size:15px;color:var(--doux);font-weight:500}
table.sons td{padding:7px 10px}table.sons td small{color:var(--doux)}
table.sons audio{display:block;width:210px;height:32px}
.var{display:flex;align-items:center;gap:6px;margin:2px 0}.var span{min-width:1em;color:var(--doux);font-size:12px}
.fic{font-size:12px;word-break:break-all}.manque{color:var(--wait);font-size:13px}
.grille-videos{display:grid;grid-template-columns:repeat(auto-fill,minmax(190px,1fr));gap:14px;margin:14px 0 8px}
.carte-video{margin:0;background:var(--surface);border:1px solid var(--ligne);border-radius:12px;overflow:hidden;display:flex;flex-direction:column}
.carte-video video{display:block;width:100%;aspect-ratio:1/1;background:#1c1f26}
.carte-video figcaption{padding:8px 10px 10px;display:flex;flex-direction:column;gap:3px;font-size:13px;line-height:1.4;color:var(--doux)}
.carte-video figcaption span:first-child{color:var(--encre);font-size:14px}
.carte-video code{font-size:12px;word-break:break-all}.carte-video .badge{margin-left:0}
.badge.emote{background:var(--tune-fond);color:var(--tune)}
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


def generer(public):
    global SITE_SONS, PAGES_PRESENTES
    dossier = PUBLIC if public else SITE
    SITE_SONS = os.path.join(dossier, "sons")
    SONS_COPIES.clear()
    ICONES_COPIEES.clear()
    VIDEOS_COPIEES.clear()
    os.makedirs(dossier, exist_ok=True)
    menu_ok = [e for e in MENU if not (public and "dev" in options(e)) and os.path.exists(os.path.join(PAGES, e[0] + ".md"))]
    PAGES_PRESENTES = {e[0] for e in menu_ok}
    pages, index = [], []
    for e in menu_ok:
        nom, court = e[0], e[1]
        md = filtrer(io.open(os.path.join(PAGES, nom + ".md"), encoding="utf-8").read(), public)
        titre, titres, corps = convertir(md)
        pages.append((nom, court, titre, corps, "sous" in options(e)))
        index.append({"p": court, "t": titre, "u": nom + ".html"})
        index.extend({"p": court, "t": t, "u": "%s.html#%s" % (nom, ident)} for _, t, ident in titres)
    for f in os.listdir(dossier):  # pages qui ne font plus partie de cette version
        if f.endswith(".html") and f[:-5] not in PAGES_PRESENTES:
            os.remove(os.path.join(dossier, f))
    maj = datetime.date.today().strftime("%d/%m/%Y")
    sous_titre = "Wiki du joueur" if public else "Wiki des règles · développeur"
    for nom, court, titre, corps, _ in pages:
        menu = "".join('<li%s><a href="%s.html"%s>%s</a></li>' % (' class="sous-page"' if sp else "", n,
                       ' class="ici" aria-current="page"' if n == nom else "", html.escape(c))
                       for n, c, _, _, sp in pages)
        pied = ("Mis à jour le %s." % maj) if public else ("Généré le %s depuis <code>Wiki/pages/%s.md</code>." % (maj, nom))
        doc = ('<!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
               '<title>%s · Wiki Deathless</title>'
               '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Fredoka:wght@500;700&amp;display=swap">'
               '<style>%s</style></head><body><div class="cadre"><nav aria-label="Pages du wiki">'
               '<a class="marque" href="index.html">DEATHLESS</a><p class="sous">%s</p>'
               '<label for="cherche" class="sous" style="display:block;margin:0 0 6px">Rechercher</label>'
               '<input id="cherche" type="search" placeholder="Classe, portail, touche…" autocomplete="off">'
               '<ul class="res" id="res"></ul><ul>%s</ul></nav>'
               '<main>%s<p class="maj">%s</p></main></div>'
               '<script>var INDEX=%s;%s</script></body></html>'
               % (html.escape(titre), CSS, sous_titre, menu, corps, pied, json.dumps(index, ensure_ascii=False),
                  JS + (JS_VIDEOS if 'class="carte-video"' in corps else "")))
        if not public:  # version développeur en ligne : pas d'indexation par les moteurs de recherche
            doc = doc.replace("<title>", '<meta name="robots" content="noindex, nofollow"><title>', 1)
        io.open(os.path.join(dossier, nom + ".html"), "w", encoding="utf-8", newline="\n").write(doc)
    copier_sons()
    copier_icones(dossier)
    copier_videos(dossier)
    print("Wiki %s : %d pages dans %s" % ("joueur" if public else "développeur", len(pages), dossier))


def main():
    generer(public=False)
    generer(public=True)


if __name__ == "__main__":
    main()
