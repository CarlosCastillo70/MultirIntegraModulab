# Integration Guide - ORA-00942 Fix Deployment

## Overview

This fix resolves the **ORA-00942: Table or View Does Not Exist** error that occurs when loading sample results from Oracle Modulab database.

## What Was Fixed

### Problem
Application couldn't find Oracle tables when:
- Connected with a different user than table owner (e.g., connecting as TECNICO but tables owned by MODULAB)
- Tables existed in MODULAB schema but weren't being qualified with schema prefix

### Solution
Automatically detects the connected user and adds schema prefix ("MODULAB.") to all table references when needed.

## Files Modified

| File | Change Type | Reason |
|------|------------|--------|
| `ModulabDbService.cs` | **Modified** | Added dynamic schema prefix detection |

## Methods Changed

### 1. `ObtenirConsultaResultatsProves()` 
- **Status:** UPDATED
- **Change:** Now uses dynamic schema prefix in all table references
- **Impact:** SQL queries now work with any connecting user

### 2. `ObtenirSchemaPrefix()` 
- **Status:** NEW
- **Change:** Detects Oracle user and returns appropriate prefix
- **Impact:** Automatic, transparent schema qualification

## Installation Steps

### Step 1: Backup Current Version
```powershell
cd C:\Projectes\MultirIntegraModulab
git commit -am "Backup before ORA-00942 fix"
```

### Step 2: Deploy New Code
The fix is already in place. Just ensure you have the updated files:
- ✅ `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\ModulabDbService.cs`

### Step 3: Rebuild Application
```powershell
# In Visual Studio:
Build -> Rebuild Solution

# Or via command line:
dotnet build MultirIntegraModulab.sln -c Release
```

### Step 4: Verify Build
Expected output:
```
Build successful
```

### Step 5: Test in Development
1. Run application in DEBUG mode
2. Monitor output for log message:
   ```
   🔐 Oracle usuari actual: [USERNAME]
   ```
3. Verify mostres load without ORA-00942 errors
4. Check that data appears correctly in application

### Step 6: Deploy to Production
```powershell
# Copy to production server
Copy-Item -Path "bin\Release\MultirIntegraModulab.exe" -Destination "\\ProdServer\SharedFolder" -Force

# Restart service
Restart-Service -Name "MultirIntegraModulab" -Force
```

## Testing Checklist

- [ ] Application starts without errors
- [ ] Log shows: `🔐 Oracle usuari actual: MODULAB` (or detected user)
- [ ] Sample loading completes successfully
- [ ] No ORA-00942 errors in logs
- [ ] Data displays correctly in application
- [ ] Performance is acceptable (no degradation)

## Rollback Plan

If issues occur:

### Quick Rollback (Revert Code Change)
```powershell
# Revert to previous version
git revert HEAD --no-edit

# Rebuild
dotnet build MultirIntegraModulab.sln -c Release

# Redeploy
```

### Manual Rollback (If Git Not Available)
1. Restore backup of `ModulabDbService.cs` from previous version
2. Rebuild application
3. Redeploy

## Performance Impact

✅ **Minimal to None**
- Schema detection runs once per application session
- Only executes one extra query: `SELECT USER FROM dual`
- Results are used to format SQL, not executed repeatedly
- No impact on query execution time

## Compatibility

- ✅ C# 7.3
- ✅ .NET Framework 4.8
- ✅ Oracle 11g and later
- ✅ Oracle.ManagedDataAccess 19.x+
- ✅ All existing code paths unaffected

## Documentation

Created comprehensive guides:
1. **FIX_ORA-00942_SCHEMA_PREFIX.md** - Technical details
2. **CHANGES_ORA-00942_FIX.md** - Change summary
3. **TROUBLESHOOTING_ORA-00942.md** - Troubleshooting steps

## Monitoring

After deployment, monitor:

**Logs to watch:**
```
✅ 🔐 Oracle usuari actual: [detected user]
⚠️ Error obtenint schema prefix: [error details]
✅ NombreTotalMostres: [count] (indicates successful load)
```

**Error patterns to avoid:**
```
❌ ORA-00942: table or view does not exist
❌ ORA-00904: invalid column name (indicates SQL parsing issue)
```

## Support & Questions

### If schema detection shows wrong user:
- Check connection string in App.config
- Verify you're connecting to correct database

### If ORA-00942 still occurs:
- See TROUBLESHOOTING_ORA-00942.md for detailed steps
- Check that reflected user has SELECT permissions on all tables

### For technical details:
- Review FIX_ORA-00942_SCHEMA_PREFIX.md

## Success Criteria

Deployment is successful when:
1. ✅ Application starts without errors
2. ✅ Log shows detected Oracle user
3. ✅ Sample data loads (no ORA-00942 errors)
4. ✅ Application processes data normally
5. ✅ Performance is acceptable
6. ✅ No new errors introduced

## Post-Deployment

1. **Monitor for 24-48 hours** for any issues
2. **Check logs daily** for error patterns
3. **Verify data accuracy** against manual checks
4. **Keep this documentation** for future reference

## Version Information

- **Fix Version:** 1.0
- **Date:** 2024-01-XX
- **Files Modified:** 1
- **Build Status:** ✅ Passing
- **Breaking Changes:** None
