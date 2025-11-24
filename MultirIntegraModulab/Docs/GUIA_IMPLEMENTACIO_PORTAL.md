# 📚 Guia d'Implementació del Portal de Documentació

Aquest document explica com implementar i desplegar el portal de documentació reorganitzat.

---

## 🎯 Estructura Final

```
MultirIntegraModulab/
├── mkdocs.yml                           # Configuració MkDocs
├── reorganize-docs.ps1                  # Script reorganització
│
└── MultirIntegraModulab/
    └── Docs/
        ├── index.md                     # 🏠 Portal principal
        ├── README.md                    # Redirecció a index.md
        │
        ├── getting-started/             # 🚀 Començar ràpid
        │   ├── index.md
        │   ├── quick-start.md
        │   ├── installation.md
        │   └── first-steps.md
        │
        ├── overview/                    # 🎯 Documents generals
        │   ├── index.md
        │   ├── RESUM_EXECUTIU.md
        │   ├── RESUM_FINAL.md
        │   ├── architecture.md
        │   └── roadmap.md
        │
        ├── technical/                   # 🔧 Documentació tècnica
        │   ├── index.md
        │   ├── PROCES_CAPTACIO_DADES.md
        │   ├── DIAGRAMES_FLUX_MERMAID.md
        │   ├── api-reference.md
        │   ├── data-model.md
        │   └── configuration.md
        │
        ├── features/                    # ⚙️ Funcionalitats
        │   ├── index.md
        │   ├── classification/
        │   ├── microorganisms/
        │   ├── resistance/
        │   ├── validation/              # ✅ Comprovacions
        │   │   ├── index.md
        │   │   ├── DIAGRAMES_COMPROVACIONS.md
        │   │   ├── COMPROVACIO_1_NEGATIUS.md
        │   │   ├── COMPROVACIO_2_NEGATIUS.md
        │   │   ├── COMPROVACIONS_NEGATIUS_RESUM.md
        │   │   └── IMPLEMENTACIO_COMPROVACIONS_RESUM.md
        │   ├── incorporation/
        │   └── audit/
        │
        ├── guides/                      # 📖 Guies
        ├── tutorials/                   # 🎓 Tutorials
        ├── reference/                   # 📋 Referència
        ├── examples/                    # 💡 Exemples
        ├── contributing/                # 🤝 Contribuir
        ├── resources/                   # 📦 Recursos
        │   ├── README_LogIndentHelper.md
        │   └── TRACTAMENT_MOSTRES_ANTIGUES.md
        │
        └── assets/                      # 🎨 Recursos estàtics
            ├── images/
            ├── css/
            └── js/
```

---

## 🚀 Pas 1: Executar Script de Reorganització

### Windows (PowerShell)

```powershell
# Des de la carpeta arrel del projecte
.\reorganize-docs.ps1
```

Això farà:
- ✅ Crear estructura de carpetes
- ✅ Moure fitxers existents
- ✅ Crear índexs de secció
- ✅ Crear README.md

---

## 📦 Pas 2: Instal·lar MkDocs

### Opció A: Amb Python i pip (Recomanat)

```bash
# Instal·lar Python 3.8+ (si no el tens)
# https://www.python.org/downloads/

# Instal·lar MkDocs i Material Theme
pip install mkdocs
pip install mkdocs-material
pip install mkdocs-mermaid2-plugin
pip install mkdocs-git-revision-date-localized-plugin
pip install mkdocs-minify-plugin
pip install mkdocs-awesome-pages-plugin
```

### Opció B: Amb requirements.txt

Crear fitxer `requirements.txt`:

```
mkdocs>=1.5.0
mkdocs-material>=9.5.0
mkdocs-mermaid2-plugin>=1.1.0
mkdocs-git-revision-date-localized-plugin>=1.2.0
mkdocs-minify-plugin>=0.7.0
mkdocs-awesome-pages-plugin>=2.9.0
pymdown-extensions>=10.0
```

Instal·lar:

```bash
pip install -r requirements.txt
```

---

## 🎨 Pas 3: Personalitzar Estils (Opcional)

### Crear CSS Custom

