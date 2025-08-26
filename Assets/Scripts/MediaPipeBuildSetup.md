# MediaPipe Unity项目构建指南

## 问题概述

构建后的应用中摄像头和语音功能无法正常运行的主要原因：

1. **权限描述缺失** - iOS/macOS需要在Info.plist中声明摄像头和麦克风使用用途
2. **权限请求时机** - 应用启动时需要主动请求权限
3. **设备检测延迟** - 构建环境与运行环境的设备差异
4. **StreamingAssets路径** - Vosk模型文件路径在构建后可能发生变化

## 解决方案

### 1. 使用构建配置助手 (必须)

在Unity编辑器中：

```
顶部菜单 → MediaPipe Tools → Configure Build Settings
```

这将自动配置：
- iOS/macOS权限描述文本
- Android最低API级别
- 图形API设置
- 性能优化设置

### 2. 验证构建要求

构建前验证：

```
顶部菜单 → MediaPipe Tools → Validate Build Requirements
```

确保以下文件存在：
- `Assets/StreamingAssets/vosk-model-small-en-us-0.15.zip`
- `Packages/com.github.homuler.mediapipe/`
- 权限描述已设置

### 3. 添加权限管理器组件

在你的主场景中：

1. 创建一个空的GameObject
2. 命名为"MediaPermissionManager"
3. 添加`MediaPermissionManager`脚本组件
4. 配置设置：
   - ✅ Auto Request On Start
   - ✅ Show Permission UI
5. 在Unity Events中连接你的初始化函数

### 4. 修改现有系统

已自动修改`EmotionDetectionSystem`以等待权限授予后再初始化。

## 平台特定设置

### Windows
- 确保DirectX 11/12支持
- 防火墙可能阻止摄像头访问
- Windows 10/11隐私设置中启用摄像头和麦克风权限

### macOS  
- 系统偏好设置 → 安全性与隐私 → 隐私 → 摄像头/麦克风
- 需要手动授权应用访问
- 首次运行时会显示系统权限对话框

### iOS
- Info.plist自动包含权限描述
- 首次运行时显示系统权限对话框
- 用户可在设置中撤销权限

### Android
- API 23+支持运行时权限请求
- 在应用设置中可管理权限
- 某些设备可能需要额外的厂商权限

## 调试步骤

### 1. 检查权限状态

运行时查看左上角的权限UI框：
- Camera: ✓ Granted / ✗ Denied  
- Microphone: ✓ Granted / ✗ Denied

### 2. 手动请求权限

点击"Request Permissions"按钮重新请求。

### 3. 查看日志输出

构建版本中启用Console输出查看详细日志：
```
[MediaPermissionManager] Starting permission requests...
[MediaPermissionManager] Camera permission granted!
[MediaPermissionManager] Microphone permission granted!
```

### 4. 设备信息检查

在`MediaPermissionManager`上调用`LogDeviceInfo()`查看可用设备。

## 常见问题与解决方案

### Q: 权限已授予但摄像头仍不工作
A: 检查MediaPipe native libraries是否正确包含在构建中。验证Graphics API设置。

### Q: 语音识别不响应
A: 
1. 检查Vosk模型文件是否在StreamingAssets中
2. 确认麦克风设备可用
3. 调整`VoiceProcessor`的音量阈值

### Q: iOS构建后闪退
A: 确保在Build Settings中配置了正确的权限描述。检查iOS Deployment Target兼容性。

### Q: Android权限对话框不显示  
A: 检查Target SDK Version是否≥23。某些Android版本需要手动在设置中授权。

### Q: WebGL平台限制
A: WebGL对摄像头和麦克风支持有限，建议使用桌面平台进行开发测试。

## 构建检查清单

构建前确认：

- [ ] 运行"Configure Build Settings"
- [ ] 运行"Validate Build Requirements"  
- [ ] 场景中添加MediaPermissionManager
- [ ] StreamingAssets包含Vosk模型
- [ ] 测试权限请求流程
- [ ] 目标平台设置正确
- [ ] Graphics API兼容

构建后测试：

- [ ] 权限对话框正常显示
- [ ] 摄像头画面可见
- [ ] 麦克风输入检测
- [ ] 语音识别功能
- [ ] 姿态检测功能
- [ ] 情感分析系统

## 技术支持

如果仍有问题：

1. 运行"Generate Build Report"查看详细配置信息
2. 检查Unity Console中的错误日志
3. 使用Development Build进行调试
4. 对比Editor和Build版本的行为差异

记住：不同平台的权限机制不同，需要分别测试和验证。 