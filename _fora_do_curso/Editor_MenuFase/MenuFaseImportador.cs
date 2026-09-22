using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica as regras de importação de pixel art em tudo que entra em Assets/MenuDeFase.
/// Roda sozinho quando um arquivo novo é adicionado ou reimportado.
///
/// Os arquivos numerados (01_ a 19_) são upscales nearest-neighbor de 8x da mesma arte
/// que está em Grass_Tiles_1..4. Em vez de mexer na arte, eles entram com PPU 128
/// (16 * 8) e grade de 128: o resultado em tela é 1 tile = 1 unidade, igual aos
/// arquivos nativos que entram com PPU 16 e grade de 16.
/// </summary>
public class MenuFaseImportador : AssetPostprocessor
{
    public const string PASTA = "Assets/MenuDeFase";

    /// <summary>Tamanho do tile em pixels na arte nativa.</summary>
    public const int TILE = 16;

    /// <summary>Fator de upscale dos arquivos numerados 01_ a 19_.</summary>
    public const int ESCALA_NUMERADOS = 8;

    private class Regra
    {
        public int ppu;
        public int celula;   // lado da célula em pixels; 0 = sprite único
        public Regra(int ppu, int celula) { this.ppu = ppu; this.celula = celula; }
    }

    // Sprite único: o arquivo é um objeto inteiro, não uma folha de tiles.
    private static readonly Regra UNICO_16 = new Regra(TILE, 0);
    private static readonly Regra UNICO_128 = new Regra(TILE * ESCALA_NUMERADOS, 0);

    // Folha de tiles: fatiada na grade indicada.
    private static readonly Regra GRADE_16 = new Regra(TILE, TILE);
    private static readonly Regra GRADE_128 = new Regra(TILE * ESCALA_NUMERADOS, TILE * ESCALA_NUMERADOS);

    private static readonly Dictionary<string, Regra> REGRAS = MontarRegras();

