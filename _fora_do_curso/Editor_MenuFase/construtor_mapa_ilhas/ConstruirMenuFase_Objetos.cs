using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// FERRAMENTA TEMPORARIA (fora do curso): casas, arvores, props, jogador, UI e cenas.
public static partial class ConstruirMenuFase
{
    const string ANIM = "Assets/MenuFase_Tiles/Animacao/";
    const string FOLHA = "Assets/MenuFase_Tiles/_personagem_frames.png";

    static bool[,] ocupado;
    static int[,] distAgua, distTerra;
    static GameObject raizCenario, raizAgua, raizArvores;

    static void Ocupar(float x0, float y0, float x1, float y1)
    {
        for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x1); x++)
            for (int y = Mathf.FloorToInt(y0); y < Mathf.CeilToInt(y1); y++)
            {
                int i = x - X0, j = y - Y0;
                if (i >= 0 && j >= 0 && i < W && j < H) ocupado[i, j] = true;
            }
    }
    static void Construcao(float x0, float y0, float x1, float y1)
    {
        Ocupar(x0, y0, x1, y1);
        construcoes.Add(Rect.MinMaxRect(x0, y0, x1, y1));
    }

    static bool Livre(int x, int y)
    {
        int i = x - X0, j = y - Y0;
        if (i < 0 || j < 0 || i >= W || j >= H) return false;
        return !ocupado[i, j];
    }

    static partial void MontarObjetosParcial()
    {
        AssetDatabase.DeleteAsset("Assets/MenuFase_Tiles/Animacao");
        AssetDatabase.CreateFolder("Assets/MenuFase_Tiles", "Animacao");
        AssetDatabase.CreateFolder("Assets/MenuFase_Tiles/Animacao", "Cenario");
        AssetDatabase.CreateFolder("Assets/MenuFase_Tiles/Animacao", "Personagem");
        ctrls.Clear();

        ocupado = new bool[W, H];
        construcoes.Clear();
        CalcularDistancias();
        // trilhas e pontes ficam livres (com 1 celula de folga)
        for (int x = X0; x <= X1; x++)
            for (int y = Y0; y <= Y1; y++)
                if (Em(trilha, x, y) || Em(ponte, x, y)) Ocupar(x - 1, y - 1, x + 2, y + 2);

        raizCenario = new GameObject("Cenario");
        raizAgua = new GameObject("Agua");
        raizArvores = new GameObject("Arvores");

        Penhascos();
        Pontes();
        Fases();
        Vila();
        Fazenda();
        Igreja();
        Barcos();
        Arvores();
        Florzinhas();
        DecoracaoAgua();
        Folhas();
        GameObject jogador = Jogador();
        Interface(jogador);
        CenasDeFase();
    }

    // ------------------------------------------------------------------ distancias

    static void CalcularDistancias()
    {
        distAgua = new int[W, H]; distTerra = new int[W, H];
        Queue<int> qa = new Queue<int>(), qt = new Queue<int>();
        for (int i = 0; i < W; i++)
            for (int j = 0; j < H; j++)
            {
                bool agua = Agua(i + X0, j + Y0);
                distAgua[i, j] = agua ? 0 : 999; distTerra[i, j] = agua ? 999 : 0;
                if (agua) qa.Enqueue(i * H + j); else qt.Enqueue(i * H + j);
            }
        Espalhar(qa, distAgua); Espalhar(qt, distTerra);
    }
    static void Espalhar(Queue<int> q, int[,] d)
    {
        while (q.Count > 0)
        {
            int k = q.Dequeue(); int i = k / H, j = k % H;
            int[] di = { 1, -1, 0, 0 }, dj = { 0, 0, 1, -1 };
            for (int n = 0; n < 4; n++)
            {
                int a = i + di[n], b = j + dj[n];
                if (a < 0 || b < 0 || a >= W || b >= H) continue;
                if (d[a, b] > d[i, j] + 1) { d[a, b] = d[i, j] + 1; q.Enqueue(a * H + b); }
            }
        }
    }
    static int DA(int x, int y) { int i = x - X0, j = y - Y0; return (i < 0 || j < 0 || i >= W || j >= H) ? 0 : distAgua[i, j]; }
    static int DT(int x, int y) { int i = x - X0, j = y - Y0; return (i < 0 || j < 0 || i >= W || j >= H) ? 999 : distTerra[i, j]; }

    // ------------------------------------------------------------------ helpers

    static GameObject Obj(GameObject pai, string nome, Sprite s, float x, float y, int ordem, float colW, float colH)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai.transform);
        go.transform.position = new Vector3(x, y, 0);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = s; r.sortingOrder = ordem;
        if (colW > 0)
        {
            BoxCollider2D c = go.AddComponent<BoxCollider2D>();
            c.size = new Vector2(colW, colH); c.offset = new Vector2(0, colH / 2f);
        }
        return go;
    }

    static int variante;
    static Dictionary<string, AnimatorController[]> ctrls = new Dictionary<string, AnimatorController[]>();

    // prop animado: um clip em loop, 3 versoes defasadas para nao piscarem juntos
    static GameObject Animado(GameObject pai, string nome, string arq, int ini, int n, int fps, float x, float y, float colW, float colH)
    {
        string chave = arq + "_" + ini;
        AnimatorController[] cs;
        if (!ctrls.TryGetValue(chave, out cs))
        {
            string pasta = ANIM + "Cenario/";
            AnimationClip clip = Clipe(pasta + nome + ".anim", fps, Quadros(arq, ini, n));
            cs = new AnimatorController[3];
            for (int v = 0; v < 3; v++)
            {
                cs[v] = AnimatorController.CreateAnimatorControllerAtPath(pasta + nome + "_" + v + ".controller");
                AnimatorState st = cs[v].layers[0].stateMachine.AddState(nome);
                st.motion = clip; st.cycleOffset = v / 3f;
            }
            ctrls[chave] = cs;
        }
        GameObject go = Obj(pai, nome, S(arq, ini), x, y, 5, colW, colH);
        Animator an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = cs[(variante++) % 3];
        return go;
    }

    static Sprite[] Quadros(string arq, int ini, int n)
    {
        Sprite[] q = new Sprite[n];
        for (int i = 0; i < n; i++) q[i] = S(arq, ini + i);
        return q;
    }

    static AnimationClip Clipe(string caminho, int fps, Sprite[] quadros)
    {
        AnimationClip a = new AnimationClip();
        a.frameRate = fps;
        EditorCurveBinding b = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] k = new ObjectReferenceKeyframe[quadros.Length + 1];
        for (int i = 0; i <= quadros.Length; i++) { k[i].time = i / (float)fps; k[i].value = quadros[i % quadros.Length]; }
        AnimationUtility.SetObjectReferenceCurve(a, b, k);
        AnimationClipSettings cfg = AnimationUtility.GetAnimationClipSettings(a);
        cfg.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(a, cfg);
        AssetDatabase.CreateAsset(a, caminho);
        return a;
    }

    // ------------------------------------------------------------------ penhascos

    // Paredao de terra sob as margens viradas para o sul, como as ilhas do Cuphead.
    // Usa a coluna do meio (e as laterais nas pontas) do corpo do 07_pilar_terra.
    static bool BeiraSul(int x, int y) { return !Agua(x, y) && Agua(x, y - 1); }

    // O paredao comeca DENTRO da celula da borda (atras da grama): a metade de baixo
    // da peca de borda e' transparente, e sem isso aparecia uma faixa de agua entre a
    // grama e o penhasco. Depois desce mais 2 tiles pela agua.
    static void Penhascos()
    {
        GameObject raiz = new GameObject("Penhascos");
        for (int x = X0; x <= X1; x++)
            for (int y = Y0; y <= Y1; y++)
            {
                if (!BeiraSul(x, y) || Em(ponte, x, y) || Em(ponte, x, y - 1)) continue;
                int col = 1;
                if (!BeiraSul(x - 1, y)) col = 0; else if (!BeiraSul(x + 1, y)) col = 2;
                string arq = (x >= 66) ? "08_pilar_pedra" : "07_pilar_terra";
                Obj(raiz, "Penhasco", S(arq, 3 * 3 + col), x + 0.5f, y + 0.5f, 0, 0, 0);
                Obj(raiz, "Penhasco", S(arq, 4 * 3 + col), x + 0.5f, y - 0.5f, 1, 0, 0);
                if (Agua(x, y - 2)) Obj(raiz, "Penhasco", S(arq, 5 * 3 + col), x + 0.5f, y - 1.5f, 1, 0, 0);
            }
    }

    // ------------------------------------------------------------------ pontes

    static void Pontes()
    {
        GameObject raiz = new GameObject("Pontes");
        Ponte(raiz, 30, 38, 12);
        Ponte(raiz, 62, 69, 18);
        // tochas grandes na cabeceira das pontes
        foreach (float px in new[] { 29.3f, 38.7f })
        {
            Animado(raizCenario, "TochaGrande", "Big_Torch_Anim", 0, 8, 12, px, 14.05f, 0.4f, 0.3f);
            Animado(raizCenario, "TochaGrande", "Big_Torch_Anim", 0, 8, 12, px, 10.6f, 0.4f, 0.3f);
        }
        foreach (float px in new[] { 61.3f, 69.7f })
        {
            Animado(raizCenario, "TochaGrande", "Big_Torch_Anim", 0, 8, 12, px, 20.05f, 0.4f, 0.3f);
            Animado(raizCenario, "TochaGrande", "Big_Torch_Anim", 0, 8, 12, px, 16.6f, 0.4f, 0.3f);
        }
    }

    static void Ponte(GameObject raiz, float xIni, float xFim, int yTrilha)
    {
        Sprite s = S("Bridge_Wood_1", 1);
        float largura = s.rect.width / 16f;
        int n = Mathf.CeilToInt((xFim - xIni) / largura);
        float inicio = (xIni + xFim) / 2f - n * largura / 2f;
        float baseY = yTrilha + 1f - s.rect.height / 32f;   // centraliza o tabuleiro nas 2 linhas da trilha
        for (int i = 0; i < n; i++)
            Obj(raiz, "Ponte", s, inicio + (i + 0.5f) * largura, baseY, 2, 0, 0);
    }

    // ------------------------------------------------------------------ fases

    static void Fases()
    {
        GameObject raiz = new GameObject("Fases");
        // x do centro da casa, y da base, deslocamento da porta, largura do colisor
        Fase(raiz, 1, S("House_1_Wood_Red_Red", 0), 7.56f, 17, -0.5625f, 5.4f, 7f);
        Fase(raiz, 2, S("Blacksmith_House_Red", 0), 50.56f, 21, -2.5625f, 9.4f, 48f);
        Fase(raiz, 3, S("Church_Blue", 2), 83f, 21, 0f, 6.4f, 83f);
    }

    static void Fase(GameObject raiz, int n, Sprite casa, float cx, float by, float porta, float colW, float xPorta)
    {
        GameObject f = Obj(raiz, "Fase" + n, casa, cx, by, 5, colW, 2.6f);
        float w = casa.rect.width / 16f, h = casa.rect.height / 16f;
        Construcao(cx - w / 2f, by - 4, cx + w / 2f, by + h);

        GameObject entrada = new GameObject("Entrada");
        entrada.transform.SetParent(f.transform, false);
        entrada.transform.localPosition = new Vector3(porta, -0.7f, 0);
        entrada.tag = "fase0" + n;
        BoxCollider2D g = entrada.AddComponent<BoxCollider2D>();
        g.isTrigger = true; g.size = new Vector2(1.8f, 1.4f);

        // marcadores de fase: tochas dos dois lados da porta, bandeirolas e placa
        Animado(raizCenario, "Tocha", "Torch_Anim", 0, 8, 12, xPorta - 1.4f, by - 0.2f, 0.4f, 0.3f);
        Animado(raizCenario, "Tocha", "Torch_Anim", 0, 8, 12, xPorta + 1.4f, by - 0.2f, 0.4f, 0.3f);
        Animado(raizCenario, "Bandeirolas", "Pole_and_Bunting_1_Anim", 0, 8, 10, xPorta, by - 2.4f, 0, 0);
        Obj(raizCenario, "Placa", S("Signs", 5), xPorta + 2.4f, by - 0.1f, 5, 0.4f, 0.3f);
    }

    // ------------------------------------------------------------------ ilha A: vila

    static void Vila()
    {
        GameObject r = raizCenario;
        Animado(r, "Fonte", "Fountain_Anim", 0, 8, 10, 15f, 12f, 1.8f, 1.0f);
        Construcao(13, 11, 17, 15);
        GameObject casa = Obj(r, "Casa", S("House_4_Wood_Green_Blue", 0), 24.06f, 18, 5, 6.4f, 2.6f);
        Construcao(20.5f, 17, 27.6f, 24);
        Obj(r, "Poco", S("Well", 0), 18.5f, 19f, 5, 1.6f, 0.8f); Construcao(17, 18, 20, 21);
        Obj(r, "Banco", S("Benches", 1), 11.5f, 16.2f, 5, 1.8f, 0.5f);
        Obj(r, "Banco", S("Benches", 1), 18.2f, 16.2f, 5, 1.8f, 0.5f);
        Obj(r, "BarrilFlor", S("barrels", 6), 20.6f, 18.1f, 5, 0.8f, 0.5f);
        Obj(r, "BarrilFlor", S("barrels", 7), 27.8f, 18.1f, 5, 0.8f, 0.5f);
        Obj(r, "Barris", S("barrels", 5), 3.8f, 17.1f, 5, 2.6f, 0.6f); Construcao(2, 16, 6, 19);
        Obj(r, "Barril", S("barrels", 2), 10.8f, 17.2f, 5, 0.8f, 0.5f);
        foreach (Vector2 p in new[] { new Vector2(9.6f, 16.1f), new Vector2(20.4f, 16.1f), new Vector2(9.6f, 9.2f), new Vector2(20.4f, 9.2f), new Vector2(13.4f, 4f), new Vector2(16.6f, 4f), new Vector2(25f, 14.2f) })
            Animado(r, "Lampiao", "Lanter_Posts", 0, 6, 8, p.x, p.y, 0.3f, 0.3f);
        Animado(r, "BandeirolasAltas", "Pole_and_Bunting_2_Anim", 0, 8, 10, 10.2f, 15.4f, 0, 0);
        Animado(r, "BandeirolasAltas", "Pole_and_Bunting_2_Anim", 0, 8, 10, 19.8f, 15.4f, 0, 0);
        Obj(r, "Cesta", S("Picnic_Basket", 0), 5.5f, 8.2f, 5, 0, 0);
    }

    // ------------------------------------------------------------------ ilha B: ferreiro e fazenda

    static void Fazenda()
    {
        GameObject r = raizCenario;
        Animado(r, "Forja", "Fireplace_Anim", 8, 8, 12, 57.6f, 21f, 1.8f, 1.0f); Construcao(56, 20, 60, 24);
        Obj(r, "Feno", S("Hay_Bales", 1), 60.3f, 21.1f, 5, 1.8f, 0.5f);

        // fogueira do acampamento, na ponta da trilha oeste
        Animado(r, "Fogueira", "Campfire_Anim", 0, 8, 12, 41.5f, 21f, 0.8f, 0.5f); Construcao(39, 20, 44, 24);
        Obj(r, "Toco", S("Camp_Decor", 2), 40f, 21.2f, 5, 0.6f, 0.4f);
        Obj(r, "Toco", S("Camp_Decor", 2), 43f, 21.2f, 5, 0.6f, 0.4f);
        Obj(r, "Tronco", S("Camp_Decor", 3), 41.5f, 22.6f, 5, 1f, 0.4f);
        Obj(r, "Lenha", S("Outdoor_Decor", 108), 39.2f, 22.4f, 5, 0.8f, 0.5f);

        // cercado da plantacao
        for (float x = 49.2f; x < 60f; x += 2.375f)
        {
            Obj(r, "Cerca", S("Fences", 1), x, 15.2f, 5, 2.3f, 0.3f);
            Obj(r, "Cerca", S("Fences", 1), x, 7.2f, 5, 2.3f, 0.3f);
        }
        Obj(r, "Cerca", S("Fences", 0), 48.1f, 8.3f, 5, 0.3f, 2.6f);
        Obj(r, "Cerca", S("Fences", 0), 48.1f, 12f, 5, 0.3f, 2.6f);
        Obj(r, "Cerca", S("Fences", 0), 61f, 8.3f, 5, 0.3f, 2.6f);
        Obj(r, "Cerca", S("Fences", 0), 61f, 12f, 5, 0.3f, 2.6f);
        Construcao(47, 6, 62, 17);
        for (int i = 0; i < 3; i++) Obj(r, "Espantalho", S("Scarecrows", i * 2), 51f + i * 3.5f, 11f, 5, 0.5f, 0.4f);
        Obj(r, "Feno", S("Hay_Bales", 0), 50f, 8.4f, 5, 0.8f, 0.5f);
        Obj(r, "Feno", S("Hay_Bales", 1), 58.5f, 8.4f, 5, 1.8f, 0.5f);
        Obj(r, "Feno", S("Hay_Bales", 0), 54.5f, 13.4f, 5, 0.8f, 0.5f);
        Obj(r, "Cocho", S("Water_Troughs", 1), 44.5f, 9.6f, 5, 1.8f, 0.5f); Construcao(43, 9, 47, 11);
        Obj(r, "Barril", S("barrels", 0), 47.5f, 16.4f, 5, 0.8f, 0.5f);
        Obj(r, "Barril", S("barrels", 3), 46.6f, 16.1f, 5, 0.8f, 0.5f);
        foreach (Vector2 p in new[] { new Vector2(52f, 17.1f), new Vector2(58f, 17.1f), new Vector2(43.4f, 15f) })
            Animado(r, "Lampiao", "Lanter_Posts", 0, 6, 8, p.x, p.y, 0.3f, 0.3f);
    }

    // ------------------------------------------------------------------ ilha C: igreja

    static void Igreja()
    {
        GameObject r = raizCenario;
        Obj(r, "Casa", S("House_2_Wood_Red_Blue", 0), 73.5f, 21, 5, 8.4f, 2.6f); Construcao(69, 20, 78, 30);
        for (float x = 70f; x < 83f; x += 4f) Animado(r, "Lampiao", "Lanter_Posts", 0, 6, 8, x, 17.1f, 0.3f, 0.3f);
        Obj(r, "Banco", S("Benches", 0), 80.2f, 17.3f, 5, 1.8f, 0.5f);
        Obj(r, "Banco", S("Benches", 0), 86.6f, 17.3f, 5, 1.8f, 0.5f);
        // jardim ao sul da igreja
        Random.InitState(3);
        int[] canteiro = { 137, 144, 138, 145, 139, 146, 137, 144 };
        for (int i = 0; i < 26; i++)
            Obj(r, "Flor", S("Outdoor_Decor", canteiro[Random.Range(0, canteiro.Length)]), Random.Range(76f, 89f), Random.Range(8.5f, 14.5f), 5, 0, 0);
        for (int i = 0; i < 4; i++)
            Obj(r, "Arbusto", S("Outdoor_Decor", new[] { 62, 91, 93 }[i % 3]), 77f + i * 3.5f, 15.2f, 5, 0.8f, 0.4f);
        Construcao(75, 8, 90, 16);
        Obj(r, "BarrilFlor", S("barrels", 8), 79.2f, 21.3f, 5, 0.8f, 0.5f);
        Obj(r, "BarrilFlor", S("barrels", 6), 86.8f, 21.3f, 5, 0.8f, 0.5f);
    }

    // ------------------------------------------------------------------ agua

    static void Barcos()
    {
        foreach (Vector2 p in new[] { new Vector2(-4f, 7f), new Vector2(33.5f, 2f), new Vector2(64f, 32f), new Vector2(98f, 16f), new Vector2(28f, 32f) })
        {
            GameObject b = Animado(raizAgua, "Barco", "Boat_Anim", 0, 4, 5, p.x, p.y, 0, 0);
            b.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
    }

    static void DecoracaoAgua()
    {
        int[] lirios = { 47, 52, 43, 48, 116, 117, 118, 119, 121, 122, 123, 125, 126, 127 };
        int[] pedras = { 53, 55, 56, 57, 66, 67, 74, 61 };
        int[] juncos = { 80, 86, 87, 131, 132, 133, 134, 135, 140, 143, 147 };
        Random.InitState(77);
        for (int x = X0 + 2; x < X1 - 2; x++)
            for (int y = Y0 + 2; y < Y1 - 2; y++)
            {
                if (!Agua(x, y) || Em(ponte, x, y) || Em(ponte, x, y + 1) || Em(ponte, x, y - 1) || Em(ponte, x, y + 2) || Em(ponte, x, y - 2)) continue;
                if (!Agua(x, y + 1) || !Agua(x, y + 2) || !Agua(x, y + 3)) continue;   // nao em cima do penhasco
                int d = DT(x, y);
                // juncos colados na margem; vitorias-regias em grupinhos perto dela;
                // uma pedra ou outra um pouco mais longe. Nada no mar aberto.
                if (d == 1 && Random.value < 0.22f)
                    Obj(raizAgua, "Junco", S("Outdoor_Decor", juncos[Random.Range(0, juncos.Length)]), x + 0.5f, y + 0.1f, 1, 0, 0);
                else if (d == 2 && Random.value < 0.07f)
                {
                    int grupo = Random.Range(2, 4);
                    for (int g = 0; g < grupo; g++)
                        Obj(raizAgua, "VitoriaRegia", S("Outdoor_Decor", lirios[Random.Range(0, lirios.Length)]), x + Random.Range(-0.6f, 1.4f), y + Random.Range(-0.5f, 0.7f), 1, 0, 0);
                }
                else if (d >= 2 && d <= 4 && Random.value < 0.012f)
                    Obj(raizAgua, "Pedra", S("Outdoor_Decor", pedras[Random.Range(0, pedras.Length)]), x + 0.5f, y, 1, 0, 0);
            }
    }

    // ------------------------------------------------------------------ arvores

    struct TipoArvore { public string arq; public int w, h; public TipoArvore(string a, int w, int h) { arq = a; this.w = w; this.h = h; } }

    // Bosques densos nas margens, como no Cuphead: arvores grandes em volta da
    // ilha e poucas soltas no meio. O tronco nao pode ficar ate 5 tiles abaixo de
    // trilha ou construcao, senao a copa (que sobe 5 tiles) cobre o caminho.
    static void Arvores()
    {
        TipoArvore[] borda = {
            new TipoArvore("Big_Oak_Tree", 2, 1), new TipoArvore("Big_Oak_Tree", 2, 1), new TipoArvore("Big_Oak_Tree", 2, 1),
            new TipoArvore("Big_Spruce_tree", 2, 1), new TipoArvore("Big_Spruce_tree", 2, 1), new TipoArvore("Big_Fruit_Tree", 2, 1),
            new TipoArvore("Medium_Oak_Tree", 2, 1), new TipoArvore("Medium_Spruce_Tree", 2, 1), new TipoArvore("Big_Birch_Tree", 2, 1) };
        TipoArvore[] meio = {
            new TipoArvore("Big_Oak_Tree", 2, 1), new TipoArvore("Medium_Fruit_Tree", 2, 1), new TipoArvore("Medium_Oak_Tree", 2, 1),
            new TipoArvore("Small_Oak_Tree", 1, 1), new TipoArvore("Medium_Birch_Tree", 2, 1) };

        bool[,] semCopa = (bool[,])ocupado.Clone();
        for (int x = X0; x <= X1; x++)
            for (int y = Y0; y <= Y1; y++)
                if (Em(trilha, x, y) || Em(ponte, x, y) || EhConstrucao(x, y))
                    for (int dx = -2; dx <= 1; dx++) for (int dy = -4; dy <= 0; dy++)
                        { int i = x + dx - X0, j = y + dy - Y0; if (i >= 0 && j >= 0 && i < W && j < H) semCopa[i, j] = true; }

        Random.InitState(11);
        for (int passo = 0; passo < 4; passo++)
            for (int y = Y1; y >= Y0; y--)
                for (int x = X0; x <= X1; x++)
                {
                    int d = DA(x, y);
                    if (d < 2) continue;
                    bool naBorda = d <= 4;
                    float p = naBorda ? 0.9f : (d <= 7 ? 0.35f : 0.14f);
                    if (passo > 0) p *= 0.85f;
                    if (Random.value > p) continue;
                    TipoArvore t = naBorda ? borda[Random.Range(0, borda.Length)] : meio[Random.Range(0, meio.Length)];
                    bool ok = true;
                    for (int dx = -1; dx <= t.w && ok; dx++)
                    {
                        int i = x + dx - X0, j = y - Y0;
                        if (i < 0 || j < 0 || i >= W || j >= H || semCopa[i, j] || ocupado[i, j] || Agua(x + dx, y) || DA(x + dx, y) < 2) ok = false;
                    }
                    if (!ok) continue;
                    Obj(raizArvores, t.arq, S(t.arq, 1), x + t.w / 2f, y + Random.value * 0.4f, 5, 0.7f, 0.4f);
                    Ocupar(x, y - 1, x + t.w, y + 2);   // 1 linha de folga acima e abaixo do tronco
                }
    }

    static List<Rect> construcoes = new List<Rect>();
    static bool EhConstrucao(int x, int y)
    {
        foreach (Rect r in construcoes) if (r.Contains(new Vector2(x + 0.5f, y + 0.5f))) return true;
        return false;
    }

    static void Florzinhas()
    {
        int[] od = { 4, 5, 6, 7, 16, 17, 18, 19, 32, 33, 20, 21, 22, 27, 44, 49, 50, 51, 62, 91, 93, 68, 69, 29 };
        Random.InitState(5);
        GameObject raiz = new GameObject("Flores");
        for (int x = X0; x <= X1; x++)
            for (int y = Y0; y <= Y1; y++)
            {
                if (Agua(x, y) || DA(x, y) < 2 || !Livre(x, y) || Random.value > 0.12f) continue;
                float sorteio = Random.value;
                if (sorteio < 0.14f)
                {
                    // peca maior e baixa (arbusto, pedra, toco, tronco): tem colisor
                    int[] medios = { 62, 91, 93, 62, 91, 70, 73, 100, 92, 78, 79, 97, 98, 29 };
                    Obj(raiz, "Moita", S("Outdoor_Decor", medios[Random.Range(0, medios.Length)]), x + 0.5f, y + 0.1f, 5, 0.8f, 0.4f);
                    Ocupar(x - 1, y - 1, x + 2, y + 2);
                    continue;
                }
                Sprite s = sorteio < 0.5f ? S("Flowers", Random.Range(0, 10) * 10 + Random.Range(0, 5)) : S("Outdoor_Decor", od[Random.Range(0, od.Length)]);
                Obj(raiz, "Flor", s, x + 0.2f + Random.value * 0.6f, y + Random.value * 0.4f, 5, 0, 0);
            }
    }

    // folhas caindo das arvores grandes: animacao de posicao e transparencia (sem codigo)
    static void Folhas()
    {
        GameObject raiz = new GameObject("FolhasCaindo");
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 12;
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0.5f), new Keyframe(2, -0.2f), new Keyframe(3, 0.4f), new Keyframe(4, 0.1f)));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", AnimationCurve.Linear(0, 0, 4, -2.5f));
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(3.2f, 1), new Keyframe(4, 0)));
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.r", AnimationCurve.Constant(0, 4, 1));
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.g", AnimationCurve.Constant(0, 4, 1));
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.b", AnimationCurve.Constant(0, 4, 1));
        AnimationClipSettings cfg = AnimationUtility.GetAnimationClipSettings(clip); cfg.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, cfg);
        AssetDatabase.CreateAsset(clip, ANIM + "Cenario/FolhaCaindo.anim");
        AnimatorController[] cs = new AnimatorController[4];
        for (int v = 0; v < 4; v++)
        {
            cs[v] = AnimatorController.CreateAnimatorControllerAtPath(ANIM + "Cenario/FolhaCaindo_" + v + ".controller");
            AnimatorState st = cs[v].layers[0].stateMachine.AddState("Cair"); st.motion = clip; st.cycleOffset = v / 4f;
        }
        int n = 0;
        foreach (Transform arv in raizArvores.transform)
        {
            if (!arv.name.StartsWith("Big_") || (n++ % 3) != 0) continue;
            GameObject pai = new GameObject("Folha");
            pai.transform.SetParent(raiz.transform);
            pai.transform.position = arv.position + new Vector3(0.3f, 3.2f, 0);
            string arq = arv.name.Contains("Birch") ? "Birch_Leaf_Particle" : (arv.name.Contains("Spruce") ? "Spruce_Needle_Particle" : "Oak_Leaf_Particle");
            GameObject f = Obj(pai, "Folha", S(arq, 0), 0, 0, 6, 0, 0);
            f.transform.SetParent(pai.transform, false); f.transform.localPosition = Vector3.zero;
            f.AddComponent<Animator>().runtimeAnimatorController = cs[n % 4];
        }
    }

    // ------------------------------------------------------------------ jogador

    static GameObject Jogador()
    {
        Sprite[] q = QuadrosPersonagem();
        AnimatorController ctrl = AnimJogador(q);

        GameObject j = new GameObject("Jogador");
        j.transform.position = new Vector3(15f, 3f, 0);
        SpriteRenderer sr = j.AddComponent<SpriteRenderer>(); sr.sprite = q[1]; sr.sortingOrder = 5;
        Rigidbody2D rb = j.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D pes = j.AddComponent<BoxCollider2D>();
        pes.offset = new Vector2(0, 0.2f); pes.size = new Vector2(0.6f, 0.4f);
        j.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        j.AddComponent<PersonagemMapa>();

        GameObject cg = new GameObject("Main Camera"); cg.tag = "MainCamera";
        cg.transform.SetParent(j.transform, false); cg.transform.localPosition = new Vector3(0, 0.75f, -10);
        Camera cam = cg.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 6;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = AGUA;
        return j;
    }

    // a folha do personagem esta em MenuFase_Tiles, fora de ARTE
    static Sprite[] QuadrosPersonagem()
    {
        SortedDictionary<int, Sprite> d = new SortedDictionary<int, Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(FOLHA))
        {
            Sprite s = o as Sprite; int n;
            if (s != null && int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out n)) d[n] = s;
        }
        Sprite[] r = new Sprite[15]; foreach (KeyValuePair<int, Sprite> kv in d) r[kv.Key] = kv.Value; return r;
    }

    static AnimatorController AnimJogador(Sprite[] ignorado)
    {
        Sprite[] q = QuadrosPersonagem();
        string p = ANIM + "Personagem/";
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ANIM + "Personagem.controller");
        foreach (string b in new[] { "andando", "frente", "lado", "costas" }) c.AddParameter(b, AnimatorControllerParameterType.Bool);
        AnimatorControllerParameter[] ps = c.parameters; ps[1].defaultBool = true; c.parameters = ps;
        AnimatorStateMachine m = c.layers[0].stateMachine;

        // cada linha da folha: passo, PARADO, passo
        string[] nomes = { "frente", "diagonal_frente", "lado", "diagonal_costas", "costas" };
        for (int d = 0; d < 5; d++)
        {
            int a = d * 3;
            AnimationClip parado = Clipe(p + "Parado_" + nomes[d] + ".anim", 12, new[] { q[a + 1] });
            AnimationClip andando = Clipe(p + "andando_" + nomes[d] + ".anim", 12, new[] { q[a], q[a + 1], q[a + 2], q[a + 1] });
            Estado(m, "Parado_" + nomes[d], parado, d, false, d == 0);
            Estado(m, "andando_" + nomes[d], andando, d, true, false);
        }
        return c;
    }

    static void Estado(AnimatorStateMachine m, string nome, AnimationClip clip, int dir, bool andando, bool inicial)
    {
        AnimatorState s = m.AddState(nome); s.motion = clip;
        if (inicial) m.defaultState = s;
        AnimatorStateTransition t = m.AddAnyStateTransition(s);
        t.hasExitTime = false; t.duration = 0; t.canTransitionToSelf = false;
        t.AddCondition(andando ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "andando");
        // frente: frente e nao lado | diag frente: frente e lado | lado: lado, sem frente/costas
        // diag costas: costas e lado | costas: costas e nao lado
        switch (dir)
        {
            case 0: t.AddCondition(AnimatorConditionMode.If, 0, "frente"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "lado"); break;
            case 1: t.AddCondition(AnimatorConditionMode.If, 0, "frente"); t.AddCondition(AnimatorConditionMode.If, 0, "lado"); break;
            case 2: t.AddCondition(AnimatorConditionMode.If, 0, "lado"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "frente"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "costas"); break;
            case 3: t.AddCondition(AnimatorConditionMode.If, 0, "costas"); t.AddCondition(AnimatorConditionMode.If, 0, "lado"); break;
            case 4: t.AddCondition(AnimatorConditionMode.If, 0, "costas"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "lado"); break;
        }
    }

    // ------------------------------------------------------------------ interface e cenas

    static void Interface(GameObject jogador)
    {
        Camera cam = jogador.GetComponentInChildren<Camera>();
        Text aviso = Texto(cam, "TXT_AVISO", "", 36, new Vector2(0, 50), TextAnchor.LowerCenter, new Vector2(0.5f, 0));
        jogador.GetComponent<PersonagemMapa>().UITextAviso = aviso;
    }

    static Text Texto(Camera cam, string nome, string texto, int tam, Vector2 pos, TextAnchor ancora, Vector2 ar)
    {
        GameObject cv = new GameObject("Canvas");
        Canvas c = cv.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 1;
        CanvasScaler cs = cv.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1366, 768); cs.matchWidthOrHeight = 0.5f;
        GameObject tg = new GameObject(nome, typeof(RectTransform)); tg.transform.SetParent(cv.transform, false);
        RectTransform rt = tg.GetComponent<RectTransform>();
        rt.anchorMin = ar; rt.anchorMax = ar; rt.pivot = ar; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1300, 300);
        Text t = tg.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.text = texto; t.fontSize = tam; t.fontStyle = FontStyle.Bold;
        t.alignment = ancora; t.color = Color.white; t.lineSpacing = 1.1f;
        Outline o = tg.AddComponent<Outline>(); o.effectColor = new Color(0.1f, 0.06f, 0.02f); o.effectDistance = new Vector2(3, -3);
        return t;
    }

    static void CenasDeFase()
    {
        // (a cena do mapa ja foi salva antes)
    }

    [MenuItem("MenuFase/3 Cenas de fase")]
    public static void CriarCenasDeFase()
    {
        string[] nomes = { "Casa da Vila", "Ferreiro", "Igreja" };
        for (int f = 1; f <= 3; f++)
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cg.transform.position = new Vector3(0, 0, -10);
            Camera cam = cg.AddComponent<Camera>(); cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(40, 40, 48, 255);
            Texto(cam, "TXT_FASE", "FASE " + f + "\n" + nomes[f - 1] + "\n\n(em construção)\n\naperte  B  para voltar ao mapa", 44, Vector2.zero, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
            new GameObject("Fase").AddComponent<VoltarAoMapa>();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/fase0" + f + ".unity");
        }
        foreach (int f in new[] { 4, 5 }) AssetDatabase.DeleteAsset("Assets/Scenes/fase0" + f + ".unity");
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MenuPrincipal.unity", true),
            new EditorBuildSettingsScene(CENA, true),
            new EditorBuildSettingsScene("Assets/Scenes/fase01.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase02.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/fase03.unity", true) };
        AssetDatabase.SaveAssets();
    }
}
