#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2016-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

precision mediump float;
precision mediump sampler;

layout(location = 0) in vec2 v_uv;

layout(location = 0) out vec4 FragColor;

layout(set = 0, binding = 0) uniform sampler2D samplerTest;

void main()
{
    FragColor = vec4(texture(samplerTest, v_uv).rgb, 1.0f);
}
