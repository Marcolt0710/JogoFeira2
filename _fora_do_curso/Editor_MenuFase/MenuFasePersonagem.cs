using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Prepara o personagem do mapa a partir de PERSONAGEM_MENUFASES.png.
///
/// A folha original não dá para usar direto por três motivos, todos medidos:
///
///  1. Ela tem um cabeçalho azul (y 0..63) com as setas indicando a direção de cada
///     grupo. Só a faixa de baixo (y 64..183) é personagem.
///  2. Cada quadro está ampliado 5x (célula de 80x120 na folha = 16x24 de verdade,
///     conferido com PSNR = infinito na redução por 5).
///  3. O fundo é VERDE OPACO, não transparente. São dois verdes: 84,165,75 no geral
///     e 58,111,51 no terceiro quadro, que veio marcado na arte original.
///
/// Este script lê o PNG original sem tocar nele, recorta os 14 quadros pela grade
/// medida (primeiro em x=9, passo de 85, célula de 80 de largura), reduz para o
/// tamanho nativo, apaga os dois verdes e grava uma folha limpa de 3 colunas por
/// 5 linhas em MenuFase_Tiles. Depois fatia, monta os clipes e o Animator.
///
/// Grupos, conforme as setas do cabeçalho (elas caem exatamente sobre as células
/// 0, 3, 6, 9 e 12):
///
///     linha 0  quadros 0,1,2    ↓  Baixo
///     linha 1  quadros 3,4,5    ↘  BaixoDiag
///     linha 2  quadros 6,7,8    →  Lado
///     linha 3  quadros 9,10,11  ↗  CimaDiag
///     linha 4  quadros 12,13    ↑  Cima      (só 2: a folha tem 14 quadros, não 15)
///
/// As direções para a esquerda não existem na arte; saem espelhando o Lado e as
/// diagonais com flipX, que o PlayerMapController faz.
/// </summary>
public static class MenuFasePersonagem
{
    public const string ORIGEM = "Assets/MenuDeFase/PERSONAGEM_MENUFASES.png";
    public const string FOLHA = MenuFaseGerarTiles.PASTA_TILES + "/_personagem_frames.png";
    public const string PASTA_ANIM = MenuFaseGerarTiles.PASTA_TILES + "/Animacao";
    public const string CONTROLLER = PASTA_ANIM + "/Personagem.controller";

    // Geometria medida na folha original.
    private const int ORIG_X0 = 9;        // x da primeira célula
    private const int ORIG_Y0 = 64;       // primeira linha abaixo do cabeçalho
    private const int ORIG_PASSO = 85;    // 80 de célula + 5 de separador
    private const int ORIG_W = 80;
    private const int ORIG_H = 120;
    private const int ESCALA = 5;

    public const int QUADRO_W = ORIG_W / ESCALA;   // 16
    public const int QUADRO_H = ORIG_H / ESCALA;   // 24
    public const int QUADROS = 14;
    public const int COLUNAS = 3;
    public const int LINHAS = 5;

    /// <summary>Quantos quadros cada direção tem. A de cima ficou com 2.</summary>
    private static readonly int[] TAMANHO_GRUPO = { 3, 3, 3, 3, 2 };

    private static readonly string[] NOME_GRUPO =
        { "Baixo", "BaixoDiag", "Lado", "CimaDiag", "Cima" };

    private const int FPS = 10;

