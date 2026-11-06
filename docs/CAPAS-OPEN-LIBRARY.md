# Capas de livros pela Open Library

Integração incorporada à `main`. A migration `AddCatalogItemCover` foi aplicada e validada
no banco principal durante os testes locais, após um ensaio isolado. Os 25 registros foram
preservados; a seleção de capa para 1984 e sua persistência após reiniciar foram confirmadas
na validação manual. Esse histórico não significa que o esquema já exista em uma instalação nova.

A integração continua **desativada por padrão**, para exigir configuração explícita do banco
e do contato de identificação. Use o [guia de validação manual](TESTE-MANUAL-CAPAS.md)
para conferir o fluxo no Visual Studio. Não reaplique migrations em bancos já preparados.

## O que está disponível

- Depois de salvar um Livro, abra seus detalhes ou a edição e use **Escolher ou trocar capa**.
- Pesquise por título e autor; os termos da pesquisa não alteram o item.
- Escolha primeiro uma obra: os resultados são ordenados pela proximidade do título e autor dentro de cada página retornada pela API, ignorando diferenças de caixa, acentos, pontuação e espaços.
- As edições são carregadas de 10 em 10. Português tem prioridade entre as edições carregadas, mantendo os outros idiomas disponíveis. Use **Carregar mais edições** para ampliar o conjunto, até 100 edições por obra.
- As opções são agrupadas por ID de imagem, ID de edição ou ISBN válido com editora e metadados compatíveis, inclusive entre páginas carregadas. **Consultar edições e imagens** preserva dados, prévias, links e seleção individual. Esse agrupamento não declara que as imagens sejam visualmente iguais; veja a [investigação de 1984](AGRUPAMENTO-CAPAS-1984.md).
- As miniaturas mostram título, editora, publicação da edição, idiomas e ISBN, quando disponíveis.
  Os idiomas são apresentados como Português, Inglês, Espanhol e Francês, entre outros; códigos desconhecidos são indicados sem inventar uma tradução. O autor da obra selecionada é mostrado acima dos resultados. A data da edição não substitui
  o ano de lançamento cadastrado. Cada edição pode oferecer até três referências de imagem; elas ficam acessíveis no mesmo grupo.
- Confirme **Usar esta capa** após carregar a imagem. A aplicação grava somente a associação.
- A seleção fica disponível em cartões e detalhes após reiniciar a aplicação.
- É possível trocar a capa ou remover somente sua associação, mantendo o livro.
- Cartões preservam o controle Mostrar/Ocultar, as artes substitutas e os créditos.
- As seis ilustrações locais de filmes, séries e jogos continuam identificadas como não oficiais.
  Nenhuma integração externa desses tipos foi adicionada.
- O CSV mantém exatamente suas oito colunas anteriores.

## Configuração

```json
"BookCovers": {
  "Enabled": false,
  "ContactEmail": ""
}
```

`Enabled` também controla a leitura da nova tabela: com `false`, listagem e detalhes
continuam funcionando no esquema anterior. A busca exige tanto a habilitação quanto
um contato válido. Sem contato, a tela informa a pendência e o cliente não faz chamadas.
Nenhum e-mail pessoal ou chave foi incluído. A API pública de pesquisa não exige chave.

Informe seu contato real por variável de ambiente local `BookCovers__ContactEmail`,
fora do Git. Ele será enviado à Open Library nos cabeçalhos `User-Agent` e `From`.
O serviço solicita registro do aplicativo: esse registro NÃO foi realizado; avalie
as diretrizes e autorize separadamente qualquer envio do formulário externo.

## Preparar um novo banco de demonstração

Pare qualquer execução anterior. Os comandos abaixo são um procedimento opcional para
um **banco novo de demonstração**, não para atualizar bancos com dados existentes.

1. Na raiz do projeto, em um terminal PowerShell, escolha um nome isolado:

```powershell
$coverValidationName = 'MyCatalogDemo_' + [guid]::NewGuid().ToString('N')
$coverValidationConnection = "Server=(localdb)\MSSQLLocalDB;Database=$coverValidationName;Trusted_Connection=True;TrustServerCertificate=True"
$env:MYCATALOG_MIGRATION_CONNECTION = $coverValidationConnection
```

2. Com a ferramenta oficial `dotnet-ef` 10.0.14 disponível:

```powershell
dotnet ef database update --project MyCatalog.csproj
```

A factory de design exige a variável explícita e rejeita o nome do banco principal
configurado no projeto. Ela não inicia `Program.cs`, não lê a conexão padrão e não executa
o seed. Em um banco vazio, o comando aplica ambas as migrations: `InitialCreate` cria os
itens e `AddCatalogItemCover` acrescenta a tabela de associações, sem modificar itens existentes.
Não execute rollback (`database update 0`) nem `database drop`.

3. Na MESMA janela do terminal, aponte a aplicação para esse banco e habilite a função:

```powershell
$env:ConnectionStrings__DefaultConnection = $coverValidationConnection
$env:CatalogSeed__Enabled = 'true' # Somente para popular este novo banco de demonstração.
$env:BookCovers__Enabled = 'true'
# Defina BookCovers__ContactEmail com um contato real autorizado, fora de arquivos versionados.
dotnet run --project MyCatalog.csproj --launch-profile https
```

O perfil `https` usa Development. O seed fica desativado por padrão e só prepara os 25 exemplos
quando `CatalogSeed__Enabled=true`; confira a conexão isolada antes de habilitá-lo. Após
popular o novo banco, encerre a aplicação e remova essa variável antes das próximas execuções
(`Remove-Item Env:\CatalogSeed__Enabled`). Não a habilite para o banco principal nem para
os bancos de teste existentes. Sem contato, é possível
validar o restante da aplicação e a mensagem de pendência; a pesquisa não será enviada.

