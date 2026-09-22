using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O cartão que sobe quando o jogador confirma numa fase: nome, estado,
/// dificuldade e os botões Entrar / Voltar.
///
/// A navegação é feita na mão (sem EventSystem.SetSelectedGameObject), igual ao
/// MenuPrincipal.cs que já existe no projeto — inclusive o truque do
/// direcaoLiberada, que impede o menu de varrer as linhas todas num frame.
/// </summary>
public class LevelCard : MonoBehaviour
{
    // Linhas navegáveis do cartão.
    private const int LINHA_DIFICULDADE = 0;
    private const int LINHA_ENTRAR = 1;
    private const int LINHA_VOLTAR = 2;

    [Header("Estrutura")]
    public GameObject painel;
    public RectTransform cartao;
    public CanvasGroup grupo;

    [Header("Textos")]
    public Text textoNome;
    public Text textoEstado;
    public Text textoSimples;
    public Text textoNormal;
    public Text textoEntrar;
    public Text textoVoltar;

    [Header("Cores")]
    public Color corNormal = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color corSelecionado = new Color(1f, 0.93f, 0.45f, 1f);
    public Color corDesligado = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Animação")]
    public float duracaoEntrada = 0.18f;
    public float escalaInicial = 0.75f;
    public float deslocamentoInicial = 40f;

    private static LevelCard instancia;
    public static LevelCard Instancia { get { return instancia; } }

    /// <summary>A UI usa isso para saber que o mapa está em pausa.</summary>
    public static bool Aberto { get; private set; }

    private LevelNode noAtual;
    private int linha = LINHA_ENTRAR;
    private Dificuldade dificuldade = Dificuldade.Normal;
    private bool direcaoLiberada = true;
    private bool aceitandoInput;
    private bool desbloqueada;

