# Unity Mali Compiler 集成工具使用指南
 
**版本：1.0**  
**更新时间：2025-08-05**

## 📋 概述

Unity Mali Compiler集成工具是一个专业级的Unity编辑器扩展，允许开发者直接在Unity中调用ARM Mali Offline Compiler来分析Shader性能，获得详细的性能指标和优化建议。

### 🎯 主要功能

- **一键Shader分析** - 直接在Unity中编译和分析Shader
- **智能代码解析** - 自动将Unity Shader转换为Mali Compiler可处理的GLSL格式
- **详细性能报告** - 提供工作寄存器、指令周期、瓶颈分析等关键指标
- **智能优化建议** - 基于编译结果自动生成优化建议
- **多GPU支持** - 支持Mali-G71到Mali-G715等多种GPU型号
- **批量处理** - 支持保存结果、自动报告生成等

## 🚀 快速开始

### 1. 环境准备

#### 下载Mali Offline Compiler
1. 访问ARM官网：https://developer.arm.com/tools-and-software/graphics-and-gaming/arm-mobile-studio/components/mali-offline-compiler
2. 根据你的操作系统下载对应版本
3. 安装完成后记录`malioc.exe`的路径

#### 安装Unity工具
1. 将工具包中的所有`.cs`文件复制到Unity项目的`Editor`文件夹中
2. 等待Unity编译完成
3. 在菜单栏找到`Tools > Mali Compiler Integration`

### 2. 工具配置

#### 基础配置
1. 打开`Tools > Mali Compiler Integration > Main Window`
2. 在"配置设置"中点击"浏览"按钮
3. 选择Mali Offline Compiler的`malioc.exe`文件
4. 配置验证成功后即可使用

#### 高级配置（可选）
- **详细输出**：启用后获得更详细的编译信息
- **保存临时文件**：用于调试，保存中间转换的GLSL文件
- **自动保存结果**：自动保存分析报告到本地
- **显示优化建议**：开启智能优化建议功能

### 3. 使用流程

#### 方法一：主窗口分析
1. 打开Mali Compiler主窗口
2. 选择要分析的Shader文件
3. （可选）指定特定的GPU型号
4. 点击"🚀 开始分析"
5. 查看性能分析报告和优化建议

#### 方法二：快速分析
1. 在Project窗口中选择Shader文件
2. 右键选择`Tools > Mali Compiler Integration > Quick Compile`
3. 或使用菜单栏`Tools > Mali Compiler Integration > Quick Compile`

#### 方法三：拖拽分析
1. 打开Mali Compiler窗口
2. 直接将Shader文件拖拽到窗口中
3. 自动开始分析流程

## 📊 结果解读

### 性能指标说明

#### 工作寄存器（Work Registers）
- **含义**：Shader执行时使用的寄存器数量
- **优化目标**：越低越好（建议≤32）
- **影响**：数量过高会限制并行执行的线程数

#### Uniform寄存器（Uniform Registers）
- **含义**：存储常量数据的只读寄存器
- **特点**：在所有线程间共享，影响相对较小

#### 16位运算占比（16-bit Arithmetic）
- **含义**：使用16位精度运算的百分比
- **优化目标**：越高越好（建议≥50%）
- **影响**：16位运算比32位运算快一倍

#### 指令周期（Instruction Cycles）
- **总周期数**：所有指令的累积周期
- **最短路径**：最优化执行路径的周期数
- **最长路径**：最复杂执行路径的周期数

#### 瓶颈单元（Bound Unit）
- **A (Arithmetic)**：算术运算瓶颈
- **T (Texture)**：纹理采样瓶颈
- **LS (Load/Store)**：内存读写瓶颈
- **V (Varying)**：插值计算瓶颈

### Shader属性分析

#### 性能影响属性
- **Has uniform computation**：包含统一计算，建议移至CPU
- **Uses late ZS test**：使用延迟深度测试，影响Early-Z优化
- **Has side-effects**：具有副作用，影响并行执行
- **Stack spilling**：寄存器溢出，严重性能问题

## 🔧 优化建议指南

### 优先级颜色说明
- 🔴 **严重**：必须立即处理的性能问题
- 🟠 **高**：显著影响性能，建议优先优化
- 🟡 **中**：有一定影响，可适当优化
- 🟢 **低**：轻微影响，时间充裕时优化

### 常见优化策略

#### 1. 寄存器优化
```hlsl
// ❌ 避免：过多局部变量
float temp1 = calcA();
float temp2 = calcB();
float temp3 = calcC();
float result = temp1 * temp2 + temp3;

// ✅ 推荐：合并计算
float result = calcA() * calcB() + calcC();
```

#### 2. 精度优化
```hlsl
// ❌ 避免：不必要的高精度
float4 color = tex2D(_MainTex, uv);
float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114));

// ✅ 推荐：使用适当精度
half4 color = tex2D(_MainTex, uv);
half brightness = dot(color.rgb, half3(0.299, 0.587, 0.114));
```

