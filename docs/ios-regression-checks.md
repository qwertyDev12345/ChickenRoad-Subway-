# iOS release checks

## Rebuild

Re-export from Unity after updating the project. `IosBuildOptimization` applies
to both manual and CI iOS builds: IL2CPP OptimizeSize, Master compiler,
Medium managed stripping, engine stripping, ASTC 6x6 sprite textures capped at
1024 pixels. Original source artwork is preserved. Check thin sprite borders
and character details on a real device after changing compression.

Apply `EasyLaunch-Patch/patch.sh` to the new export using the project's configured
values so the notification controller update reaches Xcode. Updating an older
Xcode export alone does not include the C# or texture changes.

## Regression checks

- Unity Test Runner > PlayMode > RiverTapRegressionTests: verifies a touchscreen
  press starts a jump, starting the round cannot also jump, decorative HUD does
  not block input, END RUN retains its raycast target and hit region, and
  completed rounds reject jumps. The test loads SampleScene in the real player loop.
  Full rendered EventSystem raycasting still needs the device check below;
  Unity Batch Mode leaves UI rendering depth at -1.
- On macOS: `bundle exec bash EasyLaunch-Patch/Tests/run_ios_tests.sh` includes
  the notification background regression: fade is baked into a single UIImage,
  and that same image fills the view through repeated portrait/landscape resizes.
- On device: rotate the notification screen portrait > landscape > portrait
  several times, including during the transition. The entire background must
  remain dimmed and both buttons must work.
- Test River Jump on a device with short taps, repeated taps, END RUN, replay,
  first-run tutorial, collection selection, and returning from the background.

## Size budget

Fastlane checks the exported IPA before uploading to TestFlight:

```sh
python3 scripts/check_ios_size.py path/to/Game.ipa --report build/ios-size-report.json
```

Both the IPA and its uncompressed Payload must be **below 95,000,000 bytes**.
This reserves 5 MB below the user's 100 MB limit for App Store processing.
The report includes the twenty largest files and is saved as a CI artifact,
including when the budget fails. An oversized build is not uploaded.

The gate is not proof of Apple's final download or installed size: encryption,
thinning and filesystem allocation can change those values. Check the processed
build in App Store Connect/TestFlight and the freshly installed app in iOS
Storage; both must be below 100 MB. User-selected photos and accumulated app
data are separate from the initial application bundle. No post-change signed
IPA or final App Store size has been measured on this Windows workstation.
