# Balanceamento ecológico da GameScene_Test

Abra o painel por **F1**, **F12** ou crase. A aba **Ecologia** reúne economia,
impactos, árvores, espécies e habilidades. A busca filtra nomes e parâmetros,
incluindo termos sem acento. As abas Design, Visual e Ações continuam disponíveis.

Os sliders de configuração aplicam as mudanças e salvam no mesmo JSON/PlayerPrefs
do debug existente. Configurações antigas recebem os novos valores padrão sem
perder os ajustes anteriores. Resetar padrões também restaura a ecologia.
Os controles “Nível atual” são cheats da partida, sem custo; não salvam progresso.
Em Ações, “Recarregar habilidades” encerra os efeitos ativos e zera as recargas.

## Economia

Após 60 s de jogo, Recursos têm manutenção base de 5 p.p./min, mais 0,15 p.p./min
por peso de árvore e 0,5 p.p./min por drone ativo. Água consome 2 p.p./min mais
0,08 por peso de árvore; a recuperação máxima é de 8 p.p./min, proporcional à
saúde da floresta. Comunidade se recupera quando Água >= 60% e Recursos >= 35%;
sofre perdas com Água < 40% ou Recursos < 25%. Os limiares críticos são ajustáveis.

Ganhos passivos de Água e Comunidade diminuem acima de 70%, chegando a 25%
da recuperação no teto. O slider geral Dificuldade/Decaimento escala o sistema.
Há carência inicial ajustável e os indicadores podem chegar a zero por consumo.
Recursos não têm a regeneração automática anterior.

## Árvores

| Espécie | Captura adulta relativa | Custo relativo | Crescimento | Peso na manutenção |
| --- | ---: | ---: | ---: | ---: |
| Guapuruvu | 100% | 1× | 10 s | 1× |
| Embaúba | 125% | 1,5× | 15 s | 1,3× |
| Araucária | 160% | 3× | 35 s | 1,8× |
| Jequitibá-rosa | 220% | 5× | 60 s | 3× |

Guapuruvu está disponível para plantio desde o começo. As demais mudas são
liberadas no catálogo **Manejo da Floresta → Árvores**. O botão Selecionar muda
define a próxima espécie e fecha o painel. O custo base segue o slider de plantio e o acréscimo
por quantidade; cada espécie aplica seu multiplicador sobre esse custo.

A espécie é registrada no pedido do drone e permanece a mesma mesmo que a
seleção mude antes da entrega. Mudas só geram captura passiva quando adultas;
árvores em chamas deixam de contribuir. Todas as árvores, inclusive brotamentos,
entram na manutenção. Crescimento e produção consultam os ajustes atuais.

Três espécies adultas diferentes concedem +10% de captura global. A gralha pode
ampliar esse bônus. As espécies usam variantes do pack visual existente;
os modelos são representações do jogo, sem identificação botânica validada.
Os valores de captura também são regras de balanceamento, sem equivalência
com taxas biológicas reais.

O plantio exige distância mínima de 6 unidades no plano do terreno, ajustável
em **Debug → Ecologia → Distância mínima entre árvores**. Árvores novas e mudas
ocupam o espaço desde a criação, independentemente de sua escala de crescimento.
Drones reservam o destino no envio, impedindo pedidos repetidos ou brotamentos
sobre o mesmo ponto. A validação também considera construções e limites do terreno.
Um pedido recusado não cobra; falhas ou cancelamento do drone devolvem o custo
e liberam a reserva. O bônus de Floresta só é aplicado quando a muda é plantada.

## Espécies e evolução

A coleção tem oito cards: as quatro árvores, mico, gralha, besouro e tamanduá.
**Biodiversidade** reúne os quatro animais. O novo botão **Manejo da Floresta**,
ao lado de Plantar e Biodiversidade, abre um painel com as abas **Árvores** e
**Habilidades**, seguindo o mesmo estilo de cartões arredondados e rolagem.
Os cards de espécies oferecem desbloqueio, evolução até III e curiosidades para releitura.
Custos de desbloqueio e das duas evoluções são ajustáveis individualmente.

| Habilidade passiva | I | II | III |
| --- | ---: | ---: | ---: |
| Guapuruvu: captura da própria espécie | +10% | +15% | +20% |
| Embaúba e jequitibá: captura da própria espécie | +5% | +10% | +15% |
| Araucária: economia de Água | 10% | 20% | 30% |
| Besouro: economia de manutenção de Recursos | 5% | 10% | 15% |
| Gralha: captura adicional com diversidade | +5% | +10% | +15% |
| Tamanduá: chance de afastar invasões | 20% | 35% | 50% |
| Mico: redução do intervalo de brotamento | 0% | 20% | 35% |

