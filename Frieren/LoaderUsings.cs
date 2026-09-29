// Game namespaces and loader-dependent aliases for every file of this assembly.
// MelonLoader's interop prefixes the game's namespaces with Il2Cpp; BepInEx keeps the original names.
#if BEPINEX
global using global::Refactor;
global using global::Refactor.Addressable;
global using global::Refactor.Combat;
global using global::Refactor.Component;
global using global::Refactor.Control;
global using global::Refactor.Main;
global using global::Refactor.Main.Event;
global using global::Refactor.Main.InputModule;
global using global::Refactor.Main.Processor;
global using global::Refactor.Map;
global using global::Refactor.Setting;
global using global::Refactor.Tick.DataApply;
global using global::Refactor.UI;
global using global::Refactor.Util;
global using global::TMPro;
global using global::Util;
global using global::Util.Sheet;
global using ComponentSaveList = Il2CppSystem.Collections.Generic.List<global::Refactor.ComponentSaveData>;
global using HolderList = Il2CppSystem.Collections.Generic.List<global::Refactor.Component.AffecterHolder>;
global using InventoryActions = Il2CppSystem.Collections.Generic.List<global::Refactor.Control.Interaction>;
global using SavedComponents = Il2CppSystem.Collections.Generic.List<global::Refactor.ComponentSaveData>;
global using SkillExecutionContext = global::Refactor.Combat.ExecutionContext;
#else
global using Il2CppRefactor;
global using Il2CppRefactor.Addressable;
global using Il2CppRefactor.Combat;
global using Il2CppRefactor.Component;
global using Il2CppRefactor.Control;
global using Il2CppRefactor.Main;
global using Il2CppRefactor.Main.Event;
global using Il2CppRefactor.Main.InputModule;
global using Il2CppRefactor.Main.Processor;
global using Il2CppRefactor.Map;
global using Il2CppRefactor.Setting;
global using Il2CppRefactor.Tick.DataApply;
global using Il2CppRefactor.UI;
global using Il2CppRefactor.Util;
global using Il2CppTMPro;
global using Il2CppUtil;
global using Il2CppUtil.Sheet;
global using ComponentSaveList = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.ComponentSaveData>;
global using HolderList = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.Component.AffecterHolder>;
global using InventoryActions = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.Control.Interaction>;
global using SavedComponents = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.ComponentSaveData>;
global using SkillExecutionContext = Il2CppRefactor.Combat.ExecutionContext;
#endif
