Generate {{count}} distinct video-game enemies for the theme "{{theme}}".

Each enemy must have these fields:
- "name": short, evocative, fits the theme.
- "description": one sentence.
- "level": integer, between {{level_min}} and {{level_max}} inclusive.
- "health": integer, scaled to the enemy's level.
- "damage": integer, scaled to the enemy's level.

Return the {{count}} enemies as a JSON array.
