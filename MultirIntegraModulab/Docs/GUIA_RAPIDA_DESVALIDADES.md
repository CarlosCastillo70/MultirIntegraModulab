# 🚀 Guia Ràpida - Mostres Desvalidades Millorades

## ⚡ Què és?

Sistema que detecta si una mostra desvalidada ha canviat i actua en conseqüència.

## 📌 Cas d'Ús

**Situació**: Oracle retorna una mostra sense `data_validacio` però MySQL té `data_validacio` (mostra desvalidada)

## 🔄 Dos Escenaris

### 🟢 Escenari 1: SIN CANVIS
**Condició**: Mostra actual = Mostra nova  
**Acció**: 
- Actualitza `data_validacio` → NULL
- Actualitza `estat_integracio_m` → 'P'
- Insereix auditoria 'EMCD'
- ❌ NO continua processament

### 🔴 Escenari 2: AMB CANVIS
**Condició**: Mostra actual ≠ Mostra nova  
**Acció**:
- Guarda historial amb detall
- Esborra dades (soft delete)
- ✅ Continua processament (reprocessa mostra)

## 📊 Diagrama Simple

```
Desvalidada?
     │
     ├─→ Comparar
     │      │
     │   Iguals?
     │   ├─ SÍ  → Actualitza NULL → Auditoria EMCD → STOP
     │   └─ NO  → Historial → Delete → CONTINUA
```

## 💻 Codi Clau

```csharp
// Cas 1: Sense canvis
if (!resultatComparacio.HiHaCanvis) {
    ActualitzarDataValidacio(etiquetaId, null);
    InserirAuditoriaIntegracioModulab(mostra, "EMCD");
    return false; // No continuar
}

// Cas 2: Amb canvis
else {
    GuardarHistorialMostra(etiquetaId, "DESVALIDADA", observacions);
    EsborrarDadesMostra(etiquetaId);
    return true; // Continuar
}
```

## 🔧 Configuració Ràpida

```bash
# 1. Executar SQL
mysql -u user -p marsa < SQL_INSERT_AUDIT_CODE_EMCD.sql

# 2. Verificar
SELECT * FROM integracio_modulab_resultats WHERE codi = 'EMCD';
```

## 📝 Logs a Buscar

### ✅ Cas Exitós Sense Canvis
```
✅ Mostres idèntiques - actualitzant data_validacio a NULL...
✅ Auditoria EMCD creada correctament
```

### ✅ Cas Exitós Amb Canvis
```
🔄 Mostres diferents - guardant historial i esborrant dades...
   📝 Data resultat: 15/01 -> 16/01
✔️ Historial guardat correctament
✔️ Dades esborrades correctament
➡️ Continuant processament
```

## ⚠️ Errors Comuns

| Error | Causa | Solució |
|-------|-------|---------|
| No s'ha trobat mostra existent | Etiqueta no existeix | Verificar ETIQUETA_ID |
| No s'ha pogut crear auditoria | Codi EMCD no existeix | Executar script SQL |
| Error esborrant mostra | Clau forana | Revisar dependències |

## 🎯 Taula Resum

| Camps comparats | ✓ |
|----------------|---|
| Data resultat | ✅ |
| Data validació | ✅ |
| Tipus mostra | ✅ |
| Tipus prova | ✅ |

## 📦 Fitxers Clau

- `ProcessarMostresUseCase.cs` → Mètode `TractarMostraDesvalidada()`
- `MultiRDbServiceExtensions.cs` → `ObtenirMostraDiagnostic()`, `CompararMostres()`
- `SQL_INSERT_AUDIT_CODE_EMCD.sql` → Script per BD
- `TRACTAMENT_MOSTRES_DESVALIDADES_MILLORAT.md` → Documentació completa

## 🔎 Queries Útils

```sql
-- Veure mostres desvalidades processades sense canvis
SELECT * FROM integracio_modulab WHERE resultat = 'EMCD';

-- Veure historial de mostres desvalidades amb canvis
SELECT * FROM pacients_diagnostics_mostra_historial 
WHERE tipus_canvi = 'DESVALIDADA_AMB_CANVIS';

-- Comptar desvalidades per tipus
SELECT 
    CASE 
        WHEN resultat = 'EMCD' THEN 'Sense canvis'
        ELSE 'Amb canvis'
    END AS tipus,
    COUNT(*) AS total
FROM integracio_modulab
WHERE resultat IN ('EMCD', 'DESVALIDADA')
GROUP BY tipus;
```

## ✅ Checklist Verificació

- [ ] Script SQL executat
- [ ] Codi EMCD visible a BD
- [ ] Compilació exitosa
- [ ] Logs mostren tractament correcte
- [ ] Auditories es creen correctament

---

**Versió**: 1.0  
**Última actualització**: 2024
