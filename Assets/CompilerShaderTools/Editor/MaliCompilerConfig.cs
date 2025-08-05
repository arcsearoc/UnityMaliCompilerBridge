using UnityEngine;
using System;

/// <summary>
/// Mali Compiler配置数据类
/// </summary>
[Serializable]
public class MaliCompilerConfig
{
    [Header("Mali Compiler设置")]
    public string compilerPath = "";
    public bool useCustomGPU = false;
    public string selectedGPUModel = "Mali-G78";
    
    [Header("编译选项")]
    public bool enableVerboseOutput = false;
    public bool saveTemporaryFiles = false;
    public string temporaryFilesPath = "";
    
    [Header("界面设置")]
    public bool autoSaveResults = true;
    public bool showOptimizationHints = true;
    public int maxResultDisplayLines = 1000;
    
    /// <summary>
    /// 从EditorPrefs加载配置
    /// </summary>
    public static MaliCompilerConfig Load()
    {
        MaliCompilerConfig config = new MaliCompilerConfig();
        
        config.compilerPath = UnityEditor.EditorPrefs.GetString("MaliCompiler_Path", "");
        config.useCustomGPU = UnityEditor.EditorPrefs.GetBool("MaliCompiler_UseCustomGPU", false);
        config.selectedGPUModel = UnityEditor.EditorPrefs.GetString("MaliCompiler_GPUModel", "Mali-G78");
        config.enableVerboseOutput = UnityEditor.EditorPrefs.GetBool("MaliCompiler_VerboseOutput", false);
        config.saveTemporaryFiles = UnityEditor.EditorPrefs.GetBool("MaliCompiler_SaveTempFiles", false);
        config.temporaryFilesPath = UnityEditor.EditorPrefs.GetString("MaliCompiler_TempPath", "");
        config.autoSaveResults = UnityEditor.EditorPrefs.GetBool("MaliCompiler_AutoSave", true);
        config.showOptimizationHints = UnityEditor.EditorPrefs.GetBool("MaliCompiler_ShowHints", true);
        config.maxResultDisplayLines = UnityEditor.EditorPrefs.GetInt("MaliCompiler_MaxLines", 1000);
        
        return config;
    }
    
    /// <summary>
    /// 保存配置到EditorPrefs
    /// </summary>
    public void Save()
    {
        UnityEditor.EditorPrefs.SetString("MaliCompiler_Path", compilerPath);
        UnityEditor.EditorPrefs.SetBool("MaliCompiler_UseCustomGPU", useCustomGPU);
        UnityEditor.EditorPrefs.SetString("MaliCompiler_GPUModel", selectedGPUModel);
        UnityEditor.EditorPrefs.SetBool("MaliCompiler_VerboseOutput", enableVerboseOutput);
        UnityEditor.EditorPrefs.SetBool("MaliCompiler_SaveTempFiles", saveTemporaryFiles);
        UnityEditor.EditorPrefs.SetString("MaliCompiler_TempPath", temporaryFilesPath);
        UnityEditor.EditorPrefs.SetBool("MaliCompiler_AutoSave", autoSaveResults);
        UnityEditor.EditorPrefs.SetBool("MaliCompiler_ShowHints", showOptimizationHints);
        UnityEditor.EditorPrefs.SetInt("MaliCompiler_MaxLines", maxResultDisplayLines);
    }
    
    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public void ResetToDefault()
    {
        compilerPath = "";
        useCustomGPU = false;
        selectedGPUModel = "Mali-G78";
        enableVerboseOutput = false;
        saveTemporaryFiles = false;
        temporaryFilesPath = "";
        autoSaveResults = true;
        showOptimizationHints = true;
        maxResultDisplayLines = 1000;
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid(out string errorMessage)
    {
        errorMessage = "";
        
        if (string.IsNullOrEmpty(compilerPath))
        {
            errorMessage = "Mali Compiler路径未设置";
            return false;
        }
        
        if (!System.IO.File.Exists(compilerPath))
        {
            errorMessage = "Mali Compiler文件不存在";
            return false;
        }
        
        if (saveTemporaryFiles && string.IsNullOrEmpty(temporaryFilesPath))
        {
            errorMessage = "启用保存临时文件但未设置路径";
            return false;
        }
        
        return true;
    }
}
