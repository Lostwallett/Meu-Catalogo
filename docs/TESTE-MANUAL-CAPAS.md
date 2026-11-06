# Ambiente isolado para testar capas no Visual Studio

## Ambiente já preparado

- Branch: `experimento/capas-catalogo`.
- Servidor: `(localdb)\MSSQLLocalDB`.
- Banco exclusivo: **`MyCatalogManualTest_BookCovers_2d8c6215`**.
- Migrations aplicadas somente nesse banco: `InitialCreate` e `AddCatalogItemCover`.
- Na preparação inicial, foram criados 25 exemplos no banco de teste, sem associações de capa.
  Nos testes manuais posteriores, o usuário confirmou uma associação para 1984 e sua persistência.
- Integração desativada inicialmente; nenhum contato configurado.
- `MyCatalogCompletoDb` não foi aberto ou alterado durante a preparação.

O perfil normal `https` conserva os mesmos valores de configuração anteriores,
incluindo suas portas. Não é necessário executar migrations para usar este ambiente
de teste: ele já está pronto nesta máquina.

## Executar agora no Visual Studio

1. Abra `MyCatalog.slnx` e defina **MyCatalog** como projeto de inicialização.
2. Pare qualquer execução anterior do projeto.
3. No seletor ao lado do botão verde de iniciar, escolha **teste-capas-isolado**.
   Se os novos perfis não aparecerem, feche e reabra a solução.
4. Pressione **F5** ou **Ctrl+F5**.
5. Acesse `https://localhost:7257` e abra **Meu catálogo**.

Esse perfil fixa as variáveis do processo:

```text
ConnectionStrings__DefaultConnection = conexão para MyCatalogManualTest_BookCovers_2d8c6215
ManualTest__ExpectedDatabase = MyCatalogManualTest_BookCovers_2d8c6215
BookCovers__Enabled = false
BookCovers__ContactEmail = vazio
```

Porta HTTP alternativa: `http://localhost:5257`; o middleware pode redirecionar para HTTPS.
Nenhum perfil de teste usa as portas 7157/5157 do perfil normal.

## Confirmar o banco usado

Na janela **Saída**, selecione a saída da aplicação/ASP.NET Core (ou consulte o console
aberto por Ctrl+F5). A inicialização deve registrar:

```text
TESTE ISOLADO | Servidor: (localdb)\MSSQLLocalDB | Banco: MyCatalogManualTest_BookCovers_2d8c6215 | Capas habilitadas: False
```

O nome é validado antes de abrir conexões ou executar o seed. Se a configuração do
perfil apontar para outro banco ou servidor, a aplicação para com uma mensagem de erro.
Não remova `ManualTest__ExpectedDatabase` dos perfis de teste.

Para uma confirmação diretamente no SQL Server:

1. Abra **Exibir > SQL Server Object Explorer** no Visual Studio.
2. Conecte a `(localdb)\MSSQLLocalDB` e expanda **Databases**.
3. Localize **MyCatalogManualTest_BookCovers_2d8c6215** e abra uma nova consulta nele.
4. Execute somente a consulta de leitura abaixo:

```sql
IF DB_NAME() <> N'MyCatalogManualTest_BookCovers_2d8c6215'
    THROW 51000, 'Selecione o banco isolado antes de continuar.', 1;

SELECT DB_NAME() AS BancoAtual;
SELECT COUNT(*) AS TotalItens FROM dbo.CatalogItems;
SELECT COUNT(*) AS TotalCapas FROM dbo.CatalogItemCovers;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
```

Resultado inicial esperado: nome do banco de teste, 25 itens, 0 capas e as duas migrations.
Alterações manuais futuras nesse banco podem mudar essas contagens sem afetar o principal.

## Configurar a busca real posteriormente — somente após autorização

Não foi configurado um contato e não foi feita busca real na preparação.
O segundo perfil, **teste-capas-isolado-variaveis**, utiliza o MESMO banco isolado,
mas herda `BookCovers__Enabled` e `BookCovers__ContactEmail` do processo do Visual Studio.
Sem variáveis locais, usa os padrões desativado/vazio de `appsettings.json`.

O perfil **teste-capas-isolado** sempre força a desativação e contato vazio, mesmo que
existam variáveis herdadas. Essa separação evita que o perfil inicial seja ativado por engano.

Após fornecer um contato válido e autorizar o uso externo:

1. Encerre a execução e feche todas as janelas do Visual Studio.
2. Abra um PowerShell separado pelo Windows, fora do Visual Studio.
3. Execute os comandos abaixo. `Read-Host` recebe o contato sem colocá-lo no texto do
   comando ou em qualquer arquivo versionado; o valor fica na memória do processo.

```powershell
$env:BookCovers__Enabled = 'true'
$env:BookCovers__ContactEmail = Read-Host 'Contato válido autorizado para enviar à Open Library'

$coverVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$coverStudio = & $coverVswhere -latest -products '*' -property productPath
if (-not $coverStudio) { throw 'Visual Studio não encontrado.' }
& $coverStudio 'D:\Projetos\MyCatalogCompleto\MyCatalog.slnx'
```

4. Na nova instância do Visual Studio, selecione **teste-capas-isolado-variaveis** e use F5.
5. Confira a linha `TESTE ISOLADO`: mesmo banco, agora com capas habilitadas.
6. Abra um Livro > Detalhes > **Escolher ou trocar capa**. Confira a obra, as edições
   e a imagem antes de confirmar. A consulta só é enviada ao clicar em pesquisar ou
   consultar edições; nenhum preenchimento em lote é executado.

O contato será transmitido à Open Library nos cabeçalhos de identificação das chamadas.
Não o copie para `launchSettings.json`, `appsettings.json`, comandos literais ou documentos.
Não use `SetEnvironmentVariable` no escopo User/Machine para esta experiência.

Não selecione o perfil normal `https` na instância que herdou a habilitação da API.
Ao terminar, feche essa instância do Visual Studio e o PowerShell para descartar as
variáveis temporárias. A próxima abertura normal pelo menu do Windows não herdará
as variáveis dessa janela. Para retornar ao teste sem API, use **teste-capas-isolado**.

## Proteção do seed

Os perfis isolados mantêm suas conexões e configurações de capas. O seed agora fica
desativado por padrão, inclusive em Development; não é necessário habilitá-lo para
usar os 25 itens já existentes ou a capa salva. Ele só executa com Development e
`CatalogSeed__Enabled=true`. Use essa ativação apenas ao preparar um novo banco de
demonstração, conforme o README; não a defina globalmente nem para o banco principal.

## O que foi verificado

- O banco de teste não existia antes da preparação.
- A migration aditiva preservou todos os campos dos 25 exemplos criados nele.
- `DB_NAME()` confirmou o destino isolado.
- Reexecutar o seed inseriu 0 registros e reconheceu os 25 existentes.
- O perfil normal foi comparado com o Git e mantém exatamente os mesmos valores.
- A inicialização real do novo perfil registrou o banco correto e API desativada.
- Build sem avisos/erros; 66 testes automatizados passaram, sem falhas ou ignorados.

Durante essa preparação inicial, nenhuma migration foi aplicada ao principal e não foram
feitas buscas reais, registros externos do aplicativo, commits ou push. O banco de teste
foi mantido para a revisão manual. A busca real foi validada posteriormente pelo usuário;
seus dados e associações não fazem parte do versionamento Git.