    [MenuItem("MenuFase/4 - Preparar personagem e animacoes")]
    public static void Preparar()
    {
        if (!File.Exists(ORIGEM))
        {
            Debug.LogError("[MenuFase] não achei " + ORIGEM);
            return;
        }

        MenuFaseGerarTiles.GarantirPasta(MenuFaseGerarTiles.PASTA_TILES);
        MenuFaseGerarTiles.GarantirPasta(PASTA_ANIM);

        GerarFolhaLimpa();
        ConfigurarImportacao();

        Sprite[] quadros = CarregarQuadros();
        if (quadros == null) return;

        MontarAnimator(quadros);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MenuFase] personagem pronto: " + QUADROS + " quadros em " + FOLHA +
                  ", controller em " + CONTROLLER + ". " +
                  "Rode 'MenuFase > 3' para o Jogador da cena passar a usar.");
    }

    // ------------------------------------------------------- folha limpa

    /// <summary>
    /// Recorta, reduz 5x e tira o fundo verde. O PNG original não é alterado.
    /// </summary>
    private static void GerarFolhaLimpa()
    {
        // LoadImage devolve textura legível mesmo se o asset estiver com
        // isReadable desligado, então não é preciso mexer no importer da origem.
        Texture2D origem = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        origem.LoadImage(File.ReadAllBytes(ORIGEM));

        // GetPixels32, e não GetPixel: GetPixel devolve Color em float, e a volta
        // para Color32 trunca em vez de arredondar. O verde 84,165,75 chegava como
        // 83,164,74 e a comparação exata nunca batia — o fundo ficava todo lá.
        Color32[] pixelsOrigem = origem.GetPixels32();
        int origemLargura = origem.width;

        int largura = COLUNAS * QUADRO_W;
        int altura = LINHAS * QUADRO_H;

        Color32 vazio = new Color32(0, 0, 0, 0);
        Color32[] saida = new Color32[largura * altura];
        for (int i = 0; i < saida.Length; i++) saida[i] = vazio;

        for (int q = 0; q < QUADROS; q++)
        {
            int col = q % COLUNAS;
            int linha = q / COLUNAS;

            int origemX = ORIG_X0 + ORIG_PASSO * q;

            // A cor de fundo é lida do próprio canto superior esquerdo da célula,
            // em vez de vir escrita no código. Duas razões: o terceiro quadro tem
            // um verde diferente dos outros, e o Unity não devolve os bytes do PNG
            // intactos — o LoadImage/EncodeToPNG mexe no espaço de cor e o
            // 84,165,75 do arquivo chega como 75,158,66. Amostrando, tanto faz.
            Color32 fundo = pixelsOrigem[(QUADRO_H - 1) * ESCALA * origemLargura + origemX];

            for (int py = 0; py < QUADRO_H; py++)
            {
                for (int px = 0; px < QUADRO_W; px++)
                {
                    // A textura conta o Y de baixo para cima. A faixa do personagem
                    // é justamente a parte de baixo da folha, então o Y nativo já
                    // serve direto, multiplicado pela escala.
                    int sx = origemX + px * ESCALA;
                    int sy = py * ESCALA;

                    Color32 c = pixelsOrigem[sy * origemLargura + sx];
                    if (EhFundo(c, fundo)) c = vazio;

                    // No destino, a linha 0 fica em CIMA.
                    int destY = (LINHAS - 1 - linha) * QUADRO_H + py;
                    int destX = col * QUADRO_W + px;

                    saida[destY * largura + destX] = c;
                }
            }
        }

        Texture2D destino = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        destino.SetPixels32(saida);
        destino.Apply();
        File.WriteAllBytes(FOLHA, destino.EncodeToPNG());

        Object.DestroyImmediate(origem);
        Object.DestroyImmediate(destino);

        AssetDatabase.ImportAsset(FOLHA, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// Tolerância de 3 por canal. A arte não tem anti-aliasing, então bastaria
    /// comparação exata; a folga é seguro contra arredondamento. As cores do
    /// personagem (branco, preto, vermelho, marrom) não chegam perto do verde.
    /// </summary>
    private const int TOLERANCIA = 3;

    private static bool EhFundo(Color32 c, Color32 fundo)
    {
        return Mathf.Abs(c.r - fundo.r) <= TOLERANCIA &&
               Mathf.Abs(c.g - fundo.g) <= TOLERANCIA &&
               Mathf.Abs(c.b - fundo.b) <= TOLERANCIA;
    }

    // ------------------------------------------------------- importação

    /// <summary>
    /// A folha limpa fica fora da MenuDeFase, então o MenuFaseImportador não pega
    /// nela. Além disso a célula é 16x24, e aquele importador só fatia em quadrado.
    /// </summary>
    private static void ConfigurarImportacao()
    {
        TextureImporter imp = AssetImporter.GetAtPath(FOLHA) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("[MenuFase] " + FOLHA + " não importou como textura.");
            return;
        }

        TextureImporterSettings cfg = new TextureImporterSettings();
        imp.ReadTextureSettings(cfg);
        cfg.spriteMeshType = SpriteMeshType.FullRect;
        cfg.spriteExtrude = 0;
        cfg.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(cfg);

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = MenuFaseImportador.TILE;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        string nome = Path.GetFileNameWithoutExtension(FOLHA);
        int altura = LINHAS * QUADRO_H;

        List<SpriteMetaData> fatias = new List<SpriteMetaData>();
        for (int q = 0; q < QUADROS; q++)
        {
            int col = q % COLUNAS;
            int linha = q / COLUNAS;

            SpriteMetaData md = new SpriteMetaData();
            md.name = nome + "_" + q;
            md.rect = new Rect(col * QUADRO_W,
                               altura - (linha + 1) * QUADRO_H,
                               QUADRO_W, QUADRO_H);
            md.alignment = (int)SpriteAlignment.Center;
            md.pivot = new Vector2(0.5f, 0.5f);
            fatias.Add(md);
        }

        imp.spritesheet = fatias.ToArray();
        imp.SaveAndReimport();
    }

    private static Sprite[] CarregarQuadros()
    {
        // Pelo caminho completo: a folha limpa não está na pasta de arte, e o
        // SpritesDe(nome) procura sempre dentro de MenuDeFase.
        Sprite[] todos = MenuFaseGerarTiles.SpritesDoCaminho(FOLHA);

        if (todos == null || todos.Length < QUADROS)
        {
            Debug.LogError("[MenuFase] esperava " + QUADROS + " quadros e achei " +
                           (todos == null ? 0 : todos.Length) + " em " + FOLHA);
            return null;
        }

        for (int i = 0; i < QUADROS; i++)
        {
            if (todos[i] == null)
            {
                Debug.LogError("[MenuFase] quadro " + i + " veio vazio em " + FOLHA);
                return null;
            }
        }
        return todos;
    }

    // ---------------------------------------------------------- animator

    private static AnimatorController MontarAnimator(Sprite[] quadros)
    {
        AssetDatabase.DeleteAsset(CONTROLLER);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);

        ctrl.AddParameter("Dir", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("Andando", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine maquina = ctrl.layers[0].stateMachine;

        int primeiro = 0;
        for (int g = 0; g < NOME_GRUPO.Length; g++)
        {
            int total = TAMANHO_GRUPO[g];

            Sprite[] doGrupo = new Sprite[total];
            for (int i = 0; i < total; i++) doGrupo[i] = quadros[primeiro + i];

            // Parado usa o primeiro quadro do grupo.
            AnimationClip parado = CriarClipe("Parado_" + NOME_GRUPO[g],
                                              new[] { doGrupo[0] });
            AnimationClip andando = CriarClipe("Andando_" + NOME_GRUPO[g], doGrupo);

            AdicionarEstado(ctrl, maquina, "Parado_" + NOME_GRUPO[g], parado, g, false);
            AdicionarEstado(ctrl, maquina, "Andando_" + NOME_GRUPO[g], andando, g, true);

            primeiro += total;
        }

        // Estado inicial: parado virado para baixo.
        foreach (ChildAnimatorState c in maquina.states)
        {
            if (c.state.name == "Parado_Baixo") { maquina.defaultState = c.state; break; }
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static void AdicionarEstado(AnimatorController ctrl, AnimatorStateMachine maquina,
                                        string nome, AnimationClip clipe, int dir, bool andando)
    {
        AnimatorState estado = maquina.AddState(nome);
        estado.motion = clipe;
        estado.writeDefaultValues = false;

        // Transição a partir de AnyState: com 10 estados, ligar todos entre si daria
        // 90 transições. canTransitionToSelf desligado evita reiniciar a animação
        // toda vez que a condição continua verdadeira.
        AnimatorStateTransition t = maquina.AddAnyStateTransition(estado);
        t.duration = 0f;
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.Equals, dir, "Dir");
        t.AddCondition(andando ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                       0f, "Andando");
    }

    private static AnimationClip CriarClipe(string nome, Sprite[] quadros)
    {
        string caminho = PASTA_ANIM + "/" + nome + ".anim";
        AssetDatabase.DeleteAsset(caminho);

        AnimationClip clipe = new AnimationClip();
        clipe.frameRate = FPS;

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";                  // o SpriteRenderer está no mesmo objeto
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] chaves = new ObjectReferenceKeyframe[quadros.Length];
        for (int i = 0; i < quadros.Length; i++)
        {
            chaves[i] = new ObjectReferenceKeyframe();
            chaves[i].time = i / (float)FPS;
            chaves[i].value = quadros[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clipe, binding, chaves);

        AnimationClipSettings cfg = AnimationUtility.GetAnimationClipSettings(clipe);
        cfg.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clipe, cfg);

        AssetDatabase.CreateAsset(clipe, caminho);
        return clipe;
    }
}
