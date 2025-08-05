using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Mali Compiler工具安装和初始化脚本
/// </summary>
public class MaliCompilerInstaller : EditorWindow
{
    [MenuItem("Tools/Mali Compiler Integration/Setup Wizard")]
    public static void ShowSetupWizard()
    {
        var window = GetWindow<MaliCompilerInstaller>("Mali Compiler Setup");
        window.minSize = new Vector2(500, 400);
    }
    
    private string maliCompilerPath = "";
    private bool hasValidPath = false;
    private Vector2 scrollPosition;
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Mali Compiler Integration - 安装向导", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 步骤1：Mali Compiler路径设置
        EditorGUILayout.LabelField("步骤 1: 设置Mali Offline Compiler路径", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("请先下载并安装ARM Mali Offline Compiler，然后设置malioc.exe的路径。", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        maliCompilerPath = EditorGUILayout.TextField("Mali Compiler路径:", maliCompilerPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择Mali Compiler (malioc.exe)", "", "exe");
            if (!string.IsNullOrEmpty(path))
            {
                maliCompilerPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 验证路径
        hasValidPath = !string.IsNullOrEmpty(maliCompilerPath) && File.Exists(maliCompilerPath);
        
        if (!string.IsNullOrEmpty(maliCompilerPath))
        {
            if (hasValidPath)
            {
                EditorGUILayout.HelpBox("✓ Mali Compiler路径有效", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ 文件不存在，请检查路径", MessageType.Error);
            }
        }
        
        EditorGUILayout.Space();
        
        // 下载链接
        EditorGUILayout.LabelField("Mali Offline Compiler下载地址:", EditorStyles.boldLabel);
        if (GUILayout.Button("打开ARM官方下载页面", GUILayout.Height(25)))
        {
            Application.OpenURL("https://developer.arm.com/tools-and-software/graphics-and-gaming/arm-mobile-studio/components/mali-offline-compiler");
        }
        
        EditorGUILayout.Space();
        
        // 步骤2：创建配置
        EditorGUILayout.LabelField("步骤 2: 初始化配置", EditorStyles.boldLabel);
        
        GUI.enabled = hasValidPath;
        if (GUILayout.Button("保存配置并完成安装", GUILayout.Height(30)))
        {
            SetupConfiguration();
            ShowCompletionDialog();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 使用说明
        EditorGUILayout.LabelField("使用说明:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(@"安装完成后，你可以通过以下方式使用Mali Compiler工具：

1. 主窗口分析：
   Tools > Mali Compiler Integration > Main Window

2. 快速分析：
   选择Shader文件 > Tools > Mali Compiler Integration > Quick Compile

3. 拖拽分析：
   将Shader文件拖拽到Mali Compiler窗口中

工具功能：
• 自动解析Unity Shader
• 调用Mali Compiler进行性能分析
• 生成详细的性能报告和优化建议
• 支持多种Mali GPU型号
• 智能HLSL到GLSL转换", EditorStyles.wordWrappedLabel, GUILayout.Height(150));
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SetupConfiguration()
    {
        var config = new MaliCompilerConfig();
        config.compilerPath = maliCompilerPath;
        config.useCustomGPU = false;
        config.selectedGPUModel = "Mali-G78";
        config.enableVerboseOutput = false;
        config.saveTemporaryFiles = false;
        config.autoSaveResults = true;
        config.showOptimizationHints = true;
        config.maxResultDisplayLines = 1000;
        
        config.Save();
        
        // 创建示例目录
        string exampleDir = Path.Combine(Application.dataPath, "MaliCompilerReports");
        if (!Directory.Exists(exampleDir))
        {
            Directory.CreateDirectory(exampleDir);
        }
    }
    
    private void ShowCompletionDialog()
    {
        bool openMainWindow = EditorUtility.DisplayDialog(
            "安装完成",
            "Mali Compiler Integration工具安装成功！\n\n配置已保存，现在可以开始使用工具分析Shader性能。\n\n是否立即打开主窗口？",
            "打开主窗口",
            "稍后使用"
        );
        
        if (openMainWindow)
        {
            MaliCompilerWindow.ShowWindow();
        }
        
        Close();
    }
}

/// <summary>
/// 菜单项管理
/// </summary>
public static class MaliCompilerMenuItems
{
    [MenuItem("Tools/Mali Compiler Integration/Documentation")]
    public static void OpenDocumentation()
    {
        string docPath = Path.Combine(Application.dataPath, "CompilerShaderTools", "Mali_Compiler_Unity_Tool_Guide.md");
        if (File.Exists(docPath))
        {
            Application.OpenURL("file:///" + docPath.Replace("\\", "/"));
        }
        else
        {
            EditorUtility.DisplayDialog("文档未找到", "使用文档未找到，请确保Mali_Compiler_Unity_Tool_Guide.md存在于Assets/CompilerShaderTools/ 目录中。", "确定");
        }
    }
    
    [MenuItem("Tools/Mali Compiler Integration/Reset Configuration")]
    public static void ResetConfiguration()
    {
        if (EditorUtility.DisplayDialog("重置配置", "确定要重置Mali Compiler配置吗？这将清除所有设置。", "确定", "取消"))
        {
            var config = new MaliCompilerConfig();
            config.ResetToDefault();
            config.Save();
            
            EditorUtility.DisplayDialog("配置重置", "配置已重置为默认值。", "确定");
        }
    }
    
    [MenuItem("Tools/Mali Compiler Integration/Open Reports Folder")]
    public static void OpenReportsFolder()
    {
        string reportsPath = Path.Combine(Application.persistentDataPath, "MaliCompilerReports");
        if (!Directory.Exists(reportsPath))
        {
            Directory.CreateDirectory(reportsPath);
        }
        
        EditorUtility.RevealInFinder(reportsPath);
    }
    
    [MenuItem("Tools/Mali Compiler Integration/About")]
    public static void ShowAbout()
    {
        EditorUtility.DisplayDialog("关于Mali Compiler Integration", 
            "Mali Compiler Integration for Unity\n" +
            "版本: 1.0\n" +
            "作者: MiniMax Agent\n" +
            "更新时间: 2025-08-05\n\n" +
            "专业级Unity Shader性能分析工具，集成ARM Mali Offline Compiler，" +
            "提供详细的性能指标分析和智能优化建议。\n\n" +
            "支持Mali-G71到Mali-G715等多种GPU型号，" +
            "帮助开发者优化移动游戏的渲染性能。", 
            "确定");
    }
}
