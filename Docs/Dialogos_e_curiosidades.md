# Diálogos e curiosidades da GameScene_Test

O `ForestStoryDirector`, iniciado pelo `InteractivePlanting` apenas na cena de teste,
exibe o Diário da Floresta com o robô e o padrão de painéis arredondados existente.

As mensagens automáticas aparecem uma vez por partida: primeiro plantio,
primeira decisão, primeiro incêndio, primeiro ataque de lenhador, recuperação
(saúde da floresta >= 7 e pelo menos cinco árvores) e desbloqueio de cada card.
Incêndios e ataques são comentados depois que todas as ameaças terminam,
independentemente de o jogador ter conseguido impedir as perdas.

O sistema aguarda o tutorial, decisões, pausa e fechamento do catálogo.
Há pelo menos 12 segundos de jogo entre mensagens automáticas e um segundo
de espera após o combate. Durante a leitura, o jogo pausa. O botão Continuar
só aceita interação depois de 0,35 s para evitar fechamento involuntário.

Cards desbloqueados oferecem o botão Curiosidades para reler a ficha sem
custo. A releitura pode abrir por cima do catálogo, preservando sua navegação,
e também aguarda ameaças e outros diálogos. Gralha-azul e mico-leão-dourado
têm textos individuais na mesma ficha, acompanhando o desbloqueio conjunto.

## Conteúdo e referências

O PIME 2, pp. 6–8, orienta a narrativa sobre floresta, água, comunidade e
interdependência. As falas do robô são textos originais escritos para o jogo.
As curiosidades não apresentam multiplicadores do jogo como medidas biológicas.

- Guapuruvu: classificação pioneira, [Programa Arboretum](https://www.programaarboretum.eco.br/especie/116/guapuruvu).
- Araucária e gralha-azul: pinhão como semente e alimento; armazenamento,
  [Paraná Turismo](https://www.paranaturismo.com.br/gralha-azul-do-parana/).
- Mico-leão-dourado: dispersão de sementes,
  [estudo disponível na USP](https://www.teses.usp.br/teses/disponiveis/41/41134/tde-30092004-094442/publico/Lapenta2.pdf).
- Besouro rola-bosta: matéria orgânica e enriquecimento do solo,
  [Proteção Animal de Curitiba](https://protecaoanimal.curitiba.pr.gov.br/animais-silvestres-curitiba/invertebrados/besouro-rola-bosta).
- Tamanduá-mirim: alimentação principalmente de formigas e cupins,
  [Instituto Água e Terra do Paraná](https://www.iat.pr.gov.br/Noticia/Tamandua-mirim-encontrado-em-Alto-Piquiri-e-devolvido-natureza-por-tecnicos-do-IAT).
  A ficha identifica afastar lenhadores como bônus do jogo.

As fontes são identificadas no rodapé de cada ficha; os links completos ficam
neste documento. Não foram adicionados percentuais de cobertura ou números de
espécies ameaçadas, que exigiriam recorte, data e atualização específicos.

## Verificação no Unity

- Scripts compilados sem erros.
- Primeiro plantio enfileirado uma única vez; abertura pausa e fechamento retoma.
- Proteção inicial do botão impede fechar a mensagem com o toque que a abriu.
- Decisões não abrem sobre o Diário da Floresta.
- Desbloqueio aguarda o catálogo fechar; Curiosidades permite releitura no catálogo,
  preservando sua abertura e o saldo do jogador.
- Ataque e incêndio adiam as mensagens, incluindo a espera após a ameaça terminar.
- Conteúdo das cinco fichas (seis espécies), títulos e fontes cabem nos painéis.
- Botão Continuar recebe raycast e clique, fecha o painel e retoma a simulação.
- Conferência visual em 1280 × 720; execução encerrada em modo de edição.