```css MultirIntegraModulab/Docs/assets/css/custom.css
/* Colors corporatius */
:root {
    --primary-color: #3f51b5;
    --secondary-color: #7986cb;
    --success-color: #4caf50;
    --warning-color: #ff9800;
    --error-color: #f44336;
}

/* Estils de targetes */
.user-cards {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1.5rem;
    margin: 2rem 0;
}

.user-cards > div {
    border: 1px solid var(--md-default-fg-color--lightest);
    border-radius: 8px;
    padding: 1.5rem;
    transition: transform 0.2s, box-shadow 0.2s;
}

.user-cards > div:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

/* Badges */
.badge {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 12px;
    font-size: 0.875rem;
    font-weight: 500;
}

.badge-success { background: var(--success-color); color: white; }
.badge-warning { background: var(--warning-color); color: white; }
.badge-error { background: var(--error-color); color: white; }

/* Botons CTA */
.cta-buttons {
    display: flex;
    gap: 1rem;
    margin: 2rem 0;
    flex-wrap: wrap;
}

.btn {
    padding: 0.75rem 1.5rem;
    border-radius: 4px;
    text-decoration: none;
    font-weight: 500;
    transition: all 0.2s;
}

.btn-primary {
    background: var(--primary-color);
    color: white;
}

.btn-secondary {
    background: var(--secondary-color);
    color: white;
}

.btn:hover {
    transform: translateY(-2px);
    box-shadow: 0 2px 8px rgba(0,0,0,0.2);
}

/* Millores de llegibilitat */
article h1 { color: var(--primary-color); }
article h2 { color: var(--secondary-color); }

/* Taules responsives */
table {
    display: block;
    overflow-x: auto;
    white-space: nowrap;
}
```

### Crear JavaScript Custom

```javascript MultirIntegraModulab/Docs/assets/js/custom.js
// Inicialització Mermaid
if (typeof mermaid !== 'undefined') {
    mermaid.initialize({
        startOnLoad: true,
        theme: 'default',
        themeVariables: {
            primaryColor: '#3f51b5',
            primaryTextColor: '#fff',
            primaryBorderColor: '#7c4dff',
            lineColor: '#f5f5f5',
            secondaryColor: '#7986cb',
            tertiaryColor: '#fff'
        }
    });
}

// Smooth scroll per enllaços interns
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({ behavior: 'smooth' });
        }
    });
});

// Copiar codi al portapapers
document.querySelectorAll('pre code').forEach((block) => {
    const button = document.createElement('button');
    button.className = 'copy-button';
    button.textContent = 'Copiar';
    
    button.addEventListener('click', () => {
        navigator.clipboard.writeText(block.textContent);
        button.textContent = '✓ Copiat!';
        setTimeout(() => {
            button.textContent = 'Copiar';
        }, 2000);
    });
    
    block.parentNode.appendChild(button);
});
```

---

## 🌐 Pas 4: Previsualitzar Localment

```bash
# Des de la carpeta arrel (on és mkdocs.yml)
mkdocs serve

# O especificant port
mkdocs serve --dev-addr=127.0.0.1:8080
```

Obre el navegador a: `http://localhost:8000`

---

## 📤 Pas 5: Compilar per Producció

```bash
# Generar lloc estàtic a la carpeta 'site'
mkdocs build

# Verificar que no hi hagi enllaços trencats
mkdocs build --strict
```

Resultat: Carpeta `site/` amb HTML estàtic.

---

## 🚀 Pas 6: Desplegar

### Opció A: GitHub Pages (Gratuït)

```bash
# Desplegar automàticament a GitHub Pages
mkdocs gh-deploy

# O amb missatge personalitzat
mkdocs gh-deploy --message "Actualització documentació {sha}"
```

Això:
1. Compila la documentació
2. Fa push a la branca `gh-pages`
3. GitHub Pages publica automàticament

**URL resultant**: `https://carloscastillo70.github.io/MultirIntegraModulab/`

### Opció B: Servidor Propi

```bash
# Copiar carpeta 'site' al servidor
scp -r site/ user@server:/var/www/docs/

# O amb rsync
rsync -avz --delete site/ user@server:/var/www/docs/
```

### Opció C: Netlify / Vercel

1. Connectar repositori GitHub
2. Build command: `mkdocs build`
3. Publish directory: `site`
4. Desplega automàticament a cada commit

