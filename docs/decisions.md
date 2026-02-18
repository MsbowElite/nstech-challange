Primeiramente considerando o tamanho do projeto e o nível de exigência, inicar utilizando github copilot agilizar muito a construção do 0 de uma solução atendendo todos os detalhes.

A criação de aquivos auxiliares para a IA em .github/instructions/main.md ajuda na tomada de decisões focando melhor o prompt e garantindo a consideração de todo o workspace se necessário, o próprio arquivo README.md serviu como uma grande referência, mas customização e o aumento do foco via prompt garante maior qualidade de saída de dados, mais alinhado com as expectativas, reduzindo correções e codificação manual.

Minimal API foi utilizado pelo tamanho do projeto e nenhum recurso especifico de controllers será utilizado.
O NUnit foi adotado visando o alinhamento com os padrões definidos pelo time e a viabilidade de um possível trabalho futuro na empresa.

#Phase2

Preparo para debug funcionar com dock compose
Configuração do MCP para tornar o agente do VS Code mais capacitado em suas funcionalidades e suporte ao projeto

#Phase3

Ambiente mais estável, adicionado fluent validations mas sem Result Pattern, ocorrendo um certo excesso de throws mas tornando o desenvolvimento mais fácil e rápido, deve ser analisado com time necessida de aplicação de outro padrão.

Correções de no fluxo de salvar dados, utilizando tracking aproveitando os changes do domínio, e em caso que query simples, para melhor performance segue sem tracking. Poderia ser 2 contextos diferentes, mas pela falta de complexidade e outras alterações no fluxo, pode ser considerado este mais simplificado.

#Phase4

Operação idempotente evita anomalias de concorrêcia de operação, e a forma aplicada pela IA tem riscos e gerar muitos dados desnecessários, assim preferi optar por Optimistic Concurrency Control (OCC), muito usado em casos junto com domínio com state pattern, sendo que a troca de status só é feita se o UpdateAt é o mesmo do obtido na consulta, se for diferente repete todo o processo. Nesse caso não é tão normal acontecer mais de 1 pedido de confirmação do pedido, e se acontecer vai gerar uma exceção, em casos que podem acontecer muitas exceções, pode ser aplicar a forma pessimista.

#Phase5 (Planejamento)

Considerando o contexto scoped por default, os comandos com 2 saveschanges basicamente o segundo está sendo desnecessário, pode na prática está sendo feito o pedido de save changes duplicado na mesma instância de contexto
Separar responsabilidades de Repository
Possibilidade de criar 2 contexto de banco um para leitura e outro para escrita
Aumentar a cobertura de testes
Simular fluxo com teste de integração com framework de containers
Remover auto migrations em prod para melhorar escalabilidade reduzindo o custo do cold start (necessário migrations manual quando necessário)
