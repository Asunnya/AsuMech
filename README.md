
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
 

### Beta: Advanced Simulation Resolution

The simulator now includes a beta feature that properly resolves most skills, triggers, and gauges during simulation. 
As this feature is still in beta, some edge cases and less common interactions may not yet resolve correctly.


## Installation

Add this custom repository in Dalamud (`/xlsettings` → Experimental → Custom Plugin Repositories), then install AsuMech
from the plugin installer:

```
https://raw.githubusercontent.com/Asunnya/AsuMech/asunnya-release/pluginmaster.json
```

For upstream AnoMech, see: https://github.com/anomek/MyDalamudPlugins

## Currently implemented (by default using pf NA strats):
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
- The Omega Protocol (Ultimate): _NA pf strats_
    - P2 Party Synergy
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
- AAC Heavyweight M1 (Savage) — M9S _by [Asunnya](https://github.com/Asunnya)_
    - Vamp Stomp, Hell in a Cell, Aetherletting, Coffinmaker, Undead Deathmatch, Flails, Vamp Stomp 2, Final

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
* [WorstAquaPlayer](https://github.com/WorstAquaPlayer) - rewriting core & fixing crashes, scenarios for uwu
* [Wydox](https://github.com/Wydox) - core improvements, extra scenarios and strategies
* [RoarkGit](https://github.com/RoarkGit) - win streaks, extra scenarios
* [Evernow](https://github.com/Evernow) - multiplayer (WIP), extra sceanrions and strategies
* [Asunnya](https://github.com/Asunnya) - M9S, UWU P1 Garuda / P2 Ifrit / P3 Titan / Ultimate Annihilation, DSR P1 Knights / P2 Thordan / P3 Nidhogg / P4 Eyes / P5 Wrath

AnoMech leans heavily on the work of other Dalamud plugins. Huge thanks to their authors!  
Without them, the following would not be possible:

AnoMech's zone changes are built on **Hyperborea**, which served as the baseline for loading a solo duty arena.                                                                                                                                                                                   
[Evernow](https://github.com/Evernow) has since improved that code and made it safer to use.                                                                                                                                                                                                      

**FFXIV-RaidsRewritten** and **bossmod** were the initial inspiration for the project, though neither is part of the                                                                                                                                                                              
codebase anymore.
