using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum Dificuldade
{
    Simples = 0,
    Normal = 1
}

/// <summary>
/// O que vai para o disco. JsonUtility não serializa Dictionary, então as fases
/// concluídas e as dificuldades viajam em duas listas paralelas.
/// </summary>
[Serializable]
public class DadosProgresso
{
    public List<string> fasesConcluidas = new List<string>();
    public List<int> dificuldades = new List<int>();

    public string ultimoNo = "";
    public float jogadorX;
    public float jogadorY;
    public bool temPosicaoSalva;
}

/// <summary>
/// Guarda o progresso do mapa e sobrevive à troca de cenas.
/// Grava em Application.persistentDataPath/progresso_menufase.json.
///
/// Não precisa ser colocado na cena à mão: qualquer script que peça
/// ProgressManager.Instancia cria o objeto na hora.
/// </summary>
public class ProgressManager : MonoBehaviour
{
    public const string ARQUIVO = "progresso_menufase.json";

    private static ProgressManager instancia;

    public static ProgressManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                GameObject go = new GameObject("ProgressManager");
                instancia = go.AddComponent<ProgressManager>();
                DontDestroyOnLoad(go);
                instancia.Carregar();
            }
            return instancia;
        }
    }

    private DadosProgresso dados = new DadosProgresso();

    public static string Caminho
    {
        get { return Path.Combine(Application.persistentDataPath, ARQUIVO); }
    }

    private void Awake()
    {
        // Se alguém arrastou o componente para a cena e já existe um vindo de antes,
        // o novo se apaga para não duplicar o progresso.
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        Carregar();
    }

    // ---------------------------------------------------------------- consulta

    public bool Concluida(string idFase)
    {
        if (string.IsNullOrEmpty(idFase)) return false;
        return dados.fasesConcluidas.Contains(idFase);
    }

    /// <summary>
    /// Uma fase está liberada quando TODAS as fases exigidas já foram concluídas.
    /// Lista vazia significa que ela já nasce aberta.
    /// </summary>
    public bool Desbloqueada(IList<string> idsExigidos)
    {
        if (idsExigidos == null) return true;
        for (int i = 0; i < idsExigidos.Count; i++)
        {
            string exigido = idsExigidos[i];
            if (string.IsNullOrEmpty(exigido)) continue;
            if (!Concluida(exigido)) return false;
        }
        return true;
    }

    public Dificuldade DificuldadeDe(string idFase)
    {
        int i = dados.fasesConcluidas.IndexOf(idFase);
        if (i < 0 || i >= dados.dificuldades.Count) return Dificuldade.Normal;
        return (Dificuldade)dados.dificuldades[i];
    }

    public int QuantidadeConcluida { get { return dados.fasesConcluidas.Count; } }

    // ---------------------------------------------------------------- escrita

    public void MarcarConcluida(string idFase, Dificuldade dif)
    {
        if (string.IsNullOrEmpty(idFase)) return;

        int i = dados.fasesConcluidas.IndexOf(idFase);
        if (i >= 0)
        {
            // Já tinha concluído antes: só sobe a dificuldade, nunca desce.
            if (i < dados.dificuldades.Count && (int)dif > dados.dificuldades[i])
                dados.dificuldades[i] = (int)dif;
        }
        else
        {
            dados.fasesConcluidas.Add(idFase);
            dados.dificuldades.Add((int)dif);
        }
        Salvar();
    }

    /// <summary>Lembra de onde o jogador saiu, para ele voltar na frente do nó.</summary>
    public void SalvarSaida(string idNo, Vector2 posicao)
    {
        dados.ultimoNo = idNo == null ? "" : idNo;
        dados.jogadorX = posicao.x;
        dados.jogadorY = posicao.y;
        dados.temPosicaoSalva = true;
        Salvar();
    }

    public void SalvarPosicao(Vector2 posicao)
    {
        dados.jogadorX = posicao.x;
        dados.jogadorY = posicao.y;
        dados.temPosicaoSalva = true;
    }

    public string UltimoNo { get { return dados.ultimoNo; } }

    /// <summary>
    /// Esquece o nó de retorno depois que ele já foi usado. Sem isto, o jogador
    /// andaria pelo mapa, sairia do jogo, e voltaria plantado na última fase que
    /// jogou em vez de onde parou.
    /// </summary>
    public void ConsumirUltimoNo()
    {
        dados.ultimoNo = "";
    }
    public bool TemPosicaoSalva { get { return dados.temPosicaoSalva; } }
    public Vector2 PosicaoSalva { get { return new Vector2(dados.jogadorX, dados.jogadorY); } }

    // ---------------------------------------------------------------- disco

    public void Salvar()
    {
        try
        {
            File.WriteAllText(Caminho, JsonUtility.ToJson(dados, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MenuFase] não consegui gravar o progresso: " + e.Message);
        }
    }

    public void Carregar()
    {
        try
        {
            if (!File.Exists(Caminho))
            {
                dados = new DadosProgresso();
                return;
            }
            string json = File.ReadAllText(Caminho);
            DadosProgresso lido = JsonUtility.FromJson<DadosProgresso>(json);
            dados = lido != null ? lido : new DadosProgresso();

            // Arquivo editado à mão ou salvo por uma versão antiga pode vir torto.
            if (dados.fasesConcluidas == null) dados.fasesConcluidas = new List<string>();
            if (dados.dificuldades == null) dados.dificuldades = new List<int>();
            while (dados.dificuldades.Count < dados.fasesConcluidas.Count)
                dados.dificuldades.Add((int)Dificuldade.Normal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MenuFase] progresso ilegível, começando do zero: " + e.Message);
            dados = new DadosProgresso();
        }
    }

    /// <summary>Apaga tudo. Útil para testar o fluxo de novo.</summary>
    public void Zerar()
    {
        dados = new DadosProgresso();
        try
        {
            if (File.Exists(Caminho)) File.Delete(Caminho);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MenuFase] não consegui apagar o save: " + e.Message);
        }
        Debug.Log("[MenuFase] progresso zerado.");
    }

    private void OnApplicationQuit()
    {
        Salvar();
    }
}
