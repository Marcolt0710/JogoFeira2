using UnityEngine;
// Biblioteca que permite trocar de cena (aula "Nova fase / Game over")
using UnityEngine.SceneManagement;
// Biblioteca para trabalhar com textos da tela (aula "Apresentação de textos")
using UnityEngine.UI;

// Personagem que anda pelo mapa de fases, visto de cima.
// Movimento: aula "Movimento Vertical" (gravidade zero, anda em X e em Y).
// Espelhar:  aula "Flip".
// Animações: aula "Animações" (parâmetro "andando").
// Controle:  aula "Entradas de dados" (botões do controle de PlayStation).
// Entrar na fase: aulas "Tipos de Colisão" (trigger com tag) e "Nova fase".
public class PersonagemMapa : MonoBehaviour {

	// Texto que aparece na tela quando o personagem chega perto de uma casa.
	// No Inspector, arraste o texto "TXT_AVISO" para este campo.
	public Text UITextAviso;

	float VelocidadeX; // Velocidade no eixo X (horizontal)
	float VelocidadeY; // Velocidade no eixo Y (vertical)
	float VelocidadeHorizontalMaxima; // Velocidade máxima no eixo X
	float DirecaoHorizontal; // -1 para esquerda, 1 para direita, 0 para parado
	float VelocidadeVerticalMaxima; // Velocidade máxima no eixo Y
	float DirecaoVertical; // -1 para baixo, 1 para cima, 0 para parado
	Vector2 VetorVelocidadePersonagem; // Vetor com a velocidade em X e Y

	Rigidbody2D CorpoRigidoPersonagem; // Corpo rígido do personagem
	SpriteRenderer Renderer; // Usado para espelhar o personagem
	Animator PersonagemAnimator; // Usado para trocar as animações

	string TagTriggerEnter; // Tag da área em que o personagem entrou
	string TagTriggerExit; // Tag da área de onde o personagem saiu
	string FasePerto; // Nome da cena da fase que está perto ("" se nenhuma)

	void Start () {
		CorpoRigidoPersonagem = GetComponent<Rigidbody2D> ();
		Renderer = GetComponent<SpriteRenderer> ();
		PersonagemAnimator = GetComponent<Animator> ();

		// Visto de cima o personagem não cai, por isso a gravidade é zero.
		CorpoRigidoPersonagem.gravityScale = 0f;
		// Impede que o personagem gire ao esbarrar em alguma coisa.
		CorpoRigidoPersonagem.freezeRotation = true;

		VelocidadeX = 0f;
		VelocidadeY = 0f;
		VelocidadeHorizontalMaxima = 5f;
		VelocidadeVerticalMaxima = 5f;
		DirecaoHorizontal = 0f;
		DirecaoVertical = 0f;
		VetorVelocidadePersonagem = new Vector2 (VelocidadeX, VelocidadeY);
		CorpoRigidoPersonagem.velocity = VetorVelocidadePersonagem;

		TagTriggerEnter = "";
		TagTriggerExit = "";
		FasePerto = "";
		UITextAviso.text = "";
	}

	void Update () {
		MovimentoHorizontalFlip ();
		MovimentoVertical ();
		Animacao ();
		EntrarNaFase ();
	}

	void MovimentoHorizontalFlip(){
		// Analógico do controle, setas ou A/D: -1 esquerda, 1 direita, 0 parado
		DirecaoHorizontal = Input.GetAxis ("Horizontal");
		// Se o analógico está parado, lê o direcional (D-Pad) do controle.
		if (DirecaoHorizontal == 0) {
			DirecaoHorizontal = Input.GetAxis ("DPadHorizontal");
		}

		VelocidadeX = VelocidadeHorizontalMaxima * DirecaoHorizontal;
		VelocidadeY = CorpoRigidoPersonagem.velocity.y;
		VetorVelocidadePersonagem = new Vector2 (VelocidadeX, VelocidadeY);
		CorpoRigidoPersonagem.velocity = VetorVelocidadePersonagem;

		// As imagens de lado e de diagonal olham para a direita.
		// Andando para a esquerda, elas são espelhadas.
		if (DirecaoHorizontal < 0) {
			Renderer.flipX = true;
		} else if (DirecaoHorizontal > 0) {
			Renderer.flipX = false;
		}
	}

