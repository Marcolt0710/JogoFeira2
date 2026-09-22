using UnityEngine;
// Biblioteca que permite trocar de cena (aula "Nova fase / Game over")
using UnityEngine.SceneManagement;
// Biblioteca para trabalhar com textos e imagens da tela (aula "Apresentação de textos")
using UnityEngine.UI;

// Tela em que o jogador se identifica antes de começar a fase.
// Aparece quando o personagem entra numa casa no mapa (PersonagemMapa).
// O jogador digita no teclado físico: nome e celular OU e-mail (mesmo campo).
// Por enquanto os dados só ficam guardados no PlayerPrefs, não vão para nenhum lugar.
public class TelaIdentificacao : MonoBehaviour {

	// Os dois campos de texto (InputField)
	public InputField campoNome;
	public InputField campoContato;

	// A imagem de fundo de cada campo: apagada ou acesa (campo selecionado)
	public Image fundoNome;
	public Image fundoContato;
	public Sprite campoApagado;
	public Sprite campoAceso;

	// A placa do CONFIRMAR: acende quando os dois campos estão preenchidos
	public Image placaConfirmar;
	public Text textoConfirmar;
	public Sprite placaApagada;
	public Sprite placaAcesa;

	// Texto de ajuda embaixo dos campos (também mostra os erros)
	public Text UITextAviso;

	InputField campoAtual; // Campo em que o jogador está digitando
	string faseEscolhida;  // Cena da fase que o jogador escolheu no mapa
	bool cursorParaOFim;   // Leva o cursor para o fim do texto no próximo quadro

	string AVISO_PADRAO = "Usamos só para registrar o seu tempo no ranking da feira.";
	Color corAviso = new Color (0.43f, 0.28f, 0.16f);
	Color corErro = new Color (0.67f, 0.16f, 0.08f);
	Color corConfirmarApagado = new Color (0.79f, 0.63f, 0.48f);
	Color corConfirmarAceso = new Color (1f, 0.9f, 0.54f);

	void Start () {
		// O PersonagemMapa guarda aqui o nome da cena da casa em que ele entrou.
		faseEscolhida = PlayerPrefs.GetString ("FaseEscolhida", "fase01");

		campoNome.text = "";
		campoContato.text = "";
		// Não deixa o TAB virar uma letra dentro do campo
		campoNome.onValidateInput = TirarTab;
		campoContato.onValidateInput = TirarTab;
		// Quando o jogador volta a digitar, o aviso de erro some.
		campoNome.onValueChanged.AddListener (LimparAviso);
		campoContato.onValueChanged.AddListener (LimparAviso);

		MostrarAviso (AVISO_PADRAO, false);
		SelecionarCampo (campoNome);
	}

	void Update () {
		// Se o jogador clicou num campo com o mouse, ele passa a ser o atual.
		if (campoNome.isFocused == true) {
			campoAtual = campoNome;
		}
		if (campoContato.isFocused == true) {
			campoAtual = campoContato;
		}

		TrocarCampo ();
		Confirmar ();
		Voltar ();
		AtualizarVisual ();
	}

	// TAB: vai para o outro campo
	void TrocarCampo(){
		if (Input.GetKeyDown (KeyCode.Tab) == false) {
			return;
		}

		if (campoAtual == campoNome) {
			SelecionarCampo (campoContato);
		} else {
			SelecionarCampo (campoNome);
		}
	}

	// ENTER (ou o X do controle de PlayStation, Joystick1Button1)
	void Confirmar(){
		bool apertouEnter = Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.KeypadEnter);
		bool apertouX = Input.GetKeyDown (KeyCode.Joystick1Button1);

		if (apertouEnter == false && apertouX == false) {
			return;
		}

		// No campo do nome, com o nome digitado, o ENTER só passa para o próximo campo.
		if (apertouEnter == true && campoAtual == campoNome && campoNome.text.Trim () != "" && campoContato.text.Trim () == "") {
			SelecionarCampo (campoContato);
			return;
		}

