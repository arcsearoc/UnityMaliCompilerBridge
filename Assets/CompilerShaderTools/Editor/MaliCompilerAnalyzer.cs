using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// Mali Compiler结果分析器
/// </summary>
public static class MaliCompilerAnalyzer
{
    /// <summary>
    /// 分析编译结果并提取关键性能指标
    /// </summary>
    public static PerformanceMetrics AnalyzeCompileResult(string compileOutput)
    {
        var metrics = new PerformanceMetrics();
        
        if (string.IsNullOrEmpty(compileOutput))
            return metrics;
        
        try
        {
            // 提取工作寄存器数量
            ExtractWorkRegisters(compileOutput, metrics);
            
            // 提取Uniform寄存器数量
            ExtractUniformRegisters(compileOutput, metrics);
            
            // 提取16位运算占比
            Extract16BitArithmetic(compileOutput, metrics);
            
            // 提取指令周期信息
            ExtractInstructionCycles(compileOutput, metrics);
            
            // 提取瓶颈单元信息
            ExtractBottleneckUnit(compileOutput, metrics);
            
            // 检测特殊属性
            DetectShaderProperties(compileOutput, metrics);
            
            // 提取GPU架构信息
            ExtractGPUArchitecture(compileOutput, metrics);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"分析编译结果时出错: {e.Message}");
        }
        
