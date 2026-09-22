using UnityEngine;

/// <summary>
/// Recado curto entre o mapa e a fase: qual nó abriu a fase e em que dificuldade.
///
/// É estático de propósito. Não vale a pena um objeto persistente só para isso —
/// static sobrevive à troca de cena, que é a única coisa que precisa aqui.
/// </summary>
public static class SessaoDeFase
{
    public static string IdFase { get; private set; }
    public static Dificuldade Dificuldade { get; private set; }

    /// <summary>Cena para onde a fase deve voltar quando terminar.</summary>
    public static string CenaDoMapa = "menudefase";

    public static void Definir(string idFase, Dificuldade dif)
    {
        IdFase = idFase;
        Dificuldade = dif;
    }

    public static void Limpar()
    {
        IdFase = null;
        Dificuldade = Dificuldade.Normal;
    }

    /// <summary>Marca a fase atual como vencida e volta para o mapa.</summary>
    public static void ConcluirEVoltar()
    {
        if (!string.IsNullOrEmpty(IdFase))
        {
            ProgressManager.Instancia.MarcarConcluida(IdFase, Dificuldade);
            Debug.Log("[MenuFase] '" + IdFase + "' concluída em " + Dificuldade + ".");
        }
        else
        {
            Debug.LogWarning("[MenuFase] nenhuma fase em curso; " +
                             "você abriu esta cena direto em vez de entrar pelo mapa.");
        }
        TransicaoIris.Carregar(CenaDoMapa);
    }

    /// <summary>Sai sem marcar nada.</summary>
    public static void DesistirEVoltar()
    {
        TransicaoIris.Carregar(CenaDoMapa);
    }
}
