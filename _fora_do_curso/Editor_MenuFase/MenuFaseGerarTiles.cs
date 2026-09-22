using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cria os assets de tile a partir dos sprites já fatiados pelo MenuFaseImportador.
///
/// Gera em Assets/MenuFase_Tiles:
///   - AutoTile9 para os blocos 3x3 de grama (borda + miolo chapado);
///   - Tile comum para caminho, tufos, escada e paredes;
///   - um tile invisível para a camada de colisão;
///   - a paleta MenuFase_Palette, quando a API interna do Unity permitir.
///
/// Também conserta os 20 .asset antigos da MenuDeFase: eles apontavam para a textura
/// inteira (fileID 21300000), referência que deixa de existir quando a textura passa
/// a ser Multiple. Sem esse conserto eles apareceriam vazios nas paletas chao/chao2.
/// </summary>
public static class MenuFaseGerarTiles
{
    public const string PASTA_ARTE = "Assets/MenuDeFase";
    public const string PASTA_TILES = "Assets/MenuFase_Tiles";

    [MenuItem("MenuFase/2 - Gerar tiles e paleta")]
    public static void Gerar()
    {
        GarantirPasta(PASTA_TILES);

        // A geração depende dos sprites já estarem fatiados.
        AssetDatabase.ImportAsset(PASTA_ARTE, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

        int criados = 0;

        criados += GerarAutotilesDeGrama();
        criados += GerarTilesSimples();
        criados += GerarTileDeColisao();

        ConsertarTilesAntigos();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CriarPaleta();

        Debug.Log("[MenuFase] pronto: " + criados + " tiles em " + PASTA_TILES + ".");
    }

    // ------------------------------------------------------------- autotiles

    /// <summary>
    /// Os quatro blocos 3x3 mudam só a COR DA BORDA (terra, areia, pedra); a grama
    /// deles é a mesma. Medindo a área opaca dos quatro PNGs, o verde dá 62,137,72
    /// nos quatro — que é exatamente o Grass_1_Middle.
    ///
    /// Por isso todos usam o mesmo miolo. Parear pelo número (02 com Grass_2_Middle,
    /// 09 com Grass_3_Middle...) pareceria natural e estaria errado: os Grass_2/3/4
    /// são verdes de outras paletas (51,152,75 / 124,150,60 / 63,136,108), das folhas
    /// Grass_Tiles_2..4, e deixariam uma emenda de cor entre o miolo e a borda.
    /// </summary>
    private static int GerarAutotilesDeGrama()
    {
        const string MIOLO = "Grass_1_Middle";

        int n = 0;
        n += AutoTile("AT_Grama", "01_autotile_grama", MIOLO) ? 1 : 0;
        n += AutoTile("AT_Grama_Terra", "02_autotile_grama_borda_terra", MIOLO) ? 1 : 0;
        n += AutoTile("AT_Grama_Areia", "09_autotile_grama_areia", MIOLO) ? 1 : 0;
        n += AutoTile("AT_Grama_Pedra", "10_autotile_grama_borda_pedra", MIOLO) ? 1 : 0;
        return n;
    }

    /// <summary>
    /// Monta um AutoTile9 com as 9 fatias do bloco e o miolo chapado que vem à parte.
    /// </summary>
    private static bool AutoTile(string nomeAsset, string arquivoBloco, string arquivoMiolo)
    {
        Sprite[] fatias = SpritesDe(arquivoBloco);
        if (fatias == null || fatias.Length < 9)
        {
            Debug.LogWarning("[MenuFase] " + arquivoBloco + " não tem 9 fatias (achei " +
                             (fatias == null ? 0 : fatias.Length) + "). Pulei o " + nomeAsset + ".");
            return false;
        }

        Sprite[] miolo = SpritesDe(arquivoMiolo);
        Sprite preenchimento = (miolo != null && miolo.Length > 0) ? miolo[0] : null;
        if (preenchimento == null)
            Debug.LogWarning("[MenuFase] não achei o miolo " + arquivoMiolo +
                             "; o interior de " + nomeAsset + " vai ficar vazado.");

        string caminho = PASTA_TILES + "/" + nomeAsset + ".asset";
        AutoTile9 tile = AssetDatabase.LoadAssetAtPath<AutoTile9>(caminho);

        bool novo = tile == null;
        if (novo) tile = ScriptableObject.CreateInstance<AutoTile9>();

        tile.borda = new Sprite[9];
        for (int i = 0; i < 9; i++) tile.borda[i] = fatias[i];
        tile.preenchimento = preenchimento;
        tile.tipoColisor = Tile.ColliderType.None;   // a colisão fica na camada própria
        tile.cor = Color.white;

        if (novo) AssetDatabase.CreateAsset(tile, caminho);
        else EditorUtility.SetDirty(tile);

        return true;
    }

    // ------------------------------------------------------- tiles simples

    private static int GerarTilesSimples()
    {
        int n = 0;

        // Caminho de terra.
        n += Simples("T_Caminho", "Path_Middle", 0) ? 1 : 0;
        for (int i = 0; i < 3; i++)
            n += Simples("T_Caminho_Deco_" + i, "Path_Decoration", i) ? 1 : 0;

        // Tufos de grama para a decoração.
        n += Simples("T_Tufo_1", "15_detalhe_grama_1", 0) ? 1 : 0;
        n += Simples("T_Tufo_2", "16_detalhe_grama_2", 0) ? 1 : 0;
        n += Simples("T_Tufo_3", "17_detalhe_grama_3", 0) ? 1 : 0;

        // Escada (1x3 nativo).
        for (int i = 0; i < 3; i++)
            n += Simples("T_Escada_" + i, "Ladder", i) ? 1 : 0;

        // Paredes 4x4: viram tiles avulsos, montados à mão no mapa.
        for (int i = 0; i < 16; i++)
        {
            n += Simples("T_ParedeTerra_" + i, "18_parede_terra", i) ? 1 : 0;
            n += Simples("T_ParedePedra_" + i, "19_parede_pedra", i) ? 1 : 0;
        }

        return n;
    }

    private static bool Simples(string nomeAsset, string arquivo, int indice)
    {
        Sprite[] sprites = SpritesDe(arquivo);
        if (sprites == null || indice >= sprites.Length || sprites[indice] == null)
            return false;

        string caminho = PASTA_TILES + "/" + nomeAsset + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(caminho);

        bool novo = tile == null;
        if (novo) tile = ScriptableObject.CreateInstance<Tile>();

        tile.sprite = sprites[indice];
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;

        if (novo) AssetDatabase.CreateAsset(tile, caminho);
        else EditorUtility.SetDirty(tile);

        return true;
    }

    /// <summary>
    /// Tile sem sprite, só com colisor. É o que se pinta na camada Colisao: o
    /// TilemapRenderer dela fica desligado, então nada disso aparece em tela.
    /// </summary>
    private static int GerarTileDeColisao()
    {
        string caminho = PASTA_TILES + "/T_Colisao.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(caminho);

        bool novo = tile == null;
        if (novo) tile = ScriptableObject.CreateInstance<Tile>();

        // Um sprite qualquer para a paleta ter o que mostrar; o renderer fica off.
        Sprite[] s = SpritesDe("Grass_1_Middle");
        tile.sprite = (s != null && s.Length > 0) ? s[0] : null;
        tile.color = new Color(1f, 0.2f, 0.2f, 0.45f);
        tile.colliderType = Tile.ColliderType.Grid;

        if (novo) AssetDatabase.CreateAsset(tile, caminho);
        else EditorUtility.SetDirty(tile);

        return 1;
    }

    // ------------------------------------------------- conserto dos antigos

    /// <summary>
    /// Os .asset que já estavam na MenuDeFase referenciavam a textura inteira.
    /// Depois do refatiamento essa referência morre; aqui cada um é reapontado
    /// para a primeira fatia, só para não ficar vazio nas paletas chao/chao2.
    /// </summary>
    private static void ConsertarTilesAntigos()
    {
        string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { PASTA_ARTE });
        int consertados = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guids[i]);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(caminho);
            if (tile == null || tile.sprite != null) continue;

            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
            Sprite[] sprites = SpritesDe(nome);
            if (sprites == null || sprites.Length == 0) continue;

