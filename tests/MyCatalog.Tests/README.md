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

A proteção de inicialização também é testada: configuração ausente ou falsa não resolve
serviços de banco; Production/Staging não executam o seed mesmo com a chave habilitada;
Development com ativação explícita executa os exemplos somente em SQLite em memória,
sem duplicações. Um teste usa uma variável de ambiente com prefixo exclusivo e limpeza
ao final para verificar o mapeamento de `CatalogSeed__Enabled`.

Além dos 111 casos anteriores (23 originais, 37 de integração de capas, 6 de proteção do ambiente manual isolado, 30 de relevância e idiomas e 15 de identidade bibliográfica e agrupamento por metadados), há 7 casos de proteção do seed. As respostas HTTP são
simuladas com HttpMessageHandler e nunca usam a internet. O contato example.invalid
existe somente nos testes offline e não deve ser usado na configuração da aplicação.
Os testes verificam também que o modo desabilitado funciona sem a tabela de capas.
Veja `docs/CAPAS-OPEN-LIBRARY.md` para a validação separada da migration SQL Server.
