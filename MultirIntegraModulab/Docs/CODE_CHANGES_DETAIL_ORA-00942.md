# Code Changes Detail - ORA-00942 Fix

## File: `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\ModulabDbService.cs`

### Change 1: Updated Method `ObtenirConsultaResultatsProves()`

#### Location: Lines 218-288

#### What Changed
- Now calls `ObtenirSchemaPrefix()` to detect schema prefix
- All 16 table references now use `{schemaPrefix}TABLE_NAME` pattern
- String interpolation ($@"...") instead of raw string (@"...")

#### Before Code
```csharp
private string ObtenirConsultaResultatsProves(int limitRegistres = 0)
{
	string consultaBase = @"
		SELECT DISTINCT
			r.REQUESTLABEL || SUBSTR(rc.requestcontainerlabel, 1, 3) AS ETIQUETA_ID,
			...
		FROM
			CULTUREISOLATION ci
			JOIN REQUEST r ON r.REQUESTID = ci.REQUESTID
			JOIN PATIENT p ON p.PATIENTID = r.PATIENTID
			...";

	if (limitRegistres > 0)
	{
		return $@"
			SELECT * FROM (
				{consultaBase}
			) WHERE ROWNUM <= {limitRegistres}";
	}

	return consultaBase;
}
```

#### After Code
```csharp
private string ObtenirConsultaResultatsProves(int limitRegistres = 0)
{
	// Obtenir el schema prefix de la connexió (default a buit si no es pot determinar)
	string schemaPrefix = ObtenirSchemaPrefix();

	string consultaBase = $@"
		SELECT DISTINCT
			r.REQUESTLABEL || SUBSTR(rc.requestcontainerlabel, 1, 3) AS ETIQUETA_ID,
			...
		FROM
			{schemaPrefix}CULTUREISOLATION ci
			JOIN {schemaPrefix}REQUEST r ON r.REQUESTID = ci.REQUESTID
			JOIN {schemaPrefix}PATIENT p ON p.PATIENTID = r.PATIENTID
			...";

	if (limitRegistres > 0)
	{
		return $@"
			SELECT * FROM (
				{consultaBase}
			) WHERE ROWNUM <= {limitRegistres}";
	}

	return consultaBase;
}
```

#### Key Differences
| Aspect | Before | After |
|--------|--------|-------|
| Schema prefix | None (hardcoded tables) | Dynamic, from `ObtenirSchemaPrefix()` |
| Table names | `CULTUREISOLATION` | `{schemaPrefix}CULTUREISOLATION` |
| String format | `@"..."` | `$@"..."` (interpolation enabled) |
| Schema handling | Manual (would fail) | Automatic (detects user) |

#### Tables Updated (16 total)
1. `{schemaPrefix}CULTUREISOLATION` ← ci alias
2. `{schemaPrefix}REQUEST` ← r alias
3. `{schemaPrefix}PATIENT` ← p alias
4. `{schemaPrefix}ISOLATION` ← i alias
5. `{schemaPrefix}REQUESTTEST` ← rt alias
6. `{schemaPrefix}RESISTANCEMECHANISM` ← rm1 (x5 instances)
7. `{schemaPrefix}DOCTOR` ← d alias
8. `{schemaPrefix}SERVICE` ← ser alias
9. `{schemaPrefix}SAMPLECOLLECTIONCENTER` ← scol alias
10. `{schemaPrefix}REQUESTTESTADDITIONALINFO` ← rtai alias
11. `{schemaPrefix}TEST` ← t alias
12. `{schemaPrefix}CONTAINER` ← c alias
13. `{schemaPrefix}SAMPLE` ← sam alias
14. `{schemaPrefix}REQUESTCONTAINER` ← rc alias
15. `{schemaPrefix}ADDITIONALINFO` ← ai alias
16. `{schemaPrefix}REQUESTDIAGNOSIS` ← rd alias
17. `{schemaPrefix}DIAGNOSIS` ← dia alias

