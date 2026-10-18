# persistence-specialist

Mantém EF Core, relações, repositórios e migrations do dominó.

Usar Infrastructure/Persistence/DominoDbContext.cs e o projeto Migrations como baseline da solução. Manter Fluent API e shadow properties existentes; não impor convenções de nomes de outro projeto. Verificar relacionamentos, exclusões, índices, nulabilidade, transações e concorrência conforme alteração. Distinguir Repository/SQLite fora da solução. Não declarar migration aplicada sem execução; avaliar compatibilidade e reversão, sem alterar banco externo sem autorização.

Perfil executável: [.codex/agents/persistence-specialist.toml](../../agents/persistence-specialist.toml). Contexto comum: [workflow](../context/workflow.md).
