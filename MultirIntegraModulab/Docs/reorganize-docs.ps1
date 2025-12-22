# Script per reorganitzar la documentació de MultirIntegraModulab
# Executa des de la carpeta arrel del projecte

Write-Host "🚀 Reorganitzant documentació..." -ForegroundColor Cyan

$docsPath = ".\MultirIntegraModulab\Docs"

# Crear estructura de carpetes
Write-Host "`n📁 Creant estructura de carpetes..." -ForegroundColor Yellow

$folders = @(
    "$docsPath\getting-started",
    "$docsPath\overview",
    "$docsPath\technical",
    "$docsPath\features\classification",
    "$docsPath\features\microorganisms",
    "$docsPath\features\resistance",
    "$docsPath\features\validation",
    "$docsPath\features\incorporation",
    "$docsPath\features\audit",
    "$docsPath\guides",
    "$docsPath\tutorials",
    "$docsPath\reference",
    "$docsPath\examples",
    "$docsPath\contributing",
    "$docsPath\resources",
    "$docsPath\assets\images\screenshots",
    "$docsPath\assets\images\diagrams",
    "$docsPath\assets\images\logos",
    "$docsPath\assets\css",
    "$docsPath\assets\js",
    "$docsPath\_templates"
)

foreach ($folder in $folders) {
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        Write-Host "  ✅ Creat: $folder" -ForegroundColor Green
    } else {
        Write-Host "  ⏭️  Ja existeix: $folder" -ForegroundColor Gray
    }
}

# Moure fitxers existents
Write-Host "`n📦 Movent fitxers existents..." -ForegroundColor Yellow

$moves = @{
    # Documents generals
    "$docsPath\RESUM_EXECUTIU.md" = "$docsPath\overview\RESUM_EXECUTIU.md"
    "$docsPath\RESUM_FINAL.md" = "$docsPath\overview\RESUM_FINAL.md"
    
    # Documents tècnics
    "$docsPath\PROCES_CAPTACIO_DADES.md" = "$docsPath\technical\PROCES_CAPTACIO_DADES.md"
    "$docsPath\DIAGRAMES_FLUX_MERMAID.md" = "$docsPath\technical\DIAGRAMES_FLUX_MERMAID.md"
    
    # Comprovacions (validació)
    "$docsPath\DIAGRAMES_COMPROVACIONS.md" = "$docsPath\features\validation\DIAGRAMES_COMPROVACIONS.md"
    "$docsPath\COMPROVACIO_1_NEGATIUS.md" = "$docsPath\features\validation\COMPROVACIO_1_NEGATIUS.md"
    "$docsPath\COMPROVACIO_2_NEGATIUS.md" = "$docsPath\features\validation\COMPROVACIO_2_NEGATIUS.md"
    "$docsPath\COMPROVACIONS_NEGATIUS_RESUM.md" = "$docsPath\features\validation\COMPROVACIONS_NEGATIUS_RESUM.md"
    "$docsPath\IMPLEMENTACIO_COMPROVACIONS_RESUM.md" = "$docsPath\features\validation\IMPLEMENTACIO_COMPROVACIONS_RESUM.md"
    
    # Recursos
    "$docsPath\README_LogIndentHelper.md" = "$docsPath\resources\README_LogIndentHelper.md"
    "$docsPath\TRACTAMENT_MOSTRES_ANTIGUES.md" = "$docsPath\resources\TRACTAMENT_MOSTRES_ANTIGUES.md"
    
    # Index general (mantenir còpia a l'arrel i moure a contributing)
    "$docsPath\INDEX_GENERAL.md" = "$docsPath\contributing\INDEX_GENERAL_OLD.md"
}

foreach ($source in $moves.Keys) {
    $dest = $moves[$source]
    
    if (Test-Path $source) {
        # Crear directori de destinació si no existeix
        $destDir = Split-Path $dest -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        # Moure fitxer
        Move-Item -Path $source -Destination $dest -Force
        Write-Host "  ✅ Mogut: $(Split-Path $source -Leaf) → $(Split-Path $dest -Parent)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  No trobat: $source" -ForegroundColor DarkYellow
    }
}

# Crear README.md principal que redirigeix a index.md
Write-Host "`n📝 Creant README.md..." -ForegroundColor Yellow

$readmeContent = @"
# MultirIntegraModulab - Documentació

> **📚 Portal de documentació**: [index.md](index.md)

Aquest repositori conté la documentació completa del sistema d'integració MultirIntegraModulab.