O mico brota um guapuruvu a cada **16 / 12,8 / 10,4 s**, sem cobrar plantio.
O intervalo base e as três reduções são ajustáveis. O ganho de Floresta por
brotamento é 0,25 p.p., também ajustável. Esse ganho não substitui manutenção.
Os bônus passivos de produção, diversidade e economia respeitam um teto
configurável de 40% em cada categoria.

## Impactos e habilidades ativas

O baralho mantém os assets originais. Na cena de teste, seus valores absolutos
até 0,9 / 1,9 / acima de 1,9 são convertidos em impactos de **5 / 10 / 20 p.p.**,
mostrados como `+ / ++ / +++` e `- / -- / ---`. Os dois limiares e os três valores
são ajustáveis. A prévia mostra apenas os sinais e os nomes dos indicadores,
sem quantidades de pontos ou valores antes/depois ao passar o mouse.

| Habilidade | Efeito | Custo em Recursos | Recarga |
| --- | --- | ---: | ---: |
| Mutirão de manejo | +25% de captura por 20 s | 8 p.p. | 60 s |
| Recuperação hídrica | +15 p.p. de Água imediatamente | 10 p.p. | 75 s |
| Brigada de proteção | -40% dos danos de fogo por 20 s | 8 p.p. | 60 s |

A brigada também desacelera o avanço da queima enquanto está ativa. A redução
dos danos ambientais é aplicada quando uma árvore é perdida durante a proteção.
As habilidades não acumulam o mesmo efeito e usam tempo de simulação: diálogos
e pausa congelam duração e recarga. Custos não podem zerar Recursos. Efeitos
percentuais são consultados em tempo real; duração e recarga são registradas
na ativação. O controle de duração é usado por manejo e brigada; Água é instantânea.
As habilidades são ativadas na aba Habilidades do novo catálogo, que exibe
efeito, custo, duração e recarga atualizados a partir das configurações de debug.
Os dois catálogos se fecham mutuamente e as mensagens automáticas aguardam
o fechamento de ambos. Consultar o catálogo mantém a simulação rodando.

## Referências de conteúdo

As falas e fichas anteriores continuam documentadas em
[Diálogos e curiosidades](Dialogos_e_curiosidades.md). As novas fichas usam:

- [Embrapa: embaúba](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1125794/2/guia-forrageiras-digital.pdf).
- [ICMBio: jequitibá-rosa na Rebio Sooretama](https://www.gov.br/icmbio/pt-br/assuntos/unidade-de-conservacao/unidades-de-biomas/mata-atlantica/lista-de-ucs/rebio-de-sooretama/carrossel-inicial/biodiversidade).

A integração é iniciada apenas na GameScene_Test. As cenas que não possuem
EcologySimulation mantêm as regras anteriores.

## Verificações realizadas

- Compilação no Unity e migração de JSON antigo, preservando configurações existentes.
- Serialização e leitura das novas configurações; alteração por slider do debug
  comprovada no intervalo real do mico e no JSON salvo. Valores de teste restaurados.
- Mico I/II/III, limite de evolução e brotamento gratuito com identidade de árvore.
- Custos relativos, bloqueio de mudas, produção ponderada adulta e diversidade.
- Consumo de Recursos, recuperação reduzida e custos/recargas das habilidades.
- Habilidades bloqueadas na pausa e quando não há Recursos suficientes.
- Crescimento real da muda até produzir; entrega de embaúba por drone preservada
  após trocar a seleção de muda durante o voo.
- Conversão dos impactos aplicada à decisão de acordo com os níveis da prévia;
  apresentação somente com sinais e nomes dos indicadores.
- Busca do debug aceita termos sem acento; abas e catálogo conferidos visualmente
  em 1280 × 720.
- Nova partida limpa os níveis alterados pelos cheats e mantém os parâmetros
  salvos. Curiosidades das oito espécies e listas de impactos cabem nos painéis;
  o botão de selecionar uma muda bloqueada fica desabilitado.
- Novo botão Manejo recebe clique e abre um painel com quatro árvores. A
  Biodiversidade conserva quatro animais; abrir um catálogo fecha o outro.
- Desbloquear árvore e selecionar muda pelo Manejo funciona; selecionar fecha o
  painel. Curiosidades retornam ao catálogo e mensagens automáticas aguardam seu fechamento.
- Habilidade ativada pelo cartão aplica custo, efeito e recarga. Rolagem,
  recorte dos cartões e botão fechar foram verificados; o painel fechado não bloqueia cliques.

Essas verificações são técnicas. O tempo de sobrevivência e os números finais
devem ser calibrados em partidas com jogadores usando os controles do debug.
