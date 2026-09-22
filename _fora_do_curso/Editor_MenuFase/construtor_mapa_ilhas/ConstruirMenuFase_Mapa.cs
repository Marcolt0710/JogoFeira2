using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// FERRAMENTA TEMPORARIA (fora do curso): desenho do mapa.
public static partial class ConstruirMenuFase
{
    public const string CENA = "Assets/MenuDeFase/menudefase.unity";
    const string TERRENO = "Assets/MenuFase_Tiles/Terreno/";
    public static readonly Color32 AGUA = new Color32(0, 149, 233, 255);

    // limites da agua com colisao
    const int X0 = -14, X1 = 112, Y0 = -12, Y1 = 50;

    // terra: retangulos {xmin, ymin, xmax, ymax} (inclusivos)
    static readonly int[,] TERRA = {
        // ilha A - vila (fase 1)
        { 2, 4, 30, 26 }, { 6, 0, 22, 4 }, { -2, 10, 2, 18 }, { 10, 26, 20, 29 },
        // ilha B - fazenda do ferreiro (fase 2)
        { 37, 6, 62, 30 }, { 41, 30, 55, 34 }, { 44, 2, 56, 6 },
        // ilha C - igreja (fase 3)
        { 68, 6, 92, 28 }, { 72, 28, 88, 32 }, { 90, 12, 95, 20 },
        // ilhotas
        { 31, 21, 34, 24 }, { 59, 35, 62, 38 }, { 97, 26, 100, 29 }, { -7, 24, -4, 27 }, { 64, 1, 67, 4 },
    };

    // pontes (faixas andaveis sobre a agua) {xmin, ymin, xmax, ymax}
    static readonly int[,] PONTES = { { 30, 12, 37, 13 }, { 62, 18, 68, 19 } };

    // trilhas de areia
    static readonly int[,] TRILHAS = {
        // A
        { 14, 2, 15, 11 }, { 10, 10, 19, 15 }, { 6, 12, 9, 13 }, { 6, 12, 7, 16 },
        { 20, 12, 30, 13 }, { 22, 14, 23, 17 },
        // B
        { 37, 12, 45, 13 }, { 44, 12, 45, 19 }, { 44, 18, 62, 19 }, { 47, 18, 48, 20 },
        { 40, 18, 43, 19 },
        // C
        { 68, 18, 83, 19 }, { 82, 18, 83, 20 }, { 71, 18, 72, 20 },
    };

    static bool[,] terra, trilha, ponte;
    static int W, H;

    static bool Em(bool[,] m, int x, int y)
    {
        int i = x - X0, j = y - Y0;
        if (i < 0 || j < 0 || i >= W || j >= H) return false;
        return m[i, j];
    }

    static bool[,] Mascara(int[,] r)
    {
        bool[,] m = new bool[W, H];
        for (int k = 0; k < r.GetLength(0); k++)
            for (int x = r[k, 0]; x <= r[k, 2]; x++)
                for (int y = r[k, 1]; y <= r[k, 3]; y++)
                    m[x - X0, y - Y0] = true;
        return m;
    }

    static bool Agua(int x, int y) { return !Em(terra, x, y); }

    // borda = terra encostada na agua (8 vizinhos)
    static bool Borda(int x, int y)
    {
        if (Agua(x, y)) return false;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (Agua(x + dx, y + dy)) return true;
        return false;
    }

    // Escolhe a peca de grama conforme os vizinhos "outros" (agua ou areia).
    // Anel = bloco 3x3 do buraco; ilha = bloco 2x2 dos cantos convexos.
    // Retorna 0..8 = anel, 10..13 = ilha, -1 = grama cheia.
    delegate bool Teste(int x, int y);
    static int Peca(int x, int y, Teste o)
    {
        bool n = o(x, y + 1), s = o(x, y - 1), e = o(x + 1, y), w = o(x - 1, y);
        if (n && w) return 10; if (n && e) return 11;
        if (s && w) return 12; if (s && e) return 13;
        if (n) return 7; if (s) return 1; if (w) return 5; if (e) return 3;
        if (o(x - 1, y + 1)) return 8; if (o(x + 1, y + 1)) return 6;
        if (o(x - 1, y - 1)) return 2; if (o(x + 1, y - 1)) return 0;
        return -1;
    }

    [MenuItem("MenuFase/2 Montar mapa")]
    public static void Montar()
    {
        W = X1 - X0 + 1; H = Y1 - Y0 + 1;
        terra = Mascara(TERRA); trilha = Mascara(TRILHAS); ponte = Mascara(PONTES);

        if (!AssetDatabase.IsValidFolder("Assets/MenuFase_Tiles/Terreno")) AssetDatabase.CreateFolder("Assets/MenuFase_Tiles", "Terreno");

        Tile grama = T("Grama", S("Grass_1_Middle", 0));
        Tile areia = T("Areia", S("09_autotile_grama_areia", 4));
        Tile[] anelAgua = new Tile[9], anelAreia = new Tile[9], ilhaAgua = new Tile[4], ilhaAreia = new Tile[4];
        for (int i = 0; i < 9; i++) { anelAgua[i] = T("BordaAgua_" + i, S("02_autotile_grama_borda_terra", i)); anelAreia[i] = T("BordaAreia_" + i, S("09_autotile_grama_areia", i)); }
        for (int i = 0; i < 4; i++) { ilhaAgua[i] = T("CantoAgua_" + i, S("04_ilha_grama_borda_terra", i)); ilhaAreia[i] = T("CantoAreia_" + i, S("13_ilha_grama_sobre_areia", i)); }
        Tile[] pedrinhas = { T("Areia_Pedras_0", S("Path_Decoration", 0)), T("Areia_Pedras_1", S("Path_Decoration", 1)), T("Areia_Pedras_2", S("Path_Decoration", 2)) };
        Tile[] tufos = { T("Tufo_1", S("15_detalhe_grama_1", 0)), T("Tufo_2", S("16_detalhe_grama_2", 0)), T("Tufo_3", S("17_detalhe_grama_3", 0)) };
        Tile colisao = T("Colisao", S("Grass_1_Middle", 0));
        colisao.colliderType = Tile.ColliderType.Grid;

        Scene cena = EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);
        foreach (GameObject g in cena.GetRootGameObjects()) Object.DestroyImmediate(g);

