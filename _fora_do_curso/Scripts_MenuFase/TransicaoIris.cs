using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fecha a tela num círculo e carrega a fase; ao voltar para o mapa, abre de novo.
/// O desenho do círculo é do shader MenuFase/IrisUI.
///
/// Sobrevive à troca de cena, senão a tela voltaria a ficar limpa no meio da
/// transição, quando o objeto da cena antiga fosse destruído.
/// </summary>
public class TransicaoIris : MonoBehaviour
{
    public const float RAIO_ABERTO = 1.5f;
    public const float RAIO_FECHADO = 0f;

    [Tooltip("Segundos para fechar ou abrir o círculo.")]
    public float duracao = 0.55f;

    public Image imagem;

    private Material material;
    private static TransicaoIris instancia;

    public static TransicaoIris Instancia { get { return instancia; } }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);

        if (imagem == null) imagem = GetComponentInChildren<Image>(true);
        if (imagem != null)
        {
            // Instancia o material: mexer no sharedMaterial sujaria o asset no disco.
            material = Instantiate(imagem.material);
            imagem.material = material;
            imagem.raycastTarget = false;
        }

        DefinirRaio(RAIO_ABERTO);
        if (imagem != null) imagem.gameObject.SetActive(false);
    }

    private void DefinirRaio(float r)
    {
        if (material == null) return;
        material.SetFloat("_Raio", r);
        material.SetFloat("_Aspecto", (float)Screen.width / Mathf.Max(1, Screen.height));
    }

    /// <summary>Fecha o círculo, carrega a cena e abre de novo do outro lado.</summary>
    public void IrPara(string nomeCena)
    {
        StartCoroutine(Rotina(nomeCena));
    }

    private IEnumerator Rotina(string nomeCena)
    {
        if (imagem != null) imagem.gameObject.SetActive(true);

        yield return Animar(RAIO_ABERTO, RAIO_FECHADO);

        AsyncOperation op = SceneManager.LoadSceneAsync(nomeCena);
        if (op == null)
        {
            Debug.LogError("[MenuFase] cena '" + nomeCena + "' não carregou. " +
                           "Ela está em File > Build Settings?");
            yield return Animar(RAIO_FECHADO, RAIO_ABERTO);
            if (imagem != null) imagem.gameObject.SetActive(false);
            yield break;
        }

        while (!op.isDone) yield return null;

        // Um frame para a cena nova montar antes de revelar.
        yield return null;

        yield return Animar(RAIO_FECHADO, RAIO_ABERTO);
        if (imagem != null) imagem.gameObject.SetActive(false);
    }

    private IEnumerator Animar(float de, float para)
    {
        float t = 0f;
        while (t < duracao)
        {
            // unscaledDeltaTime: a transição roda mesmo com o jogo pausado (timeScale 0).
            t += Time.unscaledDeltaTime;
            DefinirRaio(Mathf.Lerp(de, para, Mathf.Clamp01(t / duracao)));
            yield return null;
        }
        DefinirRaio(para);
    }

    /// <summary>
    /// Chamado pelas fases-teste para voltar ao mapa sem precisar de referência.
    /// </summary>
    public static void Carregar(string nomeCena)
    {
        if (instancia != null) instancia.IrPara(nomeCena);
        else SceneManager.LoadScene(nomeCena);
    }
}
