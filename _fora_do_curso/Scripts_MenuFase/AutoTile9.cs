using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Autotile de 9 fatias, escrito à mão porque o Rule Tile (pacote 2D Tilemap Extras)
/// só existe a partir do Unity 2019.4 e este projeto é 2017.4.
///
/// A arte da MenuDeFase segue esse formato: o bloco 3x3 (01_autotile_grama e irmãos)
/// é só o ANEL DE BORDA, com o miolo vazado, e o preenchimento vem separado de um
/// Grass_N_Middle chapado. Por isso aqui são 8 sprites de borda + 1 de preenchimento.
///
/// Índices das bordas, do jeito que o importador fatia (0 = canto superior esquerdo):
///
///     0  1  2
///     3 (4) 5      <- o 4 é o miolo vazado do PNG; não é usado
///     6  7  8
///
/// ATENÇÃO à orientação, que é invertida e não dá para adivinhar olhando o nome do
/// arquivo. O PNG não desenha uma ilha de grama: ele desenha um BURACO cavado num
/// campo de grama. Conferi medindo o canal alfa do 01_autotile_grama.png:
///
///     índice 1 (meio de cima)    -> opaco em cima (alfa 255), vazado embaixo (41)
///     índice 7 (meio de baixo)   -> vazado em cima (41), opaco embaixo (255)
///     índice 3 (meio da esquerda)-> opaco à esquerda (255), vazado à direita (51)
///
/// Ou seja, o tile que tem grama embaixo e vazio em cima — que é o que serve para a
/// borda DE CIMA de uma ilha — é o índice 7, não o 1. A conversão é sempre 8 menos o
/// índice, uma reflexão pelo centro. Trocar isso faz o mapa inteiro sair com as
/// bordas viradas para dentro.
///
/// A escolha é feita só pelos 4 vizinhos cardeais. Não há arte de canto interno
/// no pacote, então regiões devem ter pelo menos 3x3 para ficarem bonitas.
/// </summary>
[CreateAssetMenu(fileName = "AutoTile", menuName = "MenuFase/AutoTile 9 fatias")]
public class AutoTile9 : TileBase
{
    [Header("Bordas (9 fatias do bloco 3x3; o índice 4 é ignorado)")]
    public Sprite[] borda = new Sprite[9];

    [Header("Miolo")]
    [Tooltip("O Grass_N_Middle chapado que preenche o interior da região.")]
    public Sprite preenchimento;

    [Header("Ligação")]
    [Tooltip("Outros tiles que contam como 'igual a mim' na hora de decidir a borda. " +
             "Serve para um caminho de terra encostar na grama sem virar borda.")]
    public TileBase[] tambemConectaCom;

    [Header("Aparência")]
    public Color cor = Color.white;
    public Tile.ColliderType tipoColisor = Tile.ColliderType.None;

    public override void GetTileData(Vector3Int posicao, ITilemap mapa, ref TileData dados)
    {
        bool cima = Conecta(mapa, posicao + new Vector3Int(0, 1, 0));
        bool baixo = Conecta(mapa, posicao + new Vector3Int(0, -1, 0));
        bool esq = Conecta(mapa, posicao + new Vector3Int(-1, 0, 0));
        bool dir = Conecta(mapa, posicao + new Vector3Int(1, 0, 0));

        dados.sprite = EscolherSprite(cima, baixo, esq, dir);
        dados.color = cor;
        dados.transform = Matrix4x4.identity;
        dados.colliderType = tipoColisor;
        dados.flags = TileFlags.LockTransform;
    }

    /// <summary>
    /// Um tile só é borda do lado onde NÃO há vizinho ligado.
    /// Os cantos são testados antes das laterais porque são o caso mais específico.
    ///
    /// Os índices são os do bloco refletidos pelo centro (ver o comentário da classe):
    /// falta vizinho em cima -> usa a fatia de BAIXO do PNG, e assim por diante.
    /// </summary>
    private Sprite EscolherSprite(bool cima, bool baixo, bool esq, bool dir)
    {
        if (cima && baixo && esq && dir) return Miolo();

        if (!cima && !esq) return Pegar(8);
        if (!cima && !dir) return Pegar(6);
        if (!baixo && !esq) return Pegar(2);
        if (!baixo && !dir) return Pegar(0);

        if (!cima) return Pegar(7);
        if (!baixo) return Pegar(1);
        if (!esq) return Pegar(5);
        if (!dir) return Pegar(3);

        return Miolo();
    }

    private Sprite Miolo()
    {
        // O índice 4 do bloco é vazado, então sem o preenchimento não sobra nada.
        return preenchimento != null ? preenchimento : Pegar(4);
    }

    private Sprite Pegar(int i)
    {
        if (borda == null || i < 0 || i >= borda.Length) return null;
        return borda[i];
    }

    private bool Conecta(ITilemap mapa, Vector3Int posicao)
    {
        TileBase outro = mapa.GetTile(posicao);
        if (outro == null) return false;
        if (outro == this) return true;

        if (tambemConectaCom != null)
        {
            for (int i = 0; i < tambemConectaCom.Length; i++)
                if (tambemConectaCom[i] == outro) return true;
        }
        return false;
    }

    /// <summary>
    /// Quando um tile é pintado ou apagado, os 8 vizinhos precisam recalcular a borda
    /// deles também — senão a emenda só aparece depois de repintar por cima.
    /// </summary>
    public override void RefreshTile(Vector3Int posicao, ITilemap mapa)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                mapa.RefreshTile(posicao + new Vector3Int(dx, dy, 0));
    }
}
