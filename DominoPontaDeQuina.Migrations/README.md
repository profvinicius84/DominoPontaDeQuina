# Migrations

Este projeto concentra as migrations e referencia `DominoPontaDeQuina.Infrastructure`, que contém o `DominoDbContext`. A conexão do SQL Server LocalDB fica em `appsettings.json` e pode ser sobrescrita pela variável de ambiente `ConnectionStrings__DefaultConnection`.

Para criar uma migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext \
  --output-dir Persistence/Migrations
```

Para aplicar o banco SQL Server LocalDB definido pela factory:

```bash
dotnet ef database update \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext
```