    private static Dictionary<string, Regra> MontarRegras()
    {
        Dictionary<string, Regra> d = new Dictionary<string, Regra>();

        // --- Numerados (upscale 8x) -> PPU 128 --------------------------------
        // Blocos de autotile 3x3 (48x48 nativo). O centro é vazado: o preenchimento
        // vem dos Grass_N_Middle.
        d["01_autotile_grama"] = GRADE_128;
        d["01_autotile_grama 1"] = GRADE_128;
        d["02_autotile_grama_borda_terra"] = GRADE_128;
        d["09_autotile_grama_areia"] = GRADE_128;
        d["10_autotile_grama_borda_pedra"] = GRADE_128;

        // Ilhas 2x2 (32x32 nativo).
        d["03_ilha_grama"] = GRADE_128;
        d["04_ilha_grama_borda_terra"] = GRADE_128;
        d["13_ilha_grama_sobre_areia"] = GRADE_128;
        d["14_ilha_grama_borda_pedra"] = GRADE_128;

        // Escadas 2x4 (32x64 nativo).
        d["05_escada_madeira_e_cantos"] = GRADE_128;
        d["11_escada_pedra_e_cantos"] = GRADE_128;

        // Pilares / penhascos 3x6 (48x96 nativo).
        d["07_pilar_terra"] = GRADE_128;
        d["08_pilar_pedra"] = GRADE_128;

        // Paredes 4x4 (64x64 nativo).
        d["18_parede_terra"] = GRADE_128;
        d["19_parede_pedra"] = GRADE_128;

        // Tufos de grama: 1 tile só (16x16 nativo).
        d["15_detalhe_grama_1"] = UNICO_128;
        d["16_detalhe_grama_2"] = UNICO_128;
        d["17_detalhe_grama_3"] = UNICO_128;

        // --- Nativos (16px de verdade) -> PPU 16 ------------------------------
        // Folhas-mestre 16x10 tiles.
        d["Grass_Tiles_1"] = GRADE_16;
        d["Grass_Tiles_2"] = GRADE_16;
        d["Grass_Tiles_3"] = GRADE_16;
        d["Grass_Tiles_4"] = GRADE_16;

        // Preenchimentos chapados.
        d["Grass_1_Middle"] = UNICO_16;
        d["Grass_2_Middle"] = UNICO_16;
        d["Grass_3_Middle"] = UNICO_16;
        d["Grass_4_Middle"] = UNICO_16;
        d["Path_Middle"] = UNICO_16;

        // Tiras de tiles.
        d["Path_Decoration"] = GRADE_16;  // 3x1
        d["Ladder"] = GRADE_16;           // 1x3

        // Pontes: desenhos inteiros, viram objetos no mapa e não tiles.
        d["Bridge_Stone_Horizontal"] = UNICO_16;
        d["Bridge_Stone_Vertical"] = UNICO_16;
        d["Bridge_Wood"] = UNICO_16;
        d["Bridge_Wood_1"] = UNICO_16;

        // Mapas de demonstração já pintados. Não são tileset.
        d["Grass_Tiles_1_Blob_TEST"] = UNICO_16;
        d["Grass_Tiles_1_Blob_TEST_1"] = UNICO_16;

        // Folha do personagem. Fica como sprite único de propósito: ela tem
        // cabeçalho azul, separadores e fundo verde opaco, e a grade (80x120 com
        // passo de 85) não é uniforme desde a borda. Quem trata dela é o
        // MenuFasePersonagem, que gera uma folha limpa em MenuFase_Tiles.
        d["PERSONAGEM_MENUFASES"] = UNICO_16;

        return d;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PASTA)) return;

        TextureImporter imp = (TextureImporter)assetImporter;
        string nome = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        Regra regra;
        if (!REGRAS.TryGetValue(nome, out regra))
        {
            // Arquivo novo que ainda não está na tabela: assume pixel art nativa,
            // sprite único. Basta acrescentar na tabela acima para fatiar.
            regra = UNICO_16;
            Debug.Log("[MenuFase] '" + nome + "' não está na tabela de regras. " +
                      "Importado como sprite único em PPU " + TILE +
                      ". Edite MenuFaseImportador.cs se ele for uma folha de tiles.");
        }

        // Ajustes que só existem em TextureImporterSettings.
        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;  // sem malha apertada: o tile ocupa a célula inteira
        s.spriteExtrude = 0;                         // sem borda extra, senão vaza pixel do vizinho
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.mipmapEnabled = false;
        s.filterMode = FilterMode.Point;
        s.wrapMode = TextureWrapMode.Clamp;
        imp.SetTextureSettings(s);

        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = regra.ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = 2048;

        if (regra.celula <= 0)
        {
            imp.spriteImportMode = SpriteImportMode.Single;
            return;
        }

        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritesheet = Fatiar(assetPath, nome, regra.celula);
    }

    /// <summary>
    /// Gera os retângulos da grade. A indexação vai da esquerda para a direita e de
    /// CIMA para BAIXO (índice 0 = canto superior esquerdo), que é como a gente lê
    /// um bloco 3x3. A textura do Unity tem origem embaixo, então o Y é invertido.
    /// </summary>
    private static SpriteMetaData[] Fatiar(string caminho, string nome, int celula)
    {
        int larg, alt;
        if (!LerTamanhoPng(caminho, out larg, out alt))
        {
            Debug.LogWarning("[MenuFase] não consegui ler o tamanho de " + caminho + "; deixei sem fatiar.");
            return new SpriteMetaData[0];
        }

        int colunas = larg / celula;
        int linhas = alt / celula;
        if (colunas <= 0 || linhas <= 0)
        {
            Debug.LogWarning("[MenuFase] " + nome + " (" + larg + "x" + alt + ") é menor que a célula de " +
                             celula + "px; deixei sem fatiar.");
            return new SpriteMetaData[0];
        }

        List<SpriteMetaData> lista = new List<SpriteMetaData>(colunas * linhas);
        for (int linha = 0; linha < linhas; linha++)
        {
            for (int col = 0; col < colunas; col++)
            {
                SpriteMetaData md = new SpriteMetaData();
                md.name = nome + "_" + (linha * colunas + col);
                md.rect = new Rect(col * celula, alt - (linha + 1) * celula, celula, celula);
                md.alignment = (int)SpriteAlignment.Center;
                md.pivot = new Vector2(0.5f, 0.5f);
                lista.Add(md);
            }
        }
        return lista.ToArray();
    }

    /// <summary>
    /// Lê largura/altura direto do cabeçalho IHDR do PNG. Durante o OnPreprocessTexture
    /// a textura ainda não existe como objeto, então não dá para perguntar ao Unity.
    /// </summary>
    private static bool LerTamanhoPng(string caminho, out int larg, out int alt)
    {
        larg = 0; alt = 0;
        try
        {
            using (System.IO.FileStream fs = System.IO.File.OpenRead(caminho))
            {
                byte[] buf = new byte[24];
                if (fs.Read(buf, 0, 24) < 24) return false;
                // 0..7 assinatura, 8..15 tamanho + "IHDR", 16..19 largura, 20..23 altura (big-endian)
                if (buf[0] != 0x89 || buf[1] != 0x50) return false;
                larg = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                alt = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                return larg > 0 && alt > 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MenuFase] erro lendo " + caminho + ": " + e.Message);
            return false;
        }
    }

    [MenuItem("MenuFase/1 - Reimportar sprites da MenuDeFase")]
    public static void ReimportarTudo()
    {
        AssetDatabase.ImportAsset(PASTA, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log("[MenuFase] sprites de " + PASTA + " reimportados.");
    }
}
