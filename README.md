# MyCatalog

Aplicação web para organizar um catálogo pessoal de filmes, séries, livros, jogos e outros conteúdos. Permite cadastrar obras, consultar informações, combinar filtros, marcar favoritos e exportar resultados para CSV.

O projeto foi desenvolvido para praticar desenvolvimento backend com C# e ASP.NET Core MVC, integrando interface Razor, validação de formulários, persistência relacional e testes automatizados. É um projeto de estudo e portfólio, com execução local e sem autenticação de usuários na versão atual.

## Tecnologias

| Área | Tecnologias utilizadas |
| --- | --- |
| Aplicação | C#, .NET 10 e ASP.NET Core MVC |
| Interface | Razor Views, HTML, CSS, JavaScript e Bootstrap 5.3.3 via CDN |
| Persistência | Entity Framework Core 10.0.14 e SQL Server Express LocalDB |
| Testes | xUnit, Microsoft.NET.Test.Sdk e runner para Visual Studio |
| Banco dos testes | SQLite em memória com Entity Framework Core |
| Versionamento | Git e GitHub |

## Funcionalidades implementadas

- Cadastro, listagem, detalhes, edição e exclusão com confirmação.
- Campos de título, tipo, autor ou diretor, ano de lançamento, gênero, descrição e favorito, além do identificador do registro.
- Validação no servidor com Data Annotations e proteção antifalsificação nos formulários POST.
- Pesquisa por título e filtros combináveis por tipo e favoritos.
- Ordenação por título ou ano, em ambos os sentidos, com paginação de 10 itens.
- Estatísticas gerais do catálogo e contagem dos resultados filtrados.
- Visualização em tabela ou cartões, com preferência salva no `localStorage` do navegador.
- Dados demonstrativos para facilitar a exploração da aplicação em desenvolvimento.
- Exportação CSV dos resultados da pesquisa e dos filtros aplicados.
- Testes automatizados do gerador CSV, seed e integração de capas com HTTP simulado.
- Nesta branch: pesquisa de capas de livros por obra e edição, seleção manual e associação persistente. Desabilitada por padrão até configurar um banco isolado e um contato válido; veja [o procedimento seguro](docs/CAPAS-OPEN-LIBRARY.md).

### Dados demonstrativos

Em `Development`, a inicialização verifica 25 exemplos: 7 filmes, 7 livros, 7 séries, 3 jogos e 1 peça teatral na categoria Outro. Somente os exemplos ausentes são inseridos, mesmo que já existam outros registros no catálogo.

A identificação usa título e tipo, ignorando maiúsculas e diferenças de espaçamento. Registros existentes não são atualizados: IDs, favoritos e informações preenchidas são preservados. Repetir a inicialização não duplica exemplos com a mesma identificação. Se o título ou tipo de um exemplo for alterado, a próxima execução poderá considerar o exemplo original ausente e inseri-lo novamente.

O seed exige o esquema do banco já criado. Ele não cria o banco nem aplica migrations automaticamente e não é chamado fora de `Development`.

### Exportação CSV

O botão **Exportar CSV** baixa `MyCatalog.csv`, respeitando pesquisa, tipo, favoritos e ordenação. O arquivo inclui todos os resultados correspondentes, independentemente da página exibida.

- UTF-8 com BOM e separador `;`, para facilitar a abertura no Excel em português do Brasil.
- Cabeçalhos amigáveis: Código, Título, Tipo, Autor ou diretor, Ano de lançamento, Gênero, Descrição e Favorito.
- Favoritos representados por **Sim** ou **Não**.
- Tratamento de aspas, separadores e quebras de linha dentro dos campos.
- Prefixo de apóstrofo em valores potencialmente interpretados como fórmulas por planilhas; essa proteção altera apenas o arquivo exportado.

## Arquitetura

A aplicação segue o padrão MVC do ASP.NET Core:

- **Models:** `CatalogItem` representa o item e suas validações. `CatalogIndexViewModel` reúne itens, filtros, estatísticas e paginação para a tela de listagem.
- **Views:** páginas Razor apresentam os dados e os formulários, com componentes visuais do Bootstrap.
- **Controllers:** recebem requisições, validam entradas, consultam o contexto e retornam páginas ou arquivos. A listagem e a exportação compartilham a lógica de filtros e ordenação.

O `ApplicationDbContext` faz o acesso ao banco pelo EF Core e é recebido pelos controllers por injeção de dependência. O `CatalogCsvExporter` concentra a formatação do arquivo, e o `CatalogSeedData` concentra os exemplos demonstrativos. O `Program.cs` configura serviços, conexão, pipeline HTTP, rotas e inicialização do seed.

```text
MyCatalog.slnx
MyCatalog.csproj
Controllers/                 Requisições MVC e ações do catálogo
Models/                      Entidade e ViewModel
Views/                       Páginas Razor e layout compartilhado
Data/                        DbContext e dados demonstrativos
Services/                    Gerador CSV
Migrations/                  Histórico de esquema do EF Core
wwwroot/                     CSS e JavaScript
Properties/launchSettings.json  Perfil local de execução
tests/MyCatalog.Tests/       Projeto xUnit e documentação dos testes
```

## Executar localmente

