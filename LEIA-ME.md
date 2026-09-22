# DishFace — tela inicial (JogoFeira2)

**Está pronto.** Abre o projeto no Unity, abre a cena `Assets/Scenes/MenuPrincipal.unity`
e aperta Play. Não precisa montar nada.

Plugue o controle **antes** de apertar Play.

---

## O que já está montado

```
MenuPrincipal.unity
├── Main Camera
├── EventSystem
└── Canvas                    ← o script MenuPrincipal está aqui
    ├── Fundo
    ├── Botao_Jogar
    ├── Botao_Opcoes
    ├── Botao_Controles
    └── Botao_Sair
```

- Canvas Scaler em `1366 x 768`, Match `0.5` (funciona em qualquer resolução)
- Os 4 botões posicionados dentro do pergaminho
- Os 8 sprites (apagado + aceso) já ligados no script
- Eixo `DPadVertical` configurado no `Project Settings > Input`
- A cena já está no `Build Settings`

## Como usar

| Comando | Ação |
|---|---|
| Analógico esquerdo ↑↓ | troca de botão |
| D-Pad ↑↓ | troca de botão |
| Setas / W S | troca de botão (teclado) |
| A (ou Enter) | confirma |
| B (ou Esc) | fecha painel aberto |

JOGAR já nasce aceso. Do SAIR pra baixo, volta pro JOGAR.

---

## O que ainda falta (quando o resto do jogo existir)

Clica no **Canvas** e preenche no Inspector:

- **Cena Do Jogo** — nome da cena da fase. Depois adiciona ela em `File > Build Settings`.
  Enquanto estiver vazio, o JOGAR só escreve um aviso no Console.
- **Painel Opcoes** / **Painel Controles** — arrasta os painéis quando criar.
  Enquanto estiverem vazios, esses botões avisam no Console.

O SAIR já funciona. No editor ele só escreve "Saindo do jogo..." no Console —
o `Application.Quit()` fecha de verdade só no jogo exportado. Isso é normal.

---

## Como o script funciona

Tudo gira em torno de uma variável:

```csharp
private int botaoSelecionado;   // 0=jogar  1=opcoes  2=controles  3=sair
```

Quando ela muda, o `AtualizarBotoes()` apaga os quatro e acende só o da vez:

```csharp
botaoJogar.sprite = jogarApagado;
botaoOpcoes.sprite = opcoesApagado;
botaoControles.sprite = controlesApagado;
botaoSair.sprite = sairApagado;

if (botaoSelecionado == 0) { botaoJogar.sprite = jogarAceso; }
...
```

Apagar todos antes de acender um garante que nunca fiquem dois acesos.

O `direcaoLiberada` faz o menu andar **um botão por vez**: só libera o próximo
passo depois que o analógico volta pro meio. Sem isso, o `Update()` roda 60 vezes
por segundo e o menu varreria os 4 botões num piscar de olho.

---

## Se quiser mexer na posição dos botões

Clica no botão na Hierarchy e mexe nos **Anchors** do Rect Transform.
Os valores atuais são:

| Botão | Anchor Min | Anchor Max |
|---|---|---|
| Botao_Jogar | X 0.295 · Y 0.551 | X 0.705 · Y 0.688 |
| Botao_Opcoes | X 0.295 · Y 0.421 | X 0.705 · Y 0.558 |
| Botao_Controles | X 0.295 · Y 0.291 | X 0.705 · Y 0.428 |
| Botao_Sair | X 0.295 · Y 0.160 | X 0.705 · Y 0.298 |

São **frações da tela**, não pixels: `0.295` quer dizer "começa a 29,5% da largura".
Por isso os botões acompanham o fundo quando a tela muda de tamanho, em vez de
sair de cima do pergaminho.

Para adicionar um 5º botão seria preciso mexer no script (ele tem os quatro
escritos na mão, um por um).

---

## Se precisar remontar do zero

Para o caso de a cena se perder, ou de você querer refazer pra entender:

1. `File > New Scene` → salva em `Assets/Scenes/`
2. `GameObject > UI > Image` → cria Canvas, EventSystem e a Image juntos
3. No **Canvas**, no Canvas Scaler: `Scale With Screen Size`, `1366 x 768`, Match `0.5`
4. Renomeia a Image pra **Fundo**, anchors em stretch/stretch (ALT+SHIFT no preset
   de baixo à direita), `Source Image` = `fundo`, desmarca `Raycast Target`
5. Mais 4 `GameObject > UI > Image` dentro do Canvas: `Botao_Jogar`, `Botao_Opcoes`,
   `Botao_Controles`, `Botao_Sair` — anchors da tabela acima, `Preserve Aspect` marcado,
   `Source Image` = sprite apagado de cada um
6. Clica no Canvas → `Add Component` → **Menu Principal**
7. Arrasta as 4 Images (da Hierarchy) e os 8 sprites (do Project) nos campos

> Atenção no último: o sprite aceso do SAIR chama **`sair_2`**, não `sair_selecionado`.

---

## Se algo der errado

| Problema | Causa provável |
|---|---|
| Botão nenhum acende | Algum campo do script ficou vazio |
| Botões esticados | `Preserve Aspect` desmarcado |
| D-Pad não navega | Falta o eixo `DPadVertical` em `Edit > Project Settings > Input` |
| Analógico varre tudo de uma vez | O `direcaoLiberada` não está sendo usado |
| Botões fora do pergaminho | Anchors errados — confere a tabela |
