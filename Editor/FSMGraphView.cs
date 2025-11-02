// Assets/UFSM/Editor/FSMGraphView.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;

namespace UFSM.Editor
{
    public class FSMGraphView : GraphView
    {
        private FSMEditorWindow window;
        private FSMDefinition currentFSM;
        private Dictionary<string, StateNode> stateNodes = new Dictionary<string, StateNode>();
        private GridBackground gridBackground;

        // NEW: Flag to prevent refresh loops
        private bool isRefreshing = false;

        public FSMGraphView(FSMEditorWindow window)
        {
            this.window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            style.flexGrow = 1;
            graphViewChanged += OnGraphViewChanged;
        }

        public void Refresh(FSMDefinition fsm)
        {
            currentFSM = fsm;

            if (currentFSM == null) return;

            // NEW: Set flag to prevent OnGraphViewChanged from triggering during refresh
            isRefreshing = true;

            FSMEditorHelpers.EnsureStateGuids(currentFSM);
            FSMEditorHelpers.MigrateTransitionGuids(currentFSM);

            // Clear existing (this will NOT trigger OnGraphViewChanged now)
            DeleteElements(graphElements.ToList());
            stateNodes.Clear();

            // Create state nodes
            foreach (var stateDef in fsm.States)
            {
                CreateStateNode(stateDef);
            }

            // Create transition edges
            foreach (var transDef in fsm.Transitions)
            {
                CreateTransitionEdge(transDef);
            }

            // NEW: Clear flag after refresh completes
            isRefreshing = false;
        }

        private void CreateStateNode(StateDefinition stateDef)
        {
            var node = new StateNode(stateDef, window);

            if (stateDef.EditorPosition != Vector2.zero)
            {
                node.SetPosition(new Rect(stateDef.EditorPosition, Vector2.zero));
            }
            else
            {
                int index = currentFSM.States.IndexOf(stateDef);
                int columns = 3;
                int row = index / columns;
                int col = index % columns;

                Vector2 position = new Vector2(100 + col * 250, 100 + row * 150);
                node.SetPosition(new Rect(position, Vector2.zero));
                stateDef.EditorPosition = position;
                FSMEditorHelpers.MarkDirty(currentFSM, "Auto-arrange State");
            }

            stateNodes[stateDef.Guid] = node;
            AddElement(node);
        }

        private void CreateTransitionEdge(TransitionDefinition transDef)
        {
            if (string.IsNullOrEmpty(transDef.FromStateGuid))
            {
                return;
            }

            StateNode sourceNode = null;
            StateNode targetNode = null;

            if (!string.IsNullOrEmpty(transDef.FromStateGuid))
                stateNodes.TryGetValue(transDef.FromStateGuid, out sourceNode);

            if (!string.IsNullOrEmpty(transDef.ToStateGuid))
                stateNodes.TryGetValue(transDef.ToStateGuid, out targetNode);

            if (sourceNode == null && !string.IsNullOrEmpty(transDef.FromState))
            {
                var s = FSMEditorHelpers.FindStateByName(currentFSM, transDef.FromState);
                if (s != null) stateNodes.TryGetValue(s.Guid, out sourceNode);
            }

            if (targetNode == null && !string.IsNullOrEmpty(transDef.ToState))
            {
                var s = FSMEditorHelpers.FindStateByName(currentFSM, transDef.ToState);
                if (s != null) stateNodes.TryGetValue(s.Guid, out targetNode);
            }

            if (sourceNode != null && targetNode != null)
            {
                var edge = new Edge
                {
                    output = sourceNode.outputPort,
                    input = targetNode.inputPort
                };

                edge.userData = transDef;
                edge.AddToClassList("transition-edge");

                AddElement(edge);

                sourceNode.outputPort.Connect(edge);
                targetNode.inputPort.Connect(edge);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            // NEW: Do nothing if we're in the middle of a Refresh()
            if (isRefreshing) return changes;

            if (currentFSM == null) return changes;

            bool needsRefresh = false;

            // Handle moved elements
            if (changes.movedElements != null)
            {
                foreach (var element in changes.movedElements)
                {
                    if (element is StateNode stateNode)
                    {
                        stateNode.StateDef.EditorPosition = stateNode.GetPosition().position;
                        FSMEditorHelpers.MarkDirty(currentFSM, "Move State");
                    }
                }
            }

            // Handle removed elements
            if (changes.elementsToRemove != null)
            {
                foreach (var element in changes.elementsToRemove)
                {
                    if (element is StateNode stateNode)
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Delete State");
                        currentFSM.States.Remove(stateNode.StateDef);
                        stateNodes.Remove(stateNode.StateDef.Guid);

                        currentFSM.Transitions.RemoveAll(t =>
                            t.FromStateGuid == stateNode.StateDef.Guid ||
                            t.ToStateGuid == stateNode.StateDef.Guid ||
                            t.FromState == stateNode.StateDef.StateName ||
                            t.ToState == stateNode.StateDef.StateName);

                        needsRefresh = true;
                    }
                    else if (element is Edge edge && edge.userData is TransitionDefinition transDef)
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Delete Transition");
                        currentFSM.Transitions.Remove(transDef);
                    }
                }
            }

