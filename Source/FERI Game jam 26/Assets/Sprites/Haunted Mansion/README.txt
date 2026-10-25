HAUNTED MANSION - 16x16 PIXEL ART SIDESCROLLER TILESET  v1.0
by bubbabba  -  https://bubbabba.itch.io

WHAT IS INSIDE
  tileset/haunted-mansion_tiles_16.png    250 tiles on a 16x16 grid
  tileset/haunted-mansion_tiles_32.png    the same atlas at 2x, for 32x32 projects
  tileset/tiles/                          every tile as its own PNG, named
  props/                                  58 static props (a grandfather clock, bookcase,
                                          wingback chair, chaise longue, fireplace, suit of
                                          armour, busts and urns; a writing desk, globe,
                                          telescope, spirit table, a stuffed raven and an
                                          empty birdcage; a four-poster bed, wardrobe,
                                          vanity, rocking horse, porcelain doll, dollhouse,
                                          cradle and music box; a dining table and chairs;
                                          dust-sheeted furniture, a steamer trunk, a dress
                                          form, coffins; a wine rack, casks, shackles and a
                                          potion shelf; a dead tree, an iron gate, a lamp
                                          post, a stone angel, a gargoyle, a dry fountain, a
                                          well, a bench, a mausoleum, tombstones and a
                                          scarecrow...)
  animated/*_strip.png                    30 animated props as horizontal frame strips
                                          (candelabrum, wall sconce, chandelier, a roaring
                                          fire, a lamp post, jack-o'-lantern, cauldron,
                                          candle; a portrait whose eyes follow you, a clock
                                          pendulum, a rocking chair, a music box, a
                                          gramophone, a creaking door, a billowing curtain,
                                          lightning at the window, a floating book; wisps, a
                                          bedsheet ghost, ectoplasm, rolling fog, a crystal
                                          ball, spirit orbs; bats, a raven, a black cat, an
                                          owl, a rat and a spider)
  animated/frames/                        the same animations, one PNG per frame
  animated/gif/                           previews
  animated/frames.txt                     frame counts, sizes and suggested speeds
  backgrounds/                            parallax layers (a big moon behind torn cloud, far
                                          hills with a chapel spire and bare trees, a near
                                          ridge with gnarled oaks, a fence and the mansion
                                          itself), dusk and night, 384 px wide, loop
                                          seamlessly
  glow/                                   light pools (fire, lamp, ghost; 32/64/128 px) as
                                          alpha overlays
  characters/                             12 characters (lady in white, butler, maid, widow,
                                          ghost hunter, medium, count, skeleton, witch,
                                          orphan, groundskeeper, occultist), each with an
                                          8-frame idle and an 8-frame walk, 32x40 px
  scenes/                                 the example scenes from the store page as PNGs
  tiled/                                  Tiled tilesets (.tsx) + the grand hall, the attic
                                          storey and the grounds as maps (.tmx), prop
                                          animations set up

THE TILES
  grounds_*      grave loam under a dead lawn, 9-slice (tl t tr / l c r / bl b br)
  ashlar_*       moonlit foundation blocks, 9-slice
  slates_*       overlapping roof slates, 9-slice
  gravel_top_0-1, gravel_0-3              a gravel drive: surface and fill
  damaskwall_0-3, stripewall_0-3, panelwall_0-3, stonewall_0-3, brickwall_0-3,
  scalewall_0-3, atticwall_0-3, cellarwall_0-3, floorboards_0-3   2x2 back-wall fills
  window_0-1, oculus_0-1, portrait_0-1, mirror_0-1, tornpaper_0-1, claws_0-1,
  clockface_0-1, baywin_0-1   back-wall inserts (_1 is the lit, cracked or slashed one)
  column_t/m/b, newel_t/m/b, pier_t/m/b, drainpipe_t/m/b    columns and posts
  hallfloor_*, gallery_*, cornice_*, bookshelf_*, rafter_*, ledge_*, ironbalcony_*
                 storey floors, a landing, eaves, shelves, beams, a string course and a
                 balcony: l, m, m2, r
  ladder_t/m/b, stairs_l/r + stairs_fill  stairs rise 12 px per tile; chain them
                                          diagonally with stairs_fill underneath
  ironrail_0-1, hedge_0-1, balustrade_0-1 fences
  pond_0-1, pond_top_0-1, ecto_0-1, ecto_top_0-1      liquids and their surfaces
  cobweb_0-1, cracks_0-1, ivy_0-1, drapes_0-1, ooze_0-1, dust_0-1, moonbeam_0-1
                 overlays
  junk_0-2, weeds_0-1, leaves_0-1, toadstools, runner_0-1   clutter for the floor row

SEAMLESS WALLS
  The grounds, ashlar and slate textures repeat every 2 tiles, so each 9-slice piece
  comes in four versions: no suffix, _v1, _v2, _v3. Pick by where the tile sits:
      (x even, y even) -> none   (x odd, y even) -> _v1
      (x even, y odd)  -> _v2    (x odd, y odd)  -> _v3
  Or ignore it - any version works, the matching one just hides the repeat.
  The 2x2 back walls work the same way: damaskwall_(x%2 + 2*(y%2)).

WHERE THINGS STAND
  The walkable surface is not the very top pixel of a floor tile - most have a lip,
  turf or a rail above it. Stand props and characters on the surface line:
      grounds_t, gravel_top                        surface on row 3 (from the top)
      hallfloor, stairs, pond_top, ecto_top        surface on row 4
      ledge                                        row 6      pier_t     row 7
  So a prop on a tile at tile row y whose surface is row r goes at pixel
  y*16 + r - prop height.
  Floor-row clutter overlays (drifts, junk, bones and the like) are drawn standing on
  the bottom edge of their tile: in the tile above a floor they float a few pixels,
  so put them on an object layer and drop them onto the surface line. The example
  maps do exactly this.

PROPS
  Props sit on the floor line: put the bottom row of the sprite on the top row of
  the floor tile. Hanging props (the chandelier, sconces, the portrait, the storm
  window, bats, the spider, wisps and ghosts) hang from their top row. Animated
  props loop; suggested speeds are in frames.txt.

CHARACTERS
  characters/<name>_idle_strip.png and <name>_walk_strip.png, 8 frames of 32x40.
  They face right; flip for left. The feet stand on the bottom 2 rows.

DUSK AND NIGHT
  The scenes come in two moods. Dusk is the tiles as drawn under a violet overcast
  sky. Night is the same tiles under a deep blue layer (RGBA 10,12,44 at about 47%
  opacity) with the glow overlays over the candles and lamps.

COLOUR VARIANTS - 3 PALETTES
  variants/crimson/ and variants/spectral/ hold the whole pack re-drawn in another
  palette: tileset (16 and 32 px atlases + single tiles), props, animated strips and
  GIFs, parallax backgrounds and the example scenes.
    crimson   blood-red damask, wine-dark paper, rust lawns, a red moon
    spectral  sea-green paper, grey-teal wood and stone, a green sky
  Every atlas has the same layout as the moonlit one, so a Tiled map switches palette
  by pointing its tileset at the other image. Characters are shared by all three.

HOW IT WAS MADE
  Every tile, prop and character is drawn procedurally by custom Python code
  on a strict 16 px grid - no image-generation models. The tooling was built with
  help from an AI coding assistant.

LICENSE - see LICENSE.txt
