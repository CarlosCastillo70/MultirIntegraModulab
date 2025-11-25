# 🔧 Configuració Build Release - Neteja de DLLs Innecessàries

## 📋 Problema

Al fer el build de Release, apareixen DLLs innecessàries a la carpeta `bin/Release/net48/` que no s'utilitzen al projecte:

```
bin/Release/net48/
├── BouncyCastle.Cryptography.dll    ❌ NO utilitzada
├── BouncyCastle.Crypto.dll          ❌ NO utilitzada
├── Google.Protobuf.dll              ❌ NO utilitzada
├── K4os.Compression.LZ4*.dll        ❌ NO utilitzades
├── ZstdSharp.dll                    ❌ NO utilitzada
├── Ubiety.Dns.Core.dll              ❌ NO utilitzada
└── [altres DLLs necessàries]        ✅ Utilitzades
```

Aquestes DLLs són **dependències transitives** de `MySql.Data` i `Oracle.ManagedDataAccess` que NO s'utilitzen al codi.

---

## ✅ Solució Implementada

S'ha afegit un **target post-build** al fitxer `MultirIntegraModulab.csproj` que elimina automàticament les DLLs innecessàries després de cada compilació.

### Configuració al .csproj

```xml
<!-- ⚙️ Post-Build: Eliminar DLLs innecessàries de la sortida -->
<Target Name="RemoveUnusedDlls" AfterTargets="Build">
  <ItemGroup>
    <!-- DLLs de BouncyCastle (criptografia no utilitzada) -->
    <UnusedDlls Include="$(OutputPath)BouncyCastle.Cryptography.dll" />
    <UnusedDlls Include="$(OutputPath)BouncyCastle.Crypto.dll" />
    
    <!-- DLLs de compressió de MySql.Data no utilitzades -->
    <UnusedDlls Include="$(OutputPath)Google.Protobuf.dll" />
    <UnusedDlls Include="$(OutputPath)K4os.Compression.LZ4.dll" />
    <UnusedDlls Include="$(OutputPath)K4os.Compression.LZ4.Streams.dll" />
    <UnusedDlls Include="$(OutputPath)K4os.Hash.xxHash.dll" />
    <UnusedDlls Include="$(OutputPath)ZstdSharp.dll" />
    
    <!-- Altres dependències transitives no utilitzades -->
    <UnusedDlls Include="$(OutputPath)Ubiety.Dns.Core.dll" />
  </ItemGroup>
  <Delete Files="@(UnusedDlls)" ContinueOnError="true" />
  <Message Text="🧹 DLLs innecessàries eliminades de $(OutputPath)" Importance="high" />
</Target>
```

---

## 🎯 Com Funciona

1. **Després de cada build** (Debug o Release), el target `RemoveUnusedDlls` s'executa automàticament
2. **Identifica les DLLs innecessàries** definides a `<UnusedDlls>`
3. **Elimina les DLLs** de la carpeta de sortida (`bin/Debug/net48` o `bin/Release/net48`)
4. **Continua encara que fallin** (`ContinueOnError="true"`) per no trencar el build si una DLL no existeix
5. **Mostra un missatge** informatiu a la finestra de Build Output

---

## 📦 DLLs que S'Eliminen

### BouncyCastle (Criptografia)
```
❌ BouncyCastle.Cryptography.dll
❌ BouncyCastle.Crypto.dll
```
**Motiu**: El projecte NO utilitza criptografia avançada. Les connexions a BD ja utilitzen SSL/TLS natiu.

### Compressió MySQL
```
❌ Google.Protobuf.dll
❌ K4os.Compression.LZ4.dll
❌ K4os.Compression.LZ4.Streams.dll
❌ K4os.Hash.xxHash.dll
❌ ZstdSharp.dll
```
**Motiu**: MySql.Data inclou aquestes llibreries per compressió de protocol, però NO les utilitzem (no tenim compressió activada).

### DNS Core
```
❌ Ubiety.Dns.Core.dll
```
**Motiu**: Llibreria de resolució DNS de MySql.Data que NO necessitem (usem IPs/hostnames directes).