---

## 🔄 Pas 7: Configurar CI/CD (Opcional)

### GitHub Actions

Crear `.github/workflows/docs.yml`:

```yaml
name: Deploy Documentation

on:
  push:
    branches:
      - main
      - developer
    paths:
      - 'MultirIntegraModulab/Docs/**'
      - 'mkdocs.yml'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0
      
      - uses: actions/setup-python@v4
        with:
          python-version: 3.x
      
      - run: pip install mkdocs-material mkdocs-mermaid2-plugin
      
      - run: mkdocs gh-deploy --force
```

---

## ✅ Pas 8: Verificar i Mantenir

### Checklist de Verificació

- [ ] Tots els enllaços funcionen
- [ ] Imatges es carreguen correctament
- [ ] Diagrames Mermaid es renderitzen
- [ ] Cerca funciona
- [ ] Responsive (mòbil/tauleta/desktop)
- [ ] Temps de càrrega acceptable (<2s)
- [ ] SEO optimitzat (títols, meta descriptions)

### Mantenir Actualitzat

```bash
# Actualitzar dependències
pip install --upgrade mkdocs mkdocs-material

# Regenerar lloc
mkdocs build --clean

# Redesplegar
mkdocs gh-deploy
```

---

## 🎨 Pas 9: Personalitzacions Avançades

### Afegir Logo i Favicon

```yaml mkdocs.yml
theme:
  favicon: assets/images/logos/favicon.ico
  logo: assets/images/logos/logo.png
```

Col·locar imatges a:
- `MultirIntegraModulab/Docs/assets/images/logos/favicon.ico`
- `MultirIntegraModulab/Docs/assets/images/logos/logo.png`

### Configurar Google Analytics

```yaml mkdocs.yml
extra:
  analytics:
    provider: google
    property: G-XXXXXXXXXX
```

### Afegir Cerca Avançada

```yaml mkdocs.yml
plugins:
  - search:
      lang: ca
      separator: '[\s\-,:!=\[\]()"/]+|(?!\b)(?=[A-Z][a-z])|\.(?!\d)|&[lg]t;'
      prebuild_index: true
```

---

## 📊 Pas 10: Mètriques i Analítiques

### Habilitar Feedback

```yaml mkdocs.yml
extra:
  analytics:
    feedback:
      title: Ha estat útil aquesta pàgina?
      ratings:
        - icon: material/emoticon-happy-outline
          name: Útil
          data: 1
        - icon: material/emoticon-sad-outline
          name: No útil
          data: 0
```

### Seguiment de Versions

```bash
# Instal·lar mike per versionat
pip install mike

# Desplegar versió específica
mike deploy 1.0 latest --update-aliases
mike set-default latest
```

---

## 🆘 Troubleshooting

### Error: "Config file 'mkdocs.yml' does not exist"
**Solució**: Executar des de la carpeta on està `mkdocs.yml`.

### Error: "No module named 'mkdocs'"
**Solució**: `pip install mkdocs`

### Els diagrames Mermaid no es renderitzen
**Solució**: 
```bash
pip install mkdocs-mermaid2-plugin
```
I verificar que està a `mkdocs.yml`:
```yaml
plugins:
  - mermaid2
```

### Enllaços trencats
**Solució**: Executar `mkdocs build --strict` per detectar-los.

---

## 📚 Recursos

### Documentació Oficial
- [MkDocs](https://www.mkdocs.org/)
- [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/)
- [Mermaid](https://mermaid.js.org/)

### Exemples
- [MkDocs Material Showcase](https://squidfunk.github.io/mkdocs-material/showcase/)
- [Material Reference](https://squidfunk.github.io/mkdocs-material/reference/)

---

## 🎉 Resultat Final

Després de seguir aquesta guia tindràs:

✅ Portal de documentació professional  
✅ Estructura organitzada i escalable  
✅ Cerca integrada  
✅ Diagrames interactius  
✅ Responsive design  
✅ Dark mode  
✅ Desplegament automatitzat  
✅ Analytics i feedback  
✅ Versionat  

---

**Creat**: Gener 2025  
**Versió**: 1.0  
**Autor**: Equip de Desenvolupament