            // Handle created edges
            if (changes.edgesToCreate != null)
            {
                foreach (var edge in changes.edgesToCreate)
                {
                    var sourceNode = edge.output.node as StateNode;
                    var targetNode = edge.input.node as StateNode;

                    if (sourceNode != null && targetNode != null)
                    {
                        var newTransition = new TransitionDefinition
                        {
                            FromStateGuid = sourceNode.StateDef.Guid,
                            ToStateGuid = targetNode.StateDef.Guid,
                            FromState = sourceNode.StateDef.StateName,
                            ToState = targetNode.StateDef.StateName
                        };

                        FSMEditorHelpers.MarkDirty(currentFSM, "Create Transition");
                        currentFSM.Transitions.Add(newTransition);
                        edge.userData = newTransition;
                    }
                }
            }

            // Only refresh if we actually deleted states (not during normal refresh)
            if (needsRefresh)
            {
                window?.RefreshAll();
            }

            return changes;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;
                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }
    }

    public class StateNode : Node
    {
        public StateDefinition StateDef { get; private set; }
        public Port inputPort { get; private set; }
        public Port outputPort { get; private set; }

        private FSMEditorWindow window;
        private Label stateNameLabel;
        private Label descriptionLabel;

        public StateNode(StateDefinition stateDef, FSMEditorWindow window)
        {
            this.StateDef = stateDef;
            this.window = window;

            title = stateDef.StateName;

            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "";
            inputContainer.Add(inputPort);

            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputPort.portName = "";
            outputContainer.Add(outputPort);

            var content = new VisualElement();
            content.style.paddingTop = 5;
            content.style.paddingBottom = 5;

            stateNameLabel = new Label(stateDef.StateName);
            stateNameLabel.style.fontSize = 14;
            stateNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            content.Add(stateNameLabel);

            if (!string.IsNullOrEmpty(stateDef.Description))
            {
                descriptionLabel = new Label(stateDef.Description);
                descriptionLabel.style.fontSize = 10;
                descriptionLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                descriptionLabel.style.maxWidth = 150;
                content.Add(descriptionLabel);
            }

            mainContainer.Add(content);
            StyleNode();
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            RefreshExpandedState();
        }

        private void StyleNode()
        {
            var fsm = window.GetCurrentFSM();
            if (fsm != null && StateDef.StateName == fsm.InitialStateName)
            {
                style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.2f));
                AddToClassList("initial-state");
            }
            else
            {
                style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f));
            }

            style.minWidth = 150;
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Set as Initial State", _ => SetAsInitialState());
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Duplicate", _ => DuplicateState());
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => DeleteState());
        }

        private void SetAsInitialState()
        {
            var fsm = window.GetCurrentFSM();
            if (fsm != null)
            {
                FSMEditorHelpers.MarkDirty(fsm, "Set Initial State");
                fsm.InitialStateName = StateDef.StateName;
                window.RefreshAll();
            }
        }

        private void DuplicateState()
        {
            var fsm = window.GetCurrentFSM();
            if (fsm == null) return;

            var newState = new StateDefinition
            {
                Guid = System.Guid.NewGuid().ToString(),
                StateName = GetUniqueName(StateDef.StateName),
                Description = StateDef.Description,
                AnimationStateName = StateDef.AnimationStateName,
                TriggerAnimationOnEnter = StateDef.TriggerAnimationOnEnter,
                AnimationTriggerParameter = StateDef.AnimationTriggerParameter,
                EditorPosition = StateDef.EditorPosition + new Vector2(50, 50)
            };

            FSMEditorHelpers.MarkDirty(fsm, "Duplicate State");
            fsm.States.Add(newState);
            window.RefreshAll();
        }

        private void DeleteState()
        {
            var fsm = window.GetCurrentFSM();
            if (fsm == null) return;

            if (EditorUtility.DisplayDialog("Delete State",
                $"Delete state '{StateDef.StateName}'?\nThis will also remove all transitions to/from this state.",
                "Delete", "Cancel"))
            {
                FSMEditorHelpers.MarkDirty(fsm, "Delete State");
                fsm.States.Remove(StateDef);
                fsm.Transitions.RemoveAll(t =>
                    t.FromStateGuid == StateDef.Guid ||
                    t.ToStateGuid == StateDef.Guid ||
                    t.FromState == StateDef.StateName ||
                    t.ToState == StateDef.StateName);

                window.RefreshAll();
            }
        }

        private string GetUniqueName(string baseName)
        {
            var fsm = window.GetCurrentFSM();
            string name = $"{baseName}_Copy";
            int counter = 1;

            while (fsm.States.Exists(s => s.StateName == name))
            {
                name = $"{baseName}_Copy{counter}";
                counter++;
            }

            return name;
        }
    }
}