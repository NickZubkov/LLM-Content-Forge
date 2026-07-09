Generate {{count}} distinct video-game items for the theme "{{theme}}".

Each item must have these fields:
- "name": short, evocative, fits the theme.
- "description": one sentence.
- "rarity": one of "common", "uncommon", "rare", "epic", "legendary".
- "power": integer balance value, between {{level_min}} and {{level_max}} inclusive.
- "value": integer gold cost, scaled to the item's power and rarity.

Return the {{count}} items as a JSON array.
