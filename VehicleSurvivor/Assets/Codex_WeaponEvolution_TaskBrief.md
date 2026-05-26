# Codex Task Brief: Weapon Evolution System

Encoding note: This file is plain UTF-8 Markdown. It avoids tables and special formatting so Codex can read it safely.

## 1. Goal

The current goal is to implement the weapon evolution system for the Unity project TeamProject_May.

Before writing new code, analyze the current weapon-related code and explain the current situation.

After the analysis, suggest the safest next implementation steps.

Do not refactor unrelated systems unless they directly block weapon evolution.

## 2. Project Context

TeamProject_May is a cute 3D Vampire Survivors-like game.

Core gameplay loop:

- Player selects a character.
- The run starts with the selected character and a starting weapon.
- Player defeats enemies automatically using weapons.
- Player gains experience and levels up.
- On level up, player chooses weapons or passive stats.
- Weapons can be upgraded.
- Weapons can evolve when their conditions are met.

Important design direction:

- Character data and weapon data should be separated.
- A character may have a recommended starting weapon, but the weapon is not exclusive to that character.
- Weapons, passives, level data, and evolution conditions should be data-driven.
- Weapon data is expected to live mainly in WeaponDefinitionSO or a similar weapon definition asset.

## 3. Weapon Evolution Rule

The intended evolution rule is simple:

- A normal weapon can evolve into an evolved weapon.
- The normal weapon must reach max level.
- Current design uses weapon level 5 as the max level.
- The player must also own the required passive stat.
- If both conditions are satisfied, the weapon becomes eligible for evolution.

Recommended first implementation rule:

- Check whether the player owns the required passive.
- Do not require the passive to be max level unless the existing code or design document already says so.
- If the normal weapon has no evolutionWeaponId or no requiredPassiveId, it cannot evolve.
- Evolution should replace or upgrade the owned normal weapon into the evolved weapon.

## 4. Expected Data Fields

WeaponDefinitionSO or the equivalent weapon data should include these fields or equivalent properties:

- weaponId
- weaponName
- description
- icon
- weaponPrefab
- weaponType
- tags
- levelDataList
- evolutionWeaponId
- requiredPassiveId

The important fields for evolution are:

- weaponId: ID of the current normal weapon.
- levelDataList: used to determine max level.
- evolutionWeaponId: ID of the evolved weapon to grant or replace with.
- requiredPassiveId: ID of the passive required for evolution.

## 5. Evolution Eligibility Logic

Use this as the intended behavior when reading the code:

```text
CanEvolveWeapon(ownedWeapon):
    weaponDefinition = ownedWeapon.definition

    if weaponDefinition.evolutionWeaponId is empty:
        return false

    if weaponDefinition.requiredPassiveId is empty:
        return false

    if ownedWeapon.level is lower than weaponDefinition max level:
        return false

    if player does not own the required passive:
        return false

    return true
```

Max level can be calculated in one of these ways:

- Prefer an explicit max level if the project already has one.
- Otherwise, use levelDataList.Count.
- In the current design, max level is expected to be 5.

## 6. When Evolution Should Be Checked

When analyzing the current code, check where weapon selection and upgrades happen.

Likely candidates:

- Level-up reward selection
- Weapon upgrade handling
- Passive acquisition handling
- Chest or special reward handling, if such a system exists

Recommended first implementation:

- Add a reusable evolution eligibility check.
- Call it after weapon level-up and after passive acquisition.
- If the project has a reward selection system, mark eligible weapons as evolvable in the reward UI.
- Actual evolution can happen either immediately or through a reward/chest system, depending on current project flow.

If the current project does not yet have chest rewards, immediate evolution after both conditions are met is acceptable as a first simple version.

## 7. Weapon To Evolution Mapping

Use the current design mapping below when checking data assets.

Basic recommended weapons:

- Carrot Cannon evolves into Rabbit Strike.
  Required passive: Attack Increase.

- Shell Spin evolves into Turtle Tornado.
  Required passive: Defense Increase.

- Fire Line evolves into Backdraft.
  Required passive: HP Increase.

- Forest Friend evolves into Forest Friend Parade.
  Required passive: Item Pickup Radius Increase.

- Gift Bomb evolves into Merry Criti-Boom.
  Required passive: Move Speed Increase.

- Repulsor Beam evolves into Arc Overdrive.
  Required passive: Cooldown Reduction.

