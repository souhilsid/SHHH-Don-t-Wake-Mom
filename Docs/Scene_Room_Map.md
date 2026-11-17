# Scene / room map (2026-11-09)

House coordinates = living-room coordinates (metres, +x east, +z north). One prefab `SHHH_WorldHouse`; each room is an offset (`HouseLayout.Offset`
in Core = `HouseAssembly.Center` in Unity). The NavMesh is baked over the whole house; bots and Mom use NavMeshAgent.

| Room | Offset | Size (w×d) | Recipe stations (house space = offset + local) | Hosts recipe areas today | Scene |
|---|---|---|---|---|---|
| Living room | (0, 0, 0) | 7.2 × 6.2 | 4 tables: (−1.65,0.74,−1.25) (1.65,0.74,−1.25) (2.65,0.74,−2.6) (0,0.42,0.9) | Living, Foyer, Hallway | `LivingRoom.unity` (continuous match loads this scene; whole house present) |
| Kitchen + dining | (6.9, 0, 0) | 6.6 × 6.0 | dining table W/E ends, counter ×2 | Kitchen, PetArea | `Kitchen.unity` (legacy/diag) |
| Laundry | (6.4, 0, 6) | 5.6 × 5.4 | worktop, shelf, dryer tops ×2 | Laundry, Bathroom, ChildBedroom | `Laundry.unity` (legacy/diag) |
| Practice | separate | 5 × 5 | 1 | tutorial | `Practice.unity` |
| Mom's room | door in the living room north wall (0, 0, 3.05) | — | — | — | Mom walks out through her door (enclosed upstairs bedroom + stairs is a Gate 2/6 target) |
| Family Stage (menu) | living room (−1.8, 0, 0.7), yaw 126° | — | — | — | Boot |

Continuous match: job k of every seat goes to `HouseLayout.Rooms[k]` (5 min: living + kitchen; 10 min: + laundry). Station indices are
`k*10 + i` (`RoundController.RecipeStations`). Incident text uses the place name ("kitchen", "living room", "laundry room").

Not built yet (Gate 2): child bedroom, bathroom, foyer, hallway/stairs, pet corner, dining chairs set, upstairs Mom bedroom. The 11 recipes that
need them are marked BLOCKED in `Canonical_Recipe_Audit.csv`.
