Shader "Unlit/BlackoutShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // this is the src of Blit
        _PreviousTex ("_PreviousTex", 2D) = "white" {} //past frame
    }
    
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {   
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
            //#pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag

           struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            

            sampler2D _MainTex;
            sampler2D _PreviousTex;

            

            float4 frag (v2f fromV) : SV_Target
            {
                float4 c1 = tex2D(_MainTex, fromV.uv);
                //return black
                c1.r = 0;
                c1.g = 0;
                c1.b = 0;
                return c1; 
            }
            ENDCG
        }
    }
}
