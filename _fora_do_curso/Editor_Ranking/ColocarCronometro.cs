using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ferramenta (fora do curso): coloca o CronometroFase e o texto TXT_TEMPO nas cenas fase01..03
// e troca o texto da fase. Para usar: copie para Assets/Editor e rode
// Ferramentas > Colocar cronometro nas fases (ou -executeMethod ColocarCronometro.Colocar).
public static class ColocarCronometro
{
    static readonly string[] CENAS = { "Assets/Scenes/fase01.unity", "Assets/Scenes/fase02.unity", "Assets/Scenes/fase03.unity" };
    static readonly string[] NOMES = { "FASE 1\nCasa da Vila", "FASE 2\nFerreiro", "FASE 3\nIgreja" };

    [MenuItem("Ferramentas/Colocar cronometro nas fases")]
    public static void Colocar()
    {
        for (int i = 0; i < CENAS.Length; i++)
        {
            Scene cena = EditorSceneManager.OpenScene(CENAS[i], OpenSceneMode.Single);
            GameObject fase = GameObject.Find("Fase");
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            Text txtFase = GameObject.Find("TXT_FASE").GetComponent<Text>();

            txtFase.text = NOMES[i] + "\n\n(em construção)\n\naperte  X  para concluir (teste)\naperte  Círculo  para voltar ao mapa";

            GameObject velho = GameObject.Find("TXT_TEMPO");
            if (velho != null) Object.DestroyImmediate(velho);

            GameObject go = new GameObject("TXT_TEMPO", typeof(RectTransform));
            go.layer = 5;
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.anchorMin = new Vector2(0.5f, 1f);
            r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0, -30);
            r.sizeDelta = new Vector2(1000, 120);
            go.AddComponent<CanvasRenderer>();
            Text t = go.AddComponent<Text>();
            t.font = txtFase.font;
            t.fontSize = 40;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.9f, 0.54f);
            t.alignment = TextAnchor.UpperCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = "JOGADOR\nTEMPO  00:00.0";
            Outline o = go.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.06f, 0.02f);
            o.effectDistance = new Vector2(3, -3);

            CronometroFase cron = fase.GetComponent<CronometroFase>();
            if (cron == null) cron = fase.AddComponent<CronometroFase>();
            cron.UITextTempo = t;

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log("[Cronometro] " + CENAS[i] + " pronto");
        }
    }
}
