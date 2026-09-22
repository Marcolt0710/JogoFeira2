using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O desenho do mapa de exemplo, separado do código que monta a cena.
///
/// O terreno é descrito como "onde dá para andar": clareiras retangulares em volta
/// de cada fase, ligadas por corredores em L. O AutoTile9 se encarrega das bordas,
/// então não há nenhuma tile de canto escrita à mão aqui — mexer nos números abaixo
/// é o bastante para redesenhar o mapa inteiro.
/// </summary>
public static class MenuFaseMapa
{
    public const int LARGURA = 48;
    public const int ALTURA = 32;

    /// <summary>Largura dos corredores que ligam as clareiras, em tiles.</summary>
    private const int LARGURA_CORREDOR = 3;

    public class Fase
    {
        public string id;
        public string nome;
        public string cena;
        public Vector2Int celula;
        public string[] exige;
        public int raioClareira;

        public Fase(string id, string nome, string cena, int x, int y, int raio, string[] exige)
        {
            this.id = id;
            this.nome = nome;
            this.cena = cena;
            this.celula = new Vector2Int(x, y);
            this.raioClareira = raio;
            this.exige = exige;
        }
    }

    /// <summary>
    /// As 5 fases. A ordem de desbloqueio forma uma corrente: 1 abre 2, 2 abre 3,
    /// e a 5 só abre depois da 3 e da 4, para dar o que testar no PathGate.
    /// </summary>
    public static readonly Fase[] FASES = new Fase[]
    {
        new Fase("fase_01", "A Barraca",      "Fase_01",  8,  7, 5, new string[0]),
        new Fase("fase_02", "O Pomar",        "Fase_02", 20, 11, 5, new[] { "fase_01" }),
        new Fase("fase_03", "A Ponte Velha",  "Fase_03", 33,  8, 5, new[] { "fase_02" }),
        new Fase("fase_04", "O Mirante",      "Fase_04", 39, 21, 5, new[] { "fase_03" }),
        new Fase("fase_05", "A Feira Grande", "Fase_05", 22, 24, 6, new[] { "fase_03", "fase_04" }),
    };

    /// <summary>Ligações entre fases, por índice em FASES.</summary>
    public static readonly int[,] CAMINHOS = new int[,]
    {
        { 0, 1 },
        { 1, 2 },
        { 2, 3 },
        { 3, 4 },
        { 1, 4 },
    };

    /// <summary>
    /// O corredor que exige a fase 3 para abrir. Índice em CAMINHOS.
    /// É onde a ponte de madeira aparece.
    /// </summary>
    public const int CAMINHO_COM_PORTAO = 2;

    /// <summary>Onde o jogador nasce numa partida nova.</summary>
    public static Vector2Int CelulaInicial
    {
        get { return new Vector2Int(FASES[0].celula.x, FASES[0].celula.y - 3); }
    }

    // ------------------------------------------------------------- terreno

    /// <summary>
    /// Grade de andável. true = grama (o jogador passa), false = vazio (colisão).
    /// </summary>
    public static bool[,] MontarTerreno()
    {
        bool[,] andavel = new bool[LARGURA, ALTURA];

        for (int i = 0; i < FASES.Length; i++)
            Clareira(andavel, FASES[i].celula, FASES[i].raioClareira);

        for (int i = 0; i < CAMINHOS.GetLength(0); i++)
        {
            Vector2Int a = FASES[CAMINHOS[i, 0]].celula;
            Vector2Int b = FASES[CAMINHOS[i, 1]].celula;
            Corredor(andavel, a, b, LARGURA_CORREDOR);
        }

        return andavel;
    }

