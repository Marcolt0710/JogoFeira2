# Menu de fases — mapa das ilhas

Abra `Assets/MenuDeFase/menudefase.unity` e aperte **Play**
(ou comece pelo `MenuPrincipal`: o JOGAR abre o mapa).

Feito para **controle de PlayStation** (DualShock), e o teclado também funciona:

| Controle (PS) | Teclado | Ação |
|---|---|---|
| Analógico esquerdo ou D-Pad | WASD / setas | anda nas 8 direções (e escolhe o botão no menu principal) |
| **X** | Enter / espaço | menu principal: seleciona (JOGAR abre o mapa de fases) |
| **Círculo** | Esc | menu principal: fecha o painel aberto |
| **X** | Z / Enter | mapa: entra na casa (quando está na porta) e abre a tela de identificação |
| — | teclado físico | identificação: digita o nome e o celular ou e-mail |
| — | Tab | identificação: troca de campo |
| **X** | Enter | identificação: confirma e abre a fase (no campo do nome, o Enter passa para o próximo) |
| **Círculo** | Esc | identificação: volta ao mapa |
| **Círculo** | Esc / Enter | dentro da fase: volta ao mapa |

No Windows o Unity numera os botões do controle do PS4 assim:
`joystick button 0` = Quadrado, `1` = **X**, `2` = **Círculo**, `3` = Triângulo
([referência](https://ritchielozada.com/2016/11/21/playstation-4-dual-shock-controller-input-mapping-with-unity-on-windows-10/)).
Por isso os scripts usam `KeyCode.Joystick1Button1` (X) e `KeyCode.Joystick1Button2` (Círculo),
e no Input Manager o `Submit` usa `joystick button 1` e o `Cancel` usa `joystick button 2`.

Tudo segue o que é ensinado nas aulas de Desenvolvimento de Jogos do prof. Hélio
(helioesperidiao.com), no Unity 2017.4.40f1 do curso.

---

## O mapa

Inspirado no mapa-múndi do Cuphead: três ilhas no mar, com penhascos de terra no lado
sul, ligadas por pontes de madeira, e ilhotas em volta.

| Ilha | Fase | Casa | O que tem em volta |
|---|---|---|---|
| Oeste — a vila | **1** | casinha vermelha | praça com fonte, poço, bancos, lampiões, outra casa |
| Centro — a fazenda | **2** | ferreiro | forja acesa, fogueira com tocos, cercado com espantalhos e feno |
| Leste — a igreja | **3** | igreja | lampiões na trilha, jardim de flores, outra casa |

As casas que são fase têm **tochas dos dois lados da porta, bandeirolas na trilha e uma
placa pendurada**. As outras casas são só cenário.

**Animados** (sem nenhum código, só Animator): tochas, tochas grandes nas pontes,
lampiões, fonte, fogueira, forja, bandeirolas, barcos balançando e folhas caindo das
árvores grandes. Cada tipo tem 3 versões defasadas, para as chamas não piscarem todas
juntas.

A água é a cor de fundo da câmera: o pack não traz tile de água.

---

## O personagem

`PERSONAGEM_MENUFASES.png` tem 5 direções, cada uma com **passo – parado – passo**
(o parado é o quadro do meio). Folha recortada em `Assets/MenuFase_Tiles/_personagem_frames.png`.

Animator: `Assets/MenuFase_Tiles/Animacao/Personagem.controller`, **Samples 12**.

| Estado | Condição |
|---|---|
| frente | `frente` e não `lado` |
| diagonal de frente | `frente` e `lado` |
| lado | `lado`, sem `frente` nem `costas` |
| diagonal de costas | `costas` e `lado` |
| costas | `costas` e não `lado` |

Cada um tem `Parado_...` e `andando_...`, escolhidos pelo bool `andando`. Para a
esquerda, as imagens de lado e das diagonais são espelhadas com `flipX`.

---

## Os scripts

Só dois, em `Assets/Scripts/MenuFase/`.

**`PersonagemMapa.cs`** — no Jogador.

| Parte | Aula |
|---|---|
| `MovimentoHorizontalFlip()` / `MovimentoVertical()` com `Input.GetAxis` e `velocity`, gravidade zero | Movimento Vertical |
| lê `DPadHorizontal` / `DPadVertical` quando o analógico está parado | Entradas de dados |
| `Renderer.flipX` | Flip |
| `SetBool("andando" / "frente" / "lado" / "costas")` | Animações |
| `OnTriggerEnter2D` / `OnTriggerExit2D` lendo a tag da porta | Tipos de Colisão |
| `SceneManager.LoadScene(...)` | Nova Fase |
| `public Text UITextAviso` | Apresentação de textos |
| `KeyCode.Joystick1Button1` (botão X do PlayStation) | Entradas de dados |

**`VoltarAoMapa.cs`** — nas cenas `fase01` a `fase03`. Botão Círculo (ou Esc/Enter) volta.

**`MenuPrincipal.cs`** (em `Assets/Scripts/`) — na cena `MenuPrincipal`. X (ou Enter)
seleciona, Círculo (ou Esc) fecha o painel.

**`TelaIdentificacao.cs`** (em `Assets/Scripts/Identificacao/`) — na cena
`Assets/Identificacao/identificacao.unity`. Antes de cada fase o jogador digita, num teclado
físico, o **nome** e o **celular ou e-mail** (mesmo campo). O `PersonagemMapa` guarda a fase
escolhida em `PlayerPrefs.SetString("FaseEscolhida", ...)` e abre essa tela; ao confirmar,
ela confere os campos (nome preenchido; e-mail com `@` e ponto, ou celular com pelo menos
10 números), guarda `JogadorNome` e `JogadorContato` no `PlayerPrefs` e abre a fase.
Por enquanto os dados não são enviados para lugar nenhum. O campo selecionado fica com a borda
laranja e a placa CONFIRMAR acende quando os dois campos estão certos. Fonte: Lilita One
(Google Fonts, licença OFL em `Assets/Identificacao/Fontes/`). A cena é montada pela
ferramenta `_fora_do_curso/Editor_Identificacao/ConstruirIdentificacao.cs`.

A porta de cada casa é um filho `Entrada` com Box Collider 2D **Is Trigger** e a tag
`fase01`, `fase02` ou `fase03` — igual ao nome da cena.

---

## Como a cena está montada

- **Chão:** Tilemaps `Areia`, `Grama`, `Decoracao`. A borda das ilhas usa o anel
  `02` + os cantos da ilhazinha `04`; as trilhas usam o anel `09` + os cantos do `13`.
  Cada anel do pack tem uma ilhazinha 2×2 que dá os cantos convexos — sem ela os
  cantos saem quebrados.
- **Colisão / limites da água:** Tilemap `Colisao`, invisível, cobrindo a água, a beira
  das ilhas e uma margem de 3 tiles em volta do mapa inteiro, com um buraco nas pontes.
  Ele tem Tilemap Collider 2D (Used By Composite) + Composite Collider 2D + Rigidbody 2D
  Static, então o personagem só anda na terra e nas pontes — não entra na água.
  O Jogador é Rigidbody 2D Dynamic (Collision Detection Continuous) com Box Collider 2D
  no pé (não é trigger). Casas, árvores e props têm Box Collider 2D na base.
  Se pintar mais tiles no `Colisao` pela paleta, o Composite se refaz sozinho.
- **Texto de aviso:** o `Canvas` do `TXT_AVISO` está em **Screen Space - Overlay**
  (Sort Order 100), então o texto fica sempre por cima dos tiles e das casas.
- **Profundidade:** tudo que fica de pé está na mesma Order in Layer (5) e o
  `Transparency Sort Axis` do projeto é (0, 1, 0): quem está mais embaixo na tela é
  desenhado na frente. É isso que deixa o personagem passar atrás das casas e árvores.
- **Câmera:** filha do Jogador, `Size 6`.
- **Paleta:** `Assets/MenuFase_Tiles/MenuFase_Palette.prefab`, com os tiles de terreno
  arrumados como se encaixam.

---

## O que não é do curso

- **Progresso salvo / fases bloqueadas:** as três fases estão sempre abertas, e ao
  voltar de uma fase o personagem recomeça no começo do mapa.
- **Any State** no Animator e o **Transparency Sort Axis**: configuração, não código.
- **Folhas caindo:** animação de posição e transparência feita na janela Animation
  (a aula usa essa janela só para trocar sprites).
- O mapa foi **montado por uma ferramenta de editor**, que depois saiu do projeto
  (está em `_fora_do_curso/`). O mesmo vale para `ArrumarMenuFase.cs`, que arrumou o
  Canvas do aviso e gerou o colisor da água (`_fora_do_curso/Editor_MenuFase/`). O que ficou na cena são objetos comuns: dá para mexer em
  tudo à mão pela Hierarchy e pela paleta.