            tile.sprite = sprites[0];
            EditorUtility.SetDirty(tile);
            consertados++;
        }

        if (consertados > 0)
            Debug.Log("[MenuFase] reapontei " + consertados + " tiles antigos da MenuDeFase " +
                      "para a primeira fatia. Os tiles de verdade são os de " + PASTA_TILES + ".");
    }

    // ------------------------------------------------------------- paleta

    /// <summary>
    /// A criação de paleta usa API interna do Unity, que muda de versão para versão.
    /// Por isso vai por reflexão: se não der, o mapa ainda é montado por script e a
    /// paleta pode ser criada à mão na janela Tile Palette.
    /// </summary>
    private static void CriarPaleta()
    {
        string caminho = PASTA_TILES + "/MenuFase_Palette.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(caminho) != null) return;

        try
        {
            System.Type tipo = System.Type.GetType(
                "UnityEditor.Tilemaps.GridPaletteUtility, UnityEditor");

            if (tipo != null)
            {
                System.Reflection.MethodInfo m = tipo.GetMethod("CreateNewPalette",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (m != null)
                {
                    System.Reflection.ParameterInfo[] ps = m.GetParameters();
                    object[] args = new object[ps.Length];

                    for (int i = 0; i < ps.Length; i++)
                    {
                        System.Type pt = ps[i].ParameterType;
                        if (pt == typeof(string))
                            args[i] = (i == 0) ? PASTA_TILES : "MenuFase_Palette";
                        else if (pt == typeof(Vector3))
                            args[i] = new Vector3(1f, 1f, 0f);
                        else if (pt.IsEnum)
                            args[i] = System.Enum.ToObject(pt, 0);   // Rectangle / Automatic
                        else
                            args[i] = pt.IsValueType ? System.Activator.CreateInstance(pt) : null;
                    }

                    m.Invoke(null, args);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[MenuFase] paleta MenuFase_Palette criada em " + PASTA_TILES + ".");
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("[MenuFase] não deu para criar a paleta por script (" + e.GetType().Name + ").");
        }

        Debug.Log("[MenuFase] crie a paleta à mão se quiser pintar: janela Tile Palette > " +
                  "Create New Palette, salvando em " + PASTA_TILES + ". " +
                  "O mapa de exemplo é montado por script e não depende dela.");
    }

    // ------------------------------------------------------------- utilidades

    /// <summary>
    /// Devolve as fatias de um PNG da MenuDeFase, ordenadas pelo número no fim do
    /// nome ("_0", "_1", ...). Sprite único vira um array de um item.
    /// </summary>
    public static Sprite[] SpritesDe(string nomeArquivo)
    {
        return SpritesDoCaminho(PASTA_ARTE + "/" + nomeArquivo + ".png");
    }

    /// <summary>
    /// Mesma coisa, mas para um PNG fora da pasta de arte — a folha do personagem,
    /// por exemplo, que é gerada em MenuFase_Tiles.
    /// </summary>
    public static Sprite[] SpritesDoCaminho(string caminho)
    {
        string nomeArquivo = System.IO.Path.GetFileNameWithoutExtension(caminho);
        Object[] tudo = AssetDatabase.LoadAllAssetsAtPath(caminho);
        if (tudo == null || tudo.Length == 0) return null;

        Dictionary<int, Sprite> porIndice = new Dictionary<int, Sprite>();
        List<Sprite> semIndice = new List<Sprite>();

        // O número só conta como índice de fatia se vier DEPOIS do nome do arquivo.
        // Sem esse cuidado, "15_detalhe_grama_1" — que é sprite único — teria o "_1"
        // lido como fatia 1, e o índice 0 voltaria nulo: era o que sumia com os tufos.
        string prefixo = nomeArquivo + "_";

        for (int i = 0; i < tudo.Length; i++)
        {
            Sprite s = tudo[i] as Sprite;
            if (s == null) continue;

            int indice;
            if (s.name.Length > prefixo.Length && s.name.StartsWith(prefixo) &&
                int.TryParse(s.name.Substring(prefixo.Length), out indice))
                porIndice[indice] = s;
            else
                semIndice.Add(s);
        }

        if (porIndice.Count > 0)
        {
            int maior = -1;
            foreach (int k in porIndice.Keys) if (k > maior) maior = k;

            Sprite[] r = new Sprite[maior + 1];
            foreach (KeyValuePair<int, Sprite> kv in porIndice) r[kv.Key] = kv.Value;
            return r;
        }

        return semIndice.ToArray();
    }

    public static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;

        string pai = System.IO.Path.GetDirectoryName(caminho).Replace('\\', '/');
        string nome = System.IO.Path.GetFileName(caminho);
        if (!AssetDatabase.IsValidFolder(pai)) GarantirPasta(pai);
        AssetDatabase.CreateFolder(pai, nome);
    }
}
