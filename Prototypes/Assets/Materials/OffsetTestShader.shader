Shader "Unlit/OffsetTestShader"
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

            //Foveated specifics
            float4 _frustumVector; //frustum info (width, height, distance), a is unused
            static const float4 _viewVector = {0,0,1,0}; //assume looking forward for the calculations here

            static const float _overlaySize = 0.005f; //controls how large the overlay images will be in angles

            //for offset from calibration
            float _offsetAngleX;
            float _offsetAngleY;
            static const float _FOV = 90; //temporarily assuming the screen spans 90 degrees, this is pretty close to the actual numbers in vr mode
            
            float4 frag (v2f fromV) : SV_Target
            {
                


                float4 c1 = tex2D(_MainTex, fromV.uv); //this is the color of the pixel
                //check the angle distance from the look direction
                //angle = arccos(a.b / magA + magB)
                //A is the vector to this pixel
                //x = (texX - 0.5) * width, y = (texY - 0.5) * height, z = given distance
                //additionally to account for angle offset from calibration
                //one full screen's width is 90 degrees, which translates to 1 float distance here
                //so adding offset/FOV will give the offset (fraction of the screen to move) and should be added to the vector here
                //positive offsets go left and down, negative offsets go right and up
                float3 vecA = {(fromV.uv.x - 0.5 + (_offsetAngleX / _FOV)) * _frustumVector.x, (fromV.uv.y - 0.5 + (_offsetAngleY / _FOV)) * _frustumVector.y, _frustumVector.z};
                //B is the direction vector of the gaze
                
                float dot = (vecA.x * _viewVector.x) + (vecA.y * _viewVector.y) + (vecA.z * _viewVector.z);
                float magA = sqrt((vecA.x * vecA.x) + (vecA.y * vecA.y) + (vecA.z * vecA.z));
                float magB = sqrt((_viewVector.x * _viewVector.x) + (_viewVector.y * _viewVector.y) + (_viewVector.z * _viewVector.z));
                float angle = acos((dot / (magA * magB)));


                if(angle <= _overlaySize){
                        //make the dot the user is calibrating magenta
                        c1.g = 0;
                        c1.r = 1;
                        c1.b = 1;
                }

                return c1;
            }
            ENDCG
        }
    }
}
