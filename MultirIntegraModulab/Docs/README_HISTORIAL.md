# Funcionalitat d'Historial de Mostres

## Descripció General

La funcionalitat d'historial de mostres permet guardar un registre dels canvis detectats en les mostres quan es processen. Quan una mostra té diferències en les combinacions microorganisme-mecanisme de resistència entre Oracle (Modulab) i MySQL (MultiR), s'emmagatzema l'estat anterior a la taula `pacients_diagnostics_mostra_historial` abans d'aplicar els canvis.

## Quan es Crea Historial

L'historial es genera automàticament en els següents casos:

1. **Mostra Desvalidada amb Canvis**: Quan MySQL té data de validació però Oracle no, i s'han detectat canvis en les combinacions.
2. **Mostra Validada amb Canvis**: Quan Oracle té nova data de validació i s'han detectat canvis en les combinacions.
3. **Mostra Revalidada amb Canvis**: Quan les dates de validació són diferents i s'han detectat canvis en les combinacions.

## Estructura de la Taula d'Historial

```sql
CREATE TABLE pacients_diagnostics_mostra_historial (
    id INT AUTO_INCREMENT PRIMARY KEY,
    etiqueta_original VARCHAR(50) NOT NULL,
    tipus_canvi VARCHAR(50) NOT NULL,
    data_canvi DATETIME NOT NULL,
    estat_abans_canvi VARCHAR(20),
    pacient_sap VARCHAR(50),
    microorganisme VARCHAR(255),
    mecanisme_resistencia VARCHAR(255),
    data_resultat_original DATETIME,
    data_validacio_original DATETIME,
    estat_integracio_m_original VARCHAR(10),
    observacions TEXT,
    dt_create DATETIME NOT NULL,
    INDEX idx_etiqueta_original (etiqueta_original),
    INDEX idx_data_canvi (data_canvi),
    INDEX idx_tipus_canvi (tipus_canvi)
);
```

## Nous Mètodes Disponibles

### MultiRDbService Extensions

#### `GuardarHistorialMostra(string etiquetaId, string tipusCanvi, string observacions = null)`
Guarda l'estat actual d'una mostra a l'historial abans de fer canvis.

```csharp
bool success = mysqlService.GuardarHistorialMostra(
    "400816071", 
    "DESVALIDADA_CANVI", 
    "Mostra desvalidada amb canvis detectats en les combinacions microorganisme-mecanisme"
);
```

#### `EsborrarHistorialAnterior(string etiquetaId)`
Esborra registres d'historial anteriors per una etiqueta específica.

```csharp
bool success = mysqlService.EsborrarHistorialAnterior("400816071");
```

#### `ComprovarHistorialExisteix(string etiquetaId)`
Comprova si existeix historial per una mostra.

```csharp
int countHistorial = mysqlService.ComprovarHistorialExisteix("400816071");
```

#### `ObtenirHistorialMostra(string etiquetaId)`
Obté l'historial complet d'una mostra ordenat per data.

```csharp
var historial = mysqlService.ObtenirHistorialMostra("400816071");
foreach (var registre in historial)
{
    Console.WriteLine($"{registre.DataCanvi}: {registre.TipusCanvi} - {registre.EstatAbansCanvi}");
}
```

#### `ObtenirEstadistiquesHistorial()`
Obté estadístiques generals de l'historial.

```csharp
var stats = mysqlService.ObtenirEstadistiquesHistorial();
Console.WriteLine($"Total registres: {stats.TotalRegistresHistorial}");
foreach (var tipus in stats.RegistresPerTipus)
{
    Console.WriteLine($"{tipus.Key}: {tipus.Value} registres");
}
```

## Tipus de Canvis

- **DESVALIDADA_CANVI**: Mostra que estava validada a MySQL però Oracle no té data de validació i hi ha canvis
- **VALIDADA_CANVI**: Mostra que ara té validació a Oracle i hi ha canvis
- **REVALIDADA_CANVI**: Mostra amb dates de validació diferents i hi ha canvis

## Classes de Suport

### `RegistreHistorialMostra`
Representa un registre individual de l'historial amb tota la informació sobre l'estat anterior de la mostra.

### `EstadistiquesHistorial`
Conté estadístiques agregades sobre l'ús de l'historial:
- Total de registres
- Distribució per tipus de canvi
- Dates del primer i últim registre

## Integració amb TractamentResultats

La funcionalitat s'integra automàticament en els mètodes de processament:

```csharp
var tractament = new TractamentResultats(mysqlService);
var resum = tractament.ProcessarMostres(coleccioMostres);

// El resum inclou el nombre de mostres amb canvis
Console.WriteLine($"Mostres amb canvis historiats: {resum.MostresAmbCanvis}");
```

## Exemple d'Ús

```csharp
// Configurar servei
var mysqlService = new MultiRDbService(connectionString);

// Processar mostres (genera historial automàticament)
var tractament = new TractamentResultats(mysqlService);
var resum = tractament.ProcessarMostres(coleccioMostres);

// Consultar historial d'una mostra específica
var historial = mysqlService.ObtenirHistorialMostra("400816071");
foreach (var canvi in historial)
{
    Console.WriteLine($"Canvi: {canvi.TipusCanvi} el {canvi.DataCanvi}");
    Console.WriteLine($"Estat anterior: {canvi.EstatAbansCanvi}");
    Console.WriteLine($"Observacions: {canvi.Observacions}");
}

// Obtenir estadístiques generals
var stats = mysqlService.ObtenirEstadistiquesHistorial();
Console.WriteLine($"Total registres d'historial: {stats.TotalRegistresHistorial}");
```

## Consideracions de Rendiment

- L'historial només es crea quan hi ha canvis reals detectats
- Es recomana fer manteniment periòdic per esborrar registres antics
- Les consultes d'historial estan indexades per millor rendiment

## Manteniment

Es recomana implementar una tasca de manteniment per esborrar registres d'historial més antics de X dies:

```sql
-- Exemple: esborrar registres de més de 90 dies
DELETE FROM pacients_diagnostics_mostra_historial 
WHERE data_canvi < DATE_SUB(NOW(), INTERVAL 90 DAY);
```

## Beneficis

1. **Traçabilitat**: Permet veure l'evolució de les mostres en el temps
2. **Auditoria**: Registre dels canvis per compliment normatiu
3. **Debugger**: Facilita la investigació de problemes
4. **Anàlisi**: Permet identificar patrons de canvis en les mostres

## Logs i Missatges

Durant el processament, es mostren missatges informatius:

```
   ?? Historial guardat per mostra desvalidada amb canvis
   ?? Mostres amb canvis (guardades a historial): 5
   ?? Estadístiques de l'historial general:
      • Total registres d'historial: 156
      • DESVALIDADA_CANVI: 23 registres
      • VALIDADA_CANVI: 78 registres
      • REVALIDADA_CANVI: 55 registres
```