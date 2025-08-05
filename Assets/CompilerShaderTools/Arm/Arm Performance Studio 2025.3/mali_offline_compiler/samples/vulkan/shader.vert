#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2016-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uv;
layout(location = 2) in vec4 offset;

layout(location = 0) out vec2 v_uv;

layout(push_constant) uniform pushConstants
{
    mat4 m;
    mat4 vp;
} constants;

void main()
{
    mat4 translate = mat4(vec4(1, 0, 0, 0), vec4(0, 1, 0, 0), vec4(0, 0, 1, 0), vec4(offset.xyz, 1.0f));
    gl_Position = constants.vp * translate * constants.m * (vec4(position * offset.w, 1));
    v_uv = uv;
}
