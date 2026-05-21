# ?? Integració amb Seq - Monitorització de Logs en Temps Real

## ?? Resum

L'aplicació **MultirIntegraModulab** està integrada amb **Seq** per monitorització avançada de logs en temps real. La integració és **resilient** i permet que l'aplicació funcioni correctament encara que Seq no estigui disponible.

---

## ? Característiques de la Integració

### ??? **Gestió Resilient d'Errors**
- ? **Els logs sempre funcionen**: Encara que Seq no estigui disponible, els logs es guarden a fitxer i consola
- ?? **Avís automàtic**: Si Seq no està accessible, rebràs un avís al log
- ?? **Connexió automàtica**: Quan iniciïs Seq, l'aplicació el detectarà automàticament
- ?? **Inici ràpid**: Timeout de 2 segons per no bloquejar l'execució

### ?? **Destinacions de Logs (Sinks)**
1. **Console** - Sempre actiu
2. **File** - Sempre actiu (guardat a `Logs/`)
3. **Seq** - Actiu si està disponible i configurat

---

## ?? Iniciar Seq amb Docker

### **Opció 1: Docker amb Windows**
```powershell
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

### **Opció 2: Docker Compose (Recomanat per Producció)**
Crear un fitxer `docker-compose.yml`:

```yaml
version: '3.8'
services:
  seq:
    image: datalust/seq:latest
    container_name: seq
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
    restart: unless-stopped

volumes:
  seq-data:
```

Executar amb:
```powershell
docker-compose up -d
```

### **Accedir a Seq**
Un cop iniciat, accedeix a: **http://localhost:5341**

---

## ?? Configuració a `App.config`

```xml
<!-- Activar enviament de logs a Seq -->
<add key="Seq:Actiu" value="true" />

<!-- URL del servidor Seq -->
<add key="Seq:ServerUrl" value="http://localhost:5341" />

<!-- API Key (opcional, només si Seq requereix autenticació) -->
<add key="Seq:ApiKey" value="" />
```

### **Escenaris de Configuració**

| Escenari | `Seq:Actiu` | Comportament |
|----------|-------------|--------------|
| **Desenvolupament local amb Seq** | `true` | Logs a Console + File + Seq |
| **Desenvolupament local sense Seq** | `true` | Logs a Console + File (avís: Seq no disponible) |
| **Producció amb Seq** | `true` | Logs a Console + File + Seq |
| **Producció sense Seq** | `false` | Logs només a Console + File |

---

## ?? Funcionament Intern

### **Procés de Connexió**
1. ? L'aplicació llegeix la configuració de `App.config`
2. ?? Si `Seq:Actiu = true`, comprova si Seq està disponible (timeout: 2 segons)
3. ? Si Seq respon, configura el sink i mostra: `? Seq connectat correctament`
4. ?? Si Seq no respon, mostra avís: `?? Seq no està disponible. Els logs es guardaran només a fitxer i consola`
5. ?? L'aplicació continua l'execució normalment

### **Codi de Verificació**
```csharp
private bool ComprovarSeqDisponible(string seqServerUrl)
{
    try
    {
        var uri = new Uri(seqServerUrl);
        using (var client = new System.Net.WebClient())
        {
            client.Headers.Add("User-Agent", "MultirIntegraModulab-HealthCheck");
            var task = Task.Run(() => client.DownloadString(uri.ToString() + "/api"));
            if (Task.WhenAny(task, Task.Delay(2000)).Result == task)
            {
                return true;
            }
            return false;
        }
    }
    catch
    {
        return false;
    }
}
```

---

## ?? Avantatges de Seq

### **1. Cerca Avançada**
- Cerca per text complet
- Filtres per nivell de log (Information, Warning, Error)
- Filtres per propietats enriquides (`Application`, `Environment`, `ThreadId`)

### **2. Visualització en Temps Real**
- Dashboard amb gràfics
- Actualització automàtica
- Historial complet de logs

### **3. Alertes i Monitorització**
- Configurar alertes per errors crítics
- Integració amb Slack, email, etc.
- Anàlisi de tendències

### **4. Propietats Enriquides**
Cada log inclou automàticament:
- `Application`: "MultirIntegraModulab"
- `Environment`: "Produccio" o "Preproduccio"
- `ThreadId`: Identificador del fil d'execució
- `Timestamp`: Data i hora precisa (amb mil·lisegons)

---

## ?? Resolució de Problemes

### **Problema: Seq no està disponible**
**Simptoma:**
```
?? Seq no està disponible a http://localhost:5341. Els logs es guardaran només a fitxer i consola.
```

**Solució:**
```powershell
# Verificar si Docker està en execució
docker ps

# Iniciar Seq
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Verificar que Seq estigui en funcionament
docker logs seq
```

### **Problema: El port 5341 està ocupat**
**Solució:**
```powershell
# Opcions:
# 1. Canviar el port a App.config (ex: 5342)
<add key="Seq:ServerUrl" value="http://localhost:5342" />

# 2. Utilitzar un port diferent amb Docker
docker run -d --name seq -e ACCEPT_EULA=Y -p 5342:80 datalust/seq:latest
```

### **Problema: Seq requereix autenticació**
**Solució:**
```xml
<!-- Afegir API Key a App.config -->
<add key="Seq:ApiKey" value="LA_TEVA_API_KEY" />
```

---

## ?? Exemple de Log a Seq

```json
{
  "@t": "2024-01-26T10:30:45.123Z",
  "@l": "Information",
  "@m": "Processant mostra: 446442973323",
  "Application": "MultirIntegraModulab",
  "Environment": "Preproduccio",
  "ThreadId": 1,
  "EtiquetaMostra": "446442973323"
}
```

---

## ?? Millors Pràctiques

### **Desenvolupament**
- ? Deixar `Seq:Actiu = true` per debugging avançat
- ? Utilitzar Docker per Seq local
- ? Revisar Seq després de cada execució

### **Producció**
- ? Configurar Seq en un servidor dedicat o contenidor
- ? Mantenir `Seq:Actiu = true` per observabilitat
- ? Configurar alertes per errors crítics
- ? Configurar backup dels logs de Seq
- ? Revisar periòdicament els logs per detectar anomalies

---

## ?? Més Informació

- **Documentació oficial de Seq**: https://docs.datalust.co/docs
- **Serilog.Sinks.Seq**: https://github.com/serilog/serilog-sinks-seq
- **Docker Hub - Seq**: https://hub.docker.com/r/datalust/seq

---

## ?? Suport

Si tens problemes amb la integració de Seq:

### **?? Guia de Debugging Exhaustiva**
?? **[SEQ_DEBUGGING.md](SEQ_DEBUGGING.md)** - Guia completa per resoldre problemes de connexió

### **?? Verificacions Ràpides**
1. **Logs de l'aplicació** a `Logs/multir{data}_{entorn}.log`
2. **Estat del servei** amb `Get-Service seq`
3. **Logs de Seq** a `C:\ProgramData\Seq\Logs\`
4. **Configuració** a `App.config`
5. **Navegador** a http://localhost:5341

---

**Darrera actualització:** 26/01/2025
**Versió:** 1.0
**Autor:** MultirIntegraModulab Team