    private void Awake()
    {
        instancia = this;
        Aberto = false;
        if (painel != null) painel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
            Aberto = false;
        }
    }

    // ---------------------------------------------------------------- abrir

    public void Mostrar(LevelNode no)
    {
        if (no == null) return;

        noAtual = no;
        desbloqueada = no.Desbloqueada;

        // Se já zerou antes, reabre na dificuldade que usou da última vez.
        dificuldade = no.Concluida
            ? ProgressManager.Instancia.DificuldadeDe(no.id)
            : Dificuldade.Normal;

        linha = desbloqueada ? LINHA_ENTRAR : LINHA_VOLTAR;

        if (textoNome != null) textoNome.text = no.nomeExibicao;
        if (textoEstado != null) textoEstado.text = TextoDoEstado(no);

        Aberto = true;
        PlayerMapController.Congelado = true;

        if (painel != null) painel.SetActive(true);
        AtualizarDestaques();

        aceitandoInput = false;   // só escuta depois da animação, senão o mesmo
        StopAllCoroutines();      // toque que abriu o cartão já confirmaria Entrar
        StartCoroutine(AnimarEntrada());
    }

    private string TextoDoEstado(LevelNode no)
    {
        if (!desbloqueada)
        {
            string faltam = ListarRequisitosPendentes(no);
            return string.IsNullOrEmpty(faltam) ? "BLOQUEADA" : "BLOQUEADA — falta " + faltam;
        }
        return no.Concluida ? "CONCLUÍDA" : "DISPONÍVEL";
    }

    private static string ListarRequisitosPendentes(LevelNode no)
    {
        if (no.exigeConcluidas == null) return "";

        string s = "";
        for (int i = 0; i < no.exigeConcluidas.Length; i++)
        {
            string req = no.exigeConcluidas[i];
            if (string.IsNullOrEmpty(req)) continue;
            if (ProgressManager.Instancia.Concluida(req)) continue;
            if (s.Length > 0) s += ", ";
            s += req;
        }
        return s;
    }

    private IEnumerator AnimarEntrada()
    {
        float t = 0f;
        Vector2 posFinal = cartao != null ? cartao.anchoredPosition : Vector2.zero;
        Vector2 posInicial = posFinal + Vector2.down * deslocamentoInicial;

        while (t < duracaoEntrada)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracaoEntrada);

            // Desacelera no fim; dá o peso de "cartão assentando".
            float suave = 1f - (1f - p) * (1f - p);

            if (cartao != null)
            {
                cartao.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, suave);
                cartao.anchoredPosition = Vector2.Lerp(posInicial, posFinal, suave);
            }
            if (grupo != null) grupo.alpha = suave;

            yield return null;
        }

        if (cartao != null)
        {
            cartao.localScale = Vector3.one;
            cartao.anchoredPosition = posFinal;
        }
        if (grupo != null) grupo.alpha = 1f;

        aceitandoInput = true;
    }

    // ---------------------------------------------------------------- input

    private void Update()
    {
        if (!Aberto || !aceitandoInput) return;

        float y = Eixo("Vertical", "DPadVertical");
        float x = Eixo("Horizontal", "DPadHorizontal");

        if (Mathf.Abs(y) < 0.4f && Mathf.Abs(x) < 0.4f) direcaoLiberada = true;

        if (direcaoLiberada)
        {
            if (y > 0.4f) { Mover(-1); direcaoLiberada = false; }
            else if (y < -0.4f) { Mover(1); direcaoLiberada = false; }
            else if (Mathf.Abs(x) > 0.4f && linha == LINHA_DIFICULDADE)
            {
                dificuldade = x > 0f ? Dificuldade.Normal : Dificuldade.Simples;
                AtualizarDestaques();
                direcaoLiberada = false;
            }
        }

        if (Confirmar()) Acionar();
        else if (Cancelar()) Fechar();
    }

    private void Mover(int passo)
    {
        int primeira = desbloqueada ? LINHA_DIFICULDADE : LINHA_VOLTAR;
        int ultima = LINHA_VOLTAR;

        linha += passo;
        if (linha < primeira) linha = ultima;
        if (linha > ultima) linha = primeira;

        AtualizarDestaques();
    }

    private void Acionar()
    {
        if (linha == LINHA_VOLTAR) { Fechar(); return; }

        // Na linha da dificuldade, confirmar apenas desce para o Entrar.
        if (linha == LINHA_DIFICULDADE) { linha = LINHA_ENTRAR; AtualizarDestaques(); return; }

        if (!desbloqueada)
        {
            Debug.Log("[MenuFase] '" + noAtual.nomeExibicao + "' ainda está bloqueada.");
            return;
        }

        Entrar();
    }

    private void Entrar()
    {
        if (noAtual == null) return;

        if (string.IsNullOrEmpty(noAtual.cenaDaFase))
        {
            Debug.LogWarning("[MenuFase] o nó '" + noAtual.id + "' está sem cena definida.");
            return;
        }

        SessaoDeFase.Definir(noAtual.id, dificuldade);
        ProgressManager.Instancia.SalvarSaida(noAtual.id, noAtual.PontoDeRetorno);

        aceitandoInput = false;
        Aberto = false;
        if (painel != null) painel.SetActive(false);

        // O jogador segue congelado: a cena do mapa vai ser descarregada mesmo.
        TransicaoIris.Carregar(noAtual.cenaDaFase);
    }

    public void Fechar()
    {
        StopAllCoroutines();
        aceitandoInput = false;
        Aberto = false;
        noAtual = null;

        if (painel != null) painel.SetActive(false);
        PlayerMapController.Congelado = false;
    }

    // ---------------------------------------------------------------- visual

    private void AtualizarDestaques()
    {
        Pintar(textoEntrar, linha == LINHA_ENTRAR, desbloqueada);
        Pintar(textoVoltar, linha == LINHA_VOLTAR, true);

        bool naLinhaDif = linha == LINHA_DIFICULDADE;
        Pintar(textoSimples, naLinhaDif && dificuldade == Dificuldade.Simples, desbloqueada);
        Pintar(textoNormal, naLinhaDif && dificuldade == Dificuldade.Normal, desbloqueada);

        // Fora da linha da dificuldade, a escolhida continua marcada, só que apagada.
        if (!naLinhaDif && desbloqueada)
        {
            if (dificuldade == Dificuldade.Simples && textoSimples != null) textoSimples.color = corNormal;
            if (dificuldade == Dificuldade.Normal && textoNormal != null) textoNormal.color = corNormal;
        }

        if (textoEntrar != null)
            textoEntrar.text = desbloqueada ? "> ENTRAR" : "  (bloqueada)";
    }

    private void Pintar(Text t, bool selecionado, bool habilitado)
    {
        if (t == null) return;
        if (!habilitado) { t.color = corDesligado; return; }
        t.color = selecionado ? corSelecionado : corNormal;
    }

    // ---------------------------------------------------------------- entrada bruta

    private static float Eixo(string principal, string dpad)
    {
        float v = 0f;
        try { v = Input.GetAxisRaw(principal); } catch (System.Exception) { }
        try
        {
            float d = Input.GetAxisRaw(dpad);
            if (Mathf.Abs(d) > Mathf.Abs(v)) v = d;
        }
        catch (System.Exception) { }
        return v;
    }

    private static bool Confirmar()
    {
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            return true;
        try { if (Input.GetButtonDown("Submit")) return true; } catch (System.Exception) { }
        try { if (Input.GetButtonDown("Fire1")) return true; } catch (System.Exception) { }
        return false;
    }

    private static bool Cancelar()
    {
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Backspace))
            return true;
        try { if (Input.GetButtonDown("Cancel")) return true; } catch (System.Exception) { }
        return false;
    }
}