        GameObject grid = new GameObject("Grid");
        grid.AddComponent<Grid>();
        Tilemap tmAreia = Camada(grid, "Areia", 0);
        Tilemap tmGrama = Camada(grid, "Grama", 1);
        Tilemap tmDeco = Camada(grid, "Decoracao", 2);
        Tilemap tmCol = Camada(grid, "Colisao", 0);
        tmCol.GetComponent<TilemapRenderer>().enabled = false;
        Rigidbody2D rb = tmCol.gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D cc = tmCol.gameObject.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        tmCol.gameObject.AddComponent<TilemapCollider2D>().usedByComposite = true;

        Random.InitState(2026);
        Teste ehAgua = Agua;
        Teste ehAreia = (x, y) => Em(trilha, x, y) && !Agua(x, y);

        for (int x = X0; x <= X1; x++)
            for (int y = Y0; y <= Y1; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                bool emPonte = Em(ponte, x, y);
                if (Agua(x, y))
                {
                    if (!emPonte) tmCol.SetTile(p, colisao);
                    continue;
                }
                bool borda = Borda(x, y);
                bool areiaAqui = Em(trilha, x, y);
                if (borda && !emPonte) tmCol.SetTile(p, colisao);

                if (!borda) tmAreia.SetTile(p, (areiaAqui && Random.value < 0.18f) ? pedrinhas[Random.Range(0, 3)] : areia);
                if (areiaAqui) continue;

                int k = borda ? Peca(x, y, ehAgua) : Peca(x, y, ehAreia);
                Tile[] anel = borda ? anelAgua : anelAreia;
                Tile[] ilha = borda ? ilhaAgua : ilhaAreia;
                if (k < 0) tmGrama.SetTile(p, grama);
                else if (k >= 10) tmGrama.SetTile(p, ilha[k - 10]);
                else tmGrama.SetTile(p, anel[k]);

                if (k < 0 && Random.value < 0.06f) tmDeco.SetTile(p, tufos[Random.Range(0, 3)]);
            }

        MontarObjetos();

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Fotos();
        Debug.Log("[MenuFase] mapa montado.");
    }

    static Tilemap Camada(GameObject grid, string nome, int ordem)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(grid.transform, false);
        Tilemap t = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = ordem;
        return t;
    }

    // ---------------------------------------------------------------- assets

    static Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    public static Sprite S(string arq, int i)
    {
        Sprite[] v;
        if (!cache.TryGetValue(arq, out v))
        {
            SortedDictionary<int, Sprite> d = new SortedDictionary<int, Sprite>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ARTE + arq + ".png"))
            {
                Sprite s = o as Sprite; if (s == null) continue;
                int n;
                if (s.name == arq) d[0] = s;
                else if (s.name.Length > arq.Length + 1 && int.TryParse(s.name.Substring(arq.Length + 1), out n)) d[n] = s;
            }
            v = new Sprite[d.Count == 0 ? 0 : (new List<int>(d.Keys))[d.Count - 1] + 1];
            foreach (KeyValuePair<int, Sprite> kv in d) v[kv.Key] = kv.Value;
            cache[arq] = v;
        }
        if (i >= v.Length || v[i] == null) { Debug.LogError("[MenuFase] sprite nao existe: " + arq + "_" + i); return null; }
        return v[i];
    }

    static Tile T(string nome, Sprite s)
    {
        string p = TERRENO + nome + ".asset";
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(p);
        if (t == null) { t = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(t, p); }
        t.sprite = s; t.color = Color.white; t.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(t);
        return t;
    }

    // ---------------------------------------------------------------- fotos

    static void Fotos()
    {
        GameObject g = new GameObject("CamFoto");
        Camera c = g.AddComponent<Camera>();
        c.orthographic = true;
        c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = AGUA;
        c.orthographicSize = 28; c.transform.position = new Vector3(46.5f, 17, -10);
        Foto(c, 2560, 1280, SAIDA + "m_geral.png");
        c.orthographicSize = 9; c.transform.position = new Vector3(15, 14, -10);
        Foto(c, 1600, 900, SAIDA + "m_ilhaA.png");
        c.transform.position = new Vector3(50, 18, -10);
        Foto(c, 1600, 900, SAIDA + "m_ilhaB.png");
        c.transform.position = new Vector3(81, 18, -10);
        Foto(c, 1600, 900, SAIDA + "m_ilhaC.png");
        Object.DestroyImmediate(g);
        GameObject j = GameObject.Find("Jogador");
        if (j != null)
        {
            Camera cj = j.GetComponentInChildren<Camera>();
            if (cj != null) Foto(cj, 1366, 768, SAIDA + "m_jogador.png");
        }
    }

    static void Foto(Camera c, int w, int h, string arquivo)
    {
        RenderTexture rt = new RenderTexture(w, h, 24);
        c.targetTexture = rt; c.Render();
        RenderTexture.active = rt;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
        File.WriteAllBytes(arquivo, t.EncodeToPNG());
        c.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(t);
    }

    static partial void MontarObjetosParcial();
    static void MontarObjetos() { MontarObjetosParcial(); }
}
