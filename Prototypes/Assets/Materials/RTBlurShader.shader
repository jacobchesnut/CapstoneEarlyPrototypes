Shader "Unlit/RTBlurShader"
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
            float4 _viewVector; //vector of the gaze view (x, y, z), a is unused
            float _widthPix; //width of the screen in pixels
            float _heightPix; //height of the screen in pixels

            int _regionSize;

            static const float _overlaySize = 0.005f; //controls how large the overlay images will be in angles

            //for offset from calibration
            float _offsetAngleX;
            float _offsetAngleY;
            static const float _FOV = 90; //temporarily assuming the screen spans 90 degrees, this is pretty close to the actual numbers in vr mode

            //angles
            static const float _maxQualityAngleMax = 0.091f;
            static const float _innerAngleMax = 0.222f;
            static const float _secondAngleMax = 0.485f;
            static const float _thirdAngleMax = 1.010f;
            

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
                

                //if farther, group into pixels
                //grouping can be done by multiplying uv by the number of pixels in row/column
                //then divide by group size, and remainder will tell you where you are in group
                //(which tells you which pixels to pull from nearby to get final color)
                _regionSize = 1;
                if(angle < _maxQualityAngleMax){
                    return c1; //do no blur
                }
                else if(angle < _innerAngleMax){
                    _regionSize = 1;
                }
                else if(angle < _secondAngleMax){
                    _regionSize = 2;
                }
                else if(angle < _thirdAngleMax){
                    _regionSize = 3;
                }

                c1 *= 0; //set c1 to 0 to add in proportions of region's pixels
                int myX = round(fromV.uv.x * _widthPix); //using round to avoid any potential floating point precision errors
                int myY = round(fromV.uv.y * _heightPix);
                //sets x and y coords to grab from to the top left pixel in the region
                //not needed for blur
                //myX -= myX % _regionSize;
                //myY -= myY % _regionSize;
                float myXf = myX;
                float myYf = myY;

                float2 tempPos = {0, 0};
                
                float weight = (4*_regionSize*_regionSize);
                //need a loop tag here to force compiler to not try to unravel a dynamic loop
                //best source on this i could find: https://forum.unity.com/threads/compiling-shaders-with-large-loops-causes-unable-to-unroll-error.441897/
                [loop]
                for(int i = -_regionSize; i < _regionSize; i++){
                    [loop]
                    for(int j = -_regionSize; j < _regionSize; j++){
                        tempPos.x = (myXf + i) / _widthPix;
                        tempPos.y = (myYf + j) / _heightPix;
                        c1 += (tex2D(_MainTex, tempPos) / weight);
                    }
                }
                return c1;
            }
            ENDCG
        }
    }
}
