Shader "Unlit/FoveatedShader"
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
            float4 _trueViewVector; //vector from camera to target, which is where the user should be looking
            float _widthPix; //width of the screen in pixels
            float _heightPix; //height of the screen in pixels
            float _invWidth; //width of a pixel
            float _invHeight; //height of a pixel
            float _innerAngleMax; //angle to start foveated rendering

            int _regionSize;

            //int _debugShowRegions;
            float _debugRegionBorderSize;

            static const float _overlaySize = 0.005f; //controls how large the overlay images will be in angles

            //for offset from calibration
            float _offsetAngleX;
            float _offsetAngleY;
            static const float _FOV = 90; //temporarily assuming the screen spans 90 degrees, this is pretty close to the actual numbers in vr mode
            float _offscreenAngleUp;
            float _offscreenAngleRight;
            float _offscreenAngleDown;
            float _offscreenAngleLeft;


            // For debugging
            uint _flag;
            static const uint kShowRegionTint = 1;
            static const uint kShowOverlay = 2;
            static const uint kShowAverageBorder = 4;
            static const uint kShowVariableBorder = 8;
            
            #define CHECK_DEBUG(FLAG, DEBUG_ACTION) {   \
                if (_flag & FLAG)                       \
                    c1 = DEBUG_ACTION;                  \
            }
            

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

                //calculate outside angle
                float outsideAngle = 180;
                if(_flag & kShowVariableBorder){
                    //calculate angle based on weight of the angles x and y
                    float useX = 0;
                    float useY = 0;
                    if(fromV.uv.x < 0.5){
                        //left
                        useX = _offscreenAngleLeft;
                    }
                    else{
                        //right
                        useX = _offscreenAngleRight;
                    }
                    if(fromV.uv.y < 0.5){
                        //bottom
                        useY = _offscreenAngleUp;
                    }
                    else{
                        //top
                        useY = _offscreenAngleDown;
                    }
                    float xRatio = abs(fromV.uv.x - 0.5)/(abs(fromV.uv.x - 0.5)+abs(fromV.uv.y - 0.5));
                    float yRatio = abs(fromV.uv.y - 0.5)/(abs(fromV.uv.y - 0.5)+abs(fromV.uv.x - 0.5));
                    outsideAngle = (useX * xRatio)+(useY * yRatio);
                    
                }
                else if(_flag & kShowAverageBorder){
                    //calculate angle based on the average of the four directions
                    outsideAngle = (_offscreenAngleUp + _offscreenAngleRight + _offscreenAngleDown + _offscreenAngleLeft) / 4;
                    
                }

                //dont render (show black) outside the outside angle
                if(angle > outsideAngle){
                    //c1.r = 0;
                    //c1.g = 0;
                    //c1.b = 0;
                    //c1.a = 0;
                    //option for showing past frame:
                    float one = 1;
                    float2 tempPos = {fromV.uv.x, one-fromV.uv.y};
                    c1 = tex2D(_PreviousTex, tempPos);
                    return c1;
                }

                //if farther, group into pixels
                //grouping can be done by multiplying uv by the number of pixels in row/column
                //then divide by group size, and remainder will tell you where you are in group
                //(which tells you which pixels to pull from nearby to get final color)

                if(angle > _innerAngleMax){
                    c1 *= 0; //set c1 to 0 to add in proportions of region's pixels
                    int myX = round(fromV.uv.x * _widthPix); //using round to avoid any potential floating point precision errors
                    int myY = round(fromV.uv.y * _heightPix);
                    //sets x and y coords to grab from to the top left pixel in the region
                    myX -= myX % _regionSize;
                    myY -= myY % _regionSize;
                    float myXf = myX;
                    float myYf = myY;

                    float2 tempPos = {0, 0};
                    //need a loop tag here to force compiler to not try to unravel a dynamic loop
                    //best source on this i could find: https://forum.unity.com/threads/compiling-shaders-with-large-loops-causes-unable-to-unroll-error.441897/
                    [loop]
                    for(int i = 0; i < _regionSize; i++){
                        [loop]
                        for(int j = 0; j < _regionSize; j++){
                            tempPos.x = (myXf + i) / _widthPix;
                            tempPos.y = (myYf + j) / _heightPix;
                            c1 += (tex2D(_MainTex, tempPos) / (_regionSize * _regionSize));
                        }
                    }
                    //return c1;
                }


                //now calculate angle from the actual viewing direction:
                dot = (vecA.x * _trueViewVector.x) + (vecA.y * _trueViewVector.y) + (vecA.z * _trueViewVector.z);
                magA = sqrt((vecA.x * vecA.x) + (vecA.y * vecA.y) + (vecA.z * vecA.z));
                magB = sqrt((_trueViewVector.x * _trueViewVector.x) + (_trueViewVector.y * _trueViewVector.y) + (_trueViewVector.z * _trueViewVector.z));
                float angleTrue = acos((dot / (magA * magB)));

                if(_flag & kShowRegionTint){
                    if((abs(angle - _innerAngleMax) <= _debugRegionBorderSize && angleTrue > _innerAngleMax) ||
                    (abs(angleTrue - _innerAngleMax) <= _debugRegionBorderSize && angle > _innerAngleMax)){
                        //tint border region green
                        //for both gazes
                        c1.g += 0.1;
                    }
                    else if(angle > _innerAngleMax && angleTrue > _innerAngleMax){
                        //tint foveated region red, outside 
                        c1.r += 0.1;
                    }
                    else if(angle <= _innerAngleMax && angleTrue > _innerAngleMax){
                        //tint inner region blue
                        c1.b += 0.1;
                    }
                    else if(angleTrue <= _innerAngleMax && angle > _innerAngleMax){
                        //tint the correct region (not correctly covered) magenta
                        c1.r += 0.1;
                        c1.b += 0.1;
                    }
                    else if(angleTrue <= _innerAngleMax && angle <= _innerAngleMax){
                        //tint the correct region (which is covered correctly) yellow
                        c1.r += 0.1;
                        c1.g += 0.1;
                    }
                }
                if(_flag & kShowOverlay){
                    //overlay of important points
                    if(angle <= _overlaySize || angleTrue <= _overlaySize){
                        //make the central focal point magenta
                        //this will also turn any points which do not satisfy above conditions magenta,
                        //which clearly indicates any flawed points
                        c1.g = 0;
                        c1.r = 1;
                        c1.b = 1;
                    }
                }

                //old method (locked to 2x2 region)
                /*
                float4 c2,c3,c4;
                if(angle > _innerAngleMax){
                    int myX = round(fromV.uv.x * _widthPix); //using round to avoid any potential floating point precision errors
                    int myY = round(fromV.uv.y * _heightPix);
                    float myXf = myX;
                    float myYf = myY;
                    if((myX % 2 == 0) && (myY % 2 == 0)){
                        //pixel is top left
                        float2 tempPos = {(myXf + 1) / _widthPix, (myYf) / _heightPix}; //tr
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf) / _widthPix;
                        tempPos.y = (myYf + 1) / _heightPix; //bl
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf + 1) / _widthPix;
                        tempPos.y = (myYf + 1) / _heightPix; //br
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 1) && (myY % 2 == 0)){
                        //pixel is top right
                        float2 tempPos = {(myXf - 1) / _widthPix, (myYf) / _heightPix}; //tl
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf) / _widthPix;
                        tempPos.y = (myYf + 1) / _heightPix; //br
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf - 1) / _widthPix;
                        tempPos.y = (myYf + 1) / _heightPix; //bl
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 0) && (myY % 2 == 1)){
                        //pixel is bottom left
                        float2 tempPos = {(myXf + 1) / _widthPix, (myYf) / _heightPix}; //br
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf) / _widthPix;
                        tempPos.y = (myYf - 1) / _heightPix; //tl
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf + 1) / _widthPix;
                        tempPos.y = (myYf - 1) / _heightPix; //tr
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 1) && (myY % 2 == 1)){
                        //pixel is bottom right
                        float2 tempPos = {(myXf - 1) / _widthPix, (myYf) / _heightPix}; //bl
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf) / _widthPix;
                        tempPos.y = (myYf - 1) / _heightPix; //tr
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = (myXf - 1) / _widthPix;
                        tempPos.y = (myYf - 1) / _heightPix; //tl
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    c1 = (c1 * 0.25) + (c2 * 0.25) + (c3 * 0.25) + (c4 * 0.25);

                }
                */

                //alternative method with other pixel width calculations
                /*
                if(angle > _innerAngleMax){
                    int myX = round(fromV.uv.x * _widthPix); //using round to avoid any potential floating point precision errors
                    int myY = round(fromV.uv.y * _heightPix);
                    float myXf = myX;
                    float myYf = myY;
                    if((myX % 2 == 0) && (myY % 2 == 0)){
                        //pixel is top left
                        float2 tempPos = {fromV.uv.x + _invWidth, fromV.uv.y}; //tr
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x;
                        tempPos.y = fromV.uv.y + _invHeight; //bl
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x + _invWidth;
                        tempPos.y = fromV.uv.y + _invHeight; //br
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 1) && (myY % 2 == 0)){
                        //pixel is top right
                        float2 tempPos = {fromV.uv.x - _invWidth, fromV.uv.y}; //tl
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x;
                        tempPos.y = fromV.uv.y + _invHeight; //br
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x - _invWidth;
                        tempPos.y = fromV.uv.y + _invHeight; //bl
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 0) && (myY % 2 == 1)){
                        //pixel is bottom left
                        float2 tempPos = {fromV.uv.x + _invWidth, fromV.uv.y}; //br
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x;
                        tempPos.y = fromV.uv.y - _invHeight; //tl
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x + _invWidth;
                        tempPos.y = fromV.uv.y - _invHeight; //tr
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    if((myX % 2 == 1) && (myY % 2 == 1)){
                        //pixel is bottom right
                        float2 tempPos = {fromV.uv.x - _invWidth, fromV.uv.y}; //bl
                        c2 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x;
                        tempPos.y = fromV.uv.y - _invHeight; //tr
                        c3 = tex2D(_MainTex, tempPos);
                        tempPos.x = fromV.uv.x - _invWidth;
                        tempPos.y = fromV.uv.y - _invHeight; //tl
                        c4 = tex2D(_MainTex, tempPos);
                        //return c2;
                    }
                    c1 = (c1 * 0.25) + (c2 * 0.25) + (c3 * 0.25) + (c4 * 0.25);
                */



                /*float4 x = tex2D(_DepthTexture, fromV.uv);
                float difference = x.a - _groundPlaneY; //DepthShader records height in the alpha channel
                float d = (_fogHeight - difference) / _fogDenominator;

                //background seems to set the y-coordinate to 1
                if (x.a == 1) {    // this is background
                    if (kIncludeBackgroundFog & _flag) {
                        d = 1; //max fog
                    }
                    else {
                        d = 0; //min fog
                    }
                }

                if (difference >= _fogHeight) {
                    CHECK_DEBUG(kShowDebugNear, float4(1, 0, 0, 1))
                    return c1;
                }

                //float nd = _f - _n;
                //d = (d - _n) / nd;
                float blend = 0;
                if (kExponentialFog & _flag) {
                    blend = exp(-d * _fogDensity);
                }
                else {
                    blend = max(0, 1 - d);
                }
                c1 = c1 * blend + _fogColor * (1-blend);
                
                CHECK_DEBUG(kShowDebugBlend, float4(blend, blend, blend, 1))*/

                return c1;
            }
            ENDCG
        }
    }
}
