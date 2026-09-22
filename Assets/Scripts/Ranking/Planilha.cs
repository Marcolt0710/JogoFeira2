using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
// Biblioteca para mandar e receber dados pela internet (UnityWebRequest)
using UnityEngine.Networking;

// Manda o tempo de cada jogador para a planilha do Google (pelo Apps Script).
//
// - Cada tempo é gravado primeiro num arquivo no computador (tempos_backup.csv),
//   assim nada se perde, mesmo sem internet.
// - Depois ele entra numa fila (pendentes.txt) e é enviado. Se a internet cair,
//   a fila tenta de novo a cada minuto e quando outro jogador terminar.
// - A URL do Apps Script fica no arquivo StreamingAssets/planilha.txt
//   (dá para trocar sem abrir o Unity). Sem URL, os tempos ficam só no backup.
//
// Uso: Planilha.RegistrarTempo("fase01", 83.4f);
public class Planilha : MonoBehaviour {

	// Tem que ser igual à SENHA do Apps Script (Codigo.gs).
	const string SENHA = "dishface-feira";

	static Planilha instancia; // Só existe uma, e ela passa de uma cena para outra

	string url;
	string arquivoPendentes;
	string arquivoBackup;
	bool enviando;

	// Guarda o tempo do jogador atual (nome e contato vêm da tela de identificação).
	public static void RegistrarTempo(string fase, float segundos){
		Pegar ().Registrar (fase, segundos);
	}

	// Roda sozinho quando o jogo abre: se sobrou algo na fila (o jogo fechou
	// sem internet), já começa a tentar mandar, sem esperar alguém terminar uma fase.
	[RuntimeInitializeOnLoadMethod]
	static void Iniciar(){
		Pegar ();
	}

	static Planilha Pegar(){
		if (instancia == null) {
			GameObject objeto = new GameObject ("Planilha");
			// Não é apagado ao trocar de cena: o envio continua mesmo voltando ao mapa.
			DontDestroyOnLoad (objeto);
			instancia = objeto.AddComponent<Planilha> ();
		}
		return instancia;
	}

	void Awake () {
		arquivoPendentes = Path.Combine (Application.persistentDataPath, "pendentes.txt");
		arquivoBackup = Path.Combine (Application.persistentDataPath, "tempos_backup.csv");
		url = LerUrl ();
		enviando = false;

		if (url == "") {
			Debug.Log ("[Planilha] Sem URL em StreamingAssets/planilha.txt: os tempos ficam só em " + arquivoBackup);
		}

		// De minuto em minuto tenta mandar o que ficou na fila.
		InvokeRepeating ("TentarEnviar", 30f, 60f);
	}

	// A primeira linha do planilha.txt que não está vazia e não começa com #
	string LerUrl(){
		string caminho = Path.Combine (Application.streamingAssetsPath, "planilha.txt");
		if (File.Exists (caminho) == false) {
			return "";
		}

		string[] linhas = File.ReadAllLines (caminho);
		for (int i = 0; i < linhas.Length; i++) {
			string linha = linhas [i].Trim ();
			if (linha != "" && linha.StartsWith ("#") == false) {
				return linha;
			}
		}
		return "";
	}

	void Registrar(string fase, float segundos){
		string quando = System.DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
		string nome = Limpar (PlayerPrefs.GetString ("JogadorNome", ""));
		string contato = Limpar (PlayerPrefs.GetString ("JogadorContato", ""));
		string tempo = segundos.ToString ("0.0", CultureInfo.InvariantCulture);

		// Backup no computador (abre no Excel: separado por ponto e vírgula)
		if (File.Exists (arquivoBackup) == false) {
			File.AppendAllText (arquivoBackup, "data;nome;contato;fase;segundos\n");
		}
		File.AppendAllText (arquivoBackup, quando + ";" + nome + ";" + contato + ";" + fase + ";" + tempo + "\n");

		// Fila de envio: uma linha por tempo, separada por TAB
		File.AppendAllText (arquivoPendentes, quando + "\t" + nome + "\t" + contato + "\t" + fase + "\t" + tempo + "\n");
		Debug.Log ("[Planilha] Tempo guardado: " + nome + " / " + fase + " / " + tempo + " s");

		TentarEnviar ();
	}

	void TentarEnviar(){
		if (enviando == true || url == "") {
			return;
		}
		StartCoroutine (EnviarPendentes ());
	}

	// Manda a fila na ordem. Se um envio falhar, para e tenta de novo mais tarde.
	IEnumerator EnviarPendentes(){
		enviando = true;

		string[] linhas = new string[0];
		if (File.Exists (arquivoPendentes) == true) {
			linhas = File.ReadAllLines (arquivoPendentes);
		}

		int enviadas = 0;
		for (int i = 0; i < linhas.Length; i++) {
			string[] partes = linhas [i].Split ('\t');
			if (partes.Length < 5) {
				// linha vazia ou quebrada: só pula
				enviadas = enviadas + 1;
				continue;
			}

			WWWForm formulario = new WWWForm ();
			formulario.AddField ("senha", SENHA);
			formulario.AddField ("data", partes [0]);
			formulario.AddField ("nome", partes [1]);
			formulario.AddField ("contato", partes [2]);
			formulario.AddField ("fase", partes [3]);
			formulario.AddField ("segundos", partes [4]);

			UnityWebRequest pedido = UnityWebRequest.Post (url, formulario);
			pedido.timeout = 20;
			yield return pedido.SendWebRequest ();

			bool deuCerto = pedido.isNetworkError == false && pedido.isHttpError == false
				&& pedido.downloadHandler.text.Trim () == "ok";

			if (deuCerto == false) {
				Debug.Log ("[Planilha] Não enviou agora (" + pedido.error + " / " + pedido.downloadHandler.text + "). Fica na fila.");
				break;
			}

			Debug.Log ("[Planilha] Enviado: " + partes [1] + " / " + partes [3] + " / " + partes [4] + " s");
			enviadas = enviadas + 1;
		}

		// Tira da fila só as linhas enviadas. Lê o arquivo de novo porque
		// outro tempo pode ter entrado no fim da fila enquanto enviava.
		if (enviadas > 0) {
			string[] atual = File.ReadAllLines (arquivoPendentes);
			System.Text.StringBuilder resto = new System.Text.StringBuilder ();
			for (int i = enviadas; i < atual.Length; i++) {
				resto.Append (atual [i]).Append ("\n");
			}
			File.WriteAllText (arquivoPendentes, resto.ToString ());
		}

		enviando = false;
	}

	// Tira TAB e quebra de linha, que atrapalhariam a fila e o CSV.
	static string Limpar(string texto){
		return texto.Replace ("\t", " ").Replace ("\n", " ").Replace ("\r", " ").Replace (";", ",");
	}
}
