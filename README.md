# VRChat Avatar Workflow Utilities

A collection of lightweight, high-utility editor scripts designed to automate tedious tasks, optimize build pipelines, and accelerate VRChat avatar creation in Unity.

---

## 🚀 Features

### ⚡ ShaderPreCompiler
* **The Problem:** Unity's default build pipeline often hangs or slows down during the avatar upload phase due to on-the-fly shader variant compilation.
* **The Solution:** This tool scans your `VRC_AvatarDescriptor` and all child `SkinnedMeshRenderer` components to manually compile and warm up every shader variant ahead of time. This significantly reduces overall upload wait times.

### 🦴 TwistAssign
* **The Problem:** Manually configuring rotation and twist constraints bone-by-bone is tedious and repetitive.
* **The Solution:** Programmatically detects and assigns Twistbone constraints to all relevant joints with a single click directly from the Transform inspector.

### 📦 FBX Import Automation (`fbximport`)
* **The Solution:** Streamlines the raw asset pipeline by hooks into Unity's asset importer. 
  * Automatically sets the animation type to **Humanoid** if a compatible armature structure is detected.
  * Automatically enables **Read/Write** access.
  * Automatically extracts embedded materials.
* *Note: Due to occasional quirks with Unity's internal bone auto-mapping, manual verification of the rig configuration is still recommended for complex armatures.*

### 🎨 AlphaDetection
* **The Solution:** Automatically inspects imported `.png` textures with Read/Write enabled. If an alpha channel is detected, it instantly toggles the **Alpha Is Transparency** flag to prevent edge artifacts and rendering issues.

### **Where to place**
Download them seperate from releases and place them in your assets/editor folder of your project. 
or download the full pack and extract them in your assets/editor folder of your project.

/// NOTE: if you have lots of textures and fbx files opening your project after adding the scripts can take a while as it will reprocess all of them (only once as a new import of the files).
