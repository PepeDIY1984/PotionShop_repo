# Potion Mortar — Unity 3D prototype

A small, playable sandbox prototype for a potion crafting game. The scene is built from Unity primitives so it needs no external assets.

## Open and run

1. Create or open a Unity 3D project (Unity 2021.3 LTS or newer).
2. Copy the `Assets/PotionMortarPrototype` folder into the project's `Assets` folder.
3. In Unity, choose **Potion Mortar → Create Demo Scene** once. This saves the table, mortar, cauldron, and ingredients as ordinary scene objects.
4. Press **Play**. Later, open the saved scene and press **Play**; the objects are no longer created at runtime.

If Unity's Input Handling setting is set to **Input System Package (New)** only, switch it to **Both** in Project Settings → Player → Active Input Handling, then restart the editor. This prototype uses Unity's built-in `Input` API.

## Controls

- Click an ingredient on the front shelf and drag it into either station. Ingredients use colliders and a camera raycast.
- The smaller **Mortero** is for grinding: click the pestle itself and drag it over the ingredients. You can also drag ingredients around inside the mortar.
- The larger **Caldero** is for mixing: click the ladle and drag it around the cauldron. Labels above each station show its purpose.
- In the Scene view, the mortar gizmos show its working radius and the pestle's tilt limit. Move the **Mortero** object to reposition the whole mortar; select **Potion Mortar Demo** to edit **Mortar Radius** and **Max Rotation Degrees** in the Inspector.
- Grind ingredients in the mortar until each reaches 55%, then move them to the cauldron and stir until it reaches 100%.
- Grind two ingredients in the mortar, move both to the cauldron, then choose **Preparar poción**.
- Known recipes: lunar root + blue mushroom = healing; blue mushroom + fire petal = mana; lunar root + fire petal = speed. A correct potion gives 10 points.
- Press **R** or use the reset button to return all ingredients and reset the score.

The upper bowl materials for the mortar and cauldron are set to 48% opacity so the interiors remain visible.
