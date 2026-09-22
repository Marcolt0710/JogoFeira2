using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cena-placeholder de fase, só para fechar o ciclo mapa -> fase -> mapa.
/// Mostra qual fase e qual dificuldade vieram do cartão e oferece dois botões:
/// concluir (marca no save e volta) ou desistir (volta sem marcar).
///
/// Quando a fase de verdade existir, é só chamar SessaoDeFase.ConcluirEVoltar()
/// no fim dela e jogar esta cena fora.
/// </summary>
public class FaseTeste : MonoBehaviour
{
    public Text titulo;
    public Text subtitulo;
    public Button botaoConcluir;
    public Button botaoVoltar;

    private void Start()
    {
        string id = SessaoDeFase.IdFase;

        if (titulo != null)
            titulo.text = string.IsNullOrEmpty(id) ? "(fase aberta direto)" : id;

        if (subtitulo != null)
        {
            subtitulo.text = string.IsNullOrEmpty(id)
                ? "Entre pelo mapa para o progresso ser gravado."
                : "Dificuldade: " + SessaoDeFase.Dificuldade;
        }

        if (botaoConcluir != null)
            botaoConcluir.onClick.AddListener(SessaoDeFase.ConcluirEVoltar);

        if (botaoVoltar != null)
            botaoVoltar.onClick.AddListener(SessaoDeFase.DesistirEVoltar);
    }

    private void Update()
    {
        // Atalhos de teclado, para testar sem tirar a mão do teclado.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
            SessaoDeFase.ConcluirEVoltar();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
            SessaoDeFase.DesistirEVoltar();
    }
}