#### 3. 分支优化
```hlsl
// ❌ 避免：复杂分支
if (condition1) {
    if (condition2) {
        // 复杂计算
    }
}

// ✅ 推荐：使用lerp或step
float factor = step(0.5, condition1) * step(0.5, condition2);
result = lerp(defaultValue, complexValue, factor);
```

#### 4. 纹理优化
```hlsl
// ❌ 避免：过多纹理采样
float4 tex1 = tex2D(_Tex1, uv);
float4 tex2 = tex2D(_Tex2, uv);
float4 tex3 = tex2D(_Tex3, uv);
float4 tex4 = tex2D(_Tex4, uv);

// ✅ 推荐：纹理合并或减少采样
float4 packedTex = tex2D(_PackedTex, uv);
float4 tex1 = packedTex.rrra;
float4 tex2 = packedTex.ggga;
```

## 🎮 GPU型号对比

### Bifrost架构 (Mali-G71, G72, G76)
- **特点**：传统的分离式处理单元
- **优化重点**：算术运算优化
- **显示信息**：Arithmetic单元统计

### Valhall架构 (Mali-G77, G78, G310, G510, G610, G710, G715)
- **特点**：并行处理引擎，更细分的单元统计
- **优化重点**：FMA、CVT、SFU单元平衡
- **显示信息**：详细的单元分解

## 📁 文件结构说明

```
Editor/
├── MaliCompilerWindow.cs          # 基础编辑器窗口
├── MaliCompilerWindowV2.cs        # 高级编辑器窗口（推荐）
├── MaliCompilerConfig.cs          # 配置管理系统
├── MaliCompilerAnalyzer.cs        # 结果分析器
└── ShaderParser.cs                # Unity Shader解析器
```

### 各文件功能
- **MaliCompilerWindowV2.cs**：主要的用户界面，建议使用此版本
- **MaliCompilerAnalyzer.cs**：智能分析编译结果，生成优化建议
- **ShaderParser.cs**：将Unity Shader转换为Mali Compiler可处理的GLSL
- **MaliCompilerConfig.cs**：管理工具配置和设置

## 🔍 故障排除

### 常见问题

#### 1. "Mali Compiler文件不存在"
- **原因**：malioc.exe路径配置错误
- **解决**：重新下载并安装Mali Offline Compiler，确认路径正确

#### 2. "无法解析Shader文件"
- **原因**：不支持的Shader格式或复杂的Surface Shader
- **解决**：使用标准的Unity Shader格式，避免过于复杂的宏定义

#### 3. "编译失败"
- **原因**：HLSL到GLSL转换出错
- **解决**：检查Shader语法，避免使用不支持的函数

#### 4. Stack Spilling警告
- **原因**：寄存器使用过多
- **解决**：减少局部变量，降低精度，简化计算逻辑

### 调试技巧

1. **启用详细输出**：在高级选项中开启详细输出模式
2. **保存临时文件**：查看转换后的GLSL代码，定位转换问题
3. **逐步简化**：从简单Shader开始，逐步增加复杂度

## 📈 性能基准

### 推荐性能目标

| 指标 | 移动设备目标 | 高端设备目标 |
|------|-------------|-------------|
| 工作寄存器 | ≤16 | ≤32 |
| 16位运算占比 | ≥60% | ≥40% |
| 纹理采样数 | ≤2 | ≤4 |
| 最长路径周期 | ≤50 | ≤100 |

### GPU性能层级

| GPU型号 | 性能层级 | 适用场景 |
|---------|----------|----------|
| Mali-G71/G72 | 入门级 | 简单游戏，基础效果 |
| Mali-G76/G77 | 中端 | 中等复杂度游戏 |
| Mali-G78/G310 | 中高端 | 复杂游戏，丰富效果 |
| Mali-G510+ | 高端 | 顶级游戏，高级效果 |

## 📚 扩展资源

### ARM官方文档
- [Mali GPU架构指南](https://developer.arm.com/documentation/102849/latest/)
- [Mali Offline Compiler用户指南](https://developer.arm.com/documentation/101863/latest/)
- [Mali GPU最佳实践](https://developer.arm.com/documentation/101897/latest/)

### Unity优化资源
- [Unity Shader优化指南](https://docs.unity3d.com/Manual/SL-ShaderPerformance.html)
- [移动平台图形优化](https://docs.unity3d.com/Manual/MobileOptimisation.html)

## 🤝 技术支持

如果在使用过程中遇到问题，建议：

1. 查看Unity Console中的详细错误信息
2. 启用"详细输出"模式获取更多信息
3. 检查Mali Offline Compiler版本兼容性
4. 确认Shader格式符合工具要求

---

**注意**：本工具基于ARM Mali Offline Compiler，需要有效的Mali Compiler安装才能正常工作。工具会自动处理大部分HLSL到GLSL的转换，但复杂的Shader可能需要手动调整。