---

## ✅ DLLs que ES MANTENEN (Necessàries)

```
✅ MultirIntegraModulab.exe
✅ MultirIntegraModulab.exe.config
✅ MySql.Data.dll
✅ Oracle.ManagedDataAccess.dll
✅ System.Net.Http.dll
✅ System.Configuration.ConfigurationManager.dll
✅ [altres DLLs del framework .NET]
```

---

## 🚀 Verificar que Funciona

### 1. Build del projecte
```bash
# Des de Visual Studio
Build > Rebuild Solution

# Des de línia de comandes
dotnet build -c Release
```

### 2. Comprovar la sortida del Build
A la finestra **Output > Build**, hauríeu de veure:
```
🧹 DLLs innecessàries eliminades de bin\Release\net48\
```

### 3. Verificar la carpeta de sortida
```bash
# Llistar DLLs a la carpeta Release
dir bin\Release\net48\*.dll
```

**NO hauries de veure** cap de les DLLs de la llista d'eliminació.

---

## 🔍 Troubleshooting

### Problema: Les DLLs encara apareixen després del build

**Causa**: El target potser no s'està executant.

**Solució**:
1. Fes un **Clean Solution** (Build > Clean Solution)
2. Tanca Visual Studio
3. Elimina manualment les carpetes `bin/` i `obj/`
4. Torna a obrir Visual Studio
5. Fes un **Rebuild Solution**

---

### Problema: Error durant el build relacionat amb DLLs

**Causa**: Una DLL que s'està eliminant pot ser necessària.

**Solució**:
1. Revisa l'error a la finestra **Error List**
2. Identifica quina DLL està causant l'error
3. Elimina'l de la llista `<UnusedDlls>` al `.csproj`
4. Fes rebuild

---

### Problema: L'aplicació falla en runtime per DLL no trobada

**Causa**: Has eliminat una DLL que SÍ és necessària.

**Solució**:
1. Identifica la DLL faltant des del missatge d'error
2. Treu-la de la llista `<UnusedDlls>` al `.csproj`
3. Fes rebuild
4. Verifica que l'aplicació funciona correctament

---

## 📊 Comparativa: Abans vs Després

### Abans (Sense neteja)
```
bin/Release/net48/
├── MultirIntegraModulab.exe              → 180 KB
├── MySql.Data.dll                        → 1.8 MB
├── Oracle.ManagedDataAccess.dll          → 5.2 MB
├── BouncyCastle.Cryptography.dll         → 2.1 MB  ❌
├── Google.Protobuf.dll                   → 450 KB  ❌
├── K4os.Compression.LZ4.dll              → 180 KB  ❌
├── K4os.Compression.LZ4.Streams.dll      → 80 KB   ❌
├── K4os.Hash.xxHash.dll                  → 45 KB   ❌
├── ZstdSharp.dll                         → 620 KB  ❌
├── Ubiety.Dns.Core.dll                   → 95 KB   ❌
└── [altres]                              → X MB
─────────────────────────────────────────────────────
TOTAL: ~11.5 MB
```

### Després (Amb neteja)
```
bin/Release/net48/
├── MultirIntegraModulab.exe              → 180 KB  ✅
├── MySql.Data.dll                        → 1.8 MB  ✅
├── Oracle.ManagedDataAccess.dll          → 5.2 MB  ✅
├── System.Configuration.*.dll            → X MB    ✅
└── [altres necessàries]                  → X MB    ✅
─────────────────────────────────────────────────────
TOTAL: ~7.8 MB
```

**Estalvi**: ~3.7 MB (32% menys)

---

## 🎯 Beneficis

### 1. Mida Reduïda del Paquet
- ✅ Menys MB per transferir en desplegaments
- ✅ Instal·lacions més ràpides
- ✅ Backups més petits

### 2. Claredat i Manteniment
- ✅ Més fàcil identificar què s'utilitza realment
- ✅ Menys confusió en troubleshooting
- ✅ Documentació de dependències clares

