# 📋 Instruccions per Canviar les Credencials de Connexió a Oracle Modulab

## 📍 Ubicació dels Fitxers de Configuració

Les dades de connexió a les bases de dades es troben als fitxers **`App.config`** dels projectes:

### 1. **Projecte Principal - MultirIntegraModulab**
📄 `MultirIntegraModulab\App.config` (línies 205-220)

### 2. **Servei de Planificació - MultirIntegraModulab.Service**
📄 `MultirIntegraModulab.Service\App.config`

---

## 🔐 Configuració Oracle Modulab

### Ubicació en el fitxer:
```xml
<connectionStrings>
  <!-- Oracle Modulab Connection - Producció -->
  <add name="OracleModulab_Produccio"
	   connectionString="Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = HOST_AQUI)(PORT = PORT_AQUI))) (CONNECT_DATA = (SERVICE_NAME = SERVICE_NAME_AQUI)));User Id=USUARI_AQUI;Password=PASSWORD_AQUI;"
	   providerName="Oracle.ManagedDataAccess.Client" />

  <!-- Oracle Modulab Connection - Preproducció -->
  <add name="OracleModulab_Preproduccio"
	   connectionString="Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = HOST_AQUI)(PORT = PORT_AQUI))) (CONNECT_DATA = (SERVICE_NAME = SERVICE_NAME_AQUI)));User Id=USUARI_AQUI;Password=PASSWORD_AQUI;"
	   providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

---

## 🔧 Components a Canviar

### **Dins de `Data source=...`:**

| Component | Descripció | Exemple Actual |
|-----------|-----------|---|
| **HOST** | Servidor Oracle | `excdox-scan.cpd4.intranet.gencat.cat` |
| **PORT** | Port Oracle | `1522` |
| **SERVICE_NAME** | Nom del servei Oracle | `excdox01srv` |
| **User Id** | Usuari Oracle | `DWGI_MDP` |
| **Password** | Contrasenya Oracle | `gLesb01an` |

---

## 📝 Pasos per Actualitzar

### **Opció 1: Editar manualmente el fitxer**

1. Obrir `MultirIntegraModulab\App.config` amb un editor de text
2. Localitzar la secció `<connectionStrings>` (aproximadament línea 205)
3. Canviar els valors:
   - **HOST**: Reemplaçar `excdox-scan.cpd4.intranet.gencat.cat` → _teu_server_oracle_
   - **PORT**: Reemplaçar `1522` → _teu_port_
   - **SERVICE_NAME**: Reemplaçar `excdox01srv` → _teu_service_name_
   - **User Id**: Reemplaçar `DWGI_MDP` → _teu_usuari_
   - **Password**: Reemplaçar `gLesb01an` → _teva_password_

4. Guardar el fitxer
5. **Repetir per a Preproducció** si es necessari

### **Opció 2: Usar Visual Studio**

1. Obrir el projecte `MultirIntegraModulab` en Visual Studio
2. A l'Explorador de Solucions, fer doble click en `App.config`
3. Localitzar la secció `<connectionStrings>`
4. Editar els valors
5. Guardar (`Ctrl+S`)

---

## ⚠️ Consideracions Importants

### **Seguretat**
- ❌ **NO committed nunca les credencials reals al repositori Git**
- ✅ Usa variables d'entorn o fitxers locals no versionats per producció
- ✅ Considera usar **Azure Key Vault** o secrets management per producció

### **Doble Connexió**
La configuració actual contempla **dos entorns**:
- **Producció**: `OracleModulab_Produccio` (connexió real a Modulab)
- **Preproducció**: `OracleModulab_Preproduccio` (per testing)

**L'entorn actiu es controla per aquesta configuració:**
```xml
<add key="Entorn" value="Preproduccio" />  <!-- Canviar a "Produccio" si cal -->
```

### **Format de Connexió Oracle**
El format utilitzat és el "Net Service Name" d'Oracle:
```
Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = servidor)(PORT = port))) (CONNECT_DATA = (SERVICE_NAME = servei)));User Id=usuari;Password=contrasenya;
```

---

## ✅ Com Verificar que Funciona

1. **Build del projecte**:
   ```
   Ctrl+Shift+B
   ```

2. **Executar l'aplicació** i veure si es connecta correctament

3. **Revisar els logs** (ubicat en `Logs\` folder) per confirmar la connexió

---

## 🆘 Si Tenim Problemes de Connexió

### **Verificacions bàsiques**:
1. ✅ Xecar que Oracle està en execució
2. ✅ Verificar HOST i PORT amb `tnsping`
3. ✅ Confirmar que l'usuari existeix i té permisos
4. ✅ Provar la connexió des de SQL Developer

### **Comandes útils**:
```bash
# Testejar connexió desde SQL*Plus
sqlplus usuari/contrasenya@"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=servidor)(PORT=port))(CONNECT_DATA=(SERVICE_NAME=servei)))"
```

---

## 📚 Fitxers Relacionats

- `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\ModulabDbService.cs` - Codi que usa aquesta connexió
- `MultirIntegraModulab\Infrastructure\Configuration\ConfigurationService.cs` - Carrega les configuracions

---

## 💡 Recomanacions

| Situació | Recomanació |
|----------|------------|
| **Desenvolupament local** | Usar una BD de test en local o VM |
| **Integració contínua** | Usar variables d'entorn en el CI/CD pipeline |
| **Producció** | Usar secrets management (Azure Key Vault, etc.) |
| **Múltiples equips** | Usar `App.config.example` com a plantilla i fitxers locals `.local` no versionats |

---

**Data d'actualització**: Gener 2025
**Responsable**: Equip de Desenvolupament
