# Unity Mali Compiler Integration Tool User Guide

**[中文](README_CN.md) | English**

**Version: 1.2.0**  
**Updated: 2025-08-28**

## 📋 Overview

Unity Mali Compiler Integration Tool is a professional Unity Editor extension that allows developers to directly invoke ARM Mali Offline Compiler within Unity to analyze Shader performance, providing detailed performance metrics and optimization recommendations.

### 🎯 Key Features

- **One-Click Shader Analysis** - Compile and analyze Shaders directly in Unity
- **Smart Code Parsing** - Automatically convert Unity Shaders to GLSL format for Mali Compiler
- **Detailed Performance Reports** - Provide key metrics including work registers, instruction cycles, bottleneck analysis
- **Intelligent Optimization Suggestions** - Automatically generate optimization recommendations based on compilation results
- **Multi-GPU Support** - Support various GPU models from Mali-G71 to Mali-G715
- **Batch Processing** - Support result saving, automatic report generation, etc.

## 🚀 Quick Start

### 1. Environment Setup

#### Download Mali Offline Compiler (Also included in the project)

1. Visit ARM official website: https://developer.arm.com/tools-and-software/graphics-and-gaming/arm-mobile-studio/components/mali-offline-compiler
2. Download the version corresponding to your operating system
3. After installation, record the path to `malioc.exe`

#### Install Unity Tool

1. Copy all `.cs` files from the tool package to the `Editor` folder in your Unity project
2. Wait for Unity to complete compilation
3. Find `Tools > Mali Compiler Integration` in the menu bar

### 2. Tool Configuration

#### Basic Configuration

1. Open `Tools > Mali Compiler Integration > Main Window`
2. Click the "Browse" button in "Configuration Settings"
3. Select the `malioc.exe` file of Mali Offline Compiler
4. Ready to use after successful configuration verification

#### Advanced Configuration (Optional)

- **Verbose Output**: Enable for more detailed compilation information
- **Save Temporary Files**: For debugging, save intermediate converted GLSL files
- **Auto Save Results**: Automatically save analysis reports locally
- **Show Optimization Suggestions**: Enable intelligent optimization suggestion feature

### 3. Usage Workflow

#### Method 1: Main Window Analysis

1. Open Mali Compiler main window
2. Select the Shader file to analyze
3. (Optional) Specify a particular GPU model
4. Click "🚀 Start Analysis"
5. View performance analysis report and optimization suggestions

#### Method 2: Quick Analysis

1. Select Shader file in Project window
2. Right-click and select `Tools > Mali Compiler Integration > Quick Compile`
3. Or use menu bar `Tools > Mali Compiler Integration > Quick Compile`

#### Method 3: Drag & Drop Analysis

1. Open Mali Compiler window
2. Directly drag Shader file into the window
3. Automatically start analysis process

## 📊 Result Interpretation

### Performance Metrics Explanation

#### Work Registers

- **Meaning**: Number of registers used during Shader execution
- **Optimization Goal**: Lower is better (recommended ≤32)
- **Impact**: Excessive numbers limit the number of threads that can execute in parallel

#### Uniform Registers

- **Meaning**: Read-only registers storing constant data
- **Characteristics**: Shared among all threads, relatively minor impact

#### 16-bit Arithmetic Ratio

- **Meaning**: Percentage of 16-bit precision operations used
- **Optimization Goal**: Higher is better (recommended ≥50%)
- **Impact**: 16-bit operations are twice as fast as 32-bit operations

#### Instruction Cycles

- **Total Cycles**: Cumulative cycles of all instructions
- **Shortest Path**: Cycles for optimized execution path
- **Longest Path**: Cycles for most complex execution path

#### Bound Unit

- **A (Arithmetic)**: Arithmetic operation bottleneck
- **T (Texture)**: Texture sampling bottleneck
- **LS (Load/Store)**: Memory read/write bottleneck
- **V (Varying)**: Interpolation calculation bottleneck

### Shader Property Analysis

#### Performance Impact Properties

- **Has uniform computation**: Contains uniform computation, recommend moving to CPU
- **Uses late ZS test**: Uses late depth testing, affects Early-Z optimization
- **Has side-effects**: Has side effects, affects parallel execution
- **Stack spilling**: Register overflow, serious performance issue

## 🔧 Optimization Guide

### Priority Color Coding

- 🔴 **Critical**: Performance issues that must be addressed immediately
- 🟠 **High**: Significantly affects performance, recommend priority optimization
- 🟡 **Medium**: Some impact, moderate optimization recommended
- 🟢 **Low**: Minor impact, optimize when time permits

### Common Optimization Strategies

#### 1. Register Optimization

```hlsl
// ❌ Avoid: Too many local variables
float temp1 = calcA();
float temp2 = calcB();
float temp3 = calcC();
float result = temp1 * temp2 + temp3;

// ✅ Recommended: Merge calculations
float result = calcA() * calcB() + calcC();
```