4. Para testar com contato válido: abra um livro, pesquise, escolha uma edição e confirme.
   Reinicie usando a mesma conexão e confira a capa. Teste também troca, remoção da associação,
   cartões sem capa, Mostrar/Ocultar e exportação CSV.

5. Ao terminar, encerre a aplicação e feche essa janela do terminal para descartar as variáveis
   locais. Não copie configurações de teste ou contatos para `appsettings.json`.

No Visual Studio, use o terminal integrado para esse procedimento. Se preferir o F5,
configure um perfil LOCAL equivalente, com a conexão isolada e as variáveis acima;
não habilite a função no perfil padrão que ainda aponta para o banco principal.

O bloqueio da factory permanece intencionalmente, mesmo após a validação local no principal.
Atualizar qualquer banco existente exige um procedimento separado, com aprovação e revisão
do SQL; este guia não autoriza essa atualização. A aplicação não executa migrations automaticamente.

### Variáveis temporárias e seed

`CatalogSeed__Enabled` é falso por padrão e só é respeitado em Development. Use `true`
somente na primeira população de um banco de demonstração. Ao encerrar a aplicação,
remova a variável ou defina-a como `false`; nos bancos já preparados, mantenha o seed desativado.

Para pesquisar sem preencher dados novamente, na janela com a conexão de demonstração:

```powershell
$env:CatalogSeed__Enabled = 'false'
$env:BookCovers__Enabled = 'true'
$env:BookCovers__ContactEmail = Read-Host 'Contato válido para identificação na Open Library'
```

Não use os escopos User/Machine para esses exemplos. Ao terminar a sessão, feche a
aplicação e o terminal, ou remova as variáveis de conexão, migration, capas e seed.
No Visual Studio, as variáveis devem ser herdadas antes de abrir a IDE; o terminal
integrado não altera o ambiente de uma IDE que já estava aberta.

## Persistência e segurança

`CatalogItemCover` é opcional e possui chave primária/estrangeira `CatalogItemId`,
provedor, identificadores da obra, edição e imagem e uma versão de concorrência.
Uma edição escolhida não muda quando o título é editado: a referência é ligada ao ID
do item. Mudar o tipo para outra categoria deixa de exibir a capa do livro, sem apagar
a associação. A exclusão normal de um item remove sua associação por chave estrangeira;
nunca exclui conteúdo da Open Library.

Os POSTs possuem antiforgery e tokens Data Protection com validade de 20 minutos,
vinculados ao item, título, autor, tipo e versão da capa. Tokens vencidos, adulterados ou
de outro item são rejeitados. Pesquisas antigas não sobrescrevem mudanças posteriores.
O servidor reconstrói URLs HTTPS a partir de identificadores válidos, sem aceitar URLs
arbitrárias do formulário. Respostas externas são limitadas a 1 MiB e renderizadas com
o escape normal do Razor. A integração continua restrita ao cenário local atual, sem
autenticação; controle de acesso deve ser preparado antes de uma publicação multiusuário.

## Disponibilidade, limites e direitos

- Cliente tipado `HttpClient`, timeout de 8 segundos por tentativa, cancelamento da requisição,
  redirects desativados e no máximo uma repetição para falhas de rede/5xx.
- Limitador compartilhado por processo: uma chamada por segundo, incluindo repetições.
  `429` respeita `Retry-After`, sem repetir imediatamente; pausas longas retornam uma mensagem.
- Cache de até 100 páginas: resultados por 30 minutos, páginas sem imagens/resultados por
  5 minutos; falhas não são armazenadas. Não há buscas automáticas na listagem ou no seed.
- A pesquisa de obras tem limite de 100 páginas. A seleção carrega até 10 páginas de edições por obra (100 registros), sem varrer o catálogo inteiro automaticamente. O link da obra na fonte permite consultar outras edições.
  O controle de tráfego é por instância; várias instâncias exigiriam coordenação adicional.
- Imagens vêm diretamente de `covers.openlibrary.org` por Cover ID; `default=false` permite
  fallback para imagens ausentes. Miniaturas usam M; a seleção salva é exibida em L.
- O navegador envia requisições de imagem à Open Library; `no-referrer` evita enviar a URL
  local como referenciador. A associação persiste, mas a imagem depende do serviço externo.
- Créditos e link da edição são preservados. A aplicação exibe as capas remotamente, sem manter cópias locais como arquivos de conteúdo. As capturas de tela da documentação mostram a interface real e preservam os créditos exibidos.
  As diretrizes de exibição não são uma licença irrestrita para redistribuição de cada arte.

Referências da integração; consulte suas condições antes de novos usos ou distribuição:

- [APIs](https://openlibrary.org/developers/api)
- [Pesquisa](https://openlibrary.org/dev/docs/api/search)
- [Capas](https://openlibrary.org/dev/docs/api/covers)
- [Licenciamento](https://openlibrary.org/developers/licensing)

## Testes e verificação da migration

```powershell
dotnet build
dotnet test
```

Na última validação registrada, 118 testes passaram sem falhas ou ignorados, com build
sem avisos ou erros. A suíte usa HTTP simulado, Data Protection
efêmero e SQLite em memória: não dependem da internet nem da conexão LocalDB principal.
Cobrem identificação ausente, respostas e erros da API, cache, limites, cancelamento,
validação dos identificadores, seleção, concorrência, preservação de dados e CSV.

A migration foi ensaiada em uma cópia isolada e posteriormente aplicada ao principal
durante a validação local. Foram conferidos histórico, estrutura, chaves, índices e
integridade, além da comparação integral dos 25 itens antes e depois. A suíte automatizada
não substitui essa verificação específica do SQL Server nem cobre toda a interface MVC.