    /// <summary>
    /// Clareira com os cantos cortados, para a ilha não ficar um retângulo duro.
    /// </summary>
    private static void Clareira(bool[,] g, Vector2Int centro, int raio)
    {
        for (int dy = -raio; dy <= raio; dy++)
        {
            for (int dx = -raio; dx <= raio; dx++)
            {
                // Losango achatado: corta o canto quando a soma passa do raio.
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > raio + raio / 2) continue;
                Marcar(g, centro.x + dx, centro.y + dy);
            }
        }
    }

    /// <summary>Corredor em L: anda no X primeiro, depois no Y.</summary>
    private static void Corredor(bool[,] g, Vector2Int a, Vector2Int b, int largura)
    {
        int meio = largura / 2;

        int x0 = Mathf.Min(a.x, b.x);
        int x1 = Mathf.Max(a.x, b.x);
        for (int x = x0; x <= x1; x++)
            for (int d = -meio; d <= meio; d++)
                Marcar(g, x, a.y + d);

        int y0 = Mathf.Min(a.y, b.y);
        int y1 = Mathf.Max(a.y, b.y);
        for (int y = y0; y <= y1; y++)
            for (int d = -meio; d <= meio; d++)
                Marcar(g, b.x + d, y);
    }

    private static void Marcar(bool[,] g, int x, int y)
    {
        // A borda de 1 tile fica sempre fechada, senão o jogador escapa do mapa.
        if (x < 1 || y < 1 || x >= LARGURA - 1 || y >= ALTURA - 1) return;
        g[x, y] = true;
    }

    // ------------------------------------------------------------- caminhos

    /// <summary>
    /// As células onde vai a terra batida: a linha do meio de cada corredor.
    /// Desenhada por cima da grama, na camada Caminhos.
    /// </summary>
    public static List<Vector2Int> CelulasDeCaminho(bool[,] andavel)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        HashSet<int> vistos = new HashSet<int>();

        for (int i = 0; i < CAMINHOS.GetLength(0); i++)
        {
            Vector2Int a = FASES[CAMINHOS[i, 0]].celula;
            Vector2Int b = FASES[CAMINHOS[i, 1]].celula;

            int x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            for (int x = x0; x <= x1; x++) Somar(r, vistos, andavel, x, a.y);

            int y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            for (int y = y0; y <= y1; y++) Somar(r, vistos, andavel, b.x, y);
        }
        return r;
    }

    private static void Somar(List<Vector2Int> lista, HashSet<int> vistos, bool[,] andavel, int x, int y)
    {
        if (x < 0 || y < 0 || x >= LARGURA || y >= ALTURA) return;
        if (!andavel[x, y]) return;

        int chave = y * LARGURA + x;
        if (!vistos.Add(chave)) return;

        lista.Add(new Vector2Int(x, y));
    }

    /// <summary>
    /// Ponto médio do corredor com portão — onde a ponte e a barreira ficam.
    /// </summary>
    public static Vector2Int CelulaDoPortao()
    {
        Vector2Int a = FASES[CAMINHOS[CAMINHO_COM_PORTAO, 0]].celula;
        Vector2Int b = FASES[CAMINHOS[CAMINHO_COM_PORTAO, 1]].celula;
        // O L anda no X e depois no Y; o meio do trecho vertical é um bom lugar.
        return new Vector2Int(b.x, (a.y + b.y) / 2);
    }

    // ------------------------------------------------------------- decoração

    /// <summary>
    /// Tufos espalhados na grama, longe dos caminhos. Semente fixa para o mapa
    /// sair igual toda vez que for gerado.
    /// </summary>
    public static List<Vector2Int> CelulasDeTufo(bool[,] andavel, List<Vector2Int> caminho)
    {
        HashSet<int> proibido = new HashSet<int>();
        for (int i = 0; i < caminho.Count; i++)
        {
            // Deixa uma folga de 1 tile em volta da terra batida.
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    proibido.Add((caminho[i].y + dy) * LARGURA + (caminho[i].x + dx));
        }

        for (int i = 0; i < FASES.Length; i++)
        {
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    proibido.Add((FASES[i].celula.y + dy) * LARGURA + (FASES[i].celula.x + dx));
        }

        Random.State estado = Random.state;
        Random.InitState(20260921);

        List<Vector2Int> r = new List<Vector2Int>();
        for (int y = 1; y < ALTURA - 1; y++)
        {
            for (int x = 1; x < LARGURA - 1; x++)
            {
                if (!andavel[x, y]) continue;
                if (proibido.Contains(y * LARGURA + x)) continue;

                // Só no miolo: tufo em cima da borda serrilhada fica estranho.
                if (!andavel[x - 1, y] || !andavel[x + 1, y] ||
                    !andavel[x, y - 1] || !andavel[x, y + 1]) continue;

                if (Random.value < 0.09f) r.Add(new Vector2Int(x, y));
            }
        }

        Random.state = estado;
        return r;
    }
}
