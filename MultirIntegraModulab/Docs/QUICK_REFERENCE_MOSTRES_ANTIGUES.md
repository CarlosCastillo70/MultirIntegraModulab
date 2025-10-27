# 🚀 Quick Reference: Mostres Antigues

## ⚡ Resum Ràpid

**Problema**: Mostres amb `data_resultat = NULL` i `data_validacio = NULL` a MySQL  
**Solució**: Actualitzar amb dates d'Oracle i inserir auditoria EMCA  
**Durada**: Temporal (pocs dies)

## 📋 Checklist d'Implementació

- [x] Mètode `TractarMostraAntigua()` implementat
- [x] Mètode `ActualitzarResultatAntic()` utilitzat (ja existia)
- [x] Auditoria EMCA implementada
- [x] Logs amb indentació correcta
- [x] Documentació completa creada
- [x] Script SQL preparat
- [x] Build successful

## 🗂️ Fitxers Modificats/Creats

### Modificats
1. `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`
   - Mètode `TractarMostraAntigua()` implementat

### Creats
1. `MultirIntegraModulab\Docs\SQL_INSERT_AUDIT_CODES.sql`
   - Script per inserir codis EMCA i EMCR

2. `MultirIntegraModulab\Docs\TRACTAMENT_MOSTRES_ANTIGUES.md`
   - Documentació detallada

3. `MultirIntegraModulab\Docs\RESUM_IMPLEMENTACIO_MOSTRES_ANTIGUES.md`
   - Resum d'implementació

4. `MultirIntegraModulab\Docs\QUICK_REFERENCE_MOSTRES_ANTIGUES.md`
   - Aquest fitxer

## 🔧 Abans de Deploy

```bash
# 1. Executar script SQL
mysql -u user -p marsa < MultirIntegraModulab/Docs/SQL_INSERT_AUDIT_CODES.sql

# 2. Verificar inserció
mysql -u user -p marsa -e "SELECT * FROM integracio_modulab_resultats WHERE codi IN ('EMCA', 'EMCR');"
```

## 📊 Consultes Útils

```sql
-- Veure mostres antigues tractades
SELECT COUNT(*) as total_mostres_antigues
FROM integracio_modulab 
WHERE resultat = 'EMCA';

-- Veure últimes mostres antigues tractades
SELECT etiqueta_id, pacient_sap, dt_create
FROM integracio_modulab 
WHERE resultat = 'EMCA'
ORDER BY dt_create DESC
LIMIT 10;

-- Evolució per dies
SELECT DATE(dt_create) as data, COUNT(*) as total
FROM integracio_modulab 
WHERE resultat = 'EMCA'
GROUP BY DATE(dt_create)
ORDER BY data DESC;
```

## 🎯 Què fa el codi?

```csharp
// 1. Detecta mostra antiga
if (tipusIncorporacio == TipusIncorporacio.Antiga)
{
    // 2. Obté dates d'Oracle
    var dataResultat = mostra.Resultats[0].DataResultat;
    var dataValidacio = mostra.Resultats[0].DataValidacio;
    
    // 3. Actualitza MySQL
    _multiRRepository.ActualitzarResultatAntic(
        mostra.EtiquetaId, 
        dataResultat, 
        dataValidacio);
    
    // 4. Auditoria
    _multiRRepository.InserirAuditoriaIntegracioModulab(
        mostra, 
        "EMCA", 
        primerResultat, 
        null);
    
    // 5. NO continuar processament
    return false;
}
```

## 📈 Logs Esperats

```
⚠️ Mostra antiga (sense dates) - actualitzant dates...
   ✅ Dates actualitzades correctament
      - Data resultat: 15/01/2024 10:30
      - Data validació: 15/01/2024 14:25
   ✅ Auditoria EMCA (Estat Mostra Cas Antic) creada correctament
```

## ⚠️ Important

- ✅ **No continuar processament** després d'actualitzar dates
- ✅ **Temporal**: Només durant els primers dies
- ✅ **Data validació pot ser NULL**
- ✅ **Estat integració**: 'V' si validada, 'P' si pendent

## 🔍 Troubleshooting

### Problema: No s'actualitzen les dates
**Solució**: Verificar que `ActualitzarResultatAntic()` retorna true

### Problema: No es crea auditoria
**Solució**: Verificar que el codi EMCA existeix a `integracio_modulab_resultats`

### Problema: Massa mostres antigues
**Solució**: Normal els primers dies, disminuirà gradualment

## 📞 Contacte

Per dubtes o problemes amb aquesta implementació, consultar:
- `TRACTAMENT_MOSTRES_ANTIGUES.md` - Documentació detallada
- `RESUM_IMPLEMENTACIO_MOSTRES_ANTIGUES.md` - Resum complet

---

**Versió**: 1.0  
**Data**: $(Get-Date -Format "dd/MM/yyyy")  
**Estat**: ✅ Production Ready
