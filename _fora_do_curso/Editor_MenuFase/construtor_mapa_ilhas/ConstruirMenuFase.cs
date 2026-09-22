using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// FERRAMENTA TEMPORARIA (fora do curso): prepara os assets e monta a cena.
// Sai do projeto quando terminar. Nao faz parte do jogo.
public static partial class ConstruirMenuFase
{
    public const string ARTE = "Assets/MenuDeFase/";
    public const string SAIDA = "C:/Users/LATAP_~1/AppData/Local/Temp/claude/C--Users-latap-slhppb1-Desktop-JogoFeira2/d0210f5f-f412-43a1-ae90-33a45982eab6/scratchpad/";

    // ------------------------------------------------------------------ fatiamento

    static readonly string[] AUTO = {
        "Outdoor_Decor", "Signs", "barrels", "Benches", "Hay_Bales", "Water_Troughs", "Camp_Decor",
        "Fences", "White_Fence", "Stone_Fence_Small", "Stone_Fence_Big", "Fence_Big",
        "Bridge_Wood", "Bridge_Wood_1", "Nests", "Picnic_Basket", "Cave_Decorations", "Ores", "Fountain" };

    static readonly string[] UNICO = {
        "Well", "Lantern", "Boat", "Birch_Leaf_Particle", "Oak_Leaf_Particle", "Spruce_Needle_Particle",
        "Blacksmith_House_Black", "Blacksmith_House_Blue", "Blacksmith_House_Red" };

    [MenuItem("MenuFase/1 Fatiar assets")]
    public static void Fatiar()
    {
        // arvores: toco | arvore com sombra | arvore sem sombra
        foreach (string t in new[] { "Big_Oak_Tree", "Big_Spruce_tree" }) Grade(t, 64, 80, 3, 1);
        foreach (string t in new[] { "Big_Birch_Tree" }) Grade(t, 32, 80, 3, 1);
        foreach (string t in new[] { "Big_Fruit_Tree", "Medium_Fruit_Tree", "Small_Birch_Tree", "Small_Fruit_Tree", "Small_Oak_Tree", "Small_Spruce_Tree" }) Grade(t, 32, 64, 3, 1);
        foreach (string t in new[] { "Medium_Birch_Tree", "Medium_Oak_Tree", "Medium_Spruce_Tree" }) Grade(t, 32, 48, 3, 1);

        // animacoes
        Grade("Big_Torch_Anim", 16, 48, 8, 1);
        Grade("Torch_Anim", 16, 32, 8, 1);
        Grade("Torch_small_anim", 16, 16, 6, 1);
        Grade("Campfire_Anim", 16, 32, 8, 1);
        Grade("Fountain_Anim", 32, 48, 8, 1);
        Grade("Fireplace_Anim", 32, 48, 8, 2);
        Grade("Boat_Anim", 48, 48, 4, 1);
        Grade("Pole_and_Bunting_1_Anim", 32, 32, 8, 1);
        Grade("Pole_and_Bunting_2_Anim", 16, 64, 8, 1);
        Grade("Lanter_Posts", 16, 48, 6, 6);

        Grade("Church_Black", 112, 144, 4, 1);
        Grade("Church_Blue", 112, 144, 4, 1);
        Grade("Church_Red", 112, 144, 4, 1);
        Grade("Scarecrows", 32, 32, 5, 1);
        Grade("Flowers", 16, 16, 10, 10);

        foreach (string f in Directory.GetFiles(ARTE, "House_*.png")) Unico(Path.GetFileNameWithoutExtension(f));
        foreach (string u in UNICO) Unico(u);

        StringBuilder log = new StringBuilder();
        foreach (string a in AUTO) Auto(a, log);
        File.WriteAllText(SAIDA + "pecas.txt", log.ToString());

        AssetDatabase.Refresh();
        Debug.Log("[MenuFase] fatiado.");
    }

