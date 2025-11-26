AutoTagChildren

This Editor script helps you add tags 'Reward' and 'Boundary' to children of GameObjects, and optionally creates the tags if not present.

Usage:
1. Open the Unity Editor and open your Scene.
2. From the top menu: Tools -> Auto Tag -> Tag Rewards and Walls
   - This will look for GameObjects (root or nested) whose names contain 'reward', 'sphere', or 'wall' and will tag their children as 'Reward' or 'Boundary' respectively. Missing tags are created if possible.
3. Alternatively, select any parent GameObject in the Hierarchy, then run: Tools -> Auto Tag -> Tag Selected Parent's Children
   - The script will attempt to auto-detect whether the tag should be 'Reward' or 'Boundary' based on the selected object's name. If it cannot detect, it prompts you to choose.

Notes:
- The script uses editor-only APIs. It will not run in play mode or in builds.
- If you prefer to assign tags manually, you can do so in the Unity Inspector for each GameObject.
- It is recommended to save your Scene before running the script. Undo operations are supported for tagging changes.

Optional:
- There's also a change in `WaypointReward.cs` to set the 'Reward' tag in the Editor when the tag exists (OnValidate). This ensures waypoints keep the correct tag while editing.
