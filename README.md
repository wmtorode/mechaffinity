# mechaffinity

A Mod for HBS's BattleTech that bonds makes pilots bond with their mechs

## Settings Json

example:

```json
{
    "debug" : true,
    "missionsBeforeDecay" : -1,
    "removeAffinityAfter" : 100,
    "lowestPossibleDecay" : 0,
    "maxAffinityPoints" : 1000,
    "decayByModulo" : false,
    "defaultDaysBeforeSimDecay" : -1,
    "topAffinitiesInTooltipCount" : 3,
    "showQuirks" : false,
    "showDescriptionsOnChassis" : false,
    "trackSimDecayByStat" : true,
    "trackLowestDecayByStat": false,
    "showAllPilotAffinities" : true,
    "enablePilotQuirks" : false, 
    "globalAffinities" : [],
    "chassisAffinities" : [],
    "quirkAffinities" : [],
    "taggedAffinities" : [],
    "pilotQuirks" : []
}
```

`debug` : when true enable debug logging

`missionsBeforeDecay` : the number of deployments a pilot can not use a chassis before their experience on that chassis begins to be lost, set to `-1` to disable

`removeAffinityAfter` : the number of deployments a pilot can not use a chassis before all experience on that chassis is lost, this is used to clean up save data tracking, set to `-1` to disable

`lowestPossibleDecay` : the lowest amount of a pilots experience with a chassis can decay to `removeAffinityAfter` overrides this value. this is counted in deployements when `trackLowestDecayByStat` is `false` the number in this settings will always be used. 
if `trackLowestDecayByStat` is `true` this number becomes part of the save and cannot be changed from settings later. events or argo upgrades can
manipulate this value by changing the company stat `MaLowestDecay`

`maxAffinityPoints` : the max amount of affinity that can be obtained for a unit once a pilot reaches this number with a chassis, further points will not be obtained

`decayByModulo` : when set to true, decay is changed to 1 point for every `missionsBeforeDecay` instead of 1 point for every mission after `missionsBeforeDecay` missions

`defaultDaysBeforeSimDecay` : the default number of days that can elapse before a pilot's affinities begin to decay. when `trackSimDecayByStat` is `true` this number becomes part of the save and cannot be changed from settings later. events or argo upgrades can
manipulate this value by changing the company stat `MaSimDaysDecayModulator`. setting this stat to -1 will stop decay from occuring when a day passes. deploying a pilot into a mission will reset that pilots counter. when `trackSimDecayByStat` is `false` this 
setting value will always be used

`topAffinitiesInTooltipCount` : the number of mechs to show affinity about in the pilot tooltip, if the pilot has affinities with more mechs than this, mechs with the fewest affinities will be dropped from display

`showQuirks` : when true, quirk affinities that are assiocated with a mech will be shown in the mechbay description of the mech in addition to any chassis specific affinities

`showDescriptionsOnChassis` : when true, affinitys will be shown for chassis in the on hover chassis description in the mechbay storage screen

`showAllPilotAffinities` : when true, the pilot dossier will show every affinity the pilot has with every chassis, when false only the highest level affininty will be show for a given chassis

`enablePilotQuirks` : when true, pilot quirk patches will be enabled **Warning: This will conflict with Pilot Quirks mod**

`globalAffinities` : a list of `affinityLevel` objects. these will aplly to all pilot-chassis combos. Note that affinity levels are additive

`chassisAffinities` : a list of `ChassisAfinity` objects. These apply only to pilots-chassis combos that are called out by the affinity. Note these are additive with all other affinities.

`quirkAffinities` : a list of `QuirkAffinity` objects. These apply only to pilots-chassis combos equiped with the defined gear that are called out by the affinity. Note these are additive with all other affinities.

`taggedAffinities` : a list of `TaggedAffinity` objects. These will only apply to pilot-chassis combos that are called out by the affinity, when the pilot has the specificed tag. Note these are additive with all other affinities.

`pilotQuirks` : a list of `PilotQuirk` objects. These will only be used if `enablePilotQuirks` is set to `true`

### affinityLevel objects

```json
{
    "missionsRequired" : 1,
    "levelName" : "Professional",
    "decription" : "Get A Major Gunnery Boost",
    "affinities" : [],
    "effectData" : []
}
```

`missionsRequired` : the number of deployments required to recieve this affinity

`levelName` : the name of this affinity level

`decription` : a description of this level

`affinities` : a list of `affinity` objects this level applies

`effectData` : a list of status effects that this level applies

### affinity objects

```json
{
    "type" : "Gunnery",
    "bonus" : 5
}
```

`type` : the affinity type one of:

- Gunnery
- Guts
- Tactics
- Piloting

`bonus` : the bonus to be applyed to this skill

### ChassisAffinity objects

```json
{
    "chassisNames" : [],
    "affinityLevels" : []
}
```

`chassisNames` : a list of chassis this affinity is available to. the chassis name is the prefab name followed by a `-` and the tonnage of the mech. In the event a the chassis has an assembly variant (from custom salvage), this will be used instead of the prefab. example chassis name for the assassin `chrPrfMech_assassinBase-001_40`

`affinityLevels` : a list of `affinityLevel` objects to be considered for this affinity

### QuirkAffinity objects

```json
{
    "quirkNames" : [],
    "affinityLevels" : []
}
```

`quirkNames` : a list of fixed equipment on a chassis that this affinity should be applied to. use the items ComponentDefID for this field. a pilot can qualify for multiple quirk affinities, ideally this is used for mech quirks, but other fixed gear can also be used

