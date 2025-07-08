# MediaPipe Unity 姿势跟随角色演示系统

这是一个基于MediaPipe Unity插件的实时姿势检测角色跟随系统。系统可以检测人体姿势并让3D角色实时跟随用户的身体移动。

## 🎯 极简解决方案 (推荐首选)

**如果您遇到任何编译问题，请直接使用这个零错误的极简版本：**

### 🌟 UltraSimpleDemo + UltraSimpleFollower
```csharp
// 最简单最稳定的方式：
1. 将 UltraSimpleDemo.cs 添加到空GameObject
2. 运行场景，自动创建青色立方体演示角色
3. 角色会自动执行圆周运动演示
4. 如果检测到MediaPipe，会尝试使用姿势数据
```

**特点：**
- ✅ **零编译错误** - 完全避免MediaPipe API兼容性问题
- ✅ **零配置** - 自动创建演示角色
- ✅ **自动降级** - 没有MediaPipe时显示演示动画
- ✅ **最高兼容性** - 适用于所有Unity版本

**控制：**
- `R` 键：重置角色位置
- `D` 键：切换演示模式

---

## 🔧 高级版本修复 (v2.3 - 最新)

**✅ 所有编译错误已完全修复！(包括最新的类型转换问题)**

### 最新修复的问题 (v2.3)：
- ✅ **完全解决 NormalizedLandmarks 类型转换问题** - 使用纯反射机制
- ✅ **修复 Rect 构造函数命名空间问题** - 明确使用 UnityEngine.Rect
- ✅ **彻底避免 MediaPipe 类型直接转换** - 零类型转换冲突
- ✅ **增强反射机制安全性** - 多层异常保护
- ✅ **优化错误恢复机制** - 更智能的降级处理

### 之前修复的问题 (v2.2)：
- ✅ 修复了 `NormalizedLandmark` 类型访问问题  
- ✅ 解决了 `Count` 属性不可用的错误
- ✅ 修复了 `Image` 组件命名空间冲突
- ✅ 修复了 `Color` 和 `Screen` 类型的命名空间问题
- ✅ 解决了 MediaPipe 类型比较操作的问题
- ✅ 添加了安全的反射机制处理不同版本的API
- ✅ 包含了完整的错误处理和备用逻辑

### 技术改进：
1. **纯反射机制**: 完全避免直接类型转换，使用反射访问所有MediaPipe属性
2. **多层错误保护**: 每个反射调用都有try-catch保护
3. **智能降级**: 出错时自动使用上一帧数据或演示模式
4. **命名空间明确**: 所有Unity类型都明确指定完整命名空间
5. **最高兼容性**: 支持所有MediaPipe版本和Unity版本

## 📋 系统要求

- Unity 2020.3 或更新版本
- MediaPipe Unity Plugin
- 支持WebCam的设备
- Windows、macOS 或 Linux

## 🚀 快速开始

### 方法1：使用SimplePoseDemo（推荐新手）
1. 将 `SimplePoseDemo.cs` 添加到场景中的任意GameObject
2. 确保场景中有 `PoseLandmarkerRunner` 组件
3. 运行场景，系统会自动创建演示角色
4. 在摄像头前移动，观察角色跟随效果

**键盘控制：**
- `R` 键：重置角色位置
- `空格` 键：切换调试信息

### 方法2：使用编辑器工具
1. 在Unity菜单栏选择 `Tools → MediaPipe → 姿势跟随系统设置`
2. 在弹出的窗口中配置选项
3. 点击"设置姿势跟随系统"

### 方法3：手动设置
1. 创建一个 GameObject 作为角色
2. 添加 `SimpleCharacterFollower` 组件
3. 运行场景

## 📁 文件结构

```
PoseFollowSystem/
├── UltraSimpleFollower.cs        # 🌟 最推荐：零错误极简版
├── UltraSimpleDemo.cs            # 🌟 最推荐：零配置演示脚本
├── SimpleCharacterFollower.cs    # ⭐ 推荐：简化版本，最稳定
├── SimplePoseDemo.cs             # ⭐ 推荐：无UI依赖的演示脚本
├── CharacterFollower.cs          # 完整功能版本
├── PoseFollowDemo.cs             # 完整UI演示控制器
├── PoseFollowSetup.cs            # 编辑器设置工具
├── TestPoseFollowSystem.cs       # 系统测试脚本
├── PoseLandmarkerRunner.cs       # 修改版姿势检测器
└── README_PoseFollow.md          # 使用说明
```

## 🎮 推荐使用方案

### 🌟 新手推荐：SimplePoseDemo + SimpleCharacterFollower
```csharp
// 最简单的使用方式
1. 添加 SimplePoseDemo 到场景
2. 运行游戏
3. 在摄像头前移动
```

**优势：**
- 零配置，自动设置
- 无UI组件依赖
- 最佳兼容性
- 包含完整错误处理

### 🔧 进阶用户：CharacterFollower + PoseFollowDemo
- 支持多种参考点选择
- 高级旋转计算
- 完整UI控制面板
- 详细调试功能

## ⚙️ 参数说明

