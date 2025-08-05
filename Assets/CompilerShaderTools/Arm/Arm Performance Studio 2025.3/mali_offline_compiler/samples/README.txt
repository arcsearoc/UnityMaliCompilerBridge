Mali Offline Compiler Samples Guide
===================================

Copyright 2015-2025 Arm Ltd. All Rights Reserved.

Introduction
============

The samples bundle contains the following directories:

* opengles : Shader source samples for OpenGL ES.
* vulkan : Shader source and SPIR-V binary samples for Vulkan.
* opencl : Shader source samples for OpenCL.
* json_reports : Example JSON reports (pretty printed for readability).
* json_schemas : Reference JSON schema files for the JSON reports; refer to
      https://json-schema.org/ for more information about the JSON Schema
      syntax.

To run Mali Offline Compiler on the shader samples, navigate to the samples
folder from the command line and use following commands. These commands assume
that the malioc executable is on your path.

Linux or macOS
==============

OpenGL ES
---------

    malioc opengles/shader.vert
    malioc opengles/shader.frag
    malioc opengles/shader.comp
    malioc opengles/shader.geom
    malioc opengles/shader.tesc
    malioc opengles/shader.tese

Vulkan SPIR-V
-------------

    malioc --vulkan vulkan/shader.vert.spv
    malioc --vulkan vulkan/shader.frag.spv
    malioc --vulkan vulkan/shader.comp.spv
    malioc --vulkan vulkan/shader.geom.spv
    malioc --vulkan vulkan/shader.tesc.spv
    malioc --vulkan vulkan/shader.tese.spv
    malioc --vulkan --name foo vulkan/shader-entrypoint-foo.vert.spv

Vulkan GLSL
-----------

    malioc --vulkan vulkan/shader.vert
    malioc --vulkan vulkan/shader.frag
    malioc --vulkan vulkan/shader.comp
    malioc --vulkan vulkan/shader.geom
    malioc --vulkan vulkan/shader.tesc
    malioc --vulkan vulkan/shader.tese

OpenCL
------

    malioc opencl/kernel.cl --name convolve_sobel

Windows
=======

OpenGL ES
---------

    malioc opengles\shader.vert
    malioc opengles\shader.frag
    malioc opengles\shader.comp
    malioc opengles\shader.geom
    malioc opengles\shader.tesc
    malioc opengles\shader.tese

Vulkan SPIR-V
-------------

    malioc --vulkan vulkan\shader.vert.spv
    malioc --vulkan vulkan\shader.frag.spv
    malioc --vulkan vulkan\shader.comp.spv
    malioc --vulkan vulkan\shader.geom.spv
    malioc --vulkan vulkan\shader.tesc.spv
    malioc --vulkan vulkan\shader.tese.spv
    malioc --vulkan --name foo vulkan\shader-entrypoint-foo.vert.spv

Vulkan GLSL
-----------

    malioc --vulkan vulkan\shader.vert
    malioc --vulkan vulkan\shader.frag
    malioc --vulkan vulkan\shader.comp
    malioc --vulkan vulkan\shader.geom
    malioc --vulkan vulkan\shader.tesc
    malioc --vulkan vulkan\shader.tese

Notes
=====

The SPIR-V binary samples provided with the Mali Offline Compiler have been
created using the following glslang command lines to compile the
source shaders in the "vulkan" directory:

    glslang -V shader.vert
    glslang -V shader.frag
    glslang -V shader.comp
    glslang -V shader.geom
    glslang -V shader.tesc
    glslang -V shader.tese
    glslang -V shader.vert --source-entrypoint main -e foo -o shader-entrypoint-foo.vert.spv

We ship a pre-built version of glslang. Full documentation and source
can be found in the Khronos glslang repository on GitHub:

* https://github.com/KhronosGroup/glslang
