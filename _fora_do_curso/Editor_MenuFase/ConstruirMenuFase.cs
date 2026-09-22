using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// FERRAMENTA TEMPORARIA: monta a cena uma vez e depois sai do projeto.
// Nao faz parte do jogo.
public static class ConstruirMenuFase
{
    const string ARTE = "Assets/MenuDeFase/";
    const string GERADOS = "Assets/MenuFase_Tiles/";
    const string ANIM = "Assets/MenuFase_Tiles/Animacao/";
    const string FOLHA = "Assets/MenuFase_Tiles/_personagem_frames.png";
    const string CENA = "Assets/MenuDeFase/menudefase.unity";

    // campo de areia (interior), em celulas
    const int LARG = 36, ALT = 22, MARGEM = 13;

    // x da coluna central do pilar, y da base
    static readonly int[,] FASES = { { 5, 12 }, { 12, 4 }, { 19, 12 }, { 26, 4 }, { 31, 12 } };
    static readonly int[,] ARBUSTOS = { { 2, 2 }, { 9, 17 }, { 15, 9 }, { 23, 17 }, { 29, 10 }, { 34, 2 }, { 7, 7 }, { 33, 19 }, { 1, 18 }, { 22, 1 } };

    [MenuItem("MenuFase/Construir (temporario)")]
    public static void Construir()
    {
        PrepararFolha();
        Sprite[] q = Sprites(FOLHA);
        AnimatorController ctrl = Animacoes(q);

        Tile grama = NovoTile("T_Grama", Sprites(ARTE + "Grass_1_Middle.png")[0]);
        Sprite[] anel = Sprites(ARTE + "09_autotile_grama_areia.png");
        Tile[] areia = new Tile[9];
        for (int i = 0; i < 9; i++) areia[i] = NovoTile("T_Areia_" + i, anel[i]);
        Tile[] tufos = {
            NovoTile("T_Tufo_1", Sprites(ARTE + "15_detalhe_grama_1.png")[0]),
            NovoTile("T_Tufo_2", Sprites(ARTE + "16_detalhe_grama_2.png")[0]),
            NovoTile("T_Tufo_3", Sprites(ARTE + "17_detalhe_grama_3.png")[0]) };

        Scene cena = EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);
        foreach (GameObject g in cena.GetRootGameObjects()) Object.DestroyImmediate(g);

        // ---------------- chao
        GameObject grid = new GameObject("Grid");
        grid.AddComponent<Grid>();
        Tilemap chao = Camada(grid, "Chao", 0);
        Tilemap deco = Camada(grid, "Decoracao", 1);

        Random.InitState(7);
        for (int x = -MARGEM; x < LARG + MARGEM; x++)
            for (int y = -MARGEM; y < ALT + MARGEM; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                int i = IndiceAnel(x, y);
                if (i >= 0) chao.SetTile(p, areia[i]);
                else
                {
                    chao.SetTile(p, grama);
                    if (Random.value < 0.07f) deco.SetTile(p, tufos[Random.Range(0, 3)]);
                }
            }

        // ---------------- paredes invisiveis (Box Collider 2D, como nas aulas)
        GameObject paredes = new GameObject("Paredes");
        Parede(paredes, "Parede_Esquerda", -0.5f, ALT / 2f, 1, ALT + 2);
        Parede(paredes, "Parede_Direita", LARG + 0.5f, ALT / 2f, 1, ALT + 2);
        Parede(paredes, "Parede_Baixo", LARG / 2f, -0.5f, LARG + 2, 1);
        Parede(paredes, "Parede_Cima", LARG / 2f, ALT + 0.5f, LARG + 2, 1);

