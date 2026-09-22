using System.Globalization;
using UnityEngine;
// Biblioteca que permite trocar de cena (aula "Nova fase / Game over")
using UnityEngine.SceneManagement;
// Biblioteca para trabalhar com textos da tela (aula "Apresentação de textos")
using UnityEngine.UI;

// Conta o tempo do jogador na fase e mostra na tela.
// Quando a fase termina, chame Concluir(): o tempo vai para a Planilha e o jogo volta ao mapa.
//
// TESTE: enquanto as fases não existem, o botão X (ou Enter) conclui a fase.
// Quando a fase de verdade estiver pronta, apague o TesteConcluir() do Update
// e chame Concluir() no fim da fase (por exemplo, ao encostar na chegada).
public class CronometroFase : MonoBehaviour {

	// Texto no alto da tela com o nome do jogador e o tempo.
	public Text UITextTempo;

	float tempoInicial;
	bool terminou;
	string nomeJogador;

	void Start () {
		tempoInicial = Time.time;
		terminou = false;
		nomeJogador = PlayerPrefs.GetString ("JogadorNome", "");
	}

	void Update () {
		if (terminou == false) {
			UITextTempo.text = nomeJogador + "\nTEMPO  " + Formatar (Time.time - tempoInicial);
		}
		TesteConcluir ();
	}

	void TesteConcluir(){
		// Botão X do controle de PlayStation (Joystick1Button1) ou Enter
		bool apertou = Input.GetKeyDown (KeyCode.Joystick1Button1) || Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.KeypadEnter);

		if (apertou == true) {
			Concluir ();
		}
	}

	public void Concluir(){
		if (terminou == true) {
			return;
		}
		terminou = true;

		float segundos = Time.time - tempoInicial;
		// O nome da cena (fase01, fase02...) é o nome da fase na planilha.
		Planilha.RegistrarTempo (SceneManager.GetActiveScene ().name, segundos);
		SceneManager.LoadScene ("menudefase");
	}

	// 83.4 segundos -> "01:23.4"
	public static string Formatar(float segundos){
		// Arredonda antes, senão 59.96 s apareceria como "00:60.0"
		segundos = Mathf.Round (segundos * 10f) / 10f;
		int minutos = (int)(segundos / 60f);
		float resto = segundos - minutos * 60f;
		return minutos.ToString ("00") + ":" + resto.ToString ("00.0", CultureInfo.InvariantCulture);
	}
}
