[📖 中文版本](README.md)

# paulxstx Outfit Composer

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
| 🎽 **Super Switch Groups** | One-click toggle between full outfits (clothes, hairstyles, body shapes, etc.). Each option binds its own objects and sub-level close menus |
| 👟 **Mix & Match Groups** | Independently control shoes, socks, accessories, etc. Smart linkage with outfit switching |
| 🔧 **Close Options** | Fine-grained part toggling (e.g., hide shoelaces, ribbons, skirt layers). Auto/manual generation with save & sync defaults |
| 🔁 **LastOp Override Logic** | Tracks the "last operation" parameter value per outfit group. Re-clicking the current item intelligently restores default mix instead of re-applying, avoiding state explosion |
| 💾 **Config Backup/Restore** | One-click JSON export for safe pre-upgrade backups. Restore with relative-path object resolution |
| 🔍 **Conflict Detection** | Auto-detect duplicate parameter names, empty objects, multi-switch conflicts, existing MA component collisions |
| ⚡ **Performance Optimized** | No per-frame Inspector refresh, progress bar for generation, Parameter Driver Copy to avoid state explosion |
| 📁 **Avatar-Specific Output** | Each avatar gets its own generated asset folder — no cross-contamination |
| 🛡️ **Safe & Offline** | Pure local tool. No network requests, no remote execution, no hidden logic |

---

## 📦 Installation

### Option 2: Manual Install

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

- Menu → `Tools/最终操作换装/给选中角色添加通用混搭生成器`
- Right-click Hierarchy → `最终操作换装/添加通用混搭换装生成器`
- Or use `添加并生成通用混搭菜单` to add and build in one step

### 2. Configure Outfits & Mixes

In the Inspector:

- **Super Switch Groups**: Configure top-level outfit categories (clothes, hair, etc.). Drag objects into each option
- **Mix Groups**: Configure mixable parts (shoes, socks, etc.) with independent options
- Expand **Close Options** within each item for fine-grained part control

### 3. Generate Menu

Click the **"生成菜单"** (Generate Menu) button. The tool creates:

- MA Parameters (Expression Parameters)
- MA Menu Installer (VRChat action menu)
- FX Animator Controller (state machine with Parameter Drivers)
- MA Object Toggle & Merge Animator components

### 4. Upload

Upload your avatar as normal. The menu is bundled automatically. Enable "Build on Upload" to auto-regenerate before every upload.

---

## 🔁 How LastOp Works

"LastOp" stands for "Last Operation" — the plugin internally uses `LW_LastApplied_*` parameters to remember the most recent active value for each outfit group. When the user clicks the currently-worn outfit again, LastOp detects "no actual switch occurred" and triggers a **restore-to-default-mix** action instead of redundantly re-applying the same value. This design avoids the combinatorial state explosion that would otherwise occur with Avatar Parameter Drivers, keeping your FX layer clean and performant.

---

## 🛡️ Safety

- ✅ No network requests
- ✅ No remote execution
- ✅ No background communication
- ✅ No hidden upload logic
- ✅ Pure local Unity Editor tool

---

## 👤 Author

**paulxstx** (GitHub: [@sodakitten](https://github.com/sodakitten))

---

## 🔗 Links

- [Modular Avatar Docs](https://modular-avatar.nadena.dev/)
- [VRChat Creator Companion](https://vcc.docs.vrchat.com/)
