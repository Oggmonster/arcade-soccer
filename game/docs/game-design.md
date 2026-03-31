# Arcade 5v5 Soccer — Game Design Document

## Working title

**Pocket Pitch**

## High concept

A fast, simple 5v5 arcade soccer game inspired by the feel of classic NES *Ice Hockey*: easy to learn, quick matches, exaggerated player strengths, and direct controls. Each team is built from three player types — **Big**, **Medium**, and **Small** — creating light team strategy before the match and sharp role differences during play.

## Design pillars

1. **Arcade first** — responsive, readable, and fun within seconds.
2. **Simple controls, meaningful choices** — move, pass, shoot on offense; move, stand tackle, slide tackle on defense.
3. **Player type identity** — Big, Medium, and Small should feel clearly different.
4. **Short match loop** — ideal match length: 3–5 minutes.
5. **Readable chaos** — busy but understandable, even with 10 players on the field.

## Target platform

PC first.

## Engine and tech

* Engine: Godot with C#
* Editor workflow: Godot editor + VS Code
* Perspective: top-down or slightly angled top-down 2D
* Initial scope: local single-player vs CPU

## Core inspiration

* Classic console sports pacing
* NES *Ice Hockey* team composition idea
* Simplified soccer rules rather than full simulation

## Core gameplay loop

1. Choose teams / lineups.
2. Kickoff starts match.
3. On offense, control the player with the ball.
4. Pass or shoot to create chances.
5. On defense, switch to nearest defender automatically or with a switch rule, then tackle to regain possession.
6. Score more goals before time expires.
7. Return to results screen and optionally rematch.

## Camera and presentation

* 2D field view
* Camera follows ball with soft smoothing
* Players remain clearly readable at all times
* Strong UI indicators for:

  * controlled player
  * ball owner
  * shot power
  * score and time

## Game rules

### Match format

* 5 players per team on the field
* 2 halves or 1 short timed match for prototype
* Suggested prototype length: 2 minutes total
* Highest score wins

### Soccer rules kept

* Goals
* Kickoff after goal
* Simple possession changes

### Soccer rules simplified or removed for prototype

* No offsides
* No fouls/cards at first
* No throw-ins/corners at first; ball out of bounds can reset to nearest kick-in or simple possession restart
* No goalkeeper manual control in version 1 unless needed

## Team composition

Each team fields 5 players, mixing these types:

### Big player

**Role:** power / physical control
**Traits:**

* slowest movement
* strongest shot
* strongest tackle
* best at holding possession under pressure
* largest collision body
* slower recovery after slide

### Medium player

**Role:** balanced all-rounder
**Traits:**

* average speed
* average shot
* average pass
* average tackle
* easiest for new players to use

### Small player

**Role:** speed / agility / playmaking
**Traits:**

* fastest movement
* quickest turning
* weakest shot power
* weakest tackle strength
* best dribbling responsiveness
* smallest collision body

## Example team compositions

* 2 Big / 2 Medium / 1 Small
* 1 Big / 3 Medium / 1 Small
* 1 Big / 1 Medium / 3 Small

## Moment-to-moment controls

### Offense

* **Move:** joystick or arrow keys
* **Pass button:** short pass in movement direction or toward best teammate target
* **Shoot button:** kick toward goal; holding increases power if charge is used

### Defense

* **Move:** joystick or arrow keys
* **Stand tackle button:** quick safe challenge, short range
* **Slide tackle button:** longer reach, higher commitment, longer recovery

## Control philosophy

* Same move input on offense and defense
* Two action buttons only
* Low button complexity, high positional decision-making

## Possession and player control

### On offense

* User controls the player who has the ball
* If a teammate receives a pass, control transfers immediately to that teammate

### On defense

* Control nearest defender to the ball by default
* Optional later feature: manual player switch button

## Ball systems

### Dribbling

* Ball sticks closely to the controlled player while in possession
* Movement slightly affects ball push distance
* Small players keep tighter control at speed
* Big players push ball farther ahead while moving

### Passing

* Ground pass only for prototype
* Pass chooses intended teammate by directional cone + distance weighting
* Fast enough to feel snappy, but interceptable by defenders

### Shooting

* Direction based on player facing / input vector
* Accuracy and power vary by player type
* Optional shot charge for extra depth

## Tackling systems

### Stand tackle

* Short forward hitbox
* Lower risk
* Can poke ball loose
* Best when timed close to ball carrier

### Slide tackle

* Burst forward over short distance
* Larger hitbox
* Good for cutting passing lanes or emergency defense
* Miss leaves player vulnerable during recovery

## Goalkeeper approach

For the prototype, goalkeeper behavior should be **automatic AI only**.

### Goalkeeper prototype behavior

* stays near goal center
* moves horizontally/vertically within a restricted box
* attempts save when shot enters save zone
* catches or deflects based on shot power and angle

## AI design

### Teammate AI on offense

* spread into lanes
* avoid clustering
* move into passable space
* one player supports close, another runs wider/deeper

### Defender AI

* nearest defender pressures ball
* others mark nearby attackers or cover space between ball and goal
* avoid all defenders chasing the ball

### Goalkeeper AI

* tracks ball position
* anticipates shot line simply

## Prototype AI constraints

Keep AI intentionally simple:

* state-driven, not advanced tactics
* prioritize readability over realism

## Player stats

Use small numeric ranges for easier tuning.

### Suggested prototype stats

