Shader "maleric/TextureArray/Standard"
{
    Properties
    {
        _MainTexArr ("Texture Array", 2DArray) = "" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,2)) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma require 2darray
        #pragma target 3.5

        UNITY_DECLARE_TEX2DARRAY(_MainTexArr);
        fixed4 _Color;
        half _Brightness;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTexArr;
            float sliceIndex;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_MainTexArr = v.texcoord.xy;
            o.sliceIndex = v.texcoord.z;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTexArr, float3(IN.uv_MainTexArr, IN.sliceIndex)) * _Color;
            c.rgb *= _Brightness;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
