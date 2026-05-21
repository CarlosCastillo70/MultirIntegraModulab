# ?? Debugging de Connexió amb Seq

## ?? Problema Comú

Si veus aquest missatge:
```
?? Seq no està disponible a http://localhost:5341. Els logs es guardaran només a fitxer i consola.
```

Però el servei de Seq està en funcionament (`Get-Service seq` mostra **Running**), segueix aquesta guia de debugging.

---

## ? **Verificacions Ràpides**

### **1. Comprovar que Seq està realment en funcionament**

```powershell
# Comprovar estat del servei
Get-Service seq

# Hauria de mostrar:
# Status   Name               DisplayName
# ------   ----               -----------
# Running  seq                Seq
```

### **2. Comprovar que Seq respon al navegador**

Obre el navegador i ves a: **http://localhost:5341**

Si no càrrega, Seq pot estar actiu però no estar escoltant al port correcte.

### **3. Comprovar el port on escolta Seq**

```powershell
# Comprovar què està escoltant al port 5341
netstat -ano | findstr :5341

# Hauries de veure alguna cosa com:
# TCP    0.0.0.0:5341           0.0.0.0:0              LISTENING       1234
# TCP    [::]:5341              [::]:0                 LISTENING       1234
```

Si no veus res, Seq està configurat per escoltar a un altre port.

---

## ?? **Solucions Detallades**

### **Solució 1: Verificar Configuració del Port de Seq**

1. **Localitza el fitxer de configuració de Seq:**
   ```
   C:\ProgramData\Seq\Seq.json
   ```

2. **Obre'l amb un editor de text** (com a administrador)

3. **Comprova la configuració del port:**
   ```json
   {
     "storage": {
       "path": "C:\\ProgramData\\Seq\\Data"
     },
     "api": {
       "listenUris": ["http://localhost:5341"]
     }
   }
   ```

4. **Si el port és diferent**, actualitza `App.config`:
   ```xml
   <add key="Seq:ServerUrl" value="http://localhost:XXXX" />
   ```

5. **Reinicia el servei de Seq:**
   ```powershell
   Restart-Service seq
   ```

---

### **Solució 2: Reiniciar el Servei de Seq**

A vegades Seq pot quedar-se en un estat inconsistent:

```powershell
# Aturar el servei
Stop-Service seq

# Esperar 5 segons
Start-Sleep -Seconds 5

# Iniciar el servei
Start-Service seq

# Comprovar estat
Get-Service seq

# Esperar que Seq s'inicialitzi completament (15 segons)
Start-Sleep -Seconds 15
```

---

### **Solució 3: Comprovar Logs de Seq**

Els logs de Seq poden revelar problemes d'inici:

```powershell
# Obre el directori de logs
explorer C:\ProgramData\Seq\Logs

# Busca el fitxer més recent (ex: seq-YYYYMMDD.log)
# Obre'l amb Notepad o Visual Studio Code
```

**Busca errors com:**
- `Failed to bind to address` - Port ocupat per una altra aplicació
- `Access denied` - Problemes de permisos
- `Configuration error` - Error al fitxer Seq.json

---

### **Solució 4: Comprovar Firewall de Windows**

El firewall pot estar bloquejant la connexió:

```powershell
# Crear regla de firewall per Seq (executar com a administrador)
New-NetFirewallRule -DisplayName "Seq Server" -Direction Inbound -Protocol TCP -LocalPort 5341 -Action Allow
```

---

### **Solució 5: Provar amb cURL o PowerShell**

Comprova manualment la connexió HTTP:

```powershell
# Opció 1: Amb Invoke-WebRequest (PowerShell)
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5341" -TimeoutSec 5
    Write-Host "? Seq respon correctament: $($response.StatusCode)"
} catch {
    Write-Host "? Error connectant amb Seq: $($_.Exception.Message)"
}

# Opció 2: Amb Test-NetConnection
Test-NetConnection -ComputerName localhost -Port 5341
```

**Resultats esperats:**
- ? **TcpTestSucceeded: True** - Seq està accessible
- ? **TcpTestSucceeded: False** - Seq no està accessible o port incorrecte

---

### **Solució 6: Augmentar el Temps d'Espera**

Si Seq triga molt a inicialitzar-se, pots augmentar el timeout a `LoggerService.cs`:

```csharp
request.Timeout = 10000; // 10 segons en lloc de 5
request.ReadWriteTimeout = 10000;
```

