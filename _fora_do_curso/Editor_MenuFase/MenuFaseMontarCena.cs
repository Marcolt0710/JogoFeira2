using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Monta o mapa-múndi dentro de Assets/MenuDeFase/menudefase.unity e cria as cenas
/// Fase_01..Fase_05 de teste.
///
/// É um gerador: pode rodar de novo quantas vezes quiser. Ele apaga e refaz os
/// objetos que ele mesmo criou (os que estão em RAIZES_GERADAS) e deixa o resto
/// da cena em paz.
/// </summary>
public static class MenuFaseMontarCena
{
    public const string CENA_MAPA = "Assets/MenuDeFase/menudefase.unity";
    public const string PASTA_CENAS = "Assets/Scenes";
    public const string PASTA_TILES = MenuFaseGerarTiles.PASTA_TILES;

    // Ordem de desenho. Y-sort só acontece entre objetos com a MESMA ordem,
    // por isso jogador e construções ficam os dois em ORDEM_OBJETOS.
    private const int ORDEM_CHAO = 0;
    private const int ORDEM_CAMINHOS = 1;
    private const int ORDEM_DECORACAO = 2;
    private const int ORDEM_OBJETOS = 5;
    private const int ORDEM_ACIMA = 100;

    private static readonly string[] RAIZES_GERADAS =
    {
        "Grid", "Tilemap", "Jogador", "Objetos", "UI_MenuFase",
        "Transicao_Iris", "MapaInicializador"
    };

    [MenuItem("MenuFase/3 - Montar mapa e cenas de fase")]
    public static void Montar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        MenuFaseGerarTiles.GarantirPasta(PASTA_TILES);
        MenuFaseGerarTiles.GarantirPasta(PASTA_CENAS);

        // Os tiles precisam existir antes de pintar.
        MenuFaseGerarTiles.Gerar();

        Scene cena = EditorSceneManager.OpenScene(CENA_MAPA, OpenSceneMode.Single);

        LimparGerados(cena);

        Transform grid = CriarGrid();
        Dictionary<string, Tilemap> camadas = CriarCamadas(grid);

        bool[,] andavel = MenuFaseMapa.MontarTerreno();
        Pintar(camadas, andavel);

        GameObject jogador = CriarJogador();
        List<LevelNode> nos = CriarNos();
        CriarPortao();

        CriarCamera(jogador.transform);
        CriarInterface();

