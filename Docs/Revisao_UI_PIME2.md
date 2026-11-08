# Revisão do PIME 2 e da interface de Carbono Vivo

Data: 8 de novembro de 2026.

Documento analisado: `PIME 2.pdf`, 21 páginas, fornecido pelo usuário. As páginas abaixo se referem à ordem física do PDF. Esta revisão relaciona a proposta acadêmica à implementação e ao experimento visual da `GameScene_Test`. O PDF original permanece preservado. Dados institucionais, referências bibliográficas e estatísticas ambientais não foram verificados externamente nesta revisão.

## Parecer

A paleta de verde floresta e creme, os painéis arredondados e a organização mais legível das cartas combinam com a proposta de restauração e educação ambiental. O principal objetivo da interface deve ser tornar as relações ecológicas compreensíveis: o jogador precisa perceber o que cada decisão altera e por quê. A repaginação visual é uma etapa desse trabalho, e ainda não comprova os resultados pedagógicos previstos no documento.

O layout aprovado para esta experiência prioriza PC em paisagem. Essa decisão precisa aparecer como recorte de prototipagem, pois o PDF descreve a origem mobile e prevê acesso por celulares básicos e computadores escolares.

## Diferenças que precisam ficar explícitas

| Referência no PDF | Proposta do documento | Estado observado no projeto | Revisão sugerida |
| --- | --- | --- | --- |
| 1.1, p. 3; justificativa, p. 12 | Origem mobile e acesso em celulares e computadores | O experimento de UI atual prioriza PC em paisagem | Registrar que a validação inicial será em PC; manter adaptação mobile como etapa posterior, sujeita a testes. |
| 2.1, pp. 7–8 | Seis espécies e cartas ilustradas com referências institucionais | Cinco cards representam seis espécies; gralha e mico compartilham um desbloqueio. O catálogo atual é textual | Explicar o agrupamento. Tratar ilustrações e acervo autorizado como entrega pendente. |
| 2.2, p. 8; 4.1, p. 13 | Três barras: mata ciliar, água/solo urbano e clima/carbono | Quatro indicadores: floresta, água, comunidade e recursos, além do saldo de carbono | Atualizar a descrição da implementação atual ou planejar uma futura migração. Renomear painéis não cria uma simulação de permeabilidade ou clima. |
| 2.3, p. 8 | Vinte dilemas que afetam as três barras propostas | Vinte assets de dilemas alteram quatro indicadores | Atualizar a referência às barras e revisar cada consequência na UI. |
| 2.1, p. 7 | Tamanduá atua no controle de pragas das mudas | O efeito atual inclui chance de afastar lenhadores | Registrar como abstração de gameplay; o documento e a descrição de efeito devem distinguir função ecológica e regra do jogo. |
| 2.4, pp. 8–9; 4.1, p. 13 | Gravações bioacústicas reais de espécies identificáveis | O gerenciador examinado gera áudio procedural | Manter a curadoria e integração de gravações reais como pendência; não apresentar sons sintetizados como identificação bioacústica validada. |
| Justificativa, p. 12; recursos, p. 15 | Leveza tecnológica e uso em dispositivos básicos | Cenário 3D com UI 2D; desempenho ainda requer medição nos dispositivos-alvo | Descrever a arquitetura com precisão e condicionar alegações de desempenho a testes. |
| 2.5, p. 9 | Playtesting com jovens e guia pedagógico | Foram feitos testes técnicos locais da interface | Separar testes funcionais, avaliação com público-alvo e validação pedagógica. |

## Texto sugerido para incorporar ao documento

### Complemento à Ação 1 — interface do catálogo

O catálogo de biodiversidade utilizará uma linguagem visual orgânica, com verde floresta, superfícies em creme e cantos arredondados. Cada carta deverá separar nome da espécie, função ecológica, efeito no sistema e custo de desbloqueio. Na implementação experimental, as seis espécies estão organizadas em cinco cartas, pois Gralha-azul e Mico-leão-dourado compartilham o desbloqueio de dispersão de sementes. A organização será avaliada quanto à clareza de leitura e à compreensão dos efeitos pelo público-alvo.

### Complemento à Ação 2 — estado atual do painel

O protótipo atual apresenta quatro indicadores — floresta, água, comunidade e recursos — e um contador de carbono. A interface experimental reorganiza esses dados para melhorar a leitura durante a partida. A proposta de um painel Floresta–Água–Cidade permanece como orientação pedagógica para a evolução do sistema. Sua implementação completa exigirá definir os indicadores, as relações matemáticas e os sinais visuais que permitirão identificar as consequências urbanas de cada decisão.

