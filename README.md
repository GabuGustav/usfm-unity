# USFM - Unity Simple Finite State Machine

**No separate StateMachine class.** All logic lives in `USFMController`.

## Quick Setup
1. Add `USFMController` to your AI GameObject.
2. Create `StateDataSO` (right-click → Create → USFM → State Data).
3. Assign it + drag `Starting State` (e.g., `IdleState`).
4. Add other states to the `States` list.
5. Play → enable `Draw Gizmos`.

## Core Files
- `Core/USFMController.cs` → **The entire FSM** (Update, transitions, switching)
- `Core/BaseState.cs` → All states inherit this
- `Core/IState.cs` → Interface
- `Transitions/StateTransition.cs` → Target state + conditions
- `Utils/DebugDrawer.cs` → Green = active, Yellow = transition

## Debug
- **Green arrow**: Current state
- **Yellow line**: Valid transition
- **Red**: Blocked condition

## Add New State
```csharp
public class StunnedState : BaseState { ... }