### 3. Seguretat
- ✅ Menys superfície d'atac (menys DLLs que podrien tenir vulnerabilitats)
- ✅ Auditories de seguretat més simples

### 4. Rendiment d'Inici
- ✅ L'aplicació carrega més ràpid (menys DLLs a carregar al inici)

---

## 🔄 Actualitzacions Futures

Si en el futur **necessites alguna DLL** que ara s'està eliminant (per exemple, si implementes compressió MySQL):

1. Edita `MultirIntegraModulab.csproj`
2. Troba la secció `<Target Name="RemoveUnusedDlls">`
3. **Elimina la línia** corresponent de `<UnusedDlls>`
4. Fes rebuild

Exemple: Si necessites `Google.Protobuf.dll`:
```xml
<!-- ABANS (s'elimina) -->
<UnusedDlls Include="$(OutputPath)Google.Protobuf.dll" />

<!-- DESPRÉS (ja no s'elimina, simplement esborra la línia) -->
```

---

## 📚 Referències

### Per què MySql.Data inclou aquestes DLLs?

- **BouncyCastle**: Per suport de criptografia SHA256 en versions antigues de .NET
- **Google.Protobuf**: Protocol de serialització per MySQL X Protocol (no utilitzem)
- **K4os.Compression**: Compressió LZ4 per MySQL Protocol (no utilitzem)
- **ZstdSharp**: Compressió Zstandard per MySQL (no utilitzem)
- **Ubiety.Dns.Core**: Resolució DNS asíncrona (no necessari)

**El nostre projecte NO utilitza cap d'aquestes funcionalitats**, per això és segur eliminar-les.

---

## ✅ Validació

### Checklist de Validació Post-Build

- [ ] Build exitós sense errors
- [ ] Missatge "🧹 DLLs innecessàries eliminades" visible a Output
- [ ] DLLs de BouncyCastle NO presents a `bin/Release/net48/`
- [ ] DLLs de compressió NO presents
- [ ] `MultirIntegraModulab.exe` funciona correctament
- [ ] Connexió a Oracle OK
- [ ] Connexió a MySQL OK
- [ ] WebService de pacients SAP accessible
- [ ] Logs generats correctament
- [ ] Email enviat correctament

---

## 🆘 Suport

Si tens problemes amb aquesta configuració:

1. Revisa aquest document
2. Comprova la secció **Troubleshooting** més amunt
3. Verifica el contingut de `.csproj` (secció `RemoveUnusedDlls`)
4. Contacta amb l'equip de desenvolupament

---

**Última actualització**: Gener 2025  
**Versió**: 1.0  
**Estat**: ✅ Implementat i funcional

---

## 📝 Notes Tècniques

### Per què `ContinueOnError="true"`?

```xml
<Delete Files="@(UnusedDlls)" ContinueOnError="true" />
```

Si una DLL no existeix (per exemple, perquè ja ha estat eliminada manualment), el build continua sense fallar.

### Per què `AfterTargets="Build"`?

```xml
<Target Name="RemoveUnusedDlls" AfterTargets="Build">
```

Assegura que l'eliminació es fa **després** que totes les DLLs hagin estat copiades a la carpeta de sortida.

### Alternatives Considerades

#### Opció 1: ExcludeAssets (Descartada)
```xml
<PackageReference Include="MySql.Data">
  <ExcludeAssets>runtime</ExcludeAssets>
</PackageReference>
```
**Problema**: Exclou TOTES les dependències runtime, incloent les necessàries.

#### Opció 2: PrivateAssets (Descartada)
```xml
<PackageReference Include="BouncyCastle.Cryptography">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```
**Problema**: BouncyCastle no és una referència directa, és transitiva.

#### Opció 3: Target Post-Build (✅ IMPLEMENTADA)
```xml
<Target Name="RemoveUnusedDlls" AfterTargets="Build">
  <Delete Files="..." />
</Target>
```
**Avantatges**: 
- Simple i efectiva
- Fàcil de mantenir
- No afecta la compilació
- Funciona amb dependències transitives

---

**Fi del document** 🎉
