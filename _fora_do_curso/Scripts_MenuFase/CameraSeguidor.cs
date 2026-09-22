using UnityEngine;

/// <summary>
/// Segue o jogador com amortecimento e prende a visão dentro das bordas do mapa.
///
/// Faz o papel do Cinemachine + CinemachineConfiner2D, que não dá para usar aqui:
/// o Cinemachine só entra no 2017.4 por importação manual da Asset Store e a versão
/// dessa época não tem o Confiner2D.
///
/// Roda em LateUpdate para andar depois do jogador — em Update a câmera ficaria
/// sempre um frame atrás e o movimento tremeria.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraSeguidor : MonoBehaviour
{
    [Header("Alvo")]
    public Transform alvo;

    [Tooltip("Quanto maior, mais preguiçosa a câmera. 0 = cola no alvo.")]
    public float amortecimento = 0.15f;

    [Header("Limites do mapa (em unidades)")]
    public bool usarLimites = true;
    public Vector2 limiteMin = new Vector2(0f, 0f);
    public Vector2 limiteMax = new Vector2(60f, 40f);

    [Header("Pixel art")]
    [Tooltip("Arredonda a posição final para a grade de pixels, para a imagem não tremer.")]
    public bool travarNoPixel = true;

    private Camera cam;
    private PixelPerfectCam pixelPerfect;
    private Vector3 velocidadeSuavizacao;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCam>();
    }

    private void Start()
    {
        if (alvo == null)
        {
            PlayerMapController p = FindObjectOfType<PlayerMapController>();
            if (p != null) alvo = p.transform;
        }
        if (alvo != null) transform.position = Enquadrar(alvo.position);
    }

    private void LateUpdate()
    {
        if (alvo == null) return;

        Vector3 desejada = Enquadrar(alvo.position);

        Vector3 pos = amortecimento <= 0f
            ? desejada
            : Vector3.SmoothDamp(transform.position, desejada, ref velocidadeSuavizacao, amortecimento);

        pos.z = transform.position.z;

        if (travarNoPixel && pixelPerfect != null)
        {
            // Arredondar depois do amortecimento: arredondar antes brigaria com o
            // SmoothDamp e a câmera andaria aos solavancos.
            float p = pixelPerfect.TamanhoDoPixel;
            pos.x = Mathf.Round(pos.x / p) * p;
            pos.y = Mathf.Round(pos.y / p) * p;
        }

        transform.position = pos;
    }

    /// <summary>Centro desejado, já preso dentro das bordas do mapa.</summary>
    private Vector3 Enquadrar(Vector3 alvoPos)
    {
        Vector3 pos = new Vector3(alvoPos.x, alvoPos.y, transform.position.z);
        if (!usarLimites || cam == null) return pos;

        float meiaAltura = cam.orthographicSize;
        float meiaLargura = meiaAltura * cam.aspect;

        // Se o mapa for menor que a tela, centraliza em vez de deixar a borda entrar.
        if (limiteMax.x - limiteMin.x <= meiaLargura * 2f)
            pos.x = (limiteMin.x + limiteMax.x) * 0.5f;
        else
            pos.x = Mathf.Clamp(pos.x, limiteMin.x + meiaLargura, limiteMax.x - meiaLargura);

        if (limiteMax.y - limiteMin.y <= meiaAltura * 2f)
            pos.y = (limiteMin.y + limiteMax.y) * 0.5f;
        else
            pos.y = Mathf.Clamp(pos.y, limiteMin.y + meiaAltura, limiteMax.y - meiaAltura);

        return pos;
    }

    /// <summary>Desenha o retângulo do confiner na Scene view, para ajustar no olho.</summary>
    private void OnDrawGizmosSelected()
    {
        if (!usarLimites) return;
        Gizmos.color = Color.yellow;
        Vector3 c = new Vector3((limiteMin.x + limiteMax.x) * 0.5f, (limiteMin.y + limiteMax.y) * 0.5f, 0f);
        Vector3 t = new Vector3(limiteMax.x - limiteMin.x, limiteMax.y - limiteMin.y, 0.1f);
        Gizmos.DrawWireCube(c, t);
    }
}