		TentarConfirmar ();
	}

	// Clique do mouse na placa CONFIRMAR (ligado no Button da placa)
	public void CliqueConfirmar(){
		TentarConfirmar ();
	}

	// Confere os dois campos. Se estiver tudo certo, guarda e abre a fase.
	void TentarConfirmar(){
		string nome = campoNome.text.Trim ();
		string contato = campoContato.text.Trim ();

		if (nome == "") {
			MostrarAviso ("Digite o seu nome.", true);
			SelecionarCampo (campoNome);
			return;
		}

		if (ContatoValido (contato) == false) {
			MostrarAviso ("Digite um celular com DDD ou um e-mail válido.", true);
			SelecionarCampo (campoContato);
			return;
		}

		// Guarda quem está jogando. Depois o tempo da fase vai junto com isso.
		PlayerPrefs.SetString ("JogadorNome", nome);
		PlayerPrefs.SetString ("JogadorContato", contato);
		PlayerPrefs.Save ();

		SceneManager.LoadScene (faseEscolhida);
	}

	// ESC (ou o Círculo do controle, Joystick1Button2): volta para o mapa
	void Voltar(){
		bool apertou = Input.GetKeyDown (KeyCode.Escape) || Input.GetKeyDown (KeyCode.Joystick1Button2);

		if (apertou == true) {
			SceneManager.LoadScene ("menudefase");
		}
	}

	// Celular: pelo menos 10 números (DDD + número).
	// E-mail: tem @, tem texto antes dele e um ponto depois dele.
	bool ContatoValido(string contato){
		if (contato.Contains ("@") == true) {
			int arroba = contato.IndexOf ("@");
			int ponto = contato.LastIndexOf (".");
			bool temEspaco = contato.Contains (" ");
			return arroba > 0 && ponto > arroba + 1 && ponto < contato.Length - 1 && temEspaco == false;
		}

		int numeros = 0;
		for (int i = 0; i < contato.Length; i++) {
			if (char.IsDigit (contato [i]) == true) {
				numeros = numeros + 1;
			}
		}
		return numeros >= 10;
	}

	// Acende o campo selecionado e a placa do CONFIRMAR quando dá para confirmar.
	void AtualizarVisual(){
		if (campoAtual == campoNome) {
			fundoNome.sprite = campoAceso;
			fundoContato.sprite = campoApagado;
		} else {
			fundoNome.sprite = campoApagado;
			fundoContato.sprite = campoAceso;
		}

		bool pronto = campoNome.text.Trim () != "" && ContatoValido (campoContato.text.Trim ()) == true;
		if (pronto == true) {
			placaConfirmar.sprite = placaAcesa;
			textoConfirmar.color = corConfirmarAceso;
		} else {
			placaConfirmar.sprite = placaApagada;
			textoConfirmar.color = corConfirmarApagado;
		}
	}

	void SelecionarCampo(InputField campo){
		campoAtual = campo;
		campo.Select ();
		campo.ActivateInputField ();
		// O InputField seleciona o texto todo ao ganhar o foco; no próximo quadro
		// o cursor vai para o fim, para a próxima letra não apagar o que já tinha.
		cursorParaOFim = true;
	}

	// Chamado depois do Update, quando o InputField já selecionou o texto.
	void LateUpdate () {
		if (cursorParaOFim == true && campoAtual.isFocused == true) {
			campoAtual.MoveTextEnd (false);
			cursorParaOFim = false;
		}
	}

	void LimparAviso(string texto){
		MostrarAviso (AVISO_PADRAO, false);
	}

	void MostrarAviso(string mensagem, bool erro){
		UITextAviso.text = mensagem;
		if (erro == true) {
			UITextAviso.color = corErro;
		} else {
			UITextAviso.color = corAviso;
		}
	}

	// Chamado pelo InputField a cada letra digitada. Devolver '\0' descarta a letra.
	char TirarTab(string texto, int posicao, char letra){
		if (letra == '\t') {
			return '\0';
		}
		return letra;
	}
}
