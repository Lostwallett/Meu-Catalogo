# Validação manual de capas no Visual Studio

A integração está na `main`. Este roteiro descreve testes manuais em um banco de demonstração,
sem pressupor um ambiente já preparado em outro computador. Não é necessário executar
migrations novamente quando o banco escolhido já contém o esquema esperado.

## Preparação e escolha do perfil

1. Prepare um banco exclusivo de demonstração conforme o [guia da integração](CAPAS-OPEN-LIBRARY.md).
2. Abra `MyCatalog.slnx` e selecione **MyCatalog** como projeto de inicialização.
3. Confira o servidor, o banco e as variáveis do perfil antes de executar. Mantenha
   `CatalogSeed__Enabled=false` para preservar o conjunto já preparado.

Os perfis versionados têm finalidades diferentes:

| Perfil | Comportamento |
| --- | --- |
| `https` | Usa a conexão padrão, salvo sobrescrita por configuração externa; não possui a guarda de banco isolado |
| `teste-capas-isolado` | Conexão isolada e nome esperado explícitos; força capas desativadas e contato vazio |
| `teste-capas-isolado-variaveis` | Mesma conexão isolada; permite herdar habilitação e contato do ambiente da IDE |

Os nomes de banco dos perfis isolados são exemplos de configuração, não bancos distribuídos
com o Git. Em outra instalação, ajuste a configuração local dos dois perfis para o banco de
demonstração preparado. `ConnectionStrings__DefaultConnection` e `ManualTest__ExpectedDatabase`
devem indicar exatamente o mesmo banco, cujo nome precisa começar por `MyCatalogManualTest_`.
A guarda exige também o servidor `(localdb)\MSSQLLocalDB`. Use esse prefixo ao preparar
um banco destinado aos perfis isolados, em vez do prefixo genérico do outro guia.

Não remova `ManualTest__ExpectedDatabase` nem versione contatos ou configurações privadas.
A guarda valida a conexão antes da inicialização. Ela não cria o esquema nem impede
alterações intencionais de CRUD no banco de teste durante o uso.

## Primeiro teste: integração desativada

1. Pare qualquer execução anterior e selecione **teste-capas-isolado**.
2. Pressione F5 e abra `https://localhost:7257`.
3. Na saída da aplicação, confira a linha `TESTE ISOLADO`: servidor e banco devem
   corresponder ao ambiente de demonstração e as capas devem estar desativadas.
4. Confira a mensagem `Dados demonstrativos desativados`.
5. Abra a listagem e os detalhes; confirme a apresentação substituta e o funcionamento
   das visualizações em tabela/cartões, filtros e exportação.

Os perfis isolados usam HTTPS 7257 e HTTP 5257. O perfil normal usa HTTPS 7157 e HTTP 5157.
Não suponha que o perfil normal aponta para o banco de demonstração.

## Segundo teste: pesquisa real

Configure um contato válido somente no ambiente do processo que iniciará a IDE, fora
dos arquivos versionados. O valor será enviado à Open Library nos cabeçalhos de identificação.
Não use um contato fictício nos testes reais nem grave a configuração nos escopos User/Machine.

Com o Visual Studio fechado, em um PowerShell iniciado na raiz do projeto:

```powershell
$env:CatalogSeed__Enabled = 'false'
$env:BookCovers__Enabled = 'true'
$env:BookCovers__ContactEmail = Read-Host 'Contato válido para identificação na Open Library'

$visualStudioLocator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$visualStudioPath = & $visualStudioLocator -latest -products '*' -property productPath
if (-not $visualStudioPath) { throw 'Visual Studio não encontrado.' }
& $visualStudioPath (Join-Path (Get-Location) 'MyCatalog.slnx')
```

1. Na nova instância da IDE, selecione **teste-capas-isolado-variaveis**.
2. Execute e confira novamente o banco isolado na saída, agora com capas habilitadas
   e seed desativado. Se a IDE reutilizar um processo anterior sem as variáveis,
   encerre-o antes de repetir; não altere os padrões versionados para contornar isso.
3. Abra um Livro > **Detalhes** > **Escolher ou trocar capa**.
4. Pesquise por título e autor. Selecione a obra, confira idioma, editora, publicação e ISBN.
5. Expanda **Consultar edições e imagens** para conferir os metadados individuais dos grupos.
6. Após carregar a prévia correta, confirme **Usar esta capa**. Essa ação grava a associação
   somente no banco de demonstração escolhido.
7. Reinicie com a mesma conexão e configuração. Confira a capa em detalhes e cartões.
8. Se desejar testar troca e remoção da associação, faça isso apenas em itens de demonstração.
   A remoção da capa não exclui o livro. Confira também Mostrar/Ocultar capas e os créditos.

As requisições de pesquisa são feitas por interação do usuário. Não há pesquisa automática
em lote. A ordenação e o agrupamento dependem dos metadados disponíveis; não constituem
verificação visual de identidade das imagens.

Ao terminar, feche a aplicação, a IDE e a janela de PowerShell para encerrar os processos
que herdaram as variáveis. Para voltar ao modo sem API, use **teste-capas-isolado**.

## Validação registrada e limites

A migration `AddCatalogItemCover` foi validada em ambiente isolado e aplicada ao banco
principal nos testes locais, preservando os 25 itens originais. A validação manual no
principal confirmou pesquisa, associação de capa a 1984 e persistência após reiniciar.
Esses dados e associações não acompanham o repositório e não devem ser recriados por este roteiro.

Na última validação automatizada registrada, 118 testes passaram. Eles usam SQLite em memória
e HTTP simulado; não substituem a verificação manual da interface ou do SQL Server.
Consulte [os detalhes dos testes](../tests/MyCatalog.Tests/README.md).