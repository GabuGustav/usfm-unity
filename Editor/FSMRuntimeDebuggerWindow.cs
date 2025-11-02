using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;
using UFSM.Core;

namespace UFSM.Editor
{
    public class FSMDebuggerWindow : EditorWindow
    {
        private VisualElement leftPanel;
        private VisualElement rightPanel;
        private ListView fsmListView;
        private VisualElement detailsContainer;

        private List<FSMController> activeFSMs = new List<FSMController>();
        private FSMController selectedFSM;
        private Dictionary<FSMController, FSMDebugData> debugDataMap = new Dictionary<FSMController, FSMDebugData>();

        private double lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.1;

        [MenuItem("Window/UFSM/Runtime Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<FSMDebuggerWindow>();
            window.titleContent = new GUIContent("FSM Debugger");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                debugDataMap.Clear();
                RefreshFSMList();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                activeFSMs.Clear();
                debugDataMap.Clear();
                selectedFSM = null;
            }
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Row;

            leftPanel = new VisualElement();
            leftPanel.style.width = new StyleLength(new Length(30, LengthUnit.Percent));
            leftPanel.style.borderRightWidth = 1;
            leftPanel.style.borderRightColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            leftPanel.style.paddingTop = 5;
            leftPanel.style.paddingBottom = 5;
            leftPanel.style.paddingLeft = 5;
            leftPanel.style.paddingRight = 5;
            root.Add(leftPanel);

            var header = new Label("Active FSMs");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 5;
            leftPanel.Add(header);

            var refreshButton = new Button(RefreshFSMList);
            refreshButton.text = "Refresh";
            refreshButton.style.marginBottom = 10;
            leftPanel.Add(refreshButton);

            fsmListView = new ListView();
            fsmListView.style.flexGrow = 1;
            fsmListView.selectionType = SelectionType.Single;
            fsmListView.onSelectionChange += OnFSMSelected;
            leftPanel.Add(fsmListView);

            rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.paddingTop = 10;
            rightPanel.style.paddingBottom = 10;
            rightPanel.style.paddingLeft = 10;
            rightPanel.style.paddingRight = 10;
            root.Add(rightPanel);

            detailsContainer = new ScrollView();
            detailsContainer.style.flexGrow = 1;
            rightPanel.Add(detailsContainer);

            if (EditorApplication.isPlaying)
            {
                RefreshFSMList();
            }
            else
            {
                var notPlayingLabel = new Label("Enter Play Mode to debug FSMs");
                notPlayingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                notPlayingLabel.style.fontSize = 16;
                notPlayingLabel.style.color = new StyleColor(Color.gray);
                detailsContainer.Add(notPlayingLabel);
            }
        }

        private void Update()
        {
            if (!EditorApplication.isPlaying) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastRefreshTime >= REFRESH_INTERVAL)
            {
                lastRefreshTime = currentTime;
                RefreshFSMList();

                if (selectedFSM != null)
                {
                    UpdateDebugData(selectedFSM);
                    RefreshDetailsPanel();
                }
            }
        }

        private void RefreshFSMList()
        {
            if (!EditorApplication.isPlaying) return;

            activeFSMs = FindObjectsOfType<FSMController>().ToList();

            fsmListView.itemsSource = activeFSMs;
            fsmListView.makeItem = MakeFSMListItem;
            fsmListView.bindItem = BindFSMListItem;
            fsmListView.Rebuild();

            foreach (var fsm in activeFSMs)
            {
                if (!debugDataMap.ContainsKey(fsm))
                {
                    debugDataMap[fsm] = new FSMDebugData();
                    SubscribeToFSMEvents(fsm);
                }
            }

            var removedFSMs = debugDataMap.Keys.Where(fsm => !activeFSMs.Contains(fsm)).ToList();
            foreach (var fsm in removedFSMs)
            {
                debugDataMap.Remove(fsm);
            }
        }

        private VisualElement MakeFSMListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.paddingLeft = 5;
            container.style.paddingRight = 5;
            container.style.marginBottom = 2;
            container.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));

            var nameLabel = new Label();
            nameLabel.style.fontSize = 12;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.name = "name-label";
            container.Add(nameLabel);

            var stateLabel = new Label();
            stateLabel.style.fontSize = 10;
            stateLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            stateLabel.name = "state-label";
            container.Add(stateLabel);

            return container;
        }

        private void BindFSMListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= activeFSMs.Count) return;

            var fsm = activeFSMs[index];
            var nameLabel = element.Q<Label>("name-label");
            var stateLabel = element.Q<Label>("state-label");

            nameLabel.text = fsm.gameObject.name;
            stateLabel.text = "Active";
        }

        private void OnFSMSelected(IEnumerable<object> selection)
        {
            var selected = selection.FirstOrDefault() as FSMController;
            if (selected != null)
            {
                selectedFSM = selected;
                UpdateDebugData(selectedFSM);
                RefreshDetailsPanel();
            }
        }

        private void SubscribeToFSMEvents(FSMController fsm)
        {
            // Will add event subscriptions when FSMController events are added
        }

        private void UpdateDebugData(FSMController fsm)
        {
            if (!debugDataMap.TryGetValue(fsm, out var data)) return;

            // Will populate with actual FSM data when properties are added
            data.CurrentState = "Idle";
            data.TimeInState = Time.time;
        }

        private void RefreshDetailsPanel()
        {
            detailsContainer.Clear();

            if (selectedFSM == null || !debugDataMap.TryGetValue(selectedFSM, out var data))
            {
                var noSelectionLabel = new Label("Select an FSM to view details");
                noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noSelectionLabel.style.color = new StyleColor(Color.gray);
                detailsContainer.Add(noSelectionLabel);
                return;
            }

            var headerLabel = new Label("FSM: " + selectedFSM.gameObject.name);
            headerLabel.style.fontSize = 16;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.marginBottom = 10;
            detailsContainer.Add(headerLabel);

            var stateSection = CreateSection("Current State");
            var stateLabel = new Label(data.CurrentState ?? "None");
            stateLabel.style.fontSize = 18;
            stateLabel.style.color = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
            stateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            stateSection.Add(stateLabel);

            var timeLabel = new Label("Time in State: " + data.TimeInState.ToString("F2") + "s");
            timeLabel.style.fontSize = 11;
            timeLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            timeLabel.style.marginTop = 5;
            stateSection.Add(timeLabel);
            detailsContainer.Add(stateSection);
        }

        private VisualElement CreateSection(string title)
        {
            var section = new VisualElement();
            section.style.marginBottom = 15;
            section.style.paddingTop = 10;
            section.style.paddingBottom = 10;
            section.style.paddingLeft = 10;
            section.style.paddingRight = 10;
            section.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 12;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            section.Add(titleLabel);

            return section;
        }
    }

    public class FSMDebugData
    {
        public string CurrentState;
        public float TimeInState;
        public Dictionary<string, ParameterDebugInfo> Parameters = new Dictionary<string, ParameterDebugInfo>();
        public List<TransitionRecord> TransitionHistory = new List<TransitionRecord>();

        private const int MAX_HISTORY = 10;

        public void AddTransition(string from, string to)
        {
            TransitionHistory.Insert(0, new TransitionRecord
            {
                FromState = from,
                ToState = to,
                Time = Time.time
            });

            if (TransitionHistory.Count > MAX_HISTORY)
            {
                TransitionHistory.RemoveAt(TransitionHistory.Count - 1);
            }
        }
    }

    public class ParameterDebugInfo
    {
        public ParameterType Type;
        public object Value;
    }

    public class TransitionRecord
    {
        public string FromState;
        public string ToState;
        public float Time;
    }
}