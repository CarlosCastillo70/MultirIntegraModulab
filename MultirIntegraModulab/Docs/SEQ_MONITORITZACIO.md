# ?? Implementació de Seq per Monitorització de Logs

## ?? Objectiu

Implementar **Seq** com a sistema centralitzat de monitorització, cerca i anàlisi de logs per als projectes **MultirIntegraModulab** i **MultirRevisioVigencia**.

---

## ?? Què és Seq?

**Seq** és una plataforma moderna per:
- ?? **Cerca avançada** de logs estructurats
- ?? **Dashboards** i visualitzacions en temps real
- ?? **Alertes** configurables per errors o esdeveniments
- ?? **Anàlisi** de tendències i patrons
- ?? **Visualització** elegant i intuïtiva

### Avantatges de Seq amb Serilog

| Avantatge | Descripció |
|-----------|------------|
| ? **Logging Estructurat** | Cerca per propietats, no només text |
| ? **Temps Real** | Visualitza logs mentre s'executa l'aplicació |
| ? **Filtres Potents** | Queries SQL-like per trobar logs específics |
| ? **Dashboards** | Crea gràfics i visualitzacions personalitzades |
| ? **Alertes** | Notificacions automàtiques per errors crítics |

---

## ?? Instal·lació de Seq

### **Opció A: Seq Local (Recomanat per Desenvolupament)**

#### **Windows (amb Chocolatey)**
```powershell
choco install seq -y
```

#### **Windows (Instal·lador Manual)**
1. Descarregar de: https://datalust.co/download
2. Executar l'instal·lador
3. Seq estarà disponible a: `http://localhost:5341`

---

### **Opció B: Seq amb Docker (Recomanat per Producció)**

```sh
# Executar Seq en un contenidor Docker
docker run --name seq -d \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest
```

Seq estarà disponible a: `http://localhost:5341`

---

### **Opció C: Seq a Azure/AWS**

Seq es pot desplegar a:
- **Azure Container Instances**
- **AWS ECS**
- **Kubernetes**
- **Màquina virtual dedicada**

Consultar: https://docs.datalust.co/docs/deployment

---

##

 ? **Estat d'Implementació**

### **MultirIntegraModulab** ? COMPLETAT

| Component | Estat |
|-----------|-------|
| **Paquet Serilog.Sinks.Seq** | ? Instal·lat (v8.0.0) |
| **Configuració App.config** | ? Afegida |
| **LoggerService.cs** | ? Modificat amb suport Seq |
| **Compilació** | ? Successful |

### **MultirRevisioVigencia** ?? PENDENT

| Component | Estat |
|-----------|-------|
| **Paquet Serilog.Sinks.Seq** | ?? Descarregat però no referenciat correctament |
| **Configuració App.config** | ? Afegida |
| **SerilogLoggerService.cs** | ?? Codi comentat (TODO) |
| **Compilació** | ? Successful (amb Seq desactivat) |

---

## ?? Configuració

### **App.config**

La configuració de Seq s'ha afegit als dos projectes:

```xml
<appSettings>
  <!-- ============================================ -->
  <!-- CONFIGURACIÓ DE SEQ (Monitorització de Logs) -->
  <!-- ============================================ -->

  <!-- Activar enviament de logs a Seq per monitorització en temps real -->
  <!-- PRODUCCIÓ: "true" per tenir observabilitat completa -->
  <!-- DESENVOLUPAMENT: "true" per debugging avançat -->
  <add key="Seq:Actiu" value="false" />

  <!-- URL del servidor Seq -->
  <!-- Local: http://localhost:5341 -->
  <!-- Docker: http://localhost:5341 -->
  <!-- Azure/AWS: http://seq.yourdomain.com -->
  <add key="Seq:ServerUrl" value="http://localhost:5341" />

  <!-- API Key de Seq (opcional, només si Seq requereix autenticació) -->
  <!-- Deixar buit si no cal autenticació -->
  <add key="Seq:ApiKey" value="" />
</appSettings>
```

---

## ?? Activar Seq

### **Pas 1: Instal·lar Seq**

Escull una de les opcions d'instal·lació (local, Docker, etc.)

### **Pas 2: Activar a App.config**

```xml
<add key="Seq:Actiu" value="true" />
```

### **Pas 3: Executar l'Aplicació**

```sh
MultirIntegraModulab.exe
# o
MultirRevisioVigencia.exe
```

### **Pas 4: Obrir Seq**

Navegar a: http://localhost:5341

---

## ?? Utilitzar Seq

### **Visualització Bàsica**

Seq mostra tots els logs en temps real amb:
- ?? **Timestamp** amb mil·lisegons
- ?? **Missatge** del log
- ??? **Propietats** estructurades:
  - `Application`: "MultirIntegraModulab" o "MultirRevisioVigencia"
  - `Environment`: "Preproduccio" o "Produccio"
  - `Level`: Information, Warning, Error, etc.

