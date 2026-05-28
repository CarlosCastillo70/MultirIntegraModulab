# Exemples de Logs del Patró de Mostres

## Format del Patró Visual

La funció `ActualitzarQuantitatTargetes` ara mostra el patró visual de les mostres al log:

### Codis utilitzats:
- **P** = Positiu (valoració = '2')
- **N** = Negatiu (valoració = '1')
- **X** = No vàlid (valoració = '3')
- **?** = Pendent (valoració = '0')
- **·** = Valor desconegut
- **-** = Espai lliure (targetes pendents)

## Exemples de Logs

### Exemple 1: Mostra amb 1 negatiu + nou positiu
```
📋 Trobades 1 mostra(es) amb tipus mostra 'Frotis rectal' des de l'inici del seguiment
🔍 Patró actual de mostres: N
   🔴 Última mostra positiva trobada a l'índex 0 (Data: 13/02/2025)
Mostres després de l'última positiva: 0
Espais lliures necessaris: 3
Nova quantitat calculada: 4 (actual: 3) → Patró resultant: NP---
✅ Targetes actualitzades: 3 → 4 (seguiment ID 123)
```

### Exemple 2: Mostra amb 2 negatius + nou positiu
```
📋 Trobades 2 mostra(es) amb tipus mostra 'Aspirat traqueal' des de l'inici del seguiment
🔍 Patró actual de mostres: NN
   🔴 Última mostra positiva trobada a l'índex 1 (Data: 13/02/2025)
Mostres després de l'última positiva: 0
Espais lliures necessaris: 3
Nova quantitat calculada: 5 (actual: 2) → Patró resultant: NNP---
✅ Targetes actualitzades: 2 → 5 (seguiment ID 456)
```

### Exemple 3: Mostra amb patró complex PNNPN + nou positiu
```
📋 Trobades 5 mostra(es) amb tipus mostra 'Frotis nasal' des de l'inici del seguiment
🔍 Patró actual de mostres: PNNPN
   🔴 Última mostra positiva trobada a l'índex 4 (Data: 13/02/2025)
Mostres després de l'última positiva: 0
Espais lliures necessaris: 3
Nova quantitat calculada: 8 (actual: 7) → Patró resultant: PNNPNP---
✅ Targetes actualitzades: 7 → 8 (seguiment ID 789)
```

### Exemple 4: Mostra amb 3 negatius consecutius (no cal actualitzar)
```
📋 Trobades 4 mostra(es) amb tipus mostra 'Exsudat cutani' des de l'inici del seguiment
🔍 Patró actual de mostres: PNNN
   🔴 Última mostra positiva trobada a l'índex 0 (Data: 10/02/2025)
Mostres després de l'última positiva: 3
Espais lliures necessaris: 0
Nova quantitat calculada: 4 (actual: 4) → Patró resultant: PNNN
ℹ️ No cal actualitzar (nova quantitat 4 <= actual 4)
```

### Exemple 5: Mostres amb valor no vàlid o pendent
```
📋 Trobades 6 mostra(es) amb tipus mostra 'Orina' des de l'inici del seguiment
🔍 Patró actual de mostres: PNN?XN
   🔴 Última mostra positiva trobada a l'índex 0 (Data: 08/02/2025)
Mostres després de l'última positiva: 5
Espais lliures necessaris: 0
Nova quantitat calculada: 6 (actual: 6) → Patró resultant: PNN?XN
ℹ️ No cal actualitzar (nova quantitat 6 <= actual 6)
```

### Exemple 6: Cap mostra positiva (només negatius)
```
📋 Trobades 2 mostra(es) amb tipus mostra 'Frotis rectal' des de l'inici del seguiment
🔍 Patró actual de mostres: NN
No hi ha cap mostra positiva. Total mostres: 2
Espais lliures necessaris: 1
Nova quantitat calculada: 3 (actual: 3) → Patró resultant: NN-
ℹ️ No cal actualitzar (nova quantitat 3 <= actual 3)
```

### Exemple 7: Cap seguiment obert
```
🎯 Actualitzant possibles targetes de seguiment per pacient 12345678, tipus mostra 'Frotis rectal'
ℹ️ No hi ha seguiments oberts per aquest pacient i tipus de mostra 'Frotis rectal'
```

## Interpretació del Patró

El patró visual permet veure d'un cop d'ull:

1. **Seqüència temporal**: Les mostres es mostren en ordre cronològic (esquerra = més antiga)
2. **Última positiva**: Es pot identificar ràpidament la posició de l'última P
3. **Mostres després**: Es veu clarament quantes N o altres valors hi ha després de l'última P
4. **Espais necessaris**: Els guions (-) mostren quantes targetes més calen per assolir 3 negatius consecutius
5. **Estat d'assoliment**: Si no hi ha guions i hi ha 3+ N consecutives al final, l'objectiu s'ha assolit

## Beneficis

✅ **Visualització ràpida**: Es veu immediatament l'estat del seguiment  
✅ **Debugging fàcil**: Es pot verificar si el càlcul és correcte  
✅ **Traçabilitat**: Queda registrat al log l'estat exacte en el moment de l'actualització  
✅ **Històric**: Es pot seguir l'evolució del patró en diferents execucions  

## Data d'Actualització
13 de febrer de 2025
