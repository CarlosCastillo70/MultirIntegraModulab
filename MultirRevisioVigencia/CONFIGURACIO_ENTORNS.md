# Configuració dels Entorns - MultirRevisioVigencia

## 📋 Resum

L'aplicació **MultirRevisioVigencia** suporta dos entorns:
- **Preproducció** (per defecte) → Base de dades: `marsa_test`
- **Producció** → Base de dades: `marsa`

## ⚙️ Configuració a App.config

### 1. Selecció d'Entorn

A l'**App.config**, canviar el valor de `Entorn`:

```xml
<appSettings>
  <!-- Per PREPRODUCCIÓ -->
  <add key="Entorn" value="Preproduccio" />
  
  <!-- Per PRODUCCIÓ -->
  <!-- <add key="Entorn" value="Produccio" /> -->
</appSettings>
```

### 2. Cadenes de Connexió

Les cadenes de connexió són idèntiques a **MultirIntegraModulab**:

```xml
<connectionStrings>
  <!-- PRODUCCIÓ -->
  <add name="MySqlMultiR_Produccio"
       connectionString="Server=zeus;Database=marsa;Uid=marsa;Pwd=2a0d9a8d22;"
       providerName="MySql.Data.MySqlClient" />

  <!-- PREPRODUCCIÓ -->
  <add name="MySqlMultiR_Preproduccio"
       connectionString="Server=zeus;Database=marsa_test;Uid=marsa;Pwd=2a0d9a8d22;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

## 🔄 Canvi d'Entorn

### De Preproducció a Producció

1. Obrir `App.config`
2. Canviar:
   ```xml
   <add key="Entorn" value="Produccio" />
   ```
3. Compilar en mode Release:
   ```bash
   msbuild MultirRevisioVigencia.csproj /p:Configuration=Release
   ```
4. Desplegar `bin\Release\`

### De Producció a Preproducció

1. Obrir `App.config`
2. Canviar:
   ```xml
   <add key="Entorn" value="Preproduccio" />
   ```
3. Compilar i executar

## 🚀 Output a l'Inici

L'aplicació mostra clarament l'entorn actiu:

```
=======================================================
  ENTORN: PREPRODUCCIÓ
=======================================================

✅ Configuració carregada correctament
   - Base de dades: marsa_test
   - Servidor SMTP: smtp.trueta.intranet:25
   - Destinataris: 1
```

o

```
=======================================================
  ENTORN: PRODUCCIÓ
=======================================================

✅ Configuració carregada correctament
   - Base de dades: marsa
   - Servidor SMTP: smtp.trueta.intranet:25
   - Destinataris: 1
```

## ⚠️ Advertències Importants

1. ✅ **Sempre verificar l'entorn abans d'executar**
2. ✅ **Producció utilitza la base de dades real (`marsa`)**
3. ✅ **Preproducció utilitza la base de dades de test (`marsa_test`)**
4. ✅ **Els logs es guarden sempre a `Logs\RevisioVigencia_YYYYMMDD.log`**
5. ✅ **Els emails de resum indiquen l'entorn utilitzat**

## 📊 Verificació

Per verificar que s'està utilitzant l'entorn correcte:

1. Executar l'aplicació
2. Comprovar la primera línia de log
3. Verificar que la base de dades sigui la correcta:
   - Preproducció → `marsa_test`
   - Producció → `marsa`

## 🔐 Seguretat

- Les credencials de la base de dades són les mateixes per tots dos entorns
- Només canvia el nom de la base de dades
- No cal modificar les credencials en canviar d'entorn

## 📝 Checklist de Desplegament a Producció

- [ ] Canviar `Entorn` a `"Produccio"` a `App.config`
- [ ] Compilar en mode Release
- [ ] Verificar que es connecta a `marsa` (NO `marsa_test`)
- [ ] Configurar Task Scheduler per executar diàriament
- [ ] Provar l'enviament d'emails
- [ ] Verificar que els logs es guarden correctament
- [ ] Documentar la data de desplegament

## 🔗 Relació amb MultirIntegraModulab

| Característica | MultirIntegraModulab | MultirRevisioVigencia |
|---------------|---------------------|----------------------|
| **Freqüència** | Cada hora | 1 cop al dia |
| **Base de dades** | marsa / marsa_test | marsa / marsa_test |
| **Entorns** | Producció / Preproducció | Producció / Preproducció |
| **Selecció entorn** | AppSettings `Entorn` | AppSettings `Entorn` |
| **Logs** | `Logs\MultiR_YYYYMMDD.log` | `Logs\RevisioVigencia_YYYYMMDD.log` |