* Speed: 1–5
* Acceleration: 1–5
* Shot Power: 1–5
* Shot Accuracy: 1–5
* Pass Accuracy: 1–5
* Tackle Strength: 1–5
* Ball Control: 1–5
* Recovery: 1–5

### Archetype stat example

| Type   | Speed | Accel | Shot Power | Accuracy | Pass | Tackle | Control | Recovery |
| ------ | ----: | ----: | ---------: | -------: | ---: | -----: | ------: | -------: |
| Big    |     2 |     2 |          5 |        3 |    3 |      5 |       3 |        2 |
| Medium |     3 |     3 |          3 |        3 |    3 |      3 |       3 |        3 |
| Small  |     5 |     5 |          2 |        3 |    4 |      2 |       5 |        4 |

## Controls for keyboard prototype

* Move: Arrow keys
* Pass: Z
* Shoot / Slide: X
* On defense, Z = stand tackle, X = slide tackle
* Pause: Enter or Esc

## Visual style

* Clean arcade sprites
* Strong silhouettes by body type
* Bright team colors
* Minimal but readable field markings
* Clear shadow / outline under active player

## Audio direction

* punchy kick sounds
* tackle thumps
* crowd cheer on goals
* short retro-inspired music loop

## User interface

### Main menu

* Start Match
* Team Select
* Options
* Quit

### Team select

* Choose team color/name
* Assign 5 player body types
* Preview strengths and weaknesses

### In match HUD

* Score
* Match timer
* Team colors
* Controlled player marker
* Optional shot charge meter

### Results screen

* Final score
* Goal scorers or simple summary
* Rematch
* Return to menu

## Win/lose and scoring feedback

* Goal freeze for 0.5–1 second
* Quick celebration animation
* Reset players to kickoff spots

## Difficulty tuning knobs

* CPU reaction delay
* CPU tackle frequency
* CPU pass accuracy
* goalkeeper save chance
* shot power scaling
* player movement speed

## Minimum viable product (MVP)

The first playable version should include:

1. One field
2. Two teams
3. 5v5 match
4. Big/Medium/Small player types
5. Move, pass, shoot, stand tackle, slide tackle
6. Automatic goalkeeper
7. Score, timer, kickoff reset
8. Basic CPU opponent
9. Team select with type composition

## Out of scope for MVP

* online multiplayer
* career mode
* fouls/cards
* commentary
* advanced formations
* custom tournaments
* full animation blending polish

## Suggested Godot scene structure

### Core scenes

* `Main.tscn`
* `MainMenu.tscn`
* `TeamSelect.tscn`
* `Match.tscn`
* `Player.tscn`
* `Ball.tscn`
* `Goal.tscn`
* `Goalkeeper.tscn`
* `HUD.tscn`

### Core C# scripts

* `GameManager.cs`
* `MatchManager.cs`
* `PlayerController.cs`
* `PlayerAI.cs`
* `BallController.cs`
* `GoalkeeperAI.cs`
* `TeamManager.cs`
* `InputRouter.cs`
* `HUDController.cs`
* `StatBlock.cs`

## Recommended technical architecture

### Keep prototype architecture simple

Use:

* one `MatchManager` for rules/state
* reusable `Player` scene with stat-driven behavior
* finite state machines for player logic
* signals/events for goals, possession changes, and match state updates

### Suggested player states

* Idle
* Move
* Dribble
* ReceivePass
* Shoot
* StandTackle
* SlideTackle
* Stunned/Recover

### Suggested match states

* PreKickoff
* InPlay
* GoalScored
* ResetPositions
* HalfTime
* FullTime
* Paused

## Prototype development roadmap

### Phase 1: movement and field

* create field scene
* create player scene
* top-down movement
* camera follow

### Phase 2: ball and possession

* attach ball to player in possession
* basic dribble logic
* transfer possession

### Phase 3: pass and shoot

* pass targeting
* shooting toward goal
* goalkeeper saves
* scoring and reset

### Phase 4: defense

* stand tackle
* slide tackle
* loose ball and interceptions

### Phase 5: teams and match flow

* timer
* kickoff
* goal events
* team select
* win/lose flow

### Phase 6: AI and polish

* basic attack/defense AI
* sound
* UI polish
* stat tuning

## Tuning goals

The game should feel:

* faster than realistic soccer
* less slippery than hockey
* readable even when multiple players converge
* satisfying when passing quickly between teammates
* fair: slide tackle is powerful but punishable

## Key risks

1. **Too much chaos on screen** — solve with good spacing AI and strong visual indicators.
2. **Pass targeting feels unreliable** — solve with directional assist and visible teammate intent.
3. **Tackles feel unfair** — solve with clear hitboxes and recovery windows.
4. **Ball physics feel messy** — keep ball behavior simplified and mostly authored, not fully simulated.

## First playable milestone definition

A user can:

* start the game
* pick a simple team composition
* play a full short match against CPU
* pass, shoot, tackle, and score
* finish the match and restart

## Open design questions

* Should the player manually switch defenders, or should switching stay automatic?
* Should shooting be tap-only or charge-based?
* Should out-of-bounds be ignored, simplified, or fully implemented?
* Should goalkeeper saves be deterministic or partly random?
* Should each team have fixed formations or loose positioning?

## Recommendation for the very first prototype

Build the smallest version possible:

* 2D top-down
* no out-of-bounds
* no fouls
* auto goalkeeper
* one arena
* one team color vs another
* only 3 body types with stat differences
* 2-minute match

That will let you find the fun in movement, passing, shooting, and tackling before adding anything else.
