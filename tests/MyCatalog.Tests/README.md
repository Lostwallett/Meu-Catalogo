# Testes do MyCatalog

Execute na raiz da solução:

```powershell
dotnet build
dotnet test
```

No Visual Studio, abra `MyCatalog.slnx`, compile e use **Teste > Gerenciador de Testes > Executar Todos**.

Os testes CSV executam o serviço diretamente. Os testes de seed criam uma conexão SQLite
em memória exclusiva por teste e descartam tudo ao fechar a conexão. `EnsureCreatedAsync`
cria apenas esse esquema temporário: não executa migrations nem acessa o LocalDB.
Nenhum teste inicia `Program.cs` ou lê a connection string da aplicação.

A cobertura inclui formato e segurança do CSV, favoritos em português, inserção dos
25 exemplos, repetição do seed, comparação por título e tipo, preservação de registros
e de edições posteriores. O SQLite verifica persistência e transações relacionais,
mas não substitui testes específicos de concorrência ou configuração do SQL Server.
O seed identifica exemplos por título e tipo; renomear esses campos pode fazer com
que um exemplo original seja considerado ausente numa execução posterior.