### Complemento à metodologia — prototipagem de UI

A revisão visual será desenvolvida inicialmente na cena isolada GameScene_Test, com prioridade para PC em paisagem e uso dos componentes do Procedural UI já presentes no projeto. O experimento abrange menu, instruções, créditos, HUD, tutorial, dilemas, biodiversidade, pausa e resultados. A integração à cena principal dependerá da revisão visual e dos testes de navegação. A adaptação para celulares deverá considerar legibilidade, áreas de toque, proporções de tela e desempenho em dispositivos representativos do público-alvo.

### Complemento à Ação 5 — critérios de teste

Os testes técnicos devem verificar se os botões recebem cliques em sua área visível, se telas ocultas deixam de bloquear a interação, se o catálogo permite rolar e desbloquear espécies e se reiniciar cria uma nova partida. As instruções abertas durante a pausa devem permitir retornar à partida preservando seu estado. Em etapa distinta, o playtesting com o público-alvo deverá investigar a compreensão das relações entre floresta, água e comunidade e a clareza das consequências dos dilemas.

## Ajustes de conteúdo e prioridades de UI

1. **Unidades:** padronizar o significado do recurso. O código o exibe em ppm e também o utiliza como saldo de compra; textos antigos falam em toneladas. A interface não deve apresentar esse saldo abstrato como uma medição física validada.
2. **Efeitos das espécies:** conferir textos contra as fórmulas. Por exemplo, a descrição da Araucária fala em dobrar o sequestro, enquanto o multiplicador atual adiciona 0,5 ao fator geral.
3. **Dilemas:** melhorar o retorno após a escolha, indicando quais indicadores mudaram e a justificativa ecológica. O aviso genérico atual não explica a relação de causa e efeito.
4. **Interdependência:** planejar sinais legíveis de como floresta e água se relacionam. O HUD atual exibe valores separados; a conexão pedagógica ainda pode ficar mais clara.
5. **Resultados:** distinguir a meta numérica de vitória do estado dos indicadores. Alcançar o saldo-alvo não exige, atualmente, recuperar todos os indicadores ao máximo.
6. **Acessibilidade:** avaliar contraste, tamanho de fonte, uso de texto além da cor e leitura das cartas com os jogadores. A aprovação estética não substitui essa avaliação.

## Correções funcionais compartilhadas

As regras de pausa e navegação foram incorporadas ao `StepController`: simulação pausada no menu e nos diálogos, retorno contextual das instruções, reinício por caminho e nova partida ao sair dos resultados. `OnboardingManager` e `QuestionManager` controlam seus próprios diálogos com fades em tempo não escalado; os controles de mundo respeitam o estado de pausa. O `MobileCameraController` original concentra o arrasto no plano do terreno e os limites de zoom, sem um segundo controlador experimental.

O componente `GameSceneTestUITheme` permanece restrito à aparência da cena de teste, incluindo paleta, superfícies arredondadas e cards gerados. O visual experimental não foi copiado para a GameScene. Os ajustes salvos na cena principal se limitam à configuração da câmera.

Os testes locais por ponteiro devem ser registrados separadamente da aplicação de questionários e da validação pedagógica previstas no PIME.

### Verificação técnica desta revisão

- Raycasts em nove pontos por botão: centro e posições próximas às bordas. Passaram os botões do menu, HUD, pausa, fechamento do catálogo e três botões de espécies inteiramente visíveis no catálogo.
- Clique simulado próximo ao canto de Plantar Muda: uma árvore adicionada e custo de 10 descontado.
- Clique simulado próximo ao canto do desbloqueio do Guapuruvu: espécie ativada e custo de 25 descontado.
- Instruções abertas pela pausa: retorno à partida, com retomada do tempo.
- Reinício pela pausa: nova partida na GameScene_Test, sem conservar desbloqueios da tentativa anterior.
- A simulação permanece pausada enquanto o menu inicial está aberto.

Essas verificações cobrem interação técnica local. Os testes com jovens, a avaliação de acessibilidade e a validação das relações pedagógicas continuam sendo etapas distintas.

Após a consolidação, os fluxos de menu, tutorial, decisões, bloqueio de plantio, instruções contextuais e reinício também foram verificados na GameScene com os scripts principais. A cena de teste manteve sua aparência e passou a usar esses mesmos scripts. Ambas as cenas ficaram com um único MobileCameraController e sem referências de scripts ausentes. O controlador experimental de câmera foi removido.
