# Exemple Visual del Nou Sistema de Logs

## Escenari: Execucions cada hora durant un dia

```
📁 C:\Projectes\MultirIntegraModulab\bin\Release\net48\Logs\

📄 multir2025-01-27_08-00-15.log    (Primera execució - 8:00)
📄 multir2025-01-27_09-00-10.log    (Segona execució - 9:00)
📄 multir2025-01-27_10-00-05.log    (Tercera execució - 10:00)
📄 multir2025-01-27_11-00-22.log    (Quarta execució - 11:00)
📄 multir2025-01-27_12-00-18.log    (Cinquena execució - 12:00)
📄 multir2025-01-27_13-00-08.log    (Sisena execució - 13:00)
📄 multir2025-01-27_14-00-12.log    (Setena execució - 14:00)
📄 multir2025-01-27_15-00-25.log    (Vuitena execució - 15:00)
📄 multir2025-01-27_16-00-31.log    (Novena execució - 16:00)
📄 multir2025-01-27_17-00-09.log    (Desena execució - 17:00)
📄 multir2025-01-27_18-00-14.log    (Onzena execució - 18:00)
```

## Contingut d'un Fitxer de Log Individual

### Exemple: `multir2025-01-27_14-00-12.log`

```
================================================================================
INICI NOVA EXECUCIÓ - 2025-01-27 14:00:12
Fitxer de log: multir2025-01-27_14-00-12.log
Versió de l'aplicació: 1.0.0.0
================================================================================
2025-01-27 14:00:12 INFO : === Iniciant aplicació d' integració de dades de Modulab a MultiR ===
2025-01-27 14:00:12 INFO : 
==============================================
CONFIGURACIÓ DE L'APLICACIÓ
==============================================

ENTORN:
  - Entorn actiu: PREPRODUCCIO
  - És producció: NO

CÀRREGA DE DADES:
  - Mode càrrega: Incremental (optimitzada)
  - Dies enrere: 1 (només per primera càrrega o si falla sincronització)
  - Límit resultats: Il·limitat
...

2025-01-27 14:00:13 INFO : 👀 Comprovant connexions a bases de dades...
2025-01-27 14:00:13 INFO : ✅ Oracle Database - Connexió correcta. Data: 27/01/2025
2025-01-27 14:00:13 INFO : ✅ MySQL - Connexió correcta. Data: 27/01/2025 14:00:13
2025-01-27 14:00:13 INFO : 🔍 Carregant mostres amb càrrega incremental
2025-01-27 14:00:15 INFO : 📊 Estadístiques: 5 mostres, 4 validades, 1 pendents de validació
2025-01-27 14:00:15 INFO : 🔄 Començem a processar les mostres ...
2025-01-27 14:00:18 INFO : ✅ Mostra 2025001234 processada correctament
2025-01-27 14:00:19 INFO : ✅ Mostra 2025001235 processada correctament
2025-01-27 14:00:20 INFO : ✅ Mostra 2025001236 processada correctament
2025-01-27 14:00:21 INFO : ✅ Mostra 2025001237 processada correctament
2025-01-27 14:00:22 INFO : ⚠️ Mostra 2025001238 - Pendent de validació
2025-01-27 14:00:22 INFO : 💾 Guardant dades de sincronització...
2025-01-27 14:00:22 INFO : ✅ Sincronització guardada correctament (ID: 145)
2025-01-27 14:00:22 INFO : ✅ Execució finalitzada correctament
================================================================================
FINAL EXECUCIÓ - 2025-01-27 14:00:22
================================================================================
2025-01-27 14:00:23 INFO : 📧 Enviant email amb el resum del processament
2025-01-27 14:00:23 INFO : 📎 Adjuntant fitxer de log: multir2025-01-27_14-00-12.log
2025-01-27 14:00:24 INFO : 📤 Enviant email a 1 destinatari(s) via smtp.trueta.intranet:25...
2025-01-27 14:00:25 INFO : ✅ Email enviat correctament a: admin@hospital.cat
```

