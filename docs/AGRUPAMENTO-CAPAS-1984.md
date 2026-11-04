# Investigação de registros repetidos: 1984

Metadados consultados em **04/11/2026**, por uma única requisição de leitura à fonte
pública indicada pelo usuário. Nenhuma imagem foi baixada ou comparada visualmente.

Fonte: https://openlibrary.org/works/OL24143668W/editions.json?limit=10&offset=0

Obra: https://openlibrary.org/works/OL24143668W/1984

## Registros encontrados

A resposta indicou três edições:

| ID da edição | ID da capa | ISBN-10 | ISBN-13 | Editora | Publicação | Idioma informado |
| --- | --- | --- | --- | --- | --- | --- |
| OL47123394M | 13509851 | 8467958820 | 9788467958829 | NORMA EDITORIAL, S.A. | Nov 25, 2022 | Não informado |
| OL31851689M | 10541664 | 2377315119 | 9782377315116 | SARBACANE | 16/02/2021 | Francês |
| OL31851688M | 10541663 | 2377315119 | 9782377315116 | SARBACANE | Não informado | Não informado |

O agrupamento anterior utilizava somente o ID da imagem, por isso retornava três cartões.
Os dois registros da Sarbacane têm IDs de edição e de imagem diferentes, mas compartilham
ISBNs válidos e editora. Os campos ausentes no segundo não contradizem o primeiro.

## Resultado e critérios

Com esses metadados, os três cartões passam a **duas opções agrupadas**. Os dois cadastros
da Sarbacane são reunidos, preservando suas duas imagens, campos individuais, links e tokens
de confirmação. O registro da Norma permanece separado: seus identificadores bibliográficos
e editora são diferentes. A aparência relatada não é usada para concluir que seja a mesma capa.

O agrupamento considera, dentro da mesma obra:

1. Mesmo ID de capa, ou mesmo ID de edição; várias imagens da mesma edição ficam no grupo.
2. ISBN válido em comum, com editora conhecida e correspondente, sem contradições conhecidas
   de publicação e idioma. ISBN-10 e ISBN-13 equivalentes são normalizados e seus dígitos
   verificadores são conferidos. Ambos os tipos de ISBN retornados pela API são preservados.
3. O título só participa da ordenação; nunca é evidência suficiente para agrupar imagens.

Um registro incompleto não pode unir por ISBN dois grupos cujos metadados conhecidos conflitam.
Somente cópias completas de metadados são suprimidas; variantes de informações e referências
de imagem continuam consultáveis. A prévia de cada imagem alternativa é carregada ao abrir
o grupo, e sua confirmação só é habilitada quando a respectiva imagem carrega.

## Limitações

Não há reconhecimento de imagem, hash visual ou serviço adicional. Imagens visualmente
iguais com IDs e ISBNs diferentes podem permanecer em grupos separados. Dados inconsistentes,
editoras ausentes ou formas de publicação conflitantes também podem impedir agrupamentos.
Uma edição ou ISBN pode ter mais de uma arte: agrupar registros relacionados não declara
que suas imagens sejam idênticas. Por isso a interface usa “opções agrupadas”, não “capas distintas”.

O teste `CoverEditionMetadataGroupingTests` reproduz essa resposta usando HTTP simulado,
além de testar variantes, ISBNs inválidos, conflitos, transitividade e preservação de seleção.
Nenhum banco de dados foi acessado ou modificado nesta investigação.
