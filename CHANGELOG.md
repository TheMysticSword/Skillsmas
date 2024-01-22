## 1.3.1
* Updated Simplified Chinese translation
## 1.3.0
* Added 8 new skills:
	* Commando:
		* Burst Fire (Primary)
		* Heavy Bullets (Primary)
		* Phase Beam (Secondary)
		* Tactical Lift-Off (Utility)
	* Huntress:
		* Tracker Glaive (Secondary)
		* Take Aim (Special)
	* REX:
		* Uprooting (Primary)
		* DIRECTIVE: Stimulate (Special)
* MUL-T: Update Mode:
	* Update Duration: ~~5s~~ ⇒ 2s
		* No longer scales with attack speed
	* Buff Duration: ~~10s~~ ⇒ 7s
* Artificer: Water Bolt:
	* Now has slight homing
* Artificer: Cumulonimbus:
	* Proc Coefficient: ~~0.2~~ ⇒ 0.1
		* This was mainly changed to reduce the amount of healing done by the skill from 1% HP/s per enemy to 0.5% HP/s per enemy
* Skill config options for "Cancel Sprint", "Force Sprint", "Cancelled From Sprinting" and "Must Key Press" now don't require unchecking the "Ignore Balance Changes" option
* Moved Bandit's Murder Party kill chain mechanic description to its own keyword
## 1.2.3
* Artificer: Brimstone:
	* Damage Per Second: ~~150%~~ ⇒ 100%
* Artificer: Crashing Wave:
	* Damage Scaling: ~~125%~~ ⇒ 150%
* Added Spanish translation by Juhnter
* Updated Brazilian Portuguese and Simplified Chinese translations
## 1.2.2
* Artificer: Brimstone:
	* Damage Per Second: ~~100%~~ ⇒ 150%
* Artificer: Thunderbolt:
	* Max Zaps: ~~3~~ ⇒ 4
		* The extra zap happens immediately on spawning
* Artificer: Tectonic Shift:
	* Damage: ~~1200%~~ ⇒ 200%
* Artificer's Crashing Wave now has slight screenshake
## 1.2.1
* Added support for ArtificerExtended's Energetic Resonance passive
	* Fire, lightning and ice skills from this mod now count towards Energetic Resonance
	* While Energetic Resonance is active:
		* Picking up crystals from Crystallize also grants 20 armor for 3 seconds. The amount of rock skills equipped affects the buff duration.
		* Hitting enemies with Revitalizing skills also has a 5% chance to cleanse 1 stack of a random debuff. The amount of water skills equipped affects the cleansing chance.
* Fixed Russian translation not working
* Updated Simplified Chinese translation
## 1.2.0
* Added 4 new Artificer skills:
	* Water Bolt
	* Concentrated Nano-Stream
	* Cumulonimbus
	* Crashing Wave
* Added 2 new Mercenary skills:
	* Riptide
	* Crossing Storms
* Added 1 new keyword:
	* Revitalizing
* Artificer: Frost Barrier:
	* Now counts as a combat skill
* Artificer: Superbolide:
	* Cooldown: ~~12s~~ ⇒ 8s
	* Cooldown now starts after the skill ends
	* Now counts as a combat skill
* Added more placeholder skill icons
* Added Simplified Chinese translation by Meteorite1014
* Updated Brazilian Portuguese translation
## 1.1.1
* MUL-T: Shipping Crate:
	* The collider is now disabled for the first 0.1 seconds, which should slightly help with the issue of colliding with the crate at the moment of throwing
	* Added new config options:
		* Push Power (1x by default)
		* Lifetime (600s by default)
* Fixed Brazilian Portuguese translation not working
* Potentially fixed MUL-T's Shipping Crate not having collisions for online clients
* Artificer's Tectonic Shift and MUL-T's Shipping Crate now have Stone and Metal SurfaceDefs respectively and will not produce console warnings when stepped on
* Artificer's Superbolide petrification overlay should appear on enemies more consistently now
* Engineer's Energy Shield now plays a quieter sound loop
* Slightly improved visual feedback for Artificer's Superbolide
## 1.1.0
* Added 4 new Artificer skills:
	* Geode Bolt
	* Moulded Nano-Boulder
	* Tectonic Shift
	* Superbolide
* Added 1 new keyword:
	* Crystallize
* Added Brazilian Portuguese translation by Kauzok
## 1.0.1
* Engineer's Energy Shield now scales with attack speed
* Acrid's Autonomous Organism DPS increased from 100% to 200%
* Added skill icon for Zip Fist
* Updated skill icons for Cryo Bolt and Lit Nano-Rocket
* Reuseable Supply Beacon is now affected by Lysate Cell
* Fixed Update Mode not cancelling MUL-T's Secondary skill in Power Mode
* Removed unnecessary R2API Orb and Sound dependencies
## 1.0.0
* Release