## Email Enviat per l'Execució de les 14:00

```
De: multir@hospital.cat
Per a: admin@hospital.cat
Assumpte: MultiR - Integració Modulab - 27/01/2025 14:00
Adjunt: multir2025-01-27_14-00-12.log (15 KB)

=================================================
    MULTIR - INTEGRACIÓ MODULAB
=================================================

Data d'execució: 27/01/2025 14:00:22

RESUM DEL PROCESSAMENT:
-------------------
• Total processats:        5
• Noves incorporacions:    4
• Repetides:               0
• Validades:               4
• Revalidades:             0
• Desvalidades:            0
• Antigues:                0
• Errors:                  0
• Durada:                  10.52 segons

Percentatge d'èxit: 100.0%

=================================================

Per més detalls, consulta el fitxer de log adjunt.

--
Aquest és un missatge automàtic del sistema MultiR
```

## Avantatges Visuals

### ✅ **Identificació Ràpida**
Pots identificar immediatament quin log correspon a quina execució:
- `multir2025-01-27_14-00-12.log` → Execució de les 14:00
- `multir2025-01-27_15-00-25.log` → Execució de les 15:00

### ✅ **Troubleshooting Fàcil**
Si hi ha un problema a les 14:00, obres directament el fitxer de les 14:00.
No cal buscar en un fitxer enorme amb logs de tot el dia.

### ✅ **Emails Precisos**
Cada email adjunta només el log de la seva execució:
- Email de les 14:00 → Adjunta `multir2025-01-27_14-00-12.log`
- Email de les 15:00 → Adjunta `multir2025-01-27_15-00-25.log`

### ✅ **Historial Complet**
Es mantenen tots els logs de totes les execucions:
- Pots comparar execucions diferents
- Pots veure l'evolució durant el dia
- Pots detectar patrons d'errors

## Comparació Abans vs Després

### ❌ **ABANS (1 fitxer per dia)**
```
📄 multir2025-01-27.log (250 KB)
   ├─ Execució 08:00 (20 KB)
   ├─ Execució 09:00 (18 KB)
   ├─ Execució 10:00 (22 KB)
   ├─ Execució 11:00 (19 KB)
   ├─ Execució 12:00 (21 KB)
   ├─ Execució 13:00 (20 KB)
   ├─ Execució 14:00 (15 KB) ← Difícil de trobar!
   ├─ Execució 15:00 (25 KB)
   └─ ... (tot barrejat)
```

**Problemes:**
- Email adjunta tot el fitxer (250 KB) amb logs de totes les execucions
- Difícil trobar el log d'una execució específica
- Cal llegir tot el fitxer per trobar errors

### ✅ **DESPRÉS (1 fitxer per execució)**
```
📄 multir2025-01-27_08-00-15.log (20 KB)
📄 multir2025-01-27_09-00-10.log (18 KB)
📄 multir2025-01-27_10-00-05.log (22 KB)
📄 multir2025-01-27_11-00-22.log (19 KB)
📄 multir2025-01-27_12-00-18.log (21 KB)
📄 multir2025-01-27_13-00-08.log (20 KB)
📄 multir2025-01-27_14-00-12.log (15 KB) ← Fàcil de trobar!
📄 multir2025-01-27_15-00-25.log (25 KB)
```

**Avantatges:**
- Email adjunta només el log de l'execució actual (15 KB)
- Identificació immediata per timestamp
- Cada log és compacte i fàcil de llegir

## Configuració del Programador de Tasques de Windows

Si utilitzes el Programador de Tasques de Windows per executar l'aplicació cada hora:

```
Nom de la tasca: MultirIntegraModulab_Hourly
Desencadenador: Diari, cada 1 hora
Hora d'inici: 08:00
Durada: 1 dia
Repetir cada: 1 hora
Acció: Executar programa
Programa: C:\Projectes\MultirIntegraModulab\bin\Release\net48\MultirIntegraModulab.exe
```

**Resultat**: Es crearà un nou log cada hora amb el timestamp exacte de quan s'ha executat.
