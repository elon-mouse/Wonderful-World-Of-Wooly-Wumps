# Importer features

## Default Texture settings

The Aseprite Importer sets the following default settings to the generated texture:
- <b>Texture type:</b> Sprite
- <b>Sprite mode:</b> Multiple
- <b>Pixel per unit:</b> 100
- <b>Mesh type:</b> Tight
- <b>Wrap mode:</b> Clamp
- <b>Filter mode:</b> Point
- <b>Compression:</b> None
- <b>Generate Mip Maps:</b> False
- <b>Texture Max Size:</b> 16384

## Aseprite Importer Inspector properties
The Aseprite Importer is available after you import a .ase/.aseprite file into your Project.

### General

| **Property** | **Description** |
|-|-|
| **Import Mode** | How the file should be imported. This is set to **Animated Sprite** by default. The options are:<ul><li><strong>Sprite Sheet</strong>: The file is imported as a Sprite Sheet, and can be sliced up in the Sprite Editor.</li><li><strong>Animated Sprite</strong>: The file is imported with animation in mind. Animation assets are generated and attached to a model prefab on import.</li><li><strong>Tile Set</strong>: The importer finds all tile data in the file and generates Unity Tilemap assets on import.</li></ul> |
| **Pixels Per Unit** | Set the number of pixels that equals one Unity unit. |
| **Mesh Type** | Set the Mesh type that Unity generates for the Sprite. This is set to **Tight** by default. The options are:<ul><li><a href="https://docs.unity3d.com/Documentation/ScriptReference/SpriteMeshType.FullRect.html"><strong>Full Rect</strong></a>: Unity maps the Sprite onto a rectangular Mesh.</li><li><strong><a href="https://docs.unity3d.com/Documentation/ScriptReference/SpriteMeshType.Tight.html">Tight</a></strong>: Unity generates a Mesh based on the outline of the Sprite. If the Sprite is smaller than 32 x 32 pixels, Unity always maps it onto a **Full Rect** quad Mesh, even if you select **Tight**.</li></ul> |
| **Generate Physics Shape** | Generates a default physics shape from the outline of the Sprite/s when a physics shape has not been set in the Sprite Editor. |

### Layer import (For Animated Sprites)

| **Property** | **Description** |
|-|-|
| **Include Hidden Layers** | Enable this property to include the hidden layers of the .ase/.aseprite file in the import. This property is set to **False** by default. |
| **Import Mode** | Use this property to specify how the layers from the source file are imported. This property is set to **Merge Frame** by default. The options are:<ul><li><strong>Individual Layers</strong>: Every layer per frame generates a Sprite.</li><li><strong>Merge Frame</strong>: This is the default option. All layers per frame are merged into one Sprite.</li></ul> |
| **Pivot Space** | Select the space pivots should be calculated in. This property is set to **Canvas** by default. The options are:<ul><li><strong>Canvas</strong>: Calculate the pivot based on where the Sprite is positioned on the source asset's canvas. This is useful if the Sprite is being swapped out in an animation.</li><li><strong>Local</strong>: This is the normal pivot space used when importing a standard image in Unity.</li></ul> |
| **Pivot Alignment** | How a Sprite's graphic rectangle is aligned with its pivot point. This property is set **Bottom** by default. |
| **Mosaic Padding** | External padding between each SpriteRect. This property is set **4** pixels by default. |
| **Sprite Padding** | Internal padding within each SpriteRect. This property is set **0** pixels by default. |

### Layer import (For Tile Set)

| **Property** | **Description** |
|-|-|
| **Mosaic Padding** | External padding between each SpriteRect. This property is set **4** pixels by default. |

### Generate assets (For Animated Sprites)

| **Property** | **Description** |
|-|-|
| **Model Prefab** | Enable this property to generate a model prefab setup to look like the first frame in Aseprite. This property is set to **True** by default. |
| **Sorting Group** | Add a Sorting Group component to the root of the generated model prefab if it has more than one Sprite Renderer. This property is set to **True** by default. |
| **Shadow Casters** | Enable this property to add Shadow Casters to all GameObjects with a SpriteRenderer. This property is set to **False** by default. Note that this checkbox is only available in Unity 2023.1 and newer. |
| **Animation Clips** | Enable this property to generate Animation Clips based on the frame data in the file. Every tag in Aseprite generates one Animation Clip. If no tag is present, one Animation Clip is generated which covers all frames in the file. The Animation speed is based on the Constant Frame Rate defined in Aseprite. The length is based on the number of frames included in the tag/file. This property is set to **True** by default. |
| **Individual Events** | Enable this property to let Animation Events be generated with their own method names. If disabled, all events will be received by the method `OnAnimationEvent(string)`. This property is set to **True** by default. |
| **Export Animation Assets** | The Animator Controller and the Animation Clips are generated as Read-Only assets. This option can be used to export editable versions of these assets. |

## Aseprite Importer Preferences
The Aseprite Importer Preferences can be found at <b>Unity > Settings > 2D > Aseprite Importer</b>.

| **Property** | **Description** |
|-|-|
| **Background import** | Enable this property to enable asset import when the Unity Editor is in the background. This property is set to **True** by default. |