        // ---------------- arbustos
        Sprite[] arbusto = Sprites(ARTE + "13_ilha_grama_sobre_areia.png");
        GameObject raizArbustos = new GameObject("Arbustos");
        for (int i = 0; i < ARBUSTOS.GetLength(0); i++)
        {
            int bx = ARBUSTOS[i, 0], by = ARBUSTOS[i, 1];
            GameObject a = new GameObject("Arbusto");
            a.transform.SetParent(raizArbustos.transform);
            a.transform.position = new Vector3(bx + 1, by, 0);
            Peca(a, arbusto[0], -0.5f, 1.5f); Peca(a, arbusto[1], 0.5f, 1.5f);
            Peca(a, arbusto[2], -0.5f, 0.5f); Peca(a, arbusto[3], 0.5f, 0.5f);
            BoxCollider2D c = a.AddComponent<BoxCollider2D>();
            c.offset = new Vector2(0, 0.6f); c.size = new Vector2(1.6f, 1.0f);
        }

        // ---------------- fases
        Sprite[] pilarTerra = Sprites(ARTE + "07_pilar_terra.png");
        Sprite[] pilarPedra = Sprites(ARTE + "08_pilar_pedra.png");
        GameObject raizFases = new GameObject("Fases");
        for (int f = 0; f < FASES.GetLength(0); f++)
        {
            int fx = FASES[f, 0], fy = FASES[f, 1];
            GameObject fase = new GameObject("Fase" + (f + 1));
            fase.transform.SetParent(raizFases.transform);
            fase.transform.position = new Vector3(fx + 0.5f, fy, 0);
            Sprite[] pilar = (f % 2 == 0) ? pilarTerra : pilarPedra;
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 3; c++)
                    Peca(fase, pilar[r * 3 + c], c - 1, 5 - r + 0.5f);
            BoxCollider2D solido = fase.AddComponent<BoxCollider2D>();
            solido.offset = new Vector2(0, 1f); solido.size = new Vector2(2.6f, 2f);

