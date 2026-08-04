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

### 🦴 Physbone tool
* **The Problem:** Having to manually enter or use sliders to control size and positions of physbone components can be a hassle.
* **The Solution:** This tool adds position,rotation and scaling gizmo's to the scene view for the physbone and physbone components that let's you do the positioning, rotation and scaling in the scene view.

### 📦 FBX Import Automation (`fbximport`)
* **The Solution:** Streamlines the raw asset pipeline by hooks into Unity's asset importer. 
  * Automatically sets the animation type to **Humanoid** if a compatible armature structure is detected.
  * Automatically enables **Read/Write** access.
  * Automatically extracts embedded materials.
* *Note: Due to occasional quirks with Unity's internal bone auto-mapping, manual verification of the rig configuration is still recommended for complex armatures.*
* 
### **Where to place**
Download the repository as zip and put the required scripts in Assets/Editor.

/// NOTE: if you have lots of textures and fbx files opening your project after adding the scripts can take a while as it will reprocess all of them (only once as a new import of the files).

/// Bugs can still occur especially if similar scripts and tools already exist in the project or changes to the sdk happen... make sure to report the in Issues \\\