        return metrics;
    }
    
    private static void ExtractWorkRegisters(string output, PerformanceMetrics metrics)
    {
        var match = Regex.Match(output, @"Work registers\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int workRegisters))
            {
                metrics.WorkRegisters = workRegisters;
            }
        }
    }
    
    private static void ExtractUniformRegisters(string output, PerformanceMetrics metrics)
    {
        var match = Regex.Match(output, @"Uniform registers\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int uniformRegisters))
            {
                metrics.UniformRegisters = uniformRegisters;
            }
        }
    }
    
    private static void Extract16BitArithmetic(string output, PerformanceMetrics metrics)
    {
        var match = Regex.Match(output, @"16-bit arithmetic\s*:\s*(\d+(?:\.\d+)?)\s*%", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (float.TryParse(match.Groups[1].Value, out float percentage))
            {
                metrics.SixteenBitArithmeticPercentage = percentage;
            }
        }
    }
    
    private static void ExtractInstructionCycles(string output, PerformanceMetrics metrics)
    {
        // 提取总指令周期
        var totalMatch = Regex.Match(output, @"Total instruction cycles\s*:\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (totalMatch.Success)
        {
            if (float.TryParse(totalMatch.Groups[1].Value, out float total))
            {
                metrics.TotalInstructionCycles = total;
            }
        }
        
        // 提取最短路径周期
        var shortestMatch = Regex.Match(output, @"Shortest path cycles\s*:\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (shortestMatch.Success)
        {
            if (float.TryParse(shortestMatch.Groups[1].Value, out float shortest))
            {
                metrics.ShortestPathCycles = shortest;
            }
        }
        
        // 提取最长路径周期
        var longestMatch = Regex.Match(output, @"Longest path cycles\s*:\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (longestMatch.Success)
        {
            if (float.TryParse(longestMatch.Groups[1].Value, out float longest))
            {
                metrics.LongestPathCycles = longest;
            }
        }
    }
    
    private static void ExtractBottleneckUnit(string output, PerformanceMetrics metrics)
    {
        var match = Regex.Match(output, @"Bound\s*:\s*([A-Z]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            metrics.BottleneckUnit = match.Groups[1].Value.ToUpper();
        }
    }
    
    private static void DetectShaderProperties(string output, PerformanceMetrics metrics)
    {
        metrics.HasUniformComputation = output.Contains("Has uniform computation: true");
        metrics.HasSideEffects = output.Contains("Has side-effects: true");
        metrics.ModifiesCoverage = output.Contains("Modifies coverage: true");
        metrics.UsesLateZSTest = output.Contains("Uses late ZS test: true");
        metrics.UsesLateZSUpdate = output.Contains("Uses late ZS update: true");
        metrics.ReadsColorBuffer = output.Contains("Reads color buffer: true");
        
        // 检测Stack spilling
        if (output.Contains("Stack spilling") && !output.Contains("Stack spilling: false"))
        {
            metrics.HasStackSpilling = true;
        }
    }
    
    private static void ExtractGPUArchitecture(string output, PerformanceMetrics metrics)
    {
        if (output.Contains("Bifrost"))
        {
            metrics.GPUArchitecture = "Bifrost";
        }
        else if (output.Contains("Valhall"))
        {
            metrics.GPUArchitecture = "Valhall";
        }
        else if (output.Contains("Midgard"))
        {
            metrics.GPUArchitecture = "Midgard";
        }
    }
    
    /// <summary>
    /// 根据分析结果生成优化建议
    /// </summary>
    public static List<OptimizationSuggestion> GenerateOptimizationSuggestions(PerformanceMetrics vertexMetrics, PerformanceMetrics fragmentMetrics)
    {
        var suggestions = new List<OptimizationSuggestion>();
        
        // 工作寄存器优化建议
        if (vertexMetrics.WorkRegisters > 32 || fragmentMetrics.WorkRegisters > 32)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = Priority.High,
                Category = "寄存器优化",
                Issue = "工作寄存器使用量过高",
                Suggestion = "减少局部变量的使用，考虑合并计算步骤，使用更低精度的数据类型。",
                ExpectedImpact = "减少寄存器压力，提高并行执行效率"
            });
        }
        
        // 16位运算优化建议
        if (fragmentMetrics.SixteenBitArithmeticPercentage < 50)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = Priority.Medium,
                Category = "精度优化",
                Issue = "16位运算占比较低",
                Suggestion = "将适当的变量从highp改为mediump，特别是颜色和纹理坐标相关计算。",
                ExpectedImpact = "提高运算效率，减少功耗"
            });
        }
        
        // Stack spilling警告
        if (vertexMetrics.HasStackSpilling || fragmentMetrics.HasStackSpilling)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = Priority.Critical,
                Category = "内存优化",
                Issue = "检测到Stack Spilling",
                Suggestion = "严重的性能问题！请减少变量数量，降低精度或简化shader逻辑。",
                ExpectedImpact = "避免昂贵的内存访问，大幅提升性能"
            });
        }
        
        // 瓶颈单元优化建议
        if (!string.IsNullOrEmpty(fragmentMetrics.BottleneckUnit))
        {
            switch (fragmentMetrics.BottleneckUnit)
            {
                case "A": // Arithmetic
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Priority = Priority.High,
                        Category = "算术优化",
                        Issue = "算术运算成为瓶颈",
                        Suggestion = "减少复杂数学运算，避免使用反三角函数，考虑使用查找表或近似算法。",
                        ExpectedImpact = "减少算术单元压力，提高执行效率"
                    });
                    break;
                case "T": // Texture
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Priority = Priority.High,
                        Category = "纹理优化",
                        Issue = "纹理采样成为瓶颈",
                        Suggestion = "减少纹理采样次数，避免在分支中采样，考虑纹理合并。",
                        ExpectedImpact = "减少纹理单元压力，提高纹理缓存效率"
                    });
                    break;
                case "LS": // Load/Store
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Priority = Priority.Medium,
                        Category = "内存访问优化",
                        Issue = "内存读写成为瓶颈",
                        Suggestion = "减少varying变量传递，优化uniform buffer布局。",
                        ExpectedImpact = "提高内存访问效率"
                    });
                    break;
            }
        }
        
        // 特殊属性警告
        if (fragmentMetrics.UsesLateZSTest || fragmentMetrics.UsesLateZSUpdate)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = Priority.High,
                Category = "深度测试优化",
                Issue = "使用了Late Z测试",
                Suggestion = "避免在fragment shader中修改深度值或使用discard，这会禁用Early Z优化。",
                ExpectedImpact = "启用Early Z优化，减少overdraw"
            });
        }
        
        if (fragmentMetrics.HasSideEffects)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = Priority.Medium,
                Category = "副作用优化",
                Issue = "Shader具有副作用",
                Suggestion = "避免使用图像存储、原子操作等具有副作用的操作。",
                ExpectedImpact = "提高并行执行效率"
            });
        }
        
        return suggestions;
    }
    
    /// <summary>
    /// 格式化分析结果为可读文本
    /// </summary>
    public static string FormatAnalysisReport(PerformanceMetrics vertexMetrics, PerformanceMetrics fragmentMetrics, List<OptimizationSuggestion> suggestions)
    {
        StringBuilder report = new StringBuilder();
        
        report.AppendLine("=== Mali Compiler 性能分析报告 ===\n");
        
        // Vertex Shader分析
        report.AppendLine("【Vertex Shader 分析】");
        report.AppendLine($"• 工作寄存器: {vertexMetrics.WorkRegisters}");
        report.AppendLine($"• Uniform寄存器: {vertexMetrics.UniformRegisters}");
        report.AppendLine($"• 16位运算占比: {vertexMetrics.SixteenBitArithmeticPercentage:F1}%");
        report.AppendLine($"• 最短路径周期: {vertexMetrics.ShortestPathCycles}");
        report.AppendLine($"• 最长路径周期: {vertexMetrics.LongestPathCycles}");
        if (!string.IsNullOrEmpty(vertexMetrics.BottleneckUnit))
            report.AppendLine($"• 瓶颈单元: {vertexMetrics.BottleneckUnit}");
        report.AppendLine();
        
        // Fragment Shader分析
        report.AppendLine("【Fragment Shader 分析】");
        report.AppendLine($"• 工作寄存器: {fragmentMetrics.WorkRegisters}");
        report.AppendLine($"• Uniform寄存器: {fragmentMetrics.UniformRegisters}");
        report.AppendLine($"• 16位运算占比: {fragmentMetrics.SixteenBitArithmeticPercentage:F1}%");
        report.AppendLine($"• 最短路径周期: {fragmentMetrics.ShortestPathCycles}");
        report.AppendLine($"• 最长路径周期: {fragmentMetrics.LongestPathCycles}");
        if (!string.IsNullOrEmpty(fragmentMetrics.BottleneckUnit))
            report.AppendLine($"• 瓶颈单元: {fragmentMetrics.BottleneckUnit}");
        report.AppendLine();
        
        // 优化建议
        if (suggestions.Count > 0)
        {
            report.AppendLine("【优化建议】");
            foreach (var suggestion in suggestions)
            {
                string priorityIcon = suggestion.Priority switch
                {
                    Priority.Critical => "🔴",
                    Priority.High => "🟠",
                    Priority.Medium => "🟡",
                    Priority.Low => "🟢",
                    _ => "ℹ️"
                };
                
                report.AppendLine($"{priorityIcon} [{suggestion.Category}] {suggestion.Issue}");
                report.AppendLine($"   建议: {suggestion.Suggestion}");
                report.AppendLine($"   预期效果: {suggestion.ExpectedImpact}");
                report.AppendLine();
            }
        }
        
        return report.ToString();
    }
}

/// <summary>
/// 性能指标数据结构
/// </summary>
[Serializable]
public class PerformanceMetrics
{
    public int WorkRegisters;
    public int UniformRegisters;
    public float SixteenBitArithmeticPercentage;
    public float TotalInstructionCycles;
    public float ShortestPathCycles;
    public float LongestPathCycles;
    public string BottleneckUnit = "";
    public string GPUArchitecture = "";
    
    // Shader属性
    public bool HasUniformComputation;
    public bool HasSideEffects;
    public bool ModifiesCoverage;
    public bool UsesLateZSTest;
    public bool UsesLateZSUpdate;
    public bool ReadsColorBuffer;
    public bool HasStackSpilling;
}

/// <summary>
/// 优化建议数据结构
/// </summary>
[Serializable]
public class OptimizationSuggestion
{
    public Priority Priority;
    public string Category;
    public string Issue;
    public string Suggestion;
    public string ExpectedImpact;
}

/// <summary>
/// 优先级枚举
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}