            GameObject entrada = new GameObject("Entrada");
            entrada.transform.SetParent(fase.transform, false);
            entrada.transform.localPosition = new Vector3(0, -0.8f, 0);
            entrada.tag = "fase0" + (f + 1);
            BoxCollider2D gatilho = entrada.AddComponent<BoxCollider2D>();
            gatilho.isTrigger = true; gatilho.size = new Vector2(2.4f, 1.6f);
        }

        // ---------------- jogador
        GameObject jogador = new GameObject("Jogador");
        jogador.transform.position = new Vector3(18.5f, 1f, 0);
        SpriteRenderer sr = jogador.AddComponent<SpriteRenderer>();
        sr.sprite = q[1]; sr.sortingOrder = 5;
        Rigidbody2D rb = jogador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D pes = jogador.AddComponent<BoxCollider2D>();
        pes.offset = new Vector2(0, 0.2f); pes.size = new Vector2(0.6f, 0.4f);
        Animator an = jogador.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        PersonagemMapa pm = jogador.AddComponent<PersonagemMapa>();

        // camera filha do jogador (aula "Carregar objetos": transform.parent)
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(jogador.transform, false);
        camGo.transform.localPosition = new Vector3(0, 0.75f, -10);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 6;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(62, 137, 72, 255);

        // texto de aviso (aula "Apresentacao de textos")
        Text aviso = Canvas(cam, "TXT_AVISO", "", 34, new Vector2(0, 60), TextAnchor.LowerCenter, new Vector2(0.5f, 0));
        pm.UITextAviso = aviso;

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);

        FotoMapa(cam);

        CenasDeFase();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MenuPrincipal.unity", true),
            new EditorBuildSettingsScene(CENA, true),
            new EditorBuildSettingsScene("Assets/Scenes/fase01.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase02.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase03.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase04.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase05.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[MenuFase] construido.");
    }

    // indice da peca do 09 (0..8) para areia/borda; -1 = grama cheia
    static int IndiceAnel(int x, int y)
    {
        if (x < -1 || x > LARG || y < -1 || y > ALT) return -1;
        int col = x == -1 ? 0 : (x == LARG ? 2 : 1);
        int lin = y == ALT ? 0 : (y == -1 ? 2 : 1);
        return lin * 3 + col;
    }

    static Tilemap Camada(GameObject grid, string nome, int ordem)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(grid.transform, false);
        Tilemap t = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = ordem;
        return t;
    }

    static void Parede(GameObject pai, string nome, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai.transform);
        go.transform.position = new Vector3(x, y, 0);
        go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
    }

    static void Peca(GameObject pai, Sprite s, float x, float y)
    {
        GameObject go = new GameObject(s.name);
        go.transform.SetParent(pai.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = s; r.sortingOrder = 5;
    }

    static Text Canvas(Camera cam, string nome, string texto, int tam, Vector2 pos, TextAnchor ancora, Vector2 ancoraRect)
    {
        GameObject cv = new GameObject("Canvas");
        Canvas c = cv.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = cam; c.planeDistance = 1;
        CanvasScaler cs = cv.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1366, 768); cs.matchWidthOrHeight = 0.5f;
        GameObject tg = new GameObject(nome, typeof(RectTransform));
        tg.transform.SetParent(cv.transform, false);
        RectTransform rt = tg.GetComponent<RectTransform>();
        rt.anchorMin = ancoraRect; rt.anchorMax = ancoraRect; rt.pivot = ancoraRect;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1200, 300);
        Text t = tg.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.text = texto; t.fontSize = tam; t.fontStyle = FontStyle.Bold;
        t.alignment = ancora; t.color = Color.white;
        Outline o = tg.AddComponent<Outline>();
        o.effectColor = Color.black; o.effectDistance = new Vector2(3, -3);
        return t;
    }

    static void CenasDeFase()
    {
        for (int f = 1; f <= 5; f++)
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(40, 40, 48, 255);
            Canvas(cam, "TXT_FASE", "FASE " + f + "\n(em construção)\n\naperte ENTER para voltar ao mapa", 44, Vector2.zero, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
            new GameObject("Fase").AddComponent<VoltarAoMapa>();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/fase0" + f + ".unity");
        }
        EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);
    }

    // ---------------- personagem

    static void PrepararFolha()
    {
        AssetDatabase.ImportAsset(FOLHA, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(FOLHA);
        TextureImporterSettings cfg = new TextureImporterSettings();
        imp.ReadTextureSettings(cfg);
        cfg.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(cfg);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        List<SpriteMetaData> l = new List<SpriteMetaData>();
        for (int i = 0; i < 15; i++)
        {
            SpriteMetaData m = new SpriteMetaData();
            m.name = "_personagem_frames_" + i;
            m.rect = new Rect((i % 3) * 16, 120 - (i / 3 + 1) * 24, 16, 24);
            m.alignment = (int)SpriteAlignment.BottomCenter;   // pivo nos pes
            m.pivot = new Vector2(0.5f, 0f);
            l.Add(m);
        }
        imp.spritesheet = l.ToArray();
        imp.SaveAndReimport();
    }

    static AnimatorController Animacoes(Sprite[] q)
    {
        if (Directory.Exists(ANIM)) AssetDatabase.DeleteAsset(ANIM.TrimEnd('/'));
        AssetDatabase.CreateFolder("Assets/MenuFase_Tiles", "Animacao");
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ANIM + "Personagem.controller");
        c.AddParameter("andando", AnimatorControllerParameterType.Bool);
        c.AddParameter("frente", AnimatorControllerParameterType.Bool);
        c.AddParameter("lado", AnimatorControllerParameterType.Bool);
        c.AddParameter("costas", AnimatorControllerParameterType.Bool);
        AnimatorControllerParameter[] ps = c.parameters;
        ps[1].defaultBool = true;   // comeca virado de frente
        c.parameters = ps;

        AnimatorStateMachine m = c.layers[0].stateMachine;
        // [passo, PARADO, passo]: o quadro do meio e' a pose parada
        Estado(m, "Parado_frente", Clipe("Parado_frente", q[1]), "frente", false, true);
        Estado(m, "andando_frente", Clipe("andando_frente", q[0], q[1], q[2], q[1]), "frente", true, false);
        Estado(m, "Parado_lado", Clipe("Parado_lado", q[7]), "lado", false, false);
        Estado(m, "andando_lado", Clipe("andando_lado", q[6], q[7], q[8], q[7]), "lado", true, false);
        Estado(m, "Parado_costas", Clipe("Parado_costas", q[13]), "costas", false, false);
        Estado(m, "andando_costas", Clipe("andando_costas", q[12], q[13], q[14], q[13]), "costas", true, false);
        return c;
    }

    static void Estado(AnimatorStateMachine m, string nome, AnimationClip clipe, string direcao, bool andando, bool inicial)
    {
        AnimatorState s = m.AddState(nome);
        s.motion = clipe;
        if (inicial) m.defaultState = s;
        AnimatorStateTransition t = m.AddAnyStateTransition(s);
        t.hasExitTime = false; t.duration = 0; t.canTransitionToSelf = false;
        t.AddCondition(andando ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "andando");
        t.AddCondition(AnimatorConditionMode.If, 0, direcao);
    }

    static AnimationClip Clipe(string nome, params Sprite[] quadros)
    {
        AnimationClip a = new AnimationClip();
        a.frameRate = 12;   // "Samples: 12" como na aula de animacoes
        EditorCurveBinding b = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] k = new ObjectReferenceKeyframe[quadros.Length + 1];
        for (int i = 0; i <= quadros.Length; i++)
        {
            k[i].time = i / 12f;
            k[i].value = quadros[i % quadros.Length];   // ultima chave repete a primeira: fecha o loop
        }
        AnimationUtility.SetObjectReferenceCurve(a, b, k);
        AnimationClipSettings cfg = AnimationUtility.GetAnimationClipSettings(a);
        cfg.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(a, cfg);
        AssetDatabase.CreateAsset(a, ANIM + nome + ".anim");
        return a;
    }

    // ---------------- utilitarios

    static Tile NovoTile(string nome, Sprite s)
    {
        string p = GERADOS + nome + ".asset";
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(p);
        if (t == null) { t = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(t, p); }
        t.sprite = s; t.color = Color.white; t.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(t);
        return t;
    }

    static Sprite[] Sprites(string caminho)
    {
        string nome = Path.GetFileNameWithoutExtension(caminho);
        SortedDictionary<int, Sprite> d = new SortedDictionary<int, Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
        {
            Sprite s = o as Sprite;
            if (s == null) continue;
            int i;
            if (s.name == nome) d[0] = s;
            else if (int.TryParse(s.name.Substring(nome.Length + 1), out i)) d[i] = s;
        }
        Sprite[] r = new Sprite[d.Count];
        d.Values.CopyTo(r, 0);
        return r;
    }

    // ---------------- screenshots para conferir

    const string FOTOS = "C:/Users/LATAP_~1/AppData/Local/Temp/claude/C--Users-latap-slhppb1-Desktop-JogoFeira2/d0210f5f-f412-43a1-ae90-33a45982eab6/scratchpad/";

    static void FotoMapa(Camera camJogador)
    {
        Foto(camJogador, 1366, 768, FOTOS + "foto_jogador.png");

        GameObject g = new GameObject("CamFoto");
        Camera c = g.AddComponent<Camera>();
        c.orthographic = true; c.orthographicSize = ALT / 2f + 3;
        c.transform.position = new Vector3(LARG / 2f, ALT / 2f, -10);
        c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = Color.magenta;
        Foto(c, 1600, 1000, FOTOS + "foto_mapa.png");
        Object.DestroyImmediate(g);
    }

    static void Foto(Camera c, int w, int h, string arquivo)
    {
        RenderTexture rt = new RenderTexture(w, h, 24);
        c.targetTexture = rt;
        c.Render();
        RenderTexture.active = rt;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        t.Apply();
        File.WriteAllBytes(arquivo, t.EncodeToPNG());
        c.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(t);
    }
}
