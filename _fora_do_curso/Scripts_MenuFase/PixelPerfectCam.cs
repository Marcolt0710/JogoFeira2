using UnityEngine;

/// <summary>
/// Substitui o pacote 2D Pixel Perfect, que só existe a partir do Unity 2018.1.
///
/// Faz as duas coisas que importam:
///  1. trava o orthographicSize num valor onde 1 pixel da arte cai em um número
///     inteiro de pixels da tela — sem isso o pixel art "treme" e fica com linhas
///     de espessura desigual;
///  2. deixa o resultado num múltiplo exato, arredondando o zoom para inteiro.
///
/// O arredondamento do movimento da câmera fica no CameraSeguidor, que roda depois.
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class PixelPerfectCam : MonoBehaviour
{
    [Tooltip("Pixels por unidade da arte. Os tiles da MenuDeFase são 16.")]
    public int pixelsPorUnidade = 16;

    [Tooltip("Quantos pixels de tela para cada pixel da arte. 0 = calcula sozinho " +
             "pelo maior zoom inteiro que couber na altura desejada.")]
    public int zoom = 0;

    [Tooltip("Altura de referência em pixels da arte. Usada só quando zoom = 0.")]
    public int alturaDeReferencia = 180;

    private Camera cam;
    private int ultimaAltura = -1;
    private int ultimoZoom = -1;

    /// <summary>Zoom efetivamente aplicado no último cálculo.</summary>
    public int ZoomAtual { get; private set; }

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        Aplicar();
    }

    private void Update()
    {
        // Recalcular todo frame é desperdício; só quando a janela muda de tamanho.
        if (Screen.height != ultimaAltura || zoom != ultimoZoom) Aplicar();
    }

    public void Aplicar()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || pixelsPorUnidade <= 0) return;

        cam.orthographic = true;

        int z = zoom;
        if (z <= 0)
        {
            // Maior zoom inteiro que ainda mostra pelo menos a altura de referência.
            z = Mathf.Max(1, Mathf.FloorToInt((float)Screen.height / Mathf.Max(1, alturaDeReferencia)));
        }

        ZoomAtual = z;
        ultimaAltura = Screen.height;
        ultimoZoom = zoom;

        // Metade da altura da tela, convertida de pixels de tela para unidades.
        cam.orthographicSize = Screen.height / (2f * pixelsPorUnidade * z);
    }

    /// <summary>Tamanho de um pixel da arte, em unidades de mundo.</summary>
    public float TamanhoDoPixel
    {
        get { return 1f / Mathf.Max(1, pixelsPorUnidade); }
    }
}