## 🚀 Accés Ràpid

- **[Portal Principal](index.md)** - Pàgina d'inici del portal
- **[Guia Ràpida](getting-started/quick-start.md)** - Començar en 15 minuts
- **[Documentació Tècnica](technical/PROCES_CAPTACIO_DADES.md)** - Detall complet
- **[Diagrames](technical/DIAGRAMES_FLUX_MERMAID.md)** - Visualitzacions interactives

## 📋 Estructura

``````
Docs/
├── index.md                    # 🏠 Portal principal
├── getting-started/            # 🚀 Començar ràpid
├── overview/                   # 🎯 Documents generals
├── technical/                  # 🔧 Documentació tècnica
├── features/                   # ⚙️ Funcionalitats
│   ├── classification/
│   ├── microorganisms/
│   ├── resistance/
│   ├── validation/
│   ├── incorporation/
│   └── audit/
├── guides/                     # 📖 Guies pràctiques
├── tutorials/                  # 🎓 Tutorials
├── reference/                  # 📋 Referència
├── examples/                   # 💡 Exemples
└── contributing/               # 🤝 Contribuir
``````

## 🎯 Per On Començar?

### Si ets...

- **👨‍💼 Direcció/Gestió**: [Resum Executiu](overview/RESUM_EXECUTIU.md)
- **👨‍💻 Desenvolupador**: [Procés Captació Dades](technical/PROCES_CAPTACIO_DADES.md)
- **🎨 Analista**: [Diagrames Flux](technical/DIAGRAMES_FLUX_MERMAID.md)
- **🔬 Usuari Final**: [Guia Ràpida](getting-started/quick-start.md)

## 📞 Contacte

- **Issues**: [GitHub Issues](https://github.com/CarlosCastillo70/MultirIntegraModulab/issues)
- **Email**: suport@multir.cat

---

**Versió**: 1.0.0  
**Data**: Gener 2025
"@

Set-Content -Path "$docsPath\README.md" -Value $readmeContent -Encoding UTF8
Write-Host "  ✅ Creat: README.md" -ForegroundColor Green

# Crear fitxer .gitkeep per carpetes buides
Write-Host "`n📌 Creant .gitkeep per carpetes buides..." -ForegroundColor Yellow

$emptyFolders = @(
    "$docsPath\features\classification",
    "$docsPath\features\microorganisms",
    "$docsPath\features\resistance",
    "$docsPath\features\incorporation",
    "$docsPath\features\audit",
    "$docsPath\guides",
    "$docsPath\tutorials",
    "$docsPath\reference",
    "$docsPath\examples",
    "$docsPath\assets\images\screenshots",
    "$docsPath\assets\images\diagrams",
    "$docsPath\assets\images\logos",
    "$docsPath\assets\css",
    "$docsPath\assets\js",
    "$docsPath\_templates"
)

foreach ($folder in $emptyFolders) {
    $gitkeepPath = "$folder\.gitkeep"
    if (-not (Test-Path $gitkeepPath)) {
        New-Item -ItemType File -Path $gitkeepPath -Force | Out-Null
        Write-Host "  ✅ Creat .gitkeep a: $(Split-Path $folder -Leaf)" -ForegroundColor Green
    }
}

# Resum final
Write-Host "`n✅ Reorganització completada!" -ForegroundColor Green
Write-Host "`n📊 Resum:" -ForegroundColor Cyan
Write-Host "  • Carpetes creades: $($folders.Count)" -ForegroundColor White
Write-Host "  • Fitxers moguts: $($moves.Count)" -ForegroundColor White
Write-Host "  • Fitxers nous creats: index.md, README.md, índexs de seccions" -ForegroundColor White

Write-Host "`n🎯 Pròxims passos:" -ForegroundColor Yellow
Write-Host "  1. Revisar els fitxers moguts" -ForegroundColor White
Write-Host "  2. Actualitzar enllaços interns si cal" -ForegroundColor White
Write-Host "  3. Crear documents pendents (guides, tutorials, reference)" -ForegroundColor White
Write-Host "  4. Configurar DocFX o MkDocs per generar el portal web" -ForegroundColor White

Write-Host "`n🚀 Portal accessible a: $docsPath\index.md" -ForegroundColor Cyan
Write-Host ""
"@

Set-Content -Path ".\MultirIntegraModulab\Docs\reorganize-docs.ps1" -Value $readmeContent -Encoding UTF8
Write-Host "  ✅ Creat: README.md" -ForegroundColor Green
