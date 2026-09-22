using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ferramenta (fora do curso) que monta a cena Assets/Identificacao/identificacao.unity.
// Para usar: copie para Assets/Editor e rode o menu Ferramentas > Montar tela de identificacao
// (ou em batchmode: -executeMethod ConstruirIdentificacao.Montar).
public static class ConstruirIdentificacao
{
    const string PASTA = "Assets/Identificacao/";
    const string CENA = PASTA + "identificacao.unity";

    static Font fonte;

    [MenuItem("Ferramentas/Montar tela de identificacao")]
    public static void Montar()
    {
        ImportarSprite(PASTA + "fundo_identificacao.jpg", Vector4.zero);
        ImportarSprite(PASTA + "campo.png", new Vector4(26, 26, 26, 26));
        ImportarSprite(PASTA + "campo_selecionado.png", new Vector4(26, 26, 26, 26));
        ImportarSprite(PASTA + "placa.png", Vector4.zero);
        ImportarSprite(PASTA + "placa_acesa.png", Vector4.zero);

        fonte = AssetDatabase.LoadAssetAtPath<Font>(PASTA + "Fontes/LilitaOne-Regular.ttf");
        Sprite fundo = Carregar("fundo_identificacao.jpg");
        Sprite campo = Carregar("campo.png");
        Sprite campoAceso = Carregar("campo_selecionado.png");
        Sprite placa = Carregar("placa.png");
        Sprite placaAcesa = Carregar("placa_acesa.png");

        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        Camera camera = cam.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.1f, 0.06f);
        camera.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);

        GameObject eventos = new GameObject("EventSystem");
        eventos.AddComponent<EventSystem>();
        eventos.AddComponent<StandaloneInputModule>();

        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.layer = 5;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escala = canvasGO.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1366, 768);
        escala.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Transform raiz = canvasGO.transform;

        // Fundo: estica na tela toda
        Image imgFundo = NovaImagem("Fundo", raiz, fundo, Vector2.zero, Vector2.zero);
        RectTransform rf = imgFundo.rectTransform;
        rf.anchorMin = Vector2.zero; rf.anchorMax = Vector2.one;
        rf.offsetMin = Vector2.zero; rf.offsetMax = Vector2.zero;
        imgFundo.raycastTarget = false;

        Color marrom = new Color32(74, 44, 23, 255);
        NovoTexto("TXT_TITULO", raiz, "QUEM ESTÁ JOGANDO?", 34, marrom, TextAnchor.MiddleCenter, new Vector2(0, 128), new Vector2(760, 50));
        Image linha = NovaImagem("Linha", raiz, null, new Vector2(0, 103), new Vector2(406, 2));
        linha.color = new Color32(120, 80, 45, 255);

        NovoTexto("TXT_NOME", raiz, "NOME", 21, marrom, TextAnchor.MiddleLeft, new Vector2(0, 72), new Vector2(480, 28));
        InputField nome = NovoCampo("CAMPO_NOME", raiz, campo, new Vector2(0, 28), "Digite o seu nome", 40);

        NovoTexto("TXT_CONTATO", raiz, "CELULAR OU E-MAIL", 21, marrom, TextAnchor.MiddleLeft, new Vector2(0, -32), new Vector2(480, 28));
        InputField contato = NovoCampo("CAMPO_CONTATO", raiz, campo, new Vector2(0, -76), "ex.: (11) 91234-5678  ou  voce@email.com", 60);

        Text aviso = NovoTexto("TXT_AVISO", raiz, "", 20, new Color32(110, 72, 40, 255), TextAnchor.MiddleCenter, new Vector2(0, -131), new Vector2(620, 40));

        Image imgPlaca = NovaImagem("BOTAO_CONFIRMAR", raiz, placa, new Vector2(0, -206), new Vector2(420, 79));
        imgPlaca.preserveAspect = true;
        Button botao = imgPlaca.gameObject.AddComponent<Button>();
        botao.transition = Selectable.Transition.None;
        Navigation semNavegar = new Navigation();
        semNavegar.mode = Navigation.Mode.None;
        botao.navigation = semNavegar;
        Text txtConfirmar = NovoTexto("TXT_CONFIRMAR", imgPlaca.transform, "CONFIRMAR", 30, new Color32(201, 160, 122, 255), TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(300, 60));
        Outline contorno = txtConfirmar.gameObject.AddComponent<Outline>();
        contorno.effectColor = new Color32(40, 22, 10, 255);
        contorno.effectDistance = new Vector2(2, -2);
        txtConfirmar.raycastTarget = false;

        GameObject controlador = new GameObject("TelaIdentificacao");
        TelaIdentificacao tela = controlador.AddComponent<TelaIdentificacao>();
        tela.campoNome = nome;
        tela.campoContato = contato;
        tela.fundoNome = nome.GetComponent<Image>();
        tela.fundoContato = contato.GetComponent<Image>();
        tela.campoApagado = campo;
        tela.campoAceso = campoAceso;
        tela.placaConfirmar = imgPlaca;
        tela.textoConfirmar = txtConfirmar;
        tela.placaApagada = placa;
        tela.placaAcesa = placaAcesa;
        tela.UITextAviso = aviso;
        UnityEventTools.AddPersistentListener(botao.onClick, tela.CliqueConfirmar);

        EditorSceneManager.SaveScene(cena, CENA);
        AdicionarNoBuild();
        Debug.Log("[Identificacao] cena salva em " + CENA);
    }

    static void ImportarSprite(string caminho, Vector4 borda)
    {
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = borda;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 2048;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }

    static Sprite Carregar(string nome)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PASTA + nome);
    }

    static RectTransform NovoRect(string nome, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.layer = 5;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(pai, false);
        r.anchoredPosition = posicao;
        r.sizeDelta = tamanho;
        return r;
    }

    static Image NovaImagem(string nome, Transform pai, Sprite sprite, Vector2 posicao, Vector2 tamanho)
    {
        RectTransform r = NovoRect(nome, pai, posicao, tamanho);
        r.gameObject.AddComponent<CanvasRenderer>();
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        return img;
    }

    static Text NovoTexto(string nome, Transform pai, string texto, int tamanhoFonte, Color cor, TextAnchor alinhamento, Vector2 posicao, Vector2 tamanho)
    {
        RectTransform r = NovoRect(nome, pai, posicao, tamanho);
        r.gameObject.AddComponent<CanvasRenderer>();
        Text t = r.gameObject.AddComponent<Text>();
        t.font = fonte;
        t.fontSize = tamanhoFonte;
        t.color = cor;
        t.alignment = alinhamento;
        t.text = texto;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    // Campo de texto: Image (fundo fatiado) + InputField + Texto + Placeholder.
    // O sprite tem 14px de sobra em volta para o brilho, por isso o tamanho é 480+28 x 56+28.
    static InputField NovoCampo(string nome, Transform pai, Sprite fundo, Vector2 posicao, string dica, int limite)
    {
        Image img = NovaImagem(nome, pai, fundo, posicao, new Vector2(508, 84));
        img.type = Image.Type.Sliced;

        Text dicaTxt = NovoTexto("Placeholder", img.transform, dica, 18, new Color32(140, 110, 75, 255), TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        Esticar(dicaTxt.rectTransform);
        dicaTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

        Text texto = NovoTexto("Texto", img.transform, "", 24, new Color32(45, 26, 12, 255), TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        Esticar(texto.rectTransform);
        texto.supportRichText = false;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;

        InputField campo = img.gameObject.AddComponent<InputField>();
        campo.targetGraphic = img;
        campo.textComponent = texto;
        campo.placeholder = dicaTxt;
        campo.characterLimit = limite;
        campo.lineType = InputField.LineType.SingleLine;
        campo.transition = Selectable.Transition.None;
        campo.caretWidth = 3;
        campo.customCaretColor = true;
        campo.caretColor = new Color32(45, 26, 12, 255);
        campo.selectionColor = new Color32(246, 166, 35, 110);
        Navigation semNavegar = new Navigation();
        semNavegar.mode = Navigation.Mode.None;
        campo.navigation = semNavegar;
        return campo;
    }

    static void Esticar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(30, 14);
        r.offsetMax = new Vector2(-30, -14);
    }

    static void AdicionarNoBuild()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in lista)
        {
            if (s.path == CENA) return;
        }
        lista.Add(new EditorBuildSettingsScene(CENA, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
