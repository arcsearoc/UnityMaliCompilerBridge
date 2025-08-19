using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Text;
using static UnityShaderCompiler;

/// <summary>
/// Mali Compiler集成工具
/// 集成了配置管理、结果分析和优化建议功能
/// </summary>
public class MaliCompilerWindow : EditorWindow
{
    [MenuItem("Tools/Mali Compiler Integration/Main Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<MaliCompilerWindow>("Mali Compiler");
        window.minSize = new Vector2(600, 800);
    }
    
    [MenuItem("Tools/Mali Compiler Integration/Quick Compile")]
    public static void QuickCompile()
    {
        // 快速编译当前选中的Shader
        if (Selection.activeObject is Shader shader)
        {
            var window = GetWindow<MaliCompilerWindow>();
            window.selectedShader = shader;
            var result = window.CompileShader();
            if (result.isSuccess)
            {
                unityCompiledVertexCode = result.vertexShader;
                unityCompiledFragmentCode = result.fragmentShader;
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Mali Compiler", "请先选择一个Shader文件", "确定");
        }
    }
    
    // 配置和状态
    private MaliCompilerConfig config;
    private Vector2 scrollPosition;
    private Vector2 resultScrollPosition;
    
    // UI样式
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle warningStyle;
    private GUIStyle successStyle;
    
    // 用户输入
    private Shader selectedShader;
    private int selectedGPUIndex = 4;
    private readonly string[] gpuModels = {
        "Mali-G71", "Mali-G72", "Mali-G76", "Mali-G77", "Mali-G78", 
        "Mali-G310", "Mali-G510", "Mali-G610", "Mali-G710", "Mali-G715"
    };
    
    // Unity编译后代码输入
    private static string unityCompiledVertexCode = "";
    private static string unityCompiledFragmentCode = "";
    
    // 编译状态
    private bool isCompiling = false;
    private string statusMessage = "";
    private string rawVertexResult = "";
    private string rawFragmentResult = "";
    
    // 分析结果
    private PerformanceMetrics vertexMetrics;
    private PerformanceMetrics fragmentMetrics;
    private List<OptimizationSuggestion> optimizationSuggestions;
    private string analysisReport = "";
    
    // 滚动位置
    private Vector2 analysisScrollPosition;
    
    // 折叠面板状态
    private bool showConfiguration = true;
    private bool showShaderSelection = true;
    private bool showAdvancedOptions = false;
    private bool showRawResults = false;
    private bool showAnalysis = true;
    private bool showOptimizationSuggestions = true;
    
    private void OnEnable()
    {
        unityCompiledVertexCode = string.Empty;
        unityCompiledFragmentCode = string.Empty;
        config = MaliCompilerConfig.Load();
        selectedGPUIndex = Array.IndexOf(gpuModels, config.selectedGPUModel);
        if (selectedGPUIndex < 0) selectedGPUIndex = 4;
        
        InitializeStyles();
    }
    
    private void InitializeStyles()
    {
        // 确保EditorStyles已经初始化
        if (EditorStyles.boldLabel == null)
            return;
            
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            margin = new RectOffset(0, 0, 10, 5)
        };
        
        sectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };
        
        warningStyle = new GUIStyle(EditorStyles.helpBox);
        successStyle = new GUIStyle(EditorStyles.helpBox);
    }
    
