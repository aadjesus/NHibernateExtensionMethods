# ChunkBy
Classe útil para utilizar em uma `IQueryable` do `NHibernate`, onde a mesma criar a clausula `IN` do banco de dados com a quantidade de itens compatível ao bando de dados utilizado.

Limites permitidos para os bancos:
- Oracle = 999 
- SQL Server = 1999 

#### Utilização
```
using var session = _sessionFactory.OpenSession();
var query = session.Query<DataData>()
	.ChunkBy(c => c.Id, new int[] { 1, 3, 4,...,1500 })
	.Where(whereData);
```

#### Select banco
```
Select Id
     , Nome  
  From Tabela
 Where (Id In (1, 2, 3,...,999) OR Id In (1000,1001,1003,...,1500))
 
```