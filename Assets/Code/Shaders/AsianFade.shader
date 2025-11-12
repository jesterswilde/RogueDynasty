Shader "Custom/FogFade"
{
    Properties
    {
        _MainTex         ("Base Texture", 2D) = "white" {}
        _Tint            ("Base Tint", Color) = (1,1,1,1)

        // Manual UV transform (avoid _MainTex_ST)
        _UVTiling        ("UV Tiling (xy)", Vector) = (1,1,0,0)
        _UVOffset        ("UV Offset (xy)", Vector) = (0,0,0,0)

        // PBR controls
        _Metallic        ("Metallic", Range(0,1)) = 0.0
        _Smoothness      ("Smoothness", Range(0,1)) = 0.5
        _Occlusion       ("Occlusion", Range(0,1)) = 1.0

        // Depth-based blend (near = more of this color)
        _DepthColor      ("Depth Blend Color (near)", Color) = (1,0,0,1)
        _DepthNear       ("Depth Near (01 linear)", Range(0,1)) = 0.0
        _DepthFar        ("Depth Far (01 linear)", Range(0,1))  = 0.5
        _DepthPower      ("Depth Curve Power", Range(0.1,8))    = 1.0
        _DepthStrength   ("Depth Blend Strength", Range(0,1))   = 1.0

        // Screen-position blend
        _ScreenColor     ("Screen Blend Color", Color) = (0,0.6,1,1)
        _ScreenAxis      ("Screen Axis (XY)", Vector) = (1,0,0,0)     // 1,0 = L→R ; 0,1 = B→T
        _ScreenCenter    ("Screen Center (XY 0..1)", Vector) = (0.5,0.5,0,0)
        _ScreenPower     ("Screen Curve Power", Range(0.1,8)) = 1.0
        _ScreenStrength  ("Screen Blend Strength", Range(0,1)) = 1.0
        _ScreenInvert    ("Screen Invert (0/1)", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _Tint;

        float4 _UVTiling;
        float4 _UVOffset;

        // PBR
        half _Metallic;
        half _Smoothness;
        half _Occlusion;

        // Depth
        float4 _DepthColor;
        float  _DepthNear;
        float  _DepthFar;
        float  _DepthPower;
        float  _DepthStrength;

        // Screen
        float4 _ScreenColor;
        float4 _ScreenAxis;
        float4 _ScreenCenter;
        float  _ScreenPower;
        float  _ScreenStrength;
        float  _ScreenInvert;

        // Camera depth texture (Built-in RP)
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

        struct Input
        {
            float2 uv_MainTex;
            float4 screenPos; // set in vert()
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Manual UV transform to avoid _MainTex_ST
            o.uv_MainTex = v.texcoord.xy * _UVTiling.xy + _UVOffset.xy;

            float4 clipPos = UnityObjectToClipPos(v.vertex);
            o.screenPos = ComputeScreenPos(clipPos); // for projected depth & 0..1 screen UVs
        }

        // Return linear 0..1 depth (0 near clip, 1 far clip)
        float Linear01DepthFromScreen(float4 screenPos)
        {
            float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(screenPos));
            return Linear01Depth(rawDepth);
        }

        // 0..1 factor along chosen screen-space axis with adjustable center/power
        float ScreenFactor01(float4 screenPos)
        {
            // ComputeScreenPos gives proj coords; divide by w -> 0..1 UVs
            float2 uv01 = screenPos.xy / screenPos.w;

            float2 axis = normalize(_ScreenAxis.xy + 1e-6);
            float2 rel  = uv01 - _ScreenCenter.xy;

            // signed projection onto axis, remap to ~0..1 with 0.5 at center
            float s = dot(rel, axis);
            float f = saturate(0.5 + s);

            if (_ScreenInvert > 0.5) f = 1.0 - f;

            f = pow(f, _ScreenPower);
            return f;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 baseCol = tex2D(_MainTex, IN.uv_MainTex) * _Tint;

            // Screen-position blend first
            float screenT = ScreenFactor01(IN.screenPos);
            float3 colAfterScreen = lerp(baseCol.rgb, _ScreenColor.rgb, screenT * _ScreenStrength);

            // Depth-based blend (closer -> more of depth color)
            float d01   = Linear01DepthFromScreen(IN.screenPos);
            float denom = max(1e-5, _DepthFar - _DepthNear);
            float depthN = saturate((d01 - _DepthNear) / denom);
            float nearT  = pow(1.0 - depthN, _DepthPower); // invert so near -> 1
            float3 finalRGB = lerp(colAfterScreen, _DepthColor.rgb, nearT * _DepthStrength);

            // Output to PBR
            o.Albedo     = finalRGB;
            o.Metallic   = _Metallic;
            o.Smoothness = _Smoothness;
            o.Occlusion  = _Occlusion;
            o.Alpha      = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
