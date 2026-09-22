using UnityEngine;

/// <summary>
/// Uma fase plantada no mapa: a construção onde o jogador chega, o aviso de
/// "aperte para entrar" e a bandeirinha de concluída.
///
/// Precisa de um Collider2D com isTrigger ligado, do tamanho da zona de interação.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelNode : MonoBehaviour
{
    [Header("Identidade")]
    [Tooltip("Identificador interno, usado no save e nos requisitos das outras fases.")]
    public string id = "fase_01";

    [Tooltip("O nome que aparece no cartão.")]
    public string nomeExibicao = "Fase 1";

    [Tooltip("Nome da cena a carregar. Precisa estar em File > Build Settings.")]
    public string cenaDaFase = "Fase_01";

    [Header("Desbloqueio")]
    [Tooltip("Ids das fases que precisam estar concluídas. Vazio = já nasce aberta.")]
    public string[] exigeConcluidas = new string[0];

    [Header("Marcadores")]
    [Tooltip("Aparece em cima da fase quando ela já foi concluída.")]
    public GameObject bandeira;

    [Tooltip("O aviso 'Z / Enter' que aparece quando o jogador chega perto.")]
    public GameObject aviso;

    [Header("Retorno")]
    [Tooltip("De onde o jogador reaparece ao voltar da fase. Vazio = o próprio nó.")]
    public Transform pontoDeRetorno;

    private bool jogadorDentro;
    private PlayerMapController jogador;

    public bool Concluida
    {
        get { return ProgressManager.Instancia.Concluida(id); }
    }

    public bool Desbloqueada
    {
        get { return ProgressManager.Instancia.Desbloqueada(exigeConcluidas); }
    }

    /// <summary>Onde o jogador deve reaparecer: um passo à frente da porta.</summary>
    public Vector2 PontoDeRetorno
    {
        get
        {
            return pontoDeRetorno != null
                ? (Vector2)pontoDeRetorno.position
                : (Vector2)transform.position + Vector2.down * 1.5f;
        }
    }

    private void Start()
    {
        MostrarAviso(false);
        AtualizarVisual();
    }

    /// <summary>Liga a bandeirinha se a fase já foi vencida.</summary>
    public void AtualizarVisual()
    {
        if (bandeira != null) bandeira.SetActive(Concluida);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        PlayerMapController p = outro.GetComponent<PlayerMapController>();
        if (p == null) return;

        jogador = p;
        jogadorDentro = true;
        MostrarAviso(true);
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        if (outro.GetComponent<PlayerMapController>() == null) return;

        jogadorDentro = false;
        jogador = null;
        MostrarAviso(false);
    }

    private void Update()
    {
        if (!jogadorDentro) return;

        // Se o cartão já está aberto, o aviso some e o nó para de escutar —
        // senão o mesmo toque abriria o cartão de novo assim que ele fechasse.
        bool cartaoAberto = LevelCard.Aberto;
        MostrarAviso(!cartaoAberto);
        if (cartaoAberto) return;

        if (Confirmou()) Abrir();
    }

    private static bool Confirmou()
    {
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;

        // Botão A do controle. Envolto em try porque o eixo pode não existir.
        try
        {
            if (Input.GetButtonDown("Submit")) return true;
            if (Input.GetButtonDown("Fire1")) return true;
        }
        catch (System.Exception) { }

        return false;
    }

    private void Abrir()
    {
        if (LevelCard.Instancia == null)
        {
            Debug.LogWarning("[MenuFase] não achei o LevelCard na cena; " +
                             "rode MenuFase > 3 - Montar cena do mapa.");
            return;
        }

        // Guarda de onde o jogador saiu para ele voltar aqui na frente.
        if (jogador != null)
            ProgressManager.Instancia.SalvarSaida(id, PontoDeRetorno);

        MostrarAviso(false);
        LevelCard.Instancia.Mostrar(this);
    }

    private void MostrarAviso(bool ligado)
    {
        if (aviso != null && aviso.activeSelf != ligado) aviso.SetActive(ligado);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(PontoDeRetorno, 0.25f);
    }
}
