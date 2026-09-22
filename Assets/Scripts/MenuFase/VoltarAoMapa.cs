using UnityEngine;
// Biblioteca que permite trocar de cena (aula "Nova fase / Game over")
using UnityEngine.SceneManagement;

// Script das fases de teste (fase01 a fase05).
// Enquanto a fase de verdade não existe, ela só volta para o mapa.
public class VoltarAoMapa : MonoBehaviour {

	void Update () {
		VoltarParaOMapa ();
	}

	void VoltarParaOMapa(){
		// Enter, Esc ou o botão Círculo do controle de PlayStation (Joystick1Button2).
		// No Windows: joystick button 0 = Quadrado, 1 = X, 2 = Círculo, 3 = Triângulo.
		bool apertou = Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.Escape) || Input.GetKeyDown (KeyCode.Joystick1Button2);

		if (apertou == true) {
			SceneManager.LoadScene ("menudefase");
		}
	}
}