### **Cerca Avançada**

#### Cercar errors:
```sql
@Level = 'Error'
```

#### Cercar per aplicació:
```sql
Application = 'MultirIntegraModulab'
```

#### Cercar per entorn:
```sql
Environment = 'Produccio' and @Level = 'Error'
```

#### Cercar per text:
```sql
@Message like '%diagnòstic%'
```

### **Crear Dashboards**

1. Anar a **"Dashboards"**
2. Crear nou dashboard
3. Afegir widgets:
   - **Gràfic de línies**: Logs per hora
   - **Comptador**: Total errors/warnings
   - **Taula**: Últims 10 errors

### **Configurar Alertes**

1. Anar a **"Alerts"**
2. Crear nova alerta
3. Configurar:
   - **Condició**: `@Level = 'Error'`
   - **Freqüència**: Immediata
   - **Notificació**: Email, Slack, Teams, etc.

---

## ?? Exemples de Logs a Seq

### **MultirIntegraModulab**

```json
{
  "@t": "2026-04-27T14:01:40.765Z",
  "@mt": "?? Començem a processar les mostres ...",
  "@l": "Information",
  "Application": "MultirIntegraModulab",
  "Environment": "Preproduccio"
}
```

### **MultirRevisioVigencia**

```json
{
  "@t": "2026-04-27T14:01:40.765Z",
  "@mt": "?? Iniciant revisió de vigència de diagnòstics MR ...",
  "@l": "Information",
  "Application": "MultirRevisioVigencia",
  "Environment": "Preproduccio"
}
```

---

## ?? Queries Útils

### **Errors de les Últimes 24 Hores**
```sql
@Level = 'Error' and @Timestamp >= Now() - 1d
```

### **Logs per Aplicació i Entorn**
```sql
Application = 'MultirIntegraModulab' and Environment = 'Produccio'
```

### **Diagnòstics Marcats com a No Vigents**
```sql
@Message like '%marcat com a no vigent%'
```

### **Temps d'Execució Superior a 5 Segons**
```sql
@Message like '%Durada%' and Durada > 5.0
```

---

## ?? Alertes Recomanades

### **1. Errors Crítics**
- **Condició**: `@Level = 'Error'`
- **Freqüència**: Immediata
- **Notificació**: Email + Slack

### **2. Warnings Repetitius**
- **Condició**: `@Level = 'Warning' count > 10 in 1h`
- **Freqüència**: Cada hora
- **Notificació**: Email

### **3. Aplicació No Respon**
- **Condició**: No hi ha logs en els últims 10 minuts
- **Freqüència**: Cada 10 minuts
- **Notificació**: SMS (urgent)

---

## ??? Troubleshooting

### ? Seq no mostra logs

**Causa**: Seq no està actiu o no és accessible

**Solució**:
1. Verificar que Seq està en funcionament: `http://localhost:5341`
2. Comprovar `App.config`: `<add key="Seq:Actiu" value="true" />`
3. Verificar firewall no bloqueja el port 5341

### ? Error "Connection refused"

**Causa**: URL de Seq incorrecta

**Solució**:
```xml
<!-- Verificar URL correcta -->
<add key="Seq:ServerUrl" value="http://localhost:5341" />
```

### ? MultirRevisioVigencia no envia logs a Seq

**Causa**: Paquet Serilog.Sinks.Seq no referenciat correctament

**Solució**: Descomentar el codi a `SerilogLoggerService.cs` després de corregir les referències del paquet

---

## ?? Recursos Addicionals

### **Documentació Oficial**
- https://docs.datalust.co/
- https://github.com/serilog/serilog-sinks-seq

### **Videos i Tutorials**
- [Introduction to Seq](https://www.youtube.com/watch?v=...)
- [Structured Logging with Serilog and Seq](https://www.youtube.com/watch?v=...)

### **Community**
- https://seq.community/
- https://stackoverflow.com/questions/tagged/seq

---

## ?? Propers Passos

### **Immediats**
1. ? Instal·lar Seq localment
2. ? Activar Seq a `App.config`
3. ? Executar aplicació i verificar logs a Seq

### **Curto termini**
4. ?? Corregir referències de paquet a MultirRevisioVigencia
5. ?? Crear dashboards personalitzats
6. ?? Configurar alertes bàsiques

### **Llarg termini**
7. ?? Desplegar Seq a Docker/Azure per producció
8. ?? Implementar mètriques i KPIs
9. ?? Configurar autenticació i seguretat

---

**Data**: 27 d'abril de 2026  
**Autor**: Carlos Castillo  
**Versió**: 1.0  
**Status**: ? Parcialment Implementat (MultirIntegraModulab complet, MultirRevisioVigencia pendent)
