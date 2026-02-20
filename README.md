# BlueSlidePark

Mac Miller's Blue Slide Park game — recreated in Unity 6 from the original 2012 Flash game.

## About

The original Blue Slide Park game was a promotional Flash game built by PLA Studios for Universal Music Group, released alongside Mac Miller's debut album "Blue Slide Park" in 2011-2012. It was hosted at `macmillerofficial.com/BlueSlidePark-game/` and ran as a Unity 3.5 project compiled to Flash via cil2as.

This project recreates the game in Unity 6 (URP) using assets and game logic extracted from the original SWF files.

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
