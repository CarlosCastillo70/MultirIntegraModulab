# MultirRevisioVigencia

Aplicació per revisar automàticament la vigència dels diagnòstics de pacients del sistema MultiR.

## 📋 Descripció

Aquest aplicatiu s'executa diàriament per revisar els diagnòstics marcats com a vigents (`vigent = 'S'`) i comprovar si han superat el seu període de vigència segons els dies configurats per a cada tipus de mostra.

## 🎯 Funcionalitat

1. **Obté diagnòstics vigents**: Cerca tots els diagnòstics amb `vigent = 'S'` que tenen configuració de dies de vigència
2. **Comprova caducitat**: Per cada diagnòstic, calcula si han passat més dies dels configurats des de l'última mostra
3. **Marca no vigents**: Els diagnòstics caducats es marquen automàticament amb:
   - `vigent = 'N'`
   - `data_no_vigent = NOW()`
   - `responsable_no_vigent = 'SISTEMA_AUTO'`
   - `motiu_no_vigent = 'Caducat automàticament per superar X dies'`
4. **Envia email**: Si hi ha diagnòstics marcats o errors, s'envia un email de resum

## 🏗️ Arquitectura

```
MultirRevisioVigencia/
├── Application/
│   ├── UseCases/
│   │   └── RevisarVigenciaDiagnosticsUseCase.cs
│   └── DTOs/
│       ├── ResumRevisioVigenciaDto.cs
│       └── DiagnosticPerRevisar.cs
├── Domain/
│   └── Interfaces/
│       ├── IMultiRRepository.cs
│       └── ILoggerService.cs
├── Infrastructure/
│   ├── Configuration/
│   │   └── ConfiguracioManager.cs
│   ├── Persistence/
│   │   └── LegacyServices/
│   │       └── MultiRDbService.cs
│   ├── Logging/
│   │   └── FileLoggerService.cs
│   └── ExternalServices/
│       └── Email/
│           └── EmailService.cs
├── Program.cs
├── App.config
└── MultirRevisioVigencia.csproj
```

## ⚙️ Configuració

### App.config

```xml
<appSettings>
  <!-- IMPORTANT: Canviar a "Produccio" abans de desplegar -->
  <add key="Entorn" value="Preproduccio" />
  
  <!-- Logging -->
  <add key="RutaFitxerLog" value="Logs\RevisioVigencia_{0:yyyyMMdd}.log" />
  
  <!-- SMTP -->
  <add key="SmtpServer" value="smtp.trueta.intranet" />
  <add key="SmtpPort" value="25" />
  <add key="SmtpUsuari" value="" />
  <add key="SmtpPassword" value="" />
  <add key="UsarSSL" value="false" />
  <add key="EmailFrom" value="ccastillo.ics@gencat.cat" />
  <add key="EmailsDestinataris" value="destinatari1@gencat.cat;destinatari2@gencat.cat" />
</appSettings>

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

### Selecció d'Entorn

L'aplicació selecciona automàticament la connexió a la base de dades segons el valor de `Entorn`:

- **`Entorn = "Preproduccio"`** → Utilitza `MySqlMultiR_Preproduccio` (Database: **marsa_test**)
- **`Entorn = "Produccio"`** → Utilitza `MySqlMultiR_Produccio` (Database: **marsa**)

⚠️ **IMPORTANT**: Abans de desplegar a producció, canviar `<add key="Entorn" value="Produccio" />`
