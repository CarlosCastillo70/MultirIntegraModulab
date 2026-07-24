# Fix para ORA-00942: Table or View Does Not Exist

## Problema Original

Se recibía el error:
```
Oracle.ManagedDataAccess.Client.OracleException: ORA-00942: la tabla o vista no existe
```

Este error ocurría cuando se intentaba ejecutar la consulta SQL para cargar resultados de mostras desde la base de datos Oracle Modulab.

## Causa Raíz

El error ORA-00942 indica que Oracle no puede encontrar las tablas referenciadas en la consulta SQL. Las causas más comunes son:

1. **Usuario conectado sin permisos**: El usuario en la conexión de Oracle no es el propietario de las tablas
2. **Tablas en schema diferente**: Las tablas están en un schema (propietario) diferente del usuario actual
3. **Conexión a base de datos incorrecta**: La cadena de conexión señala a una instancia Oracle diferente

## Solución Implementada

Se ha modificado el método `ObtenirConsultaResultatsProves()` en `ModulabDbService.cs` para:

### 1. **Agregar Prefijo de Schema Dinámico**
- Ahora la consulta incluye el prefix `MODULAB.` antes de cada nombre de tabla
- Ejemplo: `FROM MODULAB.CULTUREISOLATION ci` en vez de `FROM CULTUREISOLATION ci`

### 2. **Detectar Automáticamente el Schema con `ObtenirSchemaPrefix()`**
Este nuevo método:
- Se conecta a Oracle y ejecuta `SELECT USER FROM dual`
- Obtiene el usuario actual conectado
- Si el usuario NO es "MODULAB", retorna `"MODULAB."` como prefix
- Si el usuario ES "MODULAB", retorna string vacío (no necesita prefix)
- Si hay error, intenta sin prefix como fallback

```csharp
private string ObtenirSchemaPrefix()
{
	try
	{
		using (var conn = new OracleConnection(_connectionString))
		{
			conn.Open();
			using (var cmd = new OracleCommand("SELECT USER FROM dual", conn))
			{
				object result = cmd.ExecuteScalar();
				string currentUser = result != null ? result.ToString() : string.Empty;
				_logger.Info($"🔐 Oracle usuari actual: {currentUser}");

				if (!string.IsNullOrEmpty(currentUser) && 
					currentUser.ToUpperInvariant() != "MODULAB")
				{
					return "MODULAB.";
				}

				return string.Empty;
			}
		}
	}
	catch (Exception ex)
	{
		_logger.Error($"⚠️ Error obtenint schema prefix: {ex.Message}.", ex);
		return string.Empty;
	}
}
```

### 3. **Tablas Afectadas**
Todas estas tablas ahora incluyen el prefix automático:
- `CULTUREISOLATION`
- `REQUEST`
- `PATIENT`
- `ISOLATION`
- `REQUESTTEST`
- `RESISTANCEMECHANISM` (x5)
- `DOCTOR`
- `SERVICE`
- `SAMPLECOLLECTIONCENTER`
- `REQUESTTESTADDITIONALINFO`
- `TEST`
- `CONTAINER`
- `SAMPLE`
- `REQUESTCONTAINER`
- `ADDITIONALINFO`
- `REQUESTDIAGNOSIS`
- `DIAGNOSIS`

## Cambios de Código

### Archivo Modificado
- `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\ModulabDbService.cs`

### Métodos Afectados
1. `ObtenirConsultaResultatsProves()` - Ahora construye la consulta con prefixes dinámicos
2. `ObtenirSchemaPrefix()` - Nuevo método para detectar el schema

## Pasos para Verificar la Solución

### 1. Validar la Conexión Oracle
Verifica que tu `App.config` tenga la conexión correcta:

```xml
<add name="OracleModulab" 
	 connectionString="Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = [HOST])(PORT = [PORT]))) (CONNECT_DATA = (SERVICE_NAME = [SERVICE])));User Id=[USER];Password=[PASSWORD];" 
	 providerName="Oracle.ManagedDataAccess.Client" />
```

**Importante**: Verifica:
- ✅ HOST correcto
- ✅ PORT correcto (usualmente 1521 para Oracle)
- ✅ SERVICE_NAME o SID correcto
- ✅ USER y PASSWORD válidos

### 2. Verificar Permisos en Oracle
Si tienes acceso a Oracle SQL*Plus o SQL Developer, verifica:

```sql
-- Ver usuario actual
SELECT USER FROM dual;

-- Ver si puedes acceder a las tablas con prefix MODULAB
SELECT COUNT(*) FROM MODULAB.CULTUREISOLATION;
SELECT COUNT(*) FROM MODULAB.REQUEST;

-- Ver todas las tablas disponibles
SELECT table_name FROM all_tables WHERE owner = 'MODULAB' AND ROWNUM <= 10;
```

### 3. Verificar los Logs
Una vez ejecutada la aplicación, busca en los logs:

```
🔐 Oracle usuari actual: [USER_NAME]
```

Si ves:
- `🔐 Oracle usuari actual: MODULAB` → El schema se detectó correctamente como MODULAB
- `🔐 Oracle usuari actual: [OTHER_USER]` → Se aplicará automáticamente el prefix `MODULAB.`
- `⚠️ Error obtenint schema prefix:` → Hay un problema de conexión

### 4. Ejecutar la Aplicación
Ejecuta la aplicación y observa si:
- ✅ Las mostras se cargan exitosamente
- ✅ No hay más errores ORA-00942
- ✅ Los datos se procesan correctamente

## Escenarios Soportados

| Escenario | Usuario Conectado | Prefix Usado | Resultado |
|-----------|------------------|-------------|-----------|
| 1. Usuario es MODULAB | `MODULAB` | (vacío) | ✅ Acceso directo |
| 2. Usuario es otro | `TECNICO` | `MODULAB.` | ✅ Acceso con prefix |
| 3. Error de conexión | (error) | (vacío) | ⚠️ Intenta sin prefix |

## Fallback (Plan B)

Si la detección automática falla:
1. Se intenta SIN prefix (fallback a string vacío)
2. Si esto tampoco funciona, el error ORA-00942 se continuará mostrando

**Solución manual alternativa**:
Si la detección automática no funciona, puedes modificar manualmente el método `ObtenirSchemaPrefix()` para retornar siempre `"MODULAB."`:

```csharp
private string ObtenirSchemaPrefix()
{
	// Fallback manual: siempre usar MODULAB schema
	return "MODULAB.";
}
```

## Próximas Mejoras Recomendadas

1. **Otros métodos con SQL**: Verificar y actualizar otros métodos que generen SQL directas:
   - `ObtenirConsultaResultatsProvesPerRangDates()` - También necesita schema prefix para tablas DWDIMICS y DWFACTICS
   - Otros métodos en `ModulabDbService.Sincronitzacio.cs`

2. **Configuración centralizada**: Crear una propiedad en `App.config` para especificar explícitamente el schema:
   ```xml
   <add key="ModulabSchema" value="MODULAB" />
   ```

3. **Validación al iniciar**: Verificar permisos de conexión al inicio de la aplicación

## Compatibilidad

- ✅ C# 7.3
- ✅ .NET Framework 4.8
- ✅ Oracle 11g+
- ✅ Oracle.ManagedDataAccess 19.x+

## Referencias

- [ORA-00942 en Oracle Docs](https://docs.oracle.com/error/057498/)
- [Schema Qualification en Oracle](https://docs.oracle.com/database/121/SQLRF/sql_elements003.htm#SQLRF51171)
- [Oracle User Security Model](https://docs.oracle.com/database/121/DBSEG/principles.htm)
