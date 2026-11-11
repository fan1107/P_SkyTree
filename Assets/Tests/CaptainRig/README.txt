Captain Unity cutout rig prototype

Open CaptainRigTest.unity and press Play.
Left: Breathe. Middle: Walk. Right: Run.
Compare restores the three different motions. Breathe / Walk / Run switches all characters.
Pause / Resume and the speed slider control playback.
CaptainRig.prefab is the reusable character. Animation clips are in Animations/.
Each Art PNG is independent; transforms under Hips are the animation joints.

This is a Transform-based cutout prototype, not a SpriteSkin mesh or Spine asset.
Arms and legs are each single pieces: elbow/knee articulation, IK and planted-foot locomotion are not implemented.
Walk and Run are in-place animation loops. The character does not move across the scene.
The parts atlas was AI reconstructed from the original Captain_01.png, so some details differ.
Original art and game scenes were preserved.

Validation: 9 sprites per rig, all clip binding paths resolve, all three clips loop,
pose changes sampled successfully, and all three Animator states confirmed in Play Mode.
