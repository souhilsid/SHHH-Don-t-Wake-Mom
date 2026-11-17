# NOISE & CAUSALITY

**Noise value** = `9 × loudness(material) × log2(1 + E / 0.35 J)` × distance factor (1.0 within 3 m of Mom's door → 0.45 at 14 m) × round multiplier, capped 85; impacts under 0.08 J are silent. Loudness: soft .05, fabric .12, paper .35, plastic .75, toy .85, wood .9, appliance 1.2, ceramic 1.45, glass 1.6, metal 2.0.
Gameplay noise is computed on the host from physics; audio volume and microphone input never feed it.

**SleepMeter** 0–100: 0–34 deep sleep · 35–64 stirring · 65–84 suspicious · 85–99 almost awake · ≥100 wake. After 4 s without qualifying noise it decays 3/s to the round baseline. Per-object rate limit 0.25 s; events < 0.6 ignored.

**Decisive event**: the event that crosses 100 if it is ≥ 8 (qualifying); otherwise the loudest qualifying event in the last 8 s.

**Responsibility rules** (CausalLedger.Resolve):
1. Direct drop/throw/slam → the releasing/holding sibling (confidence 1.0 within 1.5 s, 0.75 within 3 s).
2. Trap fires with no fair counterplay → trap owner.
3. Trap clue visible ≥ 1.2 s AND victim moved carelessly (> 1.1 m/s) → victim (70 %), owner 30 % (scoring weight).
4. Chain reactions → root actor is principal; deliberate intermediate actors get +0.25 weight.
5. Pet acting alone → nobody: Mom warns, meter → 60, pets calmed 20 s, round resumes.
6. Sibling lured the pet within 8 s → provoker.
7. Ambiguous/no trustworthy chain → warning, never an elimination.
Elimination needs principal confidence ≥ 0.7 and an active sibling. The round latches after the first verdict (exactly one elimination even with simultaneous events).
