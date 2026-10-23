
# ![AsuMech](images/icon6464.png) AsuMech

*Another FFXIV mechanics simulator, now with Savage*

---

AsuMech is a fork of [AnoMech](https://github.com/anomek/AnoMech) by Anomek. It keeps everything AnoMech has and adds
Savage raids and other fights that stay in this fork, starting with AAC Heavyweight M1 (Savage). The plugin is still
opened with `/asumech` (or `/asu`; `/anomech` and `/ano` still work), and only one of the two can be installed at a time.
 
Simulate FFXIV raid mechanics client-side for solo practice. Go to any Inn, open the plugin with `/anomech` and start practicing!

Thanks to improvemnts by [WorstAquaPlayer](https://github.com/WorstAquaPlayer) plugin is quite stable now! No more 
crashes after training session.

**WARNING!!!**

**You are cut off from server traffic while in the sim zone.** To keep the
fake zone stable, the plugin firewalls incoming packets from the server.  
While simulating:
  * Players joining or leaving your party will not appear in the party list
  until you leave the sim zone.
  * Ready checks will not pop.
 


## Installation

Add this custom repository in Dalamud (`/xlsettings` → Experimental → Custom Plugin Repositories), then install AsuMech
from the plugin installer:

```
https://raw.githubusercontent.com/Asunnya/AsuMech/asunnya-release/pluginmaster.json
```

For upstream AnoMech, see: https://github.com/anomek/MyDalamudPlugins

## Beta: Advanced Simulation Resolution

The simulator now includes a beta feature that properly resolves most skills, triggers, and gauges during simulation.
As this feature is still in beta, some edge cases and less common interactions may not yet resolve correctly.


## Currently implemented 
All using default NA pf strats unless noted otherwise:
- Dancing Mad (Ultimate)
    - P1 Tele-trouncing _by [Evernow](https://github.com/Evernow)_
    - P2 Forsaken
      - NA
        - [Kroxy-Rinon 341 (Center/N Stacks) melee adjust](https://raidplan.io/plan/UATE__aDcw1-bgVv)
        - [South Adjust 341](https://raidplan.io/plan/uq7zdjvuu7uuw8fj)
        - diamond markers or week one positions
      - EU _by [Wydox](https://github.com/Wydox)_
        - [\[LPDU\] Buddies](https://raidplan.io/plan/142oXOZpPc_jh3dd)
        - [\[Old\] p3Z Buddy Meow](https://raidplan.io/plan/lZWqxfxvyhF9sp3Z)
        - [\[Old\] zP6 South adjust](https://raidplan.io/plan/rtc1FcuZFMuyBzP6)
      - JP _by [altair9x](https://github.com/altair9x)_
        - [Yan](https://yan-flash.com/ultimate/yosei-ranbu#ult-phase-2)
    - P3 Limit Cut _by [Evernow](https://github.com/Evernow)_
    - P3 Black Hole _old bh (single tethers, n/s stomps)_
      - D>S>A
      - S>D>A _by [Evernow](https://github.com/Evernow)_
    - P4 Kefka Says
      - kefkabin
      - kefkabin, gazes: supports N / DPS S _by [Evernow](https://github.com/Evernow)_
    - P5 Flood _by [Evernow](https://github.com/Evernow)_
    - P5 Exaflares _by [Wydox](https://github.com/Wydox)_
    - P5 Celestriad _by [RoarkGit](https://github.com/RoarkGit)_
    - P5 Forsaken Null _no ai or damage_
- Futures Rewritten (Ultimate) _by [grayparser](https://github.com/grayparser)_
    - P2 Diamond Dust 
    - P2 Light Rampant
    - P3 Ultimate Relativity
    - P3 Apocalypse 
    - P4 Darklit Dragonsong
      - NA
      - NA, you take Somber Dance
    - P4 Crystallize Time 
    - P5 Fulgent Blade
    - P5 Paradise Regained 
- The Omega Protocol (Ultimate)
    - P2 Party Synergy
      - NA pf
      - LPDU _by [Quantumlyy](https://github.com/Quantumlyy)_
    - P5 Delta
    - P5 Sigma
    - P5 Omega
    - P6 Exasquares / Wave Cannon 2
- The Weapon's Refrain (Ultimate)
    - P1 Garuda _by [Asunnya](https://github.com/Asunnya)_
    - P2 Ifrit _by [Asunnya](https://github.com/Asunnya)_
    - P3 Titan _by [Asunnya](https://github.com/Asunnya)_
    - Ultimate Predaction _by [WorstAquaPlayer](https://github.com/WorstAquaPlayer)_
    - Ultimate Annihilation _by [Asunnya](https://github.com/Asunnya)_
    - Ultimate Suppression _by [WorstAquaPlayer](https://github.com/WorstAquaPlayer)_
    - Primal Roulette _by [Asunnya](https://github.com/Asunnya)_
- The Unending Coil of Bahamut (Ultimate) _by [RoarkGit](https://github.com/RoarkGit)_
    - Exaflares
- Dragonsong's Reprise (Ultimate) _by [Asunnya](https://github.com/Asunnya)_
    - P1 Knights (pull to Thordan: Hyperdimensional Slash, Shining Blade, Playstation, Holiest Hallowing interrupts, Pure of Heart)
    - P4 The Eyes (Hatebound orbs, Mirage Dive snake swaps)
    - P5 Wrath of the Heavens (twisters, dragoon tethers, proteans, Eruptions / Liquid Heaven baits, Chain Lightning)
    - P5 Death of the Heavens (dooms and cleansing puddles, static dives, earthquakes, playstation knockback, gazes)
    - P6 Wyrmsbreath to Wroth Flames (fire/ice tether pairs, tank breaths, Mortal Vow passes, Akh Afah, Hallowed Wings with Nidhogg's dive, orb crosses with Akh Morn and Hraesvelgr's dive, Hot Wing/Tail spreads and stacks)
    - P6 Hallowed Wings 2 to Cauterize (second Akh Afah, Hallowed Wings with Hot Wing/Tail, static Wyrmsbreath 2, Pyretic/Deep Freeze cleansed by the dragons' dives, Touchdown, last Mortal Vow passes)
- AAC Heavyweight M1 (Savage) — M9S _by [Asunnya](https://github.com/Asunnya)_
    - Vamp Stomp, Hell in a Cell, Aetherletting, Coffinmaker, Undead Deathmatch, Flails, Vamp Stomp 2, Final
- AAC Heavyweight M2 (Savage) — M10S _by [Asunnya](https://github.com/Asunnya)_
    - Flame Floater to Pyrotation (Hot Impact, Flame Floater tether charges, Alley-oop Inferno spreads, Cutback Blaze, Pyrotation)
    - Sickest Take-off to Deep Impact (Sick Swell wave and Deep Blue's lane, orb stacks or spreads, Reverse/Double-dip proteans, Deep Impact)
    - Xtreme Spectacular to Insane Air (multi-hit raidwide, both surfboards' four stops with proteans, light party stacks and tank busters)
    - Fire/Watersnaking to Hot Impact (fire/water split by distance, Deep Blue's corner proteans, Alley-oop Inferno spreads, Sick Swell's wave, shared tank buster)
    - Deep Varial to Deep Impact (wide fan with small-wave stack or spreads, Alley-oop Inferno, Steam Burst, Hot Aerial's four jumps, Sickest Take-off with the wave's push, Cutback Blaze, Deep Impact)
    - Deep Aerial (tower into the Watery Grave, six rounds of fire and water wave tethers that must stretch, fire through the prison and water around it, Scathing Steam)
- AAC Heavyweight M3 (Savage) — M11S _by [Asunnya](https://github.com/Asunnya)_
    - Raw Steel Trophy to Void Stardust (axe or scythe Raw Steel, Trophy Weapons into Assault Evolved twice, Void Stardust comets ending in a stack or spreads)
    - Dance of Domination to One and Only (raidwides into pair stacks between six lines, second Raw Steel Trophy, Charybdistopia, Ultimate Trophy Weapons' six strikes leaving Maelstroms, tornado gust baits)
    - Great Wall of Fire to Orbital Omen (two line tank busters leaving walls that explode, four pairs of portal lines with Fire and Fury's front and back cones)
    - Meteorain to Triple Tyrannhilation (proximity-baited meteors, shade tethers for the tanks, Fearsome Fireball lines blown into meteors, hiding from three shockwaves behind the last meteors)
    - Arena Split (Flatliner's knockback onto two platforms, towers that knock back, Meteowrath tethers to stretch, Fire Breath, chasing puddles and portal meteor lines twice, Massive Meteor stacks, last towers and Arcadion Avalanche)
    - Ecliptic Stampede to Heartbreak Kick (Mammoth Meteors, Atomic Impact puddles at the walls while the group baits meteors round the boss, one- and two-player towers, tethers to the corners, Two-way Fireball line stacks, Heartbreak Kick towers)

## Details

* Spawns fake party members and boss NPCs into the live game client
* Drives their positions, cast bars, tethers, and VFX so mechanics play out visually
* Your fake party members are full fledged bots that will do mechanics.  
  Some scanarios also have solo mode where you can practice without disctractions.


## How to help
1. Please provide feedback and report any issues in scenarios: bad timing, damage, config not working at it supposed
2. Bot AI currently only covers strategies from my region. Adding strategies for other regions requires little coding.
   Feel free to create pull request or contact me.
3. Adding new scenario is more involved. `tools/parser.py` generates a baseline scenario from a log, which still
   needs randomization, mechanic-failure logic and bot AI added by hand.
4. Plugin-development or reverse-engineering help, and improvement ideas, are also welcome.


## Known issues
* Visuals should closely match retail, but damage snapshot timings are best guesses
* In scenarios for Top Omega Protocol (Ultimate):
  * Tether distance threshold are very rough estimations
* Not all skills will resolve properly

#  Acknowledgments

Thanks for contributors:
* [WorstAquaPlayer](https://github.com/WorstAquaPlayer) - rewriting core & fixing crashes, extra scenarios
* [Wydox](https://github.com/Wydox) - core improvements, extra scenarios and strategies
* [Evernow](https://github.com/Evernow) - multiplayer (WIP), core improvements, extra scenarions and strategies
* [RoarkGit](https://github.com/RoarkGit) - win streaks, extra scenarios
* [grayparser](https://github.com/grayparser) - core improvements, extra scenarios
* [altair9x](https://github.com/altair9x) - extra strategies
* [Quantumlyy](https://github.com/Quantumlyy) - extra strategies
* [Linqx](https://github.com/Linqx) - success/failure checkmarks
* [Asunnya](https://github.com/Asunnya) - M9S, M10S, M11S, UWU P1 Garuda / P2 Ifrit / P3 Titan / Ultimate Annihilation, DSR P1 Knights / P2 Thordan / P3 Nidhogg / P4 Eyes / P5 Wrath / P5 Death / P6 Double Dragons

AnoMech leans heavily on the work of other Dalamud plugins. Huge thanks to their authors!  
Without them, the following would not be possible:

AnoMech's zone changes are built on **Hyperborea**, which served as the baseline for loading a solo duty arena.                                                                                                                                                                                   
[Evernow](https://github.com/Evernow) has since improved that code and made it safer to use.                                                                                                                                                                                                      

**FFXIV-RaidsRewritten** and **bossmod** were the initial inspiration for the project, though neither is part of the                                                                                                                                                                              
codebase anymore.
