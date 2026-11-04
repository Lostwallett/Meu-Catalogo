# Experimento de capas

Base visual aprovada preservada no commit `40d71e2` da branch `main`.
O trabalho de capas está na branch `experimento/capas-catalogo`, preparado para revisão
e integração à `main`. A integração da branch não aplica migrations nem altera os bancos.

## Evolução para capas de livros persistentes

As duas associações fixas de livros da experiência inicial foram removidas.
Agora o usuário pode pesquisar obras e edições na Open Library e confirmar a capa.
A referência é salva em uma tabela opcional, sem atualizar os campos do item.
A funcionalidade está desabilitada por padrão, aguardando configuração de um banco
isolado e de um contato válido. A migration não foi aplicada ao banco principal.

Procedimento, segurança, limites e fontes em [Capas pela Open Library](docs/CAPAS-OPEN-LIBRARY.md).
As diretrizes da fonte orientam exibir diretamente de `covers.openlibrary.org`, com
link da edição. As imagens remotas não são baixadas ou redistribuídas no Git.

## Ilustrações locais preservadas

Os seis SVGs em `wwwroot/images/covers-demo/` foram desenhados do zero para a experiência,
com formas geométricas e tipografia. Não reproduzem cartazes, capas oficiais, personagens,
logotipos ou capturas dos produtos e são identificados como não oficiais na interface.

As artes gráficas originais desses arquivos são disponibilizadas sob CC0 1.0
(https://creativecommons.org/publicdomain/zero/1.0/). Os títulos identificam as obras;
isso não concede direitos sobre marcas, personagens ou material oficial.

Itens: A Viagem de Chihiro, Interestelar, Dark, Stranger Things, Stardew Valley e Minecraft.
Os demais itens usam uma composição editorial demonstrativa quando não têm capa selecionada.
Nenhuma API de filmes, séries ou jogos foi integrada.

## Como comparar

Abra **Meu catálogo > Ver cartões** e alterne **Ocultar capas / Mostrar capas**.
A preferência fica no navegador, e a tabela continua sem imagens.
O layout mantém proporção vertical 2:3 e alinhamento entre cartões.

Antes de trocar para `main`, preserve o trabalho pendente, inclusive arquivos não rastreados.
Não descarte as alterações nem espere que uma simples troca de branch as isole automaticamente.
