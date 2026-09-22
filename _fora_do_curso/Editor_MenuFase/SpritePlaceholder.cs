using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera os dois sprites 16x16 que o pacote MenuDeFase não tem: o bonequinho do
/// jogador e a bandeirinha de fase concluída.
///
/// O pacote não traz arte de personagem — nem sprite solto, nem spritesheet, nem
/// animação — e também não traz bandeira nenhuma. Os tufos de grama (15/16/17) não
/// servem de marcador porque são opacos e já vêm com o fundo verde da grama baked:
/// tingir um deles daria um quadrado amarelo, não uma bandeira.
///
/// Quando a arte de verdade chegar, troque o Sprite do objeto "Jogador" e ligue um
/// Animator com os parâmetros VelX, VelY, Velocidade, UltimaX e UltimaY — o
/// PlayerMapController já alimenta todos eles.
/// </summary>
public static class SpritePlaceholder
{
    public const string CAMINHO_JOGADOR =
        MenuFaseGerarTiles.PASTA_TILES + "/_jogador_placeholder.png";

    public const string CAMINHO_BANDEIRA =
        MenuFaseGerarTiles.PASTA_TILES + "/_bandeira_placeholder.png";

    // '.' vazio  'o' contorno  's' pele  'e' olho  'b' camisa  'p' calça
    private static readonly string[] JOGADOR =
    {
        "................",
        ".....oooooo.....",
        "....osssssso....",
        "....osssssso....",
        "....oseesseo....",
        "....osssssso....",
        "....osssssso....",
        ".....oooooo.....",
        "....obbbbbbo....",
        "...obbbbbbbbo...",
        "...obbbbbbbbo...",
        "...obbbbbbbbo...",
        "....oppppppo....",
        "....opp..ppo....",
        "....opp..ppo....",
        ".....oo..oo.....",
    };

    // '.' vazio  'o' contorno  'b' pano  'p' mastro  'g' base
    private static readonly string[] BANDEIRA =
    {
        "................",
        "......ooo.......",
        "......obbo......",
        "......obbbo.....",
        "......obbbbo....",
        "......obbbbbo...",
        "......obbbbo....",
        "......obbbo.....",
        "......obbo......",
        "......ooo.......",
        "......opo.......",
        "......opo.......",
        "......opo.......",
        "......opo.......",
        ".....ogggo......",
        "......ooo.......",
    };

    public static Sprite ObterJogador()
    {
        return Obter(CAMINHO_JOGADOR, JOGADOR, PaletaJogador());
    }

    public static Sprite ObterBandeira()
    {
        return Obter(CAMINHO_BANDEIRA, BANDEIRA, PaletaBandeira());
    }

    private static Dictionary<char, Color> PaletaJogador()
    {
        Dictionary<char, Color> p = new Dictionary<char, Color>();
        p['o'] = new Color(0.16f, 0.11f, 0.13f, 1f);
        p['e'] = new Color(0.16f, 0.11f, 0.13f, 1f);
        p['s'] = new Color(0.96f, 0.80f, 0.62f, 1f);
        p['b'] = new Color(0.84f, 0.35f, 0.30f, 1f);
        p['p'] = new Color(0.30f, 0.35f, 0.55f, 1f);
        return p;
    }

    private static Dictionary<char, Color> PaletaBandeira()
    {
        Dictionary<char, Color> p = new Dictionary<char, Color>();
        p['o'] = new Color(0.16f, 0.11f, 0.13f, 1f);
        p['b'] = new Color(0.97f, 0.83f, 0.25f, 1f);   // amarelo, contrasta com a grama
        p['p'] = new Color(0.45f, 0.31f, 0.20f, 1f);   // mastro de madeira
        p['g'] = new Color(0.35f, 0.24f, 0.16f, 1f);
        return p;
    }

    private static Sprite Obter(string caminho, string[] desenho, Dictionary<char, Color> paleta)
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        if (existente != null) return existente;

        MenuFaseGerarTiles.GarantirPasta(MenuFaseGerarTiles.PASTA_TILES);
        Gravar(caminho, desenho, paleta);

        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        if (s == null) Debug.LogWarning("[MenuFase] não consegui gerar o sprite " + caminho);

        return s;
    }

    private static void Gravar(string caminho, string[] desenho, Dictionary<char, Color> paleta)
    {
        int lado = desenho.Length;

        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color vazio = new Color(0f, 0f, 0f, 0f);

        for (int linha = 0; linha < lado; linha++)
        {
            string s = desenho[linha];
            for (int col = 0; col < lado; col++)
            {
                char c = col < s.Length ? s[col] : '.';

                Color cor;
                if (!paleta.TryGetValue(c, out cor)) cor = vazio;

                // A textura conta o Y de baixo para cima; o desenho, de cima para baixo.
                tex.SetPixel(col, lado - 1 - linha, cor);
            }
        }

        tex.Apply();
        System.IO.File.WriteAllBytes(caminho, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        // Estes arquivos ficam fora da MenuDeFase, então o MenuFaseImportador não
        // pega neles: os ajustes de pixel art vão na mão aqui.
        TextureImporter imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (imp == null) return;

        TextureImporterSettings cfg = new TextureImporterSettings();
        imp.ReadTextureSettings(cfg);
        cfg.spriteMeshType = SpriteMeshType.FullRect;
        cfg.spriteExtrude = 0;
        cfg.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(cfg);

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = MenuFaseImportador.TILE;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        imp.SaveAndReimport();
    }
}
