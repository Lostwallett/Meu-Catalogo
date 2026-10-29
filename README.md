# MyCatalog — projeto completo para estudo

Projeto ASP.NET Core MVC (.NET 10) com Entity Framework Core e SQL Server LocalDB.

## Recursos
- CRUD: cadastrar, listar, detalhar, editar e excluir itens.
- Pesquisa por título, filtro por tipo e filtro de favoritos combináveis.
- Ordenação por título e ano; paginação de 10 itens.
- Estatísticas gerais e contador de resultados.
- Visualização em tabela e cartões; preferência salva no navegador.
- Formulários POST com token antifalsificação.

## Requisitos
- Visual Studio 2026 com desenvolvimento web ASP.NET e SDK .NET 10.
- SQL Server Express LocalDB.
- Acesso à internet para o CSS/JS do Bootstrap via CDN.

## Como executar em um projeto SEPARADO
1. Abra `MyCatalog.csproj` no Visual Studio.
2. Aguarde a restauração dos pacotes NuGet.
3. Abra **Ferramentas > Gerenciador de Pacotes NuGet > Console do Gerenciador de Pacotes**.
4. Confira se o projeto padrão é `MyCatalog`.
5. Execute `Add-Migration InitialCreate`.
6. Execute `Update-Database`.
7. Pressione Ctrl+F5.

Também é possível usar o terminal com a ferramenta `dotnet-ef` instalada:
`dotnet ef migrations add InitialCreate` e `dotnet ef database update`.

**Importante:** esta versão usa `MyCatalogCompletoDb` como banco, para NÃO mexer nos registros do seu projeto anterior (`MyCatalogDb`). Ela não inclui migrations prontas: a migration inicial deve ser gerada no ambiente do usuário.

## Onde modificar
- `Models/CatalogItem.cs`: campos e validações do item.
- `Models/CatalogIndexViewModel.cs`: dados enviados à página de listagem.
- `Controllers/CatalogItemsController.cs`: métodos CRUD, filtros, paginação e favoritos, com comentários.
- `Data/ApplicationDbContext.cs`: acesso ao banco.
- `Views/CatalogItems/Index.cshtml`: listagem, painel e filtros.
- `Views/CatalogItems/_CatalogForm.cshtml`: campos compartilhados entre cadastro e edição.
- `Views/CatalogItems/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `Delete.cshtml`: telas individuais.
- `wwwroot/css/site.css`: aparência.
- `wwwroot/js/catalog-view.js`: troca de tabela/cartões e localStorage.
- `appsettings.json`: string de conexão do banco.

## Observações
- É uma versão consolidada independente, não uma cópia dos arquivos existentes no computador.
- Não copie por cima do projeto original sem backup. Para migrar dados, será necessário um procedimento separado.
- Esta é uma aplicação de estudo local: ainda não tem autenticação, autorização por usuário, testes automatizados ou preparação completa para produção.
- O Bootstrap é carregado por CDN. Para uso offline, seria necessário incluir os arquivos localmente.
