// Fechamento em círculo (íris) para a transição de entrada nas fases.
//
// Vai num Image de UI esticado na tela inteira. O material pinta de preto tudo que
// estiver FORA de um círculo de raio _Raio (em UV, com o X corrigido pela proporção
// da tela). _Raio = 1.5 deixa a tela toda limpa; _Raio = 0 deixa a tela toda preta.
Shader "MenuFase/IrisUI"
{
    Properties
    {
        _Color ("Cor", Color) = (0, 0, 0, 1)
        _Raio ("Raio", Range(0, 1.5)) = 1.5
        _Suavidade ("Suavidade da borda", Range(0.0001, 0.2)) = 0.004
        _Aspecto ("Proporcao da tela (larg/alt)", Float) = 1.7777
        _CentroX ("Centro X", Range(0, 1)) = 0.5
        _CentroY ("Centro Y", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _Color;
            float _Raio;
            float _Suavidade;
            float _Aspecto;
            float _CentroX;
            float _CentroY;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 d = i.uv - float2(_CentroX, _CentroY);

                // Sem isso o circulo vira elipse em tela widescreen.
                d.x *= _Aspecto;

                float r = length(d);

                // 0 dentro do circulo (transparente), 1 fora (preto).
                float a = smoothstep(_Raio - _Suavidade, _Raio + _Suavidade, r);

                fixed4 c = _Color * i.color;
                c.a *= a;
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
