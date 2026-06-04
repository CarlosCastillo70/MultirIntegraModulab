# 📚 ÍNDEX DE DOCUMENTACIÓ - MultirIntegraModulab Windows Service

Guia ràpida per trobar la documentació que necessites.

---

## 🚀 COMENÇAR AQUÍ

| Document | Descripció | Quan utilitzar |
|----------|------------|----------------|
| **[VALIDATION-FINAL.md](VALIDATION-FINAL.md)** | ✅ Validació final i veredicte | Per confirmar que tot està OK |
| **[PRODUCTION-READY-SUMMARY.md](PRODUCTION-READY-SUMMARY.md)** | 📋 Resum executiu i guia ràpida | Per desplegament ràpid |
| **[APP-CONFIG-RESPOSTA-RAPIDA.md](APP-CONFIG-RESPOSTA-RAPIDA.md)** | ❓ Resposta sobre App.config | Si tens dubtes sobre configuració |

---

## 📖 DOCUMENTACIÓ COMPLETA

### 🔧 Desplegament i Instal·lació

| Document | Contingut |
|----------|-----------|
| **[DEPLOYMENT-README.md](DEPLOYMENT-README.md)** | Guia completa de desplegament pas a pas |
| **[PRE-PRODUCTION-CHECKLIST.md](PRE-PRODUCTION-CHECKLIST.md)** | Checklist exhaustiu de validació pre-producció |
| **[Install-Service.ps1](Install-Service.ps1)** | Script d'instal·lació del servei (PowerShell) |
| **[Uninstall-Service.ps1](Uninstall-Service.ps1)** | Script de desinstal·lació del servei (PowerShell) |
| **[Verify-Build.ps1](Verify-Build.ps1)** | Script per verificar build i fitxers de configuració |

### 🔍 Revisió Tècnica

| Document | Contingut |
|----------|-----------|
| **[PRE-PRODUCTION-REVIEW.md](PRE-PRODUCTION-REVIEW.md)** | Informe detallat de revisió tècnica |
| **[VALIDATION-FINAL.md](VALIDATION-FINAL.md)** | Validació final amb correccions aplicades |

### ⚙️ Configuració

| Document | Contingut |
|----------|-----------|
| **[APP-CONFIG-EXPLICACIO.md](APP-CONFIG-EXPLICACIO.md)** | Explicació completa sobre App.config i compilació |
| **[APP-CONFIG-RESPOSTA-RAPIDA.md](APP-CONFIG-RESPOSTA-RAPIDA.md)** | Resposta ràpida sobre App.config |
| **[workflow-schedule.json](workflow-schedule.json)** | Configuració de tasques programades (CRON) |

### 📝 README General

| Document | Contingut |
|----------|-----------|
| **[README.md](README.md)** | README general del Windows Service |

---

## 🎯 CASOS D'ÚS COMUNS

### "Vull desplegar el servei ara mateix"
1. **Llegir**: [PRODUCTION-READY-SUMMARY.md](PRODUCTION-READY-SUMMARY.md)
2. **Executar**: `.\Verify-Build.ps1` (verificar build)
3. **Seguir**: Passos d'instal·lació del Summary

### "Vull fer una revisió completa abans de producció"
1. **Llegir**: [PRE-PRODUCTION-REVIEW.md](PRE-PRODUCTION-REVIEW.md)
2. **Seguir**: [PRE-PRODUCTION-CHECKLIST.md](PRE-PRODUCTION-CHECKLIST.md)
3. **Confirmar**: [VALIDATION-FINAL.md](VALIDATION-FINAL.md)

### "Tinc dubtes sobre l'App.config"
1. **Resposta ràpida**: [APP-CONFIG-RESPOSTA-RAPIDA.md](APP-CONFIG-RESPOSTA-RAPIDA.md)
2. **Detalls complets**: [APP-CONFIG-EXPLICACIO.md](APP-CONFIG-EXPLICACIO.md)

### "Vull modificar la freqüència d'execució"
1. **Editar**: [workflow-schedule.json](workflow-schedule.json)
2. **Consultar**: [DEPLOYMENT-README.md](DEPLOYMENT-README.md) (secció "Expressions CRON")
3. **Reiniciar**: `Restart-Service MultirIntegraModulabService`

### "Vull instal·lar/desinstal·lar el servei"
- **Instal·lar**: Executar `.\Install-Service.ps1` (com a Admin)
- **Desinstal·lar**: Executar `.\Uninstall-Service.ps1` (com a Admin)

### "Vull monitoritzar el servei"
1. **Consultar**: [PRODUCTION-READY-SUMMARY.md](PRODUCTION-READY-SUMMARY.md) (secció "Monitorització")
2. **Event Viewer**: `eventvwr.msc` → Application
3. **Filtrar per**: `MultirIntegraModulabService` o `MultirRevisioVigenciaService`

---

## 📊 MAPA DE CONCEPTES

```
Windows Service (MultirIntegraModulab.Service)
│
├── 🔧 Configuració
│   ├── workflow-schedule.json          → Tasques i CRON
│   └── App.config                      → Config del servei
│
├── 📦 Executables Gestionats
│   ├── MultirIntegraModulab.exe
│   │   ├── Freqüència: Cada 15 minuts
│   │   └── Config: MultirIntegraModulab.exe.config
│   │
│   └── MultirRevisioVigencia.exe
│       ├── Freqüència: Diari 4:00 AM
│       └── Config: MultirRevisioVigencia.exe.config
│
├── 📝 Logs
│   ├── Event Viewer → MultirIntegraModulabService
│   └── Event Viewer → MultirRevisioVigenciaService
│
└── 🛠️ Gestió
	├── Start-Service
	├── Stop-Service
	└── Restart-Service
```

---

## ✅ CHECKLIST RÀPID

Abans de desplegar, assegura't que has:

- [ ] Llegit [VALIDATION-FINAL.md](VALIDATION-FINAL.md)
- [ ] Executat `.\Verify-Build.ps1` sense errors
- [ ] Revisat [PRE-PRODUCTION-CHECKLIST.md](PRE-PRODUCTION-CHECKLIST.md)
- [ ] Configurat correctament els **App.config** (entorn Producció/Preproduccio)
- [ ] Verificat que tens PowerShell amb permisos d'**Administrador**

---

## 🔗 LINKS RÀPIDS

### Documentació Codi
- [Jobs\ProcessarMostresModulabJob.cs](Jobs/ProcessarMostresModulabJob.cs)
- [Jobs\RevisarVigenciaDiagnosticsJob.cs](Jobs/RevisarVigenciaDiagnosticsJob.cs)
- [Services\WorkflowService.cs](Services/WorkflowService.cs)
- [Program.cs](Program.cs)

### Configuració
- [App.config](App.config)
- [workflow-schedule.json](workflow-schedule.json)

### Scripts
- [Install-Service.ps1](Install-Service.ps1)
- [Uninstall-Service.ps1](Uninstall-Service.ps1)
- [Verify-Build.ps1](Verify-Build.ps1)

---

## 📞 SUPORT

**Repositori**: https://github.com/CarlosCastillo70/MultirIntegraModulab  
**Branch**: developer

Per qualsevol dubte, consulta primer aquest índex per trobar la documentació rellevant.

---

**Última actualització**: 25/01/2026  
**Versió documentació**: 1.0.0