    static TextureImporter Preparar(string arq, SpriteImportMode modo)
    {
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ARTE + arq + ".png");
        TextureImporterSettings cfg = new TextureImporterSettings();
        imp.ReadTextureSettings(cfg);
        cfg.spriteMeshType = SpriteMeshType.FullRect;
        cfg.spriteExtrude = 0;
        cfg.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        imp.SetTextureSettings(cfg);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = modo;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        return imp;
    }

    static void Unico(string arq)
    {
        TextureImporter imp = Preparar(arq, SpriteImportMode.Single);
        imp.spritePivot = new Vector2(0.5f, 0f);
        imp.SaveAndReimport();
    }

    static void Grade(string arq, int cw, int ch, int colunas, int linhas)
    {
        TextureImporter imp = Preparar(arq, SpriteImportMode.Multiple);
        int altura = Altura(arq);
        List<SpriteMetaData> l = new List<SpriteMetaData>();
        for (int lin = 0; lin < linhas; lin++)
            for (int col = 0; col < colunas; col++)
            {
                SpriteMetaData m = new SpriteMetaData();
                m.name = arq + "_" + (lin * colunas + col);
                m.rect = new Rect(col * cw, altura - (lin + 1) * ch, cw, ch);
                m.alignment = (int)SpriteAlignment.BottomCenter;
                m.pivot = new Vector2(0.5f, 0f);
                l.Add(m);
            }
        imp.spritesheet = l.ToArray();
        imp.SaveAndReimport();
    }

    // detecta cada desenho solto (pixels opacos ligados) e vira um sprite
    static void Auto(string arq, StringBuilder log)
    {
        Texture2D t = new Texture2D(2, 2);
        t.LoadImage(File.ReadAllBytes(ARTE + arq + ".png"));
        int w = t.width, h = t.height;
        Color32[] px = t.GetPixels32();
        bool[] visto = new bool[w * h];
        List<RectInt> caixas = new List<RectInt>();
        Queue<int> fila = new Queue<int>();
        for (int i = 0; i < px.Length; i++)
        {
            if (visto[i] || px[i].a == 0) continue;
            int xmin = w, ymin = h, xmax = -1, ymax = -1;
            fila.Enqueue(i); visto[i] = true;
            while (fila.Count > 0)
            {
                int k = fila.Dequeue(); int x = k % w, y = k / w;
                if (x < xmin) xmin = x; if (x > xmax) xmax = x; if (y < ymin) ymin = y; if (y > ymax) ymax = y;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int n = ny * w + nx;
                        if (!visto[n] && px[n].a > 0) { visto[n] = true; fila.Enqueue(n); }
                    }
            }
            caixas.Add(new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1));
        }
        // junta caixas que se sobrepoem (pedacos do mesmo desenho)
        bool mudou = true;
        while (mudou)
        {
            mudou = false;
            for (int a = 0; a < caixas.Count && !mudou; a++)
                for (int b = a + 1; b < caixas.Count && !mudou; b++)
                    if (Sobrepoe(caixas[a], caixas[b]))
                    {
                        RectInt A = caixas[a], B = caixas[b];
                        int x0 = Mathf.Min(A.xMin, B.xMin), y0 = Mathf.Min(A.yMin, B.yMin);
                        int x1 = Mathf.Max(A.xMax, B.xMax), y1 = Mathf.Max(A.yMax, B.yMax);
                        caixas[a] = new RectInt(x0, y0, x1 - x0, y1 - y0);
                        caixas.RemoveAt(b); mudou = true;
                    }
        }
        // ordem de leitura: de cima para baixo, esquerda para direita
        caixas.Sort((p, q) => { int tp = h - p.yMax, tq = h - q.yMax; return tp != tq ? tp.CompareTo(tq) : p.xMin.CompareTo(q.xMin); });

        TextureImporter imp = Preparar(arq, SpriteImportMode.Multiple);
        List<SpriteMetaData> l = new List<SpriteMetaData>();
        for (int i = 0; i < caixas.Count; i++)
        {
            RectInt c = caixas[i];
            SpriteMetaData m = new SpriteMetaData();
            m.name = arq + "_" + i;
            m.rect = new Rect(c.x, c.y, c.width, c.height);
            m.alignment = (int)SpriteAlignment.BottomCenter;
            m.pivot = new Vector2(0.5f, 0f);
            l.Add(m);
            // coordenadas de imagem (y de cima para baixo), para eu escolher as pecas
            log.AppendLine(arq + "_" + i + "  x=" + c.x + " y=" + (h - c.yMax) + " w=" + c.width + " h=" + c.height);
        }
        imp.spritesheet = l.ToArray();
        imp.SaveAndReimport();
        Object.DestroyImmediate(t);
    }

    static bool Sobrepoe(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;
    }

    static int Altura(string arq)
    {
        byte[] b = new byte[24];
        using (FileStream fs = File.OpenRead(ARTE + arq + ".png")) fs.Read(b, 0, 24);
        return (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
    }
}