---

### **Solució 7: Desactivar Temporalment la Comprovació**

Si vols **forçar la connexió** sense comprovació prèvia (confia que Seq està disponible):

**Edita `App.config`:**
```xml
<!-- Afegir aquesta línia per saltar la comprovació (TEMPORAL) -->
<add key="Seq:SkipHealthCheck" value="true" />
```

**Modifica `LoggerService.cs` (línia ~60):**
```csharp
// Llegir configuració per saltar healthcheck
var skipHealthCheck = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["Seq:SkipHealthCheck"] ?? "false");

if (seqActiu)
{
    try
    {
        // Saltar comprovació si està configurat
        if (skipHealthCheck || ComprovarSeqDisponible(seqServerUrl))
        {
            // ... configurar Seq
        }
    }
}
```

---

## ?? **Debugging Avançat**

### **Afegir Logs de Debug al Mètode de Comprovació**

Si vols veure exactament què passa durant la comprovació:

```csharp
private bool ComprovarSeqDisponible(string seqServerUrl)
{
    try
    {
        Console.WriteLine($"[DEBUG] Comprovant Seq a: {seqServerUrl}");
        var uri = new Uri(seqServerUrl);
        var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
        request.Timeout = 5000;
        
        try
        {
            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"[DEBUG] Seq respon amb codi: {response.StatusCode}");
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
        }
        catch (System.Net.WebException webEx)
        {
            Console.WriteLine($"[DEBUG] WebException: {webEx.Status}");
            if (webEx.Response != null)
            {
                var httpResponse = (System.Net.HttpWebResponse)webEx.Response;
                Console.WriteLine($"[DEBUG] Codi HTTP: {httpResponse.StatusCode}");
            }
            // ... resta del codi
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DEBUG] Error inesperat: {ex.Message}");
        return false;
    }
}
```

---

## ?? **Taula de Codis HTTP i Significat**

| Codi HTTP | Significat | Seq Disponible? | Acció |
|-----------|-----------|----------------|-------|
| **200 OK** | Seq funciona perfectament | ? Sí | Continuar normalment |
| **301/302 Redirect** | Seq redirigeix (ex: /login) | ? Sí | Acceptable, continuar |
| **401 Unauthorized** | Seq requereix autenticació | ? Sí | Afegir API Key |
| **403 Forbidden** | Seq actiu però acces denegat | ? Sí | Revisar permisos |
| **404 Not Found** | Ruta no trobada | ?? Potser | Seq està actiu però endpoint incorrecte |
| **500 Server Error** | Error intern de Seq | ?? Potser | Revisar logs de Seq |
| **Connection Refused** | Port tancat | ? No | Seq no està en funcionament |
| **Timeout** | No respon a temps | ? No | Seq lent o penjat |

---

## ?? **Últim Recurs: Reinstal·lar Seq**

Si res funciona:

```powershell
# 1. Desinstal·lar Seq
Stop-Service seq
sc.exe delete seq

# 2. Eliminar dades antigues (OPCIONAL - perdràs logs anteriors)
Remove-Item -Path "C:\ProgramData\Seq" -Recurse -Force

# 3. Descarregar i instal·lar Seq de nou
# https://datalust.co/download
```

---

## ? **Verificació Final**

Després d'aplicar qualsevol solució:

1. **Reinicia el servei de Seq:**
   ```powershell
   Restart-Service seq
   Start-Sleep -Seconds 15
   ```

2. **Comprova amb el navegador:**
   ```
   http://localhost:5341
   ```

3. **Executa l'aplicació MultirIntegraModulab:**
   ```
   Hauries de veure: ? Seq connectat correctament a http://localhost:5341
   ```

4. **Verifica els logs a Seq:**
   - Obre http://localhost:5341
   - Hauries de veure els logs de l'aplicació aparèixer en temps real

---

## ?? **Contacte i Suport**

Si després de seguir aquesta guia encara tens problemes:

1. ?? Recull informació:
   ```powershell
   Get-Service seq
   Get-Content "C:\ProgramData\Seq\Logs\seq-*.log" -Tail 50
   netstat -ano | findstr :5341
   ```

2. ?? Contacta amb l'equip de desenvolupament amb aquesta informació

---

**Darrera actualització:** 26/01/2025  
**Versió:** 1.0  
**Autor:** MultirIntegraModulab Team
