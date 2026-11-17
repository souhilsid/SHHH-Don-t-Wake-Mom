# BOT AI

Utility AI per decision (after a human-like reaction delay):
1. Mom almost awake → freeze & shush (probability = NoiseAwareness).
2. Visible trap within 3.5 m (clue shown ≥ 1.2 s) → disarm for a rescue (RescueChance).
3. Sabotage after cooldown (SabotageChance): place a noisy item precariously beside the leading rival's likely target (always leaves the wobble clue = fair counterplay), or hide a rival-needed item to another valid reachable spot. Never undoes completed work, never targets humans specifically.
4. Chores: utility = points/step ÷ (travel + work + 1) × (1 − risk cost) × (1 − contention) + judgement noise; quiet-only when Mom is suspicious.
Bodies walk the NavMesh, carry real props, and set them down physically; careless set-downs (1 − Carefulness) clack or slip, producing real attributed noise.

| Preset | Speed | Reaction | Careful | NoiseAware | Mistakes | Sabotage |
|---|---|---|---|---|---|---|
| Rookie | 0.78 | 1.3–2.4 s | 0.70 | 0.35 | 0.18 | 0.03 |
| Normal | 0.90 | 0.8–1.6 s | 0.85 | 0.70 | 0.09 | 0.06 |
| Clever | 1.00 | 0.5–1.0 s | 0.93 | 0.95 | 0.04 | 0.10 |
Debug: `debug_room` editor command prints each bot's current goal and body state.
