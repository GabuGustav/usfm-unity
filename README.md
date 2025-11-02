# USFM - Unity Simple Finite State Machine

A lightweight, modular FSM for AI behaviors, player states, or animations in Unity. Built for easy extension — add states without touching core code.

## Quick Setup
1. Attach `USFMController` to your GameObject (e.g., Enemy prefab).
2. Create/assign a `StateDataSO` (right-click in Project → Create → USFM → State Data) for tunable values like speed, detection range.
3. Drag your starting state (e.g., `IdleState`) into the `Starting State` field in Inspector.
4. Add more states to the `States` list for transitions.
5. Hit Play — enable `Draw Gizmos` to visualize current state (green) and possible transitions (yellow).

## Core Architecture
- **IState Interface** (Core/IState.cs): Defines `Enter()`, `Update()`, `Exit()` hooks.
- **StateMachine** (Core/StateMachine.cs): Manages transitions via conditions.
- **USFMController** (Core/USFMController.cs): MonoBehaviour wrapper — handles serialization and gizmos.
- **Flow**: Enter → Update (check transitions) → Exit → Switch.

## Folder Breakdown
| Folder | Key Files | Purpose |
|--------|-----------|---------|
| **Core** | `IState.cs`, `StateMachine.cs`, `USFMController.cs` | Base logic and Mono wrapper. |
| **States** | `IdleState.cs`, `ChaseState.cs`, `AttackState.cs`, `PatrolSubState.cs` | Specific behaviors (e.g., NavMesh pathing in Chase). |
| **Transitions** | `StateTransition.cs`, `TransitionCondition.cs` | Conditions like `DistanceToPlayer < 5f` or `Health < 50%`. |
| **Utils** | `StateDataSO.cs`, `DebugDrawer.cs` | Tunable data (SOs) and in-editor viz (Gizmos/Lines). |

## Debug Tips
- **Scene View**: Green arrow = active state; Yellow lines = valid transitions; Red = blocked.
- **Console**: Enable `Debug Logs` in USFMController for state change spam.
- **Test Transitions**: Use `Force Transition` button in custom Inspector (add below).

## Extending It
1. New State: Implement `IState` → Add to `USFMController.States`.
2. New Condition: Extend `TransitionCondition` (e.g., `LineOfSightCondition`).
3. Hierarchical: Nest a `SubStateMachine` in complex states like Chase.

## Known Limits
- Flat transitions only (no deep hierarchies yet).
- Single-threaded — fine for <50 NPCs; pool for hordes.

Built with Unity 2022.3+. Questions? Ping the repo owner.