Nesta máquina, o teste manual de capas já possui banco e perfis isolados. Consulte o [passo a passo para o Visual Studio](docs/TESTE-MANUAL-CAPAS.md). A API continua desativada até configurar um contato válido e autorizar seu uso.

### Pré-requisitos

- Windows, para usar a configuração atual de SQL Server LocalDB.
- SDK .NET 10.
- SQL Server Express LocalDB com a instância `MSSQLLocalDB` disponível.
- Visual Studio com suporte ao .NET 10 e à solução `.slnx`, com a carga de trabalho **Desenvolvimento para ASP.NET e Web**, caso utilize a IDE.
- Internet para restaurar pacotes NuGet e carregar o Bootstrap via CDN.

### 1. Obter o projeto e restaurar os pacotes

```powershell
git clone https://github.com/Lostwallett/Meu-Catalogo.git
cd Meu-Catalogo
dotnet restore
dotnet build
```

### 2. Conferir a conexão

A configuração `ConnectionStrings:DefaultConnection` está em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MyCatalogCompletoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

A conexão utiliza autenticação integrada do Windows, sem senha no arquivo. Confirme a instância e o nome do banco antes de aplicar o esquema. Quem já possui esse banco com a migration aplicada não precisa recriá-lo.

### 3. Preparar um banco isolado nesta branch

A branch `experimento/capas-catalogo` inclui a migration inicial e a migration aditiva
`AddCatalogItemCover`. **Não gere outra migration e não aplique mudanças em `MyCatalogCompletoDb`.**

A factory de design exige uma conexão isolada explícita e bloqueia o nome do banco
principal. Siga o procedimento completo em [Capas pela Open Library](docs/CAPAS-OPEN-LIBRARY.md),
que prepara um novo banco, aplica o esquema e define a conexão da aplicação na mesma
janela do terminal. A aplicação não executa migrations automaticamente.

Quem já utiliza o banco original pode manter `BookCovers:Enabled=false` e continuar
usando as funcionalidades anteriores sem aplicar a nova migration.

### 4. Iniciar a aplicação

No Visual Studio, selecione o perfil **https** e pressione **F5** ou **Ctrl+F5**. Pelo terminal:

```powershell
dotnet run --project MyCatalog.csproj --launch-profile https
```

O perfil define `ASPNETCORE_ENVIRONMENT=Development` e as URLs locais `https://localhost:7157` e `http://localhost:5157`. Abra a URL HTTPS e clique em **Meu Catálogo**. Se necessário, configure a confiança no certificado de desenvolvimento:

```powershell
dotnet dev-certs https --trust
```

Na primeira execução em desenvolvimento, os exemplos ausentes serão cadastrados. A interface permite explorar tabela, cartões, filtros e exportação sem cadastro prévio de itens.

## Testes automatizados

Na raiz da solução:

```powershell
dotnet build
dotnet test
```

No Visual Studio, abra **Teste > Gerenciador de Testes** e escolha **Executar Todos**.

Os testes CSV executam o serviço diretamente. Cada teste de persistência utiliza um banco SQLite privado em memória, descartado quando a conexão é fechada. Os testes não iniciam `Program.cs`, não leem a conexão da aplicação e não acessam o LocalDB principal.

A suíte atual contém **111 casos** (incluindo os 23 originais), cobrindo:

- Cabeçalho em português, separador, BOM, acentos, campos opcionais e favoritos.
- Escape de aspas e quebras de linha, preservação da ordem recebida e proteção contra fórmulas.
- Inserção dos 25 exemplos, execução repetida sem duplicações e comparação por título e tipo.
- Preservação de registros existentes e de edições posteriores em seus demais campos.
- Respostas e erros da Open Library por HTTP simulado, identificação, cache, limites e cancelamento.
- Tokens de seleção, associação persistente, concorrência e preservação do CSV.

Na verificação desta atualização de documentação, a compilação terminou sem avisos ou erros e os 111 testes passaram, sem falhas ou casos ignorados. A suíte não cobre todo o fluxo MVC, a interface ou particularidades de concorrência do SQL Server.

Mais detalhes em [tests/MyCatalog.Tests/README.md](tests/MyCatalog.Tests/README.md).

## Capturas de tela

Espaço reservado para imagens reais da aplicação. As capturas ainda não foram adicionadas ao repositório.

| Tela | Captura a adicionar |
| --- | --- |
| Catálogo em tabela | Estatísticas, filtros e paginação |
| Catálogo em cartões | Visualização alternativa e favoritos |
| Cadastro ou edição | Campos e mensagens de validação |
| Exportação CSV | Exemplo do arquivo aberto em uma planilha |

## Possíveis melhorias futuras

As ideias abaixo **ainda não estão implementadas** e não representam funcionalidades disponíveis na versão atual:

- Autenticação e separação dos catálogos por usuário.
- Mais testes de integração para controllers, filtros e exportação HTTP, além de testes de interface.
- Pipeline de integração contínua no GitHub Actions para build e testes.
- Configuração de implantação e banco adequada a um ambiente de produção.
- Integração de capas para filmes, séries e jogos; novas opções de organização dos itens.
- Distribuição local do Bootstrap para reduzir a dependência de CDN.

A versão atual concentra-se no catálogo MVC com persistência local, dados demonstrativos, exportação CSV e testes dos serviços descritos acima.
