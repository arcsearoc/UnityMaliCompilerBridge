using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System;
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
    /// 使用反射调用Unity内部API进行Shader编译
    /// </summary>
    private static string CompileShaderForSpecificPlatform(Shader shader, ShaderCompilerPlatform platform)
    {
        try
        {
            // 模拟Shader Inspector中"Compile and show code"的典型参数
            int mode = 3; // 模式：Custom
            int platformMask = 1 << (int)platform; // 目标平台掩码（Windows）
            bool includeAllVariants = false; // 不包含所有变体
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
}
