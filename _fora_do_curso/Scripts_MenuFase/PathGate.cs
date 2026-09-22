using UnityEngine;

/// <summary>
/// Um trecho de caminho que só libera depois que certas fases forem vencidas:
/// a ponte que aparece, a escada que destrava, a pedra que some.
///
/// Põe isto no objeto que representa o bloqueio. Quando os requisitos estiverem
/// cumpridos, o bloqueio some e o colisor que barrava o jogador é desligado.
/// </summary>
public class PathGate : MonoBehaviour
{
    public enum Modo
    {
        /// <summary>O obstáculo some (pedra, tapume).</summary>
        SumirQuandoLiberado = 0,

        /// <summary>A passagem aparece (ponte, escada).</summary>
        AparecerQuandoLiberado = 1
    }

    [Header("Requisitos")]
    [Tooltip("Ids das fases que precisam estar concluídas para liberar.")]
    public string[] exigeConcluidas = new string[0];

    [Header("Comportamento")]
    public Modo modo = Modo.SumirQuandoLiberado;

    [Tooltip("O que ligar/desligar. Vazio = o próprio objeto.")]
    public GameObject alvo;

    [Tooltip("Colisor que barra o jogador enquanto estiver fechado. " +
             "Deixe vazio se não houver.")]
    public Collider2D barreira;

    public bool Liberado
    {
        get { return ProgressManager.Instancia.Desbloqueada(exigeConcluidas); }
    }

    private void Start()
    {
        Atualizar();
    }

    /// <summary>
    /// Chamado no Start e de novo sempre que o jogador volta de uma fase,
    /// pelo MapaInicializador.
    /// </summary>
    public void Atualizar()
    {
        bool liberado = Liberado;
        GameObject g = alvo != null ? alvo : gameObject;

        bool visivel = modo == Modo.SumirQuandoLiberado ? !liberado : liberado;

        // Nunca desativar o próprio GameObject deste componente: ele pararia de
        // rodar e nunca mais conseguiria se reabrir.
        if (g == gameObject)
        {
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++) sprites[i].enabled = visivel;
        }
        else
        {
            g.SetActive(visivel);
        }

        if (barreira != null) barreira.enabled = !liberado;
    }
}