    private void OnGUI()
    {
        if (headerStyle == null) InitializeStyles();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawConfigurationSection();
        DrawShaderSelectionSection();
        DrawCompiledCodeInputSection();
        DrawAdvancedOptionsSection();
        DrawCompileSection();
        DrawResultsSection();
        
        EditorGUILayout.EndScrollView();
        
        // 处理拖拽
        HandleDragAndDrop();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Mali Offline Compiler Integration Pro", headerStyle);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("专业级Unity Shader性能分析工具", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("帮助", EditorStyles.miniButton, GUILayout.Width(40)))
        {
            ShowHelp();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }
    
    private void DrawConfigurationSection()
    {
        showConfiguration = EditorGUILayout.Foldout(showConfiguration, "🔧 配置设置", true);
        if (showConfiguration)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            
            // Mali Compiler路径
            EditorGUILayout.LabelField("Mali Compiler路径:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            config.compilerPath = EditorGUILayout.TextField(config.compilerPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择Mali Compiler", "", "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    config.compilerPath = path;
                    config.Save();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 验证配置
            string errorMessage;
            bool isValid = config.IsValid(out errorMessage);
            
            if (!isValid)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                if (string.IsNullOrEmpty(config.compilerPath))
                {
                    EditorGUILayout.HelpBox("Mali Offline Compiler下载地址: https://developer.arm.com/tools-and-software/graphics-and-gaming/arm-mobile-studio/components/mali-offline-compiler", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ 配置有效", MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
    }
    
    private void DrawShaderSelectionSection()
    {
        showShaderSelection = EditorGUILayout.Foldout(showShaderSelection, "📄 Shader选择", true);
        if (showShaderSelection)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            
            selectedShader = (Shader)EditorGUILayout.ObjectField("Shader", selectedShader, typeof(Shader), false);
            
            if (selectedShader != null)
            {
                string shaderPath = AssetDatabase.GetAssetPath(selectedShader);
                EditorGUILayout.LabelField("路径: " + shaderPath, EditorStyles.miniLabel);
                
                // GPU型号选择
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("GPU型号:", EditorStyles.boldLabel);
                config.useCustomGPU = EditorGUILayout.Toggle("指定GPU型号", config.useCustomGPU);
                
                if (config.useCustomGPU)
                {
                    selectedGPUIndex = EditorGUILayout.Popup("GPU型号", selectedGPUIndex, gpuModels);
                    config.selectedGPUModel = gpuModels[selectedGPUIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请选择要分析的Shader，或将Shader文件拖拽到此窗口。", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
    }
    
    private void DrawCompiledCodeInputSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("粘贴从Unity Inspector 'Compile and show code' > GLES3x 获得的代码", MessageType.Info);
            
        EditorGUILayout.LabelField("Vertex Shader代码:", EditorStyles.boldLabel);
        unityCompiledVertexCode = EditorGUILayout.TextArea(unityCompiledVertexCode, GUILayout.Height(120));
            
        EditorGUILayout.Space(5);
            
        EditorGUILayout.LabelField("Fragment Shader代码:", EditorStyles.boldLabel);
        unityCompiledFragmentCode = EditorGUILayout.TextArea(unityCompiledFragmentCode, GUILayout.Height(120));
            
        EditorGUILayout.Space(5);
            
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清除代码", EditorStyles.miniButton))
        {
            unityCompiledVertexCode = "";
            unityCompiledFragmentCode = "";
        }
            
        if (GUILayout.Button("验证代码", EditorStyles.miniButton))
        {
            ValidateCompiledCode();
        }
        EditorGUILayout.EndHorizontal();

        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void ValidateCompiledCode()
    {
        bool vertexValid = UnityShaderCompiler.IsValidForMaliAnalysis(unityCompiledVertexCode);
        bool fragmentValid = UnityShaderCompiler.IsValidForMaliAnalysis(unityCompiledFragmentCode);
        
        if (vertexValid && fragmentValid)
        {
            EditorUtility.DisplayDialog("验证成功", "编译后的代码格式正确，可以进行Mali分析。", "确定");
        }
        else
        {
            string message = "代码验证失败：\n";
            if (!vertexValid) message += "- Vertex Shader代码格式不正确\n";
            if (!fragmentValid) message += "- Fragment Shader代码格式不正确\n";
            message += "\n请确保代码来自Unity Inspector的'Compile and show code' > GLES3x";
            
            EditorUtility.DisplayDialog("验证失败", message, "确定");
        }
    }
    
    private void DrawAdvancedOptionsSection()
    {
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "⚙️ 高级选项", true);
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            
            config.enableVerboseOutput = EditorGUILayout.Toggle("详细输出", config.enableVerboseOutput);
            config.saveTemporaryFiles = EditorGUILayout.Toggle("保存临时文件", config.saveTemporaryFiles);
            
            if (config.saveTemporaryFiles)
            {
                EditorGUILayout.BeginHorizontal();
                config.temporaryFilesPath = EditorGUILayout.TextField("临时文件路径", config.temporaryFilesPath);
                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("选择临时文件目录", "", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        config.temporaryFilesPath = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            config.autoSaveResults = EditorGUILayout.Toggle("自动保存结果", config.autoSaveResults);
            config.showOptimizationHints = EditorGUILayout.Toggle("显示优化建议", config.showOptimizationHints);
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
    }
    
    private void DrawCompileSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        
        EditorGUILayout.BeginHorizontal();
        
        string errorMessage;
        bool canCompile = config.IsValid(out errorMessage) && selectedShader != null && !isCompiling;
        
        GUI.enabled = canCompile;
        if (GUILayout.Button("🚀 开始分析", GUILayout.Height(35)))
        {
            var result = CompileShader();
            if (result.isSuccess)
            {
                unityCompiledVertexCode = result.vertexShader;
                unityCompiledFragmentCode = result.fragmentShader;
            }
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("🗑️ 清除结果", GUILayout.Width(100), GUILayout.Height(35)))
        {
            ClearResults();
        }
        
        if (GUILayout.Button("💾 保存报告", GUILayout.Width(100), GUILayout.Height(35)))
        {
            SaveReport();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (isCompiling)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔄 " + statusMessage, EditorStyles.centeredGreyMiniLabel);
            
            Rect rect = GUILayoutUtility.GetRect(0, 2, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, -1, "");
        }
        else if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }
        
        if (!canCompile && selectedShader != null)
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    private void DrawResultsSection()
    {
        if (string.IsNullOrEmpty(rawVertexResult) && string.IsNullOrEmpty(rawFragmentResult))
        {
            EditorGUILayout.HelpBox("暂无编译结果。请选择Shader并开始分析。", MessageType.None);
            return;
        }
        
        resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition, GUILayout.Height(400));
        
        // 分析报告
        if (showAnalysis)
        {
            showAnalysis = EditorGUILayout.Foldout(showAnalysis, "📊 性能分析报告", true);
            if (showAnalysis && !string.IsNullOrEmpty(analysisReport))
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                
                // 使用类成员变量保存滚动位置
                analysisScrollPosition = EditorGUILayout.BeginScrollView(analysisScrollPosition, GUILayout.Height(200));
                GUILayout.TextArea(analysisReport, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        // 优化建议
        if (showOptimizationSuggestions && config.showOptimizationHints)
        {
            showOptimizationSuggestions = EditorGUILayout.Foldout(showOptimizationSuggestions, "💡 优化建议", true);
            if (showOptimizationSuggestions && optimizationSuggestions != null && optimizationSuggestions.Count > 0)
            {
                DrawOptimizationSuggestions();
            }
        }
        
        // 原始结果
        showRawResults = EditorGUILayout.Foldout(showRawResults, "📝 原始编译结果", true);
        if (showRawResults)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            
            EditorGUILayout.LabelField("Vertex Shader结果:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(rawVertexResult, EditorStyles.textArea, GUILayout.Height(150));
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Fragment Shader结果:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(rawFragmentResult, EditorStyles.textArea, GUILayout.Height(150));
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawOptimizationSuggestions()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        
        foreach (var suggestion in optimizationSuggestions)
        {
            Color backgroundColor = suggestion.Priority switch
            {
                Priority.Critical => new Color(1f, 0.3f, 0.3f, 0.3f),
                Priority.High => new Color(1f, 0.6f, 0.0f, 0.3f),
                Priority.Medium => new Color(1f, 1f, 0.0f, 0.3f),
                Priority.Low => new Color(0.3f, 1f, 0.3f, 0.3f),
                _ => Color.clear
            };
            
            var originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalBackgroundColor;
            
            string priorityIcon = suggestion.Priority switch
            {
                Priority.Critical => "🔴",
                Priority.High => "🟠", 
                Priority.Medium => "🟡",
                Priority.Low => "🟢",
                _ => "ℹ️"
            };
            
            EditorGUILayout.LabelField($"{priorityIcon} [{suggestion.Category}] {suggestion.Issue}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"建议: {suggestion.Suggestion}", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"预期效果: {suggestion.ExpectedImpact}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is Shader shader)
                    {
                        selectedShader = shader;
                        Repaint();
                        break;
                    }
                }
            }
        }
    }
    
    private ShaderCompileResult CompileShader()
    {
        if (selectedShader == null) 
            return new ShaderCompileResult();
        
        isCompiling = true;
        statusMessage = "正在解析Shader文件...";
        
        try
        {
            // 检查是否为URP着色器
            bool isURPShader = UnityShaderCompiler.IsURPShader(selectedShader);
            if (isURPShader)
            {
                statusMessage = "检测到URP着色器，使用特殊处理...";
            }
            
            var result = UnityShaderCompiler.CompileShaderForPlatform(selectedShader, UnityEditor.Rendering.ShaderCompilerPlatform.GLES3x);
            
            if (result.vertexShader == null || result.fragmentShader == null)
            {
                statusMessage = "无法解析Shader文件，请确保是标准的Unity Shader";
                return result;
            }
            
            // 创建临时文件
            string tempDir = GetTempDirectory();
            string vertexFile = Path.Combine(tempDir, "vertex.vert");
            string fragmentFile = Path.Combine(tempDir, "fragment.frag");
            
            File.WriteAllText(vertexFile, result.vertexShader);
            File.WriteAllText(fragmentFile, result.fragmentShader);
            
            // 编译
            statusMessage = "正在编译Vertex Shader...";
            rawVertexResult = CompileShaderFile(vertexFile, "Vertex");
            
            statusMessage = "正在编译Fragment Shader...";
            rawFragmentResult = CompileShaderFile(fragmentFile, "Fragment");
            
            // 分析结果
            statusMessage = "正在分析编译结果...";
            AnalyzeResults();
            
            statusMessage = "分析完成！";
            
            // 清理临时文件
            if (!config.saveTemporaryFiles)
            {
                try
                {
                    File.Delete(vertexFile);
                    File.Delete(fragmentFile);
                }
                catch { }
            }
            
            // 自动保存
            if (config.autoSaveResults)
            {
                AutoSaveResults();
            }

            return result;
        }
        catch (Exception e)
        {
            statusMessage = $"编译过程出现错误: {e.Message}";
            UnityEngine.Debug.LogError($"Mali Compiler错误: {e}");
        }
        finally
        {
            isCompiling = false;
            Repaint();
        }

        return new ShaderCompileResult();
    }
    
    private string GetTempDirectory()
    {
        if (config.saveTemporaryFiles && !string.IsNullOrEmpty(config.temporaryFilesPath))
        {
            return config.temporaryFilesPath;
        }
        
        string tempDir = Path.Combine(Application.temporaryCachePath, "MaliCompiler");
        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);
        return tempDir;
    }
    
    private string CompileShaderFile(string filePath, string shaderType)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = config.compilerPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            if (config.useCustomGPU)
            {
                startInfo.Arguments = $"\"{filePath}\" -c {config.selectedGPUModel}";
            }
            else
            {
                startInfo.Arguments = $"\"{filePath}\"";
            }
            
            if (config.enableVerboseOutput)
            {
                startInfo.Arguments += " -d";
            }
            
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit(30000);
                
                if (process.ExitCode == 0)
                {
                    return $"=== {shaderType} Shader 编译成功 ===\n\n{output}";
                }
                else
                {
                    return $"=== {shaderType} Shader 编译失败 ===\n\n错误: {error}\n\n输出: {output}";
                }
            }
        }
        catch (Exception e)
        {
            return $"=== {shaderType} Shader 编译异常 ===\n\n{e.Message}";
        }
    }
    
    private void AnalyzeResults()
    {
        vertexMetrics = MaliCompilerAnalyzer.AnalyzeCompileResult(rawVertexResult);
        fragmentMetrics = MaliCompilerAnalyzer.AnalyzeCompileResult(rawFragmentResult);
        optimizationSuggestions = MaliCompilerAnalyzer.GenerateOptimizationSuggestions(vertexMetrics, fragmentMetrics);
        analysisReport = MaliCompilerAnalyzer.FormatAnalysisReport(vertexMetrics, fragmentMetrics, optimizationSuggestions);
    }
    
    private void ClearResults()
    {
        rawVertexResult = "";
        rawFragmentResult = "";
        analysisReport = "";
        vertexMetrics = null;
        fragmentMetrics = null;
        optimizationSuggestions = null;
        statusMessage = "";
    }
    
    private void SaveReport()
    {
        if (string.IsNullOrEmpty(analysisReport)) return;
        
        string fileName = $"MaliAnalysis_{selectedShader.name}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = EditorUtility.SaveFilePanel("保存分析报告", "", fileName, "txt");
        
        if (!string.IsNullOrEmpty(path))
        {
            StringBuilder fullReport = new StringBuilder();
            fullReport.AppendLine($"Mali Compiler 分析报告");
            fullReport.AppendLine($"Shader: {selectedShader.name}");
            fullReport.AppendLine($"生成时间: {DateTime.Now}");
            fullReport.AppendLine($"GPU型号: {(config.useCustomGPU ? config.selectedGPUModel : "默认")}");
            fullReport.AppendLine();
            fullReport.AppendLine(analysisReport);
            fullReport.AppendLine();
            fullReport.AppendLine("=== 原始编译结果 ===");
            fullReport.AppendLine();
            fullReport.AppendLine("【Vertex Shader】");
            fullReport.AppendLine(rawVertexResult);
            fullReport.AppendLine();
            fullReport.AppendLine("【Fragment Shader】");
            fullReport.AppendLine(rawFragmentResult);
            
            File.WriteAllText(path, fullReport.ToString());
            EditorUtility.DisplayDialog("保存成功", $"报告已保存到: {path}", "确定");
        }
    }
    
    private void AutoSaveResults()
    {
        if (string.IsNullOrEmpty(analysisReport)) return;
        
        string autoSaveDir = Path.Combine(Application.persistentDataPath, "MaliCompilerReports");
        if (!Directory.Exists(autoSaveDir))
            Directory.CreateDirectory(autoSaveDir);
            
        string fileName = $"Auto_{selectedShader.name.Replace("/", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = Path.Combine(autoSaveDir, fileName);
        
        StringBuilder fullReport = new StringBuilder();
        fullReport.AppendLine($"Mali Compiler 自动保存报告");
        fullReport.AppendLine($"Shader: {selectedShader.name}");
        fullReport.AppendLine($"生成时间: {DateTime.Now}");
        fullReport.AppendLine();
        fullReport.AppendLine(analysisReport);
        
        File.WriteAllText(path, fullReport.ToString());
    }
    
    private void ShowHelp()
    {
        string helpText = @"Mali Compiler Integration Pro 使用指南

1. 配置设置:
   - 下载并安装Mali Offline Compiler
   - 设置malioc.exe的路径

2. Shader选择:
   - 选择要分析的.shader文件
   - 或直接拖拽shader到窗口中

3. 开始分析:
   - 点击""开始分析""按钮
   - 查看性能分析报告和优化建议

4. 结果解读:
   - 工作寄存器: 越低越好
   - 16位运算占比: 越高越好
   - 瓶颈单元: 显示性能瓶颈所在

5. 优化建议:
   - 根据颜色优先级进行优化
   - 红色为严重问题，需优先处理

下载Mali Compiler:
https://developer.arm.com/tools-and-software/graphics-and-gaming/arm-mobile-studio/components/mali-offline-compiler";

        EditorUtility.DisplayDialog("使用帮助", helpText, "确定");
    }
    
    private void OnDestroy()
    {
        config?.Save();
    }
}
