# AppraisalAI

A Unity 6 creature simulation featuring a custom cognitive architecture for autonomous decision-making and reinforcement learning.

## Overview

An AI creature with survival needs (energy, hunger) navigates a 3D environment, seeking food and fleeing enemies. Its decisions are driven by a **case-based reasoning + differentiable neural network** hybrid called the "Decision Brain."

## How It Works

### Decision Brain
A three-layer network that drives behavior selection:
- **Input Nodes** — Raw sensor values (hunger, energy, enemy proximity, etc.)
- **Feature Nodes** — Higher-level abstractions computed from inputs (e.g., `MetabolicStress`, `RecoveryNeed`)
- **Behavior Nodes** — Competing behavior proposals (Wander, SeekFood, FleeEnemy, Idle) with weighted, learnable connections from features

### Behavior Cloud
A nearest-neighbor memory of past experiences. Each `BehaviorRecord` stores a coordinate point in state space mapped to a behavior payload. The creature queries this cloud to find the most relevant past behavior for its current situation.

### Payload Filters
Behavior records can define conditional filters on sensor values (e.g., "only flee if energy > 0.3") that gate whether a behavior is eligible.

### Reinforcement Learning
Episodic RL that traces which input/feature nodes influenced a chosen behavior, evaluates reward at episode end (based on signal deltas like food consumed or energy gained), and adjusts connection weights via learned offsets.

### Sensors
Proximity detection for food/enemy targets, state drift for internal needs (hunger, energy), and a spatial awareness system via `WorldTargetRegistry`.

## Tech Stack
- Unity 6
- Universal Render Pipeline
- C# custom AI framework (no ML libraries)