#### 2. Precision Optimization

```hlsl
// ❌ Avoid: Unnecessary high precision
float4 color = tex2D(_MainTex, uv);
float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114));

// ✅ Recommended: Use appropriate precision
half4 color = tex2D(_MainTex, uv);
half brightness = dot(color.rgb, half3(0.299, 0.587, 0.114));
```

#### 3. Branch Optimization

```hlsl
// ❌ Avoid: Complex branches
if (condition1) {
    if (condition2) {
        // Complex calculation
    }
}

// ✅ Recommended: Use lerp or step
float factor = step(0.5, condition1) * step(0.5, condition2);
result = lerp(defaultValue, complexValue, factor);
```

#### 4. Texture Optimization

```hlsl
// ❌ Avoid: Too many texture samples
float4 tex1 = tex2D(_Tex1, uv);
float4 tex2 = tex2D(_Tex2, uv);
float4 tex3 = tex2D(_Tex3, uv);
float4 tex4 = tex2D(_Tex4, uv);

// ✅ Recommended: Texture packing or reduced sampling
float4 packedTex = tex2D(_PackedTex, uv);
float4 tex1 = packedTex.rrra;
float4 tex2 = packedTex.ggga;
```

## 🎮 GPU Model Comparison

### Bifrost Architecture (Mali-G71, G72, G76)

- **Characteristics**: Traditional separate processing units
- **Optimization Focus**: Arithmetic operation optimization
- **Display Info**: Arithmetic unit statistics

### Valhall Architecture (Mali-G77, G78, G310, G510, G610, G710, G715)

- **Characteristics**: Parallel processing engine with more detailed unit statistics
- **Optimization Focus**: Balance FMA, CVT, SFU units
- **Display Info**: Detailed unit breakdown

## 🔍 Troubleshooting

### Common Issues

#### 1. "Mali Compiler file does not exist"

- **Cause**: Incorrect malioc.exe path configuration
- **Solution**: Re-download and install Mali Offline Compiler, confirm correct path

#### 2. "Cannot parse Shader file"

- **Cause**: Unsupported Shader format or complex Surface Shader
- **Solution**: Use standard Unity Shader format, avoid overly complex macro definitions

#### 3. "Compilation failed"

- **Cause**: HLSL to GLSL conversion error
- **Solution**: Check Shader syntax, avoid using unsupported functions

#### 4. Stack Spilling Warning

- **Cause**: Excessive register usage
- **Solution**: Reduce local variables, lower precision, simplify calculation logic

### Debugging Tips

1. **Enable Verbose Output**: Turn on verbose output mode in advanced options
2. **Save Temporary Files**: View converted GLSL code to locate conversion issues
3. **Gradual Simplification**: Start with simple Shaders, gradually increase complexity

## 📈 Performance Benchmarks

### Recommended Performance Targets

| Metric                  | Mobile Target | High-end Target |
| ----------------------- | ------------- | --------------- |
| Work Registers          | ≤16           | ≤32             |
| 16-bit Arithmetic Ratio | ≥60%          | ≥40%            |
| Texture Samples         | ≤2            | ≤4              |
| Longest Path Cycles     | ≤50           | ≤100            |

### GPU Performance Tiers

| GPU Model     | Performance Tier | Use Case                         |
| ------------- | ---------------- | -------------------------------- |
| Mali-G71/G72  | Entry-level      | Simple games, basic effects      |
| Mali-G76/G77  | Mid-range        | Medium complexity games          |
| Mali-G78/G310 | Mid-high-end     | Complex games, rich effects      |
| Mali-G510+    | High-end         | Top-tier games, advanced effects |

## 📚 Extended Resources

### ARM Official Documentation

- [Mali GPU Architecture Guide](https://developer.arm.com/documentation/102849/latest/)
- [Mali Offline Compiler User Guide](https://developer.arm.com/documentation/101863/latest/)
- [Mali GPU Best Practices](https://developer.arm.com/documentation/101897/latest/)

### Unity Optimization Resources

- [Unity Shader Optimization Guide](https://docs.unity3d.com/Manual/SL-ShaderPerformance.html)
- [Mobile Graphics Optimization](https://docs.unity3d.com/Manual/MobileOptimisation.html)

## 🤝 Technical Support

If you encounter issues during use, we recommend:

1. Check detailed error information in Unity Console
2. Enable "Verbose Output" mode for more information
3. Check Mali Offline Compiler version compatibility
4. Ensure Shader format meets tool requirements

---

**Note**: This tool is based on ARM Mali Offline Compiler and requires a valid Mali Compiler installation to work properly. The tool automatically handles most HLSL to GLSL conversions, but complex Shaders may require manual adjustments.
