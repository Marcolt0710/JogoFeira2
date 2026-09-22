using UnityEngine;

/// <summary>
/// Arruma o mapa toda vez que a cena abre: põe o jogador no lugar certo, acende as
/// bandeirinhas das fases vencidas e reavalia os caminhos bloqueados.
///
/// Sem isto, voltar de uma fase jogaria o jogador na posição em que o prefab
/// nasceu, e não na porta de onde ele saiu.
/// </summary>
public class MapaInicializador : MonoBehaviour
{
    [Tooltip("Onde o jogador começa numa partida nova, sem save.")]
    public Transform pontoInicial;

    public PlayerMapController jogador;

    private void Start()
    {
        if (jogador == null) jogador = FindObjectOfType<PlayerMapController>();

        PosicionarJogador();
        AtualizarMapa();

        // Ao voltar de uma fase o movimento pode ter ficado travado do cartão.
        PlayerMapController.Congelado = false;
        SessaoDeFase.Limpar();
    }

    private void PosicionarJogador()
    {
        if (jogador == null) return;

        ProgressManager prog = ProgressManager.Instancia;

        // Voltando de uma fase: reaparece na frente do nó de onde saiu.
        if (!string.IsNullOrEmpty(prog.UltimoNo))
        {
            LevelNode no = AcharNo(prog.UltimoNo);
            if (no != null)
            {
                jogador.Reposicionar(no.PontoDeRetorno);
                prog.ConsumirUltimoNo();
                return;
            }
        }

        if (prog.TemPosicaoSalva)
        {
            jogador.Reposicionar(prog.PosicaoSalva);
            return;
        }

        if (pontoInicial != null) jogador.Reposicionar(pontoInicial.position);
    }

    private static LevelNode AcharNo(string id)
    {
        LevelNode[] nos = FindObjectsOfType<LevelNode>();
        for (int i = 0; i < nos.Length; i++)
            if (nos[i].id == id) return nos[i];
        return null;
    }

    /// <summary>Repassa bandeirinhas e portões. Pode ser chamado a qualquer momento.</summary>
    public void AtualizarMapa()
    {
        LevelNode[] nos = FindObjectsOfType<LevelNode>();
        for (int i = 0; i < nos.Length; i++) nos[i].AtualizarVisual();

        PathGate[] portoes = FindObjectsOfType<PathGate>();
        for (int i = 0; i < portoes.Length; i++) portoes[i].Atualizar();
    }

    private void OnDisable()
    {
        // Guarda onde o jogador parou, para reabrir o mapa no mesmo lugar.
        if (jogador != null)
            ProgressManager.Instancia.SalvarPosicao(jogador.transform.position);
    }
}
