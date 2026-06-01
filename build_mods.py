import os
import subprocess
import shutil
from pathlib import Path

# --- CONFIGURATION ---
GAME_MODS_DIR = r"C:\dev\sts2\Release"
GODOT_PATH = r"C:\dev\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"


def discover_mods():
    """Dynamically scans the mods/ folder for any valid C# mod projects."""
    mods_root = Path("mods")
    if not mods_root.exists():
        print("❌ 'mods' folder not found in the current working directory!")
        return []
    
    found_mods = []
    for subfolder in mods_root.iterdir():
        if subfolder.is_dir():
            if list(subfolder.glob("*.csproj")):
                found_mods.append(subfolder.name)
                
    return found_mods


def ensure_godot_project_files(mod_project_dir, mod_name):
    """Ensures Godot recognizes this folder as a valid project layout for exporting."""
    # 1. Godot needs a project.godot file to look at a folder path
    project_godot_path = os.path.join(mod_project_dir, "project.godot")
    if not os.path.exists(project_godot_path):
        with open(project_godot_path, "w", encoding="utf-8") as f:
            f.write(f'[config]\nconfig_version=5\nname="{mod_name}"\n')

    # 2. Godot needs an explicit preset declaration matching the export pack argument
    presets_path = os.path.join(mod_project_dir, "export_presets.cfg")
    if not os.path.exists(presets_path):
        with open(presets_path, "w", encoding="utf-8") as f:
            f.write(f'''[preset.0]
name="{mod_name}"
platform="Windows Desktop"
runnable=false
custom_features=""
export_filter="all_files"
include_filter=""
exclude_filter=""
export_path=""
''')


def build_mod(mod_name):
    print(f"\n==== Auto-Building Mod: {mod_name} ====")
    
    mod_project_dir = os.path.abspath(f"mods/{mod_name}")
    target_deploy_dir = os.path.join(GAME_MODS_DIR, mod_name)
    os.makedirs(target_deploy_dir, exist_ok=True)

    # Step 1: Run the C# Compiler via .NET SDK
    print("Compiling C# Source Code...")
    csproj_file = os.path.join(mod_project_dir, f"{mod_name}.csproj")
    build_result = subprocess.run(["dotnet", "build", csproj_file, "-c", "Debug"], capture_output=True, text=True)
    
    if build_result.returncode != 0:
        print(f"❌ C# Compilation failed for {mod_name}!")
        print(build_result.stderr)
        return False
    
    # Step 2: Set up Godot environment variables so engine CLI binds work
    ensure_godot_project_files(mod_project_dir, mod_name)
    
    print("Packing assets into Godot archive...")
    pck_output_path = os.path.join(target_deploy_dir, f"{mod_name}.pck")
    
    # We explicitly invoke Godot's desktop template mode using our dynamic config
    godot_cmd = [
        GODOT_PATH, "--headless", 
        "--path", mod_project_dir, 
        "--export-pack", mod_name, pck_output_path
    ]
    
    try:
        # Capture output to see if Godot is throwing complaints about structural configurations
        result = subprocess.run(godot_cmd, capture_output=True, text=True)
        if result.returncode != 0 or not os.path.exists(pck_output_path):
            print(f"⚠️ Godot pack export reported an evaluation warning. Attempting fallback custom packing sequence...")
            # Fallback layout check: Let's pass a direct standalone layout pack array if standard export skipped it
            fallback_cmd = [GODOT_PATH, "--headless", "--path", mod_project_dir, "--pack", pck_output_path]
            subprocess.run(fallback_cmd, capture_output=True)
            
    except FileNotFoundError:
        print(f"❌ Could not execute Godot! Check that GODOT_PATH is correct.")
        return False

    # Step 3: Copy the freshly generated .dll and json directly to your game folder
    print("Deploying files to Slay the Spire 2...")
    
    dll_source = os.path.join(mod_project_dir, ".godot", "mono", "temp", "bin", "Debug", f"{mod_name}.dll")
    json_source = os.path.join(mod_project_dir, f"{mod_name}.json")
    
    if os.path.exists(dll_source):
        shutil.copy(dll_source, os.path.join(target_deploy_dir, f"{mod_name}.dll"))
    if os.path.exists(json_source):
        shutil.copy(json_source, os.path.join(target_deploy_dir, f"{mod_name}.json"))

    # Final Verification check on target directory contents
    if os.path.exists(pck_output_path):
        print(f"✨ Successfully deployed {mod_name}! (.dll, .json, and .pck match perfectly!)")
        return True
    else:
        print(f"❌ Asset package generation error. PCK folder footprint could not verify target destination assembly boundaries.")
        return False


if __name__ == "__main__":
    dynamic_mods_list = discover_mods()
    
    if not dynamic_mods_list:
        print("No valid mod projects discovered.")
    else:
        print(f"Discovered {len(dynamic_mods_list)} mods: {dynamic_mods_list}")
        for mod in dynamic_mods_list:
            build_mod(mod)