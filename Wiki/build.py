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
# grille. Le fichier est copié dans <version>/videos/ (site/ et public/). Dans la version joueur, une partie de légende
# qui porte {dev} est retirée (ex. le nom technique du clip), et toute la carte si {dev} est dans sa première partie ;
# une carte {à confirmer} y est retirée comme toute ligne {à confirmer}.
# Spoil : un bloc entre une ligne {spoil Libellé} et une ligne {/spoil} est replié derrière « Attention, spoil Libellé »
# (à déplier d'un clic, dans les deux versions) ; ses titres ne vont pas dans le sommaire.
# Images : une ligne qui commence par {image chemin} (chemin relatif à Wiki/, ex. media/classes/clochard/face.png) devient
# une carte image carrée (même légende et mêmes règles {dev} / {à confirmer} que les vidéos, un clic ouvre l'image entière) ;
# les lignes {video} et {image} consécutives forment une même grille. Le fichier est copié dans <version>/images/.
import html, io, json, os, re, datetime, shutil, sys, unicodedata, urllib.parse
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ICI = os.path.dirname(os.path.abspath(__file__))
PAGES = os.path.join(ICI, "pages")
SITE = os.path.join(ICI, "site")
PUBLIC = os.path.join(ICI, "public")
DATA = os.path.join(ICI, "data")
AUDIO = os.path.normpath(os.path.join(ICI, "..", "Assets", "Audio"))
SITE_SONS = os.path.join(SITE, "sons")
ICONES = os.path.normpath(os.path.join(ICI, "..", "ArtSources", "Icones"))
ICONES_COPIEES = set()  # icônes SVG référencées par {icone nom}, copiées dans <version>/icones/
VIDEOS_COPIEES = set()  # vidéos référencées par {video chemin} (chemins relatifs à media/), copiées dans <version>/videos/
IMAGES_COPIEES = set()  # images référencées par {image chemin} (chemins relatifs à media/), copiées dans <version>/images/

