[📖 English](README_EN.md) | [📖 日本語](README_JP.md)

# paulxstx · MA万能混搭生成器

<p align="center">
  <b>paulxstx Outfit Composer — Universal Mix & Match for VRChat Avatars</b>
</p>

<p align="center">
  基于 <a href="https://modular-avatar.nadena.dev/">Modular Avatar</a> 的 VRChat 角色一键换装与万能混搭菜单生成器。<br/>
  全中文 Inspector 界面 · 零代码配置 · 上传即用。
</p>

---

## ✨ 功能特性

| 功能 | 说明 |
|------|------|
| 🎽 **套装项目切换** | 一键切换整套衣服（衣服A/B/C、发型、身体形态等），每个套装可绑定独立物体和子级关闭菜单 |
| 👟 **万能混搭** | 鞋子、袜子、内衣、帽子等部件独立控制，自由组合，与套装切换智能联动 |
| 🔧 **关闭部件** | 细粒度开关（如关闭鞋带、蝴蝶结、裙摆），支持手动/自动生成，默认保存同步 |
| 🔁 **LastOp 覆盖机制** | 记录"最后一次操作"的参数值，再次点击当前项自动恢复默认混搭，避免状态混乱 |
| 💾 **配置备份/恢复** | 一键导出 JSON 备份，升级前安心存档；按相对路径恢复物体引用 |
| 🔍 **冲突检测** | 自动检测重复参数名、空物体、多开关控制同一物体、已有 MA 组件冲突等问题 |
| ⚡ **性能优化** | 不每帧刷新 Inspector、生成进度条、使用 Parameter Driver Copy 避免状态爆炸 |
| 📁 **角色独立目录** | 每个角色的生成资源放在独立文件夹，多角色不互相覆盖 |
| 🛡️ **安全无联网** | 纯本地工具，无联网请求，无远程执行，无隐藏逻辑 |

---

## 📦 安装

### 方式二：手动安装

1. 下载本仓库最新 Release 中的 `com.paulxstx.last-op-outfit-cn.zip`
2. 解压到你的 Unity 项目的 `Packages/` 目录下
3. 或者通过 Unity Package Manager → Add package from disk → 选择 `package.json`

### 依赖

| 依赖 | 最低版本 |
|------|----------|
| Unity | 2022.3 |
| VRChat SDK3 — Avatars | 3.x |
| [Modular Avatar](https://modular-avatar.nadena.dev/) | 1.x |

> 以上依赖需提前安装在你的 VRChat 角色项目中。

---

## 🚀 快速开始

### 1. 添加生成器

选中角色根节点（Avatar Root），通过以下任一方式添加：

- 菜单栏 → `Tools/最终操作换装/给选中角色添加通用混搭生成器`
- 右键 Hierarchy → `最终操作换装/添加通用混搭换装生成器`
- 或直接 `添加并生成通用混搭菜单` 一步到位

### 2. 配置套装与混搭

在 Inspector 中：

- **套装项目切换列表**：配置衣服、发型等大类切换（每个项目拖入对应物体）
- **混搭项目列表**：配置鞋子、袜子等可独立混搭的部件
- 每个选项内可展开"关闭部件开关列表"做细粒度控制

### 3. 生成菜单

点击 Inspector 底部的 **"生成菜单"** 按钮即可。生成的资源包括：

- MA Parameters（Expression Parameters 参数）
- MA Menu Installer（VRChat 动作菜单）
- FX Animator Controller（动画状态机）
- MA Object Toggle / Merge Animator 组件

### 4. 上传

正常上传角色即可，菜单会自动随角色一起上传。若开启"上传前自动重新生成"，每次上传都会使用最新配置。

---

## 📖 配置详解

### 套装项目切换（Super Switch）

```
衣服
├── 衣服A (值=1)  → 拖入衣服A本体
│   ├── 默认混搭设置 → 鞋子=1, 袜子=1
│   └── 子级关闭菜单 → 关闭蝴蝶结, 关闭裙摆
├── 衣服B (值=2)  → 拖入衣服B本体
└── 衣服C (值=3)  → 拖入衣服C本体
```

- **再次点击当前项恢复默认混搭**：开启后，穿衣服A时再点衣服A，会通过 LastOp 参数恢复鞋袜默认值
- **生成"不穿/关闭"**：开启后在菜单最前面加一个"不穿"选项

### 混搭项目（Mix Groups）

```
鞋子
├── 鞋子A (值=1) → 拖入鞋子A+袜子A组合体
├── 鞋子B (值=2) → 拖入鞋子B+袜子B组合体
└── 不穿 (值=0)
```

- **套装项目关闭时同时关闭**：当衣服选了"不穿"，鞋子也自动设为 0
- 可通过"默认混搭设置"让鞋子自动跟随衣服的当前值

### LastOp 机制说明

LastOp（Last Operation / 最后一次操作）是本插件的核心机制：插件内部使用 `LW_LastApplied_*` 参数记录每个套装上一次的实际选择值。当用户再次点击当前已穿戴的套装项时，系统通过 LastOp 参数判断"没有真正切换"，从而触发恢复默认混搭，而不是重复写入。这避免了 Avatar Parameter Driver 的状态组合爆炸问题。

### 一键全关按钮

- **一键关闭全部**：只关套装项目切换的部件，不影响混搭
- **自定义全关按钮**：自由选择要关闭的混搭项目（如"一键全关含鞋袜"）

---

## 🔍 冲突检测

点击 Inspector 底部的 **"冲突检测 / 配置检查"**，会自动检查：

- 重复参数名
- 重复参数值（同一套装内）
- 空物体引用
- 默认混搭未匹配
- 同一物体被多个开关控制
- 已有 MA Object Toggle 可能与生成结果冲突

---

## 🛡️ 安全说明

- ✅ 无联网请求
- ✅ 无远程执行
- ✅ 无后台通信
- ✅ 无隐藏上传
- ✅ 纯 Unity Editor 本地工具

---

## 🔄 升级与备份

- 升级前建议点击 **"导出当前配置备份 JSON"** 保存配置
- 升级后如配置丢失，点击 **"导入配置备份 JSON"** 恢复
- v2.1.14+ 的配置可在后续版本中继续使用（保持字段兼容）

---

## 📋 版本历史

| 版本 | 重点 |
|------|------|
| **2.1.23** | 全量中文 Inspector 显示与排版优化 |
| 2.1.20 | 配置备份/恢复 JSON、冲突检测、修复新增选项重复问题 |
| 2.1.19 | 套装项目切换流畅版，默认不自动补部件开关，MA 开关优先级 |
| 2.1.14 | 性能基线：Inspector 不再每帧刷新、GetPropertyHeight 不写 SP |

---

## 👤 作者

**paulxstx**（GitHub: [@sodakitten](https://github.com/sodakitten)）

---

## 🔗 相关链接

- [Modular Avatar 官方文档](https://modular-avatar.nadena.dev/)
- [VRChat Creator Companion](https://vcc.docs.vrchat.com/)