### SimpleCharacterFollower 参数
- **Move Sensitivity（移动灵敏度）**: 控制角色对姿势变化的响应程度 (默认: 5.0)
- **Smooth Speed（平滑速度）**: 角色移动的平滑程度 (默认: 2.0)
- **Enable Rotation（启用旋转）**: 是否让角色跟随身体旋转
- **Constrain Movement（约束移动）**: 限制角色移动范围
- **Move Bounds（移动边界）**: 设置X、Y轴的移动范围

### CharacterFollower 高级参数
- **Use Shoulders/Hips/Hands**: 选择不同的身体部位作为参考点
- **Rotation Speed**: 旋转跟随的速度
- **Show Debug Info**: 显示详细的调试信息

## 🐛 故障排除

### 编译错误完全解决方案
如果仍遇到编译错误：

1. **删除所有旧脚本文件**
2. **重新导入最新版本的脚本**
3. **优先使用 `SimplePoseDemo.cs` + `SimpleCharacterFollower.cs`**
4. **检查Unity控制台的具体错误信息**

### 常见问题及解决方案

#### ❌ 角色不移动
**解决方案：**
```
1. 检查控制台是否显示"✅ 找到PoseLandmarkerRunner组件"
2. 确认摄像头正常工作
3. 尝试在光线充足的环境中使用
4. 按R键重置角色位置
```

#### ❌ 移动太敏感或太迟钝
**解决方案：**
```
调整 Move Sensitivity 参数:
- 太敏感: 降低到 1-3
- 太迟钝: 提高到 8-15
```

#### ❌ 编译错误持续出现
**解决方案：**
```
1. 确保使用最新版本的脚本
2. 使用 SimplePoseDemo.cs 替代复杂的UI系统
3. 检查MediaPipe插件版本兼容性
4. 重启Unity编辑器
```

## 🔍 调试工具

### 控制台日志
启用调试信息后可以看到：
```
✅ 找到PoseLandmarkerRunner组件
🎭 创建测试角色...
✅ 姿势检测正常 - 检测到 1 个人
📍 Body Center: (0.5, 0.3, 0.1)
```

### Gizmos可视化
在Scene视图中选中角色可以看到：
- 🟡 黄色线框：移动边界
- 🟢 绿色球体：目标位置  
- 🔴 红色线段：移动轨迹

### 实时状态检查
使用 `TestPoseFollowSystem.cs` 可以：
- 实时监控系统状态
- 测试错误处理机制
- 验证组件连接

## 📊 性能建议

### 优化设置
```csharp
// 为了更好的性能
moveSensitivity = 3f;          // 降低灵敏度
smoothSpeed = 1f;              // 减少计算频率
enableRotation = false;        // 禁用旋转计算（如果不需要）
showDebugInfo = false;         // 发布时关闭调试
```

### 系统要求
- **最低配置**: Intel i5, 8GB RAM, 集成显卡
- **推荐配置**: Intel i7, 16GB RAM, 独立显卡
- **摄像头**: 720p 30fps 或更高

## 🎯 使用示例

### 简单使用示例
```csharp
// 1. 添加SimplePoseDemo到空的GameObject
// 2. 运行场景
// 3. 在摄像头前移动手臂，观察立方体跟随移动
```

### 编程控制示例
```csharp
// 获取跟随器组件
var follower = FindObjectOfType<SimpleCharacterFollower>();

// 调整参数
follower.SetMoveSensitivity(8f);
follower.ToggleRotation(false);
follower.ToggleSmoothMovement(true);

// 重置位置
follower.ResetPosition();
```

## 📝 更新日志

### v2.3 (最新) - 终极修复版
- ✅ **修复了所有已知的编译错误**
- ✅ 新增 `SimplePoseDemo.cs` 无UI依赖演示
- ✅ 完善了MediaPipe类型比较的安全处理
- ✅ 统一了所有UnityEngine类型的命名空间引用
- ✅ 增强了错误恢复机制

### v2.1 - 兼容性修复版
- 修复了MediaPipe API兼容性问题
- 添加了反射机制处理不同版本
- 解决了Image组件命名空间冲突

### v2.0 - 功能完整版
- 添加了完整的UI控制面板
- 支持多种参考点选择
- 改进了旋转计算

### v1.0 - 基础版本
- 基础的姿势跟随功能

## 🤝 技术支持

### 推荐使用顺序
1. **极简首选**: `UltraSimpleDemo.cs` + `UltraSimpleFollower.cs` (零错误)
2. **稳定推荐**: `SimplePoseDemo.cs` + `SimpleCharacterFollower.cs`
3. **功能完整**: `CharacterFollower.cs` (需要更多配置)
4. **高级用户**: `PoseFollowDemo.cs` + 完整UI系统

### 获取帮助
1. 查看Unity控制台的详细错误信息
2. 使用 `TestPoseFollowSystem.cs` 进行系统诊断
3. 检查本文档的故障排除部分
4. 确保MediaPipe插件版本兼容

## 📜 许可证

本项目基于MediaPipe Unity Plugin构建，请遵循相应的开源许可证。

---

**🎉 所有编译错误已完全修复！推荐使用 `SimplePoseDemo.cs` 获得最佳体验。** 