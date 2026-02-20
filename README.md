# BlueSlidePark

## Play the Original Game (Patched SWF)

The original Flash game is playable again with a working leaderboard! Download `slide_patched.swf` from this repo and open it with [Ruffle](https://ruffle.rs/downloads) (Flash emulator).

### Quick Start

1. Download [Ruffle desktop](https://ruffle.rs/downloads) for your platform
2. Download `slide_patched.swf` from this repo
3. Open `slide_patched.swf` with Ruffle
4. Enter your name and play — scores are saved to a live leaderboard

### Controls

- **Left/Right Arrow Keys** — Slide left and right
- **Up Arrow** — Speed boost
- **Space / Enter / R** — Restart after death

### How It Works

The original game's server (`macmillerofficial.umgfacebook.com`) has been defunct for years. This patched SWF replaces the dead URLs with a new leaderboard server, so score submission and the leaderboard work again. The server code is in the `server/` folder.

### Leaderboard Server

The leaderboard server matches the original API and is hosted on Heroku. To run it locally:

```bash
cd server
npm install
npm start
```

Then open http://localhost:3000 to play in-browser via Ruffle, or re-patch the SWF for localhost:

```bash
python server/patch_swf.py localhost:3000
```

## About

The original Blue Slide Park game was a promotional Flash game built by PLA Studios for Universal Music Group, released alongside Mac Miller's debut album "Blue Slide Park" in 2011-2012. It was hosted at `macmillerofficial.com/BlueSlidePark-game/` and ran as a Unity 3.5 project compiled to Flash via cil2as.

## Unity 6 Recreation (WIP)

This project is also recreating the game in Unity 6 (URP) using assets and game logic extracted from the original SWF files. This is a side project and is not yet complete.

## Original Game Videos

- https://www.youtube.com/watch?v=TgzycW1xaJs
- https://www.youtube.com/watch?v=pRylDIfK8gM

---

## SWF Extraction Findings

All data below was extracted from the original compiled SWF binary (`slide~.swf`, 43MB uncompressed FWS format).

### Server Endpoints (defunct)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `http://macmillerofficial.umgfacebook.com/post_scores.php` | POST | Score submission (user_id, score) |
| `http://macmillerofficial.umgfacebook.com/request.php` | GET | Leaderboard fetch (?score=X&name=Y&hash=Z) |
| `http://zaphod.uk.vvhp.net/vvreg/6896-219204` | GET | Prize claiming (iframe for scores >= 1500) |
| `http://umgcdn.vnetrix.com/slide.swf` | GET | CDN for game SWF |

### Anti-Cheat Hash System

**Leaderboard Fetch (request.php)**
- Secret salt: `a1e902e` (7 chars, embedded in HTTPPoke.as)
- Hash formula: `SHA1(score + name + "a1e902e")`
- Sent as `&hash=` parameter for server-side validation

**Score Submission (post_scores.php)**
- POST parameters: `user_id` (Facebook ID), `score` (best score)
- Possible signing salt: `6OEIr6ZkYm` (10 chars, found adjacent to user_id in PostScore bytecode)

**Leaderboard Response Format:**
```
rank:{n}\tname:{name}\tscore:{score}\n
```

**Why it was weak:** The salt was hardcoded in the SWF client — anyone who decompiled the Flash file could discover `a1e902e` and forge valid hashes. Typical of early 2010s web games.

### Death Screen Scoring Tiers

The original game had 8 Mac Miller-themed score tiers displayed on the death screen:

| Tier | Field Name | Estimated Threshold |
|------|-----------|-------------------|
| Most Dope | `DeadMenu$mostdopeTex$` | 5000+ |
| My Team | `DeadMenu$myteamTex$` | 3500+ |
| My Homie | `DeadMenu$myhomieTex$` | 2500+ |
| My Dude | `DeadMenu$mydudeTex$` | 1500+ |
| Dope | `DeadMenu$dopeTex$` | 1000+ |
| Weather | `DeadMenu$weatherTex$` | 500+ |
| Not Bad | `DeadMenu$notbadTex$` | 200+ |
| Loser | `DeadMenu$loserTex$` | 0+ |

Each tier had a corresponding texture and threshold value. Prize claiming was available for scores >= 1500 ("My Dude" tier and above).

### Original Game Classes

| Class | Purpose | Key Fields |
|-------|---------|------------|
| **SlideController** | Slide generation & management | PieceNumber, Pieces, OddColour, EvenColour, bonusScore, StartingPieces, PiecesAtOnce, victoryBall, victoryBallOffset, TurnCooldown, treePrefabs |
| **SliderMovement** | Player movement & physics | Speed, Dead, immortal, lean, leanLerp, dampedRotZ, dampLerp, settleFactor, deathAngle, danceAngle, timeTilBoostEnd, pickupSound, deathSound, slideNoise |
| **VictoryBall** | Pickup collectibles | rotSpeed, flyUpSpeed, pickupPos, flyUp, model + ShowPickup(), Die() |
| **ScoreManager** | Score tracking | score, bestScore, userID + GameOver(), SetScore(), UploadScore(), SubmitScore() |
| **DeadMenu** | Death/game over screen | restartButton, scoreDisplay, bestScoreDisplay, statusText, wonPrize, winPrize, restartSound + 8 tier textures/thresholds |
| **StartMenu** | Main menu | startButton, startButtonHover, haveName, playerName, startSound, buttonHoverSound |
| **TextEntry** | Name input | text, enterText, textMaxLength (20), cursorOn, blinkRate |
| **CamFollow** | Camera controller | target, positionDampTime, rotationDampTime, lerpXfactor, lerpYfactor, lerpZfactor, tiltUp |
| **Leaderboard** | Leaderboard display | entries, leaderboardScores + FetchLeaderboard(), ProcessLeaderboard(), WriteLeaderboard() |
| **LeaderboardEntry** | Single entry | name, score, rank, us |
| **Music** | Background music | songs (array), source, instance (singleton) + PlaySong() |
| **SoundManager** | Sound effects | Instance (singleton) |
| **HTTPPoke** | Network requests | isDone, fetchURL, response + FetchLeaderboardAS() |
| **ExternalCall** | JavaScript bridge | PostScore() |
| **SlidePiece** | Individual slide segment | Direction, Offset, YRotationAmount |
| **ObsticlePosition** | Obstacle placement | Offset |
| **Numbers** | Score display | displayedNumber + DisplayNumber() |
| **PlayIdleAnimations** | Idle animation system | mAnim, mIdle, mBreaks, mNextBreak, mLastIndex |
| **LoadLevelOnClick** | Level loader | levelName |

### Asset Names from SWF

| Asset | Description |
|-------|-------------|
| `ICE_CREAM` | Ice cream pickup texture |
| `MAC_TREE_TEXTURE` | Tree texture |
| `TREE_001`, `TREE_002`, `TREE_003` | Tree prefab variants |
| `MAC_TREE_001`, `MAC_TREE_002`, `MAC_TREE_003` | Mac-branded tree variants |
| `MAC_MILLER_BOY`, `MAC_MILLER_BOY_009` | Player character model |
| `Obsticle` (sic) | Obstacle prefab |
| `PlayerObj` | Player object |
| `PlayerDeathCollider` | Death trigger collider |

### Animation States

| Animation | Context |
|-----------|---------|
| `Start Level` | Scene load animation |
| `Fail Level` | Death animation |
| `Slide Loop` | Main gameplay loop |
| `Slide Left` / `Slide Right` | Player sliding animations |
| `Mouse Over Start` / `START_HOVER` | Menu button hover |
| `ThumbUp` | Thumbs up pickup collection flash |

### Sound System

| Reference | Location |
|-----------|----------|
| `SliderMovement$pickupSound$` | Pickup collection SFX |
| `SliderMovement$deathSound$` | Death SFX |
| `SliderMovement$slideNoise$` | Continuous slide ambient |
| `StartMenu$startSound$` | Game start SFX |
| `StartMenu$buttonHoverSound$` | Menu hover SFX |
| `DeadMenu$restartSound$` | Restart SFX |
| `SOUND_ON2`, `SOUND_OFF`, `SOUND_ROLLOVER` | Mute toggle UI states |

### Social Integration

- `setTweetLinks(score)` — JavaScript function for Twitter sharing with score
- `claimPrize` — Opens prize claiming iframe at `zaphod.uk.vvhp.net`
- Facebook user ID fetched via JavaScript `FetchUserID()` / `FetchName()`
- Google Analytics: `UA-31926838-1`

### Developer Info

- **Developer:** PLA Studios (plastudios.com)
- **Publisher:** Universal Music Group
- **Built:** May 24, 2012
- **Engine:** Unity 3.x compiled to Flash via cil2as
- **Original URL:** `http://www.macmillerofficial.com/BlueSlidePark-game/`

---

## Decompiled Source Code (AS3 from JPEXS)

The original game scripts were decompiled from the SWF using JPEXS Free Flash Decompiler. The SWF contained Unity 3.5.2f2 assets compiled to Flash via cil2as. All scripts are in ActionScript3 format (`scripts/global/*.as`).

### Key Discoveries from Decompiled Code

**Movement System (SliderMovement.as):**
- Speed starts at 3, dynamically calculated: `min(10, 6 + (Time.time - startTime) / 10)` — ramps from 6 to 10 over 40 seconds
- UpArrow gives 0.3-second speed boost of +10
- `dampLerp = 0.3`, `speedDampLerp = 0.3`, `leanLerp = 0.2` (originally guessed higher)
- Player uses `RotateAround` with rotation origin `(0, 0, 10)` and `RotationSpeed = 20`
- Settle formula: `RotationAmount = (settle * settleFactor + Input.Horizontal) * RotationSpeed * deltaTime`
- Animations: `idle`, `stupiddance` (layer 1), `leftlean`/`rightlean`/`leftloop`/`rightloop` (layer 2, Additive blend, ClampForever)
- Death when euler Z exceeds `deathAngle`; `danceAngle` triggers "stupiddance" animation weight

**Slide Generation (SlideController.as):**
- `StartingPieces = 14`, `PiecesAtOnce = 30`, `Offset = 30`
- `TurnCooldown = 10`, `TurnDirectionCount = 10`
- Trees: 1/6 chance per piece, placed 30-100 units left or right, +10 units up
- Obstacles: 1/10 chance, child rotated -60 to 60 degrees around forward
- VictoryBalls: 1/6 chance (mutually exclusive with obstacles)
- Score formula: `PieceNumber - StartingPieces + bonusScore`
- Turn direction: count resets to Random(20,30), isLeft = Random(1,3)==1 (1/3 left)

**Scoring (ScoreManager.as):**
- Score set by SlideController: `PieceNumber - StartingPieces + bonusScore`
- VictoryBall collection: `bonusScore += 100` (not 25/50)
- Best score via PlayerPrefs "BestScore"
- playerName and userID fields for Facebook integration

**Death Screen (DeadMenu.as):**
- Uses **strictly greater-than** (`>`) for tier comparisons, not `>=`
- Prize claiming at score `>= 1500` (separate condition from tiers)
- Restart via: Space, Enter, KeypadEnter, R key, or button click
- Calls `ExternalCall.Eval("setTweetLinks(" + score + ");")` on death

**SoundManager.as:**
- Surprisingly simple: singleton with `DontDestroyOnLoad`
- `Update()`: positions itself at `Camera.main.transform.position` each frame
- No audio clip fields — sounds were stored on individual classes (SliderMovement, StartMenu, DeadMenu)

**VictoryBall.as:**
- Model child found by name: `transform.Find("ICE_CREAM")`
- On trigger with "PlayerObj": `bonusScore += 100`, play pickupSound, `flyUp = true`, `Invoke("Die", 5)`
- ShowPickup: instantiates `pickupParticle` at `pickupPos`, parents to UICamera

**TextEntry.as:**
- Manual key-by-key input: only A-Z keys + Space (all uppercase)
- Cursor blink via `InvokeRepeating("CursorOn", 0, blinkRate)` and `InvokeRepeating("CursorOff", blinkRate*0.5, blinkRate)`
- Saves to PlayerPrefs "playerName"

**ObsticleCollider.as (important!):**
- Uses **layer-based** collision detection, not tags!
- `if (other.gameObject.layer == LayerMask.NameToLayer("PlayerDeathCollider"))` → `SendMessageUpwards("Die")`

**MuteQuality.as (previously unknown):**
- Mute toggle: `AudioListener.pause`, `AudioListener.volume = 0`, mutes all AudioSources
- Quality toggle: directional light `shadows = None` (low) vs `Hard` (high)
- Persisted via PlayerPrefs "Muted" and "Low Quality"
- On/Off/Rollover textures for both buttons

**Music.as:**
- Sets `source.loop = true` on each song AND advances index
- Songs playlist with sequential looping

**ObsticlePosition.as:**
- Default Offset: `(0, 12, 0)` — obstacles positioned 12 units above slide

**PlayIdleAnimations.as:**
- Base idle clip (layer 0), break clips starting with "idle" (layer 1)
- Break intervals: single break = 5-15s, multiple breaks = 2-8s

### UI Framework
- **NGUI** (Next-Gen UI) was the original UI framework
- Knewave Google Font for all game text
- `nameentry` UILabel reference

### Embedded Binary Assets (in SWF DefineBinaryData)
| chId | Container Name | Content |
|------|---------------|---------|
| 3 | `com.unity.UnityNative_dataSegmentBytes` | Native code |
| 4 | `ProjectSerializedFileContainer_Resources_unity_default_resources` | Default resources |
| 5 | `ProjectSerializedFileContainer_sharedassets0_assets` | Scripts, audio (MP3/LAME), fonts (Knewave), UI |
| 6 | `ProjectSerializedFileContainer_mainData` | Scene data, meshes, character model |
| 7 | `ProjectSerializedFileContainer_resources_assets` | Additional resources |

The sharedassets0 file (15.9 MB) has a valid Unity 3.5.2f2 header. Character model (`MAC_MILLER_BOY`) is likely in mainData (chId 6).

---

## Unity 6 Recreation — Implemented Features

### Scripts

| Script | Matches Original | Description |
|--------|-----------------|-------------|
| `MacController.cs` | SliderMovement | Player movement with lean, settle, death, immortal debug flag |
| `WorldMover.cs` | SlideController (partial) | Slide scrolling and segment recycling |
| `SlideController.cs` | SlideController | Slide config singleton (colors, piece counts, bonus scores) |
| `ScoreManager.cs` | ScoreManager | Distance + bonus scoring, best score persistence |
| `DeadMenuManager.cs` | DeadMenu | 8-tier death screen with Mac Miller-themed ranks |
| `LeaderboardManager.cs` | Leaderboard + HTTPPoke + ExternalCall | Full leaderboard client with SHA1 hash |
| `SoundManager.cs` | SoundManager | Centralized SFX (pickup, death, slide noise, UI sounds) |
| `MusicManager.cs` | Music | Background music playlist with auto-advance |
| `PickupSpinner.cs` | VictoryBall | Spin + bob idle animation, fly-up on collection |
| `PickupEffect.cs` | — | Particle burst on pickup collection |
| `TreeGenerator.cs` | SlideController.treePrefabs | Procedural tree generation along slide |
| `SlideMeshGenerator.cs` | SlidePiece | Curved U-shaped slide mesh generation |
| `CamFollow.cs` | CamFollow | Smooth camera follow with per-axis damping |
| `PlayIdleAnimations.cs` | PlayIdleAnimations | Idle animation with random break animations |
| `ObstaclePosition.cs` | ObsticlePosition | Obstacle/pickup positioning on slide |
| `NumbersDisplay.cs` | Numbers | Animated number counting display |
| `UIController.cs` | StartMenu + TextEntry | Name entry with blinking cursor, "YOUR NAME HERE" placeholder |
| `InGameUI.cs` | — | Animation trigger bridge (ThumbUp flash) |
| `MuteQuality.cs` | MuteQuality | Mute toggle + quality/shadows toggle with PlayerPrefs |
| `BillboardSprite.cs` | — | Camera-facing sprite utility |

### Scene Setup

**Level01.unity:**
- GameController with WorldMover, SlideMeshGenerator, ScoreManager, TreeGenerator, SoundManager, LeaderboardManager, SlideController
- 7 scrolling slide segments with curved U-shape mesh
- 5 pickup/obstacle objects (2 Poop/Obstacle, 2 IceCream/Pickup, 1 ThumbsUp/Pickup)
- Death panel with tier title, final score, best score, retry prompt
- Mac player character with physics-based movement

**MainMenu.unity:**
- "YOUR NAME HERE" placeholder text (matches original TextEntry)
- Name entry with 20 char max, blinking cursor
- Press Space to start after entering name
