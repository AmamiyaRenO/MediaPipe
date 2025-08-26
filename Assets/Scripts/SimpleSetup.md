# 简单情感检测设置指南

## 📦 基于您的参考代码

这个版本几乎完全基于您提供的参考代码，只是增加了实时音频输入功能。

## 🔧 设置步骤

### 1. 准备模型
- 确保 `model.onnx` 在 `StreamingAssets` 文件夹中
- 右键点击 `model.onnx` → `Create` → `Sentis` → `Model Asset`
- 生成 `model.asset` 文件

### 2. 创建GameObject
- 在场景中创建空GameObject
- 添加 `EmotionONNX` 组件

### 3. 配置组件
- 将 `model.asset` 拖拽到 `Model Asset` 字段
- 确保 `Use Real Time Audio` 已勾选

### 4. 运行测试
- 播放场景
- 查看控制台输出：`Emotion: happy (85.2%)`

## 🎯 功能说明

**完全基于您的参考代码：**
- 使用 `ModelAsset` 加载模型
- 使用 `TensorFloat` 和 `Softmax`
- 相同的情感标签顺序
- 相同的推理流程

**唯一增加的功能：**
- 从 `VoiceProcessor` 获取实时音频
- 简单的音频缓冲区管理
- 定时分析（1秒间隔）

## ✅ 预期结果

控制台应该显示：
```
Connected to VoiceProcessor
Emotion: neutral (76.3%)
Emotion: happy (82.1%)
Emotion: calm (74.5%)
```

这个版本应该没有编译错误，因为它基本上就是您提供的工作代码！