# HHG - Pseudorandom
Randomness that feels fair to players, for the rolls they judge: hits, crits, status effects, drops.
Cosmetic and procedural randomness doesn't need this package; `UnityEngine.Random` is fine there.

- [Setup](#setup)
- [Which technique when](#which-technique-when)
- [Core](#core): `GameRandom`, `RandomStream`, `RandomExtensions`, `SeedUtil`, `SystemRandomAdapter`
- [Project streams](#project-streams)
- [Chance](#chance): `PrdChance`, `EntropyChance`, `AdaptiveChance`, `PityTimer`, `LossGuarantee`, `StreakAssist`, `DisplayedChance`
- [Selection](#selection): `ShuffleBag<T>`, `HistoryPicker<T>`, `SpreadShuffle`, `DuplicateProtection`, `ContextualTable<T>`, `WeightedItem<T>`, `BagEntry<T>`
- [Variance](#variance): `VarianceRoll`
- [Saving](#saving)
- [Rules](#rules)

## Setup
Reference the `HHG.Pseudorandom.Runtime` assembly from your asmdef, then:

```csharp
using HHG.Pseudorandom.Runtime;
```

The package has no dependencies and compiles on Unity 2020.3+.

## Which technique when
| Problem | Use | Shipped in |
| --- | --- | --- |
| Crit/stun/proc streaks | `PrdChance` | Warcraft III, Dota 2 |
| Miss streaks on high hit chances | `PrdChance` rolled on the miss | Warcraft III, Dota 2 |
| Very frequent rolls that should feel like a rate (evasion, block) | `EntropyChance` | Path of Exile |
| Per-encounter drops that shouldn't dry up or flood | `AdaptiveChance` | Slay the Spire, League of Legends |
| Rare rewards with long droughts | `PityTimer` | Hearthstone, Genshin Impact, Slay the Spire |
| "Won, but not what I wanted" | `LossGuarantee` | Genshin Impact |
| Unwanted duplicates | `DuplicateProtection` | Hearthstone |
| Small sets that must all appear | `ShuffleBag<T>` | Tetris (7-bag) |
| Repeats (voice lines, SFX, events) | `HistoryPicker<T>` | Unity Audio Random Container, Tetris: The Grand Master |
| Clumping in an ordered list | `SpreadShuffle` | Spotify |
| "95% should hit" | `DisplayedChance` (2RN) | Fire Emblem |
| Mercy on easy difficulty | `StreakAssist` | XCOM 2, BioShock |
| Wild damage swings | `VarianceRoll` | 3d6 vs d20 |
| Catch-up help | `ContextualTable<T>` | Mario Kart 8 |

Every roll method takes an optional `IRandom` as its last argument. Pass the stream that owns the outcome (see [Project streams](#project-streams)). If you pass nothing, it uses `GameRandom.Default`.

---

## Core

### Why GameRandom
- **One seed.** The same seed replays the same sequence of outcomes, and saving its state means a reload repeats them. XCOM: Enemy Unknown did this to make save-scumming pointless.
- **Named streams.** Each system can roll on its own stream, derived from the seed and the stream's name (Slay the Spire uses a dozen). Extra rolls in one system never shift another, so seeds and bug reports keep reproducing.
- **xoshiro128\*\* instead of `System.Random`.** Its state can be saved. `System.Random` seeds that differ by a fixed offset give correlated output (Slay the Spire 2 shipped with exactly this bug), and its algorithm can differ between runtimes. `UnityEngine.Random` is one global stream for the whole engine.
- **Not thread-safe**, like `UnityEngine.Random`. Give worker threads their own `RandomStream` seeded with `SeedUtil.Derive`.

### GameRandom
The static source. It owns one seed, a default stream, and any named streams you ask for.

```csharp
// Seed once, for example when a new playthrough starts
GameRandom.InitState(SeedUtil.NewSeed());   // fresh random seed
GameRandom.InitState(12345);                // int seed
GameRandom.InitState("ABCD-1234");          // player-entered or daily seed

ulong seed = GameRandom.Seed;               // show it so players can share seeds

RandomStream fallback = GameRandom.Default;
RandomStream named = GameRandom.Stream("Example");   // created on first use, same object after that

// Convenience rolls on the Default stream
float value = GameRandom.Value;             // [0, 1)
float spread = GameRandom.Range(-2f, 2f);   // [min, max)
int face = GameRandom.Range(1, 7);          // [min, max), max exclusive
bool lucky = GameRandom.Chance(0.25f);

// Library code: accept an optional IRandom and fall back to Default
IRandom rng = GameRandom.OrDefault(null);
```

Fix one stream for a scripted sequence, then put it back exactly as it was:

```csharp
using (GameRandom.Reseed("Example", 777))
{
    // every roll on this stream plays out the same way every time
    bool scripted = GameRandom.Stream("Example").Chance(0.5f);
}
// the stream continues its own sequence here

// Spanning several frames: keep the handle and dispose it when the sequence ends
System.IDisposable scope = GameRandom.Reseed("Example", 777);
scope.Dispose();
```

Save and load (see [Saving](#saving)):

```csharp
GameRandomState state = GameRandom.GetState();
GameRandom.SetState(state);   // continues exactly where it left off
```

### RandomStream
One seedable generator with savable state. `GameRandom` hands these out; you can also create your own, for example per worker thread.

```csharp
RandomStream stream = new RandomStream(42UL);     // or new RandomStream() for a random seed
stream.InitState(99);                             // reseed (int or ulong)
uint bits = stream.NextUInt();                    // raw 32 bits

RandomStreamState snapshot = stream.GetState();   // plain struct, JsonUtility-friendly
stream.SetState(snapshot);

// Preview without consuming: roll on a copy and throw it away
RandomStream preview = stream.Clone();
bool wouldSucceed = preview.Chance(0.8f);
```

### RandomExtensions
Everything you can do with any `IRandom`. Note that `Range(float, float)` excludes the max, unlike `UnityEngine.Random.Range(float, float)`, and that a weight of 0 is never picked.

```csharp
IRandom rng = GameRandom.Default;

float f = rng.Value();                   // [0, 1)
double d = rng.NextDouble();             // [0, 1), 53-bit precision
int index = rng.Range(0, 10);            // [0, 10), unbiased
float amount = rng.Range(5f, 10f);       // [5, 10)
bool success = rng.Chance(0.3f);         // always consumes exactly one roll
int direction = rng.Sign();              // -1 or +1

string[] items = { "A", "B", "C" };
string item = rng.Pick(items);

var list = new System.Collections.Generic.List<string>(items);
rng.Shuffle(list);                       // in-place Fisher-Yates

float[] weights = { 70f, 25f, 5f };
int tier = rng.WeightedIndex(weights);   // -1 if every weight is 0

string weighted = rng.PickWeighted(items, x => x == "A" ? 3f : 1f);
```

### SeedUtil
Seed helpers. Derived seeds go through SplitMix64, so streams never correlate. `Hash` is stable on every platform and launch, unlike `string.GetHashCode`.

```csharp
ulong seed = SeedUtil.NewSeed();                   // unpredictable
ulong typed = SeedUtil.FromString("ABCD-1234");    // stable on every platform
ulong child = SeedUtil.Derive(seed, "Child");      // independent child seed
ulong hash = SeedUtil.Hash("Child");               // stable 64-bit FNV-1a
ulong mixed = SeedUtil.Mix(hash);

RandomStream childRng = new RandomStream(child);
```

### SystemRandomAdapter
Lets existing APIs that take a `System.Random` draw from a stream, without changing them.

```csharp
System.Random asSystem = GameRandom.Default.AsSystemRandom();   // cached per stream
int roll = asSystem.Next(1, 7);

// Same thing for any IRandom
System.Random wrapped = new SystemRandomAdapter(GameRandom.Stream("Example"));
```

---

## Project streams
The package has no opinion on which streams a game needs. Define them in your project, in one static class, so names stay consistent:

```csharp
// In your game's code, not in this package
public static class ProjectRandom
{
    public static RandomStream Combat => GameRandom.Stream("Combat");
    public static RandomStream Rewards => GameRandom.Stream("Rewards");
    public static RandomStream Cosmetic => GameRandom.Stream("Cosmetic");
}
```

Then pass them to rolls: `critPrd.Roll(critChance, ProjectRandom.Combat)`. A handful of streams is plenty; keep cosmetic rolls off the gameplay ones. The examples below use `GameRandom.Default` to stay self-contained.

---

## Chance
One yes/no outcome per roll. Keep one object **per actor per effect**, usually as a serialized field so it saves with the actor.

### PrdChance
**Pseudo-random distribution.** The chance on the Nth attempt since the last success is C × N, with C chosen so the long-run rate equals the nominal chance. Each failure makes the next success more likely, a success resets the counter, and success is guaranteed once C × N reaches 1.

| Nominal | C | Guaranteed by attempt |
| --- | --- | --- |
| 10% | 1.5% | 68 |
| 25% | 8.5% | 12 |
| 50% | 30.2% | 4 |
| 75% | 66.7% | 2 |

- **Shipped in:** Warcraft III and Dota 2 (crits, bashes, evasion, Counter Helix, neutral item drops). League of Legends uses a similar idea for crits.
- **Use when:** a frequent yes/no proc on one actor: crit, stun, counterattack, on-hit effects, status application. The chance can change between rolls; pass the current one and the counter carries over.
- **Avoid when:** the chance is under ~5%. C becomes tiny and it behaves like plain random until very late; use `PityTimer`. Also avoid exposing the counter to UI or AI, or procs become farmable.
- **Rules (from Dota):** only call `Roll` for attempts that could actually proc (not on cooldown, not against invalid targets), and never for previews or AI scoring.

```csharp
IRandom rng = GameRandom.Default;
PrdChance critPrd = new PrdChance();             // or [SerializeField] private PrdChance critPrd = new PrdChance();

float critChance = 0.25f;                        // may change every attack; pass the current one
bool crit = critPrd.Roll(critChance, rng);

// Hit chance: roll PRD on the MISS, so misses get spread out
PrdChance missPrd = new PrdChance();
float hitChance = 0.8f;
bool hit = !missPrd.Roll(1f - hitChance, rng);

// Debugging and tests (show players the nominal chance, not this)
float realNext = critPrd.NextChance(critChance);
int sinceLast = critPrd.AttemptsSinceSuccess;
float c = PrdChance.ConstantFor(0.25f);         // ~0.0847
double rate = PrdChance.RateFor(c);              // ~0.25

critPrd.Reset();
```

### EntropyChance
**Entropy accumulator.** A counter starts at a random value between 0 and 1. Each attempt adds the chance; when it passes 1 the attempt succeeds and 1 is subtracted. A chance p gives exactly one success every 1/p attempts, the lowest variance of any technique here.

- **Shipped in:** Path of Exile's evasion. It resets to a random value after 3.33 seconds without attacks, and all attackers share one counter per defender.
- **Use when:** rolls are very frequent and should feel like a steady rate rather than a gamble: evasion, block, armor procs, "every few hits" passives. Especially when the player is on the receiving end.
- **Avoid when:** surprise is the point. After a few rolls players can read the pattern.
- **Tip:** call `Reset()` at the start of each encounter so the pattern doesn't carry over.

```csharp
EntropyChance evade = new EntropyChance();

float evadeChance = 0.3f;
bool evaded = evade.Roll(evadeChance, GameRandom.Default);   // ~every 3rd or 4th attempt, never 4 failures in a row

evade.Reset();
```

### AdaptiveChance
**Bidirectional smoothing.** An offset on top of the nominal chance rises after a failure and falls after a success. Unlike PRD, which only climbs and then resets, it also makes back-to-back successes less likely.

- **Shipped in:** Slay the Spire potions (40% after each combat, +10% on no drop, −10% on a drop, reset each act) and League of Legends crits (a non-crit raises the next crit's chance, a crit lowers it).
- **Mean-preserving vs symmetric:** equal up and down steps pull the long-run rate toward 50% whatever the starting chance. In simulation, Slay the Spire's 40% potion settles at 50%. By default the down step is sized to keep the long-run rate at the nominal chance; pass `symmetric: true` for Slay the Spire's behavior.
- **Use when:** per-encounter rewards that shouldn't dry up or flood (consumables, currency drops), or crit smoothing where back-to-back crits should also be rarer.
- **Avoid when:** the chance is under ~20%. The down step gets big, so a success is followed by several near-certain failures, which reads as a hidden cooldown. Also avoid it when you need a hard cap on bad luck; use `PityTimer`.

```csharp
IRandom rng = GameRandom.Default;

// 40% base, +10% after each miss, down step sized to keep the long-run rate at 40%
AdaptiveChance drop = new AdaptiveChance(0.4f, 0.1f);
bool dropped = drop.Roll(rng);

// Equal +/-10% steps, which drift toward 50% over time
AdaptiveChance symmetricDrop = new AdaptiveChance(0.4f, 0.1f, symmetric: true);

// Varying nominal chance (crit smoothing): pass it per roll
AdaptiveChance critSmoothing = new AdaptiveChance(0.25f, 0.05f);
bool smoothCrit = critSmoothing.Roll(0.3f, rng);

float current = drop.CurrentChance;
drop.Reset();
```

### PityTimer
**Bad-luck protection.** Counts attempts since the last success. Soft pity adds a step to the chance from a given attempt on, hard pity guarantees success at a given attempt, and a first-success guarantee makes sure new players see the payoff early. Attempt numbers are 1-based and counted since the last success.

- **Shipped in:**
  - Hearthstone: a Legendary within 40 packs, an Epic within 10, and a Legendary in the first 10 packs.
  - Genshin Impact: 0.6% base, guaranteed at pull 90. Players measure soft pity from pull 74 at about +6% per pull; that model gives the official 1.6% overall rate.
  - Slay the Spire rare cards: an offset starts at −5%, rises 1% per common, caps at +40% and resets on a rare. Normal fights use a 3% base and elites 10%, sharing the same offset.
  - World of Warcraft: Legion: legendary "bad luck protection" ramps the chance with each eligible kill or cache.
- **Use when:** rare, valuable outcomes where a long drought ends a session: rare drops, rare events, unique rewards.
- **Watch out:** pity raises the real average rate. Lower the base chance to compensate, and measure the real rate with a quick simulation.
- **Avoid when:** the outcome is common (above ~20%). `PrdChance` or a `ShuffleBag` fits better.

```csharp
IRandom rng = GameRandom.Default;

// Hearthstone Legendary: guaranteed within 40, and within the first 10 ever
PityTimer legendary = new PityTimer(0.01f, hardPity: 40, firstSuccessBy: 10);

// Genshin 5-star: 0.6% base, +6% per pull from pull 74, guaranteed at 90 (~1.6% overall)
PityTimer fiveStar = new PityTimer(0.006f, hardPity: 90, softPityStart: 74, softPityStep: 0.06f);

// Slay the Spire rare cards: offset starts -5%, +1% per miss, capped at +40%
PityTimer rare = new PityTimer(0.03f, softPityStart: 2, softPityStep: 0.01f, initialBonus: -0.05f, maxBonus: 0.40f);

bool gotLegendary = legendary.Roll(rng);

// Shared pity, different base chance per context
bool rareFromHardContent = rare.Roll(0.10f, rng);

// Outcomes decided elsewhere (a guaranteed reward, a purchase)
rare.RecordSuccess();
rare.RecordFailure();

float next = fiveStar.NextChance();
int dry = fiveStar.FailuresSinceSuccess;
bool everWon = legendary.HasSucceeded;
legendary.Reset();
```

### LossGuarantee
**50/50 with a guarantee.** Some wins come in a wanted and an unwanted flavor. Roll the flavor normally, but after a set number of unwanted results in a row, the next win is guaranteed to be the wanted one.

- **Shipped in:** Genshin Impact's character banner. A 5-star is 50/50 featured or standard; after losing once, the next is guaranteed featured. In simulation that makes 2 in 3 wins featured instead of 1 in 2.
- **Use when:** a roll has a "won, but not what I wanted" outcome, such as a reward that fits the player's build versus one that doesn't. Pair it with `PityTimer`: pity decides *when* you win, this decides *what*.
- **Avoid when:** the pool has no clear wanted outcome.

```csharp
LossGuarantee featured = new LossGuarantee(0.5f);   // optional 2nd arg: losses before the guarantee (default 1)

// Call once per win (for example after a PityTimer success)
bool wanted = featured.RollWanted(GameRandom.Default);
bool nextIsSafe = featured.IsGuaranteed;
featured.Reset();
```

### StreakAssist
**Hidden aim assist.** After each consecutive miss, quietly add a bonus to the next chance, up to a cap. It only applies to reasonable chances, and a success clears the streak.

- **Shipped in:** XCOM 2's hidden aim assist, on Rookie through Commander and off on Legend. Its config only assists shots of 50% or more and caps them at 95%, with a separate bonus once soldiers have been lost. Related mercy rules from BioShock: an enemy's first shot always misses, and a hit that would kill you grants 1–2 seconds of invulnerability.
- **Use when:** easier difficulty modes, on the player's side only, where shown chances are high enough that a miss streak reads as a bug.
- **Avoid when:** the hardest difficulty, PvP, or anything shared with opponents. If you don't want to raise the average rate, use `PrdChance` instead.

```csharp
IRandom rng = GameRandom.Default;

// +10% per miss, only for chances of 50%+, never above 95%
StreakAssist assist = new StreakAssist(0.1f, minBaseChance: 0.5f, maxChance: 0.95f);

bool success = assist.Roll(0.65f, rng);

// Or combine with another roller: adjust, roll elsewhere, report
PrdChance missRoller = new PrdChance();
float adjusted = assist.Adjust(0.65f);
bool hitWithPrd = !missRoller.Roll(1f - adjusted, rng);
assist.Report(hitWithPrd);

int streak = assist.ConsecutiveMisses;
assist.Reset();
```

### DisplayedChance
**"True hit" curves.** Roll so that high shown chances succeed more often than shown and low ones less often. Players over-trust big numbers ("95% should hit") and under-trust small ones.

| `ChanceModel` | Behavior | Shown 90% | Shown 10% |
| --- | --- | --- | --- |
| `OneRoll` | Honest | 90% | 10% |
| `TwoRollAverage` | Average of two rolls ("2RN") | 98% | 2% |
| `Hybrid` | Honest below 50%, sine curve above | 97.05% | 10% |

- **Shipped in:** Fire Emblem. 2RN ran from The Binding Blade through Awakening and in Three Houses; the hybrid is from Fates, Shadows of Valentia and Engage. Relatedly, Sid Meier found Civilization Revolution testers accepted a loss at 2:1 odds but not at 20:10, the same ratio.
- **Use when:** the UI shows a raw percentage and high-percentage misses generate "this game cheats" complaints. It applies to both sides, so opponents' high chances become reliable too.
- **Watch out:** the shown number stops being true, and players will find out. Either show `TrueChance()` in the UI, or accept the mismatch.
- **Avoid when:** honest odds are a design pillar. `PrdChance` cuts streaks without changing the rate.

```csharp
IRandom rng = GameRandom.Default;
float shown = 0.9f;

bool honest = DisplayedChance.Roll(shown, ChanceModel.OneRoll, rng);        // 90%
bool twoRn = DisplayedChance.Roll(shown, ChanceModel.TwoRollAverage, rng);  // ~98%
bool hybrid = DisplayedChance.Roll(shown, ChanceModel.Hybrid, rng);         // ~97.05%

// What it really is, if you want the UI to be honest about it
float trueChance = DisplayedChance.TrueChance(shown, ChanceModel.TwoRollAverage);   // 0.98
```

---

## Selection
Pick items from a set. The stateful ones are `[Serializable]` and editable in the Inspector.

### ShuffleBag&lt;T&gt;
**The "7-bag".** Put N copies of every outcome in a bag, draw without replacement, and refill when it runs low. Every outcome appears within one bag's worth of draws, and long-run frequencies are exact.

- **Shipped in:** the Tetris Guideline's Random Generator. One of each of the 7 pieces per bag means at most 12 pieces between two I pieces and S/Z runs of at most 4. The New Tetris used 9 of each piece.
- **Use when:** a small set of outcomes must all show up within a predictable window: encounter types, enemy behavior cycles, tips, reward categories. Droughts are the complaint ("I never get X").
- **Knobs:**
  - `copiesEach`: 1 copy is a strict rotation; more copies are more random but still bound droughts.
  - `refillAt`: refill when this many items remain, mixing the leftovers into the new bag. 0 is the classic bag; higher is fuzzier and harder to count.
  - `avoidRepeatAcrossRefill`: stops the last item of one bag from being the first of the next.
- **Avoid when:** the outcome is a single chance that changes every roll (use `PrdChance`), or players can track the bag and exploit it. Bigger bags or a partial refill make it harder to count.

```csharp
IRandom rng = GameRandom.Default;

ShuffleBag<string> bag = new ShuffleBag<string>(new[] { "A", "B", "C", "D" });
string next = bag.Next(rng);

// More copies = more random but still bounded droughts; partial refill = harder to count
ShuffleBag<string> fuzzy = new ShuffleBag<string>(new[] { "A", "B" }, copiesEach: 3, refillAt: 2);
fuzzy.Add("C", 1);

int left = bag.Remaining;
bag.Reset();   // next draw starts a fresh bag
```

### HistoryPicker&lt;T&gt;
**Avoid repeating the last N.** A weighted pick that remembers recent results.
- `HistoryPickerMode.Exclude`: recent results can't be picked at all.
- `HistoryPickerMode.Reroll`: if the pick is recent, reroll up to K times, then accept it anyway. Repeats become rare, not impossible.

- **Shipped in:** Unity's Audio Random Container ("Avoid Repeating Last") and Wwise random containers for Exclude; Tetris: The Grand Master for Reroll (up to 4 rerolls against the last 4 pieces, 6 in TGM2). In testing, TGM-style rerolls cut immediate repeats from 14.3% to 1.5% with 7 items.
- **Use when:** variety matters more than exact odds: SFX variations, voice lines, idle animations, flavor events, ambient tracks. Use Exclude for audio and dialogue, where any repeat sounds like a bug, and Reroll when an occasional repeat is fine but streaks aren't.
- **Avoid when:** exact frequencies matter (drop odds). History shifts picks toward rarely chosen items; use `ShuffleBag`. In Exclude mode, a `historySize` at or above the item count excludes everything, so it falls back to "anything but the last one".

```csharp
IRandom rng = GameRandom.Default;

// Exclude the last 2 picks entirely: good for voice lines and SFX
HistoryPicker<string> lines = new HistoryPicker<string>(new[] { "Line 1", "Line 2", "Line 3" }, historySize: 2);
lines.Add("Rare line", 0.25f);                              // weighted add
string line = lines.Next(rng);

// Tetris: The Grand Master style: reroll up to 4 times against the last 4, then accept
HistoryPicker<string> picker = new HistoryPicker<string>(
    new[] { "A", "B", "C", "D", "E" },
    historySize: 4, mode: HistoryPickerMode.Reroll, rerolls: 4);
string picked = picker.Next(rng);

lines.ClearHistory();
```

### SpreadShuffle
**Dithered shuffle.** Orders a list so items from the same category are spread evenly instead of clumping: each category's items are spaced at even intervals with a random start offset and a little jitter, then everything is sorted by position.

- **Shipped in:** Spotify's shuffle (2014). Listeners called a true Fisher–Yates shuffle broken because three songs by one artist played in a row. In testing on a 13-item list, the longest same-category run averaged 1.7 instead of 2.8.
- **Use when:** ordering a whole sequence that mixes categories: enemy types across a wave, room types along a path, events across a stretch of play, a playlist by mood.
- **Avoid when:** you need one pick at a time (use `HistoryPicker`), or clumping is the point.

```csharp
string[] sequence = { "A", "A", "A", "A", "B", "B", "C" };

// Group by the string itself; jitter 0 = perfectly even, 0.5 = loose (default 0.1)
var ordered = SpreadShuffle.Spread(sequence, x => x, GameRandom.Default);
var even = SpreadShuffle.Spread(sequence, x => x, GameRandom.Default, jitter: 0f);
```

### DuplicateProtection
**Don't offer what they already have.** Before rolling, drop everything the player owns as many copies of as they can use. Once everything is capped, fall back to the full pool so rolls never fail.

- **Shipped in:** Hearthstone. Since 2017 a pack never gives a second copy of a Legendary until you own every Legendary in the set; since 2020 Commons, Rares and Epics are capped at two copies the same way.
- **Use when:** duplicates are worthless or feel bad: unique items, unlocks, codex entries, events already seen. Filter first, then roll the survivors with any picker.
- **Avoid when:** duplicates are useful (stackable consumables, upgrade materials), or the pool is so small that filtering makes the next pick obvious.

```csharp
string[] pool = { "A", "B", "C" };
var owned = new System.Collections.Generic.HashSet<string> { "A" };

// Ownership count per item; maxCopies 1 = uniques
System.Func<string, int> ownedCount = x => owned.Contains(x) ? 1 : 0;

string offer = DuplicateProtection.Pick(pool, ownedCount, 1, GameRandom.Default);
string weightedOffer = DuplicateProtection.PickWeighted(pool, x => 1f, ownedCount, 1, GameRandom.Default);
var eligible = DuplicateProtection.Eligible(pool, ownedCount, 1);   // full pool once everything is owned
```

### ContextualTable&lt;T&gt;
**Rubber banding.** Several weighted tables, each tagged with a minimum context value. At roll time you pass a number describing how the player is doing, and the matching table is used.

- **Shipped in:** Mario Kart 8, which picks each item box's distribution from the racer's distance to the leader.
- **Use when:** you want mild, hidden catch-up help or pressure, such as rewards weighted by the player's health or resources. Use a single number you already track as the context.
- **Avoid when:** players can see the effect and game it by playing badly on purpose. Keep the context hidden and the differences small. Also avoid contexts that swing every turn; results will feel arbitrary.

```csharp
ContextualTable<string> table = new ContextualTable<string>();

// Context = health ratio (0..1). Each table applies from its minimum upward.
table.Add(0f,   new WeightedItem<string>("Heal", 3f), new WeightedItem<string>("Rare", 1f));
table.Add(0.5f, new WeightedItem<string>("Heal", 1f), new WeightedItem<string>("Rare", 2f));

float health = 0.3f;
string reward = table.Pick(health, GameRandom.Default);   // uses the 0 table
ContextBracket<string> bracket = table.BracketFor(0.8f);  // the 0.5 table
```

### WeightedItem&lt;T&gt; and BagEntry&lt;T&gt;
Inspector-friendly item/weight and item/copies pairs used by the classes above.

```csharp
WeightedItem<string> entry = new WeightedItem<string>("A", 2f);
string item = entry.Item;
float weight = entry.Weight;   // 0 = never picked

BagEntry<string> bagEntry = new BagEntry<string>("B", 2);
int copies = bagEntry.Copies;
```

---

## Variance

### VarianceRoll
**Bell-curve rolls.** Average several uniform rolls instead of taking one. The range and the mean stay the same, but results bunch toward the middle: with 3 rolls, the middle third of the range comes up 2 times in 3, versus 1 in 3 for a flat roll.

| `rolls` | Shape |
| --- | --- |
| 1 | Flat (d20) |
| 2 | Triangle |
| 3 | Bell (3d6-like) |
| 4+ | Tighter around the middle |

- **Shipped in:** tabletop design's classic 3d6 vs d20 trade-off, and damage and stat rolls in many RPGs.
- **Use when:** damage, healing, currency or stat rolls should have texture without "hit for 2, then for 19". The min and max don't change, so previews stay valid.
- **Avoid when:** the roll is meant to spike. Crits already provide spikes; a flat roll on top doubles the swing.

```csharp
IRandom rng = GameRandom.Default;
VarianceRoll roll = new VarianceRoll(2);    // or [SerializeField] for Inspector tuning

float baseDamage = 10f;
float variance = 3f;
float damage = roll.Around(baseDamage, variance, rng);   // 7..13, mostly near 10
float amount = new VarianceRoll(3).Range(20f, 40f, rng);
float t = roll.Sample01(rng);                            // [0, 1)

float quick = VarianceRoll.Average01(3, rng);
float normal = VarianceRoll.Normal(100f, 15f, rng);      // unbounded tails: clamp it
```

---

## Saving
`GameRandomState`, `RandomStreamState` and every stateful class are `[Serializable]`, so they work with `JsonUtility` and any save system built on it. Save `GameRandom`'s state and the technique objects together, so a reload repeats the same outcomes.

```csharp
[System.Serializable]
public class RandomSaveData
{
    public GameRandomState RandomState;
    public PrdChance CritPrd;
    public PityTimer RarePity;
}
```

```csharp
PrdChance critPrd = new PrdChance();
PityTimer rarePity = new PityTimer(0.05f, hardPity: 20);

// Save
RandomSaveData data = new RandomSaveData { RandomState = GameRandom.GetState(), CritPrd = critPrd, RarePity = rarePity };
string json = UnityEngine.JsonUtility.ToJson(data);

// Load
RandomSaveData loaded = UnityEngine.JsonUtility.FromJson<RandomSaveData>(json);
GameRandom.SetState(loaded.RandomState);
critPrd = loaded.CritPrd;
rarePity = loaded.RarePity;
```

## Rules
- Roll only for real outcomes. Never roll for previews, tooltips or AI scoring; it advances the state.
- Keep stateful objects per actor per effect (one actor's crit PRD is not another's).
- Show players the nominal chance. Exposing PRD or pity counters makes them farmable.
- Keep cosmetic rolls off the gameplay streams: use `UnityEngine.Random` or a separate stream.
