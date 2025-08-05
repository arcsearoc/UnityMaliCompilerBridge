#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2016-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

precision highp float;

in vec3 te_position;
in vec2 te_tess_coord;
in float te_mip_level;
in float te_detail_level;
out vec4 f_color;

uniform mat4 model;
uniform mat4 view;
uniform samplerCube heightmap;
uniform samplerCube diffusemap;
uniform float use_mip;
uniform float height_scale;

vec3 ColorGradient(float t)
{
    vec3 a = vec3(0.5, 0.5, 0.3);
    vec3 b = vec3(0.5, 0.5, 0.3);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.0, 0.1, 0.2);
    return a + b * cos(2.0 * 3.1415926 * (c * t + d));
}

vec3 VisualizeLodFactorOfPatch()
{
    float groove = (length(te_position) - 1.0) / height_scale;
    return ColorGradient(te_detail_level) * groove;
}

vec3 VisualizeDiffuseMap()
{
    return texture(diffusemap, te_position).rgb;
}

float Wireframe()
{
    float u = te_tess_coord.x;
    float v = te_tess_coord.y;
    float w = 0.04;
    float horizontal = 1.0 - (smoothstep(0.0, w, u) - smoothstep(1.0 - w, 1.0, u));
    float vertical = 1.0 - (smoothstep(0.0, w, v) - smoothstep(1.0 - w, 1.0, v));
    return clamp(horizontal + vertical, 0.0, 1.0);
}

void main()
{
    // We fade between wireframe-view and shaded view as the
    // camera zooms inward. The fade happens across the
    // sphere surface. So fragments to the left of a fading-bar
    // gets wireframe'd. Fragments to the right get pure shading.
    float zoom = (view[3][2] - (-8.0)) / (-0.2 - (-8.0));
    float hbar = -1.0 - 1.1*height_scale + smoothstep(0.3, 0.6, zoom) * (2.0 + 2.2 * height_scale);
    float view_x = (view * model * vec4(te_position, 0.0)).x;
    float blend = smoothstep(-0.1, 0.1, view_x - hbar);

    vec3 right = VisualizeDiffuseMap();
    vec3 left = right * VisualizeLodFactorOfPatch();
    f_color.rgb = mix(left, right, blend);
    f_color.rgb += vec3(0.2) * Wireframe() * (1.0 - blend);
    f_color.a = 1.0;
}
