# Duel Daifugo

[**Play Online**](https://tomotomo.github.io/vibe-game/) | [日本語 (Japanese)](./README_JP.md)

A simple 1 vs 1 Daifugo (Tycoon) card game built with Unity for WebGL.

## Features

- **1vs1 Gameplay**: Play against a strategic CPU opponent.
- **Classic & Local Rules**:
  - **8 Cut (8切り)**: Playing an 8 clears the field and gives you another turn.
  - **5 Skip**: Playing a 5 skips the opponent's turn. Note: The field is *not* cleared, so you must play a stronger card against your own 5.
  - **11 Back (J Back)**: Playing a Jack (11) temporarily reverses card strength. If a Revolution is already active, it acts as a **Counter-Revolution**, returning strength to normal.
  - **Suit Binding (Shibari)**: If you match the suit(s) of the previous play, the suit is locked for the rest of the turn. Multi-card plays require an exact suit set match.
  - **Revolution**: 4 or more cards of the same rank reverse card strength.
- **Persistent Stats**: Tracks total games, wins, current streak, and max streak using local storage.
- **Dynamic Start**: Initial hand size is determined by a dice roll animation at the start of each game.
- **Smart UI**: 
  - **Lead Turn**: Select multiple cards and press "PLAY".
  - **Follow Turn**: Click a card to instantly play a valid matching combination (Smart Play). Smart Play intelligently handles Suit Binding constraints.
- **Replayability**: Restart the game instantly after finishing.

## Technology Stack

- **Engine**: Unity 2022.3
- **Language**: C#
- **UI**: Unity UI (uGUI) with programmatic layout generation.
- **Development Tool**: [Gemini CLI](https://github.com/google/gemini-cli) - All code, logic, and assets were generated via natural language prompting.

## Build Instructions

1. **Prerequisites**: Ensure you have Unity installed (2022.3 LTS recommended).
2. **Clone**: Clone this repository.
3. **Open**: Open the project folder in Unity Hub.
4. **Scene**: Open `Assets/Scenes/MainScene.unity`.
5. **Play**: Press the Play button in the Unity Editor.
6. **Build**: Go to `File > Build Settings`, select your platform (PC, Mac, WebGL, etc.), and click `Build`.

## Development with Gemini CLI

This project serves as a demonstration of "Vibe Coding" using an AI agent. 

- **Role**: The AI acted as the sole developer, writing scripts, debugging errors, and implementing features based on high-level instructions.
- **Workflow**:
    1. User requests a feature (e.g., "Create a Daifugo game").
    2. Agent generates the initial C# scripts.
    3. User refines the request (e.g., "Add dice roll for hand size", "Fix UI interaction").
    4. Agent modifies the code and commits changes to Git.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.