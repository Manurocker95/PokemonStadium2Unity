Shader "N3DS/N64_StadiumLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }

        Pass
        {
            Lighting Off
            Cull Back
            ZWrite On
            ZTest LEqual
            Fog { Mode Off }

            AlphaTest Greater [_Cutoff]

            BindChannels
            {
                Bind "Vertex", vertex
                Bind "TexCoord", texcoord
                Bind "Color", color
            }

            SetTexture [_MainTex]
            {
                combine texture * primary, texture * primary
            }

            SetTexture [_MainTex]
            {
                constantColor [_Color]
                combine previous * constant, previous * constant
            }
        }
    }

    Fallback Off
}