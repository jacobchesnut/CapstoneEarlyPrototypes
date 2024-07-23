Shader "Unlit/QualityTestShader"
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

            //override if not 0
            float _cpdOverride;

            //for moving the region
            float _addedXOffset;

            //variable for regular cycles per degree to be used. 6cpd = 3393, 3 cpd = 1696, 1.5cpd = 848
            float _cpdMultiplier;
            

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

                
                //testing showing 6 c/degree using sin and angle
                //sin repeats every 2pi
                //repeat 6 times
                //angle will change in radians
                //1 cycle = 2pi (diff in angle)
                //6 = 2pi * 6
                //see if this works
                //no because angle will only change ~90 degrees, weird number to tie this to
                //need it to be relative to screen space, so attached to x and y position...
                //across the screen x will go from 0 to 1, which is about a third of pi, about a sixth of a full sin wave if we need to go to 2pi to complete a cycle...
                //at 90 degrees fov, this means a change of 0.01 (repeating) is one degree change on screen. so to have 6 cycles per degree the sin function has to go from
                //0 to 12pi in the change of 0.01x
                //the full screen should go from 0 - 90 * 12pi... so 3393 times x. (3392.92 rounded up)

                //test replacements for angle
                //angle = fromV.uv.x;

                //place red dot at focal point
                if(angle < _overlaySize){
                    c1.r = 1;
                    c1.g = 0;
                    c1.b = 0;
                    return c1;
                }

                //calculate center point for y axis which should be where focal point is, this will be used to bound the display of region
                float centerY = (_offsetAngleY * 0.011f);
                
                //calculate the center point for where the gradient should be on the x axis
                float centerX = ((_offsetAngleX + _addedXOffset) * 0.011f);

                //use pattern if near middle of screen on y axis, otherwise just display white
                if(abs(fromV.uv.y + centerY - 0.5) < 0.02f && abs(fromV.uv.x + centerX - 0.5) < 0.02f){
                    c1.r = sin(fromV.uv.x * _cpdMultiplier);
                    c1.g = sin(fromV.uv.x * _cpdMultiplier);
                    c1.b = sin(fromV.uv.x * _cpdMultiplier);
                    if(_cpdOverride != 0){
                        c1.r = sin(fromV.uv.x * _cpdOverride);
                        c1.g = sin(fromV.uv.x * _cpdOverride);
                        c1.b = sin(fromV.uv.x * _cpdOverride);
                    }
                }
                else{
                    c1.r = 1;
                    c1.g = 1;
                    c1.b = 1;
                }

                

                //normal sin goes negative, so drag out and divide in half for more gradual which has a minimum of 0 and max of 1
                c1.r = (c1.r + 1)/2;
                c1.g = (c1.g + 1)/2;
                c1.b = (c1.b + 1)/2;

                return c1;
            }
            ENDCG
        }
    }
}
