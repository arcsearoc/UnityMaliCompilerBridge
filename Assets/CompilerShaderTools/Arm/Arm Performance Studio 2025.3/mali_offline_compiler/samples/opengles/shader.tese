#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2016-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

#extension GL_EXT_tessellation_shader : require
precision highp float;

layout(quads, fractional_odd_spacing, ccw) in;
patch in float tc_detail_level;
in vec3 tc_position[];
out vec3 te_position;
out vec2 te_tess_coord;
out float te_mip_level;
out float te_detail_level;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float use_mip;
uniform float height_scale;
uniform samplerCube heightmap;

// When the object's distance to the camera is very small
// we select the 0th (most high quality) mipmap. When the
// distance is large, we select the mipmap corresponding
// to the following fraction of the full texture size.
#define MIN_TEXTURE_SAMPLE_FRACTION 0.1

// We select the worst mip level when the object is this far away
#define MAX_MIP_SELECT_DIST 1.7

// And the best when the object is this far away
#define MIN_MIP_SELECT_DIST 0.7

float ComputeMipLevel(float view_z)
{
    float t = clamp((-view_z - MIN_MIP_SELECT_DIST) /
                    (MAX_MIP_SELECT_DIST - MIN_MIP_SELECT_DIST), 0.0, 1.0);
    // The expression below is derived as follows:
    // Let N : # texels in texture
    //     f : min texture sample fraction
    // Then M = fN is the size of the lowest quality mipmap we
    // sample from. When z is 0 we want the 0th mipmap, when
    // z is 1 we want the mipmap that has size M. That is,
    //    k =  log2(N) - log2(fN)
    //      =  log2(N / (fN))
    //      =  log2(1 / f)
    //      = -log2(f)
    // A simple expression is a linear interpolation between
    // 0th mip level and the kth mip level.
    return -log2(MIN_TEXTURE_SAMPLE_FRACTION) * t;
}

void main()
{
    float u = gl_TessCoord.x;
    float v = gl_TessCoord.y;
    vec3 a = mix(tc_position[0], tc_position[1], u);
    vec3 b = mix(tc_position[3], tc_position[2], u);
    te_position = mix(a, b, v);

    float h = 0.0;
    if (use_mip > 0.5)
    {
        float view_z = (view * model * vec4(te_position, 1.0)).z;
        te_mip_level = ComputeMipLevel(view_z);

        h = textureLod(heightmap, te_position, te_mip_level).r;
    }
    else
    {
        h = texture(heightmap, te_position).r;
    }

    te_position = normalize(te_position);
    te_position *= 1.0 + h * height_scale;
    te_detail_level = tc_detail_level;
    te_tess_coord = gl_TessCoord.xy;

    gl_Position = projection * view * model * vec4(te_position, 1.0);
}
