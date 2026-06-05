# Slay the Spire 2 Mods

## 🚀 How to Build & Deploy Mods
Open your terminal in `sts2`.

Run the automation script:

```Bash
python build_mods.py
```
The script compiles the C# code, packs the Godot assets, and deploys the final bundles directly to your Release/ folder.

Open your Steam library, right-click Slay the Spire 2, and select Manage ➔ Browse local files.

In the game directory that opens, look for a folder named mods. (If it doesn't exist, create a new folder and name it mods).

Copy and paste your mod folders from Release/ directly into that mods/ directory. Your game path must look like this:

```Plaintext
Slay the Spire 2/
└── mods/
    ├── QuickRestart/
    │   ├── QuickRestart.dll
    │   ├── QuickRestart.pck
    │   └── QuickRestart.json
    └── UnifiedSave/
        ├── UnifiedSave.dll
        ├── UnifiedSave.pck
        └── UnifiedSave.json
```
Launch the game. Enable the mods in the built-in mod manager pop-up!

## 📦 Included Mods & How to Use Them
1. QuickRestart (mods/QuickRestart)
* What it does: Instantly restarts your current room from the last auto-save point (mimics quitting to the main menu and clicking continue).

* How to use it: Press the R key at any point during a single-player run.

2. UnifiedSave (mods/UnifiedSave)
* Type: System Override

* What it does: Prevents the game from routing your progress to a separate _modded profile folder when mods are enabled.

* How to use it: Keep this mod active in the game launcher. It runs entirely in the background, forcing the engine to read and write directly to your main vanilla save profile.

## 🛠️ Environment Setup & Decompilation
Before building or editing the mods, you must link the unmanaged game files to your development workspace so the compiler can resolve the native `MegaCrit` and `Godot` assembly dependencies.

### 1. Link Game References
Copy the core game assemblies from your active Steam installation directory into your local workspace references folder:
* **Source:** `..\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\`
* **Destination:** `sts2\References\`

### 2. Generate Decompiled Source (For Reference)
To inspect the underlying game logic and signatures, use ILSpy (or `ilspycmd` via command line) to dump the assembly code into your workspace directory:
```bash
ilspycmd -o decompiled_source References\SlayTheSpire2.dll
```

## 🤝 Co-Author
Gemini Flash 3.5 — AI Coding Collaborator & Co-Pilot 🤖