`affinityLevels` : a list of `affinityLevel` objects to be considered for this affinity

### TaggedAffinity objects

```json
{
    "tag" : "",
    "chassisNames" : [],
    "affinityLevels" : []
}
```

`tag` : a tag that the pilot must have for this affinity to be considered

`chassisNames` : a list of chassis this affinity is available to. the chassis name is the prefab name followed by a `-` and the tonnage of the mech. In the event a the chassis has an assembly variant (from custom salvage), this will be used instead of the prefab. example chassis name for the assassin `chrPrfMech_assassinBase-001_40`

`affinityLevels` : a list of `affinityLevel` objects to be considered for this affinity

### PilotQuirk objects

```json
{
    "tag" : "",
    "quirkName" : "",
    "description" : "",
    "effectData" : [],
    "quirkEffects" : []
}
```

`tag` : a tag that the pilot must have for to be awarded this quirk

`quirkName` : a human-readable name for this quirk, will be used in tooltips

`description` : a description about what this quirk does

`effectData` : a list of status effects that this quirk applies

`quirkEffects` : a list of `QuirkEffect` objects that will be applied to this quirk


### QuirkEffect objects

```json
{
  "type" : "",
  "modifier" : 0,
  "secondaryModifier" : 0,
  "affectedIds" : []
}
```

QuirkEffect objects are for managing effects pilot quirks should apply that are sim game related
and cannot be done by status effects. not all effect types use all the fields available (unused fields for that effect are ignored)
pilots with multiple quirk effects of the same type are additive

available types:

- `MedTech`, `MechTech` and `Morale`
These types are used to modify the companies MedTech or MechTech or Morale levels respectively by the amount specified by `modifier`. this can be an int or a float
and can increment or decrement these levels for example one quirk could add `0.6` mechtech points while another could add `1.4` and a third could add `-0.1`
this would net an overall boost of 1, with 0.9 leftover and stored for when the pilot roster changes

- `PilotCostFactor`
This type modifies the amount it costs to hire and monthly pay of a pilot. this works as a multiplier to the standard costs of the pilot by the amount 
specified by `modifier` field. A modifier that is postive increases a pilots cost, while a negative decreases costs. 
examples: a modifier of `0.3` with increase the cost of a pilot by 30%, while a value of `-0.25` will decrease the pilots cost by 25%

- `CriminalEffect`
This type introduces a pilot's ability to steal either for you or from you. When a day passes all pilots with this effect make a roll. on a successful 
roll they will steal a specified amount. the chance to steal is governed by `modifier` which specifies the percentage (as an int) to successfully roll
a steal. `secondaryModifier` specifies the amount of cbills to steal when a successful roll is made, a postive amount steals from you, a negative amount
steals from you. for example a modifier of `9` and a secondaryModifier with a value of 500 gives the pilot a 9% chance to steal 500 cbills from you
when a day passes.

- `ArgoUpgradeFactor` and `ArgoUpkeepFactor`
These types affect the pilots ability to reduce or increase the upfront cost or the monthly upkeep of an argo upgrade. the `modifier` field is a float that acts as a multiplier
to the base/upkeep cost of the upgrade. `affectedIds` is a list of argo upgrade IDs that this quirk affects. to affect all upgrades a value of `PqAllArgoUpgrades` can be 
added to this list. example: `ArgoUpgradeFactor` with a modifier of `-0.3` and affectedIds list equal to `[PqAllArgoUpgrades]` will give a 30% cost reduction to 
the purchase cost of all upgrades. while a `ArgoUpkeepFactor` with a modifier of `0.15` will increase the monthly upkeep of all affected upgrades.


## Giving AI Pilots Affinities

non player pilots can also be setup to recieve affinities. to do this add a pliot tag of `affinityLevel_X` where X is the number of deployments that should be granted to the pilot. pilots with this tag will be able to recieve all affinites (Global, Chassis, Quirk & Tagged) that a player pilot of equal deployments is applicable for

## Affinities By Tags

pilots may be granted experience towards affinities (of all types) by having special tags. there are 2 variants Permanent tags and Consumable tags. These tags can be part of a pilotdef when a pilot is generated
or added by events.

Permanent Tags: These tags provide the pilot with a permanent boost to their affinity count for a given chassis (unless they lose the tag).
permanant tags follow this scheme `MaPermAffinity_X=prefabId` where X is the number of deployments to be given, and prefabId is the chassis that this boost should be given to.

Example: Pilot Raza has been given the tag `MaPermAffinity_6=chrPrfMech_urbanmechBase-001_30` this means Raza has a permanent 6 points added when using the chassis `chrPrfMech_urbanmechBase-001_30` (the UrbanMech)

Consumable Tags: These tags provide the pilot with a boost to their affinity count for a given chassis. when a day passes this tag will be removed and the number of points will be added to the tracking stat.
these boosts are therefore subject to decay as normal affinity points are.
Consumable tags follow this scheme `MaConsumableAffinity_X=prefabId` where X is the number of deployments to be given, and prefabId is the chassis that this boost should be given to.

Example: Pilot Raza has been given the tag `MaConsumableAffinity_5=chrPrfMech_urbanmechBase-001_30` this means Raza has a 5 point boost added when using the chassis `chrPrfMech_urbanmechBase-001_30` (the UrbanMech),
overtime this may decay if Raza decides to pilot another mech.
