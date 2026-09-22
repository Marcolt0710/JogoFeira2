using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// FERRAMENTA TEMPORARIA (fora do curso): arruma o texto de aviso e os limites
// da agua na cena do mapa de fases. Depois de rodar, sai do projeto
// (vai para _fora_do_curso/Editor_MenuFase/).
//
// Rodar pelo menu "MenuFase/Arrumar texto e limites" ou em batchmode:
//   Unity.exe -batchmode -quit -projectPath <projeto> -executeMethod ArrumarMenuFase.Aplicar
//   Unity.exe -batchmode -quit -projectPath <projeto> -executeMethod ArrumarMenuFase.Verificar
public static class ArrumarMenuFase
{
    const string CENA = "Assets/MenuDeFase/menudefase.unity";
    // Margem de agua solida em volta de tudo, para ninguem sair do mapa.
    const int MARGEM = 3;

    [MenuItem("MenuFase/Arrumar texto e limites")]
    public static void Aplicar()
    {
        Scene cena = EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);

        ArrumarCanvas();
        ArrumarLimites();
        ArrumarJogador();

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("[ArrumarMenuFase] cena salva.");

        Verificar();
    }

    // ------------------------------------------------------------ texto

    static void ArrumarCanvas()
    {
        GameObject go = GameObject.Find("Canvas");
        Canvas canvas = go.GetComponent<Canvas>();
        // Screen Space - Overlay: o Canvas e desenhado por cima de tudo,
        // depois da camera, sem disputar Order in Layer com os sprites.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;
        go.transform.localScale = Vector3.one;

        GameObject aviso = GameObject.Find("TXT_AVISO");
        if (aviso.GetComponent<Outline>() == null)
        {
            Outline contorno = aviso.AddComponent<Outline>();
            contorno.effectColor = new Color(0.1f, 0.06f, 0.02f, 1f);
            contorno.effectDistance = new Vector2(2f, -2f);
        }
        Debug.Log("[ArrumarMenuFase] Canvas: " + canvas.renderMode + " ordem " + canvas.sortingOrder);
    }

    // ------------------------------------------------------------ agua

    static Tilemap Mapa(string nome)
    {
        GameObject go = GameObject.Find("Grid/" + nome);
        if (go == null) go = GameObject.Find(nome);
        return go.GetComponent<Tilemap>();
    }

    static bool Terra(Tilemap areia, Tilemap grama, int x, int y)
    {
        Vector3Int p = new Vector3Int(x, y, 0);
        return areia.HasTile(p) || grama.HasTile(p);
    }

    static void ArrumarLimites()
    {
        Tilemap colisao = Mapa("Colisao");
        colisao.CompressBounds();
        BoundsInt b = colisao.cellBounds;
        TileBase tile = colisao.GetTile(new Vector3Int(b.xMin, b.yMin, 0));

        // A agua de dentro do mapa ja tem colisao (menos as pontes, que ficam
        // livres). Aqui so se acrescenta uma margem solida em volta de tudo.
        int novos = 0;
        for (int x = b.xMin - MARGEM; x < b.xMax + MARGEM; x++)
        {
            for (int y = b.yMin - MARGEM; y < b.yMax + MARGEM; y++)
            {
                bool dentro = x >= b.xMin && x < b.xMax && y >= b.yMin && y < b.yMax;
                Vector3Int p = new Vector3Int(x, y, 0);
                if (dentro == false && colisao.HasTile(p) == false)
                {
                    colisao.SetTile(p, tile);
                    novos++;
                }
            }
        }
        Debug.Log("[ArrumarMenuFase] tiles de colisao novos na margem: " + novos + " (tile " + tile.name + ")");

        // Garante que o colisor do tilemap e o composite sejam gerados e salvos.
        TilemapCollider2D tc = colisao.GetComponent<TilemapCollider2D>();
        CompositeCollider2D cc = colisao.GetComponent<CompositeCollider2D>();
        colisao.RefreshAllTiles();
        tc.usedByComposite = false;
        tc.enabled = false;
        tc.enabled = true;
        tc.usedByComposite = true;
        cc.generationType = CompositeCollider2D.GenerationType.Synchronous;
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        cc.GenerateGeometry();
        Debug.Log("[ArrumarMenuFase] composite: caminhos=" + cc.pathCount + " pontos=" + cc.pointCount);
    }

    static void ArrumarJogador()
    {
        GameObject jogador = GameObject.Find("Jogador");
        Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        BoxCollider2D caixa = jogador.GetComponent<BoxCollider2D>();
        caixa.isTrigger = false;
    }

    // ------------------------------------------------------------ conferir

    public static void Verificar()
    {
        EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);

        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        Debug.Log("[Verificar] Canvas renderMode=" + canvas.renderMode + " sortingOrder=" + canvas.sortingOrder
            + " outline=" + (GameObject.Find("TXT_AVISO").GetComponent<Outline>() != null));

        Tilemap areia = Mapa("Areia");
        Tilemap grama = Mapa("Grama");
        Tilemap colisao = Mapa("Colisao");
        CompositeCollider2D cc = colisao.GetComponent<CompositeCollider2D>();
        TilemapCollider2D tc = colisao.GetComponent<TilemapCollider2D>();
        Rigidbody2D rbCol = colisao.GetComponent<Rigidbody2D>();
        Debug.Log("[Verificar] Colisao: tilemapCollider=" + (tc != null && tc.enabled) + " usedByComposite=" + tc.usedByComposite
            + " rb=" + rbCol.bodyType + " composite caminhos=" + cc.pathCount + " pontos=" + cc.pointCount
            + " geometry=" + cc.geometryType + " bounds=" + colisao.cellBounds);

        // Agua sem colisao (tirando as pontes) e terra de dentro com colisao.
        colisao.CompressBounds();
        BoundsInt b = colisao.cellBounds;
        int aguaLivre = 0;
        List<string> livres = new List<string>();
        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                if (Terra(areia, grama, x, y) == false && colisao.HasTile(p) == false)
                {
                    aguaLivre++;
                    if (livres.Count < 60) livres.Add("(" + x + "," + y + ")");
                }
            }
        }
        Debug.Log("[Verificar] celulas de agua sem colisao (devem ser so as pontes): " + aguaLivre + " " + string.Join(" ", livres.ToArray()));

        // Pontos de teste com a fisica: o composite precisa responder.
        Physics2D.SyncTransforms();
        GameObject jogador = GameObject.Find("Jogador");
        Vector2 pe = (Vector2)jogador.transform.position + new Vector2(0f, 0.2f);
        Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();
        Debug.Log("[Verificar] Jogador pos=" + jogador.transform.position + " body=" + corpo.bodyType
            + " deteccao=" + corpo.collisionDetectionMode + " trigger=" + jogador.GetComponent<BoxCollider2D>().isTrigger
            + " colisao no pe=" + (Physics2D.OverlapPoint(pe) != null && Physics2D.OverlapPoint(pe).gameObject == colisao.gameObject));

        Teste(colisao, "agua longe (-10,40)", new Vector2(-9.5f, 40.5f));
        Teste(colisao, "agua entre ilhas (33,5)", new Vector2(33.5f, 5.5f));
        Teste(colisao, "fora do mapa (-16,0)", new Vector2(-15.5f, 0.5f));
        Teste(colisao, "ponte 1 (33,12)", new Vector2(33.5f, 12.5f));
        Teste(colisao, "ponte 2 (65,18)", new Vector2(65.5f, 18.5f));

        // Confere celula por celula: o centro de cada tile de colisao tem que
        // ser solido e o centro de cada celula sem tile tem que ser livre.
        int certos = 0, errados = 0;
        List<string> erros = new List<string>();
        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                bool temTile = colisao.HasTile(new Vector3Int(x, y, 0));
                bool solido = false;
                foreach (Collider2D t in Physics2D.OverlapPointAll(new Vector2(x + 0.5f, y + 0.5f)))
                    if (t.gameObject == colisao.gameObject) solido = true;
                if (temTile == solido) certos++;
                else
                {
                    // Testa mais 4 pontos da celula, para saber se foi so o centro.
                    int outros = 0;
                    float[] d = { 0.2f, 0.8f };
                    foreach (float dx in d) foreach (float dy in d)
                        foreach (Collider2D t in Physics2D.OverlapPointAll(new Vector2(x + dx, y + dy)))
                            if (t.gameObject == colisao.gameObject) outros++;
                    errados++;
                    if (erros.Count < 40) erros.Add("(" + x + "," + y + " tile=" + temTile + " outros pontos solidos=" + outros + "/4)");
                }
            }
        }
        Debug.Log("[Verificar] fisica x tiles: certos=" + certos + " errados=" + errados + " " + string.Join(" ", erros.ToArray()));

        foreach (Collider2D c in Object.FindObjectsOfType<Collider2D>())
        {
            if (c.isTrigger && c.gameObject.tag.Contains("fase"))
            {
                Collider2D[] toques = Physics2D.OverlapBoxAll(c.bounds.center, c.bounds.size, 0f);
                bool bateNaAgua = false;
                foreach (Collider2D t in toques) if (t.gameObject == colisao.gameObject) bateNaAgua = true;
                Debug.Log("[Verificar] porta " + c.gameObject.tag + " em " + c.bounds.center + " encosta na colisao da agua=" + bateNaAgua);
            }
        }
    }

    // Empurra o jogador para a agua por 3 segundos (sem salvar a cena)
    // e mostra onde ele parou.
    public static void Simular()
    {
        EditorSceneManager.OpenScene(CENA, OpenSceneMode.Single);
        GameObject jogador = GameObject.Find("Jogador");
        Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();
        Vector2[] direcoes = { Vector2.down, Vector2.left, Vector2.right, Vector2.up };
        Vector2[] inicios = { new Vector2(15f, 3f), new Vector2(15f, 15f), new Vector2(50f, 15f), new Vector2(34f, 12.6f) };
        Physics2D.autoSimulation = false;
        for (int k = 0; k < inicios.Length; k++)
        {
            foreach (Vector2 dir in direcoes)
            {
                corpo.position = inicios[k];
                jogador.transform.position = inicios[k];
                Physics2D.SyncTransforms();
                for (int i = 0; i < 150; i++)
                {
                    corpo.velocity = dir * 5f;
                    Physics2D.Simulate(0.02f);
                }
                Debug.Log("[Simular] de " + inicios[k] + " para " + dir + " parou em " + corpo.position);
            }
        }
    }

    static void Teste(Tilemap colisao, string nome, Vector2 ponto)
    {
        Collider2D c = Physics2D.OverlapPoint(ponto);
        bool solido = c != null && c.gameObject == colisao.gameObject;
        Debug.Log("[Verificar] " + nome + " -> solido=" + solido + (c != null ? " (" + c.name + ")" : ""));
    }
}