- Bear Paw Shockwave evolves into Bear Quake.
  Required passive: Skill Size Increase.

- Crown Boomerang evolves into Royal Road.
  Required passive: HP Increase.

- Dino Roar evolves into Jurassic Roar.
  Required passive: Skill Size Increase.

Common weapons:

- Block Trap evolves into Barefoot Hell.
  Required passive: Move Speed Increase or Skill Size Increase.
  Note: This condition is still ambiguous in the design. Pick one consistent rule before implementation.

- Gummy Bear Drop evolves into Bear Shower.
  Required passive: Skill Size Increase.

- Bouncy Ball evolves into Super Bouncy Ball.
  Required passive: Critical Chance Increase.

- Static Electricity evolves into Spark Mayhem.
  Required passive: Cooldown Reduction.

- Big Firework evolves into Festival Explosion.
  Required passive: Experience Gain Increase.

- Soap Bubble evolves into Bubble Bumper.
  Required passive: Defense Increase.

- Toy Satellite evolves into Space Mayhem.
  Required passive: Cooldown Reduction.

- Dream Aurora evolves into Sleeping Starlight.
  Required passive: Max HP Increase.
  Note: If the code only has HP Increase, treat Max HP Increase and HP Increase as the same passive category unless the project separates them.

- Paper Airplane evolves into Origami Expert.
  Required passive: Move Speed Increase.

- Yoyo evolves into Rainbow Yoyo.
  Required passive: Skill Size Increase.

- Twinkle Shot evolves into Twinkle Parade.
  Required passive: Critical Chance Increase.

- Rudolph Sleigh evolves into Rudolph Rush.
  Required passive: Move Speed Increase.

## 8. Passive Stats Relevant To Evolution

The current passive stats are expected to include:

- Attack Increase
- HP Increase or Max HP Increase
- Defense Increase
- Cooldown Reduction
- Move Speed Increase
- Critical Chance Increase
- Projectile Count Increase
- Skill Size Increase
- Item Pickup Radius Increase
- Experience Gain Increase

Projectile Count Increase is mainly a weapon strengthening passive. It is not currently listed as a required evolution condition for a specific weapon.

## 9. What Codex Should Inspect First

Read the current Unity code and identify the actual classes and responsibilities.

Focus on these areas:

- WeaponDefinitionSO or equivalent weapon data class
- WeaponLevelData
- Weapon runtime instance class, if present
- Player weapon inventory or weapon manager
- Passive stat inventory or passive manager
- Level-up reward selection code
- UI code that displays weapon cards or passive cards
- Any code that already references evolutionWeaponId or requiredPassiveId

Do not assume class names if the project uses different names. Find the actual code first.

## 10. Questions Codex Should Answer After Reading Code

After inspecting the current code, answer these questions:

1. Where is the player's owned weapon list stored?
2. Where is the player's owned passive list stored?
3. Where does weapon level-up happen?
4. Where does passive acquisition or passive level-up happen?
5. Is WeaponDefinitionSO already connected to runtime weapon instances?
6. Are evolutionWeaponId and requiredPassiveId already present and exposed?
7. Is there already a central database or resolver that can find a WeaponDefinitionSO by weaponId?
8. Is there a reward UI flow where evolvable weapons can be shown?
9. Should the first version evolve immediately, or wait for a chest/reward selection?
10. What is the smallest safe code change needed to support weapon evolution?

## 11. Suggested Minimal Implementation Direction

Prefer a small, testable implementation.

Recommended components:

- A method that checks whether a weapon can evolve.
- A method that performs the evolution.
- A reliable way to look up the evolved weapon definition by evolutionWeaponId.
- A call site after weapon upgrade.
- A call site after passive acquisition.

Possible method names:

- CanEvolve
- TryEvolveWeapon
- HasRequiredPassive
- GetMaxLevel
- ReplaceWeaponWithEvolution

Keep the implementation simple:

- Do not create a large new architecture unless the current code clearly needs it.
- Do not refactor unrelated weapon firing logic.
- Do not modify character selection unless weapon initialization depends on it.
- Do not hard-code every weapon mapping in runtime code if WeaponDefinitionSO already has evolution fields.

## 12. Expected Output From Codex

Codex should respond with:

- A summary of the current weapon code structure.
- What already exists for evolution.
- What is missing.
- The safest implementation plan.
- The exact files that need changes.
- The order of changes.
- Any design ambiguity found in the current data.

Codex should not start large-scale refactoring before reporting the current state.
