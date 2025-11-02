// Assets/UFSM/Editor/FSMEditorHelpers.cs
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UFSM.Runtime;

namespace UFSM.Editor
{
    /// <summary>
    /// Helper utilities for editor scripts: GUID migration, safe mutation (Undo/SetDirty), and lookups.
    /// </summary>
    internal static class FSMEditorHelpers
    {
        public static void EnsureStateGuids(FSMDefinition fsm)
        {
            if (fsm == null || fsm.States == null) return;

            bool changed = false;
            foreach (var s in fsm.States)
            {
                if (string.IsNullOrEmpty(s.Guid))
                {
                    s.Guid = System.Guid.NewGuid().ToString();
                    changed = true;
                }
            }

            if (changed)
            {
                MarkDirty(fsm, "Ensure State GUIDs");
            }
        }

        public static void MigrateTransitionGuids(FSMDefinition fsm)
        {
            // Adds FromStateGuid/ToStateGuid if missing using state names as fallback
            if (fsm == null || fsm.Transitions == null || fsm.States == null) return;

            bool changed = false;
            foreach (var t in fsm.Transitions)
            {
                // If GUID fields exist (via partial extension) but are empty, try to resolve by name.
                if (string.IsNullOrEmpty(t.FromStateGuid))
                {
                    var s = fsm.States.FirstOrDefault(st => st.StateName == t.FromState);
                    if (s != null)
                    {
                        t.FromStateGuid = s.Guid;
                        changed = true;
                    }
                }

                if (string.IsNullOrEmpty(t.ToStateGuid))
                {
                    var s = fsm.States.FirstOrDefault(st => st.StateName == t.ToState);
                    if (s != null)
                    {
                        t.ToStateGuid = s.Guid;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                MarkDirty(fsm, "Migrate Transition GUIDs");
            }
        }

        public static StateDefinition FindStateByGuid(FSMDefinition fsm, string guid)
        {
            if (fsm == null || fsm.States == null || string.IsNullOrEmpty(guid)) return null;
            return fsm.States.FirstOrDefault(s => s.Guid == guid);
        }

        public static StateDefinition FindStateByName(FSMDefinition fsm, string name)
        {
            if (fsm == null || fsm.States == null || string.IsNullOrEmpty(name)) return null;
            return fsm.States.FirstOrDefault(s => s.StateName == name);
        }

        public static void MarkDirty(UnityEngine.Object obj, string undoLabel)
        {
            if (obj == null) return;
            Undo.RegisterCompleteObjectUndo(obj, undoLabel);
            EditorUtility.SetDirty(obj);
        }

        public static string GetUniqueStateName(FSMDefinition fsm, string baseName = "NewState")
        {
            if (fsm == null || fsm.States == null) return baseName;
            string name = baseName;
            int counter = 1;
            while (fsm.States.Exists(s => s.StateName == name))
            {
                name = $"{baseName}{counter}";
                counter++;
            }
            return name;
        }

        public static string GetUniqueParameterName(FSMDefinition fsm, string baseName = "NewParameter")
        {
            if (fsm == null || fsm.Parameters == null) return baseName;
            string name = baseName;
            int counter = 1;
            while (fsm.Parameters.Exists(p => p.Name == name))
            {
                name = $"{baseName}{counter}";
                counter++;
            }
            return name;
        }
    }
}
