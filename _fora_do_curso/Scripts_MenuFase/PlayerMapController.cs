using UnityEngine;

/// <summary>
/// Anda pelo mapa-múndi em 8 direções.
///
/// Usa o Input Manager antigo (Edit > Project Settings > Input) porque o pacote
/// Input System exige Unity 2019.1+ e este projeto é 2017.4 — e também porque é
/// o mesmo esquema que o MenuPrincipal.cs já usa.
///
/// Eixos lidos: Horizontal / Vertical (WASD, setas e analógico esquerdo) e
/// DPadHorizontal / DPadVertical (direcional do controle).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMapController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Em unidades por segundo. 1 unidade = 1 tile = 16 pixels.")]
    public float velocidade = 5f;

    [Tooltip("Zona morta do analógico. Abaixo disso o eixo conta como parado.")]
    [Range(0f, 0.9f)]
    public float zonaMorta = 0.25f;

    [Header("Animação")]
    [Tooltip("Controller gerado por 'MenuFase > 4'. Pode ficar vazio: sem ele o " +
             "movimento continua funcionando, só não anima.")]
    public Animator animator;
    public SpriteRenderer sprite;

    private Rigidbody2D corpo;
    private Vector2 direcao;
    private Vector2 ultimaDirecao = Vector2.down;

    /// <summary>
    /// Enquanto o cartão de fase está aberto, o jogador não anda.
    /// É estático porque a UI precisa travar o movimento sem ter referência ao player.
    /// </summary>
    public static bool Congelado;

    public Vector2 UltimaDirecao { get { return ultimaDirecao; } }

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();

        // Top-down: sem gravidade e sem tombar ao esbarrar em quina de colisor.
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        MapearParametros();

        Congelado = false;
    }

    private void Update()
    {
        if (Congelado)
        {
            direcao = Vector2.zero;
            AtualizarAnimacao(Vector2.zero);
            return;
        }

        float x = LerEixo("Horizontal", "DPadHorizontal");
        float y = LerEixo("Vertical", "DPadVertical");

        Vector2 bruto = new Vector2(x, y);

        // Sem normalizar, andar na diagonal seria ~41% mais rápido.
        direcao = bruto.sqrMagnitude > 1f ? bruto.normalized : bruto;

        if (direcao.sqrMagnitude > 0.0001f)
            ultimaDirecao = direcao.normalized;

        AtualizarAnimacao(direcao);
    }

    private void FixedUpdate()
    {
        // MovePosition respeita os colisores; mexer no transform atravessaria parede.
        corpo.MovePosition(corpo.position + direcao * velocidade * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Soma o eixo principal com o do direcional, quando esse eixo existir no projeto.
    /// Pedir um eixo que não está configurado joga exceção, daí o teste uma vez só.
    /// </summary>
    private float LerEixo(string principal, string dpad)
    {
        float v = Input.GetAxisRaw(principal);
        if (TemEixo(dpad))
        {
            float d = Input.GetAxisRaw(dpad);
            if (Mathf.Abs(d) > Mathf.Abs(v)) v = d;
        }
        return Mathf.Abs(v) < zonaMorta ? 0f : Mathf.Sign(v);
    }

    private static readonly System.Collections.Generic.Dictionary<string, bool> eixosConhecidos =
        new System.Collections.Generic.Dictionary<string, bool>();

    private static bool TemEixo(string nome)
    {
        bool existe;
        if (eixosConhecidos.TryGetValue(nome, out existe)) return existe;

        try
        {
            Input.GetAxisRaw(nome);
            existe = true;
        }
        catch (System.Exception)
        {
            existe = false;
            Debug.Log("[MenuFase] eixo '" + nome + "' não está no Project Settings > Input. " +
                      "Sigo só com Horizontal/Vertical.");
        }
        eixosConhecidos[nome] = existe;
        return existe;
    }

    /// <summary>
    /// A folha do personagem só tem 5 direções: baixo, baixo-diagonal, lado,
    /// cima-diagonal e cima. As quatro que faltam (tudo que aponta para a esquerda)
    /// saem espelhando as mesmas no eixo X.
    /// </summary>
    private static int DirecaoDaFolha(Vector2 d)
    {
        bool horizontal = Mathf.Abs(d.x) > 0.01f;
        bool vertical = Mathf.Abs(d.y) > 0.01f;

        if (!horizontal) return d.y > 0f ? 4 : 0;   // Cima / Baixo
        if (!vertical) return 2;                    // Lado
        return d.y > 0f ? 3 : 1;                    // CimaDiag / BaixoDiag
    }

    private void AtualizarAnimacao(Vector2 dir)
    {
        // Parado mantém a última direção, senão o personagem "viraria" ao soltar
        // o controle.
        Vector2 referencia = dir.sqrMagnitude > 0.0001f ? dir : ultimaDirecao;

        if (sprite != null && Mathf.Abs(referencia.x) > 0.01f)
            sprite.flipX = referencia.x < 0f;

        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (temDir) animator.SetInteger("Dir", DirecaoDaFolha(referencia));
        if (temAndando) animator.SetBool("Andando", dir.sqrMagnitude > 0.0001f);
    }

    private bool temDir;
    private bool temAndando;

    /// <summary>
    /// Pedir um parâmetro que não existe no controller enche o Console de aviso,
    /// então a checagem é feita uma vez só.
    /// </summary>
    private void MapearParametros()
    {
        temDir = false;
        temAndando = false;

        if (animator == null || animator.runtimeAnimatorController == null) return;

        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == "Dir" && ps[i].type == AnimatorControllerParameterType.Int)
                temDir = true;
            else if (ps[i].name == "Andando" && ps[i].type == AnimatorControllerParameterType.Bool)
                temAndando = true;
        }
    }

    /// <summary>Teleporta sem deixar o Rigidbody interpolar a viagem inteira.</summary>
    public void Reposicionar(Vector2 posicao)
    {
        transform.position = new Vector3(posicao.x, posicao.y, transform.position.z);
        if (corpo != null)
        {
            corpo.position = posicao;
            corpo.velocity = Vector2.zero;
        }
    }
}
