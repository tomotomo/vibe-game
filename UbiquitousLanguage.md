# Ubiquitous Language (Duel Daifugo Project)

This document defines the common terminology used across the codebase, documentation, and communication for the Duel Daifugo game project.

## 1. Core Entities

*   **Card**: The fundamental playing card entity. Defined by a `Suit` and a `Rank`.
*   **Suit**: The category of the card.
    *   Values: `Spades`, `Hearts`, `Diamonds`, `Clubs`.
*   **Rank**: The numeric face value of the card.
    *   Values: 1 (Ace), 2..10, 11 (Jack), 12 (Queen), 13 (King).
*   **Strength**: The power level of a card used for comparison in the game logic.
    *   Order (Weakest to Strongest): 3 < 4 < ... < K(13) < A(1) < 2.
    *   Note: In `Revolution` state, the order is inverted.
*   **Hand**: The collection of cards currently held by a player (Player or CPU).
*   **Field**: The central area where cards are played.
*   **Deck**: The full set of 52 cards used to deal hands at the start of the game.

## 2. Game Flow & Roles

*   **Game**: A single match of Daifugo, starting from the Title Screen and ending at the Result Screen.
*   **Round**: A sequence of plays starting with a `Lead` player and continuing until the `Field` is cleared (via `Pass` or `8-Cut`).
*   **Turn**: A single opportunity for a player to play cards or pass.
*   **Lead (Parent/Oya)**: The state of a player starting a new `Round`. They can play any valid card combination.
    *   Code representation: `isNewRound = true`.
*   **Follow (Child/Ko)**: The state of a player responding to an existing play on the `Field`. They must play stronger cards of the same quantity (and matching suit if `Suit Bind` is active).
*   **Pass**: The action of forfeiting a turn. If all other players pass, the `Field` is cleared.
*   **Field Clear**: The event where all cards are removed from the `Field`, ending the current `Round`.
*   **Deal**: The initial distribution of cards to players based on the dice roll sum.

## 3. Game Rules & Events

*   **Revolution**: A persistent game state where card `Strength` is inverted (3 is strongest, 2 is weakest).
    *   Trigger: Playing 4 or more cards of the same rank at once.
*   **J-Back**: A temporary rule effect triggered by playing a Jack (11).
    *   Effect: Temporarily acts as a `Revolution` (or Counter-Revolution) for the current `Round`.
    *   Duration: Ends when the `Field` is cleared.
*   **Effective Revolution**: The actual current strength inversion state, calculated as `IsRevolution XOR IsJBackActive`.
*   **Suit Bind (Shibari)**: A rule triggered when a player plays cards of the same `Suit` as the cards currently on the `Field`.
    *   Effect: Subsequent plays in the current `Round` are restricted to that specific `Suit`.
*   **8-Cut (Eight Cut)**: A rule triggered by playing any card with Rank 8.
    *   Effect: Immediately clears the `Field` (`Field Clear`) and grants the current player the next `Lead`.
*   **5-Skip (Five Skip)**: A rule triggered by playing any card with Rank 5.
    *   Effect: Skips the opponent's turn. The current player plays again immediately against their own card (Field is NOT cleared).

## 4. UI & Screens

*   **Title Screen**: The initial landing screen containing the "START GAME" and "RULES" buttons.
    *   Code: `TitlePanel`.
*   **Rule Screen**: The screen displaying the game rules with a scrollable view.
    *   Code: `RulePanel`.
*   **Game Screen**: The main gameplay interface showing hands, field, and game info.
    *   Code: `GamePanel`.
    *   **Footer**: The bottom area containing action buttons (PLAY, PASS, EXIT).
*   **Result Screen**: The screen shown after a game ends, displaying "YOU WIN"/"CPU WINS" and stats.
    *   Code: `ResultPanel`.
    *   **Buttons**: RETRY, EXIT.
*   **Card View**: The visual representation of a card in the UI. Can be `Selected` (highlighted).

## 5. Features

*   **Smart Play**: A user-assist feature that automatically selects and plays cards when the player taps a card in a `Follow` situation, provided the move is unambiguous (unique valid combination).
*   **CPU AI**: The logic controlling the computer opponent.
    *   Strategy: Prioritizes playing weak cards but conserves special cards (A, 2, 5, 8) if they are the last copy in hand, unless playing them wins the game.
*   **Dice Animation**: A visual sequence at the game start where two dice are rolled to determine the initial hand size.
