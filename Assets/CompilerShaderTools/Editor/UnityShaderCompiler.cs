using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System;
using System.Text.RegularExpressions;
using UnityEditor.Rendering;

/// <summary>
/// Unity内置Shader编译器工具类
/// 使用Unity内置API获取编译后的Shader代码，而不是手动解析
/// </summary>
public static class UnityShaderCompiler
{
    /// <summary>
    /// 编译结果数据结构
    /// </summary>
    public class ShaderCompileResult
    {
        public string vertexShader;
        public string fragmentShader;
        public bool isSuccess;
        public string errorMessage;
        public UnityEditor.Rendering.ShaderCompilerPlatform platform;
    }
    
    /// <summary>
    /// 使用Unity内置编译器获取Shader的编译代码
    /// </summary>
    public static ShaderCompileResult CompileShaderForPlatform(Shader shader, ShaderCompilerPlatform platform)
    {
        var result = new ShaderCompileResult();
        result.platform = platform;
        
        try
        {
            if (shader == null)
            {
                result.errorMessage = "Shader对象为空";
                return result;
            }
            
            // 编译指定平台的代码
            var compiledShader = CompileShaderForSpecificPlatform(shader, platform);
            
            if (!string.IsNullOrEmpty(compiledShader))
            {
                result.vertexShader = ExtractVertexShaderFromCompiled(compiledShader);
                result.fragmentShader = ExtractFragmentShaderFromCompiled(compiledShader);
                
                // 处理GLSL版本问题，将#version 300 es替换为#version 310 es
                result.vertexShader = ProcessGLSLVersion(result.vertexShader);
                result.fragmentShader = ProcessGLSLVersion(result.fragmentShader);
                
                result.isSuccess = true;
            }
            else
            {
                result.errorMessage = "编译失败，无法获取编译后的代码";
            }
        }
        catch (System.Exception e)
        {
            result.errorMessage = $"编译过程中出现异常: {e.Message}";
            Debug.LogError($"Unity Shader编译错误: {e}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 处理GLSL版本，将#version 300 es替换为#version 310 es以解决某些编译问题
    /// </summary>
    private static string ProcessGLSLVersion(string shaderCode)
    {
        if (string.IsNullOrEmpty(shaderCode))
            return shaderCode;
            
        // 使用正则表达式替换#version 300 es为#version 310 es
        return Regex.Replace(shaderCode, @"#version\s+300\s+es", "#version 310 es");
    }
    
    /// <summary>
    /// 使用反射调用Unity内部API进行Shader编译
    /// </summary>
    private static string CompileShaderForSpecificPlatform(Shader shader, ShaderCompilerPlatform platform)
    {
        try
        {
            // 模拟Shader Inspector中"Compile and show code"的典型参数
            int mode = 3; // 模式：Custom
            int platformMask = 1 << (int)platform; // 目标平台掩码（Windows）
            bool includeAllVariants = true; // 包含所有变体
            bool preprocessOnly = false; // 不仅预处理
            bool stripLineDirectives = false; // 不剥离行指令

            // 调用方法
            bool success = InvokeOpenCompiledShader(shader, mode, platformMask, includeAllVariants, preprocessOnly, stripLineDirectives);
            if (success)
            {
                string targetPath = Application.dataPath + "/../Temp/Compiled-" + shader.name.Replace("/", "-") + ".shader";
                if (File.Exists(targetPath))
                {
                    return File.ReadAllText(targetPath);
                }
                return string.Empty;
            }
            return string.Empty;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"编译Shader时出错: {e.Message}");
            return null;
        }
    }

    // 反射调用OpenCompiledShader方法
    /// <summary>
    /// OpenCompiledShader 会调用Unity引擎内的方法，
    /// 在生成shader代码之后默认会额外打开它
    /// 是否有其他方式呢？
    /// </summary>
    /// <param name="shader"></param>
    /// <param name="mode"></param>
    /// <param name="externPlatformsMask"></param>
    /// <param name="includeAllVariants"></param>
    /// <param name="preprocessOnly"></param>
    /// <param name="stripLineDirectives"></param>
    /// <returns></returns>
    public static bool InvokeOpenCompiledShader(
        Shader shader,
        int mode,
        int externPlatformsMask,
        bool includeAllVariants,
        bool preprocessOnly,
        bool stripLineDirectives)
    {
        if (shader == null)
        {
            Debug.LogError("Shader cannot be null");
            return false;
        }

        try
        {
            // 获取ShaderUtil类型
            Type shaderUtilType = typeof(ShaderUtil);

            // 查找OpenCompiledShader方法
            MethodInfo openCompiledMethod = shaderUtilType.GetMethod(
                "OpenCompiledShader",
                BindingFlags.Static | BindingFlags.NonPublic, // 匹配internal static
                null,
                new Type[] {
                    typeof(Shader),               // shader参数
                    typeof(int),                  // mode参数
                    typeof(int),                  // externPlatformsMask参数
                    typeof(bool),                 // includeAllVariants参数
                    typeof(bool),                 // preprocessOnly参数
                    typeof(bool)                  // stripLineDirectives参数
                },
                null
            );

            if (openCompiledMethod == null)
            {
                Debug.LogError("Could not find OpenCompiledShader method");
                return false;
            }

            // 调用方法
            openCompiledMethod.Invoke(
                null, // 静态方法，第一个参数为null
                new object[] {
                    shader,
                    mode,
                    externPlatformsMask,
                    includeAllVariants,
                    preprocessOnly,
                    stripLineDirectives
                }
            );

            return true;
        }
        catch (TargetInvocationException e)
        {
            Debug.LogError($"Error invoking OpenCompiledShader: {e.InnerException.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Reflection error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 提取顶点着色器并移除最外层的#ifdef VERTEX和#endif标签
    /// </summary>
    public static string ExtractVertexShaderFromCompiled(string compiledCode)
    {
        return ExtractAndCleanShaderSection(
            compiledCode,
            "#ifdef VERTEX",
            "#endif"
        );
    }

    /// <summary>
    /// 提取片段着色器并移除最外层的#ifdef FRAGMENT和#endif标签
    /// </summary>
    public static string ExtractFragmentShaderFromCompiled(string compiledCode)
    {
        return ExtractAndCleanShaderSection(
            compiledCode,
            "#ifdef FRAGMENT",
            "#endif"
        );
    }

    /// <summary>
    /// 提取指定区间代码并移除最外层的起始和结束标签
    /// </summary>
    private static string ExtractAndCleanShaderSection(string code, string startMarker, string endMarker)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(startMarker) || string.IsNullOrEmpty(endMarker))
            return string.Empty;

        var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var sectionLines = new List<string>();
        var preprocessorStack = new Stack<string>();
        bool inTargetSection = false;

        // 第一步：提取包含外层标签的完整区间
        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();

            if (!inTargetSection && trimmedLine == startMarker)
            {
                inTargetSection = true;
                preprocessorStack.Push(startMarker);
                sectionLines.Add(line); // 暂存起始标签
                continue;
            }

            if (inTargetSection)
            {
                // 处理嵌套的预处理指令
                if (trimmedLine.StartsWith("#if") ||
                    trimmedLine.StartsWith("#ifdef") ||
                    trimmedLine.StartsWith("#ifndef"))
                {
                    preprocessorStack.Push(trimmedLine);
                    sectionLines.Add(line);
                }
                else if (trimmedLine == endMarker)
                {
                    if (preprocessorStack.Count > 0)
                        preprocessorStack.Pop();

                    sectionLines.Add(line); // 暂存结束标签

                    // 找到匹配的结束标签，退出提取
                    if (preprocessorStack.Count == 0)
                    {
                        inTargetSection = false;
                        break;
                    }
                }
                else
                {
                    sectionLines.Add(line);
                }
            }
        }

        // 第二步：移除最外层的起始和结束标签
        if (sectionLines.Count >= 2)
        {
            // 移除第一个元素（起始标签）和最后一个元素（结束标签）
            sectionLines.RemoveAt(0);
            sectionLines.RemoveAt(sectionLines.Count - 1);
        }

        // 第三步：拼接结果并清除空行
        var resultBuilder = new StringBuilder();
        foreach (var line in sectionLines)
        {
            // 保留非空行
            if (!string.IsNullOrWhiteSpace(line))
            {
                resultBuilder.AppendLine(line);
            }
        }

        return resultBuilder.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// 验证编译后的代码是否适合Mali Compiler分析
    /// </summary>
    public static bool IsValidForMaliAnalysis(string shaderCode)
    {
        if (string.IsNullOrEmpty(shaderCode))
            return false;
            
        // 检查是否包含GLSL代码特征
        return shaderCode.Contains("#version") || 
               shaderCode.Contains("gl_Position") || 
               shaderCode.Contains("texture") ||
               shaderCode.Contains("uniform") ||
               shaderCode.Contains("varying") ||
               shaderCode.Contains("attribute");
    }
    
    /// <summary>
    /// 检测是否为URP着色器
    /// </summary>
    public static bool IsURPShader(Shader shader)
    {
        if (shader == null)
            return false;
            
        string shaderPath = AssetDatabase.GetAssetPath(shader);
        if (string.IsNullOrEmpty(shaderPath))
            return false;
            
        // 检查shader文件内容是否包含URP特征
        try
        {
            string shaderContent = File.ReadAllText(shaderPath);
            return shaderContent.Contains("Universal Render Pipeline") || 
                   shaderContent.Contains("UniversalPipeline") || 
                   shaderContent.Contains("URP") ||
                   shaderContent.Contains("Packages/com.unity.render-pipelines.universal");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 表示一个已编译的 Shader 变体（按Pass/Keyword组合聚合）
    /// </summary>
    public class ShaderCompiledVariant
    {
        public string vertexShader;
        public string fragmentShader;
        public string passName = "";
        public string keywords = "";
    }

    /// <summary>
    /// 获取所有变体的已编译代码（按顺序配对第N个Vertex与第N个Fragment）
    /// </summary>
    public static List<ShaderCompiledVariant> CompileAllVariantsForPlatform(Shader shader, ShaderCompilerPlatform platform)
    {
        var variants = new List<ShaderCompiledVariant>();
        if (shader == null) return variants;

        string compiled = CompileShaderForSpecificPlatform(shader, platform);
        if (string.IsNullOrEmpty(compiled)) return variants;

        var vertexList = ExtractAllShaderSections(compiled, "#ifdef VERTEX", "#endif");
        var fragmentList = ExtractAllShaderSections(compiled, "#ifdef FRAGMENT", "#endif");

        int count = Math.Min(vertexList.Count, fragmentList.Count);
        for (int i = 0; i < count; i++)
        {
            variants.Add(new ShaderCompiledVariant
            {
                vertexShader = ProcessGLSLVersion(vertexList[i]),
                fragmentShader = ProcessGLSLVersion(fragmentList[i]),
                passName = "",
                keywords = ""
            });
        }
        return variants;
    }

    /// <summary>
    /// 提取所有匹配的区间，并移除最外层标签
    /// </summary>
    private static List<string> ExtractAllShaderSections(string code, string startMarker, string endMarker)
    {
        var results = new List<string>();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(startMarker) || string.IsNullOrEmpty(endMarker))
            return results;

        var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var buffer = new List<string>();
        var stack = new Stack<string>();
        bool inSection = false;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (!inSection && trimmed == startMarker)
            {
                inSection = true;
                stack.Push(startMarker);
                buffer.Clear();
                // 开始记录，但不包含起始标签
                continue;
            }

            if (inSection)
            {
                if (trimmed.StartsWith("#if") || trimmed.StartsWith("#ifdef") || trimmed.StartsWith("#ifndef"))
                {
                    stack.Push(trimmed);
                    buffer.Add(line);
                }
                else if (trimmed == endMarker)
                {
                    if (stack.Count > 0) stack.Pop();

                    if (stack.Count == 0)
                    {
                        // 结束当前段，保存
                        inSection = false;
                        // 去掉前后空行
                        var sb = new StringBuilder();
                        for (int i = 0; i < buffer.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(buffer[i]))
                                sb.AppendLine(buffer[i]);
                        }
                        results.Add(sb.ToString().TrimEnd('\r', '\n'));
                        buffer.Clear();
                    }
                    else
                    {
                        buffer.Add(line);
                    }
                }
                else
                {
                    buffer.Add(line);
                }
            }
        }

        return results;
    }
}
