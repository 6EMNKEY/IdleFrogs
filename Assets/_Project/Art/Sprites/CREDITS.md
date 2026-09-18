# Placeholder sprite credits

All art here is placeholder. Both sources below are safe to ship commercially, but
replace them before launch — none of it is distinctive enough to carry a store page.

## Third-party (CC0)

**`Frogs/frog.png`, `Frogs/frog_flat.png`**
From *Animal Pack Redux* by **Kenney** (kenney.nl).
License: **CC0 1.0 Universal (public domain)** — no attribution required, commercial
use permitted, modification permitted.
Source: https://opengameart.org/content/animal-pack-redux

The pack contains 30 animals in 8 styles; only the frog was taken, in the
"Round (outline)" and "Round" variants. The rest of the download was discarded.

## Generated

`Swamps/swamp.png`, `Swamps/fly.png`, `Train/train.png`, `Train/track.png`,
`Station/station.png`, `Shop/shop.png`

Drawn programmatically for this project, so no licence applies — they are yours.
Regenerate or restyle with:

```bash
python3 Tools/Build/generate_placeholder_sprites.py
```

The palette is sampled from Kenney's frog (`#2ECC71` fill, `#1B8045` outline) so the
generated pieces and the CC0 frog read as one set. Edit the colour constants at the
top of that script to reshuffle the whole look at once.

## Why these are generated rather than downloaded

No free pack exists containing a matching cartoon frog, swamp, train, station and
shop. Assembling them from separate sources produces a set that visibly does not
belong together, which is worse than clean flat placeholders for judging layout and
composition. If you want real art later, both of these are genuinely good:

- https://kenney.nl/assets — 60,000+ CC0 assets, no attribution
- https://opengameart.org — filter by CC0 to avoid attribution obligations