        CriarInicializador(jogador);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);

        CriarCenasDeFase();
        RegistrarNoBuild();

        // CriarCenasDeFase troca a cena aberta; devolve o mapa para o usuário.
        EditorSceneManager.OpenScene(CENA_MAPA, OpenSceneMode.Single);

        Debug.Log("[MenuFase] mapa montado em " + CENA_MAPA + " com " + nos.Count +
                  " fases. Aperte Play.");
    }

    // --------------------------------------------------------------- limpeza

    private static void LimparGerados(Scene cena)
    {
        GameObject[] raizes = cena.GetRootGameObjects();
        for (int i = 0; i < raizes.Length; i++)
        {
            for (int j = 0; j < RAIZES_GERADAS.Length; j++)
            {
                if (raizes[i].name == RAIZES_GERADAS[j])
                {
                    Object.DestroyImmediate(raizes[i]);
                    break;
                }
            }
        }
    }

    // ------------------------------------------------------------ tilemaps

    private static Transform CriarGrid()
    {
        GameObject go = new GameObject("Grid");
        Grid grid = go.AddComponent<Grid>();

        // 1 unidade por tile. Combina com PPU 16 nos sprites nativos e PPU 128
        // nos que são upscale 8x — os dois dão 1 tile = 1 unidade.
        grid.cellSize = new Vector3(1f, 1f, 0f);
        return go.transform;
    }

    private static Dictionary<string, Tilemap> CriarCamadas(Transform grid)
    {
        Dictionary<string, Tilemap> d = new Dictionary<string, Tilemap>();

        d["Chao"] = Camada(grid, "Chao", ORDEM_CHAO, true);
        d["Caminhos"] = Camada(grid, "Caminhos", ORDEM_CAMINHOS, true);
        d["Decoracao"] = Camada(grid, "Decoracao", ORDEM_DECORACAO, true);
        d["Colisao"] = Camada(grid, "Colisao", ORDEM_DECORACAO, false);
        d["AcimaDoJogador"] = Camada(grid, "AcimaDoJogador", ORDEM_ACIMA, true);

        MontarColisor(d["Colisao"].gameObject);
        return d;
    }

    private static Tilemap Camada(Transform grid, string nome, int ordem, bool visivel)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(grid, false);

        Tilemap mapa = go.AddComponent<Tilemap>();
        TilemapRenderer r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = ordem;
        r.enabled = visivel;

        return mapa;
    }

    /// <summary>
    /// TilemapCollider2D sozinho gera um colisor por tile, e o jogador engancha nas
    /// emendas. O CompositeCollider2D funde tudo num contorno só e resolve isso.
    /// </summary>
    private static void MontarColisor(GameObject alvo)
    {
        Rigidbody2D corpo = alvo.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Static;

        // O composite entra antes: marcar usedByComposite sem ele existir não pega.
        CompositeCollider2D cc = alvo.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;

        TilemapCollider2D tc = alvo.AddComponent<TilemapCollider2D>();
        tc.usedByComposite = true;
    }

    private static void Pintar(Dictionary<string, Tilemap> camadas, bool[,] andavel)
    {
        TileBase grama = Carregar<TileBase>(PASTA_TILES + "/AT_Grama.asset");
        TileBase caminho = Carregar<TileBase>(PASTA_TILES + "/T_Caminho.asset");
        TileBase colisao = Carregar<TileBase>(PASTA_TILES + "/T_Colisao.asset");

        TileBase[] tufos =
        {
            Carregar<TileBase>(PASTA_TILES + "/T_Tufo_1.asset"),
            Carregar<TileBase>(PASTA_TILES + "/T_Tufo_2.asset"),
            Carregar<TileBase>(PASTA_TILES + "/T_Tufo_3.asset")
        };

        Tilemap chao = camadas["Chao"];
        Tilemap mapaCaminhos = camadas["Caminhos"];
        Tilemap deco = camadas["Decoracao"];
        Tilemap mapaColisao = camadas["Colisao"];

        // Chão e colisão: o que não é grama vira parede.
        for (int y = 0; y < MenuFaseMapa.ALTURA; y++)
        {
            for (int x = 0; x < MenuFaseMapa.LARGURA; x++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                if (andavel[x, y]) chao.SetTile(p, grama);
                else mapaColisao.SetTile(p, colisao);
            }
        }

        List<Vector2Int> celulasCaminho = MenuFaseMapa.CelulasDeCaminho(andavel);
        for (int i = 0; i < celulasCaminho.Count; i++)
            mapaCaminhos.SetTile(new Vector3Int(celulasCaminho[i].x, celulasCaminho[i].y, 0), caminho);

        List<Vector2Int> celulasTufo = MenuFaseMapa.CelulasDeTufo(andavel, celulasCaminho);
        for (int i = 0; i < celulasTufo.Count; i++)
        {
            TileBase t = tufos[i % tufos.Length];
            if (t == null) continue;
            deco.SetTile(new Vector3Int(celulasTufo[i].x, celulasTufo[i].y, 0), t);
        }
    }

    // ------------------------------------------------------------- jogador

    private static GameObject CriarJogador()
    {
        GameObject go = new GameObject("Jogador");
        go.transform.position = ParaMundo(MenuFaseMapa.CelulaInicial);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ORDEM_OBJETOS;

        // Usa o personagem de verdade quando 'MenuFase > 4' já tiver rodado;
        // senão cai no bonequinho gerado, para a cena nunca ficar sem nada.
        Sprite[] quadros = MenuFaseGerarTiles.SpritesDoCaminho(MenuFasePersonagem.FOLHA);
        bool temPersonagem = quadros != null && quadros.Length > 0 && quadros[0] != null;
        sr.sprite = temPersonagem ? quadros[0] : SpritePlaceholder.ObterJogador();

        Rigidbody2D corpo = go.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        // Colisor na altura dos pés: a cabeça pode invadir a parede de cima, que é
        // o que dá a sensação de profundidade no top-down. O personagem tem 24px de
        // altura com pivô no centro, então os pés ficam 0,75 abaixo da origem.
        if (temPersonagem)
        {
            col.radius = 0.28f;
            col.offset = new Vector2(0f, -0.45f);
        }
        else
        {
            col.radius = 0.32f;
            col.offset = new Vector2(0f, -0.2f);
        }

        PlayerMapController ctrl = go.AddComponent<PlayerMapController>();
        ctrl.velocidade = 5f;
        ctrl.sprite = sr;

        AnimatorController rac = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            MenuFasePersonagem.CONTROLLER);

        if (rac != null)
        {
            Animator anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = rac;
            anim.applyRootMotion = false;
            // Sem isto o Animator dorme quando o sprite sai da tela e volta travado.
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ctrl.animator = anim;
        }
        else
        {
            Debug.Log("[MenuFase] sem controller de personagem ainda. " +
                      "Rode 'MenuFase > 4 - Preparar personagem e animacoes'.");
        }

        return go;
    }

    // ---------------------------------------------------------------- nós

    private static List<LevelNode> CriarNos()
    {
        GameObject raiz = new GameObject("Objetos");
        List<LevelNode> lista = new List<LevelNode>();

        Sprite[] pilar = MenuFaseGerarTiles.SpritesDe("07_pilar_terra");
        Sprite[] pilarPedra = MenuFaseGerarTiles.SpritesDe("08_pilar_pedra");

        for (int i = 0; i < MenuFaseMapa.FASES.Length; i++)
        {
            MenuFaseMapa.Fase f = MenuFaseMapa.FASES[i];

            GameObject go = new GameObject("Fase_" + (i + 1) + "_" + f.id);
            go.transform.SetParent(raiz.transform, false);
            go.transform.position = ParaMundo(f.celula);

            // A construção da fase é o pilar inteiro remontado, alternando terra e
            // pedra para as cinco fases não ficarem idênticas.
            Sprite[] fonte = (i % 2 == 0) ? pilar : pilarPedra;
            MontarBloco(go.transform, fonte, 3, 6);

            CircleCollider2D zona = go.AddComponent<CircleCollider2D>();
            zona.isTrigger = true;
            zona.radius = 1.4f;

            LevelNode no = go.AddComponent<LevelNode>();
            no.id = f.id;
            no.nomeExibicao = f.nome;
            no.cenaDaFase = f.cena;
            no.exigeConcluidas = f.exige;

            no.pontoDeRetorno = CriarFilho(go.transform, "PontoDeRetorno",
                new Vector3(0f, -1.5f, 0f)).transform;

            no.bandeira = CriarBandeira(go.transform);
            no.aviso = CriarAviso(go.transform);

            lista.Add(no);
        }

        return lista;
    }

    /// <summary>
    /// Remonta uma folha fatiada como um bloco de SpriteRenderers filhos.
    ///
    /// Os sprites voltam a formar o desenho original: 1 fatia = 1 unidade, a base
    /// apoiada no y do pai e o bloco centrado no x. Cada pedaço é um renderer
    /// separado de propósito — assim o Y-sort trata cada fileira por conta própria e
    /// o jogador passa na frente da base e por trás do topo.
    /// </summary>
    private static void MontarBloco(Transform pai, Sprite[] fatias, int colunas, int linhas)
    {
        if (fatias == null) return;

        float meio = (colunas - 1) * 0.5f;

        for (int linha = 0; linha < linhas; linha++)
        {
            for (int col = 0; col < colunas; col++)
            {
                int i = linha * colunas + col;
                if (i >= fatias.Length || fatias[i] == null) continue;

                GameObject peca = CriarFilho(pai, "p" + col + "_" + linha,
                    new Vector3(col - meio, linhas - 1 - linha, 0f));

                SpriteRenderer sr = peca.AddComponent<SpriteRenderer>();
                sr.sprite = fatias[i];
                sr.sortingOrder = ORDEM_OBJETOS;
            }
        }
    }

    /// <summary>Marcador de fase concluída, desenhado à parte (o pacote não tem bandeira).</summary>
    private static GameObject CriarBandeira(Transform pai)
    {
        GameObject go = CriarFilho(pai, "Bandeira", new Vector3(1.2f, 6.3f, 0f));

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpritePlaceholder.ObterBandeira();
        sr.sortingOrder = ORDEM_OBJETOS + 1;

        go.SetActive(false);
        return go;
    }

    /// <summary>O "Z / Enter" que flutua sobre a fase quando o jogador chega perto.</summary>
    private static GameObject CriarAviso(Transform pai)
    {
        GameObject go = CriarFilho(pai, "Aviso", new Vector3(0f, 7.1f, 0f));

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = ORDEM_ACIMA;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 32f);
        // A UI de mundo nasce gigante: 1 unidade = 1 pixel de canvas.
        rt.localScale = Vector3.one * 0.02f;

        GameObject textoGo = new GameObject("Texto");
        textoGo.transform.SetParent(go.transform, false);

        Text t = textoGo.AddComponent<Text>();
        t.font = FontePadrao();
        t.text = "Z / Enter";
        t.fontSize = 22;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform trt = textoGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// A ponte que só aparece depois da fase 3, com a barreira que segura o
    /// jogador enquanto isso.
    /// </summary>
    private static void CriarPortao()
    {
        GameObject raiz = GameObject.Find("Objetos");
        Vector2Int celula = MenuFaseMapa.CelulaDoPortao();

        GameObject go = new GameObject("Portao_Ponte");
        if (raiz != null) go.transform.SetParent(raiz.transform, false);
        go.transform.position = ParaMundo(celula);

        GameObject ponte = CriarFilho(go.transform, "Ponte", Vector3.zero);
        SpriteRenderer sr = ponte.AddComponent<SpriteRenderer>();
        Sprite[] s = MenuFaseGerarTiles.SpritesDe("Bridge_Wood");
        sr.sprite = (s != null && s.Length > 0) ? s[0] : null;
        sr.sortingOrder = ORDEM_CAMINHOS + 1;

        BoxCollider2D barreira = go.AddComponent<BoxCollider2D>();
        barreira.size = new Vector2(3f, 1f);

        PathGate portao = go.AddComponent<PathGate>();
        portao.exigeConcluidas = new[] { "fase_03" };
        portao.modo = PathGate.Modo.AparecerQuandoLiberado;
        portao.alvo = ponte;
        portao.barreira = barreira;
    }

    // -------------------------------------------------------------- câmera

    private static void CriarCamera(Transform alvo)
    {
        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }

        cam.orthographic = true;
        cam.backgroundColor = new Color(0.09f, 0.11f, 0.15f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(alvo.position.x, alvo.position.y, -10f);

        PixelPerfectCam pp = Pegar<PixelPerfectCam>(cam.gameObject);
        pp.pixelsPorUnidade = 16;
        pp.zoom = 0;
        pp.alturaDeReferencia = 180;

        CameraSeguidor seg = Pegar<CameraSeguidor>(cam.gameObject);
        seg.alvo = alvo;
        seg.amortecimento = 0.15f;
        seg.usarLimites = true;
        seg.limiteMin = new Vector2(0f, 0f);
        seg.limiteMax = new Vector2(MenuFaseMapa.LARGURA, MenuFaseMapa.ALTURA);
    }

    // ----------------------------------------------------------------- UI

    private static LevelCard CriarInterface()
    {
        GameObject raiz = new GameObject("UI_MenuFase");

        Canvas canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = raiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.matchWidthOrHeight = 0.5f;

        raiz.AddComponent<GraphicRaycaster>();
        GarantirEventSystem();

        LevelCard cartao = MontarCartao(raiz.transform);
        MontarIris(raiz.transform);

        return cartao;
    }

    private static LevelCard MontarCartao(Transform pai)
    {
        GameObject painel = NovoUI("Painel_Cartao", pai);
        Esticar(painel.GetComponent<RectTransform>());

        Image fundo = painel.AddComponent<Image>();
        fundo.color = new Color(0f, 0f, 0f, 0.55f);
        fundo.raycastTarget = false;

        CanvasGroup grupo = painel.AddComponent<CanvasGroup>();

        // O cartão em si.
        GameObject cartaoGo = NovoUI("Cartao", painel.transform);
        RectTransform cartaoRt = cartaoGo.GetComponent<RectTransform>();
        cartaoRt.anchorMin = new Vector2(0.5f, 0.5f);
        cartaoRt.anchorMax = new Vector2(0.5f, 0.5f);
        cartaoRt.pivot = new Vector2(0.5f, 0.5f);
        cartaoRt.sizeDelta = new Vector2(560f, 320f);
        cartaoRt.anchoredPosition = Vector2.zero;

        Image cartaoFundo = cartaoGo.AddComponent<Image>();
        cartaoFundo.color = new Color(0.13f, 0.11f, 0.09f, 0.97f);
        cartaoFundo.raycastTarget = false;

        Text nome = Texto(cartaoGo.transform, "Nome", "Fase", 40, new Vector2(0f, 108f), 500f);
        nome.fontStyle = FontStyle.Bold;

        Text estado = Texto(cartaoGo.transform, "Estado", "DISPONÍVEL", 22, new Vector2(0f, 62f), 500f);
        estado.color = new Color(0.65f, 0.65f, 0.65f, 1f);

        Texto(cartaoGo.transform, "RotuloDif", "dificuldade", 18, new Vector2(0f, 16f), 500f)
            .color = new Color(0.5f, 0.5f, 0.5f, 1f);

        Text simples = Texto(cartaoGo.transform, "Simples", "SIMPLES", 26, new Vector2(-110f, -18f), 220f);
        Text normal = Texto(cartaoGo.transform, "Normal", "NORMAL", 26, new Vector2(110f, -18f), 220f);

        Text entrar = Texto(cartaoGo.transform, "Entrar", "> ENTRAR", 30, new Vector2(0f, -78f), 500f);
        Text voltar = Texto(cartaoGo.transform, "Voltar", "VOLTAR", 24, new Vector2(0f, -122f), 500f);

        // O componente vai na RAIZ do canvas, não no painel. O painel nasce
        // desligado, e Awake não roda em objeto desligado: posto nele, o LevelCard
        // nunca se registraria e o cartão jamais abriria.
        LevelCard card = pai.gameObject.AddComponent<LevelCard>();
        card.painel = painel;
        card.cartao = cartaoRt;
        card.grupo = grupo;
        card.textoNome = nome;
        card.textoEstado = estado;
        card.textoSimples = simples;
        card.textoNormal = normal;
        card.textoEntrar = entrar;
        card.textoVoltar = voltar;

        painel.SetActive(false);
        return card;
    }

    private static void MontarIris(Transform pai)
    {
        GameObject go = new GameObject("Transicao_Iris");
        go.transform.SetParent(pai.parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // por cima de tudo, inclusive do cartão
        go.AddComponent<CanvasScaler>();

        GameObject img = NovoUI("Iris", go.transform);
        Esticar(img.GetComponent<RectTransform>());

        Image imagem = img.AddComponent<Image>();
        imagem.color = Color.black;
        imagem.raycastTarget = false;
        imagem.material = MaterialDaIris();

        TransicaoIris iris = go.AddComponent<TransicaoIris>();
        iris.imagem = imagem;
        iris.duracao = 0.55f;
    }

    private static Material MaterialDaIris()
    {
        string caminho = PASTA_TILES + "/Mat_IrisUI.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m != null) return m;

        Shader sh = Shader.Find("MenuFase/IrisUI");
        if (sh == null)
        {
            Debug.LogWarning("[MenuFase] shader MenuFase/IrisUI não encontrado; " +
                             "a transição vai ficar sem o círculo.");
            return null;
        }

        m = new Material(sh);
        m.SetFloat("_Raio", TransicaoIris.RAIO_ABERTO);
        AssetDatabase.CreateAsset(m, caminho);
        return m;
    }

    private static void CriarInicializador(GameObject jogador)
    {
        GameObject go = new GameObject("MapaInicializador");
        MapaInicializador init = go.AddComponent<MapaInicializador>();
        init.jogador = jogador.GetComponent<PlayerMapController>();

        GameObject ponto = CriarFilho(go.transform, "PontoInicial",
            ParaMundo(MenuFaseMapa.CelulaInicial));
        init.pontoInicial = ponto.transform;
    }

    // ------------------------------------------------------- cenas de fase

    private static void CriarCenasDeFase()
    {
        for (int i = 0; i < MenuFaseMapa.FASES.Length; i++)
        {
            MenuFaseMapa.Fase f = MenuFaseMapa.FASES[i];
            string caminho = PASTA_CENAS + "/" + f.cena + ".unity";
            if (System.IO.File.Exists(caminho)) continue;

            Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            GarantirEventSystem();

            GameObject raiz = new GameObject("UI");
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler sc = raiz.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1366f, 768f);
            sc.matchWidthOrHeight = 0.5f;
            raiz.AddComponent<GraphicRaycaster>();

            Texto(raiz.transform, "Cabecalho", f.nome, 46, new Vector2(0f, 150f), 900f)
                .fontStyle = FontStyle.Bold;

            Text titulo = Texto(raiz.transform, "Titulo", f.id, 26, new Vector2(0f, 96f), 900f);
            Text sub = Texto(raiz.transform, "Subtitulo", "", 22, new Vector2(0f, 56f), 900f);
            sub.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            Button concluir = Botao(raiz.transform, "Botao_Concluir", "CONCLUIR FASE  (Enter)",
                new Vector2(0f, -30f));
            Button voltar = Botao(raiz.transform, "Botao_Voltar", "VOLTAR SEM CONCLUIR  (Esc)",
                new Vector2(0f, -110f));

            FaseTeste teste = raiz.AddComponent<FaseTeste>();
            teste.titulo = titulo;
            teste.subtitulo = sub;
            teste.botaoConcluir = concluir;
            teste.botaoVoltar = voltar;

            EditorSceneManager.SaveScene(cena, caminho);
        }
    }

    private static void RegistrarNoBuild()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();

        // Reescrever a lista às vezes devolve entrada com o path em branco (só o GUID
        // sobrevive). O Build Settings então mostra uma linha vazia. Aqui cada entrada
        // antiga é remontada a partir do GUID antes de acrescentar as novas.
        EditorBuildSettingsScene[] atuais = EditorBuildSettings.scenes;
        for (int i = 0; i < atuais.Length; i++)
        {
            string caminho = atuais[i].path;

            if (string.IsNullOrEmpty(caminho) && !atuais[i].guid.Empty())
                caminho = AssetDatabase.GUIDToAssetPath(atuais[i].guid.ToString());

            if (string.IsNullOrEmpty(caminho)) continue;

            lista.Add(new EditorBuildSettingsScene(caminho, atuais[i].enabled));
        }

        List<string> queremos = new List<string>();
        queremos.Add(CENA_MAPA);
        for (int i = 0; i < MenuFaseMapa.FASES.Length; i++)
            queremos.Add(PASTA_CENAS + "/" + MenuFaseMapa.FASES[i].cena + ".unity");

        for (int i = 0; i < queremos.Count; i++)
        {
            bool tem = false;
            for (int j = 0; j < lista.Count; j++)
                if (lista[j].path == queremos[i]) { tem = true; break; }

            if (!tem && System.IO.File.Exists(queremos[i]))
                lista.Add(new EditorBuildSettingsScene(queremos[i], true));
        }

        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ------------------------------------------------------------ utilidades

    private static Vector3 ParaMundo(Vector2Int celula)
    {
        // O tile ocupa a célula inteira; +0.5 põe o objeto no centro dela.
        return new Vector3(celula.x + 0.5f, celula.y + 0.5f, 0f);
    }

    private static GameObject CriarFilho(Transform pai, string nome, Vector3 posLocal)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = posLocal;
        return go;
    }

    private static T Pegar<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static T Carregar<T>(string caminho) where T : Object
    {
        T a = AssetDatabase.LoadAssetAtPath<T>(caminho);
        if (a == null) Debug.LogWarning("[MenuFase] não achei " + caminho);
        return a;
    }

    private static GameObject NovoUI(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text Texto(Transform pai, string nome, string conteudo, int tamanho,
                              Vector2 pos, float largura)
    {
        GameObject go = NovoUI(nome, pai);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(largura, tamanho + 14f);
        rt.anchoredPosition = pos;

        Text t = go.AddComponent<Text>();
        t.font = FontePadrao();
        t.text = conteudo;
        t.fontSize = tamanho;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        return t;
    }

    private static Button Botao(Transform pai, string nome, string rotulo, Vector2 pos)
    {
        GameObject go = NovoUI(nome, pai);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460f, 56f);
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.19f, 0.17f, 1f);

        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        Text t = Texto(go.transform, "Texto", rotulo, 22, Vector2.zero, 440f);
        t.color = Color.white;

        return b;
    }

    private static Font FontePadrao()
    {
        // O pacote MenuDeFase não traz fonte nenhuma, então fica a embutida do Unity.
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void GarantirEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
