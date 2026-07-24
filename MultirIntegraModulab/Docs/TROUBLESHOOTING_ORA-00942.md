# Troubleshooting Guide - ORA-00942 Error

## If You Still See ORA-00942 After Deploy

### Step 1: Check Connection String
Verify `App.config` has correct Oracle details:

```xml
<connectionStrings>
	<add name="OracleModulab"
		 connectionString="Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = [CORRECT_HOST])(PORT = [CORRECT_PORT]))) (CONNECT_DATA = (SERVICE_NAME = [CORRECT_SERVICE])));User Id=[VALID_USER];Password=[VALID_PASSWORD];"
		 providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

**Verify:**
- ✅ HOST exists and is reachable
- ✅ PORT is correct (default: 1521)
- ✅ SERVICE_NAME matches your Oracle instance
- ✅ User and Password are valid

### Step 2: Check Logs
Look for this log entry when application starts:

```
🔐 Oracle usuari actual: MODULAB
```

**If you see this**, the fix is working correctly.

**If you see:**
```
⚠️ Error obtenint schema prefix: ...
```
Then schema detection failed, but will fallback to trying without prefix.

### Step 3: Verify Oracle Access
Test directly in Oracle SQL*Plus or SQL Developer:

```sql
-- Test 1: See current user
SELECT USER FROM dual;

-- Test 2: Try to access MODULAB tables
SELECT COUNT(*) FROM MODULAB.CULTUREISOLATION;
SELECT COUNT(*) FROM MODULAB.REQUEST;

-- If above fails, try without schema (if you ARE MODULAB user):
SELECT COUNT(*) FROM CULTUREISOLATION;
SELECT COUNT(*) FROM REQUEST;

-- Test 3: Check if your user has permissions
SELECT * FROM USER_TAB_PRIVS WHERE TABLE_NAME = 'CULTUREISOLATION';
```

### Step 4: Database Objects Investigation

If tables still can't be found:

```sql
-- Check if MODULAB schema exists
SELECT COUNT(*) FROM dba_users WHERE username = 'MODULAB';

-- See all schemas
SELECT username FROM dba_users WHERE account_status = 'OPEN' ORDER BY username;

-- List all tables visible to current user (first 20)
SELECT table_name FROM user_tables WHERE ROWNUM <= 20;

-- List tables in MODULAB schema only
SELECT table_name FROM dba_tables WHERE owner = 'MODULAB' AND ROWNUM <= 20;

-- Check if tables exist at all
SELECT COUNT(*) FROM dba_tables WHERE table_name = 'CULTUREISOLATION';
```

### Step 5: Grant Permissions
If user lacks permissions, run as DBA:

```sql
-- Grant SELECT on all MODULAB tables to your user
GRANT SELECT ON MODULAB.CULTUREISOLATION TO [YOUR_USER];
GRANT SELECT ON MODULAB.REQUEST TO [YOUR_USER];
-- ... repeat for each table

-- Or grant SELECT on all tables in schema:
GRANT SELECT ANY TABLE ON DATABASE TO [YOUR_USER];
```

### Step 6: Check Network/Firewall
If you can't connect at all:

```powershell
# From command line, test connection to Oracle host
Test-NetConnection -ComputerName [ORACLE_HOST] -Port 1521

# If blocked, check firewall rules or contact network admin
```

### Step 7: Oracle Client Version
Ensure you have Oracle client libraries installed:

```powershell
# Check if Oracle.ManagedDataAccess can load
dir "C:\Program Files\Oracle\*" -Recurse -Filter "*.dll" | Select-Object Name
```

## Common Error Messages and Solutions

### Error: "ORA-12514: TNS:listener does not currently know of service requested"
**Cause:** Wrong SERVICE_NAME
**Fix:** Verify SERVICE_NAME in connection string

### Error: "ORA-01017: invalid username/password; logon denied"
**Cause:** Wrong credentials
**Fix:** Double-check User Id and Password

### Error: "ORA-12505: TNS:listener does not currently know of SID given in connect descriptor"
**Cause:** Using SID instead of SERVICE_NAME, or SID is wrong
**Fix:** Use SERVICE_NAME instead, or get correct SID from DBA

### Error: "The network adapter could not establish the connection"
**Cause:** Cannot reach Oracle server
**Fix:** Check HOST, PORT, firewall, network connectivity

## Manual Override (If Auto-Detection Doesn't Work)

If auto-detection fails, you can manually force the schema prefix:

1. Open `ModulabDbService.cs`
2. Find this method:
```csharp
private string ObtenirSchemaPrefix()
{
	// Auto-detection code here
}
```

3. Replace with:
```csharp
private string ObtenirSchemaPrefix()
{
	// Manual override - always use MODULAB schema
	_logger.Info("🔐 Usant MODULAB schema (manual override)");
	return "MODULAB.";
}
```

4. Rebuild and redeploy

## Contact & Support

If none of these steps work:

1. **Collect information:**
   - Output from logs showing the exact error
   - Oracle connection string (without password)
   - Oracle username trying to connect
   - Schema where tables actually are located
   - Oracle version information

2. **Escalate to:**
   - Database Administrator (verify table locations, permissions)
   - Network/DevOps (verify connectivity to Oracle server)
   - Development team (review application logs)

## Prevention Checklist

✅ Always verify connection string matches target environment
✅ Test connection BEFORE deploying application
✅ Ensure user has SELECT permissions on all required tables
✅ Check logs for schema detection messages
✅ Keep Oracle client libraries updated
✅ Document which schema owns the tables
✅ Maintain separate connection strings for DEV/TEST/PROD