### Change 2: New Method `ObtenirSchemaPrefix()`

#### Location: After line 288 (immediately after `ObtenirConsultaResultatsProves()`)

#### Full Code
```csharp
/// <summary>
/// Obté el prefix del schema des de la connexió Oracle
/// Exemple: "MODULAB." o "" (buit si és el propietari directe)
/// </summary>
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

				// Si l'usuari és diferent de MODULAB, especificar el schema
				if (!string.IsNullOrEmpty(currentUser) && currentUser.ToUpperInvariant() != "MODULAB")
				{
					_logger.Info($"⚠️ Usuari connectat és '{currentUser}', no 'MODULAB'. Usant schema prefix 'MODULAB.'");
					return "MODULAB.";
				}

				return string.Empty;
			}
		}
	}
	catch (Exception ex)
	{
		_logger.Error($"⚠️ Error obtenint schema prefix: {ex.Message}. Intentaré sense prefix.", ex);
		return string.Empty;
	}
}
```

#### Method Logic Flow
```
┌─ Call ObtenirSchemaPrefix()
│
├─ Try:
│  ├─ Create Oracle connection
│  ├─ Open connection
│  ├─ Execute: SELECT USER FROM dual
│  ├─ Get current user name
│  ├─ Log: "🔐 Oracle usuari actual: [USER]"
│  │
│  ├─ If user != "MODULAB":
│  │  ├─ Log: "⚠️ Using MODULAB. prefix"
│  │  └─ Return: "MODULAB."
│  │
│  └─ If user == "MODULAB" or empty:
│     └─ Return: "" (empty string)
│
└─ Catch Exception:
   ├─ Log error
   └─ Return: "" (empty string - fallback)
```

#### Return Values
| Scenario | Current User | Returned Value | Query Result |
|----------|--------------|----------------|--------------|
| Direct owner | MODULAB | `""` | `FROM CULTUREISOLATION ci` |
| Different user | TECNICO | `"MODULAB."` | `FROM MODULAB.CULTUREISOLATION ci` |
| Connection error | (error) | `""` | Tries without prefix (may fail) |

#### Error Handling
- **Graceful degradation:** If detection fails, returns empty string and tries without prefix
- **Logging:** All scenarios logged with emoji indicators for visibility
- **No throwing:** Errors are caught and handled safely

## Integration Points

### Called By
- `CarregarResultatsDeMostres()` - Main entry point
- `CarregarResultatsDeMostresPerRangDates()` - Alternative loading method

### Requires
- `_connectionString` - Already available (class member)
- `_logger` - Already available (class member)

## Backward Compatibility

✅ **100% Backward Compatible**
- No changes to method signatures
- No changes to public API
- No changes to return types
- No changes to calling code
- Works with existing connection strings
- Works with existing table structures

## Performance Notes

- **Schema detection:** Executes once at query-building time (not per row)
- **Connection overhead:** Single SELECT USER query (~1ms typically)
- **No caching:** Re-detects each time for accuracy (acceptable given infrequent calls)
- **Impact on main query:** Negligible (adds query prefix, not data processing)

## Testing Notes

### Unit Test Scenarios
1. **User = MODULAB:** Verify returns empty string
2. **User = Other:** Verify returns "MODULAB."
3. **Connection error:** Verify returns empty string and error is logged
4. **NULL user result:** Verify returns empty string gracefully

### Integration Test Scenarios
1. **Full data load:** Verify no ORA-00942 with different users
2. **Performance:** Verify query execution time unchanged
3. **Data accuracy:** Verify correct data loaded (no schema issues)
4. **Error handling:** Verify graceful fallback if detection fails

## Related Documentation

- See `FIX_ORA-00942_SCHEMA_PREFIX.md` for full explanation
- See `TROUBLESHOOTING_ORA-00942.md` for diagnostic steps
- See `DEPLOYMENT_ORA-00942_FIX.md` for deployment guide
