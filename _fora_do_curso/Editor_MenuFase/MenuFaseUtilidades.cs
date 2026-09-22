using UnityEditor;
using UnityEngine;

/// <summary>
/// Atalhos do menu MenuFase para o dia a dia: montar tudo de uma vez, zerar o
/// save e achar o arquivo de progresso.
/// </summary>
public static class MenuFaseUtilidades
{
    [MenuItem("MenuFase/0 - Montar tudo", false, 0)]
    public static void MontarTudo()
    {
        // A ordem importa: a cena precisa dos tiles e do controller já prontos,
        // senão o Jogador nasce com o bonequinho de reserva e sem Animator.
        MenuFaseImportador.ReimportarTudo();
        MenuFaseGerarTiles.Gerar();
        MenuFasePersonagem.Preparar();
        MenuFaseMontarCena.Montar();
    }

    [MenuItem("MenuFase/Progresso/Zerar progresso", false, 100)]
    public static void Zerar()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Zerar progresso",
            "Isto apaga as fases concluídas e a posição salva.\n\n" + ProgressManager.Caminho,
            "Zerar", "Cancelar");

        if (!ok) return;

        ProgressManager.Instancia.Zerar();
    }

    [MenuItem("MenuFase/Progresso/Abrir pasta do save", false, 101)]
    public static void AbrirPasta()
    {
        EditorUtility.RevealInFinder(ProgressManager.Caminho);
    }

    [MenuItem("MenuFase/Progresso/Concluir todas as fases", false, 102)]
    public static void ConcluirTudo()
    {
        for (int i = 0; i < MenuFaseMapa.FASES.Length; i++)
            ProgressManager.Instancia.MarcarConcluida(MenuFaseMapa.FASES[i].id, Dificuldade.Normal);

        Debug.Log("[MenuFase] todas as fases marcadas como concluídas. " +
                  "Reabra a cena do mapa para ver as bandeiras e a ponte.");
    }
}