# Ordre du menu : (fichier sans extension, libellé court[, options]). Options, séparées par des espaces : "dev" si la page
# est réservée à la version développeur, "sous" pour une sous-page (affichée en retrait sous la page qui la précède).
MENU = [
    ("index", "Accueil"),
    ("univers", "L'univers"),
    ("principes", "Principes"),
    ("deroule", "Déroulé d'une partie"),
    ("donjon", "Le donjon", "sous"),
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
    ("classe-clochard", "Clochard pétomane (bientôt)", "sous"),
    ("classe-dj-bob", "DJ Bob (bientôt)", "sous"),
    ("ennemis", "Ennemis"),
    ("statuts", "Statuts"),
    ("commandes", "Commandes"),
    ("interface", "Interface"),
    ("succes", "Succès", "dev"),
    ("effets", "Effets et couleurs", "dev"),
    ("direction-artistique", "Direction artistique", "dev"),
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
    Le SVG est pris dans ArtSources/Icones/Classes, Competences, Nyxessa ou Statuts, puis copié dans <version>/icones/."""
    nom = m.group(2)
    for sous in ("Classes", "Competences", "Nyxessa", "Statuts"):
        if os.path.exists(os.path.join(ICONES, sous, nom + ".svg")):
            ICONES_COPIEES.add((sous, nom))
            return '<img class="icone%s" src="icones/%s.svg" alt="">' % (" grande" if m.group(1) else "", nom)
    return '<span class="manque">[icône %s introuvable]</span>' % nom


def copier_icones(dossier):
    cible = os.path.join(dossier, "icones")
    os.makedirs(cible, exist_ok=True)
    for sous, nom in ICONES_COPIEES:
        shutil.copyfile(os.path.join(ICONES, sous, nom + ".svg"), os.path.join(cible, nom + ".svg"))


# ------------------------------------------------------------------ vidéos ({video chemin}, les deux versions)

RE_VIDEO = re.compile(r"^\{video ([^}]+)\}\s*(.*)$")
RE_IMAGE = re.compile(r"^\{image ([^}]+)\}\s*(.*)$")
RE_MEDIA = re.compile(r"^\{(video|image) ([^}]+)\}\s*(.*)$")


def carte_video(m):
    """{video chemin} légende | ligne 2 | ... : carte avec une vidéo muette en boucle, chargée paresseusement (data-src,
    posée par JS_VIDEOS quand la carte approche de l'écran). Le fichier est copié plus tard dans <version>/videos/."""
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


def carte_image(m):
    """{image chemin} légende | ligne 2 | ... : carte avec une image carrée (chargée paresseusement) qui ouvre l'image
    entière. Le fichier est copié plus tard dans <version>/images/."""
    chemin = m.group(1).strip().replace("\\", "/")
    rel = chemin[len("media/"):] if chemin.startswith("media/") else chemin
    parts = [p.strip() for p in m.group(2).split(" | ") if p.strip()]
    alt = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", parts[0]) if parts else ""  # liens -> texte
    alt = html.escape(re.sub(r"\{[^}]*\}|\*\*|`", "", alt).strip(), quote=True)
    if not os.path.isfile(os.path.join(ICI, chemin)):
        print("Attention : image absente : Wiki/%s" % chemin)
        image = '<span class="manque">image absente : %s</span>' % html.escape(chemin)
    else:
        IMAGES_COPIEES.add(rel)
        src = html.escape("images/" + urllib.parse.quote(rel), quote=True)
        image = '<a href="%s"><img src="%s" loading="lazy" alt="%s"></a>' % (src, src, alt)
    legende = "".join("<span>%s</span>" % inline(p) for p in parts)
    return '<figure class="carte-video carte-image">%s<figcaption>%s</figcaption></figure>' % (image, legende)


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
    copier_medias(dossier, "videos", VIDEOS_COPIEES, "Vidéos")


def copier_images(dossier):
    """Copie dans <dossier>/images/ les images référencées (seulement celles qui ont changé) et retire les autres."""
    copier_medias(dossier, "images", IMAGES_COPIEES, "Images")


def copier_medias(dossier, sous, fichiers, libelle):
    cible = os.path.join(dossier, sous)
    copies = 0
    for rel in fichiers:
        src, dst = os.path.join(ICI, "media", rel), os.path.join(cible, rel)
        a = os.stat(src)
        if os.path.exists(dst):
            b = os.stat(dst)
            if b.st_size == a.st_size and int(b.st_mtime) == int(a.st_mtime):
                continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(src, dst)
        copies += 1
    gardes = {os.path.normcase(os.path.join(cible, r)) for r in fichiers}
    for dp, dn, fn in os.walk(cible, topdown=False):
        for f in fn:
            if os.path.normcase(os.path.join(dp, f)) not in gardes:
                os.remove(os.path.join(dp, f))
        if dp != cible and not os.listdir(dp):
            os.rmdir(dp)
    if fichiers:
        print("%s : %d fichiers dans %s (%d copiés)" % (libelle, len(fichiers), os.path.relpath(cible, ICI), copies))


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
        par_id = {s["id"]: s for s in _sons}
        for s in _sons:  # `remplace` : id du son actuel que ce son en attente doit remplacer (comparaison côte à côte)
            r = s.get("remplace")
            if not r:
                continue
            p = par_id.get(r)
            if p is None or r == s["id"]:
                raise SystemExit("data/sons.json, `remplace` de %s : id introuvable (%r)" % (s["id"], r))
            if s["statut"] != "a_ecouter":
                print("Attention : %s a `remplace` mais n'est pas à écouter : ignoré." % s["id"])
            elif p["statut"] == "a_creer" or not p.get("fichier"):
                print("Attention : %s remplace %s qui n'a pas de fichier : pas de comparaison." % (s["id"], r))
            else:
                if p["statut"] != "utilise":
                    print("Attention : %s remplace %s qui n'est pas au statut utilisé (%s)." % (s["id"], r, p["statut"]))
                s["_pred"] = p
                p.setdefault("_candidats", []).append(s["id"])
    return _sons


def lecteur(rel):
    """Lecteur audio d'un fichier de Assets/Audio/ (copié ensuite dans site/sons/)."""
    if not os.path.isfile(os.path.join(AUDIO, rel)):
        print("Attention : fichier audio absent : Assets/Audio/%s" % rel)
        return '<span class="manque">fichier absent</span>'
    SONS_COPIES.add(rel)
    return '<audio controls preload="none" src="sons/%s"></audio>' % html.escape(urllib.parse.quote(rel), quote=True)


def lecteurs(s):
    """Lecteur(s) d'un son (un par variante) et la liste de ses fichiers."""
    fichiers = s.get("variantes") or [s["fichier"]]
    if len(fichiers) > 1:
        ecoute = "".join('<div class="var"><span>%d</span>%s</div>' % (n, lecteur(f)) for n, f in enumerate(fichiers, 1))
    else:
        ecoute = lecteur(fichiers[0])
    return ecoute, fichiers


def etiquette_ancien(p):
    """Étiquette de la colonne gauche d'une comparaison, d'après le statut du son remplacé."""
    return {"utilise": "Utilisé actuellement", "disponible": "Ancien son (disponible)",
            "a_ecouter": "Version précédente (à écouter)"}.get(p["statut"], "Son remplacé")


def widget_validation(s, comparaison):
    """Boutons de validation d'un son à écouter (état gardé dans le navigateur, voir JS_VALID)."""
    i = html.escape(s["id"], quote=True)
    rem = ' data-remplace="%s"' % html.escape(s["_pred"]["id"], quote=True) if s.get("_pred") else ""
    nom = html.escape(s["nom"], quote=True)
    if comparaison:
        return ('<div class="valid valid--cmp" role="group" aria-label="Validation : %s" data-id="%s"%s>'
                '<button type="button" class="valid__btn valid__btn--ok" data-val="ok" aria-pressed="false">Prendre le nouveau</button>'
                '<button type="button" class="valid__btn valid__btn--ko" data-val="ko" aria-pressed="false">Garder l\'ancien</button>'
                '</div>' % (nom, i, rem))
    return ('<div class="valid" role="group" aria-label="Validation : %s" data-id="%s"%s>'
            '<button type="button" class="valid__btn valid__btn--attente" data-val="attente" aria-pressed="false">À écouter</button>'
            '<button type="button" class="valid__btn valid__btn--ok" data-val="ok" aria-pressed="false">Validé ✓</button>'
            '<button type="button" class="valid__btn valid__btn--ko" data-val="ko" aria-pressed="false">Refusé ✗</button>'
            '</div>' % (nom, i, rem))


def bloc_comparaison(s, usage=True):
    """Bloc côte à côte : à gauche le son actuel (toutes ses variantes), à droite le nouveau en attente."""
    p = s["_pred"]
    e_old, f_old = lecteurs(p)
    e_new, f_new = lecteurs(s)
    haut = ('<div class="cmp"><div class="cmp__col cmp__col--ancien"><p class="cmp__etiquette">%s</p>'
            '<p class="cmp__nom"><strong>%s</strong><br><code class="fic">%s</code></p>%s</div>'
            '<div class="cmp__col cmp__col--nouveau"><p class="cmp__etiquette">En attente</p>'
            '<p class="cmp__nom"><strong>%s</strong><br><code class="fic">%s</code></p>%s</div>'
            % (etiquette_ancien(p), html.escape(p["nom"]), html.escape(p["id"]), e_old,
               html.escape(s["nom"]), html.escape(s["id"]), e_new))
    return haut + widget_validation(s, True) + "</div>"


def ligne_son(s, colonnes):
    libelle, classe = STATUTS_SONS[s["statut"]][:2]
    q = sans_accents(" ".join(str(s.get(k) or "") for k in ("nom", "categorie", "usage", "source", "id", "fichier")))
    attrs = ' id="son-%s" data-cat="%s" data-statut="%s" data-q="%s"' % (
        html.escape(s["id"], quote=True), html.escape(s["categorie"], quote=True), s["statut"], html.escape(q, quote=True))
    nom = '<strong>%s</strong> <span class="badge %s">%s</span>' % (html.escape(s["nom"]), classe, libelle)
    usage = inline(s.get("usage") or "") or "—"
    if colonnes == "a_creer":
        return "<tr%s><td>%s</td><td>%s</td><td>%s</td></tr>" % (attrs, nom, html.escape(s["categorie"]), usage)
    source = "%s<br><small>%s</small>" % (html.escape(s.get("source") or ""), html.escape(s.get("licence") or ""))
    if s["statut"] == "a_ecouter" and s.get("_pred"):  # comparaison : la ligne s'étale sur les colonnes 2 à 4
        fic = html.escape((s.get("variantes") or [s["fichier"]])[0])
        return ('<tr%s class="ligne-cmp"><td>%s<br><code class="fic">%s</code><br><small>%s</small></td>'
                '<td colspan="3">%s<p class="cmp__usage">%s</p></td></tr>' % (
                    attrs, nom, fic, source.replace("<br>", " · "), bloc_comparaison(s), usage))
    ecoute, fichiers = lecteurs(s)
    fic = ("%s <small>(%d variantes)</small>" % (html.escape(fichiers[0]), len(fichiers))) if len(fichiers) > 1 else html.escape(fichiers[0])
    if s["statut"] == "a_ecouter":
        ecoute += widget_validation(s, False)
    elif s.get("_candidats"):
        nom += "".join('<br><small><a class="cmp-lien" href="#son-%s">voir la comparaison</a></small>' % html.escape(c, quote=True)
                       for c in s["_candidats"])
    return '<tr%s><td>%s<br><code class="fic">%s</code></td><td>%s</td><td>%s</td><td>%s</td></tr>' % (
        attrs, nom, fic, ecoute, usage, source)


JS_SONS = """
document.addEventListener('DOMContentLoaded',function(){
  var q=document.getElementById('sons-q'),c=document.getElementById('sons-cat'),s=document.getElementById('sons-statut'),nb=document.getElementById('sons-nb'),v=document.getElementById('sons-valid');
  if(!q) return;
  function norm(t){return t.toLowerCase().normalize('NFD').replace(/[\\u0300-\\u036f]/g,'');}
  function filtre(){
    var mots=norm(q.value).split(/\\s+/).filter(Boolean),cat=c.value,st=s.value,n=0;
    document.querySelectorAll('.bloc-sons').forEach(function(b){
      var vis=0;
      b.querySelectorAll('tbody tr').forEach(function(tr){
        var ok=(!cat||tr.dataset.cat===cat)&&(!st||tr.dataset.statut===st)&&(!v||!v.value||(tr.dataset.statut==='a_ecouter'&&(tr.dataset.valid||'attente')===v.value))&&mots.every(function(m){return tr.dataset.q.indexOf(m)>=0;});
        tr.hidden=!ok; if(ok) vis++;
      });
      b.hidden=!vis; n+=vis;
    });
    nb.textContent=n+(n>1?' sons affichés':' son affiché');
  }
  [q,c,s,v].forEach(function(e){if(e)e.addEventListener('input',filtre);});window.sonsFiltre=filtre;
  document.addEventListener('play',function(e){document.querySelectorAll('audio').forEach(function(a){if(a!==e.target)a.pause();});},true);
  filtre();
});
"""


JS_VALID = r"""
(function(){
  var CLE='deathless-sons-validation-v1', etat={};
  function lire(){try{var t=window.localStorage.getItem(CLE);etat=t?JSON.parse(t)||{}:{};}catch(e){etat=etat||{};}}
  function ecrire(){try{window.localStorage.setItem(CLE,JSON.stringify(etat));}catch(e){}}
  function unique(){var vus={},l=[];document.querySelectorAll('.valid[data-id]').forEach(function(w){
    var i=w.getAttribute('data-id');if(!vus[i]){vus[i]=1;l.push(w);}});return l;}
  function appliquer(){
    var ok=0,ko=0,tout=unique();
    tout.forEach(function(w){var v=etat[w.getAttribute('data-id')];if(v==='ok')ok++;else if(v==='ko')ko++;});
    document.querySelectorAll('.valid').forEach(function(w){
      var v=etat[w.getAttribute('data-id')]||'attente';
      w.setAttribute('data-etat',v);
      w.querySelectorAll('.valid__btn').forEach(function(b){var on=b.getAttribute('data-val')===v;
        b.classList.toggle('on',on);b.setAttribute('aria-pressed',on?'true':'false');});
    });
    document.querySelectorAll('tr[id^="son-"][data-statut="a_ecouter"]').forEach(function(tr){
      tr.setAttribute('data-valid',etat[tr.id.slice(4)]||'attente');});
    var txt=ok+(ok>1?' validés':' validé')+', '+ko+(ko>1?' refusés':' refusé')+', '+(tout.length-ok-ko)+' à écouter';
    document.querySelectorAll('.val-compteur').forEach(function(e){e.textContent=txt;});
    if(window.sonsFiltre)window.sonsFiltre();
  }
  function choisir(id,val){if(val==='attente'||etat[id]===val)delete etat[id];else etat[id]=val;ecrire();appliquer();}
  function selection(){
    var d=new Date(),p=function(n){return n<10?'0'+n:''+n;},lignes=[],ok=0,ko=0;
    unique().forEach(function(w){var i=w.getAttribute('data-id'),v=etat[i],r=w.getAttribute('data-remplace');
      if(v==='ok'){ok++;lignes.push(i+' : valider'+(r?' (remplace '+r+')':''));}
      else if(v==='ko'){ko++;lignes.push(i+' : refuser');}});
    return 'Sélection de sons, wiki Deathless, '+d.getFullYear()+'-'+p(d.getMonth()+1)+'-'+p(d.getDate())+' '+p(d.getHours())+':'+p(d.getMinutes())
      +' ('+ok+' validés, '+ko+' refusés)\n'+(lignes.length?lignes.join('\n'):'(rien de coché)');
  }
  function info(m){document.querySelectorAll('.val-info').forEach(function(e){e.textContent=m;});}
  function copier(){
    var t=selection(),zone=document.querySelector('.val-texte');
    function secours(){if(zone){zone.hidden=false;zone.value=t;zone.focus();zone.select();}
      try{if(document.execCommand('copy')){info('Copié. Colle-le dans le chat.');return;}}catch(e){}
      info('Copie impossible ici : le texte est sélectionné, copie-le à la main (Ctrl+C).');}
    if(navigator.clipboard&&navigator.clipboard.writeText){
      navigator.clipboard.writeText(t).then(function(){info('Copié ('+(t.split('\n').length-1)+' ligne(s)). Colle-le dans le chat.');},secours);
    }else secours();
  }
  function reset(){if(window.confirm('Tout réinitialiser : effacer toutes les validations et tous les refus de ce navigateur ?')){etat={};ecrire();appliquer();info('Réinitialisé.');}}
  document.addEventListener('click',function(e){
    var b=e.target.closest&&e.target.closest('.valid__btn');
    if(b){var w=b.closest('.valid');choisir(w.getAttribute('data-id'),b.getAttribute('data-val'));return;}
    var a=e.target.closest&&e.target.closest('[data-act]');
    if(a){var act=a.getAttribute('data-act');if(act==='copier')copier();else if(act==='reset')reset();}
  });
  document.addEventListener('DOMContentLoaded',function(){lire();appliquer();});
})();
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
           '<select id="sons-valid" aria-label="Validation"><option value="">Validation : toutes</option><option value="attente">Seulement à écouter</option>'
           '<option value="ok">Seulement validés ✓</option><option value="ko">Seulement refusés ✗</option></select>'
           '<span id="sons-nb" aria-live="polite"></span>'
           '<span class="val-compteur" aria-live="polite"></span>'
           '<button type="button" class="val-bouton" data-act="copier">Copier ma sélection</button></div>'
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
    """{sons à écouter} : encart de relecture. Par catégorie, chaque son créé qui attend l'écoute de Quentin : un son qui en
    remplace un autre (`remplace`) s'affiche en comparaison côte à côte, les autres avec leur lecteur ; chacun a ses boutons
    de validation (état gardé dans le navigateur, bouton « Copier ma sélection » : voir JS_VALID)."""
    sons = [s for s in charger_sons() if s["statut"] == "a_ecouter"]
    if not sons:
        return "<aside class=\"note encart-ecoute\" id=\"a-ecouter\">Aucun son en attente d'écoute.</aside>", []
    cats = []
    for s in sons:
        if s["categorie"] not in cats:
            cats.append(s["categorie"])
    nb_cmp = sum(1 for s in sons if s.get("_pred"))
    sections = []
    for c in cats:
        items = []
        for s in (x for x in sons if x["categorie"] == c):
            lien = '<a href="#son-%s">dans le catalogue</a>' % html.escape(s["id"], quote=True)
            if s.get("_pred"):
                items.append('<li class="ecoute-item ecoute-item--cmp" data-id="%s">%s<p class="cmp__usage">%s <small>(%s)</small></p></li>' % (
                    html.escape(s["id"], quote=True), bloc_comparaison(s), inline(s.get("usage") or ""), lien))
            else:
                ecoute, _ = lecteurs(s)
                items.append('<li class="ecoute-item" data-id="%s"><p class="ecoute-item__nom"><strong>%s</strong> <small>(%s)</small></p>'
                             '<div class="ecoute-item__corps"><div class="ecoute-item__lecteurs">%s</div>%s</div>'
                             '<p class="cmp__usage">%s</p></li>' % (
                                 html.escape(s["id"], quote=True), html.escape(s["nom"]), lien, ecoute, widget_validation(s, False),
                                 inline(s.get("usage") or "")))
        sections.append('<details class="ecoute-cat" open><summary>%s <small>(%d)</small></summary><ul class="ecoute-liste">%s</ul></details>' % (
            html.escape(c), sum(1 for x in sons if x["categorie"] == c), "".join(items)))
    return ('<aside class="note encart-ecoute" id="a-ecouter"><p><span class="badge ecoute">à écouter</span> '
            "<strong>%d sons créés attendent d'être écoutés par Quentin</strong> dont %d en comparaison avec le son actuel. Pour chacun : "
            "écouter, puis « Validé ✓ » / « Refusé ✗ » (ou « Prendre le nouveau » / « Garder l'ancien » en comparaison). "
            "Le choix reste dans ce navigateur ; « Copier ma sélection » en fait un texte à coller dans le chat avec Claude, "
            "qui applique les changements dans <code>Wiki/data/sons.json</code>.</p>"
            '<div class="val-barre"><span class="val-compteur" aria-live="polite"></span>'
            '<button type="button" class="val-bouton" data-act="copier">Copier ma sélection</button>'
            '<button type="button" class="val-bouton val-bouton--reset" data-act="reset">Tout réinitialiser</button>'
            '<span class="val-info" role="status"></span></div>%s'
            '<textarea class="val-texte" hidden readonly rows="8" aria-label="Sélection à copier"></textarea>'
            '<script>%s</script></aside>' % (len(sons), nb_cmp, "".join(sections), JS_VALID)), [(2, "À écouter", "a-ecouter")]


def jauge_kaykit():
    """{jauge-kaykit} : part des assets encore issus des packs KayKit, par famille (Wiki/jauge_kaykit.py, recalculée à
    chaque génération). Le cap est 0 % (décision de Quentin, 26/09/2026)."""
    import jauge_kaykit as jk
    d = jk.ecrire()
    def barre(pct, k, t):
        return ('<div class="jauge"><div class="jauge__piste"><div class="jauge__remplissage" style="width:%.1f%%"></div></div>'
                '<span class="jauge__valeur">%.1f %%</span><small>%d / %d</small></div>') % (pct, pct, k, t)
    lignes = "".join('<tr><td>%s</td><td>%s</td></tr>' % (html.escape(f["nom"]), barre(f["pourcent"], f["kaykit"], f["total"]))
                     for f in d["familles"])
    bloc = ('<div class="jauge-kaykit"><p class="jauge-kaykit__titre">Part de KayKit dans les assets du jeu : '
            '<strong>%.1f %%</strong> <small>(%d fichiers sur %d, mesuré le %s)</small></p>%s'
            '<table class="jauge-kaykit__table"><tbody>%s</tbody></table>'
            '<p class="jauge-kaykit__note">Compte les modèles, textures, animations, matériaux et sons sous <code>Assets/</code> '
            '(hors interface, icônes, éditeur et captures) ; est KayKit tout fichier rangé dans un dossier KayKit. '
            'Objectif : 0 %%.</p></div>') % (d["pourcent"], d["kaykit"], d["total"], d["date"], barre(d["pourcent"], d["kaykit"], d["total"]), lignes)
    return bloc, []


def jauge_kenney():
    """{jauge-kenney} : part des sons et des icônes encore issus des packs Kenney et usage réel de chaque fichier
    (Wiki/jauge_kenney.py, recalculée à chaque génération), puis le tableau « à remplacer » trié par fréquence d'usage.
    Même cap que KayKit : dégager Kenney (décision de Quentin, 03/10/2026)."""
    import jauge_kenney as jn
    d = jn.ecrire()
    s, ic = d["sons"], d["icones"]

    def barre(pct, k, tot):
        return ('<div class="jauge"><div class="jauge__piste"><div class="jauge__remplissage" style="width:%.1f%%"></div></div>'
                '<span class="jauge__valeur">%.1f %%</span><small>%d / %d</small></div>') % (pct, pct, k, tot)

    def pct(k, tot):
        return 100.0 * k / tot if tot else 0.0
    mo = lambda o: "%.1f Mo" % (o / 1048576.0)
    ligne = lambda nom, k, tot: "<tr><td>%s</td><td>%s</td></tr>" % (html.escape(nom), barre(pct(k, tot), k, tot))
    parts = "".join((
        ligne("Sons câblés dans le jeu", s["jeu_ids_kenney"], s["jeu_ids_effectifs"]),
        ligne("Entrées du catalogue de sons", s["catalogue_entrees_kenney"], s["catalogue_entrees"]),
        ligne("Fichiers audio du projet", s["fichiers"], s["total_sons_projet"]),
    ))
    etats = s["etats"]
    usage = "".join((
        ligne("Sons joués par le jeu", etats["joue"], s["fichiers"]),
        ligne("Sons en secours (derrière un son propre)", etats["repli"], s["fichiers"]),
        ligne("Sons au catalogue, jamais appelés", etats["catalogue"], s["fichiers"]),
        ligne("Sons seulement sur le disque", etats["disque"], s["fichiers"]),
        ligne("Icônes de boutons utilisées", ic["utilisees"], ic["fichiers"]),
    ))
    freq = {5: "constante", 4: "à chaque combat", 3: "régulière", 2: "occasionnelle", 1: "rare"}
    etat_txt = {"joue": "joué", "repli": "secours seulement", "catalogue": "jamais appelé"}
    par_id = {x["id"]: x for x in charger_sons()}
    lignes = []
    for r in d["a_remplacer"]:
        cands = "".join(' <a href="sons.html#son-%s"><code>%s</code></a>' % (html.escape(c, quote=True), html.escape(c))
                        for c in r["candidats"] if c in par_id and par_id[c]["statut"] == "a_ecouter")
        attente = ' <span class="badge ecoute">candidat à écouter</span>%s' % cands if cands else ""
        lignes.append("<tr><td><code>%s</code><br><small>%s, %d variante%s</small></td><td>%s</td><td>%s<br><small>%s</small></td><td>%s</td>"
                      '<td><span class="badge wait">%s</span>%s</td></tr>' % (
                          html.escape(r["id"]), html.escape(r["pack"]), r["variantes"], "s" if r["variantes"] > 1 else "",
                          html.escape(" ; ".join(r["evenements"])), freq.get(r["frequence"], str(r["frequence"])),
                          etat_txt.get(r["etat"], r["etat"]), html.escape(r["remplacant"]), r["avancement"], attente))
    tableau = ('<div class="table"><table class="jauge-kenney__remplacer"><thead><tr><th>Son Kenney</th><th>Usage dans le jeu</th>'
               '<th>Fréquence</th><th>Remplaçant proposé</th><th>État</th></tr></thead><tbody>%s</tbody></table></div>'
               % "".join(lignes))
    derives = ", ".join("<code>%s</code>" % html.escape(x["id"]) for x in d["derives"]) or "aucun"
    autres = "".join('<li><strong>%s</strong> : %d fichiers, %.1f Mo, %s (%s).</li>' % (
        html.escape(a["nom"]), a["fichiers"], a["octets"] / 1048576.0, html.escape(a["licence"]), html.escape(a["note"]))
        for a in d["autres_sources"])
    bloc = ('<div class="jauge-kaykit jauge-kenney"><p class="jauge-kaykit__titre">Part de Kenney dans les assets du jeu : '
            '<strong>%.1f %%</strong> des sons câblés <small>(%d sur %d, mesuré le %s ; %d fichiers Kenney, %s)</small></p>%s'
            '<table class="jauge-kaykit__table"><tbody>%s</tbody></table>'
            '<p class="jauge-kaykit__note">Kenney = packs RPG Audio et Interface Sounds (sons, <code>Assets/Audio/Kenney/</code>) et Input Prompts '
            '(icônes de boutons, <code>Assets/Art/UI/KenneyInputPrompts/</code>), tous CC0. Un son est « câblé » quand il est le premier id présent '
            "d'un tableau de <code>SonsDuJeu</code> ou <code>SonsPortes</code> (les sons à écouter jouent déjà devant leur repli Kenney). Objectif : 0 %%.</p>"
            '<p class="jauge-kaykit__titre" style="margin-top:14px">Usage des %d fichiers Kenney <small>(sons %s, icônes %s)</small></p>'
            '<table class="jauge-kaykit__table"><tbody>%s</tbody></table>'
            '<p class="jauge-kaykit__note">Sons dérivés (échantillons Kenney retravaillés dans un son propre) : %s.</p>'
            "<p class=\"jauge-kaykit__titre\" style=\"margin-top:14px\">À remplacer, par fréquence d'usage <small>(%d sons Kenney appelés par le jeu ; "
            "les %d autres, jamais appelés, sont à retirer sans remplaçant)</small></p>%s"
            "<p class=\"jauge-kaykit__titre\" style=\"margin-top:14px\">Autres sources tierces repérées</p><ul>%s</ul></div>") % (
        s["pourcent_jeu"], s["jeu_ids_kenney"], s["jeu_ids_effectifs"], d["date"], d["total_fichiers"], mo(d["total_octets"]),
        barre(s["pourcent_jeu"], s["jeu_ids_kenney"], s["jeu_ids_effectifs"]), parts,
        d["total_fichiers"], mo(s["octets"]), mo(ic["octets"]), usage, derives,
        len(d["a_remplacer"]), d["non_branches"]["fichiers"], tableau, autres)
    return bloc, []


BALISES = {"{catalogue sons}": catalogue_sons, "{sons à créer}": sons_a_creer, "{sons à écouter}": sons_a_ecouter,
           "{jauge-kaykit}": jauge_kaykit, "{jauge-kenney}": jauge_kenney}


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
        mv = RE_MEDIA.match(l.strip())
        if public and mv:  # carte vidéo ou image : parties de légende {dev} retirées ({dev} dans la 1re partie : carte retirée plus bas)
            parts = mv.group(3).split(" | ")
            if "{dev}" not in parts[0]:
                l = ("{%s %s} %s" % (mv.group(1), mv.group(2), " | ".join(p for p in parts if "{dev}" not in p))).rstrip()
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
        if RE_MEDIA.match(l.strip()):
            cartes = []
            while i < len(lignes) and RE_MEDIA.match(lignes[i].strip()):
                x = lignes[i].strip()
                cartes.append(carte_video(RE_VIDEO.match(x)) if RE_VIDEO.match(x) else carte_image(RE_IMAGE.match(x)))
                i += 1
            out.append('<div class="grille-videos">%s</div>' % "".join(cartes))
            continue
        ms = re.match(r"^\{spoil\s*([^}]*)\}\s*$", l.strip())
        if ms:
            # Bloc replié « Attention, spoil » (01/10/2026, points faibles et phases des boss) : tout jusqu'à {/spoil},
            # converti à part ; ses titres ne vont pas dans le sommaire (ils trahiraient le contenu).
            bloc = []
            i += 1
            while i < len(lignes) and lignes[i].strip() != "{/spoil}":
                bloc.append(lignes[i])
                i += 1
            i += 1
            _, _, interieur = convertir(chr(10).join(bloc))
            out.append('<details class="spoil"><summary><span class="spoil__alerte">Attention, spoil</span> %s</summary>'
                       '<div class="spoil__corps">%s</div></details>' % (inline(ms.group(1).strip()), interieur))
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
        while i < len(lignes) and lignes[i].strip() and not re.match(r"^(#{1,3} |- |\||> |\{video |\{image )", lignes[i]):
            para.append(lignes[i].strip())
            i += 1
        out.append("<p>%s</p>" % inline(" ".join(para)))
    return titre_page or "Sans titre", titres, "\n".join(out)


CSS = """
:root{--fond:#f6f3ec;--surface:#ffffff;--encre:#1f2433;--doux:#5b5f6b;--ligne:#e2dccd;--accent:#8a6a1f;--nyx:#1e7a45;
--ok-fond:#dcf1e3;--ok:#1b6a3a;--wait-fond:#fbe8c8;--wait:#7a4a06;--fx-fond:#dde6f7;--fx:#23457a;--tune-fond:#efe3f5;--tune:#5e2a7a;--code:#efe9dc;--ko-fond:#f8d9d6;--ko:#8c2323;color-scheme:light}
@media (prefers-color-scheme: dark){:root{--fond:#161a24;--surface:#1f2533;--encre:#f4ecd8;--doux:#b9b3a3;--ligne:#333b52;
--accent:#d9b264;--nyx:#6fd08f;--ok-fond:#1d3b2a;--ok:#9fe0b4;--wait-fond:#3d2f16;--wait:#f5c77a;--fx-fond:#1f2d47;--fx:#a9c3f0;--tune-fond:#35243f;--tune:#dcb6ef;--code:#2a3142;--ko-fond:#46201f;--ko:#f1a8a4;color-scheme:dark}}
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
.carte-video img{display:block;width:100%;aspect-ratio:1/1;object-fit:cover;background:#1c1f26}
.carte-video code{font-size:12px;word-break:break-all}.carte-video .badge{margin-left:0}
.badge.emote{background:var(--tune-fond);color:var(--tune)}
.valid{display:inline-flex;flex-wrap:wrap;gap:6px;margin:6px 0 2px}
.valid__btn,.val-bouton{padding:5px 12px;min-height:34px;border-radius:999px;border:1px solid var(--ligne);background:var(--surface);color:var(--encre);font:inherit;font-size:13px;cursor:pointer}
.valid__btn:hover,.val-bouton:hover{border-color:var(--accent)}
.valid__btn:focus-visible,.val-bouton:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.valid__btn.on{font-weight:600}
.valid__btn--attente.on{background:var(--code);border-color:var(--doux)}
.valid__btn--ok.on{background:var(--ok-fond);color:var(--ok);border-color:var(--ok)}
.valid__btn--ko.on{background:var(--ko-fond);color:var(--ko);border-color:var(--ko)}
.valid--cmp{display:flex;grid-column:1/-1}.valid--cmp .valid__btn{flex:1 1 180px;min-height:44px;font-size:15px;font-weight:600;border-radius:12px}
.val-barre{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:8px 0 12px}
.val-compteur{font-weight:600;font-size:13px;color:var(--encre)}.val-info{font-size:13px;color:var(--doux)}
.val-texte{width:100%;margin-top:8px;font:12px/1.4 ui-monospace,Consolas,monospace;background:var(--surface);color:var(--encre);border:1px solid var(--ligne);border-radius:8px}
.filtres-sons select#sons-valid{max-width:14em}
.cmp{display:grid;grid-template-columns:1fr 1fr;gap:12px;align-items:stretch;margin:6px 0}
.cmp__col{display:flex;flex-direction:column;min-width:0;padding:10px 12px;border:1px solid var(--ligne);border-radius:12px;background:var(--surface)}
.cmp__col--ancien{border-left:4px solid var(--doux)}.cmp__col--nouveau{border-left:4px solid var(--accent)}
.cmp__etiquette{margin:0 0 4px;font-size:12px;letter-spacing:.5px;text-transform:uppercase;font-weight:700;color:var(--doux)}
.cmp__col--nouveau .cmp__etiquette{color:var(--accent)}
.cmp__nom{margin:0 0 6px;font-size:14px}.cmp audio{display:block;width:100%;max-width:260px;height:32px}
.cmp__usage{margin:6px 0 0;font-size:13px;color:var(--doux);max-width:none}
tr.ligne-cmp td{padding-top:10px;padding-bottom:10px}
a.cmp-lien{font-weight:600}
.ecoute-cat{margin:10px 0;border:1px solid var(--ligne);border-radius:10px;background:var(--fond)}
.ecoute-cat summary{cursor:pointer;padding:8px 12px;font-weight:600}.ecoute-cat summary small{color:var(--doux);font-weight:400}
.encart-ecoute ul.ecoute-liste{list-style:none;margin:0;padding:0 12px 8px;font-size:inherit}
.ecoute-item{max-width:none;margin:0;padding:10px 0;border-top:1px solid var(--ligne)}
.ecoute-item__nom{margin:0 0 4px}.ecoute-item__corps{display:flex;flex-wrap:wrap;gap:10px;align-items:center}
.ecoute-item__lecteurs audio{display:block;width:240px;max-width:100%;height:32px}
@media (max-width:640px){.cmp{grid-template-columns:minmax(0,1fr)}.cmp audio{max-width:none}}
.jauge-kaykit{margin:12px 0 18px;padding:12px 14px;background:var(--surface);border:1px solid var(--ligne);border-radius:12px}
.jauge-kaykit__titre{margin:0 0 8px}.jauge-kaykit__note{margin:8px 0 0;font-size:12px;color:var(--doux)}
.jauge-kaykit__table{width:100%;border-collapse:collapse}.jauge-kaykit__table td{padding:4px 8px 4px 0;font-size:13px}.jauge-kaykit__table td:first-child{width:11em;color:var(--doux)}
.jauge-kenney table.jauge-kenney__remplacer{font-size:13px}.jauge-kenney__remplacer td,.jauge-kenney__remplacer th{padding:6px 8px}
.jauge-kenney ul{margin:6px 0 0;font-size:13px}
.jauge{display:flex;align-items:center;gap:8px}.jauge__piste{flex:1;height:10px;border-radius:5px;background:var(--code);overflow:hidden}
.jauge__remplissage{height:100%;background:var(--wait);border-radius:5px}.jauge__valeur{min-width:4em;font-weight:600}.jauge small{color:var(--doux);font-size:12px}
.spoil{margin:12px 0 16px;border:1px solid var(--ligne);border-radius:12px;background:var(--surface)}
.spoil summary{cursor:pointer;padding:10px 14px;font-weight:600;list-style:none}.spoil summary::-webkit-details-marker{display:none}
.spoil summary::before{content:"B8";display:inline-block;margin-right:8px;transition:transform .15s}.spoil[open] summary::before{transform:rotate(90deg)}
.spoil__alerte{color:var(--wait);margin-right:6px}.spoil__corps{padding:0 14px 12px}
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
    IMAGES_COPIEES.clear()
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
    copier_images(dossier)
    print("Wiki %s : %d pages dans %s" % ("joueur" if public else "développeur", len(pages), dossier))


def main():
    generer(public=False)
    generer(public=True)


if __name__ == "__main__":
    main()
