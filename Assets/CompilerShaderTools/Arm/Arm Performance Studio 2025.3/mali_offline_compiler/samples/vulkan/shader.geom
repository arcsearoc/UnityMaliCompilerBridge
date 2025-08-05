#version 310 es

/*
 * This confidential and proprietary software may be used only as
 * authorised by a licensing agreement from Arm Limited.
 *    Copyright 2016-2020 Arm Ltd. All Rights Reserved.
 * The entire notice above must be reproduced on all authorised
 * copies and copies may only be made to the extent permitted
 * by a licensing agreement from Arm Limited.
 */

#extension GL_EXT_geometry_shader : require

layout(triangles, invocations = 6) in;
layout(max_vertices = 60) out;
layout(triangle_strip) out;
void main()
{
  for(int i = 0; i < gl_in.length(); i++)
  {
    gl_Position = gl_in[i].gl_Position;
    EmitVertex();
  }
  EndPrimitive();

  for(int i = 0; i < gl_in.length(); i++)
  {
    vec4 vertex = gl_in[i].gl_Position;
    vertex.z = -vertex.z;
    gl_Position = vertex;
    EmitVertex();
  }
  EndPrimitive();
}
