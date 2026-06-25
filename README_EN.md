[📖 中文版本](README.md) | [📖 日本語](README_JP.md)

# Paulxstx MA Outfit Composer

<p align="center">
  <b>Universal Mix & Match Outfit Generator for VRChat Avatars</b>
</p>

<p align="center">
  A one-click outfit switching and mix-and-match menu generator built on <a href="https://modular-avatar.nadena.dev/">Modular Avatar</a>.<br/>
  Fully Chinese-localized Inspector UI · Zero-code configuration · Upload-ready.
</p>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🎽 **Super Switch Groups** | One-click toggle between full outfits (clothes A/B/C, hairstyles, body shapes, etc.). Each option binds its own objects and sub-level close menus |
| 👟 **Mix & Match Groups** | Independently control shoes, socks, underwear, hats, and accessories. Smart linkage with outfit switching |
| 🔧 **Close Options** | Fine-grained part toggling (e.g., hide shoelaces, ribbons, skirt layers). Supports manual/auto generation with save & sync defaults |
| 🔁 **LastOp Override** | Records the "last operation" parameter value per outfit group. Re-clicking the current item triggers a default-mix restore instead of redundant re-application, avoiding state explosion |
| 💾 **Config Backup/Restore** | One-click JSON export for safe pre-upgrade backups. Restore with relative-path object resolution |
| 🔍 **Conflict Detection** | Auto-detects duplicate parameter names, empty objects, multi-switch conflicts, existing MA component collisions |
| ⚡ **Performance Optimized** | No per-frame Inspector refresh, progress bar for generation, Parameter Driver Copy to avoid state explosion |
| 📁 **Avatar-Specific Output** | Each avatar gets its own generated asset folder — no cross-contamination |
| 🛡️ **Safe & Offline** | Pure local tool. No network requests, no remote execution, no hidden logic |

---

## 📦 Installation

### Manual Install

1. Download `com.paulxstx.last-op-outfit-cn.zip` from the latest Release
2. Extract into your Unity project's `Packages/` folder
3. Or use Unity Package Manager → Add package from disk → select `package.json`

### Dependencies

| Dependency | Minimum Version |
|------------|-----------------|
| Unity | 2022.3 |
| VRChat SDK3 — Avatars | 3.x |
| [Modular Avatar](https://modular-avatar.nadena.dev/) | 1.x |

> These must be pre-installed in your VRChat avatar project.

---

## 🚀 Quick Start

### 1. Add the Composer

Select your avatar root, then either:

- Menu bar → `Tools/最终操作换装/给选中角色添加通用混搭生成器`
- Right-click Hierarchy → `最终操作换装/添加通用混搭换装生成器`
- Or use `添加并生成通用混搭菜单` to add and build in one step

### 2. Configure Outfits & Mixes

In the Inspector:

- **Super Switch Groups list**: Configure top-level categories (clothes, hair, etc.). Drag objects into each option
- **Mix Groups list**: Configure independently mixable parts (shoes, socks, etc.)
- Expand **Close Options** within each item for fine-grained part control

### 3. Generate Menu

Click the **"生成菜单"** (Generate Menu) button. The tool creates:

- MA Parameters (Expression Parameters)
- MA Menu Installer (VRChat action menu)
- FX Animator Controller (state machine with Parameter Drivers)
- MA Object Toggle / Merge Animator components

### 4. Upload

Upload your avatar as normal — the menu is bundled automatically. If "Build on Upload" is enabled, the latest config is used on every upload.

---

## 📖 Configuration Details

### Super Switch Groups

```
Clothes
├── Clothes A (value=1)  → drag Clothes A object
│   ├── Default Mix Settings → Shoes=1, Socks=1
│   └── Sub-level Close Menu → Hide ribbon, Hide skirt
├── Clothes B (value=2)  → drag Clothes B object
└── Clothes C (value=3)  → drag Clothes C object
```

- **Re-click current item restores default mix**: When enabled, clicking Clothes A while already wearing it restores shoes/socks defaults via LastOp
- **Generate "None/Off"**: Adds a "None" option at the top of the menu

### Mix Groups

```
Shoes
├── Shoes A (value=1) → drag Shoes A + Socks A combo
├── Shoes B (value=2) → drag Shoes B + Socks B combo
└── None (value=0)
```

- **Turn off when Super Switch turns off**: When clothes are set to "None", shoes also default to 0
- Use **Default Mix Settings** to have shoes automatically follow the current clothes value

### How LastOp Works

LastOp (Last Operation) is the plugin's core mechanism: it uses `LW_LastApplied_*` parameters to remember the last active value for each outfit group. When the user clicks the currently-worn outfit again, LastOp detects "no actual switch occurred" and triggers a restore-to-default-mix action instead of re-applying the same value. This avoids the combinatorial state explosion that would otherwise occur with Avatar Parameter Drivers.

### Close-All Buttons

- **Close All Parts**: Only closes Super Switch group parts, leaving Mix Groups unaffected
- **Custom Close-All Buttons**: Choose which Mix Groups to include (e.g., "Close All including Shoes & Socks")

---

## 🔍 Conflict Detection

Click **"冲突检测 / 配置检查"** (Conflict Detection) to check for:

- Duplicate parameter names
- Duplicate parameter values (within a group)
- Empty object references
- Unmatched default mix settings
- Objects controlled by multiple switches
- Potential conflicts with existing MA Object Toggles

---

## 🛡️ Safety

- ✅ No network requests
- ✅ No remote execution
- ✅ No background communication
- ✅ No hidden upload logic
- ✅ Pure local Unity Editor tool

---

## 🔄 Upgrading & Backup

- Before upgrading, click **"导出当前配置备份 JSON"** to save your config
- After upgrading, click **"导入配置备份 JSON"** to restore if needed
- Configs from v2.1.14+ remain compatible with future versions (field-level compatibility)

---

## 📋 Version History

| Version | Highlights |
|---------|------------|
| **2.1.23** | Full Chinese Inspector display & layout optimization |
| 2.1.20 | Config backup/restore JSON, conflict detection, fixed new-option duplication bug |
| 2.1.19 | Smooth outfit switching, auto-close-options disabled by default, MA toggle priority |
| 2.1.14 | Performance baseline: no per-frame Inspector refresh, no SP writes in GetPropertyHeight |

---

## 👤 Author

**Paulxstx** (GitHub: [@sodakitten](https://github.com/sodakitten))

---

## 🔗 Links

- [Modular Avatar Docs](https://modular-avatar.nadena.dev/)
- [VRChat Creator Companion](https://vcc.docs.vrchat.com/)