	void MovimentoVertical(){
		// Analógico do controle, setas ou W/S: -1 baixo, 1 cima, 0 parado
		DirecaoVertical = Input.GetAxis ("Vertical");
		// Se o analógico está parado, lê o direcional (D-Pad) do controle.
		if (DirecaoVertical == 0) {
			DirecaoVertical = Input.GetAxis ("DPadVertical");
		}

		VelocidadeX = CorpoRigidoPersonagem.velocity.x;
		VelocidadeY = VelocidadeVerticalMaxima * DirecaoVertical;
		VetorVelocidadePersonagem = new Vector2 (VelocidadeX, VelocidadeY);
		CorpoRigidoPersonagem.velocity = VetorVelocidadePersonagem;
	}

	void Animacao(){
		if (DirecaoHorizontal != 0 || DirecaoVertical != 0) {
			PersonagemAnimator.SetBool ("andando", true);

			// Para onde o personagem está virado. Juntando as três variáveis
			// saem 5 direções: frente, diagonal de frente, lado,
			// diagonal de costas e costas.
			if (DirecaoHorizontal != 0) {
				PersonagemAnimator.SetBool ("lado", true);
			} else {
				PersonagemAnimator.SetBool ("lado", false);
			}

			if (DirecaoVertical > 0) {
				PersonagemAnimator.SetBool ("costas", true);
				PersonagemAnimator.SetBool ("frente", false);
			} else if (DirecaoVertical < 0) {
				PersonagemAnimator.SetBool ("costas", false);
				PersonagemAnimator.SetBool ("frente", true);
			} else {
				PersonagemAnimator.SetBool ("costas", false);
				PersonagemAnimator.SetBool ("frente", false);
			}
		} else {
			// Parado: só desliga o "andando". A direção fica como estava,
			// assim ele continua virado para o último lado em que andou.
			PersonagemAnimator.SetBool ("andando", false);
		}
	}

	void EntrarNaFase(){
		// Botão X do controle de PlayStation (Joystick1Button1), Z ou Enter.
		// No Windows o controle do PS4 fica assim no Unity:
		// joystick button 0 = Quadrado, 1 = X, 2 = Círculo, 3 = Triângulo
		// (fonte: ritchielozada.com/2016/11/21/playstation-4-dual-shock-controller-input-mapping-with-unity-on-windows-10/)
		bool apertou = Input.GetKeyDown (KeyCode.Joystick1Button1) || Input.GetKeyDown (KeyCode.Z) || Input.GetKeyDown (KeyCode.Return);

		// Só entra se o personagem estiver na porta de alguma casa.
		if (apertou == true && FasePerto != "") {
			SceneManager.LoadScene (FasePerto);
		}
	}

	// Executa quando o personagem chega na porta de uma casa.
	// A porta de cada casa tem uma área (trigger) com a tag igual ao nome da cena.
	void OnTriggerEnter2D(Collider2D objetoTriggerEnter){
		TagTriggerEnter = objetoTriggerEnter.gameObject.tag;

		if (TagTriggerEnter == "fase01") {
			FasePerto = "fase01";
			UITextAviso.text = "FASE 1  ·  Casa da Vila\naperte  X  para entrar";
		}
		if (TagTriggerEnter == "fase02") {
			FasePerto = "fase02";
			UITextAviso.text = "FASE 2  ·  Ferreiro\naperte  X  para entrar";
		}
		if (TagTriggerEnter == "fase03") {
			FasePerto = "fase03";
			UITextAviso.text = "FASE 3  ·  Igreja\naperte  X  para entrar";
		}
	}

	// Executa quando o personagem sai da porta.
	void OnTriggerExit2D(Collider2D objetoTriggerExit){
		TagTriggerExit = objetoTriggerExit.gameObject.tag;

		if (TagTriggerExit.Contains ("fase")) {
			FasePerto = "";
			UITextAviso.text = "";
		}
	}
}
