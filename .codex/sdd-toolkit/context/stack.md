# Dominó Ponta de Quina — Stack

## Evidência atual
- C# e projetos com target `net8.0`.
- EF Core 8.0.19 em Infrastructure/Migrations; SQL Server em Migrations.
- SQL Server LocalDB conforme README raiz.
- xUnit 2.9.2, Microsoft.NET.Test.Sdk e coverlet.collector no projeto de testes.
- Solução `DominoPontaDeQuina.slnx`.
- GitHub Actions configurado para SDK .NET 8 e build/test do projeto de testes.
- Repository usa EF Core 8.0.15/SQLite, mas não integra a solução.

## Planejado
API, Blazor e .NET MAUI conforme direcionamento do responsável. Pacotes, modelo de hospedagem e plataformas do cliente serão definidos ao especificar essas entregas.

Não atualizar o runtime apenas por herança de configuração. O suporte ao formato .slnx depende do SDK instalado: conferir antes de executar os comandos da solução. O workflow atual opera diretamente no .csproj de testes.
