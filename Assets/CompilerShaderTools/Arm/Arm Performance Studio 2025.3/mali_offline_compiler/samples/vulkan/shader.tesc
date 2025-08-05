#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2015-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

#extension GL_EXT_tessellation_shader : require

layout(vertices = 3) out;

void main()
{
    gl_TessLevelInner[0] = 1.0;
    gl_TessLevelOuter[0] = 1.0;
    gl_TessLevelOuter[1] = 1.0;
    gl_TessLevelOuter[2] = 1.0;
}
