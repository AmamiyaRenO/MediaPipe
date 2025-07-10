# 游戏开始画面功能说明

## 功能概述

添加了游戏开始画面功能，用户需要通过语音命令"start"来开始游戏。在游戏开始前会显示标题画面，所有船体控制功能暂停。

## 主要特性

### 🎮 游戏状态管理
- **GameStateManager.cs**: 负责游戏状态控制和开始画面显示
- 游戏开始前显示全屏标题界面
- 支持语音和键盘两种开始方式

### 🎤 语音启动指令
支持以下语音指令开始游戏：
- `start`
- `begin` 
- `开始` (中文)
- `go`
- `play`

### ⌨️ 键盘备用启动
- 按下**空格键**也可以开始游戏（用于测试）

## 界面设计

### 标题画面显示内容：
1. **主标题**: "Slow Boat Motion Control Game" (大字体，白色)
2. **副标题**: "Say 'start' or 'begin' to play" (中字体，黄色，闪烁效果)
3. **提示文本**: "🎤 Voice Control | 🤸 Motion Control" (小字体，青色)
4. **支持指令**: 底部显示所有支持的开始指令
5. **键盘提示**: "Or press SPACEBAR to start"

### 视觉效果：
- 半透明黑色背景覆盖
- 副标题有1秒间隔的闪烁动画
- 居中布局，适配不同屏幕尺寸

## 技术实现

### 组件关系：
```
GameStateManager (总控制器)
├── 监听语音识别事件
├── 控制游戏开始状态
├── 显示标题画面
└── 管理其他组件激活状态

BoatVoiceController (语音处理)
├── 处理开始指令 (最高优先级)
├── 游戏未开始时忽略其他指令
└── 自动查找GameStateManager

BoatController (船体控制)
├── 检查游戏开始状态
├── 游戏未开始时暂停控制
└── 游戏未开始时隐藏UI
```

### 自动组件发现：
- GameStateManager自动查找BoatController、BoatVoiceController
- BoatVoiceController自动查找GameStateManager
- BoatController自动查找GameStateManager
- 无需手动拖拽组件引用

## 使用方法

### 1. 场景配置
确保场景中包含以下GameObject：
- `GameStateManager` - 已自动添加到场景
- `VoiceController` - 已存在
- `BoatController` - 已存在

### 2. 测试步骤
1. 运行游戏
2. 看到标题画面
3. 说出"start"或按空格键
4. 游戏开始，标题消失
5. 船体控制系统激活

### 3. 自定义配置
在GameStateManager Inspector中可调整：
- `gameTitle`: 主标题文本
- `gameSubtitle`: 副标题文本
- `hintText`: 提示文本
- `startCommands`: 支持的开始指令列表
- `titleFontSize/subtitleFontSize/hintFontSize`: 字体大小

## 代码结构

### GameStateManager.cs
- `gameStarted`: 游戏状态标志
- `OnSpeechRecognized()`: 处理语音识别结果
- `StartGame()`: 开始游戏主方法
- `OnGUI()`: 绘制标题画面

### BoatVoiceController.cs 修改
- 添加`startGameCommands`字段
- `ProcessVoiceCommand()`中优先处理开始指令
- 游戏未开始时忽略其他语音指令

### BoatController.cs 修改  
- `Update()`中检查游戏状态
- `OnGUI()`中检查游戏状态
- 游戏未开始时暂停所有控制逻辑

## 调试信息

开启`showDebugLogs`后可看到：
- 🎮 游戏状态管理器初始化
- ✅ 组件自动发现结果  
- 🚀 游戏开始触发信息
- ⏸️ 游戏未开始时的指令忽略

## 注意事项

1. **语音识别初始化**: GameStateManager会等待2秒后尝试订阅语音事件
2. **JSON解析**: 支持Vosk返回的JSON格式语音结果
3. **向后兼容**: 不影响原有的船体控制和语音命令功能
4. **错误恢复**: 如果语音识别失败，可以使用空格键备用启动

## 扩展建议

- 添加游戏结束画面
- 支持更多语言的开始指令
- 添加音效和动画效果
- 保存最高分数等游戏数据 