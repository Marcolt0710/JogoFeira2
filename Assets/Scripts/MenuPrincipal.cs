using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Menu principal do DishFace.
// Cada botao tem duas imagens: uma apagada e uma acesa.
// Um numero guarda em qual botao o jogador esta. Quando esse numero muda,
// o metodo AtualizarBotoes() repinta os quatro botoes.
public class MenuPrincipal : MonoBehaviour
{
    // --- as quatro Images que aparecem na tela ---
    public Image botaoJogar;
    public Image botaoOpcoes;
    public Image botaoControles;
    public Image botaoSair;

    // --- as duas versoes de cada botao ---
    public Sprite jogarApagado;
    public Sprite jogarAceso;
    public Sprite opcoesApagado;
    public Sprite opcoesAceso;
    public Sprite controlesApagado;
    public Sprite controlesAceso;
    public Sprite sairApagado;
    public Sprite sairAceso;

    // --- o que cada botao faz ---
    public string cenaDoJogo;
    public GameObject painelOpcoes;
    public GameObject painelControles;

    // 0 = jogar, 1 = opcoes, 2 = controles, 3 = sair
    private int botaoSelecionado;
    // so deixa andar de novo depois que o jogador soltar o analogico
    private bool direcaoLiberada;
    private bool painelAberto;

    void Start()
    {
        botaoSelecionado = 0;
        direcaoLiberada = true;
        painelAberto = false;
        AtualizarBotoes();
    }

    void Update()
    {
        if (painelAberto == true)
        {
            FecharPainel();
            return;
        }

        Navegar();
        Confirmar();
    }

    // Le o analogico e o direcional e muda o botao selecionado.
    void Navegar()
    {
        float vertical = Input.GetAxisRaw("Vertical");

        // se o analogico esta parado, tenta o direcional (D-Pad)
        if (vertical == 0)
        {
            vertical = Input.GetAxisRaw("DPadVertical");
        }

        // para baixo = proximo botao
        if (vertical < -0.5f && direcaoLiberada == true)
        {
            botaoSelecionado = botaoSelecionado + 1;

            if (botaoSelecionado > 3)
            {
                botaoSelecionado = 0;
            }

            direcaoLiberada = false;
            AtualizarBotoes();
        }

        // para cima = botao anterior
        if (vertical > 0.5f && direcaoLiberada == true)
        {
            botaoSelecionado = botaoSelecionado - 1;

            if (botaoSelecionado < 0)
            {
                botaoSelecionado = 3;
            }

            direcaoLiberada = false;
            AtualizarBotoes();
        }

        // analogico voltou para o meio: pode andar de novo
        if (vertical > -0.5f && vertical < 0.5f)
        {
            direcaoLiberada = true;
        }
    }

    // Apaga os quatro botoes e acende so o que esta selecionado.
    void AtualizarBotoes()
    {
        botaoJogar.sprite = jogarApagado;
        botaoOpcoes.sprite = opcoesApagado;
        botaoControles.sprite = controlesApagado;
        botaoSair.sprite = sairApagado;

        if (botaoSelecionado == 0)
        {
            botaoJogar.sprite = jogarAceso;
        }

        if (botaoSelecionado == 1)
        {
            botaoOpcoes.sprite = opcoesAceso;
        }

        if (botaoSelecionado == 2)
        {
            botaoControles.sprite = controlesAceso;
        }

        if (botaoSelecionado == 3)
        {
            botaoSair.sprite = sairAceso;
        }
    }

    // Botao X do controle de PlayStation (ou Enter no teclado).
    // No Windows o controle do PS4 fica assim no Unity:
    // joystick button 0 = Quadrado, 1 = X, 2 = Circulo, 3 = Triangulo
    // (fonte: ritchielozada.com/2016/11/21/playstation-4-dual-shock-controller-input-mapping-with-unity-on-windows-10/)
    void Confirmar()
    {
        // X do controle (Joystick1Button1) ou o "Submit" (Enter / espaco)
        bool apertou = Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetButtonDown("Submit");

        if (apertou == false)
        {
            return;
        }

        if (botaoSelecionado == 0)
        {
            Jogar();
        }

        if (botaoSelecionado == 1)
        {
            AbrirPainel(painelOpcoes);
        }

        if (botaoSelecionado == 2)
        {
            AbrirPainel(painelControles);
        }

        if (botaoSelecionado == 3)
        {
            Sair();
        }
    }

    void Jogar()
    {
        if (cenaDoJogo == "")
        {
            Debug.Log("Preencha o campo Cena Do Jogo no Inspector.");
            return;
        }

        SceneManager.LoadScene(cenaDoJogo);
    }

    void AbrirPainel(GameObject painel)
    {
        if (painel == null)
        {
            Debug.Log("Nenhum painel foi ligado neste botao.");
            return;
        }

        painel.SetActive(true);
        painelAberto = true;
    }

    // Botao Circulo do controle de PlayStation (ou Esc no teclado) fecha o painel aberto.
    void FecharPainel()
    {
        // Circulo do controle (Joystick1Button2) ou o "Cancel" (Esc)
        bool apertou = Input.GetKeyDown(KeyCode.Joystick1Button2) || Input.GetButtonDown("Cancel");

        if (apertou == false)
        {
            return;
        }

        if (painelOpcoes != null)
        {
            painelOpcoes.SetActive(false);
        }

        if (painelControles != null)
        {
            painelControles.SetActive(false);
        }

        painelAberto = false;
        direcaoLiberada = true;
    }

    void Sair()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
