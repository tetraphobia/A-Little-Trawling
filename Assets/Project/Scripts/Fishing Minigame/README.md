# Fishing Minigame — Setup

## How it plays
- The **fish** is controlled directly with the arrow keys (WASD also works — they're bound to the same default axes).
- The **rod** is a fixed point in the top-right corner. Get the fish within `catchDistance` of it to win.
- **Debris** is scattered randomly around the rectangle and drifts slowly. Touching it knocks the fish back and costs one "hit" — after `maxHits` the line snaps and you lose.
- Press **R** after winning/losing to restart.

## Scene hierarchy to build

```
PlayArea            (BoxCollider2D, script: PlayAreaBounds)
Rod                 (positioned top-right, script: FishingLineVisual, LineRenderer)
Fish                (positioned bottom-left, Rigidbody2D, CircleCollider2D, script: FishController)
DebrisSpawner       (script: DebrisSpawner)
GameManager         (script: FishingGameManager)
```

### 1. PlayArea
- Create an empty GameObject named `PlayArea`.
- Add a **Box Collider 2D**, resize it in the Scene view to be your rectangle (e.g. width 10, height 6).
- Add the `PlayAreaBounds` script. It forces the collider to be a trigger automatically.
- (Optional) Add a child sprite/quad the same size for a visible background.

### 2. Rod
- Create an empty GameObject named `Rod`, position it near the top-right corner of the rectangle (leave a little padding, e.g. 0.5–1 unit from the edge).
- Add a **Line Renderer** component and the `FishingLineVisual` script.
  - On the Line Renderer, assign a material (e.g. built-in `Sprites-Default`) or the line won't be visible.
  - Drag `Rod` into the script's `Rod` field and `Fish` into its `Fish` field (after step 3).
- (Optional) Add a child sprite for the rod graphic.

### 3. Fish
- Create a GameObject named `Fish`, position it near the bottom-left corner.
- Add a **Sprite Renderer** (any placeholder sprite works).
- Add a **Rigidbody2D** — the script sets it to Kinematic automatically, no need to configure it.
- Add a **Circle Collider 2D**, check **Is Trigger**.
- Add the `FishController` script, and drag `PlayArea` into its `Play Area` field.

### 4. DebrisSpawner
- Create an empty GameObject named `DebrisSpawner`.
- Add the `DebrisSpawner` script.
- Assign `PlayArea`, `Rod`, and `Fish` (as `Fish Start`) in the Inspector.
- Leave `Debris Prefab` empty to use the auto-generated placeholder circles, or assign your own prefab (it just needs a SpriteRenderer/Collider2D — the spawner fills in anything missing).
- Tune `Debris Count`, `Safe Radius` (clear zone around rod/fish), and `Min Spacing` as needed.

### 5. GameManager
- Create an empty GameObject named `GameManager`.
- Add the `FishingGameManager` script.
- Assign `PlayArea`, `Rod`, and `Fish`.
- (Optional) Create a Canvas > Text (or TMP) for status messages and assign it to `Status Text`.

## One required manual step: the "Debris" tag
Unity requires tags to be pre-declared. Go to **Edit > Project Settings > Tags and Layers**, and add a tag named exactly `Debris`. The scripts assign this tag automatically at runtime, but it will throw an error if the tag doesn't already exist in the project.

## Tuning knobs
- `FishController`: `moveSpeed`, `maxHits`, `knockbackForce`, `knockbackDuration`, `edgePadding`
- `DebrisSpawner`: `debrisCount`, `debrisRadius`, `safeRadius`, `minSpacing`
- `Debris`: `drifts` (toggle off for stationary obstacles), `driftSpeed`
- `FishingGameManager`: `catchDistance`

## Easy extensions
- Add a timer / score based on time-to-catch.
- Swap the knockback-hit system for a shrinking "line tension" bar instead of a hit counter.
- Multiple fish per level, or a fish that itself drifts/resists being pulled.
- Sound/particle effects hooked into `FishingGameManager.RegisterHit()`.

## Note on input
This uses Unity's legacy Input Manager (`Input.GetAxisRaw`), which is enabled by default in most projects and already maps Horizontal/Vertical to the arrow keys. If your project uses the newer Input System package exclusively, set **Player Settings > Active Input Handling** to "Both," or swap the input lines in `FishController.HandleMovement()` for the new Input System API.
