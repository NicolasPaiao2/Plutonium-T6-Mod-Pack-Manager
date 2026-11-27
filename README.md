# Plutonium T6 // Mod & Pack Manager

[🇧🇷 Português](#português) | [🇺🇸 English](#english)

---

## Português

Gerenciador e instalador automático baseado em perfis para **Call of Duty: Black Ops II (Plutonium T6)**. Crie perfis independentes com seus próprios mods, combine vários mods ao mesmo tempo sem dor de cabeça e deixe a instalação e organização por conta do launcher.

### Funcionalidades
- **Perfis independentes:** Crie, renomeie, duplique, exclua e alterne entre diferentes packs de mods com 1 clique.
- **Instalação automática em `storage\t6`:**
  - `main` (`.iwd`, `.iwi`, camuflagens e texturas)
  - `scripts\zm`, `scripts\mp` e `scripts` (`.gsc` / `.csc`)
  - `mods\<mapa>` e `mods\<mod>` (mapas customizados e `mod.ff`)
- **Múltiplos mods simultâneos:** Ative vários mods juntos no mesmo perfil sem precisar mover arquivos manualmente.
- **Detecção automática:** Identifica modos (`MP`, `ZM`, `MP/ZM`) e camuflagens/itens comuns.
- **Verificação de integridade:** Detecta se arquivos instalados em `storage\t6` foram removidos manualmente.
- **Exportador Standalone (`.exe`):** Empacota o perfil selecionado em um instalador `.exe` único para compartilhar.

### Como compilar
1. Mantenha `PlutoniumModManager.cs`, `StandaloneInstallerTemplate.cs`, `Gerar_Executavel.bat` e `app.ico` na mesma pasta.
2. Execute **`Gerar_Executavel.bat`** para gerar `Plutonium_T6_ModManager.exe` com o ícone embutido.

---

## English

Profile-based automated mod manager and installer for **Call of Duty: Black Ops II (Plutonium T6)**. Create independent profiles with their own mods, run multiple mods together hassle-free, and let the launcher handle installation and organization automatically.

### Features
- **Independent profiles:** Create, rename, duplicate, delete, and switch between mod packs with 1 click.
- **Automated `storage\t6` deployment:**
  - `main` (`.iwd`, `.iwi`, camos, and textures)
  - `scripts\zm`, `scripts\mp`, and `scripts` (`.gsc` / `.csc`)
  - `mods\<map>` and `mods\<mod>` (custom maps and `mod.ff`)
- **Multiple mods at once:** Run several mods together within a profile without manual file management.
- **Automatic detection:** Identifies modes (`MP`, `ZM`, `MP/ZM`) and common camos/items.
- **Disk sync check:** Detects if installed files in `storage\t6` were manually removed.
- **Standalone `.exe` exporter:** Packages a selected profile into a single standalone `.exe` installer to share.

### How to build
1. Keep `PlutoniumModManager.cs`, `StandaloneInstallerTemplate.cs`, `Gerar_Executavel.bat`, and `app.ico` in the same folder.
2. Run **`Gerar_Executavel.bat`** to compile `Plutonium_T6_ModManager.exe` with the icon embedded.

---

## Author / Autor
**NicolasPaiao2** — [GitHub](https://github.com/NicolasPaiao2)
